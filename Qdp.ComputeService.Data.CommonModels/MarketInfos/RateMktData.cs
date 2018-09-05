using System;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.TradeInfos;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos
{
	[DataContract]
	[Serializable]
	public class RateMktData : MarketDataDefinition
	{
		public RateMktData(string tenor, double rate,
			string indexType,
			string instrumentType,
			string curveName,
			TradeInfoBase tradeInfo = null)
		{
			Tenor = tenor;
			Rate = rate;
			IndexType = indexType;
			InstrumentType = tradeInfo == null ? instrumentType : tradeInfo.InstrumentType;
			TradeInfo = tradeInfo;
			Name = tradeInfo == null
				? (string.Format("{0}_{1}_{2}_{3}", string.IsNullOrEmpty(curveName) ? "" : curveName, indexType, instrumentType, tenor))
				: string.Format("{0}_{1}_{2}", string.IsNullOrEmpty(curveName) ? "" : curveName, tradeInfo.InstrumentType, tradeInfo.TradeId);
		}

		public RateMktData()
		{
		}

		[DataMember]
		public TradeInfoBase TradeInfo { get; private set; }

		[DataMember]
		public string IndexType { get; private set; }

		[DataMember]
		public string Tenor { get; private set; }

		[DataMember]
		public double Rate { get; private set; }

		[DataMember]
		public string InstrumentType { get; private set; }

		public bool IsTerm()
		{
			return Tenor.IsTerm();
		}

		public object[,] ToLabelObjects()
		{
			var ret = new object[6, 2];
			ret[0, 0] = "Name";
			ret[0, 1] = Name;
			ret[1, 0] = "IndexType";
			ret[1, 1] = IndexType;
			ret[2, 0] = "InstrumentType";
			ret[2, 1] = InstrumentType;
			ret[3, 0] = "Tenor";
			ret[3, 1] = Tenor;
			ret[4, 0] = "TradeInfo";
			ret[4, 1] = TradeInfo==null?null:TradeInfo.TradeId;
			ret[5, 0] = "Rate";
			ret[5, 1] = Rate;
			return ret;
		}
	}
}