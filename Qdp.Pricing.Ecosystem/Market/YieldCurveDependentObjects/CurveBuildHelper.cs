using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.MktCalibrationInstruments;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Trade.FixedIncome;
using Qdp.Pricing.Library.Commodity;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Cds;
using Qdp.Pricing.Library.Common.Fx;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Common.Utilities.Coupons;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Base.Interfaces;

namespace Qdp.Pricing.Ecosystem.Market.YieldCurveDependentObjects
{
    public class CurveBuildHelper
    {
        /// <summary>
        /// for Option version only
        /// </summary>
        /// <param name="prebuiltMarket"></param>
        /// <param name="curveDate"></param>
        /// <param name="curveDefinition"></param>
        /// <returns></returns>
        public static YieldCurve BuildYieldCurve(PrebuiltQdpMarket prebuiltMarket, Date curveDate, InstrumentCurveDefinition curveDefinition)
        {
            IMarketCondition baseMarket;

            baseMarket = new MarketCondition(x => x.ValuationDate.Value = prebuiltMarket.ReferenceDate,
                    x => x.HistoricalIndexRates.Value = prebuiltMarket.HistoricalIndexRates);

            YieldCurve instrumentCurve = null;
            if (curveDefinition.RateDefinitions.All(
                    x => x.InstrumentType.ToInstrumentType() == InstrumentType.Dummy || x.InstrumentType.ToInstrumentType() == InstrumentType.None))
            {
                if (curveDefinition.RateDefinitions.All(x => x.IsTerm()))
                {
                    instrumentCurve = new YieldCurve(
                    curveDefinition.Name,
                    curveDate,
                    curveDefinition.RateDefinitions.Select(x => Tuple.Create((ITerm)new Term(x.Tenor), x.Rate)).ToArray(),
                    curveDefinition.CurveConvention.BusinessDayConvention.ToBda(),
                    curveDefinition.CurveConvention.DayCount.ToDayCountImpl(),
                    curveDefinition.CurveConvention.Calendar.ToCalendarImpl(),
                    curveDefinition.CurveConvention.Currency.ToCurrencyCode(),
                    curveDefinition.CurveConvention.Compound.ToCompound(),
                    curveDefinition.CurveConvention.Interpolation.ToInterpolation(),
                    curveDefinition.Trait.ToYieldCurveTrait(),
                    null,
                    null,
                    null,
                    null,
                    curveDefinition
                    );
                }
                else
                {
                    instrumentCurve = new YieldCurve(
                    curveDefinition.Name,
                    curveDate,
                    curveDefinition.RateDefinitions.Select(x => Tuple.Create(new Date(DateTime.Parse(x.Tenor)), x.Rate)).ToArray(),
                    curveDefinition.CurveConvention.BusinessDayConvention.ToBda(),
                    curveDefinition.CurveConvention.DayCount.ToDayCountImpl(),
                    curveDefinition.CurveConvention.Calendar.ToCalendarImpl(),
                    curveDefinition.CurveConvention.Currency.ToCurrencyCode(),
                    curveDefinition.CurveConvention.Compound.ToCompound(),
                    curveDefinition.CurveConvention.Interpolation.ToInterpolation(),
                    curveDefinition.Trait.ToYieldCurveTrait(),
                    null,
                    null,
                    null,
                    null,
                    curveDefinition
                    );
                }
            }
            else
            {
                var mktInstruments = new List<MarketInstrument>();
                foreach (var rateDefinition in curveDefinition.RateDefinitions)
                {
                    MktInstrumentCalibMethod calibrationMethod;
                    if (rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.InterestRateSwap)
                    {
                        var swap = CurveBuildHelper.CreateIrsInstrument(curveDate, rateDefinition, out calibrationMethod);
                        mktInstruments.Add(new MarketInstrument(swap, rateDefinition.Rate, calibrationMethod));
                    }
                    else if (rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.Deposit
                        || rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.Repo
                        || rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.Ibor)
                    {
                        var deposit = CurveBuildHelper.CreateDepositInstrument(curveDate, rateDefinition, out calibrationMethod);
                        mktInstruments.Add(new MarketInstrument(deposit, rateDefinition.Rate, calibrationMethod));
                    }
                    else if (rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.Dummy ||
                                     rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.None)
                    {
                        var dummy = CurveBuildHelper.CreateDummyInstrument(curveDate, rateDefinition);
                        mktInstruments.Add(new MarketInstrument(dummy, rateDefinition.Rate, MktInstrumentCalibMethod.Default));
                    }
                    else if (rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.CreditDefaultSwap)
                    {
                        var cds = CurveBuildHelper.CreateCreditDefaultSwap(curveDate, rateDefinition);
                        mktInstruments.Add(new MarketInstrument(cds, rateDefinition.Rate, MktInstrumentCalibMethod.Default));
                    }
                    else if (rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.CommodityForward)
                    {
                        mktInstruments.Add(new MarketInstrument(CurveBuildHelper.CreateCommodityForward(curveDate, rateDefinition), rateDefinition.Rate, MktInstrumentCalibMethod.Default));
                    }
                    else if (rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.CommoditySpot)
                    {
                        //baseMarket = baseMarket.UpdateCondition(new UpdateMktConditionPack<double>(x => x.SpotPrice, rateDefinition.Rate));
                    }
                    else if (rateDefinition.InstrumentType.ToInstrumentType() == InstrumentType.FxSpot)
                    {

                    }
                    else
                    {
                        throw new PricingLibraryException("Unrecognized product type in calibrating curve.");
                    }
                }

                var isSpcCurve = curveDefinition.RateDefinitions.All(x => x.IndexType != null && x.IndexType.ToIndexType() == IndexType.Spc);
                var isConvenicenYield = curveDefinition.RateDefinitions.All(x => x.IndexType != null && x.IndexType.ToIndexType() == IndexType.ConvenienceYield);


                //Expression<Func<IMarketCondition, object>>[] expression = null;
                //if (isSpcCurve) expression = new Expression<Func<IMarketCondition, object>>[] { x => x.SurvivalProbabilityCurve };
                //if (isConvenicenYield) expression = new Expression<Func<IMarketCondition, object>>[] { x => x.DividendCurve };

                instrumentCurve = new YieldCurve(
                    curveDefinition.Name,
                    curveDate,
                    mktInstruments.ToArray(),
                    curveDefinition.CurveConvention.BusinessDayConvention.ToBda(),
                    curveDefinition.CurveConvention.DayCount.ToDayCountImpl(),
                    curveDefinition.CurveConvention.Calendar.ToCalendarImpl(),
                    curveDefinition.CurveConvention.Currency.ToCurrencyCode(),
                    curveDefinition.CurveConvention.Compound.ToCompound(),
                    curveDefinition.CurveConvention.Interpolation.ToInterpolation(),
                    curveDefinition.Trait.ToYieldCurveTrait(),
                    baseMarket,
                    null,
                    null,
                    null,
                    null,
                    curveDefinition);
            }
            return instrumentCurve;
        }

        public static ICalibrationSupportedInstrument CreateIrsInstrument(
            Date curveDate,
            RateMktData rateMktData,
            out MktInstrumentCalibMethod calibMethod)
        {
            MktIrsJson irsJson = null;
            ICalibrationSupportedInstrument irs = null;
            if (rateMktData.TradeInfo != null)
            {
                var irsInfo = (InterestRateSwapInfo)rateMktData.TradeInfo;
                var vf = new InterestRateSwapVf(irsInfo);
                irs = vf.GenerateInstrument();
                irsJson = MktInstrumentIrsRule.MktIrsRule[irsInfo.Index.ToIndexType()];
            }
            else
            {
                irsJson = MktInstrumentIrsRule.MktIrsRule[rateMktData.IndexType.ToIndexType()];
                var irsInfo = irsJson.InterestRateSwapInfo;
                var calendar = irsInfo.Calendar.ToCalendarImpl();

                var startDate = calendar.NextBizDay(curveDate);
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

        public static ICalibrationSupportedInstrument CreateDepositInstrument(
            Date curveDate,
            RateMktData rateMktData,
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
                var startDate = curveDate;
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

        public static ICalibrationSupportedInstrument CreateDummyInstrument(Date curveDate, RateMktData rateMktData)
        {
            var startDate = curveDate;
            var isTernor = rateMktData.IsTerm();
            var tenor = isTernor ? rateMktData.Tenor : null;
            var maturityDate = isTernor ? new Term(tenor).Next(startDate) : new Date(DateTime.Parse(rateMktData.Tenor));
            return new DummyInstrument(startDate,
                maturityDate,
                rateMktData.Rate
                );
        }

        public static ICalibrationSupportedInstrument CreateCreditDefaultSwap(Date curveDate, RateMktData rateMktData)
        {
            var indexType = rateMktData.IndexType.ToIndexType();
            var cdsJson = MktInstrumentCdsRule.MktCdsRule[indexType];
            var cdsInfo = cdsJson.CreditDefaultSwapInfo;

            var calendar = cdsInfo.Calendar.ToCalendarImpl();

            var startDate = curveDate;
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

        public static ICalibrationSupportedInstrument CreateCommodityForward(Date curveDate, RateMktData rateMktData)
        {
            if (rateMktData.IsTerm())
            {
                return new CommodityForward(curveDate,
                    new Term(rateMktData.Tenor),
                    1.0,
                    rateMktData.Rate,
                    null,
                    CurrencyCode.CNY
                    );
            }
            else
            {
                return new CommodityForward(curveDate,
                    new Date(DateTime.Parse(rateMktData.Tenor)),
                    1.0,
                    rateMktData.Rate,
                    null,
                    CurrencyCode.CNY
                    );
            }
        }

        public static FxSpot CreateFxSpot(Date curveDate, RateMktData rateMktData, InstrumentCurveDefinition definition)
        {
            //var definition = (Definition as InstrumentCurveDefinition);

            var fgnCcy = definition.BaseCurveDefinition.CurveConvention.Currency.ToCurrencyCode();
            var domCcy = definition.CurveConvention.Currency.ToCurrencyCode();

            if (domCcy == fgnCcy)
            {
                return null;
            }

            var fgnCalendar = CalendarImpl.Get(definition.BaseCurveDefinition.CurveConvention.Calendar);
            var domCalendar = CalendarImpl.Get(definition.CurveConvention.Calendar);
            var spotDate = MarketExtensions.GetFxSpotDate(
                curveDate,
                new DayGap(rateMktData.Tenor),
                fgnCcy,
                domCcy,
                fgnCalendar,
                domCalendar
                );
            return new FxSpot(
                curveDate,
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
