using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;

namespace Qdp.Pricing.Base.Utilities
{
	public static class IndexRatesDictionaryExtensions
	{
		public static double GetAverageIndex(this IDictionary<Date, double> floatingIndexRates,
			Date fixingDate,
			ICalendar resetCalendar,
			int period,
			int? fixingRateDigits = null)
		{
			var indexRates = floatingIndexRates.ToSortedDictionary();
			double sum = 0.0;
			var n = period;
			var temp = new List<Tuple<Date, double>>();
			//var t1D = new Term("1D");
			while (indexRates != null && indexRates.Count > 0)
			{
				if (resetCalendar.IsHoliday(fixingDate))
				{
					fixingDate = resetCalendar.AddBizDays(fixingDate, -1);
					continue;
				}

				double value;
				var fixingTruple = indexRates.TryGetValue(fixingDate, resetCalendar);
				fixingDate = fixingTruple.Item1;
				value = fixingTruple.Item2;
				sum += value;
				temp.Add(Tuple.Create(fixingDate, value));

				fixingDate = resetCalendar.AddBizDays(fixingDate, -1);
				if (--n == 0)
				{
					break;
				}
			}

			return (sum / period).Round((fixingRateDigits ?? 13) + 2);
		}

		public static double GetAverageIndex(this IDictionary<string, double> indexRates,
			Date fixingDate,
			ICalendar resetCalendar,
			int period,
			int? fixingRateDigits = null)
		{
			var indexRatesReset = indexRates.ToSortedDictionary();
			return indexRatesReset.GetAverageIndex(fixingDate, resetCalendar, period, fixingRateDigits);
		}

		public static Tuple<Date, double> TryGetValue(this SortedDictionary<Date, double> historicalRates, Date fixingDate, ICalendar resetCalendar)
		{
			var returnTuple = Tuple.Create(fixingDate, 0.0);
			if (historicalRates != null && historicalRates.Count > 0)
			{
				var lastKeyPair = historicalRates.ElementAt(historicalRates.Count - 1);
				var firstKeyPair = historicalRates.ElementAt(0);
				if (lastKeyPair.Key < fixingDate)
				{
					returnTuple = Tuple.Create(lastKeyPair.Key, lastKeyPair.Value);
				}
				else if (firstKeyPair.Key <= fixingDate)
				{
					while (!historicalRates.ContainsKey(fixingDate) ||
						   (historicalRates.ContainsKey(fixingDate) && double.IsNaN(historicalRates[fixingDate])))
					{
						fixingDate = resetCalendar.AddBizDays(fixingDate, -1);
					}
					var fixingRate = historicalRates[fixingDate];
					returnTuple = Tuple.Create(fixingDate, fixingRate);
				}
			}
			return returnTuple;
		}

		public static Tuple<Date, double> TryGetValue(this IDictionary<string, double> historicalRates, string fixingDate, ICalendar resetCalendar)
		{
			var dic = historicalRates.ToSortedDictionary();
			return dic.TryGetValue(fixingDate.ToDate(), resetCalendar);
		}
	}
}
