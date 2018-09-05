using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Equity.Engines.Numerical;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Ecosystem.Utilities;
using UnitTest.Utilities;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using System.Collections.Generic;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace UnitTest.EquityTest
{
	[TestClass]
	public class AmericanOptionTest
	{
        [TestMethod] //TODO: fix this one
        public void TestFdAmericanOptionEngine()
		{
            var startDate = new Date(2010, 08, 31);
            var endDate = new Date(2016, 08, 31);
            var calendar = CalendarImpl.Get("chn");

            var option = new VanillaOption(startDate,
				endDate,
				OptionExercise.American,
				OptionType.Call,
				3.53,
				InstrumentType.Stock,
                calendar,
				new Act365(),
				CurrencyCode.CNY, 
				CurrencyCode.CNY, 
				calendar.BizDaysBetweenDatesInclEndDay(startDate, endDate).ToArray(),
				null,
				1.0
				);

            var engine = new FdAmericanOptionEngine(100);
			var result = engine.Calculate(option, TestMarket(), PricingRequest.All);

            Assert.AreEqual(result.Pv, 0.4459260143, 1e-8);
			Assert.AreEqual(result.Delta, 0.71918151, 1e-8);
			Assert.AreEqual(result.Gamma, 0.487730202, 1e-8);
			Assert.AreEqual(result.Vega, 0.0177395037, 1e-8);
			Assert.AreEqual(result.Theta, -0.00045427, 1e-8);
		}

		[TestMethod]
		public void BinomialAmericanOptionEngineTest()
		{
			
		}

		private IMarketCondition TestMarket()
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
				new RateMktData("1D", 0.06, "Spot", "None", fr007CurveName),
				new RateMktData("5Y", 0.06, "Spot", "None", fr007CurveName),
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

			var volSurf = new[] { new VolSurfMktData("VolSurf", 0.131804),  };
			var marketInfo = new MarketInfo("tmpMarket", "2014-02-10", curveDefinition, historiclIndexRates, null, null, volSurf);
			QdpMarket market;
			var result = MarketFunctions.BuildMarket(marketInfo, out market);

			var valuationDate = new Date(2014, 3, 26);
            var volsurf = market.GetData<VolSurfMktData>("VolSurf").ToImpliedVolSurface(valuationDate);

            return new MarketCondition(
				x => x.ValuationDate.Value = valuationDate,
				x => x.DiscountCurve.Value = market.GetData<CurveData>("Fr007").YieldCurve,
				x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>("Dividend").YieldCurve } },
				x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { {"", 3.36 } }
				);
		}
	}
}
