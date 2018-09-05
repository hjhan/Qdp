using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Market;

using Qdp.Pricing.Library.Common.Utilities.Coupons;

namespace UnitTest.CommonTests
{
	[TestClass]
	public class BondForwardTests
	{
		[TestMethod]
		public void BondForwardTest()
		{
			var bond = new Bond(
				"010000",
				new Date(2013, 2, 25),
 				new Date(2015, 2, 25),
 				100,
				CurrencyCode.CNY, 
				new FixedCoupon(0.0375),
 				CalendarImpl.Get("chn_ib"),
 				Frequency.SemiAnnual, 
				Stub.LongEnd,
				new ActActIsma(), 
				new Act365(), 
				BusinessDayConvention.None, 
				BusinessDayConvention.None, 
				null,
				TradingMarket.ChinaInterBank
				);
			var bondForward = new Forward<Bond>(
				new Date(2013, 5, 28),
 				new Date(2013, 8, 28),
 				100.0,
				98.57947065,
				bond,
				CurrencyCode.CNY
				);

			var referenceDate = new Date(2013, 8, 23);
			var yieldCurve = new YieldCurve(
				"中债国债收收益率曲线",
				referenceDate,
				new[]
				{
					Tuple.Create((ITerm)new Term("1D"), 0.035),
					Tuple.Create((ITerm)new Term("1Y"), 0.035)
				},
				BusinessDayConvention.ModifiedFollowing, 
				new Act365(), 
				CalendarImpl.Get("chn_ib"),
 				CurrencyCode.CNY,
				Compound.Simple,
				Interpolation.CubicHermiteMonotic,
				YieldCurveTrait.SpotCurve
				);

			var market = new MarketCondition
			(
				x => x.ValuationDate.Value = referenceDate,
				x => x.MktQuote.Value = new Dictionary<string, Tuple<PriceQuoteType, double>> { { "010000", Tuple.Create(PriceQuoteType.Dirty, 99.71798177) } },
				x => x.DiscountCurve.Value = yieldCurve,
				x => x.FixingCurve.Value = yieldCurve,
				x => x.RiskfreeCurve.Value = yieldCurve,
				x => x.UnderlyingDiscountCurve.Value = yieldCurve,
				x => x.HistoricalIndexRates.Value = new Dictionary<IndexType, SortedDictionary<Date, double>>()
			);

			var bondengine = new BondEngine();
			var zeroSpread = bondengine.Calculate(bond, market, PricingRequest.ZeroSpread).ZeroSpread;
			var engine = new ForwardEngine<Bond>();
			var result = engine.Calculate(bondForward, market, PricingRequest.Pv);
			Assert.AreEqual(-0.68888788, result.Pv, 1e-8);
		}
	}
}
