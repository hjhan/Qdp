using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Library.Base.Implementations
{
	public class Amortization : IAmortization
	{
		public Dictionary<Date, double> AmortizationSchedule { get; private set; }
		public AmortizationType AmortizationType { get; private set; }
		public bool RenormalizeAfterAmoritzation { get; private set; }

		public Amortization(Dictionary<Date, double> amortizationSchedule = null,
			bool renormalizeFlag = false, 
			AmortizationType amortizationType = AmortizationType.None)
		{
			AmortizationSchedule = amortizationSchedule;
			AmortizationType = amortizationType;
			RenormalizeAfterAmoritzation = renormalizeFlag;

			if (amortizationSchedule != null && amortizationSchedule.Count > 0)
			{
				var sum = AmortizationSchedule.Sum(x => x.Value);
				if (!sum.AlmostEqual(1.0))
				{
					throw new PricingLibraryException("Amortization must sum up to 1.0, but is not");
				}
			}
		}

		public IAmortization Adjust(Schedule paymentSchedule, 
			ICalendar calendar = null, 
			BusinessDayConvention bda = BusinessDayConvention.None,
			Frequency frequency = Frequency.None)
		{
			Dictionary<Date, double> amortizationSchedule;

			if (AmortizationSchedule == null || !AmortizationSchedule.Any())
			{
				amortizationSchedule = new[] {Tuple.Create(paymentSchedule.Last(), 1.0)}.ToDictionary(x => x.Item1, x => x.Item2);
			}
			else
			{
				var principalPay =
					AmortizationSchedule.Select(
						x => Tuple.Create(
							bda != BusinessDayConvention.None && calendar != null ? bda.Adjust(calendar, x.Key) : x.Key, 
							x.Value))
							.OrderBy(x => x.Item1).ToList();
				if (paymentSchedule.Last() <= principalPay.First().Item1)
				{
					return new Amortization(new[] {Tuple.Create(paymentSchedule.Last(), 1.0)}.ToDictionary(x => x.Item1, x => x.Item2));
				}

				var preMaturity = principalPay.Where(x => x.Item1 <= paymentSchedule.Last()).OrderBy(x => x.Item1).ToList();
				var remaingPrincipal = principalPay.Where(x => x.Item1 > paymentSchedule.Last()).ToList().Sum(x => x.Item2);
				amortizationSchedule = preMaturity.Select(
					(x, index) =>
						Tuple.Create(x.Item1, index < preMaturity.Count -1 ? x.Item2 : x.Item2+remaingPrincipal))
						.ToDictionary(x => x.Item1, x => x.Item2);
			}

			return new Amortization(amortizationSchedule, RenormalizeAfterAmoritzation, AmortizationType);
		}

		public double GetRemainingPrincipal(Date valueDate)
		{
			return AmortizationSchedule.Where(x => x.Key > valueDate).Sum(x => x.Value);
		}

		public IAmortization ResetAmortization(Date valueDate)
		{
			Dictionary<Date, double> amortizationSchedule = AmortizationSchedule.ToDictionary(x=>x.Key, y=>y.Value);
			if (RenormalizeAfterAmoritzation && AmortizationSchedule != null)
			{
				amortizationSchedule = AmortizationSchedule.Where(x => x.Key > valueDate).ToDictionary(x => x.Key, y => y.Value);
				var total = amortizationSchedule.Sum(y => y.Value);
				foreach (var scheduleKey in amortizationSchedule.ToDictionary(x => x.Key, y => y.Key).Keys)
				{
					amortizationSchedule[scheduleKey] = amortizationSchedule[scheduleKey] / total;
				}
			}
			return new Amortization(amortizationSchedule, RenormalizeAfterAmoritzation, AmortizationType);
		}
	}
}
