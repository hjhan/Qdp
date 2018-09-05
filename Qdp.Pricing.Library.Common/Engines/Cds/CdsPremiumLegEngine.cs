using System;
using System.Linq;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities;

namespace Qdp.Pricing.Library.Common.Engines.Cds
{
	public class CdsPremiumLegEngine : Engine<SwapLeg>
	{
		public override IPricingResult Calculate(SwapLeg premiumLeg, IMarketCondition market, PricingRequest request)
		{
			var valuationDate = market.ValuationDate;
			var result = new PricingResult(valuationDate, request);

			if (result.IsRequested(PricingRequest.Pv))
			{
				result.Pv = CalcRPv(premiumLeg, market);
				
			}

			var accDates = premiumLeg.Accruals.ToArray();
			if (result.IsRequested(PricingRequest.Ai))
			{
				if (!accDates.Any() || valuationDate >= accDates.Last() || valuationDate <= premiumLeg.StartDate)
				{
					result.Ai = 0.0;
				}
				else
				{
					var idx = Array.FindIndex(accDates, x => x > valuationDate) - 1;
					var dcf = premiumLeg.DayCount.CalcDayCountFraction(accDates[idx], valuationDate);
					CfCalculationDetail[] temp;
					var coupon = premiumLeg.Coupon.GetCoupon(accDates[idx], accDates[idx + 1], market.FixingCurve.Value, market.HistoricalIndexRates, out temp);
					result.Ai = premiumLeg.Notional * coupon * dcf;
				}
			}

			return result;
		}

		private double CalcRPv(SwapLeg premiumLeg, IMarketCondition market)
		{
			var valuationDate = market.ValuationDate;
			if (valuationDate >= premiumLeg.UnderlyingMaturityDate)
			{
				return 0.0;
			}

			var rPv = 0.0;
			var accDates = premiumLeg.Accruals.ToArray();
			var basis = premiumLeg.DayCount;
			var dcCurve = market.DiscountCurve.Value;
			var spcCurve = market.SurvivalProbabilityCurve.Value;
			CfCalculationDetail[] temp;
			if (valuationDate <= premiumLeg.StartDate)
			{
				for (var i = 1; i < accDates.Length; ++i)
				{
					var dcf = basis.CalcDayCountFraction(accDates[i-1], accDates[i]);
					var df = dcCurve.GetDf(valuationDate, accDates[i]);
					var coupon = premiumLeg.Coupon.GetCoupon(accDates[i-1], accDates[i], market.FixingCurve.Value, market.HistoricalIndexRates, out temp);
					var prob0 = spcCurve.GetSpotRate(accDates[i - 1]);
					var prob1 = spcCurve.GetSpotRate(accDates[i]);
					rPv += df*dcf*(prob0 + prob1)/2.0*coupon;
				}
			}
			else
			{
				var idx = Array.FindIndex(accDates, x => x > valuationDate) - 1;
				var probability = market.SurvivalProbabilityCurve.Value.GetSpotRate(accDates[idx + 1]);
				var dcf1 = premiumLeg.DayCount.CalcDayCountFraction(accDates[idx], valuationDate);
				var dcf2 = premiumLeg.DayCount.CalcDayCountFraction(valuationDate, accDates[idx + 1]);
				var dcf3 = premiumLeg.DayCount.CalcDayCountFraction(accDates[idx], accDates[idx + 1]);
				var coupon = premiumLeg.Coupon.GetCoupon(accDates[idx], accDates[idx + 1], market.FixingCurve.Value, market.HistoricalIndexRates, out temp);
				var df = market.DiscountCurve.Value.GetDf(valuationDate, accDates[idx + 1]);
				rPv = df*((1.0 - probability)*(dcf1 + 0.5*dcf2) + probability*dcf3)*coupon;
				for (var i = idx + 2; i < accDates.Length; ++i)
				{
					var df2 = market.DiscountCurve.Value.GetDf(valuationDate, accDates[i]);
					var dcf4 = premiumLeg.DayCount.CalcDayCountFraction(accDates[i - 1], accDates[i]);
					var probability2 = market.SurvivalProbabilityCurve.Value.GetSpotRate(accDates[i - 1]);
					var probability3 = market.SurvivalProbabilityCurve.Value.GetSpotRate(accDates[i]);
					coupon = premiumLeg.Coupon.GetCoupon(accDates[i - 1], accDates[i], market.FixingCurve.Value, market.HistoricalIndexRates, out temp);
					rPv += 0.5*df2*dcf4*(probability2 + probability3)*coupon;
				}
			}
			return rPv*premiumLeg.Notional;
		}
	}
}
