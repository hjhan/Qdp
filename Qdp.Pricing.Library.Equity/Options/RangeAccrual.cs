using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Equity.Utilities;
using Qdp.Pricing.Library.Equity.Interfaces;

namespace Qdp.Pricing.Library.Equity.Options
{
	public class RangeAccrual : OptionBase
	{
        public override string TypeName { get { return "RangeAccrual"; } }
        public Dictionary<Date, double> Fixings { get; private set; } 
		public RangeDefinition[] Ranges { get; private set; }
		public RangeAccrual(Date startDate, 
			Date maturityDate, 
			OptionExercise exercise, 
			OptionType optionType,
			double strike, 
			RangeDefinition[] ranges,
			InstrumentType underlyingInstrumentType, 
			ICalendar calendar, 
			IDayCount dayCount, 
			CurrencyCode payoffCcy, 
			CurrencyCode settlementCcy, 
			Date[] exerciseDates, 
			Date[] observationDates,
			Dictionary<Date, double> fixings,
			double notional = 1, 
			DayGap settlementGap = null, 
			Date optionPremiumPaymentDate = null, 
			double optionPremium = 0) 
			: base(startDate, maturityDate, exercise, optionType, new double[] { strike }, underlyingInstrumentType, calendar, dayCount, payoffCcy, settlementCcy, exerciseDates, observationDates, notional, settlementGap, optionPremiumPaymentDate, optionPremium)
		{
			Ranges = ranges;
			Fixings = fixings;
		}

		public override Cashflow[] GetPayoff(double[] price)
		{
			throw new NotImplementedException();
		}

		public override Cashflow[] GetPayoff(Dictionary<Date, double> pricePath)
		{
			var prices = new Dictionary<Date, double>(pricePath);
			//merges fixings and monte carlo path
			
			foreach (var key in Fixings.Keys)
			{
				prices[key] = Fixings[key];
			}

			var amount = Ranges.Select(
					x => 1.0*x.ObservationDates.Count(date => prices[date] > x.LowerRange && prices[date] < x.UpperRange)*x.BonusRate/x.ObservationDates.Length
					).Sum();

			var settlementDate = SettlmentGap.Get(Calendar, UnderlyingMaturityDate);
			return new[]
			{
				new Cashflow(StartDate, UnderlyingMaturityDate, settlementDate, amount*Notional, SettlementCcy, CashflowType.Net, false, double.NaN, null),
				new Cashflow(StartDate, UnderlyingMaturityDate,OptionPremiumPaymentDate ?? settlementDate, OptionPremium, SettlementCcy, CashflowType.Net, false, double.NaN, null)
			};
		}

        public override IOption Clone(OptionExercise exercise)
        {
            throw new NotImplementedException();
        }
    }
}
