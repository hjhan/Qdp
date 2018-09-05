using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Ecosystem.Utilities;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Common.Utilities.Coupons;

namespace Qdp.Pricing.Ecosystem.Trade.FixedIncome
{
	public class BasisSwapVf : ValuationFunction<BasisSwapInfo, InterestRateSwap>
	{
		public BasisSwapVf(BasisSwapInfo tradeInfo)
			: base(tradeInfo)
		{
		}

		//leg1 <=> fixedLeg in InterestRateSwap
		//leg2 <=> floatingLeg in InterestRateSwap
		public override InterestRateSwap GenerateInstrument()
		{
			var startDate = TradeInfo.StartDate.ToDate();
			var maturityDate = TradeInfo.MaturityDate.ToDate();
			var tenor = new Term(maturityDate - startDate, Period.Day);

			var leg1Frequency = TradeInfo.Leg1Frequency.ToFrequency();
			var leg1Calendar = TradeInfo.Leg1Calendar.ToCalendarImpl();
			var leg1CouponResetTerm = new Term(TradeInfo.Leg2ResetTerm);
			if (leg1CouponResetTerm.Equals(leg1Frequency.GetTerm()))
			{
				leg1CouponResetTerm = null;
			}
			var leg1Coupon =
				new FloatingCoupon(
					new Index(TradeInfo.Leg1Index.ToIndexType(), 1, TradeInfo.Leg1ResetCompound.ToCouponCompound()),
					leg1Calendar,
					TradeInfo.Leg1DayCount.ToDayCountImpl(),
					0.0,
					leg1CouponResetTerm,
					TradeInfo.Leg1ResetStub.ToStub(),
					TradeInfo.Leg1ResetTerm.ToBda(),
					new DayGap(TradeInfo.Leg1ResetToFixingGap));
			var leg1 = new SwapLeg(startDate,
				maturityDate,
				1.0,
				false,
				TradeInfo.Currency.ToCurrencyCode(),
				leg1Coupon,
				leg1Calendar,
				TradeInfo.Leg1Frequency.ToFrequency(),
				TradeInfo.Leg1Stub.ToStub(),
				TradeInfo.Leg1DayCount.ToDayCountImpl(),
				TradeInfo.Leg1BusinessDayConvention.ToBda()
				);


			var leg2Calendar = TradeInfo.Leg2Calendar.ToCalendarImpl();
			var leg2Frequency = TradeInfo.Leg2Frequency.ToFrequency();
			var leg2CouponResetTerm = new Term(TradeInfo.Leg2ResetTerm);
			if (leg2CouponResetTerm.Equals(leg2Frequency.GetTerm()))
			{
				leg2CouponResetTerm = null;
			}

			var leg2Coupon =
				new FloatingCoupon(
					new Index(TradeInfo.Leg2Index.ToIndexType(), 1, TradeInfo.Leg2ResetCompound.ToCouponCompound()),
					leg2Calendar,
					TradeInfo.Leg2DayCount.ToDayCountImpl(),
					0.0,
					leg2CouponResetTerm,
					TradeInfo.Leg2ResetStub.ToStub(),
					TradeInfo.Leg2ResetTerm.ToBda(),
					new DayGap(TradeInfo.Leg2ResetToFixingGap));
			var leg2 = new SwapLeg(startDate,
				maturityDate,
				1.0,
				false,
				TradeInfo.Currency.ToCurrencyCode(),
				leg2Coupon,
				leg2Calendar,
				TradeInfo.Leg2Frequency.ToFrequency(),
				TradeInfo.Leg2Stub.ToStub(),
				TradeInfo.Leg2DayCount.ToDayCountImpl(),
				TradeInfo.Leg2BusinessDayConvention.ToBda()
				);

			return new InterestRateSwap(leg1, leg2, TradeInfo.SwapDirection.ToSwapDirection(), tenor.ToString());
		}

		public override IEngine<InterestRateSwap> GenerateEngine()
		{
			return new CashflowProductEngine<InterestRateSwap>();
		}

		public override IMarketCondition GenerateMarketCondition(QdpMarket market)
		{
			var valuationParameter = TradeInfo.ValuationParamters;

			return new MarketCondition(
				x => x.ValuationDate.Value = market.ReferenceDate,
				x => x.DiscountCurve.Value = market.GetData<CurveData>(valuationParameter.Leg1DiscountCurveName).YieldCurve,
				x => x.FixingCurve.Value = market.GetData<CurveData>(valuationParameter.Leg1FixingCurveName).YieldCurve,
				x => x.FgnDiscountCurve.Value = market.GetData<CurveData>(valuationParameter.Leg2DiscountCurveName).YieldCurve,
				x => x.FgnFixingCurve.Value = market.GetData<CurveData>(valuationParameter.Leg2FixingCurveName).YieldCurve,
				x => x.HistoricalIndexRates.Value = market.HistoricalIndexRates
				);
		}
	}
}
