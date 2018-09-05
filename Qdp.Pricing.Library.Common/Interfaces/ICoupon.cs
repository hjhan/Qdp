using System;
using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Implementations;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface ICoupon
	{
		//double[] GetCoupon(Schedule accruals, IYieldCurve fixingCurve, Dictionary<IndexType, Dictionary<Date, double>> historicalRates , out List<CfCalculationDetail[]> cfCalcDetails, double initialCoupon = double.NaN, Dictionary<int, double> stepWiseCompensationRate = null );
		double[] GetCoupon(Schedule accruals, IYieldCurve fixingCurve, Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates, out List<CfCalculationDetail[]> cfCalcDetails, double initialCoupon = double.NaN, double[] stepWiseCompensationRate = null);
		double GetCoupon(Date accStartDate, Date accEndDate, IYieldCurve fixingCurve, Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates, out CfCalculationDetail[] cfCalcDetail, double compensationCoupon = 0.0);
		Tuple<Date, double> GetPrimeCoupon(Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates, IYieldCurve fixingCurve, Date fixingDate);
	}
}
