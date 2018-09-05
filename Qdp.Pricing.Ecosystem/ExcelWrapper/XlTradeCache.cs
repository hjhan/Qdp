using System.Collections.Generic;
using System.Linq;
using Qdp.ComputeService.Data.CommonModels.TradeInfos;
using Qdp.Foundation.TableWithHeader;
using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Ecosystem.ExcelWrapper
{
	/// <summary>
	/// Trade cache. Bond and TF MUST be loaded on open of addin.
	/// </summary>
	public class XlTradeCache
	{
		public string TradeCacheName
		{
			get { return "qbTradeCache_"; }
		}

		private Dictionary<string, TradeInfoBase> _tradeDict;

		public string[] AllTradeIds
		{
			get { return _tradeDict.Keys.ToArray(); }
		}

		public XlTradeCache(TradeInfoBase[] tradeInfos)
		{
			if (tradeInfos == null || !tradeInfos.Any())
			{
				_tradeDict = new Dictionary<string, TradeInfoBase>();
				return;
			}
			if (tradeInfos.Length > tradeInfos.Select(x => x.TradeId).Distinct().Count())
			{
				throw new PricingLibraryException("There are duplicate tradeIDs in the array of trades input!");
			}
			_tradeDict = tradeInfos.ToDictionary(x => x.TradeId, x => x);
			//AddDependenciesTrades(tradeInfos);
		}

		public bool AddTrades(TradeInfoBase[] tradeInfos)
		{
			foreach (var tradeInfoBase in tradeInfos)
			{
				_tradeDict[tradeInfoBase.TradeId] = tradeInfoBase;
			}
			//AddDependenciesTrades(tradeInfos);
			return true;
		}

		//private bool AddDependenciesTrades(TradeInfoBase[] tradeInfos)
		//{
		//	var dependencyTrades = new List<TradeInfoBase>();
		//	foreach (var tradeInfoBase in tradeInfos)
		//	{
		//		var dependencies = tradeInfoBase.GetDependenciesTrades();
		//		if (dependencies == null)
		//		{
		//			continue;
		//		}
		//		dependencyTrades.AddRange(dependencies);
		//	}
		//	foreach (var dependency in dependencyTrades)
		//	{
		//		_tradeDict[dependency.TradeId] = dependency;
		//	}
		//	return true;
		//}

		public bool RemoveTrades(string[] tradeIds)
		{
			foreach (var tradeId in tradeIds)
			{
				if (_tradeDict.ContainsKey(tradeId))
				{
					_tradeDict.Remove(tradeId);
				}
			}
			return true;
		}

		public TradeInfoBase GetTradeInfo(string tradeId)
		{
			if (!_tradeDict.ContainsKey(tradeId))
			{
				return null;
			}
			else
			{
				return _tradeDict[tradeId];
			}
		}

		public object GetTradeCacheInLabelData()
		{
			var typeList = _tradeDict.Values.Select(x=>x.GetType()).ToList().Distinct().ToArray();
			var orderDic = new Dictionary<string, int>();
			foreach (var type in typeList)
			{
				var props = type.GetProperties().ToDictionary(x => x.Name, x => x);
				var labelDic = props.ToDictionary(x => x.Key, y =>
				{
					var column = y.Value.GetCustomAttributes(false).FirstOrDefault(c => c is ColumnAttribute) as ColumnAttribute;
					return column == null ? 10000 : column.Column;
				}).ToDictionary(x => x.Key, x => x.Value);

				foreach (var key in labelDic.Keys)
				{
					orderDic[key] = labelDic[key];
				}
			}

			var labels = orderDic.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value).Keys.ToArray();

			var ret = new object[_tradeDict.Count + 1, labels.Length];
			var title = 0;
			foreach (var label in labels)
			{
				ret[0, title++] = label;
			}
			
			int row = 1;
			foreach (var tradeId in _tradeDict.Keys)
			{
				var tradeInfo = _tradeDict[tradeId];

				var type = tradeInfo.GetType();
				var properties = type.GetProperties();
				var methodInfos = type.GetMethods();
				int col = 0;
				foreach (var label in labels)
				{
					var prop = properties.FirstOrDefault(x => x.Name == label);
					if (prop != null)
					{
						var converterName = string.Format("{0}ToLabelData", prop.Name);
						var converter = methodInfos.FirstOrDefault(x => x.Name.Equals(converterName) && x.GetParameters().Length == 0);
						if (converter != null)
						{
							ret[row, col++] = converter.Invoke(tradeInfo, null);
						}
						else
						{
							ret[row, col++] = prop.GetValue(tradeInfo, null) ?? string.Empty;
						}
					}
					else
					{
						ret[row, col++] = string.Empty;
					}
				}
				row++;
			}

			return ret;
		}

		public object GetTradeInfoInLabelData(string tradeId, string[] outputLabels)
		{
			var tradeInfo = GetTradeInfo(tradeId);

			if (tradeInfo == null)
			{
				return string.Format("Cannot find trade {0} in memory.", tradeId);
			}

			return tradeInfo.ToTradeInfoInLabelData(outputLabels);
		}
	}
}
