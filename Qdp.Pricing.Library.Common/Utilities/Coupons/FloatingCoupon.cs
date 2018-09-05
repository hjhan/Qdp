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
	public class FloatingCoupon : CouponBase
	{
		public IIndex Index { get; private set; }
		public ICalendar Calendar { get; private set; }
		public IDayCount DayCount { get; private set; }
		public double CouponSpread { get; private set; }
		public ITerm ResetTerm { get; private set; }
		public Stub ResetStub { get; private set; }
		public BusinessDayConvention Bda { get; private set; }
		public DayGap InitialResetToFixingGap { get; private set; }
		public DayGap ResetToFixingGap { get; private set; }
		public double FloorRate { get; private set; }
		public double CapRate { get; private set; }
		public double Multiplier { get; private set; }
		public FloatingCouponCalcType FrnCalc { get; private set; }

		public FloatingCoupon(IIndex index,
			ICalendar calendar,
			IDayCount dayCount,
			double couponSpread,
			ITerm resetTerm,
			Stub resetStub,
			BusinessDayConvention bda,
			DayGap resetToFixingGap = null,
			DayGap initialResetToFixingGap = null,
			double floorRate = -100,
			double capRate = 100,
			FloatingCouponCalcType frnCalc = FloatingCouponCalcType.SimpleFrn,
			double multiplier = 1.0)
		{
			Index = index;
			Calendar = calendar;
			DayCount = dayCount;
			CouponSpread = couponSpread;
			ResetTerm = resetTerm;
			ResetStub = resetStub;
			Bda = bda;
			ResetToFixingGap = resetToFixingGap;
			InitialResetToFixingGap = initialResetToFixingGap;
			FloorRate = floorRate;
			CapRate = capRate;
			FrnCalc = frnCalc;
			Multiplier = multiplier;
		}

		public override double GetCoupon(Date accStartDate,
			Date accEndDate,
			IYieldCurve fixingCurve,
			Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates,
			out CfCalculationDetail[] cfCalcDetail,
			double stepWiseCompensationCoupon = 0.0)
		{

			return Index.GetFixingRate(fixingCurve,
				Calendar,
				DayCount,
				accStartDate,
				accEndDate,
				ResetTerm,
				Index.IndexType.ForwardTerm(),
				ResetToFixingGap,
				ResetStub,
				Bda,
				CouponSpread,
				CapRate,
				FloorRate,
				out cfCalcDetail,
				historicalRates[Index.IndexType],
				FrnCalc,
				stepWiseCompensationCoupon,
				Multiplier);
		}

		public override Tuple<Date, double> GetPrimeCoupon(Dictionary<IndexType, SortedDictionary<Date, double>> historicalRates, IYieldCurve fixingCurve, Date valueDate)
		{
			var fixingDate = ResetToFixingGap.Get(Calendar, valueDate);

			var fixingRate = Index.GetResetRate(fixingCurve,
				fixingDate,
				Calendar,
				Index.IndexType.ForwardTerm(),
				Index.AverageDays,
				historicalRates[Index.IndexType],
				Compound.Continuous,
				Index.IndexType.DayCount()
				);
			var fixingTuple = historicalRates[Index.IndexType].TryGetValue(fixingDate, Calendar);

			return Tuple.Create(fixingTuple.Item1, fixingRate);
		}
	}
}
