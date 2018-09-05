using System;
using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Equity.Interfaces;

namespace Qdp.Pricing.Library.Equity.Options
{
	public class DoubleSharkFinOption : OptionBase
	{
        public override string TypeName { get { return "DoubleSharkFinOption"; } }
        public DoubleSharkFinOption(Date startDate, 
			Date maturityDate, 
			OptionExercise exercise, 
			OptionType optionType, 
			double strike,
			InstrumentType underlyingInstrumentType, 
			ICalendar calendar, 
			IDayCount dayCount, 
			CurrencyCode payoffCcy, 
			CurrencyCode settlementCcy, 
			Date[] exerciseDates, 
			Date[] observationDates, 
			double notional = 1, 
			DayGap settlementGap = null, 
			Date optionPremiumPaymentDate = null, 
			double optionPremium = 0) 
			: base(startDate, maturityDate, exercise, optionType,  new double[] { strike } , underlyingInstrumentType, calendar, dayCount, payoffCcy, settlementCcy, exerciseDates, observationDates, notional, settlementGap, optionPremiumPaymentDate, optionPremium)
		{
		}

		public override Cashflow[] GetPayoff(double[] price)
		{
			throw new NotImplementedException();
		}

		public override Cashflow[] GetPayoff(Dictionary<Date, double> pricePath)
		{
			throw new NotImplementedException();
		}

        public override IOption Clone(OptionExercise exercise)
        {
            throw new NotImplementedException();
        }
    }
}
