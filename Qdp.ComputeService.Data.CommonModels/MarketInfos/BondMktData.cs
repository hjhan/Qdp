using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos
{
	[DataContract]
	[Serializable]
	public class BondMktData : MarketDataDefinition
	{
		public BondMktData(string bondId,
			string priceQuoteType,
			double quote)
			: base(bondId)
		{
			BondId = bondId;
			PriceQuoteType = priceQuoteType;
			Quote = quote;
		}

		public BondMktData()
		{
		}

		[DataMember]
		public string BondId { get; set; }

		[DataMember]
		public string PriceQuoteType { get; set; }

		[DataMember]
		public double Quote { get; set; }
		public double CleanPrice { get; set; }
		public double DirtyPrice { get; set; }
		public double Ytm { get; set; }
	}
}