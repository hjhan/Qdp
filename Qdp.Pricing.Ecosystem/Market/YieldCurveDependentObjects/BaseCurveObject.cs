using System;
using System.Reflection;
using log4net;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.MktCalibrationInstruments;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.DependencyTree;
using Qdp.Pricing.Ecosystem.Trade.FixedIncome;
using Qdp.Pricing.Library.Commodity;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Cds;
using Qdp.Pricing.Library.Common.Fx;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Common.Utilities.Coupons;

namespace Qdp.Pricing.Ecosystem.Market.YieldCurveDependentObjects
{
	public abstract class BaseCurveObject<T> : DependentObject
		where T : InstrumentCurveDefinition
	{
		protected static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		public T Definition { get; set; }
		public IQdpMarket Market { get; set; }

		protected ICalibrationSupportedInstrument CreateIrsInstrument(RateMktData rateMktData,
			out MktInstrumentCalibMethod calibMethod)
		{
			MktIrsJson irsJson = null;
			ICalibrationSupportedInstrument irs = null;
			if (rateMktData.TradeInfo != null)
			{
				var irsInfo = (InterestRateSwapInfo) rateMktData.TradeInfo;
				var vf = new InterestRateSwapVf(irsInfo);
				irs = vf.GenerateInstrument();
				irsJson = MktInstrumentIrsRule.MktIrsRule[irsInfo.Index.ToIndexType()];
			}
			else
			{
				irsJson = MktInstrumentIrsRule.MktIrsRule[rateMktData.IndexType.ToIndexType()];
				var irsInfo = irsJson.InterestRateSwapInfo;
				var calendar = irsInfo.Calendar.ToCalendarImpl();

				var startDate = calendar.NextBizDay(Market.ReferenceDate);
				var isTernor = rateMktData.IsTerm();
				var tenor = isTernor ? rateMktData.Tenor : null;
				var maturityDate = isTernor ? new Term(tenor).Next(startDate) : new Date(DateTime.Parse(rateMktData.Tenor));

				var fixedLeg = new SwapLeg(startDate,
					maturityDate,
					-1.0,
					false,
					irsInfo.Currency.ToCurrencyCode(),
					new FixedCoupon(rateMktData.Rate),
					calendar,
					irsInfo.FixedLegFreq.ToFrequency(),
					irsInfo.FixedLegStub.ToStub(),
					irsInfo.FixedLegDC.ToDayCountImpl(),
					irsInfo.FixedLegBD.ToBda()
					);

				var floatingLegFrequency = irsInfo.FloatingLegFreq.ToFrequency();
				var floatingCouponResetTerm = new Term(irsInfo.ResetTerm);
				if (floatingCouponResetTerm.Equals(floatingLegFrequency.GetTerm()))
				{
					floatingCouponResetTerm = null;
				}
				var floatingCoupon =
					new FloatingCoupon(
						new Index(rateMktData.IndexType.ToIndexType(), 1, irsInfo.ResetCompound.ToCouponCompound()),
						calendar,
						irsInfo.FloatingLegDC.ToDayCountImpl(),
						0.0,
						floatingCouponResetTerm,
						irsInfo.ResetStub.ToStub(),
						irsInfo.ResetBD.ToBda(),
						new DayGap(irsInfo.ResetToFixingGap));
				var floatingLeg = new SwapLeg(startDate,
					maturityDate,
					1.0,
					false,
					irsInfo.Currency.ToCurrencyCode(),
					floatingCoupon,
					calendar,
					irsInfo.FloatingLegFreq.ToFrequency(),
					irsInfo.FloatingLegStub.ToStub(),
					irsInfo.FloatingLegDC.ToDayCountImpl(),
					irsInfo.FloatingLegBD.ToBda()
					);
				irs = new InterestRateSwap(fixedLeg, floatingLeg, SwapDirection.Payer, tenor);
			}

			calibMethod = irsJson.CalibrationMethod.ToCalibMethod();
			return irs;
		}

		protected ICalibrationSupportedInstrument CreateDepositInstrument(RateMktData rateMktData,
			out MktInstrumentCalibMethod calibMethod)
		{
			ICalibrationSupportedInstrument deposit = null;
			if (rateMktData.TradeInfo != null)
			{
				var depositInfo = (DepositInfo)rateMktData.TradeInfo;
				deposit = new Deposit(depositInfo.StartDate.ToDate(),
					depositInfo.MaturityDate.ToDate(),
					depositInfo.Coupon,
					depositInfo.DayCount.ToDayCount().Get(),
					depositInfo.Calendar.ToCalendarImpl(),
					depositInfo.BusinessDayConvention.ToBda(),
					depositInfo.Currency.ToCurrencyCode()
					);
			}
			else
			{
				var indexType = rateMktData.IndexType.ToIndexType();
				var depositInfo = MktInstrumentDepositRule.MktDepositRule[indexType].DepositInfo;

				var calendar = depositInfo.Calendar.ToCalendarImpl();
				var startDate = Market.ReferenceDate;
				var isTernor = rateMktData.IsTerm();
				var tenor = isTernor ? rateMktData.Tenor : null;
				var maturityDate = isTernor ? new Term(tenor).Next(startDate) : new Date(DateTime.Parse(rateMktData.Tenor));
				deposit = new Deposit(startDate,
					maturityDate,
					rateMktData.Rate,
					depositInfo.DayCount.ToDayCount().Get(),
					calendar,
					depositInfo.BusinessDayConvention.ToBda(),
					depositInfo.Currency.ToCurrencyCode(),
					1.0,
					tenor
					);
			}
			calibMethod = MktInstrumentCalibMethod.Default;
			return deposit;
		}

		protected ICalibrationSupportedInstrument CreateDummyInstrument(RateMktData rateMktData)
		{
			var startDate = Market.ReferenceDate;
			var isTernor = rateMktData.IsTerm();
			var tenor = isTernor ? rateMktData.Tenor : null;
			var maturityDate = isTernor ? new Term(tenor).Next(startDate) : new Date(DateTime.Parse(rateMktData.Tenor));
			return new DummyInstrument(startDate,
				maturityDate,
				rateMktData.Rate
				);
		}

		protected ICalibrationSupportedInstrument CreateCreditDefaultSwap(RateMktData rateMktData)
		{
			var indexType = rateMktData.IndexType.ToIndexType();
			var cdsJson = MktInstrumentCdsRule.MktCdsRule[indexType];
			var cdsInfo = cdsJson.CreditDefaultSwapInfo;

			var calendar = cdsInfo.Calendar.ToCalendarImpl();

			var startDate = Market.ReferenceDate;
			var isTernor = rateMktData.IsTerm();
			var tenor = isTernor ? rateMktData.Tenor : null;
			var maturityDate = isTernor ? new Term(tenor).Next(startDate) : new Date(DateTime.Parse(rateMktData.Tenor));
			var premiumLeg = new SwapLeg(startDate,
				maturityDate,
				1.0,
				false,
				cdsInfo.Currency.ToCurrencyCode(),
				new FixedCoupon(rateMktData.Rate),
				calendar,
				cdsInfo.Frequency.ToFrequency(),
				cdsInfo.Stub.ToStub(),
				cdsInfo.DayCount.ToDayCountImpl(),
				cdsInfo.BusinessDayConvention.ToBda()
				);
			var protectionLeg = new CdsProtectionLeg(startDate, maturityDate, null, cdsInfo.Currency.ToCurrencyCode(), 1.0, cdsInfo.RecoveryRate);

			return new CreditDefaultSwap(premiumLeg, protectionLeg, SwapDirection.Payer, tenor, cdsJson.CreditDefaultSwapInfo.NumIntegrationInterval);
		}

		protected ICalibrationSupportedInstrument CreateCommodityForward(RateMktData rateMktData)
		{
			if (rateMktData.IsTerm())
			{
				return new CommodityForward(Market.ReferenceDate,
					new Term(rateMktData.Tenor),
					1.0,
					rateMktData.Rate,
					null,
					CurrencyCode.CNY
					);
			}
			else
			{
				return new CommodityForward(Market.ReferenceDate,
					new Date(DateTime.Parse(rateMktData.Tenor)),
					1.0,
					rateMktData.Rate,
					null,
					CurrencyCode.CNY
					);
			}
		}

		protected FxSpot CreateFxSpot(RateMktData rateMktData)
		{
			var definition = (Definition as InstrumentCurveDefinition);

			var fgnCcy = definition.BaseCurveDefinition.CurveConvention.Currency.ToCurrencyCode();
			var domCcy = definition.CurveConvention.Currency.ToCurrencyCode();

			if (domCcy == fgnCcy)
			{
				return null;
			}

			var fgnCalendar = CalendarImpl.Get(definition.BaseCurveDefinition.CurveConvention.Calendar);
			var domCalendar = CalendarImpl.Get(definition.CurveConvention.Calendar);
			var spotDate = MarketExtensions.GetFxSpotDate(
				Market.ReferenceDate,
				new DayGap(rateMktData.Tenor),
				fgnCcy,
				domCcy,
				fgnCalendar,
				domCalendar
				);
			return new FxSpot(
				Market.ReferenceDate,
				spotDate,	
				rateMktData.Rate,
				domCalendar,
				domCcy,
				1.0,
				fgnCalendar,
				fgnCcy,
				rateMktData.Rate,
				domCcy
				);
		}
	}
}
