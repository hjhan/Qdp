using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome
{
	[DataContract]
	[Serializable]
	public class FxOptionValuationParameters : ValuationParameters
	{
		public FxOptionValuationParameters(string domCcyDiscountCurveName,
			string fgnCcyDiscountCurveName,
			string fxVolSurfName)
		{
			DomCcyDiscountCurveName = domCcyDiscountCurveName;
			FgnCcyDiscountCurveName = fgnCcyDiscountCurveName;
			FxVolSurfName = fxVolSurfName;
		}

		public FxOptionValuationParameters()
		{
		}

		[DataMember]
		public string DomCcyDiscountCurveName { get; set; }

		[DataMember]
		public string FgnCcyDiscountCurveName { get; set; }

		[DataMember]
		public string FxVolSurfName { get; set; }

		public override string ToLabelData()
		{
			throw new NotImplementedException();
		}
	}
}