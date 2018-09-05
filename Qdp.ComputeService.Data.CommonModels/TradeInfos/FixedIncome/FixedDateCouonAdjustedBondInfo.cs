using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	[DataContract]
	[Serializable]
	public class FixedDateCouonAdjustedBondInfo : BondInfoBase
	{
		public FixedDateCouonAdjustedBondInfo(string tradeId)
			: base(tradeId, "FixedDateCouonAdjustedBond")
		{
		}

		public FixedDateCouonAdjustedBondInfo()
			: base(Guid.NewGuid().ToString(), "FixedDateCouonAdjustedBond")
		{
		}

		[DataMember]
		public string AdjustMmDd { get; set; }
		[DataMember]
		public string Index { get; set; }
		[DataMember]
		public double FloatingRateMultiplier { get; set; }
		[DataMember]
		public double Spread { get; set; }
		[DataMember]
		public string FixedDateCouponAdjustedStyle { get; set; }
	}
}
