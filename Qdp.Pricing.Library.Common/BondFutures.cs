using System;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities.Coupons;

namespace Qdp.Pricing.Library.Common
{
	public class BondFutures : IFuture<Bond>
	{ 
        public string Id { get; private set; }
        public string TypeName { get { return "BondFutures"; } }
        public double NominalCoupon { get; private set; }
		public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public DayGap SettlmentGap { get; private set; }
		public ICalendar Calendar { get; private set; }
		public IDayCount DayCount { get; private set; }
		public double Notional { get; set; }
		public Date FinalTradeDate { get; private set; }
		public Bond[] Deliverables { get; private set; }
		public CurrencyCode Currency { get; private set; }

		public BondFutures(
			string id,
			Date startDate,
			Date maturityDate,
			Date finalTradeDate,
			ICalendar calendar,
			Bond[] deliverables,
			IDayCount dayCount,
			CurrencyCode currency = CurrencyCode.CNY,
			double notional = 100.0,
			double nominalCoupon = 0.03)
		{
			Id = id;
			StartDate = startDate;
			UnderlyingMaturityDate = maturityDate;
			Calendar = calendar;
			FinalTradeDate = finalTradeDate;
			NominalCoupon = nominalCoupon;
			Deliverables = deliverables;
			DayCount = dayCount;
			SettlmentGap = new DayGap("0BD");
			Currency = currency;
			Notional = notional;
		}

		public double GetConversionFactor(Bond bond, IMarketCondition market)
		{
			var cfs = bond.GetCashflows(market);
			var n = cfs.Count(cf => cf.PaymentDate > UnderlyingMaturityDate);
			var f = bond.PaymentFreq.CountPerYear();
			var c = ((FixedCoupon)bond.Coupon).FixedRate;

			var nextPayDate = cfs.Where(cf => cf.PaymentDate >= new Date(UnderlyingMaturityDate.Year, UnderlyingMaturityDate.Month, 1)).ToArray()[0].PaymentDate;
			double x;
			if (UnderlyingMaturityDate.Month == nextPayDate.Month)
			{
				x = 12/f;
			}
			else
			{
				x = Math.Abs((UnderlyingMaturityDate.Year - nextPayDate.Year)*12 + (UnderlyingMaturityDate.Month - nextPayDate.Month));

			}

			var r = NominalCoupon;
			return
				Math.Round(
					1.0 / Math.Pow(1 + r / f, x * f / 12.0) *
					(c / f + c / r * (1.0 - 1.0 / Math.Pow(1 + r / f, n - 1)) + 1 / Math.Pow(1 + r / f, n - 1)) - c/f*(12 - f*x)/12, 4);
		}
	}
}
