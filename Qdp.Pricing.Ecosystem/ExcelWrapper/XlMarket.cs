using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Ecosystem.ExcelWrapper
{
	public class XlMarket
	{
		public QdpMarket QdpMarket
		{
			get { return _qdpMarket; }
		}

		public MarketInfo MarketInfo
		{
			get { return _marketInfo; }
		}

		public string MarketName
		{
			get { return _qdpMarket.MarketName; }
		}

		public XlMarket(MarketInfo marketInfo)
		{
			MarketDataDefinition[] rawDefinitions;
			MarketDataDefinition[] ripeDefinitions;
			_marketInfo = marketInfo;
			marketInfo.GetClassifiedDefinitions(out rawDefinitions, out ripeDefinitions);
			_mktDataDict = ripeDefinitions.ToDictionary(x => x.Name, x => x);
			foreach (var marketDataDefinition in rawDefinitions)
			{
				_mktDataDict[marketDataDefinition.Name] = marketDataDefinition;
			}

			var historicalIndexRates = new Dictionary<IndexType, SortedDictionary<Date, double>>();
			if (marketInfo.HistoricalIndexRates != null && marketInfo.HistoricalIndexRates.Any())
			{
				foreach (var historicalIndexKey in marketInfo.HistoricalIndexRates.Keys)
				{
					var indexKey = historicalIndexKey.ToIndexType();
					var historicalIndexDateDic = new SortedDictionary<Date, double>();
					foreach (var historicalIndexDateKey in marketInfo.HistoricalIndexRates[historicalIndexKey].Keys)
					{
						historicalIndexDateDic.Add(historicalIndexDateKey.ToDate(),
							marketInfo.HistoricalIndexRates[historicalIndexKey][historicalIndexDateKey]);
					}
					historicalIndexRates.Add(indexKey, historicalIndexDateDic);
				}
			}
			else
			{
				historicalIndexRates = XlUdf.HistoricalIndexRates;
			}

			_qdpMarket = new QdpMarket(marketInfo.MarketName, marketInfo.ReferenceDate.ToDate(), historicalIndexRates, ripeDefinitions);
			var result = _qdpMarket.UpdateMarketRawData(rawDefinitions);
			result.WaitTillFinished();
		}

		public string[] GetMktObjNames()
		{
			return _mktDataDict.Keys.ToArray();
		}

	    /// <summary>
	    /// 
	    /// </summary>
	    /// <param name="newMktName"></param>
	    /// <param name="newDate">2017-08-07</param>
	    /// <returns></returns>
	    public XlMarket ReCalibrateToDate(string newMktName, string newDate)
	    {

            var newMarketInfo = new MarketInfo
	        {
	            MarketName = newMktName,
                ReferenceDate = newDate,
                YieldCurveDefinitions = this.MarketInfo.YieldCurveDefinitions,
                HistoricalIndexRates = this.MarketInfo.HistoricalIndexRates,
                BondMktDatas = this.MarketInfo.BondMktDatas,
                CommodityMktDatas = this.MarketInfo.CommodityMktDatas,
                VolSurfMktDatas = this.MarketInfo.VolSurfMktDatas,
                StockMktDatas = this.MarketInfo.StockMktDatas,
                SpcCurveDefinitions = this.MarketInfo.SpcCurveDefinitions,
                FuturesMktDatas = this.MarketInfo.FuturesMktDatas,
                TreasuryFutureMktData = this.MarketInfo.TreasuryFutureMktData,
                DoNotCache = this.MarketInfo.DoNotCache,
                OverrideExisting = this.MarketInfo.OverrideExisting
	        };

            return new XlMarket(newMarketInfo);
	    }

		public MarketDataDefinition GetMktObj(string mktObjName)
		{
			if (!_mktDataDict.ContainsKey(mktObjName))
			{
				return null;
			}
			return _mktDataDict[mktObjName];
		}

		public void MergeDefinitions(object mktDataDefObject, bool isOverride = true)
		{
			if (mktDataDefObject is MarketDataDefinition)
			{
				var mktDataDef = mktDataDefObject as MarketDataDefinition;
				if ((_mktDataDict.ContainsKey(mktDataDef.Name) && isOverride) || !_mktDataDict.ContainsKey(mktDataDef.Name))
				{
					MergeSingleDataDef(new[] {mktDataDef}, true);
					MergeSingleDataDef(mktDataDef.GetDependencies(), false);
				}
			}
			else if (mktDataDefObject is Dictionary<IndexType, Dictionary<Date, double>>)
			{
				var mktDataDef = (Dictionary<IndexType, Dictionary<Date, double>>)mktDataDefObject;
				foreach (var key in mktDataDef.Keys)
				{
					var mktValues = mktDataDef[key];
					if (_marketInfo.HistoricalIndexRates.ContainsKey(key.ToString()))
					{
						foreach (var dateKey in mktValues.Keys)
						{
							if((_marketInfo.HistoricalIndexRates[key.ToString()].ContainsKey(dateKey.ToString()) && isOverride) || !_marketInfo.HistoricalIndexRates[key.ToString()].ContainsKey(dateKey.ToString()))
							{
								_marketInfo.HistoricalIndexRates[key.ToString()][dateKey.ToString()] = mktValues[dateKey];
								_qdpMarket.HistoricalIndexRates[key][dateKey] = mktValues[dateKey];
							}
						}
					}
					else
					{
						_marketInfo.HistoricalIndexRates[key.ToString()] = mktValues.ToDictionary(x=>x.Key.ToString(), y=>y.Value);
						_qdpMarket.HistoricalIndexRates[key] = mktValues.ToSortedDictionary();
					}
				}
			}
		}

		private void MergeSingleDataDef(MarketDataDefinition[] mktDataDefs, bool isTop)
		{
			if (isTop)
			{
				_qdpMarket.AddMarketDataDefinition(mktDataDefs);
			}
			else
			{
				var result = _qdpMarket.UpdateMarketRawData(mktDataDefs);
				result.WaitTillFinished();
			}
			foreach (var mktDataDef in mktDataDefs)
			{
				UpdateMarketInfo(mktDataDef);
			}
		}

		private void UpdateMarketInfo(MarketDataDefinition mktDataDef)
		{
			if (mktDataDef is BondMktData)
			{
				var bondIndex = _marketInfo.BondMktDatas.FirstOrDefaultIndexOf(x => x.BondId.Equals(mktDataDef.Name));
				if (bondIndex != null)
				{
					_marketInfo.BondMktDatas[bondIndex.Value] = (BondMktData) (mktDataDef);
				}
				else
				{
					var list = new List<BondMktData>();
					list.AddRange(_marketInfo.BondMktDatas);
					list.Add((BondMktData)mktDataDef);
					_marketInfo.BondMktDatas = list.ToArray();
				}
				_mktDataDict[mktDataDef.Name] = mktDataDef;
			}
			else if (mktDataDef is FuturesMktData)
			{
				var futuresIndex = _marketInfo.FuturesMktDatas.FirstOrDefaultIndexOf(x => x.FuturesId.Equals(mktDataDef.Name));
				if (futuresIndex != null)
				{
					_marketInfo.FuturesMktDatas[futuresIndex.Value] = (FuturesMktData)(mktDataDef);
				}
				else
				{
					var list = new List<FuturesMktData>();
					list.AddRange(_marketInfo.FuturesMktDatas);
					list.Add((FuturesMktData)mktDataDef);
					_marketInfo.FuturesMktDatas = list.ToArray();
				}
				_mktDataDict[mktDataDef.Name] = mktDataDef;
			}
			else if (mktDataDef is TreasuryFutureMktData)
			{
				var tfIndex = _marketInfo.TreasuryFutureMktData.FirstOrDefaultIndexOf(x => x.BondId.Equals(mktDataDef.Name));
				if (tfIndex != null)
				{
					_marketInfo.TreasuryFutureMktData[tfIndex.Value] = (TreasuryFutureMktData)(mktDataDef);
				}
				else
				{
					var list = new List<TreasuryFutureMktData>();
					list.AddRange(_marketInfo.TreasuryFutureMktData);
					list.Add((TreasuryFutureMktData)mktDataDef);
					_marketInfo.TreasuryFutureMktData = list.ToArray();
				}
				_mktDataDict[mktDataDef.Name] = mktDataDef;
			}
			else if (mktDataDef is InstrumentCurveDefinition)
			{
				var instrumentCurveIndex =
					_marketInfo.YieldCurveDefinitions.FirstOrDefaultIndexOf(x => x.Name.Equals(mktDataDef.Name));
				if (instrumentCurveIndex != null)
				{
					_marketInfo.YieldCurveDefinitions[instrumentCurveIndex.Value] = (InstrumentCurveDefinition) (mktDataDef);
				}
				else
				{
					var list = new List<InstrumentCurveDefinition>();
					list.AddRange(_marketInfo.YieldCurveDefinitions);
					list.Add((InstrumentCurveDefinition) mktDataDef);
					_marketInfo.YieldCurveDefinitions = list.ToArray();
				}
				_mktDataDict[mktDataDef.Name] = mktDataDef;
			}
			else if (mktDataDef is RateMktData)
			{
				var yieldCurveDefinitions = _marketInfo.YieldCurveDefinitions.Where(x => x.Name.Equals(mktDataDef.Name.Split('_')[0])).ToArray();
				if (yieldCurveDefinitions.Any())
				{
					foreach (var yieldCurveDefinition in yieldCurveDefinitions)
					{
						yieldCurveDefinition.MergeDependencies(mktDataDef);
						_mktDataDict[yieldCurveDefinition.Name].MergeDependencies(mktDataDef);
					}
				}
				_mktDataDict[mktDataDef.Name] = mktDataDef;
			}
			else if (mktDataDef is CurveConvention)
			{
				foreach (var yieldCurveDefinition in _marketInfo.YieldCurveDefinitions.Where(yieldCurveDefinition => yieldCurveDefinition.CurveConvention.Name.Equals(mktDataDef.Name)))
				{
					yieldCurveDefinition.CurveConvention = (CurveConvention)mktDataDef;
					_mktDataDict[yieldCurveDefinition.Name].MergeDependencies(mktDataDef);
				}
				_mktDataDict[mktDataDef.Name] = mktDataDef;
			}
		}

		public void RemoveDefinitions(MarketDataDefinition mktDataDef)
		{
			_qdpMarket.RemoveMarketDataDefinition(new[] { mktDataDef });
			RemoveMarketInfo(mktDataDef);
		}

		private void RemoveMarketInfo(MarketDataDefinition mktDataDef)
		{
			if (mktDataDef is BondMktData)
			{
				_marketInfo.BondMktDatas = _marketInfo.BondMktDatas.Remove(x => x.Name.Equals(mktDataDef.Name)).ToArray();
				_mktDataDict.Remove(mktDataDef.Name);
			}
			else if (mktDataDef is FuturesMktData)
			{
				_marketInfo.FuturesMktDatas = _marketInfo.FuturesMktDatas.Remove(x => x.Name.Equals(mktDataDef.Name)).ToArray();
				_mktDataDict.Remove(mktDataDef.Name);
			}
			else if (mktDataDef is TreasuryFutureMktData)
			{
				_marketInfo.TreasuryFutureMktData = _marketInfo.TreasuryFutureMktData.Remove(x => x.Name.Equals(mktDataDef.Name)).ToArray();
				_mktDataDict.Remove(mktDataDef.Name);
			}
			else if (mktDataDef is InstrumentCurveDefinition)
			{
				_marketInfo.YieldCurveDefinitions = _marketInfo.YieldCurveDefinitions.Remove(x => x.Name.Equals(mktDataDef.Name)).ToArray();
				_mktDataDict.Remove(mktDataDef.Name);
			}
			else if (mktDataDef is RateMktData)
			{
				var yieldCurveDefinitions = _marketInfo.YieldCurveDefinitions.Where(x => x.Name.Equals(mktDataDef.Name.Split('_')[0])).ToArray();
				if (yieldCurveDefinitions.Any())
				{
					foreach (var yieldCurveDefinition in yieldCurveDefinitions)
					{
						yieldCurveDefinition.RemoveDependencies(mktDataDef);
						_mktDataDict[yieldCurveDefinition.Name].RemoveDependencies(mktDataDef);
					}
				}
				_mktDataDict.Remove(mktDataDef.Name);
			}
            else if (mktDataDef is StockMktData)
            {
                _marketInfo.StockMktDatas = _marketInfo.StockMktDatas.Remove(x => x.Name.Equals(mktDataDef.Name)).ToArray();
                _mktDataDict.Remove(mktDataDef.Name);
            }
            else if (mktDataDef is VolSurfMktData)
            {
                _marketInfo.VolSurfMktDatas = _marketInfo.VolSurfMktDatas.Remove(x => x.Name.Equals(mktDataDef.Name)).ToArray();
                _mktDataDict.Remove(mktDataDef.Name);
            }
		}

		private MarketInfo _marketInfo;
		private QdpMarket _qdpMarket;
		private Dictionary<string, MarketDataDefinition> _mktDataDict;
	}
}
