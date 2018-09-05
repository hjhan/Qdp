using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Trade.FixedIncome;

namespace UnitTest.CommonTests
{
	[TestClass]
	public class LoanTest
	{
		public const string LoanJson = @"";

		[TestMethod]
		public void TestLoanWithRepurchase()
		{
			#region

			var loanInfo = new LoanInfo
			{
				Amortization = "EqualPrincipal",
				Coupon = 0.18,
				Currency = "CNY",
				DayCount = "Act360",
				FirstPaymentDate = "20150712",
				Frequency = "Monthly",
				IsFloatingRate = false,
				MaturityDate = "20160612",
				Notional = 500000000,
				NumOfPayment = 12,
				StartDate = "20150612",
				MortgageCalcMethod = "Simple",
				AbsPrepaymentModel = "Cpr",
				AbsDefaultModel = "Cdr",
				AnnualCprRate = 0.0,
				AnnualCdrRate = 0.03,
				TaxRate = 0.056
			};

			#endregion

			var maturityDates = new[] { "20150912", "20151212", "20160312", "20160612" };
			var coupons = new[] { 0.05, 0.052, 0.054, 0.055 };
			var notionals = new[] {115000000, 113000000, 110000000, 112000000};
			var bondInfos =
				maturityDates.Select((x, i) =>
					new FixedRateBondInfo("bond" + x)
					{
						StartDate = "20150612",
						MaturityDate = x,
						Notional = notionals[i],
						Currency = "CNY",
						FixedCoupon = coupons[i],
						Calendar = "chn",
						PaymentFreq = "Quarterly",
						PaymentStub = "ShortEnd",
						AccrualDC = "Act365NoLeap",
						DayCount = "Act360",
						AccrualBD = "None",
						PaymentBD = "None",
						TradingMarket = "ChinaInterBank",
						Settlement = "+0D",
						ValuationParamters = new SimpleCfValuationParameters("Fr007", null, "Fr007")
					}).ToArray();

			var loanWithRepurchaseInfo = new AbsWithRepurchaseInfo("test1")
			{
				Tranches = bondInfos,
				LoanInfo = loanInfo,
				RepurchaseRatio = 0.8,
				MaintenanceFeeRate = 0.004,
				ProtectionFeeRate = 0.0015
			};
			var marketInfo = new MarketInfo("testMarket", "20160202");

			QdpMarket qdpMarket;
			MarketFunctions.BuildMarket(marketInfo, out qdpMarket);

			var loanVf = new AbsWithRepurchaseVf(loanWithRepurchaseInfo);
			var market = loanVf.GenerateMarketCondition(qdpMarket);

			var loanWithRepurchase = loanVf.GenerateInstrument();
			var engine = loanVf.GenerateEngine();
			var result = engine.Calculate(loanWithRepurchase, market, PricingRequest.Cashflow);
			foreach (var cashflow in result.Cashflows)
			{
				Console.WriteLine("{0},{1},{2}", cashflow.PaymentDate, cashflow.CashflowType, cashflow.PaymentAmount);
			}
		}

		[TestMethod]
		public void TestLoanPayment()
		{
			var loanInfo = new LoanInfo()
			{
				Amortization = "EqualPrincipalAndInterest",
				Coupon = 0.045,
				Currency = "CNY",
				DayCount = "Act360",
				FirstPaymentDate = "20141209",
				FloatingRateMultiplier = 1,
				Frequency = "Monthly",
				IndexType = "Gjj5YAbove",
				IsFloatingRate = true,
				MaturityDate = "20320409",
				Notional = 294150.32,
				NumOfPayment = 209,
				ResetDate = "20150101",
				StartDate = "20141109",
				MortgageCalcMethod = "Simple",
				AbsPrepaymentModel = "Psa",
				AbsDefaultModel = "Sda",
				PsaMultiplier = 0.0,
				SdaMultiplier = 0.0,
			};

			var market = new MarketInfo("testMarket", "20160202")
			{
				HistoricalIndexRates = new Dictionary<string, Dictionary<string, double>>
				{
					{"Gjj5YAbove", new Dictionary<string, double>{{"20140101", 0.0425}}},
					{"Gjj5Y", new Dictionary<string, double>{{"20150825", 0.0275}}}
				}
			};

			QdpMarket qdpMarket;
			MarketFunctions.BuildMarket(market, out qdpMarket);
			var loanVf = new LoanVf(loanInfo);

			var result = loanVf.ValueTrade(qdpMarket, PricingRequest.Cashflow);
			var cfs = result.Cashflows;

			Assert.AreEqual(Math.Round(cfs.Where(c => c.PaymentDate == new Date(2014, 12, 09) && c.CashflowType == CashflowType.Principal).ToArray()[0].PaymentAmount, 2), 929.71);
			Assert.AreEqual(Math.Round(cfs.Where(c => c.PaymentDate == new Date(2014, 12, 09) && c.CashflowType == CashflowType.Coupon).ToArray()[0].PaymentAmount, 2), 1103.06);

			Assert.AreEqual(Math.Round(cfs.Where(c => c.PaymentDate == new Date(2015, 01, 09) && c.CashflowType == CashflowType.Principal).ToArray()[0].PaymentAmount, 2), 956.04);
			Assert.AreEqual(Math.Round(cfs.Where(c => c.PaymentDate == new Date(2015, 01, 09) && c.CashflowType == CashflowType.Coupon).ToArray()[0].PaymentAmount, 2), 1119.94);

			Assert.AreEqual(Math.Round(cfs.Where(c => c.PaymentDate == new Date(2015, 02, 09) && c.CashflowType == CashflowType.Principal).ToArray()[0].PaymentAmount, 2), 959.42);
			Assert.AreEqual(Math.Round(cfs.Where(c => c.PaymentDate == new Date(2015, 02, 09) && c.CashflowType == CashflowType.Coupon).ToArray()[0].PaymentAmount, 2), 1035.10);

			var netCf = cfs.GroupBy(x => x.PaymentDate)
				.Select(x => Tuple.Create(x.Key, x.Sum(entry => entry.PaymentAmount)))
				.OrderBy(x => x.Item1)
				.ToArray();
			foreach (var tuple in netCf)
			{
				if (tuple.Item1 > new Date(2015, 02, 09))
				{
					Assert.AreEqual(Math.Round(tuple.Item2, 2), 1994.53);
				}
			}
		}

		[TestMethod]
		public void TestLoanPayment2()
		{
			var loanInfo = new LoanInfo()
			{
				Amortization = "EqualPrincipalAndInterest",
				Coupon = 0.045,
				Currency = "CNY",
				DayCount = "B30360",
				FirstPaymentDate = "20141209",
				FloatingRateMultiplier = 1,
				Frequency = "Monthly",
				IndexType = "Gjj5YAbove",
				IsFloatingRate = true,
				MaturityDate = "20320409",
				Notional = 294150.32,
				NumOfPayment = 209,
				ResetDate = "20150101",
				StartDate = "20141109",
				MortgageCalcMethod = "TimeWeighted",
				AbsPrepaymentModel = "Psa",
				AbsDefaultModel = "Sda",
			};

			var market = new MarketInfo("testMarket", "20160202")
			{
				HistoricalIndexRates = new Dictionary<string, Dictionary<string, double>>
				{
					{"Gjj5YAbove", new Dictionary<string, double>{{"20140101", 0.0425}}},
					{"Gjj5Y", new Dictionary<string, double>{{"20150825", 0.0275}}}
				}
			};

			QdpMarket qdpMarket;
			MarketFunctions.BuildMarket(market, out qdpMarket);
			var loanVf = new LoanVf(loanInfo);

			var result = loanVf.ValueTrade(qdpMarket, PricingRequest.Cashflow);
			var cfs = result.Cashflows;

			Assert.AreEqual(Math.Round(cfs.Where(c => c.PaymentDate == new Date(2014, 12, 09) && c.CashflowType == CashflowType.Principal).ToArray()[0].PaymentAmount, 2), 929.71);
			Assert.AreEqual(Math.Round(cfs.Where(c => c.PaymentDate == new Date(2014, 12, 09) && c.CashflowType == CashflowType.Coupon).ToArray()[0].PaymentAmount, 2), 1103.06);

			Assert.AreEqual(Math.Round(cfs.Where(c => c.PaymentDate == new Date(2015, 01, 09) && c.CashflowType == CashflowType.Principal).ToArray()[0].PaymentAmount, 2), 956.04);
			Assert.AreEqual(Math.Round(cfs.Where(c => c.PaymentDate == new Date(2015, 01, 09) && c.CashflowType == CashflowType.Coupon).ToArray()[0].PaymentAmount, 2), 1083.29); //manually calculated using B30360 in Excel

			Assert.AreEqual(Math.Round(cfs.Where(c => c.PaymentDate == new Date(2015, 02, 09) && c.CashflowType == CashflowType.Principal).ToArray()[0].PaymentAmount, 2), 959.42);
			Assert.AreEqual(Math.Round(cfs.Where(c => c.PaymentDate == new Date(2015, 02, 09) && c.CashflowType == CashflowType.Coupon).ToArray()[0].PaymentAmount, 2), 1035.10);

			var netCf = cfs.GroupBy(x => x.PaymentDate)
				.Select(x => Tuple.Create(x.Key, x.Sum(entry => entry.PaymentAmount)))
				.OrderBy(x => x.Item1)
				.ToArray();
			foreach (var tuple in netCf)
			{
				if (tuple.Item1 > new Date(2015, 02, 09))
				{
					Assert.AreEqual(Math.Round(tuple.Item2, 2), 1994.53);
				}
			}
		}

		[TestMethod]
		public void TestLoanPayment3()
		{
			var loanInfo = new LoanInfo()
			{
				Amortization = "EqualPrincipalAndInterest",
				Coupon = 0.05,
				Currency = "CNY",
				DayCount = "B30360",
				FirstPaymentDate = "20141209",
				FloatingRateMultiplier = 1,
				Frequency = "Monthly",
				IndexType = "Gjj5YAbove",
				IsFloatingRate = false,
				MaturityDate = "20441109",
				Notional = 10000000.00,
				NumOfPayment = 360,
				ResetDate = "20150101",
				StartDate = "20141109",
				MortgageCalcMethod = "Simple",
				AbsPrepaymentModel = "Psa",
				AbsDefaultModel = "Sda",
				SdaMultiplier = 1,
				PsaMultiplier = 0
			};

			var market = new MarketInfo("testMarket", "20160202")
			{
				HistoricalIndexRates = new Dictionary<string, Dictionary<string, double>>
				{
					{"Gjj5YAbove", new Dictionary<string, double>{{"20140101", 0.0425}}},
					{"Gjj5Y", new Dictionary<string, double>{{"20150825", 0.0275}}}
				}
			};

			QdpMarket qdpMarket;
			MarketFunctions.BuildMarket(market, out qdpMarket);
			var loanVf = new LoanVf(loanInfo);

			var result = loanVf.ValueTrade(qdpMarket, PricingRequest.Cashflow);
			var cfs = result.Cashflows;

			var p =
				cfs.Where(x => x.CashflowType == CashflowType.Principal || x.CashflowType == CashflowType.PrincipalLossOnDefault)
					.Sum(x => x.PaymentAmount);
			Assert.AreEqual(p, loanInfo.Notional, 1e-4);
			//Assert.AreEqual(Math.Round(cfs[8].PaymentAmount, 2), 12115.23);
			//Assert.AreEqual(Math.Round(cfs[9].PaymentAmount, 2), 41564.25);
			//Assert.AreEqual(Math.Round(cfs[11].PaymentAmount, 2), 498.30);

			//Assert.AreEqual(Math.Round(cfs[1435].PaymentAmount, 2), 1.28);
			//Assert.AreEqual(Math.Round(cfs[1436].PaymentAmount, 2), 51141.30);
			//Assert.AreEqual(Math.Round(cfs[1437].PaymentAmount, 2), 213.09);
		}
	}
}
