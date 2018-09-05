using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Ecosystem.Utilities;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using UnitTest.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace UnitTest.Commodity
{
    [TestClass]
    public class CommodityResetStrikeOptionTest
    {
        [TestMethod]
        public void ResetStrikeOptionGreekTest()
        {
            ResetStrikeOptionGreekCalc(expectedPv: 0.0914869278811309, expectedDelta: 0.297065194728718, expectedGamma: 2.8691667985913938, expectedVega: 0.0032974193162242649, expectedTheta: -0.0003288089958675966, expectedRho: -4.4113181118243805E-06, isCall: true, isPercentage: true);
            ResetStrikeOptionGreekCalc(expectedPv: 0.0902671667774961, expectedDelta: 0.346930785117827, expectedGamma: 2.6853683499084, expectedVega: 0.0031606938755652236, expectedTheta: -0.00031386632486146704, expectedRho: -4.3525036519387328E-06, isCall: true, isPercentage: false);
            ResetStrikeOptionGreekCalc(expectedPv: 0.0911462114910516, expectedDelta: -0.35645869040867861, expectedGamma: 2.7944627500886554, expectedVega: 0.003105061329403358, expectedTheta: -0.00025830937133936027, expectedRho: -4.3948894435930086E-06, isCall: false, isPercentage: true);
            ResetStrikeOptionGreekCalc(expectedPv: 0.0956899281603423, expectedDelta: -0.341878569504739, expectedGamma: 3.10274112791387, expectedVega: 0.00335835880022006, expectedTheta: -0.000292522193550165, expectedRho: -4.61397844458977E-06, isCall: false, isPercentage: false);

        }



        private void ResetStrikeOptionGreekCalc(double expectedPv,double expectedDelta,double expectedGamma, double expectedVega, double expectedTheta, double expectedRho,
            String ValuationDate = "2017-12-20", Double vol = 0.28, Double spot = 1.0, Double strike = 1.03, Boolean isCall = true, Boolean isPercentage = true)
        {
            var valuationDate = DateFromStr(ValuationDate);
            var strikefixingDate = new Term("50D").Next(valuationDate);
            var exerciseDate = new Term("176D").Next(valuationDate);
            var calendar = CalendarImpl.Get("chn");
            

            var option = new ResetStrikeOption(
                valuationDate,
                exerciseDate,
                OptionExercise.European,
                isCall ? OptionType.Call : OptionType.Put,
                isPercentage ? ResetStrikeType.PercentagePayoff : ResetStrikeType.NormalPayoff,
                strike,
                InstrumentType.EquityIndex,
                calendar,
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { exerciseDate },
                new[] { exerciseDate },
                strikefixingDate
                );
            var market = TestMarket(referenceDate: ValuationDate, vol: vol, spot: spot);

            var analyticalEngine = new AnalyticalResetStrikeOptionEngine();
            var analyticalResult = analyticalEngine.Calculate(option, market, PricingRequest.All);

            Assert.AreEqual(expectedPv, analyticalResult.Pv, 1e-8);
            Assert.AreEqual(expectedDelta, analyticalResult.Delta, 1e-8);
            Assert.AreEqual(expectedGamma, analyticalResult.Gamma, 1e-8);
            Assert.AreEqual(expectedVega, analyticalResult.Vega, 1e-8);
            Assert.AreEqual(expectedTheta, analyticalResult.Theta, 1e-8);
            Assert.AreEqual(expectedRho, analyticalResult.Rho, 1e-8);

            
        }

        [TestMethod]
        public void ResetStrikeOptionPnLTest()
        {
            ResetStrikeOptionPnLCalc(isCall: true, isPercentage:true, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            ResetStrikeOptionPnLCalc(isCall: true, isPercentage: false, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            ResetStrikeOptionPnLCalc(isCall: false, isPercentage: true, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            ResetStrikeOptionPnLCalc(isCall: false, isPercentage: false, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);

        }
        private void ResetStrikeOptionPnLCalc(double vol = 0.28, double spot = 1.0, double strike = 1.03, Boolean isCall = true,  Boolean isPercentage = true,
            string t0 = "2017-12-20", string t1 = "2017-12-21", double volMove = 0.10, double mktMove = 1e-4, double toleranceInPct = 2)
        {
            var T0 = DateFromStr(t0);
            var T1 = DateFromStr(t1);
            var spotNew = spot + spot * mktMove;
            var volNew = vol + volMove;

            var strikefixingDate = new Term("50D").Next(T0);
            var exerciseDate = new Term("176D").Next(T0);
            var calendar = CalendarImpl.Get("chn");
           
            var valuationDay = t0;
            var valuationDayNew = t1;

            var option = new ResetStrikeOption(
                T0,
                exerciseDate,
                OptionExercise.European,
                isCall ? OptionType.Call : OptionType.Put,
                isPercentage ? ResetStrikeType.PercentagePayoff : ResetStrikeType.NormalPayoff,
                strike,
                InstrumentType.EquityIndex,
                calendar,
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { exerciseDate },
                new[] { exerciseDate },
                strikefixingDate
                );
            var market = TestMarket(referenceDate: t0, vol: vol, spot: spot);
            var marketNew = TestMarket(referenceDate: t1, vol: volNew, spot: spotNew);
            var marketPI = TestMarket(referenceDate: t0, vol: vol, spot: spotNew);
            var marketVI = TestMarket(referenceDate: t0, vol: volNew, spot: spot);
            var marketPVC = TestMarket(referenceDate: t0, vol: volNew, spot: spotNew);

            var engine = new AnalyticalResetStrikeOptionEngine();

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


        //Parity: 
        //For Call: strikefixingdate = valuationdate; S>X; For Put: strikefixingdate = valuationdate; S<X;
        //Type1 should be a vanilla european option * strike; Type2 should be a vanilla european option.
        [TestMethod]
        public void ResetStrikeOptionParityTest()
        {
            ResetStrikeOptionParityCalc(spot: 2.0, strike: 1.5, isCall: true, isPercentage: true);
            ResetStrikeOptionParityCalc(spot: 2.0, strike: 1.5, isCall: true, isPercentage: false);
            ResetStrikeOptionParityCalc(spot: 1.0, strike: 1.5, isCall: false, isPercentage: true);
            ResetStrikeOptionParityCalc(spot: 1.0, strike: 1.5, isCall: false, isPercentage: false);
        }

        private void ResetStrikeOptionParityCalc(String ValuationDate = "2017-12-20", Double vol = 0.28, Double spot = 2.0, Double strike = 1.03, Boolean isCall = true, Boolean isPercentage = true)
        {
            var valuationDate = DateFromStr(ValuationDate);
            var strikefixingDate = valuationDate;
            var exerciseDate = new Term("176D").Next(valuationDate);
            var calendar = CalendarImpl.Get("chn");


            var option1 = new ResetStrikeOption(
                valuationDate,
                exerciseDate,
                OptionExercise.European,
                isCall ? OptionType.Call : OptionType.Put,
                isPercentage ? ResetStrikeType.PercentagePayoff : ResetStrikeType.NormalPayoff,
                strike,
                InstrumentType.EquityIndex,
                calendar,
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { exerciseDate },
                new[] { exerciseDate },
                strikefixingDate
                );

            var option2 = new VanillaOption(
                valuationDate,
                exerciseDate,
                OptionExercise.European,
                isCall ? OptionType.Call : OptionType.Put,
                strike,
                InstrumentType.EquityIndex,
                calendar,
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { exerciseDate },
                new[] { exerciseDate }
                );
            var market = TestMarket(referenceDate: ValuationDate, vol: vol, spot: spot);

            var engine = new AnalyticalResetStrikeOptionEngine();
            var result = engine.Calculate(option1, market, PricingRequest.All);
            var engineVanilla = new AnalyticalVanillaEuropeanOptionEngine();
            var resultVanilla = engineVanilla.Calculate(option2, market, PricingRequest.All);

            var result1 = (isPercentage) ? result.Pv * strike : result.Pv; 

            Assert.AreEqual(result1, resultVanilla.Pv, 1e-8);
            
        }


        private IMarketCondition TestMarket(String referenceDate = "2017-12-20", Double vol = 0.28, Double spot = 1.0)
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

        
    }
}

