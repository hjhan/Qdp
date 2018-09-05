using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.Utilities;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos
{
	[DataContract]
	[Serializable]
	[KnownType(typeof (InstrumentCurveDefinition))]
	public class MarketInfo
	{
		public MarketInfo()
		{
		}

		public MarketInfo(string marketName,
			string referenceDate = null,
			InstrumentCurveDefinition[] yieldCurveDefinitions = null,
			Dictionary<string, Dictionary<string, double>> historicalIndexRates = null,
			BondMktData[] bondDataDefinitions = null,
			CommodityMktData[] commodityDataDefinitions = null,
			VolSurfMktData[] volSurfaceDefinitions = null,
            CorrSurfMktData[] corrSurfMktDefinitions = null,
            StockMktData[] stockDataDefinitions = null,
			InstrumentCurveDefinition[] spcCurveDefinitions = null,
			FuturesMktData[] futuresMktDatas = null,
			TreasuryFutureMktData[] tfMktData = null,
			bool doNotCache = false,
			bool overrideExisting = false
			)
		{
			MarketName = marketName;
			ReferenceDate = referenceDate;
			YieldCurveDefinitions = yieldCurveDefinitions ?? new InstrumentCurveDefinition[0];
			HistoricalIndexRates = historicalIndexRates ?? new Dictionary<string, Dictionary<string, double>>();
			BondMktDatas = bondDataDefinitions ?? new BondMktData[0];
			CommodityMktDatas = commodityDataDefinitions ?? new CommodityMktData[0];
			VolSurfMktDatas = volSurfaceDefinitions ?? new VolSurfMktData[0];
            CorrSurfMktDatas = corrSurfMktDefinitions ?? new CorrSurfMktData[0];
            StockMktDatas = stockDataDefinitions ?? new StockMktData[0];
			SpcCurveDefinitions = spcCurveDefinitions ?? new InstrumentCurveDefinition[0];
			FuturesMktDatas = futuresMktDatas ?? new FuturesMktData[0];
			TreasuryFutureMktData = tfMktData ?? new TreasuryFutureMktData[0];
			DoNotCache = doNotCache;
			OverrideExisting = overrideExisting;
		}


        public MarketInfo Copy(
            string marketName,
            string referenceDateNew = null,
            InstrumentCurveDefinition[] yieldCurveDefinitionsNew = null,
            Dictionary<string, Dictionary<string, double>> historicalIndexRatesNew = null,
            BondMktData[] bondDataDefinitionsNew = null,
            CommodityMktData[] commodityDataDefinitionsNew = null,
            VolSurfMktData[] volSurfaceDefinitionsNew = null,
            CorrSurfMktData[] corrSurfaceDedinitionsNew = null,
            StockMktData[] stockDataDefinitionsNew = null,
            InstrumentCurveDefinition[] spcCurveDefinitionsNew = null,
            FuturesMktData[] futuresMktDatasNew = null,
            TreasuryFutureMktData[] tfMktDataNew = null) {
            return new MarketInfo(
                marketName: marketName,
                referenceDate: referenceDateNew?? this.ReferenceDate,
                yieldCurveDefinitions: yieldCurveDefinitionsNew ?? this.YieldCurveDefinitions,
                historicalIndexRates: historicalIndexRatesNew ?? this.HistoricalIndexRates,
                bondDataDefinitions: bondDataDefinitionsNew ?? this.BondMktDatas,
                commodityDataDefinitions: commodityDataDefinitionsNew ?? this.CommodityMktDatas,
                volSurfaceDefinitions: volSurfaceDefinitionsNew ?? this.VolSurfMktDatas,
                corrSurfMktDefinitions: corrSurfaceDedinitionsNew ?? this.CorrSurfMktDatas,
                stockDataDefinitions: stockDataDefinitionsNew ?? this.StockMktDatas,
                spcCurveDefinitions: spcCurveDefinitionsNew ?? this.SpcCurveDefinitions,
                futuresMktDatas: futuresMktDatasNew ?? this.FuturesMktDatas,
                tfMktData: tfMktDataNew ?? this.TreasuryFutureMktData,
                doNotCache: this.DoNotCache,
                overrideExisting: this.OverrideExisting
                );
        }


		[DataMember]
		public string MarketName { get; set; }

		[DataMember]
		public string ReferenceDate { get; set; }

		[DataMember]
		public InstrumentCurveDefinition[] YieldCurveDefinitions { get; set; }

		[DataMember]
		public InstrumentCurveDefinition[] SpcCurveDefinitions { get; set; }

		[DataMember]
		public VolSurfMktData[] VolSurfMktDatas { get; set; }

        [DataMember]
        public CorrSurfMktData[] CorrSurfMktDatas { get; set; }

        [DataMember]
		public Dictionary<string, Dictionary<string, double>> HistoricalIndexRates { get; set; }

		[DataMember]
		public BondMktData[] BondMktDatas { get; set; }

		[DataMember]
		public CommodityMktData[] CommodityMktDatas { get; set; }

		[DataMember]
		public StockMktData[] StockMktDatas { get; set; }

		[DataMember]
		public FuturesMktData[] FuturesMktDatas { get; set; }

        /// <summary>
        /// 国债期货可交割券数据
        /// </summary>
		[DataMember]
		public TreasuryFutureMktData[] TreasuryFutureMktData { get; set; }

        //not used
		[DataMember]
		public bool DoNotCache { get; set; }

        //not used
		[DataMember]
		public bool OverrideExisting { get; set; }

		[DataMember]
		public MktCacheConfig MktCacheConfig { get; set; }

		[DataMember]
		public MktBuildConfig MktBuildConfig { get; set; }

		public bool IsEmpty()
		{
			return (YieldCurveDefinitions == null || YieldCurveDefinitions.Length == 0)
				   && (BondMktDatas == null || BondMktDatas.Length == 0)
				   && (TreasuryFutureMktData == null || TreasuryFutureMktData.Length == 0)
			       && (CommodityMktDatas == null || CommodityMktDatas.Length == 0)
			       && (VolSurfMktDatas == null || VolSurfMktDatas.Length == 0)
                   && (CorrSurfMktDatas == null || CorrSurfMktDatas.Length == 0)
                   && (StockMktDatas == null || StockMktDatas.Length == 0)
			       && (SpcCurveDefinitions == null || SpcCurveDefinitions.Length == 0)
			       && (HistoricalIndexRates == null || HistoricalIndexRates.Count == 0);
		}

		public void GetClassifiedDefinitions(out MarketDataDefinition[] rawDefinitions,
			out MarketDataDefinition[] ripeDefinitions)
		{
			var rawDefs = new List<MarketDataDefinition>();
			var ripeDefs = new List<MarketDataDefinition>();
			var allDefinitions = GetAllMarketDataDefinitions();
			foreach (var marketDataDefinition in allDefinitions)
			{
				MarketDataDefinition[] raw;
				MarketDataDefinition[] ripe;
				GetTreeClassifiedDefinitions(marketDataDefinition, out raw, out ripe);
				rawDefs.AddRange(raw);
				ripeDefs.AddRange(ripe);
				if (marketDataDefinition.GetDependencies().Length == 0)
				{
					rawDefs.Add(marketDataDefinition);
				}
				else
				{
					ripeDefs.Add(marketDataDefinition);
				}
			}
			rawDefinitions = rawDefs.GroupBy(x => x.Name).Select(x => x.First()).ToArray();
			ripeDefinitions = ripeDefs.GroupBy(x => x.Name).Select(x => x.First()).ToArray();
		}

		public IEnumerable<MarketDataDefinition> GetAllMarketDataDefinitions()
		{
			return YieldCurveDefinitions
				.Union<MarketDataDefinition>(BondMktDatas)
				.Union(CommodityMktDatas)
				.Union(VolSurfMktDatas)
                .Union(CorrSurfMktDatas)
                .Union(StockMktDatas)
				.Union(SpcCurveDefinitions)
				.Union(FuturesMktDatas)
				.Union(TreasuryFutureMktData)
				.ToArray();
		}

		public object[,] InstrumentCurveDefinitionsToLabelData(InstrumentCurveDefinition[] instrumentCurveDefinitions)
		{
			if (instrumentCurveDefinitions != null && instrumentCurveDefinitions.Length > 0)
			{
				var ret = new object[instrumentCurveDefinitions.Length + 1, 6];
				ret[0, 0] = "Name";
				ret[0, 1] = "RateDefinitions";
				ret[0, 2] = "CurveConvention";
				ret[0, 3] = "Trait";
				ret[0, 4] = "BaseCurveDefinition";
				ret[0, 5] = "RegriddedTenors";
				int i = 1;
				foreach (var instrumentCurveDefinition in instrumentCurveDefinitions)
				{
					ret[i, 0] = instrumentCurveDefinition.Name;
					ret[i, 1] = instrumentCurveDefinition.RateDefinitions.Select(x=>x.Name).Aggregate("", (current, x) => current + "," + x).Substring(1);
					ret[i, 2] = instrumentCurveDefinition.CurveConvention.Name;
					ret[i, 3] = instrumentCurveDefinition.Trait;
					ret[i, 4] = instrumentCurveDefinition.BaseCurveDefinition == null ? null : instrumentCurveDefinition.BaseCurveDefinition.Name;
					ret[i, 5] = instrumentCurveDefinition.RegriddedTenors;
					i++;
				}
				return ret;
			}
			else
			{
				return null;
			}
		}

		public Dictionary<string, object> HistoricalIndexRatesToLabelData()
		{
			var ret = new Dictionary<string, object>();
			foreach (var key in HistoricalIndexRates.Keys)
			{
				ret[key] = HistoricalIndexRates[key].To2DArray();
			}
			return ret;
		}

		public object[,] VolSurfMktDatasToLabelData()
		{
			if (VolSurfMktDatas == null || VolSurfMktDatas.Length <= 0)
			{
				return null;
			}
			return VolSurfMktDatas.ToLableData(new List<string>() { "VolSurfaces"}, 5);
		}

        public object[,] CorrSurfMktDatasToLabelData()
        {
            if (CorrSurfMktDatas == null || CorrSurfMktDatas.Length <= 0)
            {
                return null;
            }
            return CorrSurfMktDatas.ToLableData(new List<string>() { "Correlation" }, 5);
        }
        public object[,] YieldCurveDefinitionsToLabelData()
		{
			return InstrumentCurveDefinitionsToLabelData(YieldCurveDefinitions);
		}

		public object[,] SpcCurveDefinitionsToLabelData()
		{
			return InstrumentCurveDefinitionsToLabelData(SpcCurveDefinitions);
		}

		public object[,] BondMktDatasToLabelData()
		{
			if (BondMktDatas == null || BondMktDatas.Length <= 0)
			{
				return null;
			}
			return BondMktDatas.ToLableData(new List<string>(){"CleanPrice", "DirtyPrice","Ytm"},5);
		}

		public object[,] CommodityMktDatasToLabelData()
		{
			if (CommodityMktDatas == null || CommodityMktDatas.Length <= 0)
			{
				return null;
			}
			return CommodityMktDatas.ToLableData(new List<string>(),4);
		}

		public object[,] FuturesMktDatasToLabelData()
		{
			if (FuturesMktDatas == null || FuturesMktDatas.Length <= 0)
			{
				return null;
			}
			return FuturesMktDatas.ToLableData(new List<string>(),4);
		}

		public object[,] StockMktDatasToLabelData()
		{
			if (StockMktDatas == null || StockMktDatas.Length <= 0)
			{
				return null;
			}
			return StockMktDatas.ToLableData(new List<string>(),4);
		}

		public object[,] TreasuryFutureMktDataToLabelData()
		{
			if (TreasuryFutureMktData == null || TreasuryFutureMktData.Length <= 0)
			{
				return null;
			}
			return TreasuryFutureMktData.ToLableData(new List<string>(), 5);
		}

		private void GetTreeClassifiedDefinitions(MarketDataDefinition root, out MarketDataDefinition[] rawDefinitions,
			out MarketDataDefinition[] ripeDefinitions)
		{
			var rawDefs = new List<MarketDataDefinition>();
			var ripeDefs = new List<MarketDataDefinition>();

			foreach (var marketDataDefinition in root.GetDependencies())
			{
				MarketDataDefinition[] childrenRaw;
				MarketDataDefinition[] childrenRipe;
				GetTreeClassifiedDefinitions(marketDataDefinition, out childrenRaw, out childrenRipe);
				rawDefs.AddRange(childrenRaw);
				ripeDefs.AddRange(childrenRipe);
				if (marketDataDefinition.GetDependencies().Length == 0)
				{
					rawDefs.Add(marketDataDefinition);
				}
				else
				{
					ripeDefs.Add(marketDataDefinition);
				}
			}
			rawDefinitions = rawDefs.ToArray();
			ripeDefinitions = ripeDefs.ToArray();
		}
	}
}