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
	public class MktInstrumentDepositRule
	{
		public static readonly Dictionary<IndexType, MktDepositJson> MktDepositRule;

		static MktInstrumentDepositRule()
		{
			var configReader2 = new ConfigFileTextReader("Configurations", "MarketInstrumentConventions", "Deposit.cfg");
			MktDepositRule = DataContractJsonObjectSerializer.Deserialize<MktDepositJson[]>(configReader2.ReadAllText())
				.ToDictionary(x => x.IndexType.ToIndexType(), x => x);
		}
	}

	[DataContract]
	[Serializable]
	public class MktDepositJson
	{
		[DataMember]
		public DepositInfo DepositInfo { get; set; }

		[DataMember]
		public string IndexType { get; set; }
	}
}