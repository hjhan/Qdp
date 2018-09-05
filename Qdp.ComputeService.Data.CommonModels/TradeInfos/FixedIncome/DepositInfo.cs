using System;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	[DataContract]
	[Serializable]
	public sealed class DepositInfo : TradeInfoBase
	{
		public DepositInfo(string tradeId)
			: base(tradeId, "Deposit")	
		{
		}

		public DepositInfo()
			: base(Guid.NewGuid().ToString(), "Deposit")	
		{
		}

		[DataMember]
		public string StartDate { get; set; }

		[DataMember]
		public string MaturityDate { get; set; }

		[DataMember]
		public string Currency { get; set; }

		[DataMember]
		public double Coupon { get; set; }

		[DataMember]
		public string Calendar { get; set; }

		[DataMember]
		public string BusinessDayConvention { get; set; }

		[DataMember]
		public string DayCount { get; set; }

		[DataMember]
		public SimpleCfValuationParameters ValuationParamters { get; set; }
		public override ValuationParameters GetValuationParamters()
		{
			return ValuationParamters;
		}
	}
}