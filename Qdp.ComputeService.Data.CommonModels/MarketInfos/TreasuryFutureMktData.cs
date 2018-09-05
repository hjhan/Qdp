using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos
{
	[DataContract]
	[Serializable]
	public class TreasuryFutureMktData : MarketDataDefinition
	{
		public TreasuryFutureMktData(string bondId,
			string priceQuoteType,
			double quote)
			: base(bondId)
		{
			BondId = bondId;
			PriceQuoteType = priceQuoteType;
			Quote = quote;
		}

		public TreasuryFutureMktData()
		{
		}

		[DataMember]
		public string BondId { get; set; }

		[DataMember]
		public string PriceQuoteType { get; set; }

		[DataMember]
		public double Quote { get; set; }
	}
}