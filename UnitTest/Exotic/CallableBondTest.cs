using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.MathMethods.Processes.ShortRate;
using Qdp.Pricing.Library.Common.Utilities.Coupons;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Pricing.Library.Exotic;
using Qdp.Pricing.Library.Exotic.Engines;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using UnitTest.CommonTests;
using UnitTest.Utilities;

namespace UnitTest.Exotic
{
	[TestClass]
	public class CallableBondTest
	{
		[TestMethod]
		public void TestCallableBond()
		{
			var bond = new Bond(
				"010001",
				new Date(2012, 05, 03),
 				new Date(2017, 05, 03),
 				100,
				CurrencyCode.CNY, 
				new FixedCoupon(0.068),
 				CalendarImpl.Get("chn_ib"),
 				Frequency.Annual, 
				Stub.LongEnd,
				new ActActIsma(), 
				new ActAct(), 
				BusinessDayConvention.None, 
				BusinessDayConvention.ModifiedFollowing, 
				null,
				TradingMarket.ChinaInterBank
				);

			var option = new VanillaOption(new Date(2012, 05, 03), 
				new Date(2017, 05, 03),
				OptionExercise.European, 
				OptionType.Put, 
				106.8,
				InstrumentType.Bond, 
				CalendarImpl.Get("chn_ib"),
				new Act365(), 
				CurrencyCode.CNY, 
				CurrencyCode.CNY, 
				new[] { new Date(2015, 05, 03) },
				new[] { new Date(2015, 05, 03) }
				);

			var callableBond = new CallableBond(bond, new[] {option}, new[] {PriceQuoteType.Dirty});
			var market = TestMarket();
			var engine = new CallableBondEngine<HullWhite>(new HullWhite(0.1, 0.01), true, 40);
			var result = engine.Calculate(callableBond, market, PricingRequest.All);
			Assert.AreEqual(107.0198942139, result.Pv, 1e-8);
		}

		[TestMethod]
		public void Test101658062_IB()
		{
			var bond = new Bond(
				"101658062.IB",
				new Date(2016, 10, 27),
				new Date(2029, 10, 27),
				100,
				CurrencyCode.CNY,
				new FixedCoupon(0.045),
				CalendarImpl.Get("chn_ib"),
				Frequency.Annual,
				Stub.LongEnd,
				new ActActIsma(),
				new ActAct(),
				BusinessDayConvention.None,
				BusinessDayConvention.ModifiedFollowing,
				null,
				TradingMarket.ChinaInterBank,
				false,
				null,
				null,
				false,
				double.NaN,
				Double.NaN,
				AmortizationType.None,
				null,
				null,
				false,
				new Dictionary<int, double> { { 4, 0.03} }
				);

			var options = new List<VanillaOption>();
			var start = new Date(2016, 10, 27);
			for (var k = 1; k < 16; k++)
			{
				var term = new Term(k, Period.Year);
				var maturity = term.Next(start);
				if (maturity > bond.UnderlyingMaturityDate)
				{
					continue;
					;
				}
				options.Add(new VanillaOption(start,
					maturity,
					OptionExercise.European,
					OptionType.Call,
					100,
					InstrumentType.Bond,
					CalendarImpl.Get("chn_ib"),
					new Act365(),
					CurrencyCode.CNY,
					CurrencyCode.CNY,
					new[] {maturity},
					new[] {maturity}
					));
			}

			var callableBond = new CallableBond(bond, options.ToArray(), options.Select(x => PriceQuoteType.Clean).ToArray());
			var valueDates = new[]
			{
				new Date(2016, 10, 28), 
				new Date(2016, 11, 01), 
				new Date(2016, 11, 02), 
				new Date(2016, 11, 03), 
				new Date(2016, 11, 04), 
				new Date(2016, 11, 07), 
			};
			foreach (var valueDate in valueDates)
			{
				var market = TestMarket2(valueDate);
				var engine = new CallableBondEngine<HullWhite>(new HullWhite(0.5196, 0.03157), true, 3);
				var result = engine.Calculate(callableBond, market, PricingRequest.All);
				Console.WriteLine("{0},{1}",valueDate, result.Pv);
			}
			
		}

		private IMarketCondition TestMarket2(Date valueDate)
		{
			var curveGroups = File.ReadAllLines(@"Data\101658062.csv").Select(x =>
			{
				var split = x.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				var dt = DateTime.Parse(split[0]);
				var referenceDate = new Date(dt.Year, dt.Month, dt.Day);

				var tuple = new Tuple<Date, Date, double>(referenceDate, new Term(split[1]).Next(referenceDate), Double.Parse(split[2])/100.0);
				return tuple;
			}).OrderBy(x => x.Item1).GroupBy(x => x.Item1);

			var yieldCurves = new List<IYieldCurve>();
			foreach (var curveRateGroup in curveGroups)
			{
				var keypoints =
					curveRateGroup
					.OrderBy(x => x.Item2)
					.Select(
						x => new Tuple<Date, double>(x.Item2, x.Item3)
					)
					.ToArray();

				yieldCurves.Add(new YieldCurve(
					"中债国债收收益率曲线",
					curveRateGroup.Key,	
					keypoints,
					BusinessDayConvention.Following, 
					new Act365(), 
					CalendarImpl.Get("chn_ib"),
					CurrencyCode.CNY,
					Compound.Continuous,
					Interpolation.CubicHermiteMonotic, 
					YieldCurveTrait.SpotCurve
					)
				);
			}

			var hw = new HullWhiteParameterEstimationTest(yieldCurves.Last(), yieldCurves.ToArray());
			double a;
			double sigma;
			double sigma2;
			double sigma3;
			hw.CalcOrnsteinUhlenbeck(out a, out sigma, out sigma2, out sigma3);
			Console.WriteLine(a);
			Console.WriteLine(sigma);
			Console.WriteLine(sigma2);
			Console.WriteLine(sigma3);

			var marketInfo = new MarketInfo("TestMarket")
			{
				ReferenceDate = valueDate.ToString(),
				HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates
			};

			QdpMarket market;
			MarketFunctions.BuildMarket(marketInfo, out market);
			return new MarketCondition(
				x => x.ValuationDate.Value = new Date(DateTime.Parse(marketInfo.ReferenceDate)),
				x => x.DiscountCurve.Value = yieldCurves.Single(c => c.ReferenceDate == valueDate),
				x => x.FixingCurve.Value = yieldCurves.Single(c => c.ReferenceDate == valueDate),
				x => x.HistoricalIndexRates.Value = new Dictionary<IndexType, SortedDictionary<Date, double>>()
				);
		}

		private IMarketCondition TestMarket()
		{
			var curveConvention = new CurveConvention("curveConvention",
					"CNY", 
					"Following", 
					"Chn_ib", 
					"Act365", 
					"Continuous",
					"Linear");
			var curveDefinitions = new List<InstrumentCurveDefinition>();
			// 1 - discount curve
			var rates = new[]
			{
				new RateMktData("1D", 0.06, "Spot", "None","Fr007"),
				new RateMktData("10Y", 0.06, "Spot", "None","Fr007"),
			};

			var fr007Curve = new InstrumentCurveDefinition(
				"Fr007",
				curveConvention,
				rates,
				"SpotCurve");
			curveDefinitions.Add(fr007Curve);

			var marketInfo = new MarketInfo("TestMarket")
			{
				ReferenceDate = "2014-05-05",
				YieldCurveDefinitions = curveDefinitions.ToArray(),
				HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates
			};

			QdpMarket market;
			MarketFunctions.BuildMarket(marketInfo, out market);
			return new MarketCondition(
				x => x.ValuationDate.Value = new Date(DateTime.Parse(marketInfo.ReferenceDate)),
				x => x.DiscountCurve.Value = market.GetData<CurveData>("Fr007").YieldCurve,
				x => x.FixingCurve.Value = market.GetData<CurveData>("Fr007").YieldCurve,
				x => x.HistoricalIndexRates.Value = new Dictionary<IndexType, SortedDictionary<Date, double>>()
				);
		}
	}
}
