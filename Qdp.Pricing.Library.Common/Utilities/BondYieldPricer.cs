using System;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Foundation.Utilities;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Maths;

namespace Qdp.Pricing.Library.Common.Utilities
{
	public class BondYieldPricer : IBondYieldPricer
	{
		public double GetMacDuration(Cashflow[] cashflows,
			IDayCount dayCount,
			Frequency frequency,
			Date startDate,
			Date valueDate,
			double yield,
			TradingMarket tradingMarket)
		{
			var remainingCfs = cashflows.Where(x => x.PaymentDate > valueDate).ToList();

			if (!remainingCfs.Any()) return 0.0;

			if (remainingCfs.Count == 1)
			{
				if (tradingMarket.Equals(TradingMarket.ChinaInterBank))
				{
					dayCount = new ModifiedAfb();
				}
				return dayCount.CalcDayCountFraction(valueDate, remainingCfs[0].PaymentDate, remainingCfs.First().RefStartDate,
					remainingCfs.First().RefEndDate);
			}

			var npv = 0.0;
			var timeWeightedNpv = 0.0;
			var freq = frequency.CountPerYear();
			foreach (var remainingCf in remainingCfs)
			{
				var t = dayCount.CalcDayCountFraction(valueDate, remainingCf.PaymentDate, remainingCf.RefStartDate, remainingCf.RefEndDate);
				var df = 1.0/Math.Pow(1 + yield / freq, freq * t);
				npv += remainingCf.PaymentAmount * df;
				timeWeightedNpv += remainingCf.PaymentAmount * df * t;
			}
			return timeWeightedNpv / npv;
		}

		public double YieldFromFullPrice(Cashflow[] cashflows, 
			IDayCount dayCount, 
			Frequency frequency, 
			Date startDate, 
			Date valueDate,
			double fullPrice, 
			TradingMarket tradingMarket,
			bool irregularPayment = false)
		{
			var remainingCfs = cashflows.Where(x => x.PaymentDate > valueDate).ToList();

			if (!remainingCfs.Any()) return 0.0;

			try
			{
				var left = - 0.999999;
				if (remainingCfs.Count == 1)
				{
					if (tradingMarket.Equals(TradingMarket.ChinaInterBank))
					{
						dayCount = new ModifiedAfb();
					}
					var timeToMaturity = dayCount.CalcDayCountFraction(valueDate, cashflows.Last().PaymentDate, remainingCfs.First().RefStartDate, remainingCfs.First().RefEndDate);
					left = -1.0 / timeToMaturity + double.Epsilon;
				}
				var fcn = new SolveYtm(cashflows, dayCount, frequency, startDate, valueDate, fullPrice, tradingMarket, irregularPayment, this);
				return BrentZero.Solve(fcn, left, 1000000, 1e-12);
			}
			catch (Exception ex)
			{
				throw new PricingBaseException("Bond yield does not converge " + ex.GetDetail());
			}
		}

		public double FullPriceFromYield(Cashflow[] cashflows, 
			IDayCount dayCount, 
			Frequency frequency, 
			Date startDate, 
			Date valueDate,
			double yield,
			TradingMarket tradeingMarket,
			bool irregularPayment = false)
		{
			var remainingCfs = cashflows.Where(x => x.PaymentDate > valueDate).ToList();
			if (!remainingCfs.Any()) return 0.0;

			if (remainingCfs.Count == 1)
			{
				if (tradeingMarket.Equals(TradingMarket.ChinaInterBank))
				{
					dayCount = new ModifiedAfb();
				}
				var cf = remainingCfs.Single();
				if (cf.PaymentDate <= new Term("1Y").Next(valueDate))
				{
					return cf.PaymentAmount / (1.0 + yield * dayCount.CalcDayCountFraction(valueDate, cf.PaymentDate, cf.RefStartDate, cf.RefEndDate));
				}
				else
				{
					return cf.PaymentAmount / Math.Pow(1.0 + yield, dayCount.CalcDayCountFraction(valueDate, cf.PaymentDate, cf.RefStartDate, cf.RefEndDate));
				}
			}

			var npv = 0.0;
			var freq = frequency.CountPerYear();
			foreach (var remainingCf in remainingCfs)
			{
				var dt = dayCount.CalcDayCountFraction(valueDate, remainingCf.PaymentDate, remainingCf.RefStartDate, remainingCf.RefEndDate);
				var df = 1.0 / Math.Pow(1 + yield / freq, freq * dt);
				npv += remainingCf.PaymentAmount * df;
			}
			return npv;
		}
	}
}
