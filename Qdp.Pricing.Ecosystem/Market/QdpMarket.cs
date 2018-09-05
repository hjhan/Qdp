using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
#if !NETCOREAPP2_1
using System.Windows.Threading;
#endif
using log4net;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Implementations;
using Qdp.Foundation.Utilities;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.DependencyTree;
using Qdp.Pricing.Ecosystem.Market.YieldCurveDependentObjects;
using Qdp.Pricing.Ecosystem.Trade;

namespace Qdp.Pricing.Ecosystem.Market
{
	public class QdpMarket : IQdpMarket
	{
		private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public string MarketName { get; protected set; }
		public Date ReferenceDate { get; protected set; }
		private readonly DependentObjectManager _doManager = new DependentObjectManager();
        //private readonly Dispatcher _dispatcher;
#if !NETCOREAPP2_1
        private const DispatcherPriority RawDataUpdatePriority = DispatcherPriority.Normal;
		private const DispatcherPriority TradeValuePriority = DispatcherPriority.Background;
		private const DispatcherPriority DataGetPriority = DispatcherPriority.Background;
#endif

#region reference market data
		public Dictionary<IndexType, SortedDictionary<Date, double>> HistoricalIndexRates { get; private set; }
#endregion

        public QdpMarket()
        { }

		public QdpMarket(string marketName,
			Date referenceDate,
			Dictionary<IndexType, SortedDictionary<Date, double>> historicalIndexRates,
			params MarketDataDefinition[] marketDataDefinition)
		{
			MarketName = marketName;
			ReferenceDate = referenceDate;
			HistoricalIndexRates = historicalIndexRates;
			//_dispatcher = DispatcherHelper.CreateNewThreadDispatcher(string.Format("{0}_mktDispatcher", MarketName), ThreadPriority.Normal, true);
			if (marketDataDefinition != null)
			{
				AddMarketDataDefinition(marketDataDefinition);
			}
		}

		public void AddMarketDataDefinition(IEnumerable<MarketDataDefinition> marketDataDefinition)
		{
			foreach (var definition in marketDataDefinition)
			{
				if (definition is InstrumentCurveDefinition)
				{
					_doManager.GetObject(definition.Name, out InstrumentCurveObject dobj);
					dobj.DependsOn(definition.GetDependencies().Select(x => x.Name).ToArray());
					dobj.Market = this;
					dobj.Definition = definition as InstrumentCurveDefinition;
					dobj.UpdateValue(definition);
				}
				else
				{
                    _doManager.GetObject(definition.Name, out DependentObject dobj);
                    dobj.DependsOn(definition.GetDependencies().Select(x => x.Name).ToArray());
					dobj.UpdateValue(definition);
				}
			}
		}

		public void RemoveMarketDataDefinition(IEnumerable<MarketDataDefinition> marketDataDefinition)
		{
			foreach (var definition in marketDataDefinition)
			{
				if (definition is InstrumentCurveDefinition)
				{
                    _doManager.RemoveObject(definition.Name, out InstrumentCurveObject dobj);
                }
				else if (!(definition is CurveConvention))
				{
                    _doManager.GetObject(definition.Name, out DependentObject dobj);
                    _doManager.GetDependByObject(definition.Guid, out string[] dobjs);
					dobj.RemoveDepends(dobjs);
					_doManager.RemoveObject(definition.Name, out dobj);
				}
			}
		}

		public Result<string> UpdateMarketRawData(IEnumerable<MarketDataDefinition> definitions)
		{
			lock (_doManager)
			{
				var ret = string.Empty;
				var result = new Result<string>();
				//_dispatcher.BeginInvoke(new Action(() =>
				//{
				//	try
				//	{
				//		foreach (var definition in definitions)
				//		{
				//			_doManager.Update(definition.Name, definition);
				//		}
				//	}
				//	catch (Exception ex)
				//	{
				//		Logger.Error(string.Format("Error when building market raw data {0}", ex.GetDetail()));
				//	}

				//	try
				//	{
				//		_doManager.ClearDependencyTree();
				//	}
				//	catch (Exception ex)
				//	{
				//		Logger.Error(string.Format("Error when building dependency tree {0}", ex.GetDetail()));
				//	}
				//	result.Update(ret, true);
				//}), RawDataUpdatePriority);

                try
                {
                    foreach (var definition in definitions)
                    {
                        _doManager.Update(definition.Name, definition);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Error when building market raw data {0}", ex.GetDetail()));
                }

                try
                {
                    _doManager.ClearDependencyTree();
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Error when building dependency tree {0}", ex.GetDetail()));
                }
                result.Update(ret, true);

                return result;
			}
		}

        //not used
		public IPricingResult[] ValueTrades(IEnumerable<IValuationFunction> trades, IEnumerable<PricingRequest> requests)
		{
			var results = new List<IPricingResult>();
			var arrRequests = requests.ToArray();

            //_dispatcher.Invoke(new Action(() =>
			//{
			//	try
			//	{
			//		results.AddRange(trades.Select((trade, i) => trade.ValueTrade(this, arrRequests[i])));
			//	}
			//	catch (Exception ex)
			//	{
			//		Logger.ErrorFormat("Error when valuing trades {0}", ex);
			//	}
			//}), TradeValuePriority);

            try
            {
                results.AddRange(trades.Select((trade, i) => trade.ValueTrade(this, arrRequests[i])));
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Error when valuing trades {0}", ex);
            }

            return results.ToArray();
		}

		public T GetData<T>(string key) where T : GuidObject
		{
			DependentObject dobj = null;
			//_dispatcher.Invoke(new Action(() =>
			//{
			//	try
			//	{
			//		_doManager.GetObject(key, out dobj);
			//	}
			//	catch (Exception ex)
			//	{
			//		Logger.Error(string.Format("Error when building dependency tree {0}", ex.GetDetail()));
			//	}
			//}), DataGetPriority);

            try
            {
                _doManager.GetObject(key, out dobj);
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("Error when building dependency tree {0}", ex.GetDetail()));
            }

            if (dobj == null)
			{
				throw new PricingBaseException(string.Format("Cannot find market object {0}", key));
			}

			return dobj.Value as T;
		}

		public Dictionary<string, object> GetAllData()
		{
			var ret = new Dictionary<string, object>();
			var filteredObjects = _doManager.GetAllObject();
			foreach (var key in filteredObjects.Keys)
			{
				if (filteredObjects[key] is RateMktData)
				{
					var rateMktData = filteredObjects[key] as RateMktData;
					ret[key] = rateMktData.ToLabelObjects();
				}
				else
				{
					var curveData = (CurveConvention)filteredObjects[key];
					ret[key] = curveData.ToLabelObjects();
				}
			}
			return ret;
		}

		public void Dispose()
		{
			//_dispatcher.InvokeShutdown();
			_doManager.Dispose();
		}

	}
}
