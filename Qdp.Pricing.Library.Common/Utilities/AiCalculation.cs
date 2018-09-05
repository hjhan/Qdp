using System;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Library.Common.Utilities
{
	public static class AiCalculation
	{
		//cash flow must be sorted by date
		public static double GetAccruedInterest(Date calcDate, Cashflow[] cashflow, IDayCount dayCount, bool isEod = true)
		{
			
			int nextCashflow;
			var flg = GetAccruedInterestCommon(out nextCashflow, calcDate, cashflow);
			if (!flg)
				return 0.0;

			var startDate = nextCashflow == 0 ? cashflow[0].AccrualStartDate : cashflow[nextCashflow - 1].PaymentDate;
			var coupon = cashflow[nextCashflow].CouponRate;
			var refStartDate = cashflow[nextCashflow].RefStartDate;
			var refEndDate = cashflow[nextCashflow].RefEndDate;
			var notional = cashflow[nextCashflow].StartPrincipal;

			if (!isEod)
			{
				return dayCount.CalcDayCountFraction(startDate, calcDate, refStartDate, refEndDate) * coupon * notional;
			}
			else
			{
				return dayCount.CalcDayCountFraction(startDate, calcDate.AddDays(1), refStartDate, refEndDate) * coupon * notional;
			}

		}

		//cash flow must be sorted by date
		public static int GetAccruedInterestDays(Date calcDate, Cashflow[] cashflow, bool isEod = true)
		{
			int nextCashflow;
			var flg = GetAccruedInterestCommon(out nextCashflow, calcDate, cashflow);
			if (!flg)
				return 0;

			var startDate = nextCashflow == 0 ? cashflow[0].AccrualStartDate : cashflow[nextCashflow - 1].PaymentDate;
			return Convert.ToInt32(calcDate - startDate) + (isEod ? 1 : 0);
		}

		private static bool GetAccruedInterestCommon(out int nextCashflow, Date calcDate, Cashflow[] cashflow)
		{
			nextCashflow = 0;
			if (calcDate < cashflow[0].AccrualStartDate || calcDate > cashflow.Last().PaymentDate)
			{
				return false;
			}

			cashflow = cashflow.Where(cf => cf.CashflowType == CashflowType.Coupon || cf.CashflowType == CashflowType.FixedLegInterest || cf.CashflowType == CashflowType.FloatingLegInterest)
				.GroupBy(cf => new { cf.AccrualStartDate, cf.AccrualEndDate, cf.PaymentDate, cf.PaymentCurrency })
				.Select(item => new Cashflow(item.Key.AccrualStartDate, item.Key.AccrualEndDate, item.Key.PaymentDate, item.Sum(entry => entry.PaymentAmount), item.Key.PaymentCurrency, CashflowType.Coupon, false, double.NaN, null))
				.OrderBy(cf => cf.PaymentDate)
				.ToArray();

			if (!cashflow.Any())
			{
				return false;
			}
			nextCashflow = cashflow.FirstIndexOf(x => x.PaymentDate > calcDate);

			return true;
		}
	}
}
