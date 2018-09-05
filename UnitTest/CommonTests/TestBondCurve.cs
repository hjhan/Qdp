using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Trade.FixedIncome;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities;

namespace UnitTest.CommonTests
{
	[TestClass]
	public class TestBondCurve
	{
		[TestMethod]
		public void testBondCurve1()
		{
			var bondIds = new[] {"1", "2", "3", "4", "5"};
			var maturitys = new[] {"2017-10-28", "2018-10-28", "2019-10-28", "2020-10-28", "2021-10-28"};
			var coupons = new[] {0.0367, 0.0378, 0.04, 0.039, 0.041};
			var yields = new[] {0.04, 0.041, 0.0415, 0.0425, 0.044};

			var bondInfos = bondIds.Select((x, i) =>
				
				new FixedRateBondInfo(bondIds[i])
				{
					StartDate = "2016-10-28",
					MaturityDate = maturitys[i],
					Notional = 100.0,
					Currency = "CNY",
					FixedCoupon = coupons[i],
					Calendar = "chn_ib",
					PaymentFreq = "SemiAnnual",
					PaymentStub = "LongEnd",
					AccrualDC = "ActActIsma",
					DayCount = "ActActIsma",
					AccrualBD = "None",
					PaymentBD = "None",
					TradingMarket = "ChinaInterBank",
					Settlement = "+0D",
					ValuationParamters = new SimpleCfValuationParameters("Fr007", null, "Fr007")
				}
				).ToArray();

			var bonds = bondInfos.Select(x => new BondVf(x).GenerateInstrument()).ToArray();
			var marketInstrumetns =
				bonds.Select((x, i) => new MarketInstrument(bonds[i], yields[i], MktInstrumentCalibMethod.Default)).ToArray();

			var referenceDate = new Date(2017, 03, 23);
			var yieldCurve = new YieldCurve(
				"中债国债收收益率曲线",
				referenceDate,
				marketInstrumetns.ToArray(),
				BusinessDayConvention.ModifiedFollowing,
				new Act365(), 
				CalendarImpl.Get("chn_ib"),
				CurrencyCode.CNY,
				Compound.Continuous,
				Interpolation.ExponentialSpline,
				YieldCurveTrait.DiscountCurve,
				null,
				null,
				null,
				maturitys.Select(x => x.ToDate()).ToArray());

			var term = new Term("1D");

			var targetSpotRates = new[]
			{
				0.0395360864,
				0.0405725835,
				0.0410856649,
				0.0420840373,
				0.0436655798,

			};

			Console.WriteLine("Fitting error is {0}", yieldCurve.fittingError);




			for (var i = 0; i < yieldCurve.KeyPoints.Length; ++i)
			{
				Console.WriteLine("{0},{1},{2},{3}", yieldCurve.KeyPoints[i].Item1, yieldCurve.GetDf(yieldCurve.KeyPoints[i].Item1), yieldCurve.GetSpotRate(yieldCurve.KeyPoints[i].Item1), yieldCurve.ZeroRate(referenceDate, yieldCurve.KeyPoints[i].Item1));
				if(i > 3)
					Assert.AreEqual(yieldCurve.GetSpotRate(yieldCurve.KeyPoints[i].Item1), targetSpotRates[i-4], 1e-10); 
			}

			Console.WriteLine();
			var baseMarket = new MarketCondition(
				x => x.ValuationDate.Value = referenceDate,
				x => x.DiscountCurve.Value = null,
				x => x.FixingCurve.Value = null,
				x => x.HistoricalIndexRates.Value = new Dictionary<IndexType, SortedDictionary<Date, double>>()
				);
			var bondCfs = marketInstrumetns.Select(x => (x.Instrument as Bond).GetCashflows(baseMarket, true)).ToList();

			var bondPricer = new BondYieldPricer();
			for (var i = 0; i < bondCfs.Count; ++i)
			{
				var pv = bondCfs[i].Select(cf => cf.PaymentAmount * yieldCurve.GetDf(referenceDate, cf.PaymentDate)).Sum();
				var x = marketInstrumetns[i].Instrument as Bond;
				var mktPrice = bondPricer.FullPriceFromYield(bondCfs[i], x.PaymentDayCount, x.PaymentFreq, x.StartDate,
					referenceDate, marketInstrumetns[i].TargetValue, x.BondTradeingMarket, x.IrregularPayment);
				Console.WriteLine("{0},{1},{2}", pv, mktPrice, pv - mktPrice);
			}
		}
	}
}
