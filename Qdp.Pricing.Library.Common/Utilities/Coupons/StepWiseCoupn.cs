using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities.Coupons
{
	public class StepWiseCoupon : CouponBase
	{
		private readonly Dictionary<Date, double> _stepWiseCoupon;
		private readonly ICalendar _calendar;
		public StepWiseCoupon(Dictionary<Date, double> couponSteps,
			ICalendar calendar)
		{
			_stepWiseCoupon = couponSteps;
			_calendar = calendar;
		}

		public override double GetCoupon(Date accStartDate, Date accEndDate, IYieldCurve fixingCurve, Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates, out CfCalculationDetail[] cfCalcDetail, double stepWiseCompensationCoupon = 0.0)
		{
			var couponAdjustDates = _stepWiseCoupon.Keys.OrderBy(x => x).ToArray();
			var n = couponAdjustDates.Length;
			int i;
			List<CfCalculationDetail> detail = new List<CfCalculationDetail>();
			for (i = n-1; i >= 0; --i)
			{
				if (accEndDate > BusinessDayConvention.Following.Adjust(_calendar, couponAdjustDates[i]))
				{
					break;
				}
			}

			detail.Add(new CfCalculationDetail(accStartDate, accEndDate, couponAdjustDates[i], _stepWiseCoupon[couponAdjustDates[i]], 0.0, true));
			cfCalcDetail = detail.OrderBy(x=>x.FixingDate).ToArray();
			return _stepWiseCoupon[couponAdjustDates[i]];
		}

		public override Tuple<Date, double> GetPrimeCoupon(Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates, IYieldCurve fixingCurve, Date fixingDate)
		{
			throw new System.NotImplementedException();
		}
	}
}
