using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IIndex
	{
		IndexType IndexType { get; }
		int AverageDays { get; }
		CouponCompound CouponCompound { get; }
		double GetFixingRate(IYieldCurve fixingCurve,
			ICalendar resetCalendar,
			IDayCount dayCount,
			Date accStartDate,
			Date accEndDate,
			ITerm resetTerm,
			ITerm fixingTerm,
			DayGap fixingToResetGap,
			Stub resetStub,
			BusinessDayConvention resetBda,
			double couponSpread,
			double capRate,
			double floorRate,
			out CfCalculationDetail[] resetDetails,
			IDictionary<Date, double> historicalRate = null,
			FloatingCouponCalcType frnCalc = FloatingCouponCalcType.SimpleFrn,
			double stepWiseCompensationCoupon = 0.0,
			double multiplier = 1.0
			);

		double GetResetRate(
			IYieldCurve fixingCurve,
			Date fixingDate,
			ICalendar resetCalendar,
			ITerm fixingTenor,
			int period,
			IDictionary<Date, double> historicalRates,
			Compound indexCompound,
			IDayCount indexDayCount
			);
	}
}
