using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using UnitTest.Utilities;

namespace UnitTest.CommonTests
{
	[TestClass]
	public class ForwardFlatCurveTest
	{
		[TestMethod]
		public void TestForwardFlatCurve()
		{
			var mkt = VerifyCurveConstructionMarket("2015-10-23");
			var fr007ForwardFlat = mkt.GetData<CurveData>("Fr007ForwardFlat").YieldCurve;
			var fr007Cubic = mkt.GetData<CurveData>("Fr007Cubic").YieldCurve;

			for (var t = 0.0; t < 20.0; t += 0.1)
			{
				Console.WriteLine("{0},{1},{2},{3},{4}", t, fr007ForwardFlat.GetSpotRate(t), fr007Cubic.GetSpotRate(t), fr007ForwardFlat.GetForwardRate(t, 0.25), fr007Cubic.GetForwardRate(t, 0.25));
			}
		}

		public QdpMarket VerifyCurveConstructionMarket(string referenceDate)
		{
			var curveConvention = new CurveConvention("curveConvention",
					"CNY",
					"ModifiedFollowing",
					"Chn_ib",
					"Act365",
					"Continuous",
					"ForwardFlat");
			var curveDefinitions = new List<InstrumentCurveDefinition>();
			var name = "Fr007ForwardFlat";
			// 1 - discount curve
			var rates = new[]
			{
				new RateMktData("1D", 0.0189, "Fr001", "Deposit", name),
				new RateMktData("7D", 0.024, "Fr001", "Deposit", name),
				new RateMktData("3M", 0.0239, "Fr007", "InterestRateSwap",name),
				new RateMktData("6M", 0.02385, "Fr007", "InterestRateSwap",name),
				new RateMktData("9M", 0.02385, "Fr007", "InterestRateSwap",name),
				new RateMktData("1Y", 0.0239, "Fr007", "InterestRateSwap",name),
				new RateMktData("2Y", 0.02405, "Fr007", "InterestRateSwap",name),
				new RateMktData("3Y", 0.02495, "Fr007", "InterestRateSwap",name),
				new RateMktData("4Y", 0.0259, "Fr007", "InterestRateSwap",name),
				new RateMktData("5Y", 0.0267, "Fr007", "InterestRateSwap",name),
				new RateMktData("7Y", 0.0283, "Fr007", "InterestRateSwap",name),
				new RateMktData("10Y", 0.0297, "Fr007", "InterestRateSwap",name),
				new RateMktData("15Y", 0.032, "Fr007", "InterestRateSwap",name)
			};

			var fr007Curve = new InstrumentCurveDefinition(
				name,
				curveConvention,
				rates,
				"ForwardCurve");
			curveDefinitions.Add(fr007Curve);

			var curveConvention2 = new CurveConvention("curveConventionCubic",
					"CNY",
					"ModifiedFollowing",
					"Chn_ib",
					"Act365",
					"Continuous",
					"CubicHermiteMonotic");
			
			var name2 = "Fr007Cubic";
			// 1 - discount curve
			var rates2 = new[]
			{
				new RateMktData("1D", 0.0189, "Fr001", "Deposit", name),
				new RateMktData("7D", 0.024, "Fr001", "Deposit", name),
				new RateMktData("3M", 0.0239, "Fr007", "InterestRateSwap",name),
				new RateMktData("6M", 0.02385, "Fr007", "InterestRateSwap",name),
				new RateMktData("9M", 0.02385, "Fr007", "InterestRateSwap",name),
				new RateMktData("1Y", 0.0239, "Fr007", "InterestRateSwap",name),
				new RateMktData("2Y", 0.02405, "Fr007", "InterestRateSwap",name),
				new RateMktData("3Y", 0.02495, "Fr007", "InterestRateSwap",name),
				new RateMktData("4Y", 0.0259, "Fr007", "InterestRateSwap",name),
				new RateMktData("5Y", 0.0267, "Fr007", "InterestRateSwap",name),
				new RateMktData("7Y", 0.0283, "Fr007", "InterestRateSwap",name),
				new RateMktData("10Y", 0.0297, "Fr007", "InterestRateSwap",name),
				new RateMktData("15Y", 0.032, "Fr007", "InterestRateSwap",name)
			};

			var fr007Curve2 = new InstrumentCurveDefinition(
				name2,
				curveConvention2,
				rates2,
				"SpotCurve");
			curveDefinitions.Add(fr007Curve2);

			var marketInfo = new MarketInfo("TestMarket")
			{
				ReferenceDate = referenceDate,
				YieldCurveDefinitions = curveDefinitions.ToArray(),
				HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates
			};

			QdpMarket market;
			MarketFunctions.BuildMarket(marketInfo, out market);
			return market;
		}
	}
}
