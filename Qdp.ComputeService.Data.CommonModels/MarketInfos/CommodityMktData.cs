using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos
{
	[DataContract]
	[Serializable]
	public class CommodityMktData : MarketDataDefinition
	{
		public CommodityMktData(string id, double price)
			: base(id)
		{
			Id = id;
			Price = price;
		}

		private CommodityMktData()
		{
		}

		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public double Price { get; set; }
	}
}