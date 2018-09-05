using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Ecosystem.Trade.FixedIncome;
using UnitTest.Utilities;

namespace UnitTest.CommonTests
{
	[TestClass]
	public class CdsTest
	{
		[TestMethod]
		public void TestCdsCalibration()
		{
			var historiclIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates;

			const string fr007CurveName = "Swap_Fr007";
			var fr007CurveConvention = new CurveConvention(
				"fr007CurveConvention",
				"CNY", 
				"None", 
				"Chn_ib", 
				"Act365", 
				"Continuous",
				"CubicHermiteMonotic");
			var rateDefinition = new[]
			{
				new RateMktData("1D", 0.026, "Spot", "None", fr007CurveName),
				new RateMktData("20Y", 0.026, "Spot", "None", fr007CurveName)
			};

			var fr007CurveDefinition = new InstrumentCurveDefinition(fr007CurveName, fr007CurveConvention, rateDefinition, "SpotCurve");
			var curveDefinition = new[]
			{
				fr007CurveDefinition
			};

			const string spcCurveName = "spc_testCurve";
			var spcCurveConvention = new CurveConvention("spcCurveConvention",
				"CNY",
				"None",
				"Chn_ib",
				"Act365",
				"Continuous",
				"CubicHermiteMonotic");
			var cdsSpreads = new[]
			{
				new RateMktData("2015-06-20", 0.001535, "Spc", "CreditDefaultSwap", spcCurveName),
				new RateMktData("2015-12-20", 0.001901, "Spc", "CreditDefaultSwap", spcCurveName),
				new RateMktData("2016-12-20", 0.003131, "Spc", "CreditDefaultSwap", spcCurveName),
				new RateMktData("2017-12-20", 0.004749, "Spc", "CreditDefaultSwap", spcCurveName),
				new RateMktData("2018-12-20", 0.007395, "Spc", "CreditDefaultSwap", spcCurveName),
				new RateMktData("2019-12-20", 0.0096833, "Spc", "CreditDefaultSwap", spcCurveName),
				new RateMktData("2021-12-20", 0.01339, "Spc", "CreditDefaultSwap", spcCurveName),
				new RateMktData("2024-12-20", 0.015116, "Spc", "CreditDefaultSwap", spcCurveName)
			};

			var spcCurveDefinitions = new[]
			{
				new InstrumentCurveDefinition(spcCurveName, spcCurveConvention, cdsSpreads, "SpotCurve", fr007CurveDefinition)
			};

			var marketInfo = new MarketInfo("tmpMarket", "2014-12-16", curveDefinition, historiclIndexRates)
			{
				SpcCurveDefinitions = spcCurveDefinitions
			};
			QdpMarket market;
			var result = MarketFunctions.BuildMarket(marketInfo, out market);

			var spcCurve = market.GetData<CurveData>(spcCurveName).YieldCurve;
			var keyPoints = spcCurve.KeyPoints;
			Assert.AreEqual(keyPoints[0].Item2, 1.0);
			Assert.AreEqual(keyPoints[1].Item2, 0.99868359032, 1.0e-9);
			Assert.AreEqual(keyPoints[2].Item2, 0.99680057341, 1.0e-9);
			Assert.AreEqual(keyPoints[3].Item2, 0.98952113745, 1.0e-9);
			Assert.AreEqual(keyPoints[4].Item2, 0.97616083777, 1.0e-9);
			Assert.AreEqual(keyPoints[5].Item2, 0.95048151684, 1.0e-9);
			Assert.AreEqual(keyPoints[6].Item2, 0.91939248365, 1.0e-9);
			Assert.AreEqual(keyPoints[7].Item2, 0.84740619752, 1.0e-9);
			Assert.AreEqual(keyPoints[8].Item2, 0.76457411556, 1.0e-9);

			var cdsInfo = new CreditDefaultSwapInfo()
			{
				StartDate = "2014-09-22",
				MaturityDate = "2019-12-20",
				Notional = 10000000,
				BusinessDayConvention = "ModifiedFollowing",
				Calendar = "Chn_ib",
				Coupon = 0.01,
				Currency = "CNY",
				DayCount = "Act365",
				Frequency = "Quarterly",
				SwapDirection = "Payer",
				RecoveryRate = 0.4,
				Stub = "ShortEnd",
				NumIntegrationInterval = 100,
				ValuationParameters = new CdsValuationParameters(fr007CurveName, spcCurveName)
			};
			var cdsVf = new CreditDefaultSwapVf(cdsInfo);
			var pricingResult = cdsVf.ValueTrade(market, PricingRequest.Pv | PricingRequest.Ai);
			Assert.AreEqual(pricingResult.Pv, -38365.7413, 1e-4);
			Assert.AreEqual(pricingResult.Ai, -23287.6712, 1e-4);
		}

	}
}
