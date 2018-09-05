using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using UnitTest.Utilities;

namespace UnitTest.CommonTests
{
	[TestClass]
	public class CurveInterpolationTest
	{
		[TestMethod]
		public void TestCurveInterpolations()
		{
			var market = TestMarket(YieldCurveTrait.DiscountCurve.ToString(), Interpolation.LogLinear.ToString());
			var curve = market.GetData<CurveData>("Fr007").YieldCurve;

			market = TestMarket(YieldCurveTrait.SpotCurve.ToString(), Interpolation.CubicHermiteMonotic.ToString());
			var curve2 = market.GetData<CurveData>("Fr007").YieldCurve;

			market = TestMarket(YieldCurveTrait.ForwardCurve.ToString(), Interpolation.ForwardFlat.ToString());
			var curve3 = market.GetData<CurveData>("Fr007").YieldCurve;

			market = TestMarket(YieldCurveTrait.DiscountCurve.ToString(), Interpolation.LogCubicSpline.ToString());
			var curve5 = market.GetData<CurveData>("Fr007").YieldCurve;

			market = TestMarket(YieldCurveTrait.ForwardCurve.ToString(), Interpolation.ConvexMonotic.ToString());
			var curve6 = market.GetData<CurveData>("Fr007").YieldCurve;

			foreach (var date in curve.KeyPoints.Select(x => x.Item1))
			{
				Console.WriteLine("{0},{1},{2},{3},{4},{5}", date, curve.GetDf(date), curve2.GetDf(date), curve3.GetDf(date), curve5.GetDf(date), curve6.GetDf(date));
			}
		}

		private QdpMarket TestMarket(string trait, string interpolation)
		{
			var curveConvention = new CurveConvention("curveConvention",
				"CNY",
				"ModifiedFollowing",
				"Chn_ib",
				"Act365",
				"Continuous",
				interpolation);
			var curveDefinitions = new List<InstrumentCurveDefinition>();
			// 1 - discount curve
			var rates = new[]
			{
				new RateMktData("1D", 0.0189, "Fr001", "Deposit", "Fr007"),
				new RateMktData("7D", 0.024, "Fr001", "Deposit", "Fr007"),
				new RateMktData("3M", 0.0239, "Fr007", "InterestRateSwap", "Fr007"),
				new RateMktData("6M", 0.02385, "Fr007", "InterestRateSwap", "Fr007"),
				new RateMktData("9M", 0.02385, "Fr007", "InterestRateSwap", "Fr007"),
				new RateMktData("1Y", 0.0239, "Fr007", "InterestRateSwap", "Fr007"),
				new RateMktData("2Y", 0.02405, "Fr007", "InterestRateSwap", "Fr007"),
				new RateMktData("3Y", 0.02495, "Fr007", "InterestRateSwap", "Fr007"),
				new RateMktData("4Y", 0.0259, "Fr007", "InterestRateSwap", "Fr007"),
				new RateMktData("5Y", 0.0267, "Fr007", "InterestRateSwap", "Fr007"),
				new RateMktData("7Y", 0.0283, "Fr007", "InterestRateSwap", "Fr007"),
				new RateMktData("10Y", 0.0297, "Fr007", "InterestRateSwap", "Fr007"),
				new RateMktData("15Y", 0.032, "Fr007", "InterestRateSwap", "Fr007")
			};

			var fr007Curve = new InstrumentCurveDefinition(
				"Fr007",
				curveConvention,
				rates,
				trait);
			curveDefinitions.Add(fr007Curve);


			var marketInfo = new MarketInfo("TestMarket")
			{
				ReferenceDate = "2016-04-28",
				YieldCurveDefinitions = curveDefinitions.ToArray(),
				HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates
			};

			QdpMarket market;
			MarketFunctions.BuildMarket(marketInfo, out market);
			return market;
		}
	}
}
