using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome
{
	[DataContract]
	[Serializable]
	public class BasisSwapValuationParameters : ValuationParameters
	{
		public BasisSwapValuationParameters(string leg1DiscountCurveName,
			string leg1FixingCurveName,
			string leg2DiscountCurveName,
			string leg2FixingCurveName)
		{
			Leg1DiscountCurveName = leg1DiscountCurveName;
			Leg1FixingCurveName = leg1FixingCurveName;
			Leg2DiscountCurveName = leg2DiscountCurveName;
			Leg2FixingCurveName = leg2FixingCurveName;
		}

		public BasisSwapValuationParameters()
		{
		}

		[DataMember]
		public string Leg1DiscountCurveName { get; set; }

		[DataMember]
		public string Leg1FixingCurveName { get; set; }

		[DataMember]
		public string Leg2DiscountCurveName { get; set; }

		[DataMember]
		public string Leg2FixingCurveName { get; set; }

		public override string ToLabelData()
		{
			throw new NotImplementedException();
		}
	}
}