using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.Equity;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Trade.Equity;
using UnitTest.Utilities;

namespace UnitTest.EquityTest
{
	[TestClass]
	public class RangeAccrualTest
	{
        [TestMethod]
        public void TestRangeAccrual()
        {
            var marketInfo = TestMarket();
            QdpMarket market;
            MarketFunctions.BuildMarket(marketInfo, out market);

            var startDate = new Date(2015, 03, 19);
            var maturityDate = new Date(2015, 06, 21);
            var calendar = CalendarImpl.Get("chn");
            var obsDates = calendar.BizDaysBetweenDates(startDate, maturityDate).Union(new[] { maturityDate }).ToArray();
            var fixings = File.ReadAllLines(@"./Data/HistoricalEquityPrices/hs300.csv")
                    .Select(x =>
                    {
                        var splits = x.Split(',');
                        return Tuple.Create(new Date(DateTime.Parse(splits[0])), Double.Parse(splits[1]));
                    })
                    .Where(x => x.Item1 >= startDate && x.Item1 <= market.ReferenceDate)
                    .ToDictionary(x => x.Item1, x => x.Item2);
            var initialSpot = fixings[startDate];
            fixings = fixings.Select(x => Tuple.Create(x.Key, x.Value / initialSpot)).ToDictionary(x => x.Item1, x => x.Item2);

            var fixingsStr = fixings.Aggregate("", (current, fixing) => current + (fixing.Key.ToString() + QdpConsts.Comma + fixing.Value + QdpConsts.Semilicon));
            var rangeAccrualInfo = new RangeAccrualInfo
                ("RangeAccrual1", 
                strike: 1.0, 
                underlyingTicker: "SomeIndex", 
                underlyingInstrumentType: "EquityIndex",
                valuationParameter: new OptionValuationParameters("Fr007", "Dividend", "VolSurf", "000300.SH"),
                startDate: "2015-03-19",
                exerciseDates: "2015-06-21",
                notional: 1000000.0,
                optionType: "Call",
                dayCount: "Act365"
                )
            {
                DayCount = "Act365",
                Ranges = "1.05,1.1,0.015;1.1,1.15,0.025",
                Fixings = fixingsStr.Remove(fixingsStr.Length - 1),
            };

            var rangeAccrualVf = new RangeAccrualVf(rangeAccrualInfo);

            var result = rangeAccrualVf.ValueTrade(market, PricingRequest.All);

            var rangeAccrualInfoMc = new RangeAccrualInfo(
                "RangeAccrual1", strike: 1.0,
                underlyingTicker: "", underlyingInstrumentType: "EquityIndex",
                valuationParameter: new OptionValuationParameters("Fr007", "Dividend", "VolSurf", "000300.SH"),
                startDate: "2015-03-19",
                exerciseDates: "2015-06-21",
                notional: 1000000.0,
                optionType: "Call",
                monteCarlo: true,
                parallelDegree: 4,
                nsimulations: 20000
                )
			{
				Ranges = "1.05,1.1,0.015;1.1,1.15,0.025",
				Fixings = fixingsStr.Remove(fixingsStr.Length - 1),
			};
			rangeAccrualVf = new RangeAccrualVf(rangeAccrualInfoMc);
			var resultMc = rangeAccrualVf.ValueTrade(market, PricingRequest.All);
			Console.WriteLine(string.Format("Analytical: {0}, {1}, {2}, {3}, {4}, {5}", result.Pv, result.Delta, result.Gamma, result.Vega, result.Theta, result.Rho));
			Console.WriteLine(string.Format("Monte Carlo: {0}, {1}, {2}, {3}, {4}, {5}", resultMc.Pv, resultMc.Delta, resultMc.Gamma, resultMc.Vega, resultMc.Theta, resultMc.Rho));
		}

		private MarketInfo TestMarket()
		{
			var historiclIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates;

			var curveConvention = new CurveConvention("fr007CurveConvention",
					"CNY",
					"ModifiedFollowing",
					"Chn_ib",
					"Act365",
					"Continuous",
					"CubicHermiteMonotic");

			var fr007CurveName = "Fr007";
			var fr007RateDefinition = new[]
			{
				new RateMktData("1D", 0.05, "Spot", "None", fr007CurveName),
				new RateMktData("5Y", 0.05, "Spot", "None", fr007CurveName),
			};

			var dividendCurveName = "Dividend";
			var dividendRateDefinition = new[]
			{
				new RateMktData("1D", 0.02, "Spot", "None", dividendCurveName),
				new RateMktData("5Y", 0.02, "Spot", "None", dividendCurveName),
			};

			var curveDefinition = new[]
			{
				new InstrumentCurveDefinition(fr007CurveName, curveConvention, fr007RateDefinition, "SpotCurve"),
				new InstrumentCurveDefinition(dividendCurveName, curveConvention, dividendRateDefinition, "SpotCurve"),
			};

			var volSurf = new[] { new VolSurfMktData("VolSurf", 0.28), };

			var stockMktData = new[] { new StockMktData("000300.SH", 3971.7 / 3839.74), };

			var referenceDate = "2015-03-27";
            return new MarketInfo(
                marketName: "tmpMarket",
                referenceDate: referenceDate,
                yieldCurveDefinitions: curveDefinition,
                historicalIndexRates: historiclIndexRates,
                volSurfaceDefinitions: volSurf,
                stockDataDefinitions: stockMktData
                );
		}
	}
}
