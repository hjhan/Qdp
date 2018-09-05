using System;
using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities.Coupons
{
	class CustomizedCoupon : CouponBase
	{
		public ICoupon[] Coupons { get; private set; }

		public override double GetCoupon(Date accStartDate, Date accEndDate, IYieldCurve fixingCurve, Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates, out CfCalculationDetail[] cfCalcDetail, double stepWiseCompensationCoupon = 0.0)
		{
			throw new NotImplementedException();
		}

		public override Tuple<Date, double> GetPrimeCoupon(Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates, IYieldCurve fixingCurve, Date fixingDate)
		{
			throw new NotImplementedException();
		}
	}
}
