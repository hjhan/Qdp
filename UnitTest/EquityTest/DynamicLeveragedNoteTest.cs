using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Equity.Engines.Note;
using Qdp.Pricing.Library.Equity.Notes;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;

namespace UnitTest.EquityTest
{
	[TestClass]
	public class DynamicLeveragedNoteTest
	{
		[TestMethod]
		public void DlnTest()
		{
			var valueDate = new Date(2015, 06, 16);

			var market = new MarketCondition(x => x.ValuationDate.Value = valueDate);

			var fixings = File.ReadAllLines(@"./Data/HistoricalEquityPrices/hs300.csv")
				.Select(x =>
				{
					var splits = x.Split(',');
					return Tuple.Create(new Date(DateTime.Parse(splits[0])), Double.Parse(splits[1]));
				}).ToDictionary(x => x.Item1, x => x.Item2);

			var dln = new DynamicLeveragedNote(new Date(2015, 1, 5),
				new Date(2018, 10, 29),
				1000,
				2.0,
				1.75,
				2.25,
				0.05,
				0.004,
				fixings
				);
			var engine = new DynamicLeveragedNoteEngine();
			var result = engine.Calculate(dln, market, PricingRequest.All);
			Assert.AreEqual(result.Pv/dln.Notional, 1.793219889635, 1e-10);
			Assert.AreEqual(result.Delta/dln.Notional, 3.695695861697, 1e-10);

			var dln2 = new DynamicLeveragedNote(new Date(2015, 1, 5),
				new Date(2018, 10, 29),
				1000,
				1.0,
				0.75,
				1.25,
				0.05,
				0.004,
				fixings
				);
			result = engine.Calculate(dln2, market, PricingRequest.All);
			Assert.AreEqual(result.Pv / dln2.Notional, 1.39084563124, 1e-10);
			Assert.AreEqual(result.Delta / dln2.Notional, 1.39084563124, 1e-10);
		}
	}
}
