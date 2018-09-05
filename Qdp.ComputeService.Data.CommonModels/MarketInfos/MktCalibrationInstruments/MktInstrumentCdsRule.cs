using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Foundation.ConfigFileReaders;
using Qdp.Foundation.Serializer;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos.MktCalibrationInstruments
{
	public class MktInstrumentCdsRule
	{
		public static readonly Dictionary<IndexType, MktCdsJson> MktCdsRule;

		static MktInstrumentCdsRule()
		{
			var configReader = new ConfigFileTextReader("Configurations", "MarketInstrumentConventions", "Cds.cfg");
			MktCdsRule = DataContractJsonObjectSerializer.Deserialize<MktCdsJson[]>(configReader.ReadAllText())
				.ToDictionary(x => x.IndexType.ToIndexType(), x => x);
		}
	}

	[DataContract]
	[Serializable]
	public class MktCdsJson
	{
		[DataMember]
		public string IndexType { get; set; }

		[DataMember]
		public CreditDefaultSwapInfo CreditDefaultSwapInfo { get; set; }
	}
}