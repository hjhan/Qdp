using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Ecosystem.Trade.FixedIncome;
using UnitTest.Utilities;

namespace UnitTest.CommonTests
{
	[TestClass]
	public class MarketTest
	{
		[TestMethod]
		public void TestHistoricalIndexRatesLoad()
		{
			var fr007Rates = HistoricalDataLoadHelper.GetIndexRates("Fr007");
		}

		[TestMethod]
		public void Fr007CurveConstructionTest()
		{
			var historiclIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates;

			var fr007CurveName = "Swap_Fr007";
			var fr007CurveConvention = new CurveConvention("fr007CurveConvention",
				"CNY",
				"ModifiedFollowing",
				"Chn_ib",
				"Act365",
				"Continuous",
				"CubicHermiteMonotic");
			var rateDefinition = new[]
			{
				new RateMktData("1D", 0.0431, "Fr007", "Deposit", fr007CurveName),
				new RateMktData("7D", 0.053, "Fr007", "Deposit", fr007CurveName),
				new RateMktData("3M", 0.0494, "Fr007", "InterestRateSwap", fr007CurveName),
				new RateMktData("6M", 0.0493, "Fr007", "InterestRateSwap", fr007CurveName),
			};

			var curveDefinition = new[]
			{
				new InstrumentCurveDefinition(fr007CurveName, fr007CurveConvention, rateDefinition, "SpotCurve")
			};

			var marketInfo = new MarketInfo("tmpMarket", "2014-02-10", curveDefinition, historiclIndexRates);
			var temp = CalendarImpl.Get("chn_ib");

			QdpMarket market = null;

			var time1 = DateTime.Now;
			for (var i = 0; i <= 10; ++i)
			{
				var result = MarketFunctions.BuildMarket(marketInfo, out market);
			}
			var time2 = DateTime.Now;
			Console.WriteLine("{0}ms", (time2-time1).TotalMilliseconds);
			var fr007Curve = market.GetData<CurveData>(fr007CurveName).YieldCurve;
			Assert.AreEqual(fr007Curve.KeyPoints[0].Item2, 0.0430974555290732, 1e-4); // use IrsFloatingLegPvReal
			Assert.AreEqual(fr007Curve.KeyPoints[3].Item2, 0.048992349561639, 1e-4); // use IrsFloatingLegPvReal
		}

		[TestMethod]
		public void VerifyFr007SwapCashflow()
		{
			var irsInfo = new InterestRateSwapInfo
			{
				StartDate = "20150828",
				MaturityDate = "20160828",

				Notional = 50000000,
				Calendar = "chn_ib",
				Currency = "CNY",
				SwapDirection = "Payer",

				FixedLegDC = "Act365",
				FixedLegBD = "ModifiedFollowing",
				FixedLegCoupon = 0.02395,
				FixedLegFreq = "Quarterly",
				FixedLegStub = "ShortEnd",

				FloatingLegDC = "Act365",
				FloatingLegFreq = "Quarterly",
				FloatingLegBD = "ModifiedFollowing",
				FloatingLegStub = "ShortEnd",
				Index = "Fr007",
				ResetTerm = "1W",
				ResetBD = "None",
				ResetToFixingGap = "-1BD",
				ResetStub = "ShortEnd",
				ResetCompound = "Compounded",
				ValuationParamters = new SimpleCfValuationParameters("Fr007", "Fr007")
			};

			var qdpMarket = VerifyCurveConstructionMarket("2015-10-23");
			var irsVf = new InterestRateSwapVf(irsInfo);
			var result = irsVf.ValueTrade(qdpMarket, PricingRequest.All);
			Assert.AreEqual(result.Pv, -4184.93417566339, 1e-4);
			foreach (var d in result.KeyRateDv01)
			{
				Console.WriteLine("{0},{1}", d.Key, d.Value);
			}
			Console.WriteLine("Total Dv01 {0}", result.Dv01);
			Console.WriteLine("Summed Dv01 {0}", result.KeyRateDv01.Sum(x => x.Value.Sum(e => e.Risk)));
		}


		[TestMethod]
		public void VerifyCurveConstruction()
		{
			var mkt = VerifyCurveConstructionMarket("2015-10-23");
			var mktShibor1D = VerifyCurveConstructionMarket("2015-01-07");
			var fr007Curve = mkt.GetData<CurveData>("Fr007").YieldCurve;
			var shibor3MCurve = mkt.GetData<CurveData>("Shibor3M").YieldCurve;
			var shibor1DCurve = mktShibor1D.GetData<CurveData>("Shibor1D").YieldCurve;
			var depo1YCurve = mkt.GetData<CurveData>("Depo1Y").YieldCurve;


			var bootstrapResult = File.ReadAllLines(@"./Data/Benchmark/YieldcurveBuiltResult.txt").Select(x =>
			{
				var splits = x.Split(',');
				return Tuple.Create(splits[0], new Date(DateTime.Parse(splits[1])), Convert.ToDouble(splits[2]));
			})
			.GroupBy(x => x.Item1)
			.ToDictionary(x => x.Key, x => x.ToDictionary(entry => entry.Item2, entry => entry.Item3));

			var QdpCurve = new[] { fr007Curve, shibor3MCurve, depo1YCurve, shibor1DCurve };
			var curveNames = new[] { "Fr007", "Shibor3M", "Depo1Y", "Shibor1D" };
			for (var i = 0; i < 4; ++i)
			{
				var QdpkeyPoints = QdpCurve[i].KeyPoints;
				var xfiKeyPoints = bootstrapResult[curveNames[i]];

				foreach (var QdpkeyPoint in QdpkeyPoints)
				{
					Assert.AreEqual(QdpkeyPoint.Item2, xfiKeyPoints[QdpkeyPoint.Item1], 1e-6);
				}
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
					"CubicHermiteMonotic");
			var curveDefinitions = new List<InstrumentCurveDefinition>();
			// 1 - discount curve
			var rates = new[]
			{
				//new RateMktData("1D", 0.0189, "Spot", "None","Fr007"),
				//new RateMktData("7D", 0.024, "Spot", "None","Fr007"),
				new RateMktData("1D", 0.0189, "Fr001", "Deposit","Fr007"),
				new RateMktData("7D", 0.024, "Fr001", "Deposit","Fr007"),
				new RateMktData("3M", 0.0239, "Fr007", "InterestRateSwap","Fr007"),
				new RateMktData("6M", 0.02385, "Fr007", "InterestRateSwap","Fr007"),
				new RateMktData("9M", 0.02385, "Fr007", "InterestRateSwap","Fr007"),
				new RateMktData("1Y", 0.0239, "Fr007", "InterestRateSwap","Fr007"),
				new RateMktData("2Y", 0.02405, "Fr007", "InterestRateSwap","Fr007"),
				new RateMktData("3Y", 0.02495, "Fr007", "InterestRateSwap","Fr007"),
				new RateMktData("4Y", 0.0259, "Fr007", "InterestRateSwap","Fr007"),
				new RateMktData("5Y", 0.0267, "Fr007", "InterestRateSwap","Fr007"),
				new RateMktData("7Y", 0.0283, "Fr007", "InterestRateSwap","Fr007"),
				new RateMktData("10Y", 0.0297, "Fr007", "InterestRateSwap","Fr007"),
				new RateMktData("15Y", 0.032, "Fr007", "InterestRateSwap","Fr007")
			};

			var fr007Curve = new InstrumentCurveDefinition(
				"Fr007",
				curveConvention,
				rates,
				"SpotCurve");
			curveDefinitions.Add(fr007Curve);

			var shibor3Mrates = new[]
			{
				new RateMktData("1D", 0.01909, "Shibor1D", "Deposit","Shibor3M"),
				new RateMktData("7D", 0.02401, "Shibor1W", "Deposit","Shibor3M"),
				new RateMktData("3M", 0.03144, "Shibor3M", "Deposit","Shibor3M"),
				new RateMktData("6M", 0.0323, "Shibor3M", "InterestRateSwap","Shibor3M"),
				new RateMktData("9M", 0.0323, "Shibor3M", "InterestRateSwap","Shibor3M"),
				new RateMktData("1Y", 0.0323, "Shibor3M", "InterestRateSwap","Shibor3M"),
				new RateMktData("2Y", 0.033, "Shibor3M", "InterestRateSwap","Shibor3M"),
				new RateMktData("3Y", 0.03325, "Shibor3M", "InterestRateSwap","Shibor3M"),
				new RateMktData("4Y", 0.03345, "Shibor3M", "InterestRateSwap","Shibor3M"),
				new RateMktData("5Y", 0.03405, "Shibor3M", "InterestRateSwap","Shibor3M"),
				new RateMktData("7Y", 0.03435, "Shibor3M", "InterestRateSwap","Shibor3M"),
				new RateMktData("10Y", 0.03585, "Shibor3M", "InterestRateSwap","Shibor3M"),
			};

			var shibor3MCurve = new InstrumentCurveDefinition(
				"Shibor3M",
				curveConvention,
				shibor3Mrates,
				"SpotCurve");
			curveDefinitions.Add(shibor3MCurve);

			var shibor1Drates = new[]
			{
				new RateMktData("1D", 0.02881, "Shibor1D", "Deposit","Shibor1D"),
				new RateMktData("7D", 0.03873, "Shibor1W", "Deposit","Shibor1D"),
				new RateMktData("3M", 0.0285, "Shibor1D", "InterestRateSwap","Shibor1D"),
				new RateMktData("6M", 0.0285, "Shibor1D", "InterestRateSwap","Shibor1D"),
				new RateMktData("9M", 0.0285, "Shibor1D", "InterestRateSwap","Shibor1D"),
				new RateMktData("1Y", 0.0285, "Shibor1D", "InterestRateSwap","Shibor1D"),
				new RateMktData("2Y", 0.0291, "Shibor1D", "InterestRateSwap","Shibor1D"),
				new RateMktData("3Y", 0.0293, "Shibor1D", "InterestRateSwap","Shibor1D"),
			};

			var shibor1DCurve = new InstrumentCurveDefinition(
				"Shibor1D",
				curveConvention,
				shibor1Drates,
				"SpotCurve");
			curveDefinitions.Add(shibor1DCurve);

			var depo1Yrates = new[]
			{
				new RateMktData("3M", 0.0135, "Depo3M", "Deposit","Depo1Y"),
				new RateMktData("6M", 0.0155, "Depo6M", "Deposit","Depo1Y"),
				new RateMktData("1Y", 0.0175, "Depo1Y", "Deposit","Depo1Y"),
				new RateMktData("2Y", 0.01575, "Depo1Y", "InterestRateSwap","Depo1Y"),
				new RateMktData("3Y", 0.0155, "Depo1Y", "InterestRateSwap","Depo1Y"),
				new RateMktData("4Y", 0.0166, "Depo1Y", "InterestRateSwap","Depo1Y"),
				new RateMktData("5Y", 0.0166, "Depo1Y", "InterestRateSwap","Depo1Y"),
				new RateMktData("7Y", 0.0171, "Depo1Y", "InterestRateSwap","Depo1Y"),
				new RateMktData("10Y", 0.0181, "Depo1Y", "InterestRateSwap","Depo1Y"),
			};

			var depo1YCurve = new InstrumentCurveDefinition(
				"Depo1Y",
				curveConvention,
				depo1Yrates,
				"SpotCurve");
			curveDefinitions.Add(depo1YCurve);

			var marketInfo2 = new MarketInfo("TestMarket")
			{
				ReferenceDate = referenceDate,
				YieldCurveDefinitions = curveDefinitions.ToArray(),
				HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates
			};

			QdpMarket market;
			MarketFunctions.BuildMarket(marketInfo2, out market);
			return market;
		}
	}
}
