using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using UnitTest.Utilities;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Commodity;
using Qdp.Pricing.Ecosystem.Trade.Commodity;
using Qdp.Pricing.Library.Commodity.CommodityProduct;
using Qdp.Pricing.Library.Commodity.Engines.Analytical;

namespace UnitTest.Commodity
{
    //TODO: fix the technical issue here
	[TestClass]
	public class CommodityForwardTest
	{
		private QdpMarket GetMarket()
		{
			var referenceDate ="2015-06-11";
			var curveConvention = new CurveConvention("curveConvention",
					"CNY",
					"ModifiedFollowing",
					"Chn",
					"Act365",
					"Continuous",
					"CubicHermiteMonotic");
			var curveDefinitions = new List<InstrumentCurveDefinition>();
			// 1 - discount curve
			var rates = new[]
			{
				new RateMktData("0D", 0.0114, "Spot", "None","Fr007"),
				new RateMktData("1D", 0.0114, "Spot", "None","Fr007"),
				new RateMktData("7D", 0.021, "Spot", "None","Fr007"),
				new RateMktData("98D", 0.0228, "Spot", "None","Fr007"),
				new RateMktData("186D", 0.0231, "Spot", "None","Fr007"),
				new RateMktData("277D", 0.024, "Spot", "None","Fr007"),
				new RateMktData("368D", 0.0241, "Spot", "None","Fr007"),
				new RateMktData("732D", 0.0248, "Spot", "None","Fr007"),
			};

			var fr007Curve = new InstrumentCurveDefinition(
				"Fr007",
				curveConvention,
				rates,
				"SpotCurve");
			curveDefinitions.Add(fr007Curve);


			var goldRates = new[]
			{
				new RateMktData("0D", 236.49, "ConvenienceYield", "CommoditySpot", "goldYield"),
				new RateMktData("187D", 240.6, "ConvenienceYield", "CommodityForward", "goldYield"),
				new RateMktData("370D", 243.45, "ConvenienceYield", "CommodityForward", "goldYield"),
			};
			var goldConvenienceYieldCurve = new InstrumentCurveDefinition("goldYield", curveConvention, goldRates, "SpotCurve", fr007Curve);
			curveDefinitions.Add(goldConvenienceYieldCurve);

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

		private IYieldCurve ZeroFirstPoint(IYieldCurve yieldCurve)
		{
			return new YieldCurve(
				yieldCurve.Name,
				yieldCurve.ReferenceDate,
				yieldCurve.KeyPoints.Select((x, i) => i == 0 ? Tuple.Create(x.Item1, 0.0) : x).ToArray(),
				yieldCurve.Bda,
				yieldCurve.DayCount,
				yieldCurve.Calendar,
				yieldCurve.Currency,
				yieldCurve.Compound,
				yieldCurve.Interpolation,
				YieldCurveTrait.SpotCurve,
				yieldCurve.BaseMarket,
				((YieldCurve)yieldCurve).CalibrateMktUpdateCondition,
				yieldCurve.Spread
				);
		}



        [TestMethod]
        public void CommodityForwardCalcTest()
        {
            CommodityForwardCalc(expectedPv: 10500, notional: 100, basis: 5, spot: 100);
        }

        public void CommodityForwardCalc(double expectedPv, double notional,double basis,double spot)
        {
            var commodityForward = creatCommodityForward(notional, basis);
            var vf = new CommodityForwardCNYVf(commodityForward);
            var market = TestMarket(spot: spot);
            var res = vf.ValueTrade(market, PricingRequest.Pv);
            var pv = res.Pv;

            Assert.AreEqual(pv, expectedPv, 1e-8);

        }

        [TestMethod]
        public void CommoditySwapCalcTest()
        {
            CommoditySwapCalc(expectedPv: 62409.54, notionals: new double[] { 100, 200,10000 }, spots: new double[] { 5, 1 });
        }

        public void CommoditySwapCalc(double expectedPv, double[] notionals, double[] spots)
        {
            var commoditySwap = creatCommoditySwap(notionals[0], notionals[1], notionals[2]);
            var engine = new AnalyticalCommoditySwapEngine();
            var market = SwapTestMarket(spots[0], spots[1]);
            var res = engine.Calculate(commoditySwap, market, PricingRequest.Pv);
            var pv = res.Pv;

            Assert.AreEqual(pv, expectedPv, 1e-8);

        }


        private CommodityForwardCNYInfo creatCommodityForward(double notional, double basis)
        {
            return new CommodityForwardCNYInfo(
                tradeId: "",
                underlyingTicker: "",
                notional: notional,
                basis: basis,
                startDate: "2018-03-22",
                maturityDate: "2018-05-22"
                );
        }

        private QdpMarket TestMarket(double spot, String referenceDate = "2018-03-22")
        {
            var historiclIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates;

            var stockMktData = new[] { new StockMktData("", spot), };

            var marketInfo = new MarketInfo("tmpMarket",
                referenceDate: referenceDate,
                stockDataDefinitions: stockMktData
                );
            QdpMarket market;
            MarketFunctions.BuildMarket(marketInfo, out market);
            return market;
        }

        private CommoditySwap creatCommoditySwap(double recNotional,double payNotional, double fxNotional)
        {
            return new CommoditySwap(
                recTicker: "CU1806",//铜
                payTicker:"HGM18",//纽约COMEX 铜
                fxTicker:"CUSF1812",//香港期货美元兑人民币
                recNotional:recNotional,
                payNotional:payNotional,
                fxNotional:fxNotional,
                recCcy:CurrencyCode.CNY,
                payCcy:CurrencyCode.USD,
                startDate: DateFromStr("2018-03-22"),
                maturityDate: DateFromStr("2018-05-22")
                );
        }

        private IMarketCondition SwapTestMarket(double recSpot, double paySpot, double exchangeRate = 6.3173, string referenceDate = "2018-03-22")
        {
            var historiclIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates;

            var stockMktData = new Dictionary<string, double> { { "CU1806", recSpot }, { "HGM18", paySpot } };

            return new MarketCondition(
               x => x.ValuationDate.Value = DateFromStr(referenceDate),
               x => x.SpotPrices.Value = stockMktData,
               x => x.FxSpot.Value = new Dictionary<string, double> { { "CUSF1812", exchangeRate } }
               );
        }
        private Date DateFromStr(String dateStr)
        {
            var dt = Convert.ToDateTime(dateStr);
            return new Date(dt.Year, dt.Month, dt.Day);
        }

    }
}
