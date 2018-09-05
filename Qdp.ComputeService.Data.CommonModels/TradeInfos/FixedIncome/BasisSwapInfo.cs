using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	public class BasisSwapInfo : TradeInfoBase
	{
		public BasisSwapInfo(string tradeId)
			: base(tradeId, "BasisSwap")
		{
		}

		private BasisSwapInfo()
			: base(Guid.NewGuid().ToString(), "BasisSwap")
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
		public string Leg1Calendar { get; set; }

		[DataMember]
		public string Leg1DayCount { get; set; }

		[DataMember]
		public string Leg1Frequency { get; set; }

		[DataMember]
		public string Leg1BusinessDayConvention { get; set; }

		[DataMember]
		public string Leg1Stub { get; set; }

		[DataMember]
		public string Leg1Index { get; set; }

		[DataMember, DefaultValue(null)]
		public string Leg1ResetTerm { get; set; }

		[DataMember]
		public string Leg1ResetStub { get; set; }

		[DataMember]
		public string Leg1ResetBusinessDayConvention { get; set; }

		[DataMember]
		public string Leg1ResetToFixingGap { get; set; }

		[DataMember]
		public string Leg1ResetCompound { get; set; }

		[DataMember]
		public string Leg2Calendar { get; set; }

		[DataMember]
		public string Leg2DayCount { get; set; }

		[DataMember]
		public string Leg2Frequency { get; set; }

		[DataMember]
		public string Leg2BusinessDayConvention { get; set; }

		[DataMember]
		public string Leg2Stub { get; set; }

		[DataMember]
		public string Leg2Index { get; set; }

		[DataMember, DefaultValue(null)]
		public string Leg2ResetTerm { get; set; }

		[DataMember]
		public string Leg2ResetStub { get; set; }

		[DataMember]
		public string Leg2ResetBusinessDayConvention { get; set; }

		[DataMember]
		public string Leg2ResetToFixingGap { get; set; }

		[DataMember]
		public string Leg2ResetCompound { get; set; }

		[DataMember]
		public BasisSwapValuationParameters ValuationParamters { get; set; }
	}
}