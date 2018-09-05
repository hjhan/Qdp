using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using UnitTest.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Ecosystem.Utilities;
using System.Collections.Generic;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Base.Utilities;


namespace UnitTest.Commodity
{
    [TestClass]
    public class CommoditySpreadOptionTest
    {
        [TestMethod]
        public void SpreadOptionPricingTest()
        {
            SpreadOptionGreekCalc(1.06593465293668E-08, spreadType: "TwoAssetsSpread");
            SpreadOptionGreekCalc(2.4847739350197E-22, spreadType: "ThreeAssetsSpread");
            SpreadOptionGreekCalc(0.045922252277597, spreadType: "ThreeAssetsSpreadBasket");
            SpreadOptionGreekCalc(1.09088182242643E-22, spreadType: "FourAssetsSpread");
            SpreadOptionGreekCalc(0.045922252277597, spreadType: "FourAssetsSpreadBasketType1");
            SpreadOptionGreekCalc(3.48723715134953, spreadType: "FourAssetsSpreadBasketType2");
        }

        private void SpreadOptionGreekCalc(double ExpectedPv, string spreadType = "TwoAssetsSpread", string ValuationDate = "2017-12-26")
        {
            var valuationDate = DateFromStr(ValuationDate);
            var maturityDate = new Term("176D").Next(valuationDate);
            var calendar = CalendarImpl.Get("chn");

            var asset1 = "asset1";
            var asset2 = "asset2";
            var asset3 = "asset3";
            var asset4 = "asset4";


            var option = new SpreadOption(
               startDate: valuationDate,
               maturityDate: maturityDate,
               exercise: OptionExercise.European,
               optionType: OptionType.Call,
               spreadType: ToSpreadType(spreadType),
               strike: 1.03,
               weights:new double[] { 1.0,1.0,1.0,1.0},
               underlyingInstrumentType: InstrumentType.EquityIndex,
               calendar: calendar,
               dayCount: new Act365(),
               payoffCcy: CurrencyCode.CNY,
               settlementCcy: CurrencyCode.CNY,
               exerciseDates: new[] { maturityDate },
               observationDates: new[] { maturityDate },
               underlyingTickers: new[] { asset1, asset2, asset3, asset4 }
               )
            {
                UnderlyingTickers = new string[] { asset1, asset2, asset3, asset4 }
            };


            var market = TestMarket(referenceDate: ValuationDate, asset1: asset1, asset2: asset2, asset3: asset3, asset4: asset4);

            //var analyticalEngine = new AnalyticalSpreadOptionEngine();
            //var analyticalResult = analyticalEngine.Calculate(option, market, PricingRequest.All);

            var Engine = new AnalyticalSpreadOptionKirkEngine();
            var Result = Engine.Calculate(option, market, PricingRequest.All);

            //Assert.AreEqual(ExpectedPv, analyticalResult.Pv, 1e-8);
            Assert.AreEqual(ExpectedPv, Result.Pv, 1e-8);
        }

        [TestMethod]
        public void SpreadOptionPnLTest()
        {
            SpreadOptionGreekTest(spreadType: "TwoAssetsSpread", volMove: 0.10, mktMove: 10e-4, toleranceInPct: 3);
            SpreadOptionGreekTest(spreadType: "ThreeAssetsSpread", volMove: 0.10, mktMove: 10e-4, toleranceInPct: 3);
            SpreadOptionGreekTest(spreadType: "ThreeAssetsSpreadBasket",  volMove: 0.10, mktMove: 10e-4, toleranceInPct: 3);
            
        }
        private void SpreadOptionGreekTest(double vol1 = 0.28, double vol2 = 0.30, double vol3 = 0.40, double spot1 = 4900, double spot2 = 1500, double spot3 = 1500, 
            string spreadType = "TwoAssetsSpread", string t0 = "2017-12-26", string t1 = "2017-12-27", double volMove = 0.10, double mktMove = 1e-4, double toleranceInPct = 2)
        {
            var valuationDate = DateFromStr(t0);
            var maturityDate = new Term("176D").Next(valuationDate);
            var calendar = CalendarImpl.Get("chn");
            var spot1New = spot1 + spot1 * mktMove;
            var spot2New = spot2 + spot2 * mktMove;
            var spot3New = spot3 + spot3 * mktMove;
            var vol1New = vol1 + volMove;
            var vol2New = vol2 + volMove;
            var vol3New = vol3 + volMove;
            var T0 = DateFromStr(t0);
            var T1 = DateFromStr(t1);


            var valuationDay = t0;
            var valuationDayNew = t1;

            var asset1 = "asset1";
            var asset2 = "asset2";
            var asset3 = "asset3";
            var asset4 = "asset4";
            var option = new SpreadOption(
              startDate: valuationDate,
              maturityDate: maturityDate,
              exercise: OptionExercise.European,
              optionType: OptionType.Call,
              spreadType: ToSpreadType(spreadType),
              strike: 1000,
              weights: new double[] { 1.0, 1.0, 1.0,1.0 },
              underlyingInstrumentType: InstrumentType.EquityIndex,
              calendar: calendar,
              dayCount: new Act365(),
              payoffCcy: CurrencyCode.CNY,
              settlementCcy: CurrencyCode.CNY,
              exerciseDates: new[] { maturityDate },
              observationDates: new[] { maturityDate },
              underlyingTickers: new[] { asset1, asset2, asset3,asset4 }
              )
            {
                UnderlyingTickers = new string[] { asset1, asset2, asset3, asset4 }
            };

            var market = TestMarket(referenceDate: t0, vol1: vol1, vol2: vol2, vol3: vol3, spot1: spot1, spot2: spot2, spot3: spot3,
                asset1: asset1, asset2: asset2, asset3: asset3, asset4: asset4);
            var marketNew = TestMarket(referenceDate: t1, vol1: vol1New, vol2: vol2New, vol3: vol3New, spot1: spot1New, spot2: spot2New, spot3: spot3New,
                asset1: asset1, asset2: asset2, asset3: asset3, asset4: asset4);
            var marketPI = TestMarket(referenceDate: t0, vol1: vol1, vol2: vol2, vol3: vol3, spot1: spot1New, spot2: spot2New, spot3: spot3New,
                asset1: asset1, asset2: asset2, asset3: asset3, asset4: asset4);
            var marketVI = TestMarket(referenceDate: t0, vol1: vol1New, vol2: vol2New, vol3: vol3New, spot1: spot1, spot2: spot2, spot3: spot3,
                asset1: asset1, asset2: asset2, asset3: asset3, asset4: asset4);
            var marketPVC = TestMarket(referenceDate: t0, vol1: vol1New, vol2: vol2New, vol3: vol3New, spot1: spot1New, spot2: spot2New, spot3: spot3New,
                asset1: asset1, asset2: asset2, asset3: asset3, asset4: asset4);

            var engine = new AnalyticalSpreadOptionKirkEngine();

            var result = engine.Calculate(option, market, PricingRequest.All);
            var resultNew = engine.Calculate(option, marketNew, PricingRequest.All);
            var resultPI = engine.Calculate(option, marketPI, PricingRequest.All);
            var resultVI = engine.Calculate(option, marketVI, PricingRequest.All);
            var resultPVC = engine.Calculate(option, marketPVC, PricingRequest.All);

            var actualPL = resultNew.Pv - result.Pv;

            //price Impact
            //PI = PV(t-1, priceNew) - Pv(t-1)
            var basePv = result.Pv;
            var PI = resultPI.Pv - basePv;
            var thetapl = result.Theta * (T1 - T0);

            //vol impact
            //VI = PV(t-1. volNew) - Pv (t-1)
            var VI = resultVI.Pv - basePv;

            //price vol cross impact
            //PVC = PV(t-1. volNew, PriceNew) - Pv (t-1) - (PI+VI)
            var PVC = resultPVC.Pv - basePv - PI - VI;

            var newEstimate = PI + VI + PVC + thetapl;
            var newUnexplained = actualPL - newEstimate;

            //Time impact
            //TI = PV(t, all OldInfo) - Pv(t-1)

            //TODO:
            //Time/ price cross Impact
            //TPC = PV(t, priceNew) - pv(t-1) - (TI +PI)

            //Time/vol cross impact
            //TVC = PV(t, volNew) - pv(t-1) -(TI+VI)


            //TODO: 
            //in case of big move ( vol and spot), we need high order risk to explain pnl
            //var diff = actualPL - esimstatedPL;
            //Assert.AreEqual(true, Math.Abs(diff / actualPL) * 100.0 < toleranceInPct); //pnl well explained in not too extreme moves
            Assert.AreEqual(true, Math.Abs(newUnexplained / actualPL) * 100.0 < toleranceInPct);

        }




        //Degenerate ThreeAssetsSpreadOption and ThreeAssetsSpreadOption to TwoAssetsSpreadOption with S3=vol3=rho23=rho31=0.0;
        //Three Prices should be the same.
        [TestMethod]
        public void SpreadOptionParityTwoAssetsTest()
        {
            SpreadOptionTwoAssetsParity();
            SpreadOptionTwoAssetsParity(vol1: 0.5, vol2: 0.4, spot1: 4500, spot2: 4900, strike: 300, rho12: 0.5);

        }

        private void SpreadOptionTwoAssetsParity(string ValuationDate = "2017-12-26", double vol1 = 0.28, double spot1 = 1.0, double vol2 = 0.30, double spot2 = 2.0, double strike = 1.03, double rho12 = 0.4)
        {
            var valuationDate = DateFromStr(ValuationDate);
            var maturityDate = new Term("176D").Next(valuationDate);
            var calendar = CalendarImpl.Get("chn");

            var asset1 = "asset1";
            var asset2 = "asset2";
            var asset3 = "asset3";
            var asset4 = "asset4";

            var option1 = new SpreadOption(
                startDate: valuationDate,
                maturityDate: maturityDate,
                exercise: OptionExercise.European,
                optionType: OptionType.Call,
                spreadType: SpreadType.ThreeAssetsSpread,
                strike: strike,
                weights: new double[] { 1.0, 1.0, 1.0,1.0 },
                underlyingInstrumentType: InstrumentType.EquityIndex,
                calendar: calendar,
                dayCount: new Act365(),
                payoffCcy: CurrencyCode.CNY,
                settlementCcy: CurrencyCode.CNY,
                exerciseDates: new[] { maturityDate },
                observationDates: new[] { maturityDate },
                underlyingTickers: new[] { asset1, asset2, asset3, asset4 }
                )
            {
                UnderlyingTickers = new string[] { asset1, asset2, asset3, asset4 }
            };


            var option2 = new SpreadOption(
                startDate: valuationDate,
                maturityDate: maturityDate,
                exercise: OptionExercise.European,
                optionType: OptionType.Call,
                spreadType: SpreadType.TwoAssetsSpread,
                strike: strike,
                weights: new double[] { 1.0, 1.0, 1.0,1.0 },
                underlyingInstrumentType: InstrumentType.EquityIndex,
                calendar: calendar,
                dayCount: new Act365(),
                payoffCcy: CurrencyCode.CNY,
                settlementCcy: CurrencyCode.CNY,
                exerciseDates: new[] { maturityDate },
                observationDates: new[] { maturityDate },
                underlyingTickers: new[] { asset1, asset2, asset3, asset4 }
                )
            {
                UnderlyingTickers = new string[] { asset1, asset2, asset3, asset4 }
            };

            var market = TestMarket(referenceDate: ValuationDate, vol1: vol1, vol2: vol2, vol3: 0.0, spot1: spot1, spot2: spot2, spot3: 0.0, 
                asset1: asset1, asset2: asset2, asset3: asset3, asset4: asset4, rho12: rho12, rho23: 0.0, rho13:0.0);       

            var analyticalEngine = new AnalyticalSpreadOptionBjerksundEngine();
            var result1 = analyticalEngine.Calculate(option1, market, PricingRequest.All);
            var result2 = analyticalEngine.Calculate(option2, market, PricingRequest.All);
     
            Assert.AreEqual(result1.Pv, result2.Pv, 1.0e-8);
        }

        [TestMethod]
        public void SpreadOptionParityThreeAssetsTest()
        {
            SpreadOptionParity();
            SpreadOptionParity(vol1: 0.5, vol2: 0.4, spot1: 4500, spot2: 4900, strike: 300);

        }

        private void SpreadOptionParity(string ValuationDate = "2017-12-26", double vol1 = 0.28, double spot1 = 1.0, double vol2 = 0.30, double spot2 = 2.0, double vol3 = 0.30, double spot3 = 2.0, double strike = 1.03)
        {
            var valuationDate = DateFromStr(ValuationDate);
            var maturityDate = new Term("176D").Next(valuationDate);
            var calendar = CalendarImpl.Get("chn");

            var asset1 = "asset1";
            var asset2 = "asset2";
            var asset3 = "asset3";
            var asset4 = "asset4";

            var option1 = new SpreadOption(
                startDate: valuationDate,
                maturityDate: maturityDate,
                exercise: OptionExercise.European,
                optionType: OptionType.Call,
                spreadType: SpreadType.ThreeAssetsSpread,
                strike: strike,
                weights: new double[] { 1.0, 1.0, 1.0, 1.0 },
                underlyingInstrumentType: InstrumentType.EquityIndex,
                calendar: calendar,
                dayCount: new Act365(),
                payoffCcy: CurrencyCode.CNY,
                settlementCcy: CurrencyCode.CNY,
                exerciseDates: new[] { maturityDate },
                observationDates: new[] { maturityDate },
                underlyingTickers: new[] { asset1, asset2, asset3, asset4 }
                )
            {
                UnderlyingTickers = new string[] { asset1, asset2, asset3, asset4 }
            };


            var option2 = new SpreadOption(
                startDate: valuationDate,
                maturityDate: maturityDate,
                exercise: OptionExercise.European,
                optionType: OptionType.Call,
                spreadType: SpreadType.FourAssetsSpread,
                strike: strike,
                weights: new double[] { 1.0, 1.0, 1.0, 1.0 },
                underlyingInstrumentType: InstrumentType.EquityIndex,
                calendar: calendar,
                dayCount: new Act365(),
                payoffCcy: CurrencyCode.CNY,
                settlementCcy: CurrencyCode.CNY,
                exerciseDates: new[] { maturityDate },
                observationDates: new[] { maturityDate },
                underlyingTickers: new[] { asset1, asset2, asset3, asset4 }
                )
            {
                UnderlyingTickers = new string[] { asset1, asset2, asset3, asset4 }
            };

            var option3 = new SpreadOption(
                startDate: valuationDate,
                maturityDate: maturityDate,
                exercise: OptionExercise.European,
                optionType: OptionType.Call,
                spreadType: SpreadType.ThreeAssetsSpreadBasket,
                strike: strike,
                weights: new double[] { 1.0, 1.0, 1.0, 1.0 },
                underlyingInstrumentType: InstrumentType.EquityIndex,
                calendar: calendar,
                dayCount: new Act365(),
                payoffCcy: CurrencyCode.CNY,
                settlementCcy: CurrencyCode.CNY,
                exerciseDates: new[] { maturityDate },
                observationDates: new[] { maturityDate },
                underlyingTickers: new[] { asset1, asset2, asset3, asset4 }
                )
            {
                UnderlyingTickers = new string[] { asset1, asset2, asset3, asset4 }
            };


            var option4 = new SpreadOption(
                startDate: valuationDate,
                maturityDate: maturityDate,
                exercise: OptionExercise.European,
                optionType: OptionType.Call,
                spreadType: SpreadType.FourAssetsSpreadBasketType1,
                strike: strike,
                weights: new double[] { 1.0, 1.0, 1.0, 1.0 },
                underlyingInstrumentType: InstrumentType.EquityIndex,
                calendar: calendar,
                dayCount: new Act365(),
                payoffCcy: CurrencyCode.CNY,
                settlementCcy: CurrencyCode.CNY,
                exerciseDates: new[] { maturityDate },
                observationDates: new[] { maturityDate },
                underlyingTickers: new[] { asset1, asset2, asset3, asset4 }
                )
            {
                UnderlyingTickers = new string[] { asset1, asset2, asset3, asset4 }
            };

            var market = TestMarket(referenceDate: ValuationDate, vol1: vol1, vol2: vol2, vol3: vol3, spot1: spot1, spot2: spot2, spot3: spot3,
                asset1: asset1, asset2: asset2, asset3: asset3, asset4: asset4);

            var analyticalEngine = new AnalyticalSpreadOptionKirkEngine();
            var result1 = analyticalEngine.Calculate(option1, market, PricingRequest.All);
            var result2 = analyticalEngine.Calculate(option2, market, PricingRequest.All);
            var result3 = analyticalEngine.Calculate(option3, market, PricingRequest.All);
            var result4 = analyticalEngine.Calculate(option4, market, PricingRequest.All);

            var Engine = new AnalyticalSpreadOptionBjerksundEngine();
            var result5 = Engine.Calculate(option1, market, PricingRequest.All);
            var result6 = Engine.Calculate(option2, market, PricingRequest.All);
            var result7 = Engine.Calculate(option3, market, PricingRequest.All);
            var result8 = Engine.Calculate(option4, market, PricingRequest.All);

            Assert.AreEqual(result1.Pv, result2.Pv, 1.0e-8);
            Assert.AreEqual(result3.Pv, result4.Pv, 1.0e-8);
            //Assert.AreEqual(result5.Pv, result6.Pv, 1.0e-8);
            Assert.AreEqual(result7.Pv, result8.Pv, 1.0e-8);
        }



        [TestMethod]
        public void SpreadVanillaTest()
        {
            SpreadVanillaCalc();
        }
        private void SpreadVanillaCalc(string spreadType = "TwoAssetsSpread", string ValuationDate = "2017-12-26",
            double spot1 = 4500, double spot2 = 2500, double strike = 2000, double vol1=0.5, double vol2=0.5, double rho12 = 0.5 )
        {
            var valuationDate = DateFromStr(ValuationDate);
            var maturityDate = new Term("176D").Next(valuationDate);
            var calendar = CalendarImpl.Get("chn");

            var asset1 = "asset1";
            var asset2 = "asset2";
            var asset3 = "asset3";
            var asset4 = "asset4";

            var option = new SpreadOption(
               startDate: valuationDate,
               maturityDate: maturityDate,
               exercise: OptionExercise.European,
               optionType: OptionType.Call,
               spreadType: ToSpreadType(spreadType),
               strike: strike,
               weights: new double[] { 1.0, 1.0, 1.0,1.0 },
               underlyingInstrumentType: InstrumentType.EquityIndex,
               calendar: calendar,
               dayCount: new Act365(),
               payoffCcy: CurrencyCode.CNY,
               settlementCcy: CurrencyCode.CNY,
               exerciseDates: new[] { maturityDate },
               observationDates: new[] { maturityDate },
               underlyingTickers: new[] { asset1, asset2, asset3, asset4 }
               )
            {
                UnderlyingTickers = new string[] { asset1, asset2, asset3, asset4 }
            };

            var option1 = new VanillaOption(
               valuationDate,
               maturityDate,
               OptionExercise.European,
               OptionType.Call,
               2.0*strike,
               InstrumentType.EquityIndex,
               calendar,
               new Act365(),
               CurrencyCode.CNY,
               CurrencyCode.CNY,
               new[] { maturityDate },
               new[] { maturityDate }
               );

            var option2 = new VanillaOption(
               valuationDate,
               maturityDate,
               OptionExercise.European,
               OptionType.Put,
               strike,
               InstrumentType.EquityIndex,
               calendar,
               new Act365(),
               CurrencyCode.CNY,
               CurrencyCode.CNY,
               new[] { maturityDate },
               new[] { maturityDate }
               );

            var market = TestMarket(referenceDate: ValuationDate, vol1: vol1, vol2: vol2, vol3: 0.0, spot1: spot1, spot2: spot2, spot3: 0.0,
                asset1: asset1, asset2: asset2, asset3: asset3, asset4: asset4, rho12: rho12, rho23: 0.0, rho13: 0.0);
            var market1 = GetMarket(referenceDate: ValuationDate, vol: vol1,  spot: spot1);
            var market2 = GetMarket(referenceDate: ValuationDate, vol: vol2, spot: spot2);

            var analyticalEngine = new AnalyticalSpreadOptionKirkEngine();
            var analyticalEngine2 = new AnalyticalVanillaEuropeanOptionEngine();
            var result = analyticalEngine.Calculate(option, market, PricingRequest.All);
            var result1 = analyticalEngine2.Calculate(option1, market1, PricingRequest.All);
            var result2 = analyticalEngine2.Calculate(option2, market2, PricingRequest.All);

            Assert.AreEqual(true, result1.Pv + result2.Pv - result.Pv > 0);
        }


        [TestMethod]
        public void SpreadOptionKirkBjerksundTest()
        {
            //SpreadOptionBjerksundCalc(spreadType: "TwoAssetsSpread");
            //SpreadOptionBjerksundCalc(spreadType: "ThreeAssetsSpread");
            //SpreadOptionBjerksundCalc(spreadType: "ThreeAssetsSpreadBasket", tol: 1e-2);
            //SpreadOptionBjerksundCalc(spreadType: "TwoAssetsSpread", isCall: false);
            //SpreadOptionBjerksundCalc(spreadType: "ThreeAssetsSpread", isCall: false);
            //SpreadOptionBjerksundCalc(spreadType: "ThreeAssetsSpreadBasket", isCall: false, tol: 1e-3);
            //SpreadOptionBjerksundCalc(spreadType: "TwoAssetsSpread", spot1: 4900, spot2: 1900, strike: 3000, tol: 1e-2);
            //SpreadOptionBjerksundCalc(spreadType: "ThreeAssetsSpread", spot1: 4900, spot2: 1500, spot3: 1500, strike: 1900, tol: 1e-2);
            //SpreadOptionBjerksundCalc(spreadType: "FourAssetsSpread", spot1: 4900, spot2: 1500, spot3: 1500, spot4: 1000, strike: 1900, tol: 1);
            //SpreadOptionBjerksundCalc(spreadType: "ThreeAssetsSpreadBasket", spot1: 4900, spot2: 1500, spot3: 1500, strike: 4000, tol: 1e2);
            //SpreadOptionBjerksundCalc(spreadType: "FourAssetsSpreadBasketType1", spot1: 4900, spot2: 1500, spot3: 1500, spot4: 1500, strike: 4000, tol: 1e2);
            SpreadOptionBjerksundCalc(spreadType: "FourAssetsSpreadBasketType2", spot1: 4900, spot2: 1500, spot3: 1500, spot4: 1500, strike: 4000, tol: 1);
        }

        private void SpreadOptionBjerksundCalc(string spreadType = "TwoAssetsSpread", Boolean isCall = true, string ValuationDate = "2017-12-26", double vol1 = 0.28, double spot1 = 1.0, double vol2 = 0.30, double spot2 = 2.0,
            double vol3 = 0.28, double spot3 = 1.0, double vol4 = 0.28, double spot4 = 1.0, double strike = 1.03,double tol=1e-8)
        {
            var valuationDate = DateFromStr(ValuationDate);
            var maturityDate = new Term("176D").Next(valuationDate);
            var calendar = CalendarImpl.Get("chn");

            var asset1 = "asset1";
            var asset2 = "asset2";
            var asset3 = "asset3";
            var asset4 = "asset4";


            var option = new SpreadOption(
               startDate: valuationDate,
               maturityDate: maturityDate,
               exercise: OptionExercise.European,
               optionType: (isCall) ? OptionType.Call : OptionType.Put,
               spreadType: ToSpreadType(spreadType),
               strike: strike,
               weights: new double[] { 1.0, 1.0, 1.0,1.0 },
               underlyingInstrumentType: InstrumentType.EquityIndex,
               calendar: calendar,
               dayCount: new Act365(),
               payoffCcy: CurrencyCode.CNY,
               settlementCcy: CurrencyCode.CNY,
               exerciseDates: new[] { maturityDate },
               observationDates: new[] { maturityDate },
               underlyingTickers: new[] { asset1, asset2, asset3, asset4 }
               )
            {
                UnderlyingTickers = new string[] { asset1, asset2, asset3 ,asset4}
            };


            var market = TestMarket(referenceDate: ValuationDate, vol1: vol1, vol2: vol2, vol3: vol3, vol4:vol4,spot1: spot1, spot2: spot2, spot3: spot3,spot4:spot4,
                asset1: asset1, asset2: asset2, asset3: asset3, asset4: asset4);

            var analyticalEngine = new AnalyticalSpreadOptionBjerksundEngine();
            var analyticalResult = analyticalEngine.Calculate(option, market, PricingRequest.All);

            var Engine = new AnalyticalSpreadOptionKirkEngine();
            var Result = Engine.Calculate(option, market, PricingRequest.All);

            Assert.AreEqual(analyticalResult.Pv, Result.Pv, tol);

        }



        private IMarketCondition TestMarket(String referenceDate = "2017-12-26",
            Double vol1 = 0.28, Double vol3 = 0.30, Double vol2=0.40, Double vol4=0.0,Double spot1 = 1.0, Double spot3 = 2.0, Double spot2 = 1.5, Double spot4=0.0,Double rho13 = 0.5,
            Double rho23 = 0.40, Double rho12 = 0.50, string asset1 = null, string asset2 = null, string asset3=null, string asset4 = null)
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
                new RateMktData("1D", 0.035, "Spot", "None", fr007CurveName),
                new RateMktData("5Y", 0.035, "Spot", "None", fr007CurveName),
            };

            var dividendCurveName = "Dividend";
            var dividendRateDefinition = new[]
            {
                new RateMktData("1D", 0.0, "Spot", "None", dividendCurveName),
                new RateMktData("5Y", 0.0, "Spot", "None", dividendCurveName),
            };

            var curveDefinition = new[]
            {
                new InstrumentCurveDefinition(fr007CurveName, curveConvention, fr007RateDefinition, "SpotCurve"),
                new InstrumentCurveDefinition(dividendCurveName, curveConvention, dividendRateDefinition, "SpotCurve"),
            };

            var volSurf = new[] { new VolSurfMktData("VolSurf1", vol1), new VolSurfMktData("VolSurf2", vol2), new VolSurfMktData("VolSurf3", vol3), new VolSurfMktData("VolSurf4", vol4), };
            var corr = new[] { new CorrSurfMktData("Correlation12", rho12), new CorrSurfMktData("Correlation23", rho23), new CorrSurfMktData("Correlation13", rho13),
            new CorrSurfMktData("Correlation14", 0.0),
            new CorrSurfMktData("Correlation24", 0.0),
            new CorrSurfMktData("Correlation34", 0.0),};


            var marketInfo = new MarketInfo("tmpMarket", referenceDate, curveDefinition, historiclIndexRates, null, null, volSurf, corr);
            QdpMarket market;
            MarketFunctions.BuildMarket(marketInfo, out market);

            var impliedVol = market.GetData<VolSurfMktData>("VolSurf1").ToImpliedVolSurface(market.ReferenceDate);
            var impliedVol2 = market.GetData<VolSurfMktData>("VolSurf2").ToImpliedVolSurface(market.ReferenceDate);
            var impliedVol3 = market.GetData<VolSurfMktData>("VolSurf3").ToImpliedVolSurface(market.ReferenceDate);
            var impliedVol4 = market.GetData<VolSurfMktData>("VolSurf4").ToImpliedVolSurface(market.ReferenceDate);
            var corr12 = market.GetData<CorrSurfMktData>("Correlation12").ToImpliedVolSurface(market.ReferenceDate);
            var corr23 = market.GetData<CorrSurfMktData>("Correlation23").ToImpliedVolSurface(market.ReferenceDate);
            var corr13 = market.GetData<CorrSurfMktData>("Correlation13").ToImpliedVolSurface(market.ReferenceDate);
            var corr14 = market.GetData<CorrSurfMktData>("Correlation14").ToImpliedVolSurface(market.ReferenceDate);
            var corr24 = market.GetData<CorrSurfMktData>("Correlation24").ToImpliedVolSurface(market.ReferenceDate);
            var corr34 = market.GetData<CorrSurfMktData>("Correlation34").ToImpliedVolSurface(market.ReferenceDate);
            return new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>("Fr007").YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { asset1, market.GetData<CurveData>("Dividend").YieldCurve }, { asset2, market.GetData<CurveData>("Dividend").YieldCurve }, { asset3, market.GetData<CurveData>("Dividend").YieldCurve }, { asset4, market.GetData<CurveData>("Dividend").YieldCurve } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { asset1, impliedVol }, { asset2, impliedVol2 }, { asset3, impliedVol3 }, { asset4, impliedVol4 } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { asset1, spot1 }, { asset2, spot2 }, { asset3, spot3}, { asset4, spot4 } },
                x => x.Correlations.Value = new Dictionary<string, IVolSurface> { { asset1+asset2, corr12 } , { asset2+asset3, corr23 } , { asset1+asset3, corr13 },
                { asset1+asset4, corr14 } , { asset2+asset4, corr24 } , { asset3+asset4, corr34 } }
                );
        }

        private IMarketCondition GetMarket(String referenceDate = "2017-12-26", Double vol = 0.28, Double spot = 1.0)
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
                new RateMktData("1D", 0.035, "Spot", "None", fr007CurveName),
                new RateMktData("5Y", 0.035, "Spot", "None", fr007CurveName),
            };

            var dividendCurveName = "Dividend";
            var dividendRateDefinition = new[]
            {
                new RateMktData("1D", 0.0, "Spot", "None", dividendCurveName),
                new RateMktData("5Y", 0.0, "Spot", "None", dividendCurveName),
            };

            var curveDefinition = new[]
            {
                new InstrumentCurveDefinition(fr007CurveName, curveConvention, fr007RateDefinition, "SpotCurve"),
                new InstrumentCurveDefinition(dividendCurveName, curveConvention, dividendRateDefinition, "SpotCurve"),
            };

            var volSurf = new[] { new VolSurfMktData("VolSurf", vol), };

            var marketInfo = new MarketInfo("tmpMarket", referenceDate, curveDefinition, historiclIndexRates, null, null, volSurf);
            QdpMarket market;
            var result = MarketFunctions.BuildMarket(marketInfo, out market);
            var volsurf = market.GetData<VolSurfMktData>("VolSurf").ToImpliedVolSurface(market.ReferenceDate);


            return new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>("Fr007").YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>("Dividend").YieldCurve } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } }
                );
        }

        private Date DateFromStr(String dateStr)
        {
            var dt = Convert.ToDateTime(dateStr);
            return new Date(dt.Year, dt.Month, dt.Day);
        }
        private SpreadType ToSpreadType(String input)
        {
            return input.ToEnumType<SpreadType>();
        }
    }



}

