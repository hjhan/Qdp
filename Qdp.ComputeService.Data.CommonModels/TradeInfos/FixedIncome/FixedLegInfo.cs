using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	[DataContract]
	[Serializable]
	public class FixedLegInfo : TradeInfoBase
	{
		public FixedLegInfo(string tradeId)
			: base(tradeId, "FixedLeg")
		{
		}

		private FixedLegInfo()
			: base(Guid.NewGuid().ToString(), "FixedLeg")
		{
		}

		[DataMember]
		public string StartDate { get; set; }

		[DataMember]
		public string MaturityDate { get; set; }

		[DataMember]
		public string Tenor { get; set; }

		[DataMember]
		public string SwapDirection { get; set; }

		[DataMember, DefaultValue(10000000.0)]
		public double Notional { get; set; }

		[DataMember]
		public string Currency { get; set; }

		[DataMember]
		public string Calendar { get; set; }

		[DataMember]
		public string FixedLegDC { get; set; }

		[DataMember]
		public string FixedLegFreq { get; set; }

		[DataMember]
		public string FixedLegBD { get; set; }

		[DataMember]
		public string FixedLegStub { get; set; }

		[DataMember]
		public double FixedLegCoupon { get; set; }

		[DataMember]
		public SimpleCfValuationParameters ValuationParamters { get; set; }
		public override ValuationParameters GetValuationParamters()
		{
			return ValuationParamters;
		}
	}
}