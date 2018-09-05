using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.MathMethods.Processes.ShortRate;
using Qdp.Pricing.Library.Common.Utilities.Coupons;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using UnitTest.Utilities;

namespace UnitTest.CommonTests
{
    [TestClass]
    public class BondFutureTests
    {

        [TestMethod]
        public void BondFuturesCTDPnLTest() {
            var rateT0 = 5.0 / 100.0;
            var rateT1 = 5.1 / 100.0;
            var priceT0 = 95.695;
            var priceT1 = 95.495;  //originally 95.895
            var instAndMarkets = SetupBondFuturePnLTestMarket(rateT0, rateT1, priceT0, priceT1);
            var marketT0 = instAndMarkets.Item2;
            var marketT1 = instAndMarkets.Item3;
            var tMarketRollToT1 = instAndMarkets.Item4;

            var bond = new Bond(
                        "170001.IB",
                        new Date(2017, 1, 12),
                        new Date(2022, 1, 12),
                        100.0,
                        CurrencyCode.CNY,
                        new FixedCoupon(0.0288),
                        CalendarImpl.Get("chn_ib"),
                        Frequency.Annual,
                        Stub.LongEnd,
                        new ActActIsma(),
                        new ActAct(),
                        BusinessDayConvention.None,
                        BusinessDayConvention.ModifiedFollowing,
                        new DayGap("0D"),
                        TradingMarket.ChinaInterBank);

            var bPriceT0 = 96.2326;
            var bPriceT1 = 95.9326;

            var engine = new BondEngineCn();
            var resultT0 = engine.Calculate(bond, marketT0, PricingRequest.ZeroSpread | PricingRequest.Ytm | 
                PricingRequest.KeyRateDv01 | PricingRequest.ZeroSpreadDelta|PricingRequest.Pv | PricingRequest.Convexity);
            var resultT1 = engine.Calculate(bond, marketT1, PricingRequest.ZeroSpread | PricingRequest.Ytm | PricingRequest.Pv);

            var resultRolledT1 = engine.Calculate(bond, tMarketRollToT1, PricingRequest.Pv);
            var rateScaling = 1e4;

            var zsprdT0 = resultT0.ZeroSpread;
            var zsprdT1 = resultT1.ZeroSpread;
            var zspreadPnl = (zsprdT1 - zsprdT0) * rateScaling * resultT0.ZeroSpreadDelta;

            var curveMove = (rateT1 - rateT0) * rateScaling;
            var curvePnLs = resultT0.KeyRateDv01.First().Value.Select(x => new CurveRisk(x.Tenor, 1.0 * x.Risk * curveMove));
            var curvePnl = curvePnLs.Sum(x => x.Risk);

            var pnlConverixy = 0.5 * Math.Pow(resultT1.Ytm - resultT0.Ytm, 2.0) * resultT0.Convexity * bPriceT0;

            var timePnl = resultRolledT1.Pv - resultT0.Pv;

            var actualPnl = bPriceT1 - bPriceT0;
            var estimatedPnL = curvePnl + zspreadPnl + pnlConverixy + timePnl;
            var diff = (actualPnl - estimatedPnL)/ actualPnl * 100;
            Assert.AreEqual(true, Math.Abs(diff) < 3);

        }

        private Tuple<BondFutures, IMarketCondition, IMarketCondition, IMarketCondition> SetupBondFuturePnLTestMarket(double rateT0, double rateT1, double priceT0, double priceT1) {
            var t0 = "2017-11-13";
            var t1 = "2017-11-14";

            var fileNameT0 = @"./Data/BondFuture/TF1712DeliverableBondInfo_20171113.txt";
            var fileNameT1 = @"./Data/BondFuture/TF1712DeliverableBondInfo_20171114.txt";
            var bondMarketTuple = marketFromDeliverableBondsAndPrices(valuationDay: t0, futurePrice: priceT0, rate: rateT0, fileName: fileNameT0);
            var bondFut = bondMarketTuple.Item1;
            var marketT0 = bondMarketTuple.Item2;
            var marketT1 = marketFromDeliverableBondsAndPrices(valuationDay: t1, futurePrice: priceT1, rate: rateT1, fileName: fileNameT1).Item2;

            var tMarketRollToT1 = marketFromDeliverableBondsAndPrices(valuationDay: t1, futurePrice: priceT0, rate: rateT0, fileName: fileNameT0).Item2;
            return Tuple.Create<BondFutures, IMarketCondition, IMarketCondition, IMarketCondition>(bondFut, marketT0, marketT1, tMarketRollToT1);
        }

        [TestMethod]
        public void BondFuturesPnLTest() {
            var rateT0 = 5.0 / 100.0;
            var rateT1 = 5.1 / 100.0;
            var priceT0 = 95.695;

            //priceT1 = 95.495 will be perfect, 
            //but even if fut price and ctd price go in totally different direction, our pnl still looks good!
            var priceT1 = 95.895; 

            var instAndMarkets = SetupBondFuturePnLTestMarket(rateT0, rateT1, priceT0, priceT1);
            var bondFut = instAndMarkets.Item1;
            var marketT0 = instAndMarkets.Item2;
            var marketT1 = instAndMarkets.Item3;
            var tMarketRollToT1 = instAndMarkets.Item4;

            //1.market as of T0
            //price & risk it, calc BasisT0 = future - ctd / conv
            //2. market as of T1,  calc Basis, Zpread again
            //3. actual pnl = (new price - old price) /100* 1e6 * lots
            //4. 
            //estimated = curve pnl  + zspread pnl + basis pnl + time pnl from CTD
            //basis pnl  = (basisT1 - basisT0) / 100 * 1e6 * lots

            var engine = new BondFuturesEngine<HullWhite>(new HullWhite(0.1, 0.01), 20);
            var resultT0 = engine.Calculate(bondFut, marketT0, PricingRequest.KeyRateDv01 | PricingRequest.ZeroSpread
                | PricingRequest.ZeroSpreadDelta | PricingRequest.Basis | PricingRequest.ConvertFactors 
                | PricingRequest.UnderlyingPv | PricingRequest.Convexity | PricingRequest.Ytm | PricingRequest.CheapestToDeliver);

            var resultT1 = engine.Calculate(bondFut, marketT1, PricingRequest.ZeroSpread | PricingRequest.Ytm);
            var rateScaling = 1e4;
            
            var zsprdT0 = resultT0.ZeroSpread;
            var zsprdT1 = resultT1.ZeroSpread;
            var zspreadPnl = (zsprdT1 - zsprdT0) * rateScaling * resultT0.ZeroSpreadDelta;

            var curveMove = (rateT1 - rateT0) * rateScaling;
            var curvePnLs = resultT0.KeyRateDv01.First().Value.Select(x => new CurveRisk(x.Tenor, 1.0 * x.Risk * curveMove));
            var curvePnl = curvePnLs.Sum(x => x.Risk);

            var ctdId = resultT0.CheapestToDeliver;
            var ctdPriceT0 = marketT0.MktQuote.Value[ctdId].Item2;
            var pnlConverixy = 0.5 * Math.Pow(resultT1.Ytm - resultT0.Ytm, 2.0) * resultT0.Convexity * ctdPriceT0;

            var basisT0 = resultT0.Basis;
            var basisT1 = resultT1.Basis;
            var futPosScaling = 1.0 / 100.0 * bondFut.Notional;
            var basisPnL = (basisT1 - basisT0) * futPosScaling;
            
            var resultRollToT1 = engine.Calculate(bondFut, tMarketRollToT1, PricingRequest.UnderlyingPv);
            var timePnl = resultRollToT1.UnderlyingPv - resultT0.UnderlyingPv;

            //debug purpose
            var cf = resultT0.ConvertFactors[ctdId];

            var estimatedFromCTD = curvePnl + zspreadPnl + timePnl + pnlConverixy;
            var estimatedPnL = estimatedFromCTD + basisPnL;
        
            var actualPnl = (priceT1 - priceT0)/100 * bondFut.Notional;

            var diffPct = (actualPnl - estimatedPnL) / actualPnl * 100;
            Assert.AreEqual(true, Math.Abs(diffPct) < 2);
        }

        private Tuple<BondFutures, Dictionary<string, Tuple<PriceQuoteType, double>>>  SetupDeliverableBonds(string fileName, double futureLots, double futurePrice) {
            var bondPrices = new Dictionary<string, Tuple<PriceQuoteType, double>>();
            var deliverableBonds = File.ReadAllLines(fileName)
                .Select(x =>
                {
                    var splits = x.Split(',');
                    bondPrices[splits[0]] = Tuple.Create(PriceQuoteType.Dirty, Convert.ToDouble(splits[6]));
                    return new Bond(
                        splits[0],
                        new Date(DateTime.Parse(splits[4])),
                        new Date(DateTime.Parse(splits[5])),
                        100.0,
                        CurrencyCode.CNY,
                        new FixedCoupon(Convert.ToDouble(splits[2])),
                        CalendarImpl.Get("chn_ib"),
                        Convert.ToInt32(splits[3]) == 1 ? Frequency.Annual : Frequency.SemiAnnual,
                        Stub.LongEnd,
                        new ActActIsma(),
                        new ActAct(),
                        BusinessDayConvention.None,
                        BusinessDayConvention.ModifiedFollowing,
                        new DayGap("0D"),
                        TradingMarket.ChinaInterBank
                        );
                }).ToArray();

            var futureCode = "TF1712";
            var bondFuture = new BondFutures(
                futureCode,
                new Date(2017, 3, 13),
                new Date(2017, 12, 13),
                new Date(2017, 12, 8),
                CalendarImpl.Get("chn"),
                deliverableBonds,
                new Act365(),
                notional: futureLots * 1e6
                );
            bondPrices[futureCode] = Tuple.Create(PriceQuoteType.Dirty, futurePrice);
            return Tuple.Create<BondFutures, Dictionary<string, Tuple<PriceQuoteType, double>>>(bondFuture, bondPrices);
        }

        private Tuple<BondFutures, IMarketCondition> marketFromDeliverableBondsAndPrices(string valuationDay, double futurePrice, double rate,
            string fileName = @"./Data/BondFuture/TF1509DeliverableBondInfo.txt") {
            var futureLots = 1.0;
            var bondFutAndPrices = SetupDeliverableBonds(fileName, futureLots, futurePrice);
            var bondFuture = bondFutAndPrices.Item1;
            var bondPrices = bondFutAndPrices.Item2;
            var market = TestMarket(valuationDay, fundingRate: rate, reinvestmentRate: rate, compound: "Annual", interpolation: "Linear");
            market = market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, bondPrices));
            return new Tuple<BondFutures, IMarketCondition>(bondFuture, market);    
        }

        //Note: this is a bad test, only shows 
        //1) bad deliverable bond choice, bonds to mature 2022 maturity,  they cannot be deliverable for TF1509,  cannot be <5.25 yr in maturity month of this 5y contract matures
        //2) zspread calibration failure for these bonds
		//[TestMethod]
		public void BondFutureTest()
		{
			var bondPrices = new Dictionary<string, Tuple<PriceQuoteType, double>>();
			var deliverableBonds = File.ReadAllLines(@"./Data/BondFuture/TF1509DeliverableBondInfo.txt")
				.Select(x =>
				{
					var splits = x.Split(',');
					bondPrices[splits[0]]= Tuple.Create(PriceQuoteType.Dirty, Convert.ToDouble(splits[6]));
					return new Bond(
						splits[0],
						new Date(DateTime.Parse(splits[4])),
						new Date(DateTime.Parse(splits[5])),
						100.0,
						CurrencyCode.CNY,
						new FixedCoupon(Convert.ToDouble(splits[2])),
						CalendarImpl.Get("chn_ib"),
						Convert.ToInt32(splits[3]) == 1 ? Frequency.Annual : Frequency.SemiAnnual,
						Stub.LongEnd,
						new ActActIsma(), 
						new ActAct(),
						BusinessDayConvention.None,
						BusinessDayConvention.ModifiedFollowing,
						new DayGap("0D"),
						TradingMarket.ChinaInterBank
						);
				}).ToArray();

			var bondFuture = new BondFutures(
				"TF1509",
				new Date(2015, 06, 11),
				new Date(2015, 09, 16),
				new Date(2015, 09, 11),
				CalendarImpl.Get("chn"),
				deliverableBonds,
				new Act365()
				);

		    bondPrices["TF1509"] = Tuple.Create(PriceQuoteType.Dirty, 100.0);
			var market = TestMarket("2015-06-16");
			market = market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, bondPrices));
			var engine = new BondFuturesEngine<HullWhite>(new HullWhite(0.1, 0.01), 20);
            var result = engine.Calculate(bondFuture, market, PricingRequest.Pv01);


            //Console.WriteLine("{0},{1}", result.Pv, result.Dv01);
            //Assert.AreEqual(result.Pv, 96.661228296513, 1e-9);
            ////Assert.AreEqual(result.Dv01, 0.063742890569, 1e-9); Cash flow Dv01 changed from pv(-1bp)-pv(1bp) to pv(-1bp)-pv(0bp) for faster calculation
            //Assert.AreEqual(result.Dv01, 0.0637648860348321, 1e-4);
        }


		[TestMethod]
		public void TestConversionFactor()
		{
			var targetConversionFactors = new List<double>();
			var bondPrices = new Dictionary<string, double>();
			var bonds = File.ReadAllLines(@"./Data/BondFuture/ConversionFactorTest.txt")
				.Select(x =>
				{
					var splits = x.Split(',');
					var startDate = new Date(DateTime.Parse(splits[3]));
					var maturityDate = new Date(DateTime.Parse(splits[4]));
					var rate = Convert.ToDouble(splits[1]);
					var frequency = splits[2] == "Annual" ? Frequency.Annual : Frequency.SemiAnnual;
					targetConversionFactors.Add(Convert.ToDouble(splits[5]));
					bondPrices[splits[0]] = Convert.ToDouble(splits[6]);
					return new Bond(
						splits[0],
						startDate,
						maturityDate,
						100.0,
						CurrencyCode.CNY,
						new FixedCoupon(rate),
						CalendarImpl.Get("chn_ib"),
						frequency,
						Stub.LongEnd,
						new Act365(),
						new Act365(),
						BusinessDayConvention.None,
						BusinessDayConvention.ModifiedFollowing,
						null,
						TradingMarket.ChinaInterBank);
				}).ToArray();

			var bondfuture = new BondFutures(
				"TF1509",
				new Date(2014, 6, 16),
				new Date(2015, 3, 13),
				new Date(2015, 3, 9),
				CalendarImpl.Get("chn"),
				bonds,
				new Act365()
				);
			var market = TestMarket("2014-07-25");
			for (var i = 0; i < bonds.Length; ++i)
			{
				Assert.AreEqual(bondfuture.GetConversionFactor(bonds[i], market), targetConversionFactors[i]);
			}
		}

		[TestMethod]
		public void IrrTest()
		{
			var bondPrices = new Dictionary<string, Tuple<PriceQuoteType, double>>();
			var deliverableBonds = File.ReadAllLines(@"./Data/BondFuture/QbTF1606CfTest.txt")
				.Select(x =>
				{
					var splits = x.Split(',');
					bondPrices[splits[0]] = Tuple.Create(PriceQuoteType.Ytm, Convert.ToDouble(splits[8]));
					return new Bond(
						splits[0],
						new Date(DateTime.Parse(splits[3])),
						new Date(DateTime.Parse(splits[4])),
						100.0,
						CurrencyCode.CNY,
						new FixedCoupon(Convert.ToDouble(splits[1])),
						CalendarImpl.Get("chn_ib"),
						splits[2] == "Annual" ? Frequency.Annual : Frequency.SemiAnnual,
						Stub.LongEnd,
						new ActActIsma(), 
						new ActActIsma(), 
						BusinessDayConvention.None,
						BusinessDayConvention.None,
						new DayGap("0D"),
						TradingMarket.ChinaInterBank
						);
				}).ToArray();

			var bondFuture = new BondFutures(
				"TF1606",
				new Date(2016, 04, 08),
				new Date(2016, 06, 15),
				new Date(2016, 06, 10),
				CalendarImpl.Get("chn"),
				deliverableBonds,
				new Act365()
				);

			bondPrices.Add("TF1606", Tuple.Create(PriceQuoteType.Dirty, 100.66));
			var market = TestMarket("2016-04-29", bondPrices, "Simple");
			//market = market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, bondPrices));
			var engine = new BondFuturesEngine<HullWhite>(new HullWhite(0.1, 0.01), 20);
			var result = engine.Calculate(bondFuture, market, PricingRequest.Irr);

			Dictionary<string, Dictionary<string, double>> expectResults = new Dictionary<string, Dictionary<string, double>>()
			{
				{"160002", new Dictionary<string, double>(){{"Irr", -0.091403},{"BondCleanPrice", 100.1693},{"BondDirtyPrice", 100.9021}}},
				{"150019", new Dictionary<string, double>(){{"Irr", -0.067332},{"BondCleanPrice", 102.513},{"BondDirtyPrice", 104.5205}}},
				{"140013", new Dictionary<string, double>(){{"Irr", -0.074859},{"BondCleanPrice", 107.011},{"BondDirtyPrice", 110.317}}},
				{"140006", new Dictionary<string, double>(){{"Irr", -0.082592},{"BondCleanPrice", 108.3015},{"BondDirtyPrice", 108.61}}},
				{"140003", new Dictionary<string, double>(){{"Irr", -0.086425},{"BondCleanPrice", 108.5624},{"BondDirtyPrice", 109.8241}}},
				{"130020", new Dictionary<string, double>(){{"Irr", -0.087293},{"BondCleanPrice", 106.7046},{"BondDirtyPrice", 108.8731}}},
				{"130015", new Dictionary<string, double>(){{"Irr", -0.071649},{"BondCleanPrice", 103.8394},{"BondDirtyPrice", 106.6093}}},
				{"110019", new Dictionary<string, double>(){{"Irr", -0.075644},{"BondCleanPrice", 106.6631},{"BondDirtyPrice", 107.4296}}},
				{"110008", new Dictionary<string, double>(){{"Irr", -0.087619},{"BondCleanPrice", 106.0245},{"BondDirtyPrice", 106.472}}},
				{"110002", new Dictionary<string, double>(){{"Irr", -0.088161},{"BondCleanPrice", 106.4155},{"BondDirtyPrice", 107.4979}}},
				{"100034", new Dictionary<string, double>(){{"Irr", -0.091778},{"BondCleanPrice", 105.0912},{"BondDirtyPrice", 105.1012}}},
				{"100031", new Dictionary<string, double>(){{"Irr", -0.090643},{"BondCleanPrice", 103.4498},{"BondDirtyPrice", 103.8432},{"Basis", 1.6322},{"NetBasis", 1.5463},
                    { "Spread", -0.115643},{"Margin", -1.212},{"AiStart", 0.3934},{"AiEnd", 0.8136},{"Coupon", 0},{"InterestIncome", 0.4202},{"PnL", 0.0859},{"InvoicePrice", 102.6312}}},
				{"100024", new Dictionary<string, double>(){{"Irr", -0.0891},{"BondCleanPrice", 103.3754},{"BondDirtyPrice", 104.1324}}},
				{"050012", new Dictionary<string, double>(){{"Irr", -0.0895917},{"BondCleanPrice", 105.0325},{"BondDirtyPrice", 106.697}}}
			};

			foreach (var expectResult in expectResults)
			{
				var irr = result.ProductSpecific["Irr"][expectResult.Key].Rate;
				var bondCleanPrice = result.ProductSpecific["BondCleanPrice"][expectResult.Key].Rate;
				var bondDirtyPrice = result.ProductSpecific["BondDirtyPrice"][expectResult.Key].Rate;
				Console.WriteLine("{0}: IRR: {1}", expectResult.Key, irr);
				Console.WriteLine("{0}: BondCleanPrice: {1}", expectResult.Key, bondCleanPrice);
				Console.WriteLine("{0}: BondDirtyPrice: {1}", expectResult.Key, bondDirtyPrice);
				//try
				//{
				Assert.AreEqual(expectResults[expectResult.Key]["Irr"], irr, 1e-4);
				Assert.AreEqual(expectResults[expectResult.Key]["BondCleanPrice"], bondCleanPrice, 1e-4);
				Assert.AreEqual(expectResults[expectResult.Key]["BondDirtyPrice"], bondDirtyPrice, 1e-2);
				//}
				//catch (AssertFailedException e)
				//{
				//	Console.WriteLine(e.Message);
				//}
			}

			var expectTradeId = "100031";
			//try
			//{
				Console.WriteLine("{0}: Basis: {1}", expectTradeId, result.ProductSpecific["Basis"][expectTradeId].Rate);
				Console.WriteLine("{0}: NetBasis: {1}", expectTradeId, result.ProductSpecific["NetBasis"][expectTradeId].Rate);
				Console.WriteLine("{0}: Spread: {1}", expectTradeId, result.ProductSpecific["Spread"][expectTradeId].Rate);
				Console.WriteLine("{0}: Margin: {1}", expectTradeId, result.ProductSpecific["Margin"][expectTradeId].Rate);
				Console.WriteLine("{0}: AiStart: {1}", expectTradeId, result.ProductSpecific["AiStart"][expectTradeId].Rate);
				Console.WriteLine("{0}: AiEnd: {1}", expectTradeId, result.ProductSpecific["AiEnd"][expectTradeId].Rate);
				Console.WriteLine("{0}: InterestIncome: {1}", expectTradeId, result.ProductSpecific["InterestIncome"][expectTradeId].Rate);
				Console.WriteLine("{0}: InterestCost: {1}", expectTradeId, result.ProductSpecific["InterestCost"][expectTradeId].Rate);
				Console.WriteLine("{0}: PnL: {1}", expectTradeId, result.ProductSpecific["PnL"][expectTradeId].Rate);
				Console.WriteLine("{0}: InvoicePrice: {1}", expectTradeId, result.ProductSpecific["InvoicePrice"][expectTradeId].Rate);
				Console.WriteLine("{0}: Coupon: {1}", expectTradeId, result.ProductSpecific["Coupon"][expectTradeId].Rate);

				Assert.AreEqual(expectResults[expectTradeId]["Basis"], result.ProductSpecific["Basis"][expectTradeId].Rate, 1e-4);
				Assert.AreEqual(expectResults[expectTradeId]["NetBasis"], result.ProductSpecific["NetBasis"][expectTradeId].Rate, 1e-4);
				Assert.AreEqual(expectResults[expectTradeId]["Spread"], result.ProductSpecific["Spread"][expectTradeId].Rate, 1e-4);
				Assert.AreEqual(expectResults[expectTradeId]["Margin"], result.ProductSpecific["Margin"][expectTradeId].Rate, 1e-4);
				Assert.AreEqual(expectResults[expectTradeId]["AiStart"], result.ProductSpecific["AiStart"][expectTradeId].Rate, 1e-4);
				Assert.AreEqual(expectResults[expectTradeId]["AiEnd"], result.ProductSpecific["AiEnd"][expectTradeId].Rate, 1e-4);
				Assert.AreEqual(expectResults[expectTradeId]["InterestIncome"], result.ProductSpecific["InterestIncome"][expectTradeId].Rate, 1e-4);
				Assert.AreEqual(expectResults[expectTradeId]["PnL"], result.ProductSpecific["PnL"][expectTradeId].Rate, 1e-4);
				Assert.AreEqual(expectResults[expectTradeId]["InvoicePrice"], result.ProductSpecific["InvoicePrice"][expectTradeId].Rate, 1e-4);
				Assert.AreEqual(expectResults[expectTradeId]["Coupon"], result.ProductSpecific["Coupon"][expectTradeId].Rate, 1e-4);
			//}
			//catch (AssertFailedException e)
			//{
			//	Console.WriteLine(e.Message);
			//}
		}

		[TestMethod]
		public void BondPriceTest()
		{
			var bondPrices = new Dictionary<string, Tuple<PriceQuoteType, double>>();
			var deliverableBonds = File.ReadAllLines(@"./Data/BondFuture/QbTF1606CfTest.txt")
				.Select(x =>
				{
					var splits = x.Split(',');
					return new Bond(
						splits[0],
						new Date(DateTime.Parse(splits[3])),
						new Date(DateTime.Parse(splits[4])),
						100.0,
						CurrencyCode.CNY,
						new FixedCoupon(Convert.ToDouble(splits[1])),
						CalendarImpl.Get("chn_ib"),
						splits[2] == "Annual" ? Frequency.Annual : Frequency.SemiAnnual,
						Stub.LongEnd,
						new ActActIsma(), 
						new Act365(),
						BusinessDayConvention.None,
						BusinessDayConvention.ModifiedFollowing,
						new DayGap("0BD"),
						TradingMarket.ChinaInterBank
						);
				}).ToArray();

			var bondFuture = new BondFutures(
				"TF1606",
				new Date(2016, 04, 08),
				new Date(2016, 06, 15),
				new Date(2016, 06, 10),
				CalendarImpl.Get("chn"),
				deliverableBonds,
				new Act365()
				);

			bondPrices.Add("TF1606", Tuple.Create(PriceQuoteType.Dirty, 100.66));

			var engine = new BondFuturesEngine<HullWhite>(new HullWhite(0.1, 0.01), 20);

			Dictionary<string, Dictionary<string, double>> expectResults = new Dictionary<string, Dictionary<string, double>>()
			{
				{"160002", new Dictionary<string, double>(){{"Irr", -0.091403},{"BondCleanPrice", 100.1693},{"BondDirtyPrice", 100.9021}}},
				{"150019", new Dictionary<string, double>(){{"Irr", -0.067332},{"BondCleanPrice", 102.513},{"BondDirtyPrice", 104.5205}}},
				{"140013", new Dictionary<string, double>(){{"Irr", -0.074859},{"BondCleanPrice", 107.0109},{"BondDirtyPrice", 110.317}}},
				{"140006", new Dictionary<string, double>(){{"Irr", -0.082592},{"BondCleanPrice", 108.3015},{"BondDirtyPrice", 108.61}}},
				{"140003", new Dictionary<string, double>(){{"Irr", -0.086425},{"BondCleanPrice", 108.5624},{"BondDirtyPrice", 109.8241}}},
				{"130020", new Dictionary<string, double>(){{"Irr", -0.087293},{"BondCleanPrice", 106.7046},{"BondDirtyPrice", 108.8731}}},
				{"130015", new Dictionary<string, double>(){{"Irr", -0.071649},{"BondCleanPrice", 103.8394},{"BondDirtyPrice", 106.6093}}},
				{"110019", new Dictionary<string, double>(){{"Irr", -0.075644},{"BondCleanPrice", 106.663},{"BondDirtyPrice", 107.4296}}},
				{"110008", new Dictionary<string, double>(){{"Irr", -0.087619},{"BondCleanPrice", 106.0245},{"BondDirtyPrice", 106.472}}},
				{"110002", new Dictionary<string, double>(){{"Irr", -0.088161},{"BondCleanPrice", 106.4155},{"BondDirtyPrice", 107.4979}}},
				{"100034", new Dictionary<string, double>(){{"Irr", -0.091778},{"BondCleanPrice", 105.0912},{"BondDirtyPrice", 105.1012}}},
				{"100024", new Dictionary<string, double>(){{"Irr", -0.0891},{"BondCleanPrice", 103.3754},{"BondDirtyPrice", 104.1324}}},
				{"050012", new Dictionary<string, double>(){{"Irr", -0.089582},{"BondCleanPrice", 105.0324},{"BondDirtyPrice", 106.697}}}
			};
			var marketInfo = TestMarket("2016-04-29", bondPrices, "Simple");
			foreach (var deliverableBond in deliverableBonds)
			{
				marketInfo.MktQuote.Value[bondFuture.Id + "_" + deliverableBond.Id] = Tuple.Create(PriceQuoteType.Irr, 0.025);
			}
			var result = engine.Calculate(bondFuture, marketInfo, PricingRequest.UnderlyingFairQuote);
			foreach (var expectResult in expectResults)
			{
				var irr = result.ProductSpecific["Irr"][expectResult.Key].Rate;
				var bondCleanPrice = result.ProductSpecific["BondCleanPrice"][expectResult.Key].Rate;
				var bondDirtyPrice = result.ProductSpecific["BondDirtyPrice"][expectResult.Key].Rate;
				Console.WriteLine("{0}: IRR: {1}", expectResult.Key, irr);
				Console.WriteLine("{0}: BondCleanPrice: {1}", expectResult.Key, bondCleanPrice);
				Console.WriteLine("{0}: BondDirtyPrice: {1}", expectResult.Key, bondDirtyPrice);
			}

			expectResults = new Dictionary<string, Dictionary<string, double>>()
			{
				{"100031", new Dictionary<string, double>(){{"Irr", -0.090643},{"BondCleanPrice", 103.4498},{"BondDirtyPrice", 103.8432},{"Basis", 1.6322},{"NetBasis", 1.5463},{"Spread", -0.115643},{"Margin", -1.212},{"AiStart", 0.3934},{"AiEnd", 0.8136},{"Coupon", 0},{"InterestIncome", 0.4202},{"PnL", 0.0859},{"InvoicePrice", 102.6311}}},
			};
			var priceQuoteArray = new PriceQuoteType[] { PriceQuoteType.Basis, PriceQuoteType.NetBasis, PriceQuoteType.Irr };

			foreach (var priceQuoteRequest in priceQuoteArray)
			{
				foreach (var expectResult in expectResults)
				{
					var expectTradeId = expectResult.Key;
					IMarketCondition market;
					var priceQuote = bondPrices.ToDictionary(x => x.Key, y => y.Value);
					if (priceQuoteRequest == PriceQuoteType.Irr)
					{
						market = TestMarket("2016-04-29", priceQuote, "Simple", 0.025, expectResults[expectTradeId]["Irr"]);
						market.MktQuote.Value[bondFuture.Id + "_" + expectTradeId] = Tuple.Create(PriceQuoteType.Irr, expectResults[expectTradeId]["Irr"]);
					}
					else
					{
						priceQuote["TF1606_" + expectTradeId] = Tuple.Create(priceQuoteRequest, expectResults[expectTradeId][priceQuoteRequest.ToString()]);
						market = TestMarket("2016-04-29", priceQuote, "Simple");
					}

					result = engine.Calculate(bondFuture, market, PricingRequest.UnderlyingFairQuote);

					Assert.AreEqual(expectResults[expectTradeId]["Basis"], result.ProductSpecific["Basis"][expectTradeId].Rate, 1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["NetBasis"], result.ProductSpecific["NetBasis"][expectTradeId].Rate,
						1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["Spread"], result.ProductSpecific["Spread"][expectTradeId].Rate, 1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["Margin"], result.ProductSpecific["Margin"][expectTradeId].Rate, 1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["AiStart"], result.ProductSpecific["AiStart"][expectTradeId].Rate,
						1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["AiEnd"], result.ProductSpecific["AiEnd"][expectTradeId].Rate, 1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["InterestIncome"],
						result.ProductSpecific["InterestIncome"][expectTradeId].Rate, 1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["PnL"], result.ProductSpecific["PnL"][expectTradeId].Rate, 1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["InvoicePrice"],
						result.ProductSpecific["InvoicePrice"][expectTradeId].Rate, 1e-3);
					Assert.AreEqual(expectResults[expectTradeId]["Coupon"], result.ProductSpecific["Coupon"][expectTradeId].Rate, 1e-4);
				}
			}
		}

		[TestMethod]
		public void FuturesPriceForBasisTest()
		{
			var bondPrices = new Dictionary<string, Tuple<PriceQuoteType, double>>();
			var deliverableBonds = File.ReadAllLines(@"./Data/BondFuture/QbT1609BasisTest.txt").Where(x=>!x.StartsWith("#"))
				.Select(x =>
				{
					var splits = x.Split(',');
					bondPrices[splits[0]] = Tuple.Create(PriceQuoteType.Dirty, Convert.ToDouble(splits[5]));
					return new Bond(
						splits[0],
						new Date(DateTime.Parse(splits[3])),
						new Date(DateTime.Parse(splits[4])),
						100.0,
						CurrencyCode.CNY,
						new FixedCoupon(Convert.ToDouble(splits[1])),
						CalendarImpl.Get("chn_ib"),
						splits[2] == "Annual" ? Frequency.Annual : Frequency.SemiAnnual,
						Stub.LongEnd,
						new ActActIsma(), 
						new Act365(),
						BusinessDayConvention.None,
						BusinessDayConvention.ModifiedFollowing,
						new DayGap("0BD"),
						TradingMarket.ChinaInterBank
						);
				}).ToArray();

			var bondFuture = new BondFutures(
				"TF1606",
				new Date(2016, 07, 25),
				new Date(2016, 09, 14),
				new Date(2016, 09, 09),
				CalendarImpl.Get("chn"),
				deliverableBonds,
				new Act365()
				);

			var priceQuoteArray = new PriceQuoteType[] { PriceQuoteType.Basis, PriceQuoteType.NetBasis, PriceQuoteType.Irr};
			var engine = new BondFuturesEngine<HullWhite>(new HullWhite(0.1, 0.01), 20);

			Dictionary<string, Dictionary<string, double>> expectResults = new Dictionary<string, Dictionary<string, double>>()
			{
				{"019119", new Dictionary<string, double>(){{"Irr", -0.077030},{"BondCleanPrice", 107.3},{"BondDirtyPrice", 109.0059},{"Basis", 1.7095},{"NetBasis", 1.5239},{"Spread", -0.115643},{"Margin", -1.212},{"AiStart", 1.7059},{"AiEnd", 0.2883},{"Coupon", 1.9650},{"timeWeightedCoupon",0.1454},{"InterestIncome", 0.4202},{"PnL", 0.1855},{"InvoicePrice", 105.8789},{"FuturesPrice", 101.315}}},
				{"019320", new Dictionary<string, double>(){{"Irr", -0.054963},{"BondCleanPrice", 106.8299},{"BondDirtyPrice", 109.9658},{"Basis", 1.4116},{"NetBasis", 1.2133},{"Spread", -0.115643},{"Margin", -1.212},{"AiStart", 3.1359},{"AiEnd", 3.7030},{"Coupon", 0},{"timeWeightedCoupon",0},{"InterestIncome", 0.4202},{"PnL", 0.1984},{"InvoicePrice", 109.1213},{"FuturesPrice", 101.315}}},
				{"010512", new Dictionary<string, double>(){{"Irr", -0.007217},{"BondCleanPrice", 104.4902},{"BondDirtyPrice", 105.1944},{"Basis", 0.6119},{"NetBasis", 0.4588},{"Spread", -0.115643},{"Margin", -1.212},{"AiStart", 0.7042},{"AiEnd", 1.2101},{"Coupon", 0},{"timeWeightedCoupon",0},{"InterestIncome", 0.4202},{"PnL", 0.1531},{"InvoicePrice", 105.0883},{"FuturesPrice", 101.315}}}
			};

			foreach (var priceQuoteRequest in priceQuoteArray)
			{
				foreach (var expectResult in expectResults)
				{
					var expectTradeId = expectResult.Key;
					IMarketCondition market = null;
					var priceQuote = bondPrices.ToDictionary(x => x.Key, y => y.Value);
					if (priceQuoteRequest == PriceQuoteType.Irr)
					{
						market = TestMarket("2016-07-25", priceQuote, "Simple", 0.024);
					}
					else
					{
						priceQuote["TF1606_" + expectTradeId] = Tuple.Create(priceQuoteRequest, expectResults[expectTradeId][priceQuoteRequest.ToString()]);
						market = TestMarket("2016-07-25", priceQuote, "Simple", 0.024);
					}
					foreach (var deliverable in bondFuture.Deliverables)
					{
						market.MktQuote.Value[bondFuture.Id + "_" + deliverable.Id] = Tuple.Create(PriceQuoteType.Irr, expectResults[deliverable.Id]["Irr"]);
					}

					var result = engine.Calculate(bondFuture, market, PricingRequest.FairQuote);

					Assert.AreEqual(expectResults[expectTradeId]["Irr"], result.ProductSpecific["Irr"][expectTradeId].Rate, 1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["Basis"], result.ProductSpecific["Basis"][expectTradeId].Rate, 1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["NetBasis"], result.ProductSpecific["NetBasis"][expectTradeId].Rate, 1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["AiStart"], result.ProductSpecific["AiStart"][expectTradeId].Rate, 1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["AiEnd"], result.ProductSpecific["AiEnd"][expectTradeId].Rate, 1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["PnL"], result.ProductSpecific["PnL"][expectTradeId].Rate, 1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["InvoicePrice"], result.ProductSpecific["InvoicePrice"][expectTradeId].Rate, 1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["Coupon"], result.ProductSpecific["Coupon"][expectTradeId].Rate, 1e-4);
					Assert.AreEqual(expectResults[expectTradeId]["timeWeightedCoupon"], result.ProductSpecific["TimeWeightedCoupon"][expectTradeId].Rate, 1e-3);
					Assert.AreEqual(expectResults[expectTradeId]["FuturesPrice"], result.ProductSpecific["FuturesPrice"][expectTradeId].Rate, 1e-3);
				}

			}
		}

		[TestMethod]
		public void MktQuoteTest()
		{
			var bondPrices = new Dictionary<string, Tuple<PriceQuoteType, double>>();
			var deliverableBonds = File.ReadAllLines(@"./Data/BondFuture/QbTF1606CfTest.txt")
				.Select(x =>
				{
					var splits = x.Split(',');
					bondPrices[splits[0]] = Tuple.Create(PriceQuoteType.Ytm, Convert.ToDouble(splits[8]));
					return new Bond(
						splits[0],
						new Date(DateTime.Parse(splits[3])),
						new Date(DateTime.Parse(splits[4])),
						100.0,
						CurrencyCode.CNY,
						new FixedCoupon(Convert.ToDouble(splits[1])),
						CalendarImpl.Get("chn_ib"),
						splits[2] == "Annual" ? Frequency.Annual : Frequency.SemiAnnual,
						Stub.LongEnd,
						new Act365(),
						new Act365(),
						BusinessDayConvention.None,
						BusinessDayConvention.ModifiedFollowing,
						new DayGap("0BD"),
						TradingMarket.ChinaInterBank
						);
				}).ToArray();

			var bondFuture = new BondFutures(
				"TF1606",
				new Date(2016, 04, 08),
				new Date(2016, 06, 15),
				new Date(2016, 06, 10),
				CalendarImpl.Get("chn"),
				deliverableBonds,
				new Act365()
				);

			bondPrices.Add("TF1606", Tuple.Create(PriceQuoteType.Dirty, 100.66));
			var market = TestMarket("2016-04-29", bondPrices, "Simple");
			foreach (var deliverableBond in deliverableBonds)
			{
				market.MktQuote.Value.Add(bondFuture.Id + "_" + deliverableBond.Id, Tuple.Create(PriceQuoteType.Irr, 0.025));
			}
			//market = market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, bondPrices));
			var engine = new BondFuturesEngine<HullWhite>(new HullWhite(0.1, 0.01), 20);
			var result = engine.Calculate(bondFuture, market, PricingRequest.Irr);

			Dictionary<string, Dictionary<string, double>> expectResults = new Dictionary<string, Dictionary<string, double>>()
			{
				{"160002", new Dictionary<string, double>(){{"Irr", -0.091403},{"BondCleanPrice", 100.1693},{"BondDirtyPrice", 100.9021}}},
				{"150019", new Dictionary<string, double>(){{"Irr", -0.067332},{"BondCleanPrice", 102.513},{"BondDirtyPrice", 104.5205}}},
				{"140013", new Dictionary<string, double>(){{"Irr", -0.074859},{"BondCleanPrice", 107.0109},{"BondDirtyPrice", 110.317}}},
				{"140006", new Dictionary<string, double>(){{"Irr", -0.082592},{"BondCleanPrice", 108.3015},{"BondDirtyPrice", 108.61}}},
				{"140003", new Dictionary<string, double>(){{"Irr", -0.086425},{"BondCleanPrice", 108.5624},{"BondDirtyPrice", 109.8241}}},
				{"130020", new Dictionary<string, double>(){{"Irr", -0.087293},{"BondCleanPrice", 106.7046},{"BondDirtyPrice", 108.8731}}},
				{"130015", new Dictionary<string, double>(){{"Irr", -0.071649},{"BondCleanPrice", 103.8394},{"BondDirtyPrice", 106.6093}}},
				{"110019", new Dictionary<string, double>(){{"Irr", -0.075644},{"BondCleanPrice", 106.663},{"BondDirtyPrice", 107.4296}}},
				{"110008", new Dictionary<string, double>(){{"Irr", -0.087619},{"BondCleanPrice", 106.0245},{"BondDirtyPrice", 106.472}}},
				{"110002", new Dictionary<string, double>(){{"Irr", -0.088161},{"BondCleanPrice", 106.4155},{"BondDirtyPrice", 107.4979}}},
				{"100034", new Dictionary<string, double>(){{"Irr", -0.091778},{"BondCleanPrice", 105.0912},{"BondDirtyPrice", 105.1012}}},
				{"100031", new Dictionary<string, double>(){{"Irr", -0.090643},{"BondCleanPrice", 103.4498},{"BondDirtyPrice", 103.8432},{"Basis", 1.6322},{"NetBasis", 1.5463},{"Spread", -0.115643},{"Margin", -1.212},{"AiStart", 0.3934},{"AiEnd", 0.8136},{"Coupon", 0},{"InterestIncome", 0.4202},{"PnL", 0.0859},{"InvoicePrice", 102.6312}}},
				{"100024", new Dictionary<string, double>(){{"Irr", -0.0891},{"BondCleanPrice", 103.3754},{"BondDirtyPrice", 104.1324}}},
				{"050012", new Dictionary<string, double>(){{"Irr", -0.089582},{"BondCleanPrice", 105.0324},{"BondDirtyPrice", 106.697}}}
			};

			foreach (var expectResult in expectResults)
			{
				var irr = result.ProductSpecific["Irr"][expectResult.Key].Rate;
				var bondCleanPrice = result.ProductSpecific["BondCleanPrice"][expectResult.Key].Rate;
				var bondDirtyPrice = result.ProductSpecific["BondDirtyPrice"][expectResult.Key].Rate;
				Console.WriteLine("{0}: IRR: {1}", expectResult.Key, irr);
				Console.WriteLine("{0}: BondCleanPrice: {1}", expectResult.Key, bondCleanPrice);
				Console.WriteLine("{0}: BondDirtyPrice: {1}", expectResult.Key, bondDirtyPrice);
			}
		}

        public IMarketCondition TestMarket(string valueDate, Dictionary<string, Tuple<PriceQuoteType, double>> bondPrices = null,
            string compound = "Continuous", double fundingRate = 0.025, double reinvestmentRate = 0.025, 
            string interpolation = "CubicHermiteMonotic",  Dictionary<string, double> futurePrice = null)
		{
			var historiclIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates;
            var rateTrait = "Spot";
            var instrumentType = "None";

            var curveConvention = new CurveConvention("fr007CurveConvention",
				"CNY",
				"ModifiedFollowing",
				"Chn_ib",
				"Act365",
				compound,
                interpolation);

			var reinvestmentCurveConvention = new CurveConvention("reinvestmentCurveConvention",
				"CNY",
				"ModifiedFollowing",
				"Chn_ib",
				"Act365",
				"Simple",
                interpolation);

			var fr007CurveName = "Fr007";
			var fr007RateDefinition = new[]
			{
				new RateMktData("1D", fundingRate, rateTrait, instrumentType, fr007CurveName),
                new RateMktData("4Y", fundingRate, rateTrait, instrumentType, fr007CurveName),
                new RateMktData("5Y", fundingRate, rateTrait, instrumentType, fr007CurveName),
			};

			var reinvestmentCurveName = "reinvestment";
			var reinvestmentRateDefinition = new[]
			{
				new RateMktData("1D", reinvestmentRate, rateTrait, instrumentType, reinvestmentCurveName),
                new RateMktData("2Y", reinvestmentRate, rateTrait, instrumentType, reinvestmentCurveName),
                new RateMktData("3Y", reinvestmentRate, rateTrait, instrumentType, reinvestmentCurveName),
                new RateMktData("4Y", reinvestmentRate, rateTrait, instrumentType, reinvestmentCurveName),
                new RateMktData("5Y", reinvestmentRate, rateTrait, instrumentType, reinvestmentCurveName),
                new RateMktData("6Y", reinvestmentRate, rateTrait, instrumentType, reinvestmentCurveName),
            };

			var curveDefinition = new[]
			{
				new InstrumentCurveDefinition(fr007CurveName, curveConvention, fr007RateDefinition, "SpotCurve"),
				new InstrumentCurveDefinition(reinvestmentCurveName, reinvestmentCurveConvention, reinvestmentRateDefinition, "SpotCurve")
			};

			var marketInfo = new MarketInfo("tmpMarket", valueDate, curveDefinition, historiclIndexRates);
			QdpMarket market;
			MarketFunctions.BuildMarket(marketInfo, out market);

            var futQuotes = new Dictionary<string, Tuple<PriceQuoteType, double>>();
            if (futurePrice != null) {
                var futPriceKvPair = futurePrice.First();
                futQuotes.Add(futPriceKvPair.Key, Tuple.Create(PriceQuoteType.Dirty, futPriceKvPair.Value));
            }

            return new MarketCondition(
				x => x.ValuationDate.Value = market.ReferenceDate,
				x => x.DiscountCurve.Value = market.GetData<CurveData>(reinvestmentCurveName).YieldCurve,
				x => x.FixingCurve.Value = market.GetData<CurveData>(fr007CurveName).YieldCurve,
				x => x.RiskfreeCurve.Value = market.GetData<CurveData>(fr007CurveName).YieldCurve,
				x => x.MktQuote.Value = bondPrices ?? futQuotes,
				x => x.HistoricalIndexRates.Value = new Dictionary<IndexType, SortedDictionary<Date, double>>()
				);
		}
	}
}
