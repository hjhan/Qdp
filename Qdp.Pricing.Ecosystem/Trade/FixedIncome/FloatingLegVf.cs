using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Ecosystem.Utilities;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Common.Utilities.Coupons;

namespace Qdp.Pricing.Ecosystem.Trade.FixedIncome
{
	public class FloatingLegVf : ValuationFunction<FloatingLegInfo, SwapLeg>
	{
		public FloatingLegVf(FloatingLegInfo tradeInfo)
			: base(tradeInfo)
		{
		}

		public override SwapLeg GenerateInstrument()
		{
			var calendar = TradeInfo.Calendar.ToCalendarImpl();
			var startDate = TradeInfo.StartDate.ToDate();
			var maturityDate = !string.IsNullOrEmpty(TradeInfo.Tenor) ?new DayGap("+0BD").Get(calendar, new Term(TradeInfo.Tenor).Next(startDate)) : TradeInfo.MaturityDate.ToDate();
			var swapDirection = TradeInfo.SwapDirection.ToSwapDirection();
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
			return new SwapLeg(startDate,
				maturityDate,
				-TradeInfo.Notional * swapDirection.Sign(),
                true,
				TradeInfo.Currency.ToCurrencyCode(),
				floatingCoupon,
				calendar,
				TradeInfo.FloatingLegFreq.ToFrequency(),
				TradeInfo.FloatingLegStub.ToStub(),
				TradeInfo.FloatingLegDC.ToDayCountImpl(),
				TradeInfo.FloatingLegBD.ToBda()
				);
		}

		public override IEngine<SwapLeg> GenerateEngine()
		{
			return new CashflowProductEngine<SwapLeg>();
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
                    x => x.FixingCurve.Value = market.GetData<CurveData>(valuationParameter.FixingCurveName).YieldCurve,
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
                x => x.FixingCurve.Value = prebuiltMarket.YieldCurves[valuationParameter.FixingCurveName],
                x => x.HistoricalIndexRates.Value = prebuiltMarket.HistoricalIndexRates
                );
        }
    }
}
