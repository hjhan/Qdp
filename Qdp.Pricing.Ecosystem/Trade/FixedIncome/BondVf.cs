using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Common.Utilities.Coupons;

namespace Qdp.Pricing.Ecosystem.Trade.FixedIncome
{
	public class BondVf : ValuationFunction<BondInfoBase, Bond>
	{
		public BondVf(BondInfoBase tradeInfo)
			: base(tradeInfo)
		{
		}

		public override Bond GenerateInstrument()
		{
			ICoupon coupon = null;
			var calendar = string.IsNullOrEmpty(TradeInfo.Calendar) ? CalendarImpl.Get("chn_ib") : TradeInfo.Calendar.ToCalendarImpl();

			if (TradeInfo is FixedRateBondInfo)
			{
				var tempTradeInfo = (FixedRateBondInfo)TradeInfo;
				coupon = new FixedCoupon(tempTradeInfo.FixedCoupon);
			}	
			else if (TradeInfo is FloatingRateBondInfo)
			{
				var tempTradeInfo = (FloatingRateBondInfo)TradeInfo;

				var index = new Index(tempTradeInfo.Index.ToIndexType(), tempTradeInfo.ResetAverageDays, tempTradeInfo.ResetCompound.ToCouponCompound(), tempTradeInfo.ResetRateDigits);
				
				var dayCount = tempTradeInfo.ResetDC.ToDayCountImpl();
				var stub = tempTradeInfo.ResetStub.ToStub();
				var bda = tempTradeInfo.ResetBD.ToBda();
				
				var floatingCouponResetTerm = new Term(tempTradeInfo.ResetTerm);

				var resetFrequency = tempTradeInfo.PaymentFreq.ToFrequency();
				if (floatingCouponResetTerm.Equals(resetFrequency.GetTerm()))
				{
					floatingCouponResetTerm = null;
				}

				var resetToFixingGap = new DayGap(tempTradeInfo.ResetToFixingGap);
				coupon = new FloatingCoupon(
						index, 
						calendar, 
						dayCount, 
						tempTradeInfo.Spread, 
						floatingCouponResetTerm, 
						stub,
						bda,
						resetToFixingGap,
						null,
						double.IsNaN(tempTradeInfo.FloorRate) ? -100 : tempTradeInfo.FloorRate,
						double.IsNaN(tempTradeInfo.CapRate) ? 100 : tempTradeInfo.CapRate,
						tempTradeInfo.FloatingCalc.ToFloatingCalcType(),
						(double.IsNaN(tempTradeInfo.FloatingRateMultiplier) || tempTradeInfo.FloatingRateMultiplier.IsAlmostZero()) ? 1.0 : tempTradeInfo.FloatingRateMultiplier
					);
			}
			else if (TradeInfo is FixedDateCouonAdjustedBondInfo)
			{
				var tempTradeInfo = (TradeInfo as FixedDateCouonAdjustedBondInfo);
				coupon = new FixedDateAdjustedCoupon(tempTradeInfo.Index.ToIndexType(),
					calendar,
					tempTradeInfo.DayCount.ToDayCountImpl(),
					tempTradeInfo.FixedDateCouponAdjustedStyle.ToFixedDateAdjustedCouponStyle(),
					tempTradeInfo.AdjustMmDd,
					tempTradeInfo.FloatingRateMultiplier,
					tempTradeInfo.Spread
					);
			}

			return new Bond(
				TradeInfo.TradeId,
				TradeInfo.StartDate.ToDate(),
				TradeInfo.MaturityDate.ToDate(),
				TradeInfo.Notional,
				string.IsNullOrEmpty(TradeInfo.Currency) ? CurrencyCode.CNY : TradeInfo.Currency.ToCurrencyCode(),
				coupon,
				calendar,
				TradeInfo.PaymentFreq.ToFrequency(),
				string.IsNullOrEmpty(TradeInfo.PaymentStub) ? Stub.ShortEnd : TradeInfo.PaymentStub.ToStub(),
				string.IsNullOrEmpty(TradeInfo.AccrualDC) ? new ActActIsma() : TradeInfo.AccrualDC.ToDayCount().Get(),
				string.IsNullOrEmpty(TradeInfo.DayCount) ? null : TradeInfo.DayCount.ToDayCount().Get(),
				TradeInfo.AccrualBD.ToBda(),
				TradeInfo.PaymentBD.ToBda(),
				string.IsNullOrEmpty(TradeInfo.Settlement) ? new DayGap("+0D") : new DayGap(TradeInfo.Settlement),
				string.IsNullOrEmpty(TradeInfo.TradingMarket)
					? TradingMarket.ChinaInterBank
					: TradeInfo.TradingMarket.ToTradingMarket(),
				TradeInfo.StickToEom,
				(double.IsNaN(TradeInfo.RedemptionRate) || Math.Abs(TradeInfo.RedemptionRate - 0.0) < double.Epsilon)? null : new Redemption(TradeInfo.RedemptionRate, TradeInfo.RedemptionIncludeLastCoupon ? RedemptionType.SeparatePrincipalWithLastCoupon : RedemptionType.SeparatePrincipal),
				TradeInfo.FirstPaymentDate.ToDate(),
				TradeInfo.IsZeroCouponBond,
				TradeInfo.IssuePrice,
				(double.IsNaN(TradeInfo.IssueRate) || TradeInfo.IssueRate <= 0.0) ? double.NaN : TradeInfo.IssueRate,
				string.IsNullOrEmpty(TradeInfo.AmortizationType)
					? AmortizationType.None
					: TradeInfo.AmortizationType.ToAmortizationType(),
				TradeInfo.AmoritzationInDate == null
					? null
					: TradeInfo.AmoritzationInDate.ToDictionary(x => x.Key.ToDate(), x => x.Value),
				TradeInfo.AmoritzationInIndex,
				TradeInfo.RenormAmortization,
				TradeInfo.CompensationRate,
				TradeInfo.OptionToCall,
				TradeInfo.OptionToPut,
				TradeInfo.OptionToAssPut,
				TradeInfo.SettlementCoupon.IsAlmostZero() ? double.NaN : TradeInfo.SettlementCoupon,
				TradeInfo.RoundCleanPrice
				);
		}

		public override IEngine<Bond> GenerateEngine()
		{
			return new BondEngineCn(new BondYieldPricerCn());
		}

		public override IMarketCondition GenerateMarketCondition(QdpMarket market)
		{
            var prebuiltMarket = market as PrebuiltQdpMarket;
            if (prebuiltMarket != null)
            {
                return GenerateMarketConditionFromPrebuilt(prebuiltMarket);
            }
            else
            {
                var valuationParameter = TradeInfo.ValuationParamters;
                var bondMktData = market.GetData<BondMktData>(TradeInfo.TradeId);

                if (valuationParameter == null)
                {
                    return new MarketCondition(
                        x => x.ValuationDate.Value = market.ReferenceDate,
                        x => x.DiscountCurve.Value = null,
                        x => x.FixingCurve.Value = null,
                        x => x.RiskfreeCurve.Value = null,
                        x => x.MktQuote.Value = new Dictionary<string, Tuple<PriceQuoteType, double>> { { TradeInfo.TradeId, Tuple.Create((bondMktData == null || string.IsNullOrEmpty(bondMktData.PriceQuoteType)) ? PriceQuoteType.None : bondMktData.PriceQuoteType.ToPriceQuoteType(), (bondMktData == null ? 100.0 : bondMktData.Quote)) } },
                        x => x.HistoricalIndexRates.Value = market.HistoricalIndexRates
                        );
                }

                var fixingCurveName = string.IsNullOrEmpty(valuationParameter.FixingCurveName)
                    ? valuationParameter.DiscountCurveName
                    : valuationParameter.FixingCurveName;
                return new MarketCondition(
                        x => x.ValuationDate.Value = market.ReferenceDate,
                        x => x.DiscountCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
                        x => x.FixingCurve.Value = market.GetData<CurveData>(fixingCurveName).YieldCurve,
                        x => x.RiskfreeCurve.Value = market.GetData<CurveData>(valuationParameter.RiskfreeCurveName).YieldCurve,
                        x => x.MktQuote.Value = new Dictionary<string, Tuple<PriceQuoteType, double>> { { TradeInfo.TradeId, Tuple.Create((bondMktData == null || string.IsNullOrEmpty(bondMktData.PriceQuoteType)) ? PriceQuoteType.None : bondMktData.PriceQuoteType.ToPriceQuoteType(), (bondMktData == null ? 100.0 : bondMktData.Quote)) } },
                        x => x.HistoricalIndexRates.Value = market.HistoricalIndexRates
                        );
            }
		}

        private IMarketCondition GenerateMarketConditionFromPrebuilt(PrebuiltQdpMarket prebuiltMarket)
        {
            var valuationParameter = TradeInfo.ValuationParamters;
            BondMktData bondMktData = null;
            if (prebuiltMarket.BondPrices.ContainsKey(TradeInfo.TradeId))
            {
                bondMktData = prebuiltMarket.BondPrices[TradeInfo.TradeId];
            }
            if (valuationParameter == null)
            {
                return new MarketCondition(
                    x => x.ValuationDate.Value = prebuiltMarket.ReferenceDate,
                    x => x.DiscountCurve.Value = null,
                    x => x.FixingCurve.Value = null,
                    x => x.RiskfreeCurve.Value = null,
                    x => x.MktQuote.Value = new Dictionary<string, Tuple<PriceQuoteType, double>> { { TradeInfo.TradeId, Tuple.Create((bondMktData == null || string.IsNullOrEmpty(bondMktData.PriceQuoteType)) ? PriceQuoteType.None : bondMktData.PriceQuoteType.ToPriceQuoteType(), (bondMktData == null ? 100.0 : bondMktData.Quote)) } },
                    x => x.HistoricalIndexRates.Value = prebuiltMarket.HistoricalIndexRates);
            }

            var fixingCurveName = string.IsNullOrEmpty(valuationParameter.FixingCurveName)
                ? valuationParameter.DiscountCurveName
                : valuationParameter.FixingCurveName;

            return new MarketCondition(
                    x => x.ValuationDate.Value = prebuiltMarket.ReferenceDate,
                    x => x.DiscountCurve.Value = prebuiltMarket.YieldCurves[valuationParameter.DiscountCurveName],
                    x => x.FixingCurve.Value = prebuiltMarket.YieldCurves[fixingCurveName],
                    x => x.RiskfreeCurve.Value = prebuiltMarket.YieldCurves[valuationParameter.RiskfreeCurveName],
                    x => x.MktQuote.Value = new Dictionary<string, Tuple<PriceQuoteType, double>> { { TradeInfo.TradeId, Tuple.Create((bondMktData == null || string.IsNullOrEmpty(bondMktData.PriceQuoteType)) ? PriceQuoteType.None : bondMktData.PriceQuoteType.ToPriceQuoteType(), (bondMktData == null ? 100.0 : bondMktData.Quote)) } },
                    x => x.HistoricalIndexRates.Value = prebuiltMarket.HistoricalIndexRates
                    );
        }

    }
}
