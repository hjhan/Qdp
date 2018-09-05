using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities.Coupons
{
	public class FixedDateAdjustedCoupon : CouponBase
	{
		private IndexType _indexType;
		private Tuple<int, int>[] _mmdds;
		private string _mmddstr;
		private ICalendar _calendar;
		private IDayCount _dayCount;
		private double _multiplier;
		private double _spread;
		private FixedDateAdjustedCouponStyle _fixedDateAdjustedCouponStyle;
		public FixedDateAdjustedCoupon(IndexType indexType, ICalendar calendar, IDayCount dayCount, FixedDateAdjustedCouponStyle fixedDateAdjustedCouponStyle, string mmdds, double multiplier, double spread)
		{
			//mmdds is a string looks like "02-15|05-15|09-22"
			_fixedDateAdjustedCouponStyle = fixedDateAdjustedCouponStyle;
			if (_fixedDateAdjustedCouponStyle == FixedDateAdjustedCouponStyle.SpecifiedDates)
			{
				_mmdds = mmdds.Split('|')
					.Select(x =>
					{
						var arr = x.Split('-');
						return Tuple.Create(Int32.Parse(arr[0]), Int32.Parse(arr[1]));
					}).ToArray();
			}
			else
			{
				_mmddstr = mmdds;
			}
			_indexType = indexType;
			_dayCount = dayCount;
			_calendar = calendar;
			
			_multiplier = multiplier;
			_spread = spread;
		}

		public override double GetCoupon(Date accStartDate,
				Date accEndDate,
				IYieldCurve fixingCurve,
				Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates,
				out CfCalculationDetail[] cfCalcDetail,
				double stepWiseCompensationCoupon = 0)
		{
			var sortedKeyDates = historicalRates[_indexType].Select(x => x.Key).OrderBy(x => x).ToArray();
			var couponAdjustmentDates = GetAdjustmentDates(accStartDate, accEndDate, sortedKeyDates);

			if (couponAdjustmentDates.Length < 1)
			{
				throw new PricingLibraryException("Fixed date adjusted coupon must have at least 1 fixing date!");
			}

			double coupon;
			if (couponAdjustmentDates.Length > 1)
			{
				var tmpcfCalcDetail = new List<CfCalculationDetail>();
				var begDate = accStartDate;
				var fixingDate = couponAdjustmentDates[0];
				var accumulatedCouon = 0.0;
				foreach (var date in couponAdjustmentDates.Skip(1).Union(new []{accEndDate}))
				{
					var dt = _dayCount.CalcDayCountFraction(begDate, date);
					var tmpCoupon = GetFixingRate(historicalRates[_indexType], fixingDate, sortedKeyDates);
					tmpcfCalcDetail.Add(new CfCalculationDetail(begDate, date, fixingDate, tmpCoupon, dt, true));
					accumulatedCouon += dt*tmpCoupon;
					begDate = date;
					fixingDate = date;
				}
				cfCalcDetail = tmpcfCalcDetail.ToArray();
				coupon = accumulatedCouon / _dayCount.CalcDayCountFraction(accStartDate, accEndDate);
			}
			else
			{
				coupon = GetFixingRate(historicalRates[_indexType], couponAdjustmentDates[0], sortedKeyDates);
				cfCalcDetail = new[]
				{
					new CfCalculationDetail(accStartDate, accEndDate, couponAdjustmentDates[0], coupon, _dayCount.CalcDayCountFraction(accStartDate, accEndDate), true)
				};
			}

			return coupon*_multiplier+_spread;
		}

		public override Tuple<Date, double> GetPrimeCoupon(Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates, IYieldCurve fixingCurve, Date fixingDate)
		{
			throw new NotImplementedException();
		}

		private double GetFixingRate(SortedDictionary<Date, double> refRates, Date fixingDate, Date[] sortedKeyDates)
		{
			if (fixingDate > sortedKeyDates.Last())
			{
				return refRates[sortedKeyDates.Last()];
			}

			fixingDate = sortedKeyDates.Last(x => x <= fixingDate);

			return refRates[fixingDate];
		}

		private Date[] GetAdjustmentDates(Date startDate, Date endDate, Date[] sortedKeyDates)
		{
			if (_fixedDateAdjustedCouponStyle == FixedDateAdjustedCouponStyle.SpecifiedDates)
			{
				var possibleAdjustmentDates = _mmdds.Select(x => new Date(startDate.Year - 1, x.Item1, x.Item2))
					.Union(_mmdds.Select(x => new Date(startDate.Year, x.Item1, x.Item2)))
					.Union(_mmdds.Select(x => new Date(endDate.Year, x.Item1, x.Item2)))
					.OrderBy(x => x)
					.ToArray();
				return new[] {possibleAdjustmentDates.Last(x => x <= startDate)}
					.Union(possibleAdjustmentDates.Where(x => x > startDate && x < endDate))
					.ToArray();
			}
			else
			{
				return new[] {sortedKeyDates.Last(x => x < startDate)}
					.Union(sortedKeyDates.Where(x => x > startDate && x < endDate))
					.ToArray();

			}
		}
	}
}
