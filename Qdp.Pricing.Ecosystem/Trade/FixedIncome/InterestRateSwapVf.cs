using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Common.Utilities.Coupons;

namespace Qdp.Pricing.Ecosystem.Trade.FixedIncome
{
	public class InterestRateSwapVf : ValuationFunction<InterestRateSwapInfo, InterestRateSwap>
	{
		public InterestRateSwapVf(InterestRateSwapInfo tradeInfo)
			: base(tradeInfo)
		{
		}

		public override InterestRateSwap GenerateInstrument()
		{
			var calendar = TradeInfo.Calendar.ToCalendarImpl();
			var startDate = TradeInfo.StartDate.ToDate();
			Date maturityDate;
			string tenor;
			if (!string.IsNullOrEmpty(TradeInfo.Tenor))
			{
				maturityDate = new DayGap("+0BD").Get(calendar, new Term(TradeInfo.Tenor).Next(startDate));
				tenor = TradeInfo.Tenor;
			}
			else
			{
				maturityDate = TradeInfo.MaturityDate.ToDate();
				tenor = new Term(maturityDate - startDate, Period.Day).ToString();
			}
			var swapDirection = TradeInfo.SwapDirection.ToSwapDirection();
			var fixedLeg = new SwapLeg(startDate,
				maturityDate,
				TradeInfo.Notional * swapDirection.Sign(),
				false,
				TradeInfo.Currency.ToCurrencyCode(),
				new FixedCoupon(TradeInfo.FixedLegCoupon),
				calendar,
				TradeInfo.FixedLegFreq.ToFrequency(),
				TradeInfo.FixedLegStub.ToStub(),
				TradeInfo.FixedLegDC.ToDayCountImpl(),
				TradeInfo.FixedLegBD.ToBda()
				);

			var floatingLegFrequency = TradeInfo.FloatingLegFreq.ToFrequency();
			var floatingCouponResetTerm = new Term(TradeInfo.ResetTerm);
			if (floatingCouponResetTerm.Equals(floatingLegFrequency.GetTerm()))
			{
				floatingCouponResetTerm = null;
			}
			var floatingCoupon =
				new FloatingCoupon(
					new Index(TradeInfo.Index.ToIndexType(), 1, TradeInfo.ResetCompound.ToCouponCompound()),
					calendar,
					TradeInfo.FloatingLegDC.ToDayCountImpl(),
					0.0,
					floatingCouponResetTerm,
					TradeInfo.ResetStub.ToStub(),
					TradeInfo.ResetBD.ToBda(),
					new DayGap(TradeInfo.ResetToFixingGap));
			var floatingLeg = new SwapLeg(startDate,
				maturityDate,
				-TradeInfo.Notional * swapDirection.Sign(),
				false,
				TradeInfo.Currency.ToCurrencyCode(),
				floatingCoupon,
				calendar,
				TradeInfo.FloatingLegFreq.ToFrequency(),
				TradeInfo.FloatingLegStub.ToStub(),
				TradeInfo.FloatingLegDC.ToDayCountImpl(),
				TradeInfo.FloatingLegBD.ToBda()
				);

			return new InterestRateSwap(fixedLeg, floatingLeg, swapDirection, tenor);
		}

		public override IEngine<InterestRateSwap> GenerateEngine()
		{
			return new IrsEngine();
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

                return new MarketCondition(
                    x => x.ValuationDate.Value = market.ReferenceDate,
                    x => x.DiscountCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
                    x => x.FixingCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
                    x => x.HistoricalIndexRates.Value = market.HistoricalIndexRates
                    );
            }
		}

        private IMarketCondition GenerateMarketConditionFromPrebuilt(PrebuiltQdpMarket prebuiltMarket)
        {
            var valuationParameter = TradeInfo.ValuationParamters;
            return new MarketCondition(
                x => x.ValuationDate.Value = prebuiltMarket.ReferenceDate,
                x => x.DiscountCurve.Value = prebuiltMarket.YieldCurves[valuationParameter.DiscountCurveName],
                x => x.FixingCurve.Value = prebuiltMarket.YieldCurves[valuationParameter.DiscountCurveName],
                x => x.HistoricalIndexRates.Value = prebuiltMarket.HistoricalIndexRates);
        }
    }
}
