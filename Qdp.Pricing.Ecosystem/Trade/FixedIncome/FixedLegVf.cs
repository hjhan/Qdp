using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Ecosystem.Utilities;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Common.Utilities.Coupons;

namespace Qdp.Pricing.Ecosystem.Trade.FixedIncome
{
	public class FixedLegVf : ValuationFunction<FixedLegInfo, SwapLeg>
	{
		public FixedLegVf(FixedLegInfo tradeInfo)
			: base(tradeInfo)
		{
		}

		public override SwapLeg GenerateInstrument()
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
			return new SwapLeg(startDate,
				maturityDate,
				TradeInfo.Notional * swapDirection.Sign(),
				true,
				TradeInfo.Currency.ToCurrencyCode(),
				new FixedCoupon(TradeInfo.FixedLegCoupon),
				calendar,
				TradeInfo.FixedLegFreq.ToFrequency(),
				TradeInfo.FixedLegStub.ToStub(),
				TradeInfo.FixedLegDC.ToDayCountImpl(),
				TradeInfo.FixedLegBD.ToBda()
				);
		}

		public override IEngine<SwapLeg> GenerateEngine()
		{
			return new CashflowProductEngine<SwapLeg>();
		}

		public override IMarketCondition GenerateMarketCondition(QdpMarket market)
		{
			var valuationParameter = TradeInfo.ValuationParamters;

			return new MarketCondition(
				x => x.ValuationDate.Value = market.ReferenceDate,
				x => x.DiscountCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
				x => x.FixingCurve.Value = market.GetData<CurveData>(valuationParameter.FixingCurveName).YieldCurve,
				x => x.HistoricalIndexRates.Value = market.HistoricalIndexRates
				);
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
