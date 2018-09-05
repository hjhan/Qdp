using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.Foundation.Implementations;
using Qdp.Foundation.Utilities;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Ecosystem.Market
{
	public class MarketFunctions
	{
		public static Result<string> BuildMarket(MarketInfo marketInfo, out QdpMarket qdpMarket)
		{

            marketInfo.GetClassifiedDefinitions(out MarketDataDefinition[] rawDefinitions, out MarketDataDefinition[] ripeDefinitions);
            var historicalIndexRates = new Dictionary<IndexType, SortedDictionary<Date, double>>();
			if (marketInfo.HistoricalIndexRates != null)
			{
				foreach (var historicalIndexKey in marketInfo.HistoricalIndexRates.Keys)
				{
					var indexKey = historicalIndexKey.ToIndexType();
					var historicalIndexDateDic = new SortedDictionary<Date, double>();
					foreach (var historicalIndexDateKey in marketInfo.HistoricalIndexRates[historicalIndexKey].Keys)
					{
						historicalIndexDateDic.Add(historicalIndexDateKey.ToDate(),marketInfo.HistoricalIndexRates[historicalIndexKey][historicalIndexDateKey]);
					}
					historicalIndexRates.Add(indexKey, historicalIndexDateDic);
				}
			}

			qdpMarket = new QdpMarket(marketInfo.MarketName, marketInfo.ReferenceDate.ToDate(), historicalIndexRates, ripeDefinitions);
			var result = qdpMarket.UpdateMarketRawData(rawDefinitions);
			result.WaitTillFinished();

			return result;
		}
	}
}
