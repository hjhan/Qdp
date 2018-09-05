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
	public class MktInstrumentIrsRule
	{
		public static readonly Dictionary<IndexType, MktIrsJson> MktIrsRule;

		static MktInstrumentIrsRule()
		{
			var configReader = new ConfigFileTextReader("Configurations", "MarketInstrumentConventions", "Irs.cfg");
			MktIrsRule = DataContractJsonObjectSerializer.Deserialize<MktIrsJson[]>(configReader.ReadAllText())
				.ToDictionary(x => x.IndexType.ToIndexType(), x => x);
		}
	}

	[DataContract]
	[Serializable]
	public class MktIrsJson
	{
		[DataMember]
		public string IndexType { get; set; }

		[DataMember]
		public InterestRateSwapInfo InterestRateSwapInfo { get; set; }

		[DataMember]
		public string CalibrationMethod { get; set; }
	}
}