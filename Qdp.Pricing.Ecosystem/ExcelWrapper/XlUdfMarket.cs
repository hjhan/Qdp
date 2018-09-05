
using System.Collections.Generic;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;

namespace Qdp.Pricing.Ecosystem.ExcelWrapper
{
	public partial class XlUdf
	{

		private static Dictionary<IndexType, SortedDictionary<Date, double>> _historicalIndexRates = new Dictionary<IndexType, SortedDictionary<Date, double>>();

		public static Dictionary<IndexType, SortedDictionary<Date, double>> HistoricalIndexRates
		{
			get { return _historicalIndexRates; }
		}

		/// <summary>
		/// Add bond market data definition to an existing market.
		/// </summary>
		/// <param name="marketName">The Existing Market Name</param>
		/// <param name="bondId">The Bond Id</param>
		/// <param name="quoteType">The Price quote type(Clean, Dirty, Ytm, YtmExecution)</param>
		/// <param name="quote">The quote of bond</param>
		/// <param name="isOverride">if true, then add or override. if false, then ignore(default true)</param>
		/// <returns>if succeeded, return true. if failed, return error message : "Market {market object name} is not available!"</returns>
		public static object xl_MktAddBondData(string marketName, string bondId, string quoteType, double quote, bool isOverride = true)
		{
			var marketData = new BondMktData(bondId, quoteType, quote);
			return XlManager.MergeMarketInfo(marketName, marketData, isOverride);
		}

		/// <summary>
		/// Add bondfutures market data definition to an existing market.
		/// </summary>
		/// <param name="marketName">The Existing Market Name</param>
		/// <param name="tfId">The Bond futures Id</param>
		/// <param name="quote">The quote of bond futures price</param>
		/// <param name="isOverride">if true, then add or override. if false, then ignore(default true)</param>
		/// <returns>if succeeded, return true. if failed, return error message : "Market {market object name} is not available!"</returns>
		public static object xl_MktAddFuturesData(string marketName, string tfId, double quote, bool isOverride = true)
		{
			var marketData = new FuturesMktData(tfId, quote);
			return XlManager.MergeMarketInfo(marketName, marketData, isOverride);
		}

		/// <summary>
		/// Add tf market data definition to an existing market.
		/// </summary>
		/// <param name="marketName">The Existing Market Name</param>
		/// <param name="tfId">The Bond futures Id</param>
		/// <param name="bondId">The Bond Id</param>
		/// <param name="quoteType">The Price quote type(Irr, Basis, NetBasis)</param>
		/// <param name="quote">The quote of bond futures</param>
		/// <param name="isOverride">if true, then add or override. if false, then ignore(default true)</param>
		/// <returns>if succeeded, return true. if failed, return error message : "Market {market object name} is not available!"</returns>
		public static object xl_MktAddTreasuryFutureData(string marketName, string tfId, string bondId, string quoteType, double quote, bool isOverride = true)
		{
			var mktObjName = tfId + "_" + bondId;
			var marketData = new TreasuryFutureMktData(mktObjName, quoteType, quote);
			return XlManager.MergeMarketInfo(marketName, marketData, isOverride);
		}

		/// <summary>
		/// Add rate market data definition to an existing market.
		/// </summary>
		/// <param name="marketName">The Existing Market Name</param>
		/// <param name="curveName">The curve name</param>
		/// <param name="indexType">The index type(enum IndexType)</param>
		/// <param name="instrumentType">The InstrumentType(enum InstrumentType)</param>
		/// <param name="tenor">The curve rate tenor</param>
		/// <param name="rate">The rate value</param>
		/// <param name="isOverride">if true, then add or override. if false, then ignore(default true)</param>
		/// <returns>if succeeded, return true. if failed, return error message : "Market {market object name} is not available!"</returns>
		public static object xl_MktAddRateData(string marketName, string curveName, string indexType, string instrumentType, string tenor, double rate, bool isOverride = true)
		{
			var marketData = new RateMktData(tenor, rate, indexType, instrumentType, curveName);
			return XlManager.MergeMarketInfo(marketName, marketData, isOverride);
		}

		/// <summary>
		/// Add curve convention market data definition to an existing market.
		/// </summary>
		/// <param name="marketName">The Existing Market Name</param>
		/// <param name="curveConventionName">The curve convention name</param>
		/// <param name="currency">The currency (enum CurrencyCode)</param>
		/// <param name="businessDayConvention">(enum BusinessDayConvention)</param>
		/// <param name="calendar">(enum Calendar)</param>
		/// <param name="dayCount">(enum DayCount)</param>
		/// <param name="compound">(enum Compound)</param>
		/// <param name="interpolation">(enum Interpolation)</param>
		/// <param name="isOverride">if true, then add or override. if false, then ignore(default true)</param>
		/// <returns>if succeeded, return true. if failed, return error message : "Market {market object name} is not available!"</returns>
		public static object xl_MktAddCurveConvention(string marketName, string curveConventionName,
			string currency,
			string businessDayConvention,
			string calendar,
			string dayCount,
			string compound,
			string interpolation,
			bool isOverride = true)
		{
			var marketData = new CurveConvention(curveConventionName, currency, businessDayConvention, calendar, dayCount, compound, interpolation);
			return XlManager.MergeMarketInfo(marketName, marketData, isOverride);
		}

		/// <summary>
		/// Add instrument curve market data definition to an existing market.
		/// </summary>
		/// <param name="marketName">The Existing Market Name</param>
		/// <param name="curveName">The curve name</param>
		/// <param name="curveConvention">The curveConvention</param>
		/// <param name="rateDefinitions">The rateMktData array</param>
		/// <param name="trait">(enum YieldCurveTrait)</param>
		/// <param name="baseCurveDefinition">The base InstrumentCurveDefinition(default null)</param>
		/// <param name="regriddedTenors">The regriddedTenors array(default null)</param>
		/// <param name="isOverride">if true, then add or override. if false, then ignore(default true)</param>
		/// <returns>if succeeded, return true. if failed, return error message : "Market {market object name} is not available!"</returns>
		public static object xl_MktAddYieldCurve(string marketName, string curveName,
			CurveConvention curveConvention,
			RateMktData[] rateDefinitions,
			string trait,
			InstrumentCurveDefinition baseCurveDefinition = null,
			string[] regriddedTenors = null,
			bool isOverride = true)
		{
			var marketData = new InstrumentCurveDefinition(curveName, curveConvention, rateDefinitions, trait, baseCurveDefinition, regriddedTenors);
			return XlManager.MergeMarketInfo(marketName, marketData, isOverride);
		}

		/// <summary>
		/// Add HistoricalIndexRates market data definition to an existing market.
		/// </summary>
		/// <param name="marketName">The Existing Market Name</param>
		/// <param name="indexType">The index type(enum IndexType)</param>
		/// <param name="indexDate">The index date</param>
		/// <param name="indexRate">The rate</param>
		/// <param name="isOverride">if true, then add or override. if false, then ignore(default true)</param>
		/// <returns>if succeeded, return true. if failed, return error message : "Market {market object name} is not available!"</returns>
		public static object xl_MktAddHistoricalIndexRates(string marketName, IndexType indexType, Date indexDate, double indexRate, bool isOverride = true)
		{
			var hisDic = new Dictionary<IndexType, Dictionary<Date, double>>{{indexType, new Dictionary<Date, double>{{indexDate, indexRate}}}};
			return XlManager.MergeMarketInfo(marketName, hisDic, isOverride);
		}

		/// <summary>
		/// Add HistoricalIndexRates market data definition to an existing excel market.
		/// </summary>
		/// <param name="indexType">The index type(enum IndexType)</param>
		/// <param name="indexDate">The index date</param>
		/// <param name="indexRate">The rate</param>
		/// <param name="isOverride">if true, then add or override. if false, then ignore(default true)</param>
		/// <returns>if added, return true. if not added, return false.</returns>
		public static object xl_MktAddHistoricalIndexRates(IndexType indexType, Date indexDate, double indexRate, bool isOverride = true)
		{
			if (_historicalIndexRates.ContainsKey(indexType))
			{
				if ((_historicalIndexRates[indexType].ContainsKey(indexDate) && isOverride) || (!_historicalIndexRates[indexType].ContainsKey(indexDate)))
				{
					_historicalIndexRates[indexType][indexDate] = indexRate;
					return true;
				}
			}
			else if (!_historicalIndexRates.ContainsKey(indexType))
			{
				_historicalIndexRates[indexType] = new SortedDictionary<Date, double> { { indexDate, indexRate } };
				return true;
			}
			return false;
		}

		/// <summary>
		/// Add HistoricalIndexRates market data definition to an existing excel market.
		/// </summary>
		/// <param name="historicalIndexRates">HistoricalIndexRates</param>
		/// <param name="isOverride">if true, then add or override. if false, then ignore(default true)</param>
		/// <returns>if added, return true. if not added, return false.</returns>
		public static object xl_MktAddHistoricalIndexRates(Dictionary<IndexType, SortedDictionary<Date, double>> historicalIndexRates, bool isOverride = true)
		{
			var isSet = false;
			foreach (var indexType in historicalIndexRates.Keys)
			{
				if (_historicalIndexRates.ContainsKey(indexType))
				{
					foreach (var indexDate in historicalIndexRates[indexType].Keys)
					{
						if ((_historicalIndexRates[indexType].ContainsKey(indexDate) && isOverride) ||
						    (!_historicalIndexRates[indexType].ContainsKey(indexDate)))
						{
							_historicalIndexRates[indexType][indexDate] = historicalIndexRates[indexType][indexDate];
							isSet = true;
						}
					}
				}
				else if (!_historicalIndexRates.ContainsKey(indexType))
				{
					_historicalIndexRates[indexType] = historicalIndexRates[indexType];
					isSet = true;
				}
			}
			return isSet;
		}
	}
}
