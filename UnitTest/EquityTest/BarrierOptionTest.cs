using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.Equity;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Trade.Equity;
using UnitTest.Utilities;

namespace CalcUnit
{
	[TestClass]
	public class BarrierOptionTest
	{
		[TestMethod]
		public void DoubleBarrierOptionTest()
		{
			//var nsimu = new[] {1000, 10000, 20000, 100000, 500000};
			var nsimu = new[] { 100, 200, 300 };
			var mcTrials = 6;
			var mc = new[] { true, false };
			var marketInfo = TestMarket();
            MarketFunctions.BuildMarket(marketInfo, out QdpMarket market);

            Console.WriteLine("NSimulation,TrialNum,PV,Delta,Gamma,Vega,Rho,Theta");
			for (var k = 0; k < nsimu.Length; k++)
			{
				for (var i = 0; i < mc.Length; ++i)
				{
					var trial = mc[i] ? mcTrials : 1;
                    for (var j = 0; j < trial; j++)
                    {
                        var barrierOptionInfo = new BarrierOptionInfo
                            ("", strike: 1.05,
                            underlyingTicker: "", underlyingInstrumentType: "EquityIndex",
                            valuationParameter: new OptionValuationParameters("Fr007",  "Dividend", "VolSurf", "000300.SH"),
                            startDate: "2016-12-13",
                            exerciseDates: "2017-12-13",
                            optionType: "Call",
                            exercise: "European",
                            calendar: "chn",
                            dayCount: "Act365",
                            notional: 1000000,
                            monteCarlo: mc[i],
                            parallelDegree : 1,
                            nsimulations: nsimu[k]
                            )
						{
							BarrierType = "DoubleTouchOut",
							Barrier = 0.75, //80% of S0//1e-6
							UpperBarrier = 1.25, //120% of S0 //1e6
							Coupon = 0.01,
							ParticipationRate = 1.0,
							Rebate = 0.02,
							Fixings = "2016-12-12,0.995;2016-12-13,1.0",
							UseFourier = true,
						};

						var barrierOptionVf = new BarrierOptionVf(barrierOptionInfo);
						var result = barrierOptionVf.ValueTrade(market, PricingRequest.All);
						Console.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", nsimu[k], mc[i] ? "MonteCarlo" : "Analytical", result.Pv, result.Delta, result.Gamma, result.Vega, result.Rho, result.Theta);
					}
				}
			}
		}


		private MarketInfo TestMarket()
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
				new RateMktData("1D", 0.015, "Spot", "None", fr007CurveName),
				new RateMktData("5Y", 0.015, "Spot", "None", fr007CurveName),
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

			var volSurf = new[] { new VolSurfMktData("VolSurf", 0.2), };
			var stockMktData = new[] { new StockMktData("000300.SH", 1.0), };

            return new MarketInfo(marketName: "tmpMarket",
                referenceDate: "2016-12-13", 
                yieldCurveDefinitions:curveDefinition, 
                historicalIndexRates:historiclIndexRates,
                volSurfaceDefinitions:volSurf, 
                stockDataDefinitions:stockMktData);

		}
	}
}