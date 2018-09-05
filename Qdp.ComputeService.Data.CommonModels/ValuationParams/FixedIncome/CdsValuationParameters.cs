using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome
{
	[DataContract]
	[Serializable]
	public class CdsValuationParameters : ValuationParameters
	{
		public CdsValuationParameters(string discountCurveName,
			string spcCurveName
			)
		{
			DiscountCurveName = discountCurveName;
			SpcCurveName = spcCurveName;
		}

		private CdsValuationParameters()
		{
		}

		[DataMember]
		public string DiscountCurveName { get; set; }

		[DataMember]
		public string SpcCurveName { get; set; }

		public override string ToLabelData()
		{
			throw new NotImplementedException();
		}
	}
}