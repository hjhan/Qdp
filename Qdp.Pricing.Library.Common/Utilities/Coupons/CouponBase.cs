using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities.Coupons
{
	public abstract class CouponBase : ICoupon
	{
		public double[] GetCoupon(Schedule accruals,
			IYieldCurve fixingCurve,
			Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates,
			out List<CfCalculationDetail[]> cfCalcDetails,
			double initialPeriodCoupon = double.NaN,
			double[] compensationCoupon = null)
		{
			var accDates = accruals.ToArray();

			var coupons = new List<double>();


			var cfCalcDetailList = new List<CfCalculationDetail[]>();
			for (var i = 0; i < accDates.Length - 1; ++i)
			{
				CfCalculationDetail[] temp;
				coupons.Add(GetCoupon(accDates[i], accDates[i + 1], fixingCurve, historicalRates, out temp, compensationCoupon == null ? 0.0 : compensationCoupon[i]));
				if (i == 0 && !double.IsNaN(initialPeriodCoupon))
				{
					coupons[0] = initialPeriodCoupon;
					temp[0].FixingRate = coupons[0];
				}
				cfCalcDetailList.Add(temp);
			}
			cfCalcDetails = cfCalcDetailList;

			return coupons.ToArray();
		}

		public abstract double GetCoupon(Date accStartDate, Date accEndDate, IYieldCurve fixingCurve, Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates, out CfCalculationDetail[] cfCalcDetail, double stepWiseCompensationCoupon = 0.0);
		public abstract Tuple<Date, double> GetPrimeCoupon(Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates, IYieldCurve fixingCurve, Date fixingDate);
	}
}
