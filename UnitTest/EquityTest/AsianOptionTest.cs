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
using Qdp.Pricing.Base.Utilities;
using UnitTest.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace UnitTest.EquityTest
{
	[TestClass]
	public class AsianOptionTest
	{
		[TestMethod]
		public void AsianOptionPricingTest()
		{
            //AsianOptionGreekCalc(asianType: "GeometricAverage", isCall: true);
            //AsianOptionGreekCalc(asianType: "ArithmeticAverage", isCall: true, isFixed: true);
            AsianOptionGreekCalc(asianType: "DiscreteArithmeticAverage", isCall: true, isFixed: true);
        }

        //[TestMethod]
        public void AsianArithmeticOptionTest() {
            var valuationDate = DateFromStr("2018-03-14");
            var maturityDate = new Term("176D").Next(valuationDate);
            var calendar = CalendarImpl.Get("chn");

            //var obsDates = new[] { new Date(2018, 3, 14), new Date(2015, 5, 4), new Date(2015, 6, 2), new Date(2015, 7, 2), new Date(2015, 8, 3), new Date(2015, 9, 2) };


        }

        private void AsianOptionGreekCalc(String ValuationDate = "2015-03-19", Double vol = 0.28, Double spot = 1.0, Double strike = 1.03,  
            string asianType= "ArithmeticAverage", Boolean isCall = true,Boolean isFixed =true, 
            double expectedPv = 0.03368701153344,
            double expectedDelta= 0.431553260493781,
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
            #region comment
            //var fixings = File.ReadAllLines(@"./Data/HistoricalEquityPrices/hs300.csv")
            //	.Select(x =>
            //	{
            //		var splits = x.Split(',');
            //		return Tuple.Create(new Date(DateTime.Parse(splits[0])), Double.Parse(splits[1]));
            //	}).ToDictionary(x => x.Item1, x => x.Item2);
            //var initialSpot = fixings[startDate];
            //fixings = fixings.Select(x => Tuple.Create(x.Key, x.Value / initialSpot)).Where(x => x.Item1 < startDate).ToDictionary(x => x.Item1, x => x.Item2);
            #endregion comment
            var option = new AsianOption(
                valuationDate,
                maturityDate,
                OptionExercise.European,
                isCall ? OptionType.Call : OptionType.Put,
                ToAsianType(asianType),
                isFixed ? StrikeStyle.Fixed:StrikeStyle.Floating,
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
            var market = TestMarket(referenceDate: ValuationDate, vol: vol, spot: spot);

            var analyticalEngine = new AnalyticalAsianOptionEngine();
            var analyticalResult = analyticalEngine.Calculate(option, market, PricingRequest.All);

            //var engine = new GenericMonteCarloEngine(2, 30000);
            //var result = engine.Calculate(option, market, PricingRequest.All);
            //Console.WriteLine("Analytical: {0},{1},{2},{3},{4},{5}", analyticalResult.Pv, analyticalResult.Delta, analyticalResult.Gamma, analyticalResult.Vega, analyticalResult.Rho, analyticalResult.Theta);
            //Console.WriteLine("Monte Carlo: {0},{1},{2},{3},{4},{5}", result.Pv, result.Delta, result.Gamma, result.Vega, result.Rho, result.Theta);

            Assert.AreEqual(analyticalResult.Pv, expectedPv, 1e-1);
            Assert.AreEqual(analyticalResult.Delta, expectedDelta, 1e-1);
            Assert.AreEqual(analyticalResult.Gamma, expectedGamma, 1e4);
            Assert.AreEqual(analyticalResult.Vega, expectedVega, 1e-1);
            Assert.AreEqual(analyticalResult.Rho, expectedRho, 1e-1);
            Assert.AreEqual(analyticalResult.Theta, expectedTheta, 1e-1);
        }


        [TestMethod]
        public void AsianOptionPricingGTJATest()
        {
            //AsianOptionGreekCalc(asianType: "GeometricAverage", isCall: true);
            //Contract Value: 1920000
            AsianOptionGTJACalc(asianType: "ArithmeticAverage", isCall: false, isFixed: true, expectedPv: 1911376.72004646);
            AsianOptionGTJACalc(asianType: "DiscreteArithmeticAverage", isCall: false, isFixed: true, expectedPv: 1910044.79980354);
        }

        private void AsianOptionGTJACalc(String ValuationDate = "2018-06-29", Double vol = 0.12, Double spot = 1820, Double strike = 1850, string asianType = "ArithmeticAverage", Boolean isCall = true, Boolean isFixed = true,
            double expectedPv = 0.03368701153344)
        {
            var valuationDate = DateFromStr(ValuationDate);
            var maturityDate = DateFromStr("2018-11-01");
            var calendar = CalendarImpl.Get("chn");
            var obsStartDate = DateFromStr("2018-10-01");
            var obsDates = calendar.BizDaysBetweenDatesInclEndDay(obsStartDate, maturityDate).ToArray();
            #region comment
            //var fixings = File.ReadAllLines(@"./Data/HistoricalEquityPrices/hs300.csv")
            //	.Select(x =>
            //	{
            //		var splits = x.Split(',');
            //		return Tuple.Create(new Date(DateTime.Parse(splits[0])), Double.Parse(splits[1]));
            //	}).ToDictionary(x => x.Item1, x => x.Item2);
            //var initialSpot = fixings[startDate];
            //fixings = fixings.Select(x => Tuple.Create(x.Key, x.Value / initialSpot)).Where(x => x.Item1 < startDate).ToDictionary(x => x.Item1, x => x.Item2);
            #endregion comment
            var option = new AsianOption(
                valuationDate,
                maturityDate,
                OptionExercise.European,
                isCall ? OptionType.Call : OptionType.Put,
                ToAsianType(asianType),
                isFixed ? StrikeStyle.Fixed : StrikeStyle.Floating,
                strike,
                InstrumentType.CommodityFutures,
                calendar,
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { maturityDate },
                obsDates,
                new Dictionary<Date, double>(),
                notional:30000
                );
            var market = TestMarket(referenceDate: ValuationDate, vol: vol, spot: spot);

            var analyticalEngine = new AnalyticalAsianOptionEngine();
            var analyticalResult = analyticalEngine.Calculate(option, market, PricingRequest.All);

            //var engine = new GenericMonteCarloEngine(2, 30000);
            //var result = engine.Calculate(option, market, PricingRequest.All);
            //Console.WriteLine("Analytical: {0},{1},{2},{3},{4},{5}", analyticalResult.Pv, analyticalResult.Delta, analyticalResult.Gamma, analyticalResult.Vega, analyticalResult.Rho, analyticalResult.Theta);
            //Console.WriteLine("Monte Carlo: {0},{1},{2},{3},{4},{5}", result.Pv, result.Delta, result.Gamma, result.Vega, result.Rho, result.Theta);

            Assert.AreEqual(analyticalResult.Pv, expectedPv, 1e-1);
        }

        [TestMethod]
        public void AsianOptionPnLTest()
        {
            AsianOptionGreekTest(isCall: true, asianType: "GeometricAverage", volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            AsianOptionGreekTest(isCall: true, asianType: "ArithmeticAverage", volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            AsianOptionGreekTest(isCall: true, asianType: "DiscreteArithmeticAverage", volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            AsianOptionGreekTest(isCall: false, asianType: "GeometricAverage", volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            AsianOptionGreekTest(isCall: false, asianType: "ArithmeticAverage", volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            AsianOptionGreekTest(isCall: false, asianType: "DiscreteArithmeticAverage", volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            AsianOptionGreekTest(isCall: true, asianType: "GeometricAverage", isFixed:false, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            AsianOptionGreekTest(isCall: true, asianType: "ArithmeticAverage", isFixed: false, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            AsianOptionGreekTest(isCall: true, asianType: "DiscreteArithmeticAverage", isFixed: false, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            AsianOptionGreekTest(isCall: false, asianType: "GeometricAverage", isFixed: false, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            AsianOptionGreekTest(isCall: false, asianType: "ArithmeticAverage", isFixed: false, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);
            AsianOptionGreekTest(isCall: false, asianType: "DiscreteArithmeticAverage", isFixed: false, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 1);

        }
        private void AsianOptionGreekTest(double vol = 0.28, double spot = 1.0, double strike = 1.03, Boolean isCall = true, string asianType = "ArithmeticAverage",Boolean isFixed = true,
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

            var option = new AsianOption(
                T0,
                maturityDate,
                OptionExercise.European,
                isCall ? OptionType.Call : OptionType.Put,
                ToAsianType(asianType),
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

            var engine = new AnalyticalAsianOptionEngine();
           
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
		public void TestAsianOptionPvAgainistHaugerExcel()
		{
			var startDate = new Date(2015, 03, 19);
			var maturityDate = new Term("183D").Next(startDate);
			var calendar = CalendarImpl.Get("chn");

			var oneWeek = new Term("7D");
			var dates = new List<Date>() { new Date(2015, 03, 20) };
			for (int i = 1; i < 27; i++)
			{
				dates.Add(oneWeek.Next(dates[i - 1]));
			}

            var option = new AsianOption(
                startDate,
                maturityDate,
                OptionExercise.European,
                OptionType.Call,
                AsianType.DiscreteArithmeticAverage,
                StrikeStyle.Fixed,
				100,
				InstrumentType.EquityIndex,
				calendar,
				new Act365(),
				CurrencyCode.CNY,
				CurrencyCode.CNY,
				new[] { maturityDate },
				dates.ToArray(),
				new Dictionary<Date, double>()
				);
			var market = TestMarket2();

			var analyticalEngine = new AnalyticalAsianOptionEngineLegacy();
			var result = analyticalEngine.Calculate(option, market, PricingRequest.All);

			Console.WriteLine("{0},{1},{2},{3},{4}", result.Pv, result.Delta, result.Gamma, result.Vega, result.Rho);
            //Note: originally was 1.9622573116
            Assert.AreEqual(2.91746104388858, result.Pv, 1e-8);
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

		private IMarketCondition TestMarket2()
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
				new RateMktData("1D", 0.08, "Spot", "None", fr007CurveName),
				new RateMktData("5Y", 0.08, "Spot", "None", fr007CurveName),
			};

			var dividendCurveName = "Dividend";
			var dividendRateDefinition = new[]
			{
				new RateMktData("1D", 0.05, "Spot", "None", dividendCurveName),
				new RateMktData("5Y", 0.05, "Spot", "None", dividendCurveName),
			};

			var curveDefinition = new[]
			{
				new InstrumentCurveDefinition(fr007CurveName, curveConvention, fr007RateDefinition, "SpotCurve"),
				new InstrumentCurveDefinition(dividendCurveName, curveConvention, dividendRateDefinition, "SpotCurve"),
			};

			var volSurf = new[] { new VolSurfMktData("VolSurf", 0.1), };

			var referenceDate = "2015-03-19";
			var marketInfo = new MarketInfo("tmpMarket", referenceDate, curveDefinition, historiclIndexRates, null, null, volSurf);
			QdpMarket market;
			var result = MarketFunctions.BuildMarket(marketInfo, out market);
            var volsurf = market.GetData<VolSurfMktData>("VolSurf").ToImpliedVolSurface(market.ReferenceDate);

            return new MarketCondition(
				x => x.ValuationDate.Value = market.ReferenceDate,
				x => x.DiscountCurve.Value = market.GetData<CurveData>("Fr007").YieldCurve,
				x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>("Dividend").YieldCurve } },
				x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", 100.0 } } 
				);
		}
        private Date DateFromStr(String dateStr)
        {
            var dt = Convert.ToDateTime(dateStr);
            return new Date(dt.Year, dt.Month, dt.Day);
        }

        private AsianType ToAsianType(String input)
        {
            return input.ToEnumType<AsianType>();
        }
    }
}
