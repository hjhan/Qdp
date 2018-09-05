using System;
using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Ecosystem.Trade;

namespace Qdp.Pricing.Ecosystem.Market
{
	public interface IQdpMarket : IDisposable
	{
		string MarketName { get; }
		Date ReferenceDate { get; }
		IPricingResult[] ValueTrades(IEnumerable<IValuationFunction> trades, IEnumerable<PricingRequest> requests);
		Dictionary<IndexType, SortedDictionary<Date, double>> HistoricalIndexRates { get; }
	}
}
