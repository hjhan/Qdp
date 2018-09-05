using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos
{
	[DataContract]
	[Serializable]
	public class FuturesMktData : MarketDataDefinition
	{
		public FuturesMktData(string futuresId,
			double futuresPrice)
			: base(futuresId)
		{
			FuturesId = futuresId;
			FuturesPrice = futuresPrice;
		}

		public FuturesMktData()
		{
		}

		[DataMember]
		public string FuturesId { get; set; }

		[DataMember]
		public double FuturesPrice { get; set; }
	}
}