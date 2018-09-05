using System;
using System.Collections.Generic;
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
	public class ConstantLeveragedNoteTest
	{
		[TestMethod]
		public void ClnTest()
		{
			var valueDate = new Date(2015, 06, 1);

			var market = new MarketCondition(x => x.ValuationDate.Value = valueDate);

			var fixings = File.ReadAllLines(@"./Data/HistoricalEquityPrices/hsgy.csv")
				.Select(x =>
				{
					var splits = x.Split(',');
					return Tuple.Create(new Date(DateTime.Parse(splits[0])), Double.Parse(splits[1]));
				}).ToDictionary(x => x.Item1, x => x.Item2);

			var cln = new ConstantLeveragedNote(new Date(2015, 5, 28),
				new Date(2018, 10, 29),
				1000,
				3.0,
				0.055,
				fixings,
				new Dictionary<Date, double>()
				{
					{new Date(2015, 05, 28), 0.8002},
					{new Date(2015, 06, 01), 0.8}
				}
				);
			var engine = new ConstantLeveragedNoteEngine();
			var result = engine.Calculate(cln, market, PricingRequest.All);
			Assert.AreEqual(result.Pv / cln.Notional, 1.022959, 1e-6);
		}
	}
}
