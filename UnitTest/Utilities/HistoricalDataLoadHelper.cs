using System;
using System.Collections.Generic;
using System.IO;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;

namespace UnitTest.Utilities
{
	public class HistoricalDataLoadHelper
	{
		public static readonly Dictionary<string, Dictionary<string, double>> HistoricalIndexRates;

		public static readonly Dictionary<IndexType, SortedDictionary<Date, double>> HistoricalIndexRatesMarket;

		static HistoricalDataLoadHelper()
		{
			HistoricalIndexRates = new Dictionary<string, Dictionary<string, double>>();
			HistoricalIndexRatesMarket = new Dictionary<IndexType, SortedDictionary<Date, double>>();
			var files = Directory.GetFiles(@".\Data\HistoricalIndexRates");

			foreach (var file in files)
			{
				var shortName = Path.GetFileNameWithoutExtension(file);
				IndexType indexType;
				if (Enum.TryParse(shortName, out indexType))
				{
					var temp = new Dictionary<string, double>();
					var temp1 = new SortedDictionary<Date, double>();
					var lines = File.ReadAllLines(file);
					foreach (var line in lines)
					{
						var splits = line.Split(',');
						temp[splits[0]] = Convert.ToDouble(splits[1]);
						temp1[splits[0].ToDate()] = Convert.ToDouble(splits[1]);
					}
					HistoricalIndexRates[shortName] = temp;
					HistoricalIndexRatesMarket[shortName.ToIndexType()] = temp1;
				}
			}
		}

		public static Dictionary<string, double> GetIndexRates(string indexType)
		{
			return HistoricalIndexRates[indexType];
		} 
	}
}
