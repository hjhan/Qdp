using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	[DataContract]
	[Serializable]
	public class FloatingLegInfo : TradeInfoBase
	{
		public FloatingLegInfo(string tradeId)
			: base(tradeId, "FloatingLeg")
		{
		}

		private FloatingLegInfo()
			: base(string.Empty, "FloatingLeg")
		{
		}

		[DataMember]
		public string StartDate { get; set; }

		[DataMember]
		public string MaturityDate { get; set; }

		[DataMember]
		public string Tenor { get; set; }

		[DataMember, DefaultValue(10000000.0)]
		public double Notional { get; set; }

		[DataMember]
		public string SwapDirection { get; set; }

		[DataMember]
		public string Currency { get; set; }

		[DataMember]
		public string Calendar { get; set; }

		[DataMember]
		public string FloatingLegDC { get; set; }

		[DataMember]
		public string FloatingLegFreq { get; set; }

		[DataMember]
		public string FloatingLegBD { get; set; }

		[DataMember]
		public string FloatingLegStub { get; set; }

		[DataMember]
		public string Index { get; set; }

		[DataMember, DefaultValue(null)]
		public string ResetTerm { get; set; }

		[DataMember]
		public string ResetStub { get; set; }

		[DataMember]
		public string ResetBD { get; set; }

		[DataMember]
		public string ResetToFixingGap { get; set; }

		[DataMember]
		public string ResetCompound { get; set; }

		[DataMember]
		public SimpleCfValuationParameters ValuationParamters { get; set; }

		[DataMember, DefaultValue(null)]
		public List<string> ForwardSwapStartTenors { get; set; }

		[DataMember, DefaultValue(null)]
		public List<string> ForwardSwapEndTenors { get; set; }
		public override ValuationParameters GetValuationParamters()
		{
			return ValuationParamters;
		}
	}
}