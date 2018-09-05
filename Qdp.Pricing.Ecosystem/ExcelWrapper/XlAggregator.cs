using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Ecosystem.ExcelWrapper
{
	/// <summary>
	/// Aggregator to display aggregated result.
	/// </summary>
	public static class XlAggregator
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pricingResults"></param>
		/// <returns></returns>
		public static object AggregatedResult(Dictionary<string, IPricingResult> pricingResults)
		{
			var propertyObjects = typeof(IPricingResult).GetProperties();
			var resultDic = new Dictionary<string, object>();
			var map = new Dictionary<string, object>();
			foreach (var propertyObject in propertyObjects)
			{
				var result = AggregatedSingleResult(pricingResults, propertyObject);
				if (result != null)
				{
					map[propertyObject.Name] = result;
				}
			}
			resultDic["result"] = pricingResults;
			resultDic["summaryResult"] = map;
			return resultDic;
		}

		private static object AggregatedSingleResult(Dictionary<string, IPricingResult> pricingResults, PropertyInfo propertyInfo)
		{
			//var map = new object[pricingResults.Keys.Count, 2];
			//int i = 0;
			//foreach (var key in pricingResults.Keys)
			//{
			//	map[i, 0] = key;
			//	if (propertyInfo.PropertyType == typeof(Dictionary<string, Dictionary<string, RateRecord>>))
			//	{
			//		var productSpecificDic = (Dictionary<string, Dictionary<string, RateRecord>>)propertyInfo.GetValue(pricingResults[key], null);
			//		var specificDic = new Dictionary<string, object>();
			//		foreach (var productSpecificKey in productSpecificDic.Keys)
			//		{
			//			specificDic[productSpecificKey] = productSpecificDic[productSpecificKey].To2DArray();
			//		}
			//		map[i++, 1] = specificDic;
			//	}
			//	else if (propertyInfo.PropertyType == typeof (Date))
			//	{
			//		var date = propertyInfo.GetValue(pricingResults[key], null);
			//		map[i++, 1] = date!=null?date.ToString():null;
			//	}
			//	else
			//	{
			//		map[i++, 1] = propertyInfo.GetValue(pricingResults[key], null);
			//	}
			//}
			var summable = PricingResult.SummableProperties.ContainsKey(propertyInfo.Name);
			if (summable)
			{
				return AggregatedDataByType(pricingResults, propertyInfo);
			}
			return null;
		}

		private static object AggregatedDataByType(Dictionary<string, IPricingResult> pricingResults, PropertyInfo propertyInfo)
		{
			object result = null;
			if (propertyInfo.PropertyType == typeof(Dictionary<string, double>))
			{
				var propertyValuesArray = pricingResults.Select(x => propertyInfo.GetValue(x.Value, null)).ToArray();
				var dicresult = propertyValuesArray.Cast<Dictionary<string, double>>().Aggregate<Dictionary<string, double>, Dictionary<string, double>>(null, (current, dic) => PricingResultExtension.Aggregate(current, dic));
				result = dicresult.To2DArray();
			}
			else if (propertyInfo.PropertyType == typeof(Double))
			{
				var propertyValuesArray = pricingResults.Select(x => propertyInfo.GetValue(x.Value, null)).ToArray();
				result = propertyValuesArray.Cast<double>().Aggregate<double, double>(double.NaN, (current, dic) => PricingResultExtension.Aggregate(current, dic));
			}
			else if (propertyInfo.PropertyType == typeof(Cashflow[]))
			{
				var propertyValuesArray = pricingResults.Select(x => propertyInfo.GetValue(x.Value, null)).ToArray();
				var cashflowArray = propertyValuesArray.Cast<Cashflow[]>().Aggregate<Cashflow[], Cashflow[]>(null, (current, cashflow) => PricingResultExtension.Aggregate(current, cashflow));
				var removedPropertiesNames = new List<string>() {"CalculationDetails"};
				result = cashflowArray.ToLableData(removedPropertiesNames, typeof(Cashflow).GetProperties().Length - removedPropertiesNames.Count);
			}
			else
			{
				throw new NotImplementedException();
			}
			return result;
		}
	}
}
