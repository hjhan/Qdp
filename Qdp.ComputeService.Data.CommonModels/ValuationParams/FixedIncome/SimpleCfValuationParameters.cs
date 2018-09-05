using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome
{
	[DataContract]
	[Serializable]
	public class SimpleCfValuationParameters : ValuationParameters
	{
		public SimpleCfValuationParameters(string discountCurveName,
			string fixingCurveName = null,
			string riskfreeCurveName = null)
		{
			DiscountCurveName = discountCurveName;
			FixingCurveName = fixingCurveName;
			RiskfreeCurveName = riskfreeCurveName;
		}

		public SimpleCfValuationParameters()
		{
		}

		[DataMember]
		public string DiscountCurveName { get; set; }

		[DataMember, DefaultValue(null)]
		public string FixingCurveName { get; set; }

		[DataMember, DefaultValue(null)]
		public string RiskfreeCurveName { get; set; }

		public override string ToLabelData()
		{
			return string.Format("DiscountCurveName:{0};FixingCurveName:{1};RiskfreeCurveName:{2}", DiscountCurveName ?? "", FixingCurveName ?? "", RiskfreeCurveName ?? "");
		}
	}
}