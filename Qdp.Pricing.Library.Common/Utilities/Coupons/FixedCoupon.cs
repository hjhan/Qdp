using System;
using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities.Coupons
{
	public class FixedCoupon : CouponBase
	{
		public double FixedRate { get; private set; }

		public FixedCoupon(double fixedRate)
		{
			FixedRate = fixedRate;
		}

		public override double GetCoupon(Date accStartDate, Date accEndDate, IYieldCurve fixingCurve, Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates, out CfCalculationDetail[] cfCalcDetail, double stepWiseCompensationCoupon = 0.0)
		{
			var rate = FixedRate + stepWiseCompensationCoupon;
			cfCalcDetail = new []
			{
				new CfCalculationDetail(accStartDate, accEndDate, accEndDate, rate, 0.0, true)
			};
			return rate;
		}

		public override Tuple<Date, double> GetPrimeCoupon(Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates, IYieldCurve fixingCurve, Date fixingDate)
		{
			throw new System.NotImplementedException();
		}
	}
}
