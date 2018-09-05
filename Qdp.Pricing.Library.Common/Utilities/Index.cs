using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities
{
	public class Index : IIndex
	{
		public IndexType IndexType { get; private set; }
		public int AverageDays { get; private set; }
		public CouponCompound CouponCompound { get; private set; }
		private readonly int? _resetRateDigits;

		public Index(IndexType indexType, int averageDays, CouponCompound couponCompound, int? resetRateDigits = null)
		{
			IndexType = indexType;
			AverageDays = averageDays;
			CouponCompound = couponCompound;
			_resetRateDigits = resetRateDigits;
		}

		public double GetFixingRate(IYieldCurve fixingCurve,
			ICalendar resetCalendar,
			IDayCount dayCount,
			Date accStartDate,
			Date accEndDate,
			ITerm resetTerm,
			ITerm fixingTenor,
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
			double multiplier = 1.0)
		{
			var details = new List<CfCalculationDetail>();
			var resetPriods = resetTerm == null
				? new List<Date> { accStartDate, accEndDate }
				: new Schedule(accStartDate, accEndDate, resetTerm, resetStub, resetCalendar, resetBda).ToList();


			for (var i = 0; i < resetPriods.Count - 1; ++i)
			{
				var resetStartDate = resetPriods[i];
				var resetEndDate = resetPriods[i + 1];
				if (resetEndDate > accEndDate)
				{
					resetEndDate = accEndDate;
				}
				var fixingStartDate = fixingToResetGap.Get(resetCalendar, resetStartDate);
				
				var forwardCompound = frnCalc == FloatingCouponCalcType.ZzFrn ? Compound.Continuous : IndexType.ForwardCompound();
				details.Add(new CfCalculationDetail(
					resetStartDate,
					resetEndDate,
					fixingStartDate,
					multiplier*GetResetRate(fixingCurve, fixingStartDate, resetCalendar, fixingTenor, AverageDays, historicalRate, forwardCompound, IndexType.DayCount()) + stepWiseCompensationCoupon,
					dayCount.CalcDayCountFraction(resetStartDate, resetEndDate, accStartDate, accEndDate),
					resetStartDate < (fixingCurve == null ? fixingStartDate : fixingCurve.ReferenceDate)));
			}

			resetDetails = details.ToArray();
			var totalDcf = resetDetails.Sum(x => x.FixingDcf);
			double couponRate;
			switch (CouponCompound)
			{
				case CouponCompound.Compounded:
					couponRate =
						(resetDetails.Select(x =>
						{
							var coupon = FilterRate(x.FixingRate + couponSpread, capRate, floorRate);
							return 1.0 + coupon * x.FixingDcf;
						})
							.Aggregate(1.0, (current, v) => current * v) - 1.0) / totalDcf;
					return couponRate;
				case CouponCompound.Simple:
					couponRate =
					resetDetails.Select(x =>
					{
						var coupon = FilterRate(x.FixingRate + couponSpread, capRate, floorRate);
						return coupon*x.FixingDcf;
					}).Sum() / totalDcf;
					return couponRate;
				default:
					throw new PricingLibraryException("Unknow type of coupon compund type" + CouponCompound);
			}
		}

		private double FilterRate(double coupon, double capRate, double floorRate)
		{
			if (coupon > capRate)
			{
				coupon = capRate;
			}
			else if (coupon < floorRate)
			{
				coupon = floorRate;
			}
			return coupon;
		}


		public double GetResetRate(
			IYieldCurve fixingCurve,
			Date fixingDate,
			ICalendar resetCalendar,
			ITerm fixingTenor,
			int period,
			IDictionary<Date, double> historicalRates,
			Compound indexCompound,
			IDayCount indexDayCount
			)
		{
			var refDate = fixingCurve != null ? fixingCurve.ReferenceDate : fixingDate;
			var t1D = new Term("1D");

			while (resetCalendar.IsHoliday(fixingDate))
			{
				fixingDate = t1D.Prev(fixingDate);
			}

			IDictionary<Date, double> indexRates = new SortedDictionary<Date, double>();
			if (fixingDate <= refDate)
			{
				indexRates = historicalRates;
			}
			else
			{
				for (var i = 0; i < period; i++)
				{
					var resetFixingDate = resetCalendar.AddBizDays(fixingDate, -i);
					double reseFixingRate = fixingCurve.GetForwardRate(resetFixingDate < refDate ? refDate : resetFixingDate, fixingTenor, indexCompound, indexDayCount);
					indexRates.Add(resetFixingDate, reseFixingRate);
				}
			}

			return indexRates.GetAverageIndex(fixingDate, resetCalendar, period, _resetRateDigits);
		}
	}
}
