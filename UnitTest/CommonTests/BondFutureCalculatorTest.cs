using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Utilities.Coupons;

namespace UnitTest.CommonTests
{
	[TestClass]
	public class BondFutureCalculatorTest
	{
		[TestMethod]
		public void TestTfDeliverableBondCf()
		{
			var startDates = new[] { new Date(2016, 04, 06), new Date(2016, 04, 06), new Date(2016, 04, 06), };
			var maturityDates = new[] { new Date(2016, 06, 15), new Date(2016, 09, 14), new Date(2016, 12, 13), };
			var files = new[] { "QbTF1606CfTest.txt", "QbTF1609CfTest.txt", "QbTF1612CfTest.txt" };
			var futuresNames = new[] {"TF1606", "TF1609", "TF1612"};
			for (var k = 0; k < startDates.Length; ++k)
			{
				var bondPrices = new Dictionary<string, double>();
				var targetConversionFactors = new List<double>();
				var bonds = File.ReadAllLines(@"./Data/BondFuture/" + files[k])
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
					futuresNames[k],
					startDates[k],
					maturityDates[k],
					maturityDates[k],
					CalendarImpl.Get("chn"),
					bonds,
					new Act365()
					);
				var market = new BondFutureTests().TestMarket("2016-04-06");
				for (var i = 0; i < bonds.Length; ++i)
				{
					Assert.AreEqual(bondfuture.GetConversionFactor(bonds[i], market), targetConversionFactors[i]);
				}
			}
		}

		[TestMethod]
		public void TestQbTfDeliverableBondYield()
		{
			var files = new[] { "QbTF1606CfTest.txt", "QbTF1609CfTest.txt", "QbTF1612CfTest.txt" };

			for (var k = 0; k < files.Length; ++k)
			{
				var bondPrices = new Dictionary<string, Tuple<PriceQuoteType, double>>();
				var targetYtm = new List<double>();
				var targetAi = new List<double>();
				var bonds = File.ReadAllLines(@"./Data/BondFuture/" + files[k])
					.Select(x =>
					{
						var splits = x.Split(',');
						var startDate = new Date(DateTime.Parse(splits[3]));
						var maturityDate = new Date(DateTime.Parse(splits[4]));
						var rate = Convert.ToDouble(splits[1]);
						var frequency = splits[2] == "Annual" ? Frequency.Annual : Frequency.SemiAnnual;
						
						targetYtm.Add(Convert.ToDouble(splits[8]));
						targetAi.Add(Convert.ToDouble(splits[6]) - Convert.ToDouble(splits[7]));
						bondPrices[splits[0]] = Tuple.Create(PriceQuoteType.Dirty, Convert.ToDouble(splits[6]));

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
							new ActActIsma(), 
							new ActActIsma(), 
							BusinessDayConvention.None,
							BusinessDayConvention.None,
							null,
							TradingMarket.ChinaInterBank);
					}).ToArray();

				var market = new BondFutureTests().TestMarket("2016-04-06", bondPrices);
				var engine = new BondEngine();
				
				for (var i = 0; i < bonds.Length; ++i)
				{
					var result = engine.Calculate(bonds[i], market, PricingRequest.Ytm | PricingRequest.Ai);
					Assert.AreEqual(Math.Round(result.Ai, 4), targetAi[i], 1e-5);
					Assert.AreEqual(Math.Round(result.Ytm, 6), targetYtm[i], 1e-7);
				}
			}
		}
	}
}

