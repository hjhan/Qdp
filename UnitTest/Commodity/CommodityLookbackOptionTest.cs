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

namespace UnitTest.EquityTest
{
    [TestClass]
    public class CommodityLookbackOptionTest
    {
        [TestMethod]
        public void CommodityLookbackOptionPricingTest()
        {
            CommodityLookbackOptionGreekCalc(isCall: true, isFixed: false, expectedPv: 36.7835306916571, expectedDelta: 0.153264711215684, expectedGamma: 0, expectedVega: 1.16248873749105, expectedTheta: -0.102804302070055, expectedRho: 0.0051218652275935);
            CommodityLookbackOptionGreekCalc(isCall: false, isFixed: false, expectedPv: 37.265501525508, expectedDelta: 0.155272923021954, expectedGamma: 0, expectedVega: 1.48380262672494, expectedTheta: -0.105519859720197, expectedRho: -0.00636245792076551);
        }


        private void CommodityLookbackOptionGreekCalc(String ValuationDate = "2015-03-19", Double vol = 0.28, Double spot = 240, Double strike = 240,
            Boolean isCall = true, Boolean isFixed = true,
            double expectedPv = 0.03368701153344,
            double expectedDelta = 0.431553260493781,
            double expectedGamma = 4319.00095926793,
            double expectedVega = 0.00146247323594882,
            double expectedRho = -1.62432084616776E-06,
            double expectedTheta = -0.000398443365606245
            )
        {
            var valuationDate = DateFromStr(ValuationDate);
            var maturityDate = new Term("176D").Next(valuationDate);
            var calendar = CalendarImpl.Get("chn");
            var obsDates = new[] { new Date(2015, 4, 2), new Date(2015, 5, 4), new Date(2015, 6, 2), new Date(2015, 7, 2), new Date(2015, 8, 3), new Date(2015, 9, 2) };

            var option = new LookbackOption(
                valuationDate,
                maturityDate,
                OptionExercise.European,
                isCall ? OptionType.Call : OptionType.Put,
                isFixed ? StrikeStyle.Fixed : StrikeStyle.Floating,
                strike,
                InstrumentType.EquityIndex,
                calendar,
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { maturityDate },
                obsDates,
                new Dictionary<Date, double>(),
                notional:1
                );
            var market = TestMarket(referenceDate: ValuationDate, vol: vol, spot: spot);

            var analyticalEngine = new AnalyticalLookbackOptionEngine();
            var analyticalResult = analyticalEngine.Calculate(option, market, PricingRequest.All);

            Assert.AreEqual(expectedPv, analyticalResult.Pv, 1e-8);
            Assert.AreEqual(expectedDelta, analyticalResult.Delta,  1e-8);
            Assert.AreEqual(expectedGamma, analyticalResult.Gamma,  1e-8);
            Assert.AreEqual(expectedVega, analyticalResult.Vega,  1e-8);
            Assert.AreEqual(expectedRho, analyticalResult.Rho,  1e-8);
            Assert.AreEqual(expectedTheta, analyticalResult.Theta,  1e-8);
        }

        [TestMethod]
        public void CommodityLookbackOptionPnLTest()
        {
            CommodityLookbackOptionPnLCalc(isCall: true, isFixed: false, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            CommodityLookbackOptionPnLCalc(isCall: false,  isFixed: false, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);

        }
        private void CommodityLookbackOptionPnLCalc(double vol = 0.28, double spot = 240, double strike = 240, Boolean isCall = true,  Boolean isFixed = true,
            string t0 = "2015-03-19", string t1 = "2015-03-20", double volMove = 0.10, double mktMove = 1e-4, double toleranceInPct = 2)
        {
            var T0 = DateFromStr(t0);
            var T1 = DateFromStr(t1);
            var spotNew = spot + spot * mktMove;
            var volNew = vol + volMove;

            var maturityDate = new Term("176D").Next(T0);
            var calendar = CalendarImpl.Get("chn");
            var obsDates = new[] {
                new Date(2015, 4, 2), new Date(2015, 5, 4), new Date(2015, 6, 2), new Date(2015, 7, 2), new Date(2015, 8, 3), new Date(2015, 9, 2) };

            var valuationDay = t0;
            var valuationDayNew = t1;

            var option = new LookbackOption(
                T0,
                maturityDate,
                OptionExercise.European,
                isCall ? OptionType.Call : OptionType.Put,
                isFixed ? StrikeStyle.Fixed : StrikeStyle.Floating,
                strike,
                InstrumentType.EquityIndex,
                calendar,
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { maturityDate },
                obsDates,
                new Dictionary<Date, double>()
                );
            var market = TestMarket(referenceDate: t0, vol: vol, spot: spot);
            var marketNew = TestMarket(referenceDate: t1, vol: volNew, spot: spotNew);
            var marketPI = TestMarket(referenceDate: t0, vol: vol, spot: spotNew);
            var marketVI = TestMarket(referenceDate: t0, vol: volNew, spot: spot);
            var marketPVC = TestMarket(referenceDate: t0, vol: volNew, spot: spotNew);

            var engine = new AnalyticalLookbackOptionEngine();

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
        public void CommodityLookbackVanillaOptionTest()
        {
            CommodityLookbackVanillaComparison(isCall: true, isFixed: false);
            CommodityLookbackVanillaComparison(isCall: false, isFixed: false);
        }


        private void CommodityLookbackVanillaComparison(String ValuationDate = "2015-03-19", Double vol = 0.28, Double spot = 240, Double strike = 240,
            Boolean isCall = true, Boolean isFixed = true)
        {
            var valuationDate = DateFromStr(ValuationDate);
            var maturityDate = new Term("176D").Next(valuationDate);
            var calendar = CalendarImpl.Get("chn");
            var obsDates = new[] { new Date(2015, 4, 2), new Date(2015, 5, 4), new Date(2015, 6, 2), new Date(2015, 7, 2), new Date(2015, 8, 3), new Date(2015, 9, 2) };

            var option = new LookbackOption(
                valuationDate,
                maturityDate,
                OptionExercise.European,
                isCall ? OptionType.Call : OptionType.Put,
                isFixed ? StrikeStyle.Fixed : StrikeStyle.Floating,
                strike,
                InstrumentType.EquityIndex,
                calendar,
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { maturityDate },
                obsDates,
                new Dictionary<Date, double>(),
                notional: 1
                );

            var vanilla = new VanillaOption(
                valuationDate,
                maturityDate,
                OptionExercise.European,
                isCall ? OptionType.Call : OptionType.Put,
                strike,
                InstrumentType.EquityIndex,
                calendar,
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { maturityDate },
                obsDates,
                notional: 1
                );
            var market1 = TestMarket(referenceDate: ValuationDate, vol: vol, spot: spot);
            var market2 = TestMarket(referenceDate: ValuationDate, vol: vol * 0.8, spot: spot);

            var analyticalEngine = new AnalyticalLookbackOptionEngine();
            var vanillaEngine = new AnalyticalVanillaEuropeanOptionEngine();
            var analyticalResult = analyticalEngine.Calculate(option, market1, PricingRequest.All);
            var vanillaResult = vanillaEngine.Calculate(vanilla, market1, PricingRequest.All);
            var volResult = analyticalEngine.Calculate(option, market2, PricingRequest.All);

            //Floating Lookback Option >= ATM vanilla 
            //High volatility Floating >= Low volatility Floating 
            Assert.AreEqual(analyticalResult.Pv >= vanillaResult.Pv, true);
            Assert.AreEqual(analyticalResult.Pv >= volResult.Pv, true);

        }


        private IMarketCondition TestMarket(String referenceDate = "2015-03-19", Double vol = 0.28, Double spot = 1.0)
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

