using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Trade.FixedIncome;
using Qdp.Pricing.Ecosystem.ExcelWrapper;
using Qdp.ComputeService.Data.CommonModels.TradeInfos;
using UnitTest.Utilities;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Utilities.Coupons;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Common.Market;

namespace UnitTest.CommonTests
{ 
	[TestClass]
	public class BondTest
	{  
		[TestMethod]
		public void Test()
		{
			var d = 1000.0;
			var f = 1/Math.Pow(d, 20);
			Console.WriteLine(f);
		}

        [TestMethod]
        public void BasicBondTest()
        {
            var bond = new Bond(
                id: "bond",
                startDate: new Date(2016, 3, 15),
                maturityDate: new Date(2019, 3, 15),
                notional: 100.0,
                currency: CurrencyCode.CNY,
                coupon: new FixedCoupon(0.05),
                calendar: CalendarImpl.Get("chn"),
                paymentFreq: Frequency.SemiAnnual,
                stub: Stub.ShortEnd,
                accrualDayCount: new Act365(),
                paymentDayCount: new Act365(),
                accrualBizDayRule: BusinessDayConvention.ModifiedFollowing,
                paymentBizDayRule: BusinessDayConvention.ModifiedFollowing,
                settlementGap: new DayGap("+0D"),
                bondTradingMarket: TradingMarket.ChinaInterBank);

            var engine = new BondEngineCn(new BondYieldPricerCn());
            var market = new MarketCondition(
                    x => x.ValuationDate.Value = new Date(2018, 8, 1),
                    x => x.MktQuote.Value = 
                        new Dictionary<string, Tuple<PriceQuoteType, double>>
                        {
                            { "bond", Tuple.Create(PriceQuoteType.Clean, 100.0) }
                        }
                );
            var result = engine.Calculate(bond, market, PricingRequest.Ytm);
        }

		[TestMethod]
		public void ReviewSchedule()
		{
			var nUnits = 50;
			var nDays = nUnits + 8;
			var lists = new List<string>[nDays];
			for (var i = 0; i < nDays; ++i)
			{
				lists[i] = new List<string>();
			}

			for (var i = 1; i < nUnits; ++i)
			{
				lists[i].Add(string.Format("{0}{1}{2}", "第", i, "单元"));
				lists[i+1].Add(string.Format("{0}{1}{2}", "第", i, "单元"));
				lists[i+3].Add(string.Format("{0}{1}{2}", "第", i, "单元"));
				lists[i+6].Add(string.Format("{0}{1}{2}", "第", i, "单元"));
			}

			for (var j = 1; j < lists.Length; ++j)
			{
				Console.WriteLine(string.Format("{0}{1}{2}{3}", "第", j, "天", lists[j].Aggregate("", (current, v) => current + "," + v)));
			}
		}

		[TestMethod]
		public void TestFixedDateCouponAdjustedBond1()
		{
			var bond = new FixedDateCouonAdjustedBondInfo("313100015")
			{
				StartDate = "2015-03-23",
				MaturityDate = "2018-11-17",
				PaymentFreq = "Quarterly",
				Notional = 32000,
				AccrualDC = "Act365",
				AccrualBD = "None",
				DayCount = "Act365",
				PaymentBD = "None",
				FirstPaymentDate = "2015-06-21",
				Index = "Lrb5Y",
				FloatingRateMultiplier = 0.9,
				FixedDateCouponAdjustedStyle = "SpecifiedDates",
				AdjustMmDd = "11-18",
				AmoritzationInDate = new Dictionary<string, double>
				{
					{"2015-06-20", 0.125}, //percentage of initial Notional
					{"2015-12-20", 0.125},
					{"2016-06-20", 0.125},
					{"2016-12-20", 0.125},
					{"2017-06-20", 0.125},
					{"2017-12-20", 0.125},
					{"2018-06-20", 0.125},
					{"2018-11-17", 0.125},
				},

				//Settlment = "+0BD",
				//PaymentStub = "ShortEnd",
				//Calendar = "chn_ib",
				//Currency = "CNY",
				//TradingMarket = "ChinaInterBank",
				//IsZeroCouponBond = false,
				//IssuePrice = 100,
				//ValuationParamters = new SimpleCfValuationParameters("Fr007", "", "Fr007")
			};
			var marketInfo = TestMarket("2015-03-23", new BondMktData(bond.TradeId, "Dirty", bond.Notional));
			var bondVf = new BondVf(bond);

			var result = bondVf.ValueTrade(marketInfo, PricingRequest.Cashflow);
			var expectedResults = new[]
			{
				"Coupon 459.9715 CNY@2015-06-20",
				"Coupon 4.5222 CNY@2015-06-21",
				"Coupon 416.0416 CNY@2015-09-21",
				"Coupon 392.6367 CNY@2015-12-20",
				"Coupon 3.4915 CNY@2015-12-21",
				"Coupon 317.7271 CNY@2016-03-21",
				"Coupon 317.7271 CNY@2016-06-20",
				"Coupon 2.9096 CNY@2016-06-21",
				"Coupon 267.6822 CNY@2016-09-21",
				"Coupon 261.863 CNY@2016-12-20",
				"Coupon 2.3277 CNY@2016-12-21",
				"Coupon 209.4904 CNY@2017-03-21",
				"Coupon 211.8181 CNY@2017-06-20",
				"Coupon 1.7458 CNY@2017-06-21",
				"Coupon 160.6093 CNY@2017-09-21",
				"Coupon 157.1178 CNY@2017-12-20",
				"Coupon 1.1638 CNY@2017-12-21",
				"Coupon 104.7452 CNY@2018-03-21",
				"Coupon 105.909 CNY@2018-06-20",
				"Coupon 0.5819 CNY@2018-06-21",
				"Coupon 53.5364 CNY@2018-09-21",
				"Coupon 33.1693 CNY@2018-11-17",
				"Principal 4000 CNY@2015-06-20",
				"Principal 4000 CNY@2015-12-20",
				"Principal 4000 CNY@2016-06-20",
				"Principal 4000 CNY@2016-12-20",
				"Principal 4000 CNY@2017-06-20",
				"Principal 4000 CNY@2017-12-20",
				"Principal 4000 CNY@2018-06-20",
				"Principal 4000 CNY@2018-11-17",
			};
			for (var i = 0; i < result.Cashflows.Length; ++i)
			{
				Assert.AreEqual(result.Cashflows[i].ToString(), expectedResults[i]);
			}
		}

		[TestMethod]
		public void TestFixedDateCouponAdjustedBond2()
		{
			var bond = new FixedDateCouonAdjustedBondInfo("1141100129")
			{
				StartDate = "2015-03-23",
				MaturityDate = "2018-04-24",
				PaymentFreq = "Quarterly",
				Notional = 4000,
				AccrualDC = "Act365",
				AccrualBD = "None",
				DayCount = "Act365",
				PaymentBD = "None",
				FirstPaymentDate = "2015-06-21",
				Index = "Lrb5Y",
				FloatingRateMultiplier = 1.0,
				FixedDateCouponAdjustedStyle = "Follow",
				AdjustMmDd = "11-08",
				AmoritzationInDate = new Dictionary<string, double>
				{
					{"2015-05-18", 0.25}, //percentage of initial Notional
					{"2015-11-19", 0.25},
					{"2016-05-18", 0.5}
				},

				//Settlment = "+0BD",
				//PaymentStub = "ShortEnd",
				//Calendar = "chn_ib",
				//Currency = "CNY",
				//TradingMarket = "ChinaInterBank",
				//IsZeroCouponBond = false,
				//IssuePrice = 100,
				//ValuationParamters = new SimpleCfValuationParameters("Fr007", "", "Fr007")
			};
			var marketInfo = TestMarket("2015-03-23", new BondMktData(bond.TradeId, "Dirty", bond.Notional));
			var bondVf = new BondVf(bond);

			var result = bondVf.ValueTrade(marketInfo, PricingRequest.Cashflow);

			var expectedResults = new[]
			{
				"Coupon 36.2082 CNY@2015-05-18",
				"Coupon 16.4877 CNY@2015-06-21",
				"Coupon 44.6137 CNY@2015-09-21",
				"Coupon 28.611 CNY@2015-11-19",
				"Coupon 10.3452 CNY@2015-12-21",
				"Coupon 29.4192 CNY@2016-03-21",
				"Coupon 18.7507 CNY@2016-05-18",
				"Coupon 0 CNY@2016-06-21",
				"Coupon 0 CNY@2016-09-21",
				"Coupon 0 CNY@2016-12-21",
				"Coupon 0 CNY@2017-03-21",
				"Coupon 0 CNY@2017-06-21",
				"Coupon 0 CNY@2017-09-21",
				"Coupon 0 CNY@2017-12-21",
				"Coupon 0 CNY@2018-03-21",
				"Coupon 0 CNY@2018-04-24",
				"Principal 1000 CNY@2015-05-18",
				"Principal 1000 CNY@2015-11-19",
				"Principal 2000 CNY@2016-05-18",
			};
			for (var i = 0; i < result.Cashflows.Length; ++i)
			{
				Assert.AreEqual(result.Cashflows[i].ToString(), expectedResults[i]);
			}
		}

		[TestMethod]
		public void TestBondPriceConversions()
		{
			var targetDirtyPrice = new List<double>();
			var targetCleanPrice = new List<double>();
			var targetYtm = new List<double>();
			var fixedBonds = File.ReadAllLines(@"./Data/BondFuture/QbTF1606CfTest.txt")
					.Select(x =>
					{
						var splits = x.Split(',');
						targetDirtyPrice.Add(Convert.ToDouble(splits[6]));
						targetCleanPrice.Add(Convert.ToDouble(splits[7]));
						targetYtm.Add(Convert.ToDouble(splits[8]));

						return new FixedRateBondInfo(splits[0])
						{
							StartDate = new Date(DateTime.Parse(splits[3])).ToString(),
							MaturityDate = new Date(DateTime.Parse(splits[4])).ToString(),
							Notional = 100.0,
							Currency = "CNY",
							FixedCoupon = Convert.ToDouble(splits[1]),
							Calendar = "chn_ib",
							PaymentFreq = (splits[2] == "Annual" ? Frequency.Annual : Frequency.SemiAnnual).ToString(),
							PaymentStub = "LongEnd",
							AccrualDC = "ActActIsma",
							DayCount = "ActActIsma",
							AccrualBD = "None",
							PaymentBD = "None",
							TradingMarket = "ChinaInterBank",
							Settlement = "+0D",
							ValuationParamters = new SimpleCfValuationParameters("Fr007", null, "Fr007")
						};
					}).ToArray();

			for (var i = 0; i < fixedBonds.Length; ++i)
			{
				var bondVf = new BondVf(fixedBonds[i]);
				var markets = new[]
				{
					TestMarket("2016-04-06", new BondMktData(fixedBonds[i].TradeId, "Dirty", targetDirtyPrice[i])),
					TestMarket("2016-04-06", new BondMktData(fixedBonds[i].TradeId, "Clean", targetCleanPrice[i])),
					TestMarket("2016-04-06", new BondMktData(fixedBonds[i].TradeId, "Ytm", targetYtm[i]))
				};
				foreach (var qdpMarket in markets)
				{
					var result = bondVf.ValueTrade(qdpMarket, PricingRequest.Ytm);
					Assert.AreEqual(Math.Round(result.DirtyPrice, 4), targetDirtyPrice[i], 1e-5);
					Assert.AreEqual(Math.Round(result.CleanPrice, 4), targetCleanPrice[i], 1e-5);
					Assert.AreEqual(Math.Round(result.Ytm, 6), targetYtm[i], 1e-7);
				}
			}

		}

        [TestMethod]
        public void TestConvertibleBondWhenCallOnMaturity()
        {
            var bond = new FixedRateBondInfo("110030.SH")
            {
                StartDate = "2014-12-25",
                MaturityDate = "2019-12-25",
                Notional = 100.0,
                Currency = "CNY",
                FixedCoupon = 0.006,
                Calendar = "chn",
                PaymentFreq = "Annual",
                PaymentStub = "ShortStart",
                AccrualDC = "Act365NoLeap",
                DayCount = "ModifiedAfb",
                AccrualBD = "None",
                PaymentBD = "None",
                TradingMarket = "ChinaExShg",
                Settlement = "+0BD",
                CompensationRate = new Dictionary<int, double>()
                {
                    {2, 0.002 }, {3, 0.002}, {4, 0.005}, {5, 0.005}
                },
                ValuationParamters = new SimpleCfValuationParameters("Fr007", null, "Fr007"),
                RedemptionRate = 1.06,
                RedemptionIncludeLastCoupon = true
            };

            var bondVf = new BondVf(bond);
            var market = TestMarket("2017-10-20", new BondMktData(bond.TradeId, "Dirty", 95.90891));

            //var result = bondVf.ValueTrade(market, PricingRequest.Ytm);
            XlManager.AddTrades(new TradeInfoBase[] { bond });
            var ytm = XlUdf.xl_YieldFromPrice("110030.SH", "2017-10-20", 95.90891);
        }

		[TestMethod]
		public void TestBondPriceConversions2()
		{
			var bond = new FixedRateBondInfo("bond1")
			{
				StartDate = "2012-08-16",
				MaturityDate = "2017-08-16",
				Notional = 100.0,
				Currency = "CNY",
				FixedCoupon = 0.0295,
				Calendar = "chn_ib",
				PaymentFreq = "Annual",
				PaymentStub = "ShortStart",
				AccrualDC = "Act365NoLeap",
				DayCount = "ModifiedAfb",
				AccrualBD = "None",
				PaymentBD = "None",
				TradingMarket = "ChinaExShg",
				Settlement = "+0BD",
				ValuationParamters = new SimpleCfValuationParameters("Fr007", null, "Fr007")
			};

			var bondVf = new BondVf(bond);
			var market = TestMarket("2016-08-04", new BondMktData(bond.TradeId, "Dirty", 102.86109589041));

			var result = bondVf.ValueTrade(market, PricingRequest.Ytm);
			Assert.AreEqual(Math.Round(result.Ytm, 15), 0.029405797502054, 1e-13);
		}

		[TestMethod]
		public void TestBondPriceConversions3()
		{
			var bond = new FixedRateBondInfo("bond1")
			{
				StartDate = "2010-10-28",
				MaturityDate = "2020-10-28",
				Notional = 100.0,
				Currency = "CNY",
				FixedCoupon = 0.0367,
				Calendar = "chn",
				PaymentFreq = "SemiAnnual",
				PaymentStub = "ShortStart",
				AccrualDC = "Act365NoLeap",
				DayCount = "ModifiedAfb",
				AccrualBD = "None",
				PaymentBD = "None",
				TradingMarket = "ChinaExShg",
				Settlement = "+0BD",
				ValuationParamters = new SimpleCfValuationParameters("Fr007", null, "Fr007")
			};

			var bondVf = new BondVf(bond);
			var market = TestMarket("2016-08-04", new BondMktData(bond.TradeId, "Dirty", 100.995424657531));

			var result = bondVf.ValueTrade(market, PricingRequest.Ytm);
			Assert.AreEqual(Math.Round(result.Ytm, 15), 0.036663932289318, 1e-13);
		}

		[TestMethod]
		public void TestStepWiseCompensationRate()
		{
            //为什么没输入tradedate？应该有tradedate为交易属性
			var bond = new FixedRateBondInfo("bond1")
			{
				StartDate = "2016-10-28",
				MaturityDate = "2020-10-28",
				Notional = 100.0,  //trade
				Currency = "CNY",
				FixedCoupon = 0.0367,
				Calendar = "chn",
				PaymentFreq = "Annual",  //付息频率
				PaymentStub = "ShortStart", //instrument 
				AccrualDC = "ActActIsma",  // 日期规则，应计利息
				DayCount = "ModifiedAfb",  // 收益率的日期规则
				AccrualBD = "None",     //应计利息 日期调整规则 instrument
				PaymentBD = "None",     //支付利息 日期调整规则 
				TradingMarket = "ChinaExShg", //交易市场 
				Settlement = "+0BD",          //BD工作日， 后可以拿到交易的东西 instrument
				CompensationRate = new Dictionary<int, double> // :
//补偿利率的意思是有些债券会根据年限的变化增长利息
//2，0.01是指第二年开始利率增加0.01

                {
					{2, 0.01},
					{3, 0.015}
				},
				ValuationParamters = new SimpleCfValuationParameters("Fr007", null, "Fr007")
			};

			var bondVf = new BondVf(bond);
			var market = TestMarket("2016-08-04", new BondMktData(bond.TradeId, "Dirty", 100.995424657531));

			var result = bondVf.ValueTrade(market, PricingRequest.Cashflow);
			var nettedCfs = result.Cashflows.GroupBy(cf => cf.PaymentDate)
					.Select(item => new Cashflow(item.Min(x => x.AccrualStartDate), item.Max(x => x.AccrualEndDate), item.Key, item.Sum(entry => entry.PaymentAmount), item.First().PaymentCurrency, CashflowType.Coupon, item.Aggregate(true, (current, v) => current && v.IsFixed), double.NaN, item.Min(x => x.CalculationDetails), item.Min(x => x.RefStartDate), item.Max(x => x.RefEndDate), item.Max(entry => entry.StartPrincipal), item.Sum(entry => entry.CouponRate)))
					.OrderBy(cf => cf.PaymentDate)
					.ToArray();
			Assert.AreEqual(nettedCfs[0].PaymentAmount, 3.67, 1e-3);
			Assert.AreEqual(nettedCfs[1].PaymentAmount, 4.67, 1e-3);
			Assert.AreEqual(nettedCfs[2].PaymentAmount, 6.17, 1e-3);
			Assert.AreEqual(nettedCfs[3].PaymentAmount, 106.17, 1e-3);



       

            //Date tradedate = new Date(2016, 11, 28);
            //Date startdate = new Date(2016, 10, 28);
            //Date maturitydate = new Date(2020, 10, 28);
            //var bond_v2 = new V2.FixedRateBondInfo("bond1",tradedate, startdate,maturitydate, V2.TradeType.Buy, 100, 1000);

            //var bond2s = CreateBonds();
            //var singlebond = bond2s[0];
            //var marketV2 = CreateTestMarket("2016-11-28", singlebond);
            //IPricingResult ipResult = bond_v2.CalculateRisks(marketV2, PricingRequest.Cashflow);

            //var nettedCfs2 = ipResult.Cashflows.GroupBy(cf => cf.PaymentDate)
            //        .Select(item => new Cashflow(item.Min(x => x.AccrualStartDate), item.Max(x => x.AccrualEndDate), item.Key, item.Sum(entry => entry.PaymentAmount), item.First().PaymentCurrency, CashflowType.Coupon, item.Aggregate(true, (current, v) => current && v.IsFixed), double.NaN, item.Min(x => x.CalculationDetails), item.Min(x => x.RefStartDate), item.Max(x => x.RefEndDate), item.Max(entry => entry.StartPrincipal), item.Sum(entry => entry.CouponRate)))
            //        .OrderBy(cf => cf.PaymentDate)
            //        .ToArray();
            //string results = "0:" + nettedCfs2[0].PaymentAmount + "1:" + nettedCfs2[1].PaymentAmount  +"2:" + nettedCfs2[2].PaymentAmount +"3:" + nettedCfs2[3].PaymentAmount; ;
            //System.Diagnostics.Debugger.Log(2, "results", results);
        }




		[TestMethod]
		public void TestFloatingBondPriceConversions()
		{
			var targetDirtyPrice = new List<double>();
			//var targetCleanPrice = new List<double>();
			//var targetYtm = new List<double>();

			var floatingBonds = File.ReadAllLines(@"./Data/BondFuture/QbTF1606CfTest.txt")
					.Select(x =>
					{
						var splits = x.Split(',');
						targetDirtyPrice.Add(Convert.ToDouble(splits[6]));
						//targetCleanPrice.Add(Convert.ToDouble(splits[7]));
						//targetYtm.Add(Convert.ToDouble(splits[8]));

						return new FloatingRateBondInfo(splits[0])
						{
							StartDate = new Date(DateTime.Parse(splits[3])).ToString(),
							MaturityDate = new Date(DateTime.Parse(splits[4])).ToString(),
							Notional = 100.0,
							Currency = "CNY",
							Spread = Convert.ToDouble(splits[1]),
							Calendar = "chn_ib",
							PaymentFreq = (splits[2] == "Annual" ? Frequency.Annual : Frequency.SemiAnnual).ToString(),
							PaymentStub = "LongEnd",
							AccrualDC = "Act365",
							DayCount = "Act365",
							AccrualBD = "None",
							PaymentBD = "ModifiedFollowing",
							TradingMarket = "ChinaInterBank",
							Settlement = "+0BD",
							ValuationParamters = new SimpleCfValuationParameters("Fr007", "Fr007", "Fr007"),

							Index = IndexType.Fr007.ToString(),
							ResetDC = DayCount.Act365.ToString(),
							ResetAverageDays = 1,
							ResetTerm = "1W",
							ResetStub = Stub.LongEnd.ToString(),
							ResetBD = BusinessDayConvention.ModifiedFollowing.ToString(),
							ResetToFixingGap = "-1BD",
							FloatingCalc = "SimpleFrn",
							ResetCompound = CouponCompound.Simple.ToString()
						};
					}).ToArray();

			for (var i = 0; i < floatingBonds.Length; ++i)
			{
				var bondVf = new BondVf(floatingBonds[i]);
				var markets = new[]
				{
					TestMarket("2014-10-21", new BondMktData(floatingBonds[i].TradeId, "Dirty", targetDirtyPrice[i])),
					//TestMarket("2014-10-21", new BondMktData(floatingBonds[i].TradeId, "Clean", targetCleanPrice[i])),
					//TestMarket("2014-10-21", new BondMktData(floatingBonds[i].TradeId, "Ytm", targetYtm[i]))
				};
				foreach (var qdpMarket in markets)
				{
					var result = bondVf.ValueTrade(qdpMarket, PricingRequest.Ytm);
					Console.WriteLine("DirtyPrice:{0}", result.DirtyPrice);
					Console.WriteLine("CleanPrice:{0}", result.CleanPrice);
					Console.WriteLine("Ytm:{0}", result.Ytm);
					//Assert.AreEqual(targetDirtyPrice[i], Math.Round(result.DirtyPrice, 4), 1e-5);
					//Assert.AreEqual(targetCleanPrice[i], Math.Round(result.CleanPrice, 4), 1e-5);
					//Assert.AreEqual(targetYtm[i], Math.Round(result.Ytm, 6), 1e-7);
				}
			}
		}

		[TestMethod]
		public void TestCurrentPrimeRate()
		{
			var bond = new FloatingRateBondInfo("N0000472015ABSLBS0103.CIB")
			{
				StartDate = "20150910",
				MaturityDate = "2017-07-26",
				Notional = 100.0,
				Currency = "CNY",
				Spread = 0.029900000000000003,
				Calendar = "chn_ib",
				PaymentFreq = "Quarterly",
				PaymentStub = "ShortStart",
				AccrualDC = "ActActIsma",
				DayCount = "ActActIsma",
				AccrualBD = "None",
				PaymentBD = "None",
				TradingMarket = "ChinaInterBank",
				Settlement = "+0D",
				ValuationParamters = new SimpleCfValuationParameters("Fr007", "Fr007", "Fr007"),

				Index = IndexType.Depo1Y.ToString(),
				ResetDC = DayCount.ActActIsma.ToString(),
				ResetAverageDays = 1,
				ResetTerm = "3M",
				ResetStub = Stub.ShortStart.ToString(),
				ResetBD = BusinessDayConvention.None.ToString(),
				ResetToFixingGap = "-0dD",
				FloatingCalc = "ZzFrn",
				ResetCompound = CouponCompound.Simple.ToString(),
				ResetRateDigits = 4
			};

			var bondVf = new BondVf(bond);
			var bondInstrument = bondVf.GenerateInstrument();
			var valueDate = new Date(2017,1,9);
			var market = TestMarket(valueDate.ToString(), new BondMktData(bond.TradeId, "Dirty", 100.995424657531));

			var marketCondition = bondVf.GenerateMarketCondition(market);

			var result = bondInstrument.Coupon.GetPrimeCoupon(marketCondition.HistoricalIndexRates, marketCondition.FixingCurve.Value, valueDate);
			Assert.AreEqual(0.015, result.Item2);
			Assert.AreEqual("2016-12-23", result.Item1.ToString());
		}

		public QdpMarket TestMarket(string valueDate, BondMktData bondMktData)
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
				new RateMktData("1D", 0.035, "Spot", "None", fr007CurveName),
				new RateMktData("5Y", 0.035, "Spot", "None", fr007CurveName),
			};


			var curveDefinition = new[]
			{
				new InstrumentCurveDefinition(fr007CurveName, curveConvention, fr007RateDefinition, "SpotCurve"),
			};

			var marketInfo = new MarketInfo("tmpMarket", valueDate, curveDefinition)
			{
				BondMktDatas = new[] { bondMktData },
				HistoricalIndexRates = historiclIndexRates
			};
			QdpMarket market;
			MarketFunctions.BuildMarket(marketInfo, out market);
			return market;
		}


        public QdpMarket CreateTestMarket(string valueDate, Bond bond)
        {
            var historiclIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates;

            var curveConvention = new CurveConvention("fr007CurveConvention",
                "CNY",
                "ModifiedFollowing",
                "Chn_ib",
                "Act365",
                "Continuous",
                "CubicHermiteMonotic");

            var reinvestmentCurveConvention = new CurveConvention("reinvestmentCurveConvention",
                "CNY",
                "ModifiedFollowing",
                "Chn_ib",
                "Act365",
                "Simple",
                "CubicHermiteMonotic");

            var fr007CurveName = "中债国债收益率曲线";
            ///spot是和forward相对的，spot的意思是从现在开始算，forward的意思是从未来某一天开始算。我们通常说利率都是指的spot
            var fr007RateDefinition = new[]
            {
                new RateMktData("1D", 0.025, "Spot", "None", fr007CurveName),
                new RateMktData("5Y", 0.025, "Spot", "None", fr007CurveName),
            };

            var reinvestmentCurveName = "reinvestment";
            var reinvestmentRateDefinition = new[]
            {
                new RateMktData("1D", 0.025, "Spot", "None", reinvestmentCurveName),
                new RateMktData("5Y", 0.025, "Spot", "None", reinvestmentCurveName),
            };

            var curveDefinition = new[]
            {
                new InstrumentCurveDefinition(fr007CurveName, curveConvention, fr007RateDefinition, "SpotCurve"),
                new InstrumentCurveDefinition(reinvestmentCurveName, reinvestmentCurveConvention, reinvestmentRateDefinition, "SpotCurve")
            };

            var marketInfo = new MarketInfo("tmpMarket", valueDate, curveDefinition, historiclIndexRates);
            var bondPrices = new List<BondMktData>();
            var treasuryFuturePrices = new List<TreasuryFutureMktData>();
            //foreach (var bond in bond.Deliverables)
            //{
            //    bondPrices.Add(new BondMktData(bond.Id, "Clean", 100.0));
            //    treasuryFuturePrices.Add(new TreasuryFutureMktData(bond.Id + "_" + bond.Id, "Clean", 100.0));
            //}
            var futurePrices = new List<FuturesMktData>()
            {
                new FuturesMktData(bond.Id, 100.0)
            };

            marketInfo.BondMktDatas = bondPrices.ToArray();
            marketInfo.FuturesMktDatas = futurePrices.ToArray();
            marketInfo.TreasuryFutureMktData = treasuryFuturePrices.ToArray();

            QdpMarket market;
            MarketFunctions.BuildMarket(marketInfo, out market);
            return market;
        }

        public Bond[] CreateBonds()
        {
            var bondPrices = new Dictionary<string, Tuple<PriceQuoteType, double>>();
            var deliverableBonds = File.ReadAllLines(@"./Data/BondFuture/TF1509DeliverableBondInfo.txt")
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

            return deliverableBonds;
        }
    }
}
