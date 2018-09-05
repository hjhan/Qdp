using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	[DataContract]
	[Serializable]
	public class HoldingPeriodInfo : TradeInfoBase
	{
		public HoldingPeriodInfo(string tradeId)
			: base(tradeId, "HoldingPeriod")
		{
		}

		public HoldingPeriodInfo()
			: base(Guid.NewGuid().ToString(), "HoldingPeriod")
		{
		}

		[DataMember]
		public BondInfoBase UnderlyingBondInfo { get; set; }

		[DataMember]
		public string StartDate { get; set; }

		[DataMember]
		public string EndDate { get; set; }

		[DataMember]
		public double Notional { get; set; }

		[DataMember]
		public string Direction { get; set; }

		[DataMember]
		public double InterestTaxRate { get; set; }

		[DataMember]
		public double BusinessTaxRate { get; set; }

		[DataMember]
		public double HoldingCost { get; set; }

		[DataMember]
		public double StartFrontCommission { get; set; }

		[DataMember]
		public double StartBackCommission { get; set; }

		[DataMember]
		public double EndFrontCommission { get; set; }

		[DataMember]
		public double EndBackCommission { get; set; }

		[DataMember]
		public string PaymentBusinessDayCounter { get; set; }

		[DataMember]
		public double StartFixingRate { get; set; }

		[DataMember]
		public double EndFixingRate { get; set; }
		public override TradeInfoBase[] GetDependenciesTrades()
		{
			return new []{ UnderlyingBondInfo};
		}
	}
}
