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
    public class CommodityRainbowOptionTest
    {
        [TestMethod]
        public void CommodityRainbowOptionPricingTest()
        {
            RainbowOptionGreekCalc(0.98730010167128279, isCall: true, rainbowType:"Max");
            RainbowOptionGreekCalc(0.071702857713402846, isCall: true, rainbowType: "Min");
            RainbowOptionGreekCalc(4.1559335493879246E-05, isCall: false, rainbowType: "Max");
            RainbowOptionGreekCalc(0.08448709735013793, isCall: false, rainbowType: "Min");
            RainbowOptionGreekCalc(2.0000629503217557, isCall: true, rainbowType: "BestOfAssetsOrCash");
            RainbowOptionGreekCalc(0.98249363414213031, isCall: true, rainbowType: "BestCashOrNothing");
            RainbowOptionGreekCalc(0.00077126746027060848, isCall: false, rainbowType: "BestCashOrNothing");
            RainbowOptionGreekCalc(0.50458130870235574, isCall: true, rainbowType: "WorstCashOrNothing");
            RainbowOptionGreekCalc(0.47952212269262523, isCall: false, rainbowType: "WorstCashOrNothing");
            RainbowOptionGreekCalc(0.42817216548364284, isCall: true, rainbowType: "TwoAssetsCashOrNothing");
            RainbowOptionGreekCalc(0.0010337002754387181, isCall: false, rainbowType: "TwoAssetsCashOrNothing");
            RainbowOptionGreekCalc(1.8810333422731788E-05, isCall: false, rainbowType: "TwoAssetsCashOrNothingUpDown");
            RainbowOptionGreekCalc(0.55404022550989651, isCall: false, rainbowType: "TwoAssetsCashOrNothingDownUp");
        }

        private void RainbowOptionGreekCalc(double ExpectedPv, string rainbowType = "Max",string ValuationDate = "2017-12-18", double vol = 0.28, double spot = 1.0, double volNew = 0.30, double spotNew = 2.0,
            double strike1 = 1.03,double strike2=1.05,  Boolean isCall = true)
        {
            var valuationDate = DateFromStr(ValuationDate);
            var maturityDate = new Term("176D").Next(valuationDate);
            var calendar = CalendarImpl.Get("chn");

            var asset1 = "asset1";
            var asset2 = "asset2";

            var option = new RainbowOption(
                startDate: valuationDate,
                maturityDate: maturityDate,
                exercise: OptionExercise.European,
                optionType: isCall ? OptionType.Call : OptionType.Put,
                rainbowType:ToRainbowType(rainbowType),
                strikes: new double[] { strike1,strike2 },
                cashAmount:1.0,
                underlyingInstrumentType: InstrumentType.EquityIndex,
                calendar: calendar,
                dayCount: new Act365(),
                payoffCcy: CurrencyCode.CNY,
                settlementCcy: CurrencyCode.CNY,
                exerciseDates: new[] { maturityDate },
                observationDates: new[] { maturityDate },
                underlyingTickers: new[] { asset1, asset2 }
                )
            {
                UnderlyingTickers = new string[] { asset1, asset2 }
            };

            var market = TestMarket(referenceDate: ValuationDate, vol: vol, volNew: volNew, spot: spot, spotNew: spotNew, asset1: asset1, asset2: asset2);

            var analyticalEngine = new AnalyticalRainbowOptionEngine();
            var analyticalResult = analyticalEngine.Calculate(option, market, PricingRequest.All);

            Assert.AreEqual(ExpectedPv, analyticalResult.Pv,1e-8);
        }


        [TestMethod]
        public void CommodityRainbowOptionPnLTest()
        {
            RainbowOptionGreekTest(isCall: true, isBest: true, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 3);
            RainbowOptionGreekTest(isCall: true, isBest: false, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 3);
            RainbowOptionGreekTest(isCall: false, isBest: true, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 3);
            RainbowOptionGreekTest(isCall: false, isBest: false, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 3);

        }
        private void RainbowOptionGreekTest(double vol = 0.28, double spot = 1.0, double volNew = 0.30, double spotNew = 2.0,
            double strike1 = 1.03, double strike2 = 1.05, Boolean isCall = true, Boolean isBest = true, 
            string t0 = "2015-03-19", string t1 = "2015-03-20", double volMove = 0.10, double mktMove = 1e-4, double toleranceInPct = 2)
        {
            var valuationDate = DateFromStr(t0);
            var maturityDate = new Term("176D").Next(valuationDate);
            var calendar = CalendarImpl.Get("chn");
            var spot2 = spot + spot * mktMove;
            var spotNew2 = spotNew + spotNew * mktMove;
            var vol2 = vol + volMove;
            var volNew2 = volNew + volMove;
            var T0 = DateFromStr(t0);
            var T1 = DateFromStr(t1);


            var valuationDay = t0;
            var valuationDayNew = t1;

            var asset1 = "asset1";
            var asset2 = "asset2";
            var option = new RainbowOption(
                startDate: valuationDate,
                maturityDate: maturityDate,
                exercise: OptionExercise.European,
                optionType: isCall ? OptionType.Call : OptionType.Put,
                rainbowType: isBest ? RainbowType.Max : RainbowType.Min,
                strikes: new double[] { strike1,strike2 },
                cashAmount: 1.0,
                underlyingInstrumentType: InstrumentType.EquityIndex,
                calendar: calendar,
                dayCount: new Act365(),
                payoffCcy: CurrencyCode.CNY,
                settlementCcy: CurrencyCode.CNY,
                exerciseDates: new[] { maturityDate },
                observationDates: new[] { maturityDate },
                underlyingTickers: new[] { asset1, asset2 }
                )
            {
                UnderlyingTickers = new string[] { asset1, asset2 }
            };
            var market = TestMarket(referenceDate: t0, vol: vol, volNew: volNew, spot: spot, spotNew: spotNew, asset1: asset1, asset2: asset2 );
            var marketNew = TestMarket(referenceDate: t1, vol: vol2, volNew: volNew2, spot: spot2, spotNew: spotNew2, asset1: asset1, asset2: asset2);
            var marketPI = TestMarket(referenceDate: t0, vol: vol, volNew: volNew, spot: spot2, spotNew: spotNew2, asset1: asset1, asset2: asset2);
            var marketVI = TestMarket(referenceDate: t0, vol: vol2, volNew: volNew2, spot: spot, spotNew: spotNew, asset1: asset1, asset2: asset2);
            var marketPVC = TestMarket(referenceDate: t0, vol: vol2, volNew: volNew2, spot: spot2, spotNew: spotNew2, asset1: asset1, asset2: asset2);

            var engine = new AnalyticalRainbowOptionEngine();

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


        [TestMethod]
        public void CommodityRainbowOptionParityTest()
        {
            RainbowOptionParity(vol: 0.5, spot: 130.0, volNew: 0.30, spotNew: 145.0, strike1: 140.0, isCall: true, isBest: true);
            RainbowOptionParity(isCall: true, isBest: false);

        }



        private void RainbowOptionParity( string ValuationDate = "2017-12-18", double vol = 0.28, double spot = 1.0, double volNew = 0.30, double spotNew = 2.0,
            double strike1 = 1.03, double strike2 = 1.05, Boolean isCall = true, Boolean isBest = true)
        {
            var valuationDate = DateFromStr(ValuationDate);
            var maturityDate = new Term("176D").Next(valuationDate);
            var calendar = CalendarImpl.Get("chn");

            var asset1 = "asset1";
            var asset2 = "asset2";

            var option1 = new RainbowOption(
                startDate: valuationDate,
                maturityDate: maturityDate,
                exercise: OptionExercise.European,
                optionType: isCall ? OptionType.Call : OptionType.Put,
                rainbowType: isBest ? RainbowType.Max : RainbowType.Min,
                strikes:new double[] { strike1,strike2 },
                cashAmount:1.0,
                underlyingInstrumentType: InstrumentType.EquityIndex,
                calendar: calendar,
                dayCount: new Act365(),
                payoffCcy: CurrencyCode.CNY,
                settlementCcy: CurrencyCode.CNY,
                exerciseDates: new[] { maturityDate },
                observationDates: new[] { maturityDate },
                underlyingTickers: new[] { asset1, asset2 }
                )
            {
                UnderlyingTickers = new string[] { asset1, asset2 }
            };


            var option2 = new RainbowOption(
               startDate: valuationDate,
                maturityDate: maturityDate,
                exercise: OptionExercise.European,
                optionType: isCall ? OptionType.Put : OptionType.Call,
                rainbowType: isBest ? RainbowType.Max : RainbowType.Min,
                strikes: new double[] { strike1,strike2 },
                cashAmount: 1.0,
                underlyingInstrumentType: InstrumentType.EquityIndex,
                calendar: calendar,
                dayCount: new Act365(),
                payoffCcy: CurrencyCode.CNY,
                settlementCcy: CurrencyCode.CNY,
                exerciseDates: new[] { maturityDate },
                observationDates: new[] { maturityDate },
                underlyingTickers: new[] { asset1, asset2 }
                )
            {
                UnderlyingTickers = new string[] { asset1, asset2 }
            };


            var option = new RainbowOption(
                startDate: valuationDate,
                maturityDate: maturityDate,
                exercise: OptionExercise.European,
                optionType: isCall ? OptionType.Call : OptionType.Put,
                rainbowType: isBest ? RainbowType.Max : RainbowType.Min,
                strikes: new double[] { 1e-8 ,0},
                cashAmount: 1.0,
                underlyingInstrumentType: InstrumentType.EquityIndex,
                calendar: calendar,
                dayCount: new Act365(),
                payoffCcy: CurrencyCode.CNY,
                settlementCcy: CurrencyCode.CNY,
                exerciseDates: new[] { maturityDate },
                observationDates: new[] { maturityDate },
                underlyingTickers: new[] { asset1, asset2 }
                )
            {
                UnderlyingTickers = new string[] { asset1, asset2 }
            };
            var market = TestMarket(referenceDate: ValuationDate, vol: vol, volNew: volNew, spot: spot, spotNew: spotNew, asset1: asset1, asset2: asset2);

            var analyticalEngine = new AnalyticalRainbowOptionEngine();
            var result1 = analyticalEngine.Calculate(option1, market, PricingRequest.All);
            var result2 = analyticalEngine.Calculate(option2, market, PricingRequest.All);
            var result = analyticalEngine.Calculate(option, market, PricingRequest.All);
            var r = market.DiscountCurve.Value.ZeroRate(market.ValuationDate, maturityDate);
            var T= option1.DayCount.CalcDayCountFraction(market.ValuationDate, maturityDate);

            Assert.AreEqual(0.0, (result1.Pv + strike1 * Math.Exp(-r * T) - result2.Pv - result.Pv),1.0e-6);

        }


  

        private IMarketCondition TestMarket(String referenceDate = "2015-03-19", 
            Double vol = 0.28, Double volNew = 0.30, Double spot = 1.0, Double spotNew = 2.0, Double rho = 0.5,
            string asset1 = null, string asset2 = null)
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

            var volSurf = new[] { new VolSurfMktData("VolSurf", vol), new VolSurfMktData("VolSurfNew", volNew), };
            var corr = new[] { new CorrSurfMktData("Correlation", rho), };


            var marketInfo = new MarketInfo("tmpMarket", referenceDate, curveDefinition, historiclIndexRates, null, null, volSurf, corr);
            QdpMarket market;
            MarketFunctions.BuildMarket(marketInfo, out market);

            var impliedVol = market.GetData<VolSurfMktData>("VolSurf").ToImpliedVolSurface(market.ReferenceDate);
            var impliedVol2 = market.GetData<VolSurfMktData>("VolSurfNew").ToImpliedVolSurface(market.ReferenceDate);
            var corre = market.GetData<CorrSurfMktData>("Correlation").ToImpliedVolSurface(market.ReferenceDate);

            return new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>("Fr007").YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { asset1, market.GetData<CurveData>("Dividend").YieldCurve }, { asset2, market.GetData<CurveData>("Dividend").YieldCurve } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { asset1, impliedVol }, { asset2, impliedVol2 } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { asset1, spot }, { asset2, spotNew } },
                x => x.Correlations.Value =new Dictionary<string, IVolSurface> { { asset1, corre } }
                );
        }

        private Date DateFromStr(String dateStr)
        {
            var dt = Convert.ToDateTime(dateStr);
            return new Date(dt.Year, dt.Month, dt.Day);
        }
        private RainbowType ToRainbowType(String input)
        {
            return input.ToEnumType<RainbowType>();
        }
    }


   
}
