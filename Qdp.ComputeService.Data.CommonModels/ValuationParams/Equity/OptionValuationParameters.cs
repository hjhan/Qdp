using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.ValuationParams.Equity
{
    [DataContract]
    [Serializable]
    public class OptionValuationParameters : ValuationParameters
	{
        public OptionValuationParameters(string discountCurveName,
            string[] dividendCurveNames,
            string[] volSurfNames,
            string[] corrSurfNames,
            string underlyingId)
        {
            DiscountCurveName = discountCurveName;
            DividendCurveNames = dividendCurveNames;
            VolSurfNames = volSurfNames;
            CorrSurfNames = corrSurfNames;
            UnderlyingId = underlyingId;
        }
        public OptionValuationParameters(string discountCurveName,
            string[] dividendCurveNames, 
            string[] volSurfNames, 
			string underlyingId)
		{
			DiscountCurveName = discountCurveName;
			DividendCurveNames = dividendCurveNames;
			VolSurfNames = volSurfNames;
            UnderlyingId = underlyingId;
		}

        public OptionValuationParameters(string discountCurveName,
            string dividendCurveName,
            string volSurfName,
            string underlyingId)
        {
            DiscountCurveName = discountCurveName;
            DividendCurveNames = new string[] { dividendCurveName };
            VolSurfNames = new string[] { volSurfName };
            UnderlyingId = underlyingId;
        }


        private OptionValuationParameters()
		{
		}

		[DataMember]
		public string DiscountCurveName { get; set; }

		[DataMember]
		public string[] DividendCurveNames { get; set; }

        [DataMember]
		public string[] VolSurfNames { get; set; }

        [DataMember]
        public string[] CorrSurfNames { get; set; }

        [DataMember]
		public string UnderlyingId { get; set; }

        public override string ToLabelData()
		{
			throw new NotImplementedException();
		}
    }
}