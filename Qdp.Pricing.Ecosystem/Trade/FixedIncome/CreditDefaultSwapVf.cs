using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Ecosystem.Utilities;
using Qdp.Pricing.Library.Common.Cds;
using Qdp.Pricing.Library.Common.Engines.Cds;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Common.Utilities.Coupons;

namespace Qdp.Pricing.Ecosystem.Trade.FixedIncome
{
	public class CreditDefaultSwapVf : ValuationFunction<CreditDefaultSwapInfo, CreditDefaultSwap>
	{
		public CreditDefaultSwapVf(CreditDefaultSwapInfo tradeInfo)
			: base(tradeInfo)
		{
		}

		public override CreditDefaultSwap GenerateInstrument()
		{
			var calendar = TradeInfo.Calendar.ToCalendarImpl();
			var startDate = TradeInfo.StartDate.ToDate();
			var maturityDate = TradeInfo.MaturityDate.ToDate();

			var premiumLegNotiaonalFactor = TradeInfo.SwapDirection.ToSwapDirection() == SwapDirection.Payer ? -1 : 1;
			var premiumLeg = new SwapLeg(startDate,
				maturityDate,
				premiumLegNotiaonalFactor*TradeInfo.Notional,
				false,
				TradeInfo.Currency.ToCurrencyCode(),
				new FixedCoupon(TradeInfo.Coupon),
				calendar,
				TradeInfo.Frequency.ToFrequency(),
				TradeInfo.Stub.ToStub(),
				TradeInfo.DayCount.ToDayCountImpl(),
				TradeInfo.BusinessDayConvention.ToBda());
			var protectionLeg = new CdsProtectionLeg(
				startDate,
				maturityDate,
				null,
				TradeInfo.Currency.ToCurrencyCode(),
				-premiumLegNotiaonalFactor * TradeInfo.Notional,
				TradeInfo.RecoveryRate);

			return new CreditDefaultSwap(premiumLeg, protectionLeg, TradeInfo.SwapDirection.ToSwapDirection());
		}

		public override IEngine<CreditDefaultSwap> GenerateEngine()
		{
			return new CdsEngine(TradeInfo.NumIntegrationInterval);
		}

		public override IMarketCondition GenerateMarketCondition(QdpMarket market)
		{
			var valuationParameter = TradeInfo.ValuationParameters;

			return new MarketCondition(
				x => x.ValuationDate.Value = market.ReferenceDate,
				x => x.DiscountCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
				x => x.FixingCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
				x => x.SurvivalProbabilityCurve.Value = market.GetData<CurveData>(valuationParameter.SpcCurveName).YieldCurve,
				x => x.HistoricalIndexRates.Value = market.HistoricalIndexRates
				);
		}
	}
}
