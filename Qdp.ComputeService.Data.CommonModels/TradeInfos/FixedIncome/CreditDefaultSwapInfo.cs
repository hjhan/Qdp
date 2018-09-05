using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	[DataContract]
	[Serializable]
	public class CreditDefaultSwapInfo : TradeInfoBase
	{
		public CreditDefaultSwapInfo(string tradeId)
			: base(tradeId, "CreditDefaultSwap")
		{
		}

		public CreditDefaultSwapInfo()
			: base(string.Empty, "CreditDefaultSwap")
		{
		}

		[DataMember]
		public string StartDate { get; set; }

		[DataMember]
		public string MaturityDate { get; set; }

		[DataMember, DefaultValue(10000000.0)]
		public double Notional { get; set; }

		[DataMember]
		public string Currency { get; set; }

		[DataMember]
		public string SwapDirection { get; set; }

		[DataMember]
		public string Calendar { get; set; }

		[DataMember]
		public string DayCount { get; set; }

		[DataMember]
		public string Frequency { get; set; }

		[DataMember, DefaultValue("None")]
		public string BusinessDayConvention { get; set; }

		[DataMember]
		public string Stub { get; set; }

		[DataMember]
		public double Coupon { get; set; }

		[DataMember]
		public double RecoveryRate { get; set; }

		[DataMember, DefaultValue(40)]
		public int NumIntegrationInterval { get; set; }

		[DataMember]
		public CdsValuationParameters ValuationParameters { get; set; }
	}
}