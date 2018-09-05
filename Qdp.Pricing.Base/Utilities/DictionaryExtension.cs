using System;
using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Foundation.Interfaces;
using Qdp.Pricing.Base.Implementations;

namespace Qdp.Pricing.Base.Utilities
{
	public static class DictionaryExtension
	{
		public static Dictionary<string, TValue> UpdateKey<TValue>(this Dictionary<string, TValue> dict, string key, TValue value)
		{
			var newDict = new Dictionary<string, TValue>(dict);
			if (newDict.ContainsKey(key))
			{
				newDict.Remove(key);
			}
			newDict[key] = value;
			return newDict;
		}

		public static SortedDictionary<Date, double> ToSortedDictionary(this IDictionary<string, double> dictionary)
		{
			var sortedDictionary = new SortedDictionary<Date, double>();
			if (!(dictionary is SortedDictionary<string, double>))
			{
				foreach (var key in dictionary.Keys)
				{
					sortedDictionary.Add(key.ToDate(), dictionary[key]);
				}
			}
			return sortedDictionary;
		}

		public static SortedDictionary<Date, double> ToSortedDictionary(this IDictionary<Date, double> dictionary)
		{
			var sortedDictionary = new SortedDictionary<Date, double>();
			if (!(dictionary is SortedDictionary<Date, double>))
			{
				foreach (var key in dictionary.Keys)
				{
					sortedDictionary.Add(key, dictionary[key]);
				}
			}
			else
			{
				sortedDictionary = (SortedDictionary<Date, double>)dictionary;
			}
			return sortedDictionary;
		}

		public static Dictionary<string, double> UpdateKey(this Dictionary<string, double> dict, string key, double value)
		{
			return dict.UpdateKey<double>(key, value);
		}

		public static Dictionary<string, Tuple<string, double>> UpdateKey(this Dictionary<string, Tuple<string, double>> dict, string key, Tuple<string, double> value)
		{
			return dict.UpdateKey<Tuple<string, double>>(key, value);
		}

		public static object[,] To2DArray(this Dictionary<string, double> dic)
		{
			object[,] stringArray2D = new object[2, dic.Count];
			int i = 0;

			foreach (var item in dic)
			{
				stringArray2D[0, i] = item.Key;
				stringArray2D[1, i] = item.Value;
				i++;
			}

			return stringArray2D;
		}

		public static object[,] To2DArray(this Dictionary<string, RateRecord> dic)
		{
			object[,] stringArray2D = new object[2, dic.Count];
			int i = 0;

			foreach (var item in dic)
			{
				stringArray2D[0, i] = item.Key;
				stringArray2D[1, i] = item.Value.Rate;
				i++;
			}

			return stringArray2D;
		}
	}
}
