using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	[DataContract]
	[Serializable]
	public class LoanInfo : TradeInfoBase
	{
		public LoanInfo(string tradeId)
			: base(tradeId, "Loan")
		{
		}

		public LoanInfo()
			: base(Guid.NewGuid().ToString(), "Loan")
		{
		}

		[DataMember]
		public string StartDate { get; set; }

		[DataMember]
		public string MaturityDate { get; set; }

		[DataMember]
		public string FirstPaymentDate { get; set; }

		[DataMember]
		public double Notional { get; set; }

		[DataMember]
		public string Currency { get; set; }

		[DataMember]
		public double Coupon { get; set; }

		[DataMember]
		public string DayCount { get; set; }

		[DataMember]
		public string Frequency { get; set; }

		[DataMember]
		public bool IsFloatingRate { get; set; }

		[DataMember]
		public string ResetDate { get; set; }

		[DataMember]
		public string IndexType { get; set; }

		[DataMember]
		public double FloatingRateMultiplier { get; set; }

		[DataMember]
		public string Amortization { get; set; }

		[DataMember]
		public int NumOfPayment { get; set; }
		[DataMember]
		public string MortgageCalcMethod { get; set; }
		[DataMember]
		public string AbsPrepaymentModel { get; set; }
		[DataMember]
		public string AbsDefaultModel { get; set; }
		[DataMember]
		public double PsaMultiplier { get; set; }

		[DataMember, DefaultValue(0.0)]
		public double SdaMultiplier { get; set; }

		[DataMember, DefaultValue(0.0)]
		public double RecoveryRate { get; set; }

		[DataMember, DefaultValue(0.0)]
		public double AnnualCprRate { get; set; }
		[DataMember, DefaultValue(0.0)]
		public double AnnualCdrRate { get; set; }
		[DataMember, DefaultValue(0.0)]
		public double TaxRate { get; set; }
	}
}