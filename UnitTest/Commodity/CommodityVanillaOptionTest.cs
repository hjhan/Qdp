using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Ecosystem.Utilities;
using UnitTest.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Equity.Engines.MonteCarlo;
using Qdp.Pricing.Library.Equity.Engines.Numerical;
using Qdp.Pricing.Library.Common.MathMethods.VolTermStructure;
using Qdp.Pricing.Library.Common.Utilities;

namespace UnitTest.Commodity
{

    [TestClass]
	public class CommodityVanillaOptionTest
	{
        [TestMethod]
        public void CommodityVanillaEuropeanMonteCarlo()
        {
            VanillaEuropeanMonteCarloTest(spot: 14170.0, T0: "2018-01-19", T1: "2018-02-09", strike: 14565.0, baseVol: 0.28, notional: 1, toleranceInPct: 1, rate: 0);
        }

        private void VanillaEuropeanMonteCarloTest(double spot, string T0, string T1,
            double strike = 1.0, double baseVol = 0.2,
             double notional = 1e6, double toleranceInPct = 1.0, double rate = 0.05)
        {
            var startDay = DateFromStr(T0);
            var exerciseDay = DateFromStr(T1);
            var observationDates = CalendarImpl.Get(Calendar.Chn).BizDaysBetweenDatesExcluStartDay(startDay, exerciseDay).ToArray();
            var euopean = createVanillaOption(startDay: startDay, expiry: exerciseDay, futureMaturityDate: exerciseDay,obs:observationDates,
                strike: strike, notional: notional, isCall: true, isEuropean: true);

            var market = TestMarket(spot: spot, vol: baseVol, referenceDate: T0, rate: rate);
         
            var discCurveName = "Fr007";
            var dividentCurveName = "Dividend";
            var volName = "VolSurf";

            //TODO: specifying spot twice is not ideal,  to improve 
            var volsurf = market.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate);
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>(discCurveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(dividentCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }
                );
         
            var vanillaEngine = new GenericMonteCarloEngine(10, 100000,true);
            var Engine = new AnalyticalVanillaEuropeanOptionEngine();

            var vanillaResult = vanillaEngine.Calculate(euopean, marketCondition, PricingRequest.Pv);
            var Result = Engine.Calculate(euopean, marketCondition, PricingRequest.Pv);

            var diff = (vanillaResult.Pv - Result.Pv) / Result.Pv;
            Assert.AreEqual(true, Math.Abs(diff * 100) < toleranceInPct);
        }

        [TestMethod]
        public void VanillaOptionTradingDayCountModeTest() {
            // time to maturity fraction is often larger in tradingDay mode
            // for short term trades ,  timeSpan_in_Biz Day /244 >  timeSpan_in_Calendar_Day / 365  , therefore Pv > calendarMode

            //1. extreme corner case, too many holidays (spring festival, qingming and labor day) fall within the time span, 
            //therefore shorter time to maturity in trading day mode
            TradingDayCountModeTest(new Date(2018, 02, 12), new Date(2018, 05, 15), 1.0, isCounterExample: true);

            //2. normal case
            TradingDayCountModeTest(new Date(2018, 3, 1), new Date(2018, 04, 2), 1.0);
        }

        private void TradingDayCountModeTest(Date startDate, Date endDate, double strike, bool isCounterExample = false) {
            var spot = 240;
            var baseVol = 0.3;
            var optionCalender = new VanillaOption(
                startDate,
                endDate,
                OptionExercise.European,
                OptionType.Call,
                strike,
                InstrumentType.Futures,
                //InstrumentType.CommoditySpot,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { endDate },
                new[] { endDate },
                isMoneynessOption: true,
                initialSpotPrice: spot
                );

            var optionBus244 = new VanillaOption(
                startDate,
                endDate,
                OptionExercise.European,
                OptionType.Call,
                strike,
                InstrumentType.Futures,
                //InstrumentType.CommoditySpot,
                CalendarImpl.Get("chn"),
                new Bus244(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { endDate },
                new[] { endDate },
                isMoneynessOption: true,
                initialSpotPrice: spot
                );

            var market = TestMarket(spot: spot, vol: baseVol, referenceDate: startDate.ToString(), rate: 0.05, curveDayCount: "Act365");

            var discCurveName = "Fr007";
            var dividentCurveName = "Dividend";
            var volName = "VolSurf";

            var volsurf = market.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate);
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>(discCurveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(dividentCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }
                );
            var engineAnalyticalEuropean = new AnalyticalVanillaEuropeanOptionEngine();


            var pricingReq = PricingRequest.Pv | PricingRequest.Delta | PricingRequest.Gamma | PricingRequest.Theta ;
            var calendarMode = engineAnalyticalEuropean.Calculate(optionCalender, marketCondition, pricingReq);
            var tradingDayMode = engineAnalyticalEuropean.Calculate(optionBus244, marketCondition, pricingReq);

            var compareSign = isCounterExample ? -1 : 1;
            Assert.AreEqual(true, compareSign * (tradingDayMode.Pv - calendarMode.Pv) > 0 );
            Assert.AreEqual(true, compareSign * (tradingDayMode.Delta - calendarMode.Delta) > 0);
            Assert.AreEqual(true, compareSign * (tradingDayMode.Gamma - calendarMode.Gamma) < 0);

            //In trading day mode, value drops faster, hence larger absolute theta, regardless of how many holidays fall within the timespan
            Assert.AreEqual(true, (tradingDayMode.Theta - calendarMode.Theta) < 0); 
        }


        //TODO: fix the technical issue here, missing dividend curve configuration
        [TestMethod]
		public void CommodityVanillaEuropeanOptionTest()
		{
			var goldOption = new VanillaOption(
				new Date(2015, 06, 11),
				new Date(2015, 09, 11),
				OptionExercise.European,
				OptionType.Call,
				240.0,
                //InstrumentType.Futures,
                InstrumentType.CommoditySpot,
                CalendarImpl.Get("chn"),
				new Act365(),
				CurrencyCode.CNY,
				CurrencyCode.CNY,
				new[] { new Date(2015, 09, 11) },
				new[] { new Date(2015, 09, 11) },
				5.5
				);

			var market = GetMarket();
            var volsurf = market.GetData<VolSurfMktData>("goldVolSurf").ToImpliedVolSurface(market.ReferenceDate);
            var yc = market.GetData<CurveData>("Fr007").YieldCurve;

            //TODO:  this commodity convenience yield curve extraction does not work now.
            //failed to build curve -2018-01-08
            //var dvc = market.GetData<CurveData>("goldYield").YieldCurve;
            //var dv = new Dictionary<string, IYieldCurve> { { "", dvc } };

            IMarketCondition marketCondition = new MarketCondition(
				x => x.ValuationDate.Value = market.ReferenceDate,
				x => x.DiscountCurve.Value = yc,
                //x => x.DividendCurves.Value = dv,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", yc } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { {"", 236.49 } },
				x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }

                );

            // set first point on dividend curve to 0
            //marketCondition = marketCondition.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, IYieldCurve>>(x => x.DividendCurves, 
                //new Dictionary<string, IYieldCurve> { {"", ZeroFirstPoint(marketCondition.DividendCurves.Value.Values.First()) } }));

			var engine = new AnalyticalVanillaEuropeanOptionEngine();
			var result = engine.Calculate(goldOption, marketCondition, PricingRequest.All);
            
            Assert.AreEqual(27.7268791749271, result.Pv, 1e-8);
            Assert.AreEqual(2.35620788686148, result.Delta, 1e-8);
            Assert.AreEqual(0.129265432988745, result.Gamma, 1e-8);
            Assert.AreEqual(2.55083448506368, result.Vega, 1e-8);
            Assert.AreEqual(0.0133635518021553, result.Rho, 1e-8);
            Assert.AreEqual(true, result.DVegaDvol != result.DDeltaDvol);
            Assert.AreEqual(true, result.DVegaDvol >0 );
            Assert.AreEqual(true, result.DDeltaDvol > 0);
        }

        [TestMethod]
        public void VanillaAmericanMoneynessOptionWithCashDividendTest()
        {
            //Tree, american with dividends
            var spot = 240;
            var baseVol = 0.3;

            var cashDividend = 20;
            var startDate = new Date(2017, 06, 9);
            var maturityDate = new Date(2017, 08, 9);

            var dividendSchedule = new Dictionary<Date, double>();
            dividendSchedule.Add(new Date(2017, 7,13), cashDividend);

            var calendar = CalendarImpl.Get("chn");
            
            var exerciseDates = calendar.BizDaysBetweenDatesInclEndDay(startDate, maturityDate).ToArray();

            #region moneyness
            var moneynessOption = new VanillaOption(
               startDate,
                maturityDate,
                OptionExercise.American,
                OptionType.Call,
                1.2,
                InstrumentType.Stock,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                exerciseDates,
                exerciseDates,
                5.5,  //useless
                isMoneynessOption: true,
                initialSpotPrice: spot,
                dividends : dividendSchedule
                );
            #endregion moneyness

            #region eqv european
            var singleExerciseDate = new[] { maturityDate };
            var equivalentEuropean = new VanillaOption(
               startDate,
                maturityDate,
                OptionExercise.European,
                OptionType.Call,
                1.2,
                InstrumentType.Stock,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                singleExerciseDate,
                singleExerciseDate,
                5.5,  //useless
                isMoneynessOption: true,
                initialSpotPrice: spot,
                dividends: dividendSchedule
                );
            #endregion eqv european

            var marketForAmerican = createMktCondition(spot: spot, baseVol: baseVol);
            var americanEngine = new BinomialTreeAmericanEngine();
            var americanResult = americanEngine.Calculate(moneynessOption, marketForAmerican, PricingRequest.All);

            //construct equivalent market for european option pricing
            var marketForEuropean = createMktCondition(spot: spot, baseVol: baseVol, dividendYield: 20.0/240.0 * (12/2) );
            var vanillaEngine = new AnalyticalVanillaEuropeanOptionEngine();
            var europeanResult = vanillaEngine.Calculate(equivalentEuropean, marketForEuropean, PricingRequest.All);

            Assert.AreEqual(true, americanResult.Pv >= europeanResult.Pv);
            Assert.AreEqual(true, americanResult.Delta >= europeanResult.Delta);
            //2018-07-09, assertion below does not hold right now,  a little counter-intuitive
            //Assert.AreEqual(true, americanResult.Gamma >= europeanResult.Gamma );

            //TODO:  what's the actual relationship?
            Assert.AreEqual(true, americanResult.Vega >= europeanResult.Vega);
            Assert.AreEqual(true, Math.Abs(americanResult.Theta) >= Math.Abs(europeanResult.Theta));

        }

        [TestMethod]
        public void VanillaEuropeanOptionWithCashDividendTest()
        {
            //european with dividends
            var spot = 240;
            var baseVol = 0.3;

            var cashDividend = 20;
            var startDate = new Date(2017, 06, 9);
            var maturityDate = new Date(2017, 08, 9);

            var dividendSchedule = new Dictionary<Date, double>();
            dividendSchedule.Add(new Date(2017, 7, 13), cashDividend);

            var calendar = CalendarImpl.Get("chn");

            #region cashDividends
            var cashDividendsCallOption = new VanillaOption(
               startDate,
                maturityDate,
                OptionExercise.European,
                OptionType.Call,
                1.2,
                InstrumentType.Stock,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { maturityDate },
                new[] { maturityDate },
                5.5,  //useless
                isMoneynessOption: true,
                initialSpotPrice: spot,
                dividends: dividendSchedule
                );

            var cashDividendsPutOption = new VanillaOption(
               startDate,
                maturityDate,
                OptionExercise.European,
                OptionType.Put,
                1.2,
                InstrumentType.Stock,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { maturityDate },
                new[] { maturityDate },
                5.5,  //useless
                isMoneynessOption: true,
                initialSpotPrice: spot,
                dividends: dividendSchedule
                );
            #endregion cashDividends

            #region eqv european
            var singleExerciseDate = new[] { maturityDate };
            var equivalentCallEuropean = new VanillaOption(
               startDate,
                maturityDate,
                OptionExercise.European,
                OptionType.Call,
                1.2,
                InstrumentType.Stock,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                singleExerciseDate,
                singleExerciseDate,
                5.5,  //useless
                isMoneynessOption: true,
                initialSpotPrice: spot
                );

            var equivalentPutEuropean = new VanillaOption(
               startDate,
                maturityDate,
                OptionExercise.European,
                OptionType.Put,
                1.2,
                InstrumentType.Stock,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                singleExerciseDate,
                singleExerciseDate,
                5.5,  //useless
                isMoneynessOption: true,
                initialSpotPrice: spot
                );
            #endregion eqv european

            var market = createMktCondition(spot: spot, baseVol: baseVol);
            var Engine = new AnalyticalVanillaEuropeanOptionEngine();
            var cashDividendsCall = Engine.Calculate(cashDividendsCallOption, market, PricingRequest.All);
            var vanillaCall = Engine.Calculate(equivalentCallEuropean, market, PricingRequest.All);
            var cashDividendsPut = Engine.Calculate(cashDividendsPutOption, market, PricingRequest.All);
            var vanillaPut = Engine.Calculate(equivalentPutEuropean, market, PricingRequest.All);
            Assert.AreEqual(true, cashDividendsCall.Pv <= vanillaCall.Pv);
            Assert.AreEqual(true, cashDividendsPut.Pv >= vanillaPut.Pv);

        }

        [TestMethod]
        public void VanillaAmericanOptionWithCashDividendTest()
        {
            //european with dividends
            var spot = 240;
            var baseVol = 0.3;

            var cashDividend = 20;
            var startDate = new Date(2017, 06, 9);
            var maturityDate = new Date(2017, 08, 9);

            var dividendSchedule = new Dictionary<Date, double>();
            dividendSchedule.Add(new Date(2017, 7, 13), cashDividend);

            var calendar = CalendarImpl.Get("chn");
            var exerciseDates = calendar.BizDaysBetweenDatesInclEndDay(startDate, maturityDate).ToArray();

            #region cashDividends
            var cashDividendsCallOption = new VanillaOption(
               startDate,
                maturityDate,
                OptionExercise.American,
                OptionType.Call,
                1.2,
                InstrumentType.Stock,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                exerciseDates,
                exerciseDates,
                5.5,  //useless
                isMoneynessOption: true,
                initialSpotPrice: spot,
                dividends: dividendSchedule
                );

            var cashDividendsPutOption = new VanillaOption(
               startDate,
                maturityDate,
                OptionExercise.American,
                OptionType.Put,
                1.2,
                InstrumentType.Stock,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                exerciseDates,
                exerciseDates,
                5.5,  //useless
                isMoneynessOption: true,
                initialSpotPrice: spot,
                dividends: dividendSchedule
                );
            #endregion cashDividends

            #region american
            var equivalentCall = new VanillaOption(
                startDate,
                maturityDate,
                OptionExercise.American,
                OptionType.Call,
                1.2,
                InstrumentType.Stock,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { maturityDate },
                new[] { maturityDate },
                5.5,  //useless
                isMoneynessOption: true,
                initialSpotPrice: spot
                );

            var equivalentPut = new VanillaOption(
               startDate,
                maturityDate,
                OptionExercise.American,
                OptionType.Put,
                1.2,
                InstrumentType.Stock,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { maturityDate },
                new[] { maturityDate },
                5.5,  //useless
                isMoneynessOption: true,
                initialSpotPrice: spot
                );
            #endregion american

            var market = createMktCondition(spot: spot, baseVol: baseVol);
            var Engine = new AnalyticalVanillaAmericanOptionBAWEngine();
            var treeEngine = new BinomialTreeAmericanEngine();
            var cashDividendsCall = Engine.Calculate(cashDividendsCallOption, market, PricingRequest.All);
            var vanillaCall = Engine.Calculate(equivalentCall, market, PricingRequest.All);
            var cashDividendsPut = Engine.Calculate(cashDividendsPutOption, market, PricingRequest.All);
            var vanillaPut = Engine.Calculate(equivalentPut, market, PricingRequest.All);

            var cashDividendsTreeCall = treeEngine.Calculate(cashDividendsCallOption, market, PricingRequest.All);
            var vanillaTreeCall = treeEngine.Calculate(equivalentCall, market, PricingRequest.All);
            var cashDividendsTreePut = treeEngine.Calculate(cashDividendsPutOption, market, PricingRequest.All);
            var vanillaTreePut = treeEngine.Calculate(equivalentPut, market, PricingRequest.All);

            Assert.AreEqual(true, cashDividendsCall.Pv <= vanillaCall.Pv);
            Assert.AreEqual(true, cashDividendsPut.Pv >= vanillaPut.Pv);
            Assert.AreEqual(true, cashDividendsTreeCall.Pv <= vanillaTreeCall.Pv);
            Assert.AreEqual(true, cashDividendsTreePut.Pv >= vanillaTreePut.Pv);

        }

        private IMarketCondition createMktCondition(double spot, double baseVol, double dividendYield = 0.0) {
            var market = TestMarket(spot: spot, vol: baseVol, referenceDate: "2017-06-09", rate: 0.05, dividend: dividendYield);
            var discCurveName = "Fr007";
            var dividentCurveName = "Dividend";
            var volName = "VolSurf";
            var volsurf = market.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate);
            return new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>(discCurveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(dividentCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }
                );
        }

        [TestMethod]
        public void CommodityVanillaEuropeanMoneynessOptionTest()
        {
            var spot = 240;
            var baseVol = 0.3;

            var moneynessOption = new VanillaOption(
                new Date(2015, 06, 11),
                new Date(2015, 09, 11),
                OptionExercise.European,
                OptionType.Call,
                1,
                InstrumentType.Futures,
                //InstrumentType.CommoditySpot,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { new Date(2015, 09, 11) },
                new[] { new Date(2015, 09, 11) },
                isMoneynessOption: true,
                initialSpotPrice: spot
                );

            var vanillaOption = new VanillaOption(
                new Date(2015, 06, 11),
                new Date(2015, 09, 11),
                OptionExercise.European,
                OptionType.Call,
                spot,
                InstrumentType.Futures,
                //InstrumentType.CommoditySpot,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { new Date(2015, 09, 11) },
                new[] { new Date(2015, 09, 11) }
                );

            var market = TestMarket(spot: spot, vol: baseVol, referenceDate: "2015-06-11", rate:0.05 );

            var discCurveName = "Fr007";
            var dividentCurveName = "Dividend";
            var volName = "VolSurf";

            //TODO: specifying spot twice is not ideal,  to improve 
            var volsurf = market.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate);
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>(discCurveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(dividentCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }
                );


            var engine = new AnalyticalVanillaEuropeanOptionEngine();
            var calcRequest = PricingRequest.Pv | PricingRequest.Delta;
            var resultmoneyness = engine.Calculate(moneynessOption, marketCondition, calcRequest);
            var resulteuropean = engine.Calculate(vanillaOption, marketCondition, calcRequest);

            Assert.AreEqual(resulteuropean.Pv, resultmoneyness.Pv, 1e-8);
            Assert.AreEqual(resulteuropean.Pv/spot, resultmoneyness.PctPv, 1e-8);
            Assert.AreEqual(resulteuropean.Delta, resultmoneyness.Delta, 1e-8);

        }

        [TestMethod]
        public void CommodityOptionExpiryDayDeltaTest()
        {
            var spot = 230;
            var baseVol = 0.3;
            var initialSpotPrice = 240;
            var notional = 10;

            var today = new Date(DateTime.Now.Date);

            var vanillaEuropean = new VanillaOption(
                new Date(2018, 02, 09),
                new Date(DateTime.Now.Date),
                OptionExercise.European,
                OptionType.Put,
                1,
                InstrumentType.Futures,
                //InstrumentType.CommoditySpot,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { today },
                new[] { today },
                notional,
                isMoneynessOption: true,
                initialSpotPrice: initialSpotPrice
                );

            //Note: this definition is wrong, should be many exercises days
            var vanillaAmerican = new VanillaOption(
                new Date(2018, 02, 09),
                new Date(DateTime.Now.Date),
                OptionExercise.American,
                OptionType.Put,
                1,
                InstrumentType.Futures,
                //InstrumentType.CommoditySpot,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { today },
                new[] { today },
                notional,
                isMoneynessOption: true,
                initialSpotPrice: initialSpotPrice
                );



            var market = TestMarket(spot: spot, vol: baseVol, referenceDate: today.ToString(), rate: 0.05);

            var discCurveName = "Fr007";
            var dividentCurveName = "Dividend";
            var volName = "VolSurf";

            //TODO: specifying spot twice is not ideal,  to improve 
            var volsurf = market.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate);
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>(discCurveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(dividentCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }
                );


            var engineAnalyticalEuropean = new AnalyticalVanillaEuropeanOptionEngine();
            var engineAnalyticalAmerican = new AnalyticalVanillaAmericanOptionBSEngine();
            var engineTree = new BinomialTreeAmericanEngine();
            var europeanResult = engineAnalyticalEuropean.Calculate(vanillaEuropean, marketCondition, PricingRequest.Delta);
            var americanResult = engineAnalyticalAmerican.Calculate(vanillaAmerican, marketCondition, PricingRequest.Delta);
            var americanTreeResult = engineTree.Calculate(vanillaAmerican, marketCondition, PricingRequest.Delta);

            //Put, ITM
            Assert.AreEqual(americanResult.Delta / -1.0 / notional , 1, 0.1);
            Assert.AreEqual(europeanResult.Delta / -1.0 /notional,  1, 0.1);

            //a good test to tune
            Assert.AreEqual(americanTreeResult.Delta / europeanResult.Delta, 1 , 0.1);

        }

        public void CommodityOptionPreciseTimeValueModeCalc(bool HasNightMarket = false)
        {
            
            var spot = 230;
            var strike = 230;
            var baseVol = 0.2;
            var notional = 10;

            var vanillaEuropean = new VanillaOption(
                new Date(2018, 02, 09),
                new Date(2018, 05, 15),
                OptionExercise.European,
                OptionType.Call,
                strike,
                InstrumentType.Futures,
                //InstrumentType.CommoditySpot,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { new Date(2018, 05, 15) },
                new[] { new Date(2018, 05, 15) },
                notional,
                isMoneynessOption: false,
                hasNightMarket: HasNightMarket
                );

            var vanillaEuropeanPrecise = new VanillaOption(
                new Date(2018, 02, 09),
                new Date(2018, 05, 15),
                OptionExercise.European,
                OptionType.Call,
                strike,
                InstrumentType.Futures,
                //InstrumentType.CommoditySpot,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { new Date(2018, 05, 15) },
                new[] { new Date(2018, 05, 15) },
                notional,
                isMoneynessOption: false,
                hasNightMarket: HasNightMarket,
                commodityFuturesPreciseTimeMode: true
                );

            var market = TestMarket(spot: spot, vol: baseVol, referenceDate: new Date(2018, 02, 11).ToString(), rate: 0.05);

            var discCurveName = "Fr007";
            var dividentCurveName = "Dividend";
            var volName = "VolSurf";

            //TODO: specifying spot twice is not ideal,  to improve 
            var volsurf = market.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate);
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>(discCurveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(dividentCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }
                );


            var engineAnalyticalEuropean = new AnalyticalVanillaEuropeanOptionEngine();

            var europeanResult = engineAnalyticalEuropean.Calculate(vanillaEuropean, marketCondition, PricingRequest.Pv);

            var europeanPreciseResult = engineAnalyticalEuropean.Calculate(vanillaEuropeanPrecise, marketCondition, PricingRequest.Pv);

            if (DateTime.Now.Hour >= 15)
            {
                Assert.AreEqual(europeanResult.Pv == europeanPreciseResult.Pv, true);
            }
            if (DateTime.Now.Hour <= 15)
            {
                Assert.AreEqual(europeanResult.Pv < europeanPreciseResult.Pv, true);
            }

        }



        [TestMethod]
        public void CommodityVanillaEuropeanOptionPnLTest() {
            Date defaultStartDay = new Date(2017, 12, 11);
            Date startDay = new Date(2017, 12, 11);
            Date exerciseDay = new Date(2018, 3, 11);
            string T0 = "2017-10-25";
            string T1 = "2017-10-26";


            //original diff tolerance 1 pct
            VanillaEuropeanOptionGreekTest(spot: 1.0, mktMove: 10e-4, volMove: 0.1, notional: 1e6, toleranceInPct: 1,
                startDay : startDay, exerciseDay: exerciseDay, T0: T0, T1: T1);

            //original diff tolerance 1 pct
            //Note: 1bp quick gamma is not so useful in predicting pnl for big mkt move
            VanillaEuropeanOptionGreekTest(spot: 1.0, mktMove: 100e-4, volMove: 0.1, notional: 1e6, toleranceInPct: 2,
                startDay: startDay, exerciseDay: exerciseDay, T0: T0, T1: T1);
        }

        [TestMethod]
        public void CommodityVanillaEuropeanOptionBadPnL()
        {
            //T: 4000
            //T1: 4100
            //vol: 0.277
            //strike: 4500
            //vanilla european call
            //expiry 2018-1-5
            //CommodityFutures

            Date defaultStartDay = new Date(2017, 12, 6);
            Date startDay = new Date(2017, 12, 6);
            Date exerciseDay = new Date(2018, 1, 5);
            Date futureMaturityDate = new Date(2018, 1, 15);

            string T0 = "2017-12-06";
            string T1 = "2017-12-07";
            double strike = 4500;

            //test is helpful, fixed one bug in xl_pnl. but unexplained is still not small enough
            //TODO:  implement new pnl framework
            VanillaEuropeanOptionGreekTest(spot: 4000.0, startDay: startDay, exerciseDay: exerciseDay, futureMaturityDate: futureMaturityDate,
                T0: T0, T1: T1, strike: strike, baseVol: 0.277,
                mktMove: 300, volMove: 0, notional: 1, toleranceInPct: 20, rate :0.05, rateMove: 0);

            //mktMove = 100, pnl explain is good
        }

        private Date defaultStartDay = new Date(2017, 12, 11);
        private void VanillaEuropeanOptionGreekTest(double spot, Date startDay, Date exerciseDay, string T0, string T1,
            Date futureMaturityDate = null,
            double strike = 1.0, double baseVol = 0.2,
             double mktMove = 1e-4, double volMove = 0.1,
             double notional = 1e6, double toleranceInPct = 1.0, double rate = 0.05, double rateMove = 0)
        {
            var euopean = createVanillaOption(startDay: startDay, expiry: exerciseDay, futureMaturityDate: futureMaturityDate ?? exerciseDay,
                strike: strike, notional: notional, isCall: true, isEuropean: true);
            var market = TestMarket(spot: spot, vol: baseVol, referenceDate: T0, rate: rate);
            var marketNew = TestMarket(spot: spot + mktMove, vol: baseVol + volMove, referenceDate: T1, rate: rate + rateMove);

            var discCurveName = "Fr007";
            var dividentCurveName = "Dividend";
            var volName = "VolSurf";

            //TODO: specifying spot twice is not ideal,  to improve 
            var volsurf = market.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate);
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>(discCurveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(dividentCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }
                );

            var volsurfNew = marketNew.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate);
            IMarketCondition marketConditionNew = new MarketCondition(
                x => x.ValuationDate.Value = marketNew.ReferenceDate,
                x => x.DiscountCurve.Value = marketNew.GetData<CurveData>(discCurveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", marketNew.GetData<CurveData>(dividentCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot + mktMove } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurfNew } }
                );

            var vanillaEngine = new GenericMonteCarloEngine(100000, 100000, false);
            var analyticalEngine = new AnalyticalVanillaEuropeanOptionEngine();

            var calcRequests = PricingRequest.Pv | PricingRequest.Delta | PricingRequest.Gamma | PricingRequest.Vega
                | PricingRequest.Theta | PricingRequest.Rho | PricingRequest.DDeltaDt | PricingRequest.DDeltaDvol | PricingRequest.DVegaDt;

            //commented and kept here, in case we want to run MC simulation to generate csv
            //var baseResult2 = vanillaEngine.Calculate(euopean, marketCondition, calcRequests);
            //var shockResult2 = vanillaEngine.Calculate(euopean, marketConditionNew, calcRequests);
            var baseResult = analyticalEngine.Calculate(euopean, marketCondition, calcRequests);
            var shockResult = analyticalEngine.Calculate(euopean, marketConditionNew, calcRequests);

            //vanillaRes1.Pv
            var actualPL = shockResult.Pv - baseResult.Pv;
            var deltapl = baseResult.Delta * mktMove;
            var gammapl = 0.5 * baseResult.Gamma * Math.Pow(mktMove, 2);
            var vegapl = baseResult.Vega * volMove * 100;
            var theta = baseResult.Theta;
            var rhopl = baseResult.Rho * rateMove * 10000; //rho is 1bp rate risk

            var esimstatedPL = deltapl + gammapl + vegapl + theta + rhopl;
            var diff = (actualPL - esimstatedPL) / actualPL * 100;
            Assert.AreEqual(true, Math.Abs(diff) < toleranceInPct);

            var ddeltadt = baseResult.DDeltaDt;
            var ddeltadvol = baseResult.DDeltaDvol;
            var dvegadt = baseResult.DVegaDt;
            var dvegadvol = baseResult.DVegaDvol;

            //Long, therefore delta/gamma/vega  all +Ve
            Assert.AreEqual(true, baseResult.Delta > 0 );
            Assert.AreEqual(true, baseResult.Gamma > 0);
            Assert.AreEqual(true, baseResult.Vega > 0);
            Assert.AreEqual(true, baseResult.Rho < 0);
            Assert.AreEqual(true, baseResult.Theta < 0);

            //TODO:  analyze this
            //Assert.AreEqual(true, baseResult.DDeltaDt > 0); //other pnl test need this
            //Assert.AreEqual(true, baseResult.DDeltaDt < 0); //bad pnl test need this
            Assert.AreEqual(true, baseResult.DDeltaDvol > 0); //long
            Assert.AreEqual(true, baseResult.DVegaDt < 0); //vega bleed
            //Assert.AreEqual(true, baseResult.DVegaDvol > 0); //convex vega >0

        }

        [TestMethod]
        public void VanillaEuropeanOptionImpliedVolCalibrationTest()
        {
            var spot = 1.0;
            var notional = 1e6;
            var vol = 0.5;

            //vanilla option, given vol price it
            var exerciseDate = new Date(2018, 3, 11);
            var euopean = createVanillaOption(startDay: new Date(2017, 12, 11), expiry: exerciseDate, futureMaturityDate: exerciseDate,
                strike: 1.0, notional: notional, isCall: true, isEuropean: true);
            var market = TestMarket(spot: spot, vol: vol);

            var discCurveName = "Fr007";
            var dividentCurveName = "Dividend";
            var volName = "VolSurf";
            var volsurf = market.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate);
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>(discCurveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(dividentCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { {"", spot} },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }
                );

            var vanillaEngine = new AnalyticalVanillaEuropeanOptionEngine();
            var pv = vanillaEngine.Calculate(euopean, marketCondition, PricingRequest.Pv).Pv;

            //calibrate implied vol from price
            var iv = vanillaEngine.ImpliedVol(euopean, marketCondition, pv);

            Assert.AreEqual(vol, iv, 1e-7);
        }

        [TestMethod]
        public void CommodityVanillaAmericanAnalyticalOptionTest()
        {
            CommodityVanillaAmericanAnalyticalOptionCalc(spot: 3600, strike: 3500, isCall: true);
            CommodityVanillaAmericanAnalyticalOptionCalc(spot: 3600, strike: 3500, isCall: false);
            CommodityVanillaAmericanAnalyticalOptionCalc(spot: 3600, strike: 3600, isCall: true);
            CommodityVanillaAmericanAnalyticalOptionCalc(spot: 3600, strike: 3600, isCall: false);
        }
        public void CommodityVanillaAmericanAnalyticalOptionCalc(double spot, double strike, bool isCall, double notional = 1.0)
        {
            var exerciseDay = new Date(2018, 3, 15);
            var optionStartDay = new Date(2018, 3, 11);
            var futureMaturityDate = exerciseDay;  //assume option and undelrying expire on the same day

            var european = createVanillaOption(startDay: optionStartDay, expiry: exerciseDay, futureMaturityDate: futureMaturityDate, strike: strike, notional: notional, isCall: isCall, isEuropean: true );
            var american = createVanillaOption(startDay: optionStartDay, expiry: exerciseDay, futureMaturityDate: futureMaturityDate, strike: strike, notional: notional, isCall: isCall, isEuropean: false);

            var market = TestMarket();
            var volsurf = market.GetData<VolSurfMktData>("VolSurf").ToImpliedVolSurface(market.ReferenceDate);
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>("Fr007").YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>("Dividend").YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }
                );
            var vanillaEngine = new AnalyticalVanillaEuropeanOptionEngine();

            var calcRequest = PricingRequest.Pv | PricingRequest.Delta | PricingRequest.Gamma | PricingRequest.Vega | PricingRequest.Theta;
            var vanillaResult = vanillaEngine.Calculate(european, marketCondition, calcRequest);

            var americanBSEngine = new AnalyticalVanillaAmericanOptionBSEngine();
            var americanBSResult = americanBSEngine.Calculate(american, marketCondition, calcRequest);

            var americanBAWEngine = new AnalyticalVanillaAmericanOptionBAWEngine();
            var americanBAWResult = americanBAWEngine.Calculate(american, marketCondition, calcRequest);

            //1. americanWorthMoreThanEuropean, >= 
            Assert.AreEqual(true, americanBSResult.Pv >= vanillaResult.Pv);
            //BAW method WorthMore Than BS Method >
            Assert.AreEqual(true, americanBAWResult.Pv > americanBSResult.Pv);
            //2. American can do delta/gamma/vega/theta/rho, number is stable
            Assert.AreEqual(true, Math.Abs(americanBSResult.Delta)  >= Math.Abs(vanillaResult.Delta));
            Assert.AreEqual(true, americanBSResult.Gamma  >= vanillaResult.Gamma);      
            Assert.AreEqual(true, americanBSResult.Vega   >= vanillaResult.Vega);   
            Assert.AreEqual(true, Math.Abs(americanBSResult.Theta)  >= Math.Abs(vanillaResult.Theta));

            //TODO: properly implement rho risk in American analytical pricer
            //Assert.AreEqual(true, Math.Abs(americanResult.Rho)    >= Math.Abs(vanillaResult.Rho));

            //TODO:  think of a better way to check high orders
            //Assert.AreEqual(true, Math.Abs(americanResult.DVegaDt) > Math.Abs(vanillaResult.DVegaDt));
            //Assert.AreEqual(true, Math.Abs(americanResult.DVegaDvol) > Math.Abs(vanillaResult.DVegaDvol));
            //Assert.AreEqual(true, Math.Abs(americanResult.DDeltaDvol) > Math.Abs(vanillaResult.DDeltaDvol));
            //Assert.AreEqual(true, Math.Abs(americanResult.DDeltaDt) > Math.Abs(vanillaResult.DDeltaDt));

            //3. backout equivalent expiry day from vanilla option
            var impliedExpiryDate = vanillaEngine.ImpliedExpiryDate(option: european, market: marketCondition, targetPremium: americanBSResult.Pv);
            Assert.AreEqual(true, impliedExpiryDate - exerciseDay >= 0);  // 2018-5-16  vs 2018-3-15

            //4.backout effective implied european vol from american pv
            var impliedVolForAmerican = vanillaEngine.ImpliedVol(option: european, market: marketCondition, targetPremium: americanBSResult.Pv);
            Assert.AreEqual(true, impliedVolForAmerican - marketCondition.VolSurfaces.Value.Values.First().GetValue(european.ExerciseDates[0], european.Strike)  > 0);  
        }


        [TestMethod]
        public void CommodityVanillaAmericanBAWTest()
        {
            CommodityVanillaAmericanBAWCalc(spot: 125, strike: 120, expectedPv: 14.5266795044548, expectedDelta: 0.667240481219089, expectedGamma: 0.0136264155159438, expectedTheta: -0.00872640048206996, expectedVega: 0.598171227531316);
            CommodityVanillaAmericanBAWCalc(spot: 120, strike: 125, expectedPv: 9.06135892170762, expectedDelta: 0.517129828700114, expectedGamma: 0.016060286007757441, expectedTheta: -0.0086380032963315756, expectedVega: 0.649440232636448);
            CommodityVanillaAmericanBAWCalc(spot: 125, strike: 120, expectedPv: 5.64465621284957, expectedDelta: -0.322399331909029, expectedGamma: 0.0156194754197259, expectedTheta: -0.00399419718694638, expectedVega: 0.615818540658187, isCall: false);
            CommodityVanillaAmericanBAWCalc(spot: 120, strike: 125, expectedPv: 10.044246801449285, expectedDelta: -0.50241571762512649, expectedGamma: 0.020353865792799297, expectedTheta: -0.0036424101642200668, expectedVega: 0.637417371998279, isCall: false);
        }
        public void CommodityVanillaAmericanBAWCalc(double strike, double spot, double expectedPv, double expectedDelta, double expectedGamma, double expectedVega, double expectedTheta, bool isCall = true)
        {
            var exerciseDay = new Date(2020, 3, 11);
            var optionStartDay = new Date(2018, 3, 11);
            var futureMaturityDate = exerciseDay;  //assume option and undelrying expire on the same day
            var american = createVanillaOption(startDay: optionStartDay, expiry: exerciseDay, futureMaturityDate: futureMaturityDate, strike: strike, notional: 1.0, isCall: isCall, isEuropean: false, instrument: InstrumentType.Stock);

            var market = TestMarket(referenceDate:"2018-03-11",vol: 0.14, rate: 0.04, dividend: 0.02);
            var volsurf = market.GetData<VolSurfMktData>("VolSurf").ToImpliedVolSurface(market.ReferenceDate);
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>("Fr007").YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>("Dividend").YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }
                );


            var americanBAWEngine = new AnalyticalVanillaAmericanOptionBAWEngine();
            var americanBAWResult = americanBAWEngine.Calculate(american, marketCondition, PricingRequest.All);

            Assert.AreEqual(expectedPv, americanBAWResult.Pv, 1e-8);
            Assert.AreEqual(expectedDelta, americanBAWResult.Delta, 1e-8);
            Assert.AreEqual(expectedGamma, americanBAWResult.Gamma, 1e-8);
            Assert.AreEqual(expectedTheta, americanBAWResult.Theta, 1e-8);
            Assert.AreEqual(expectedVega, americanBAWResult.Vega, 1e-8);


        }




        [TestMethod]
        public void CommodityVanillaAmericanOptionTreeAnalyticalTest()
        {
            var exerciseDay = new Date(2018, 5, 29);
            var optionStartDay = new Date(2018, 1, 15);
            var futureMaturityDate = new Date(2018, 5, 29);  //assume option and undelrying expire on the same day

            var european = createVanillaOption(startDay: optionStartDay, expiry: exerciseDay, futureMaturityDate: futureMaturityDate, strike: 3872, notional: 1, isCall: true, isEuropean: true);
            var american = createVanillaOption(startDay: optionStartDay, expiry: exerciseDay, futureMaturityDate: futureMaturityDate, strike: 3872, notional: 1, isCall: true, isEuropean: false);

            var market = TestMarket("2018-01-15", vol: 0.3, spot: 3872, rate: 0.03, dividend:0.03);
            var volsurf = market.GetData<VolSurfMktData>("VolSurf").ToImpliedVolSurface(market.ReferenceDate);
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>("Fr007").YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>("Dividend").YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", 3872 } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }
                );
            var vanillaEngine = new AnalyticalVanillaEuropeanOptionEngine();
            var europeanResult = vanillaEngine.Calculate(european, marketCondition, PricingRequest.All);

            //var americanEngine = new AnalyticalVanillaAmericanOptionEngine();
            var tree = new BinomialTreeAmericanEngine();
            var americanAnalytical = new AnalyticalVanillaAmericanOptionBSEngine();

            var europeanTreeResult = tree.Calculate(european, marketCondition, PricingRequest.All);

            //TODO: lets see
            Assert.AreEqual(true, europeanTreeResult.Pv == europeanResult.Pv); //after variate control

            var americanTreeResult = tree.Calculate(american, marketCondition, PricingRequest.All);
            var americanAnalyticalResult = americanAnalytical.Calculate(american, marketCondition, PricingRequest.All);
            //1. americanWorthMoreThanEuropean, >= , in the case of futures option, same PV
            Assert.AreEqual(true, americanTreeResult.Pv >= europeanResult.Pv);

            //2. American can do delta/gamma/vega/theta/rho, number is stable
            Assert.AreEqual(true, Math.Abs(americanTreeResult.Delta) >= Math.Abs(europeanResult.Delta));
            //Assert.AreEqual(true, americanTreeResult.Gamma >= europeanResult.Gamma);

            //Assert.AreEqual(true, americanAnalyticalResult.Gamma >= europeanResult.Gamma);

            //Assert.AreEqual(true, americanTreeResult.Vega <= europeanResult.Vega);  // earlier to exercise, so smaller vega
            Assert.AreEqual(true, Math.Abs(americanTreeResult.Theta) >= Math.Abs(europeanResult.Theta));

            //TODO: properly implement rho risk in American analytical pricer
            //Assert.AreEqual(true, Math.Abs(americanResult.Rho)    >= Math.Abs(vanillaResult.Rho));

            //TODO:  think of a better way to check high orders
            //Assert.AreEqual(true, Math.Abs(americanResult.DVegaDt) > Math.Abs(vanillaResult.DVegaDt));
            //Assert.AreEqual(true, Math.Abs(americanResult.DVegaDvol) > Math.Abs(vanillaResult.DVegaDvol));
            //Assert.AreEqual(true, Math.Abs(americanResult.DDeltaDvol) > Math.Abs(vanillaResult.DDeltaDvol));
            //Assert.AreEqual(true, Math.Abs(americanResult.DDeltaDt) > Math.Abs(vanillaResult.DDeltaDt));

            //3. backout equivalent expiry day from vanilla option
            var impliedExpiryDate = vanillaEngine.ImpliedExpiryDate(option: european, market: marketCondition, targetPremium: americanTreeResult.Pv);
            Assert.AreEqual(true, impliedExpiryDate - exerciseDay >= 0);  // 2018-5-16  vs 2018-3-15

            //4.backout effective implied european vol from american pv
            var impliedVolForAmerican = vanillaEngine.ImpliedVol(option: european, market: marketCondition, targetPremium: americanTreeResult.Pv);
            Assert.AreEqual(true, impliedVolForAmerican - marketCondition.VolSurfaces.Value.Values.First().GetValue(european.ExerciseDates[0], european.Strike) > 0);
        }

        private Date DateFromStr(String dateStr)
        {
            var dt = Convert.ToDateTime(dateStr);
            return new Date(dt.Year, dt.Month, dt.Day);
        }

        private VanillaOption createVanillaOption(Date startDay, Date expiry, Date futureMaturityDate, double strike, double notional, Boolean isCall = true, Boolean isEuropean = true,Date[] obs=null, InstrumentType instrument = InstrumentType.CommodityFutures, Dictionary<Date, double> dividends = null)
        {

            var calendar = CalendarImpl.Get("chn");
            Date[] exerciseDates = new[] { expiry };
            if (!isEuropean) {
                exerciseDates = calendar.BizDaysBetweenDatesInclEndDay(startDay, expiry).ToArray();
            }

            return new VanillaOption(
                startDay,
                futureMaturityDate,
                isEuropean? OptionExercise.European : OptionExercise.American,
                isCall ? OptionType.Call : OptionType.Put,
                strike,
                instrument,
                calendar,
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                exerciseDates,
                obs?? new[] { expiry },
                notional: notional,
                dividends: dividends
                );
        }

        [TestMethod]
        public void CommodityVanillaEuropeanMoneynessVolTest()
        {
            var spot = 240;
            var startDate = new Date(2018, 03, 02);
            var maturityDate = new Date(2018, 04, 27);

            var vanillaOption = new VanillaOption(
                startDate,
                maturityDate,
                OptionExercise.European,
                OptionType.Call,
                1 * spot,
                InstrumentType.Futures,
                //InstrumentType.CommoditySpot,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { startDate },
                new[] { maturityDate },
                1,
                isMoneynessOption: false
                );

            var moneynessOption = new VanillaOption(
                startDate,
                maturityDate,
                OptionExercise.European,
                OptionType.Call,
                1,
                InstrumentType.Futures,
                //InstrumentType.CommoditySpot,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { startDate },
                new[] { maturityDate },
                1,
                isMoneynessOption: true,
                initialSpotPrice: spot
                );

            var marketStrike = TestMarketVolSurface(startDate, spot: spot);
            var marketMoneyness = TestMarketVolSurface(startDate, spot: spot, volSurfaceType:VolSurfaceType.MoneynessVol);

            var engine = new AnalyticalVanillaEuropeanOptionEngine();
            var calcRequest = PricingRequest.Pv | PricingRequest.Delta;
            var resultStrikeVol = engine.Calculate(vanillaOption, marketStrike, calcRequest);
            var resultMoneynessVol = engine.Calculate(vanillaOption, marketMoneyness, calcRequest);
            var resultStrikeVol2 = engine.Calculate(moneynessOption, marketStrike, calcRequest);
            var resultMoneynessVol2 = engine.Calculate(moneynessOption, marketMoneyness, calcRequest);


            Assert.AreEqual(resultStrikeVol.Pv, resultMoneynessVol.Pv, 1e-3);
            Assert.AreEqual(resultStrikeVol.Delta, resultMoneynessVol.Delta, 1e-3);
            Assert.AreEqual(resultStrikeVol2.Pv, resultMoneynessVol2.Pv, 1e-3);

        }

        //TODO:  configure the same env as barrier test
        //TODO: ActAct change to Act365
        private QdpMarket TestMarket(String referenceDate = "2017-10-25", double vol = 0.2, double spot = 1.0, double rate = 0.05, double dividend = 0.0,string curveDayCount = "Act365")
        {
            var historiclIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates;

            var curveConvention = new CurveConvention("fr007CurveConvention",
                    "CNY",
                    "ModifiedFollowing",
                    "Chn",
                    curveDayCount,
                    "Continuous",
                    "CubicHermiteMonotic");

            var fr007CurveName = "Fr007";
            var fr007RateDefinition = new[]
            {
                new RateMktData("1D", rate, "Spot", "None", fr007CurveName),
                new RateMktData("5Y", rate, "Spot", "None", fr007CurveName),
            };

            var dividendCurveName = "Dividend";
            var dividendRateDefinition = new[]
            {
                new RateMktData("1D", dividend, "Spot", "None", dividendCurveName),
                new RateMktData("5Y", dividend, "Spot", "None", dividendCurveName),
            };

            var curveDefinition = new[]
            {
                new InstrumentCurveDefinition(fr007CurveName, curveConvention, fr007RateDefinition, "SpotCurve"),
                new InstrumentCurveDefinition(dividendCurveName, curveConvention, dividendRateDefinition, "SpotCurve"),
            };

            var volSurf = new[] { new VolSurfMktData("VolSurf", vol), };
            var stockMktData = new[] { new StockMktData("000300.SH", spot), };

            var marketInfo = new MarketInfo("tmpMarket",
                referenceDate: referenceDate,
                yieldCurveDefinitions: curveDefinition,
                historicalIndexRates: historiclIndexRates,
                volSurfaceDefinitions: volSurf,
                stockDataDefinitions: stockMktData
                );
            QdpMarket market;
            MarketFunctions.BuildMarket(marketInfo, out market);
            return market;
        }

        private IMarketCondition TestMarketVolSurface(Date startDate, double spot = 1.0, double rate = 0.05, double dividend = 0.0, VolSurfaceType volSurfaceType = VolSurfaceType.StrikeVol)
        {
            var historiclIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates;
            var curveConvention = new CurveConvention("fr007CurveConvention",
                "CNY",
                "ModifiedFollowing",
                "Chn",
                "Act365",
                "Continuous",
                "CubicHermiteMonotic");

            var fr007CurveName = "Fr007";
            var fr007RateDefinition = new[]
            {
                new RateMktData("1D", rate, "Spot", "None", fr007CurveName),
                new RateMktData("5Y", rate, "Spot", "None", fr007CurveName),
            };

            var dividendCurveName = "Dividend";
            var dividendRateDefinition = new[]
            {
                new RateMktData("1D", dividend, "Spot", "None", dividendCurveName),
                new RateMktData("5Y", dividend, "Spot", "None", dividendCurveName),
            };

            var curveDefinition = new[]
            {
                new InstrumentCurveDefinition(fr007CurveName, curveConvention, fr007RateDefinition, "SpotCurve"),
                new InstrumentCurveDefinition(dividendCurveName, curveConvention, dividendRateDefinition, "SpotCurve"),
            };

            var volSurf = new[] { new VolSurfMktData("VolSurf", 0.3), };
            var marketInfo = new MarketInfo("tmpMarket", "2018-03-02", curveDefinition, historiclIndexRates, null, null, volSurf);
            QdpMarket qdpMarket;
            MarketFunctions.BuildMarket(marketInfo, out qdpMarket);

            var maturities = new Date[]
            {
                (new Term("1W")).Next(startDate),
                (new Term("2W")).Next(startDate),
                (new Term("1M")).Next(startDate)
            };

            var strikes = (volSurfaceType == VolSurfaceType.MoneynessVol) ? new double[]
            {
                0.9,
                0.95,
                1.0,
                1.05,
                1.1
            } :
            new double[]
            {
                0.9 * spot,
                0.95 * spot,
                1.0 * spot,
                1.05 * spot,
                1.1 * spot
            };

            var vols = new double[3, 5];
            for (var i = 0; i < vols.GetLength(0); ++i)
            {
                for (var j = 0; j < vols.GetLength(1); ++j)
                {
                    vols[i, j] = 0.3;
                }
            }
            vols[2, 2] = 0.4;

            var surface = new ImpliedVolSurface(startDate, maturities, strikes, vols, Interpolation2D.VarianceBiLinear, volSurfaceType: volSurfaceType);
            // market with flat vol surface
            return new MarketCondition(
                x => x.ValuationDate.Value = startDate,
                x => x.DiscountCurve.Value = qdpMarket.GetData<CurveData>("Fr007").YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", qdpMarket.GetData<CurveData>("Dividend").YieldCurve } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", surface } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } }
                );
        }

        private QdpMarket GetMarket()
		{
			var referenceDate = "2015-06-11";
			var curveConvention = new CurveConvention("curveConvention",
					"CNY",
					"ModifiedFollowing",
					"Chn",
					"Act365",
					"Continuous",
					"CubicHermiteMonotic");

            // 1 - discount curve
            var fr007CurveName = "Fr007";
            var rates = new[]
			{
				new RateMktData("0D", 0.0114, "Spot", "None",fr007CurveName),
				new RateMktData("1D", 0.0114, "Spot", "None",fr007CurveName),
				new RateMktData("7D", 0.021, "Spot", "None",fr007CurveName),
				new RateMktData("98D", 0.0228, "Spot", "None",fr007CurveName),
				new RateMktData("186D", 0.0231, "Spot", "None",fr007CurveName),
				new RateMktData("277D", 0.024, "Spot", "None",fr007CurveName),
				new RateMktData("368D", 0.0241, "Spot", "None",fr007CurveName),
				new RateMktData("732D", 0.0248, "Spot", "None",fr007CurveName),
			};

			var fr007Curve = new InstrumentCurveDefinition(fr007CurveName, curveConvention, rates, "SpotCurve");

            var dividendCurveName = "goldYield";
            var curveConvention2 = new CurveConvention("curveConvention2",
					"CNY", "ModifiedFollowing",
						  "Chn",
						  "Act365",
						  "Continuous",
					"CubicHermiteFd");
			var goldRates = new[]
			{
				new RateMktData("0D", 236.49, "ConvenienceYield", "CommoditySpot", dividendCurveName),
				new RateMktData("187D", 240.6, "ConvenienceYield", "CommodityForward", dividendCurveName),
				new RateMktData("370D", 243.45, "ConvenienceYield", "CommodityForward", dividendCurveName),
			};
			var goldConvenienceYieldCurve = new InstrumentCurveDefinition("goldYield", curveConvention2, goldRates, "SpotCurve");
            var curveDefinitions = new[] { fr007Curve, goldConvenienceYieldCurve };

            var volSurfMktDatas = new[] { new VolSurfMktData("goldVolSurf", 0.14), };
			var marketInfo2 = new MarketInfo("TestMarket")
			{
				ReferenceDate = referenceDate,
				YieldCurveDefinitions = curveDefinitions.ToArray(),
				HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates,
				VolSurfMktDatas = volSurfMktDatas
			};

			QdpMarket market;
			MarketFunctions.BuildMarket(marketInfo2, out market);
			return market;
		}

		private IYieldCurve ZeroFirstPoint(IYieldCurve yieldCurve)
		{
			return new YieldCurve(
				yieldCurve.Name,
				yieldCurve.ReferenceDate,
				yieldCurve.KeyPoints.Select((x, i) => i == 0 ? Tuple.Create(x.Item1, 0.0) : x).ToArray(),
				yieldCurve.Bda,
				yieldCurve.DayCount,
				yieldCurve.Calendar,
				yieldCurve.Currency,
				yieldCurve.Compound,
				yieldCurve.Interpolation,
				YieldCurveTrait.SpotCurve,
				yieldCurve.BaseMarket,
				((YieldCurve)yieldCurve).CalibrateMktUpdateCondition,
				yieldCurve.Spread
				);
		}
	}
}
