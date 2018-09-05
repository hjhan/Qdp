using System;
using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Equity.Interfaces;

namespace Qdp.Pricing.Library.Equity.Options
{
    public class ResetStrikeOption : OptionBase
    {
        public override string TypeName { get { return "ResetStrikeOption"; } }
        public ResetStrikeType ResetStrikeType { get; private set; }
        public Date StrikeFixingDate { get; private set; }

        public ResetStrikeOption(Date startDate,
            Date maturityDate,
            OptionExercise exercise,
            OptionType optionType,
            ResetStrikeType resetStrikeType,
            double strike,
            InstrumentType underlyingInstrumentType,
            ICalendar calendar,
            IDayCount dayCount,
            CurrencyCode payoffCcy,
            CurrencyCode settlementCcy,
            Date[] exerciseDates,
            Date[] observationDates,
            Date strikefixingDate,
            double notional = 1,
            DayGap settlementGap = null,
            Date optionPremiumPaymentDate = null,
            double optionPremium = 0)
            : base(startDate: startDate, maturityDate: maturityDate, exercise: exercise, optionType: optionType, strike: new double[] { strike },
                  underlyingInstrumentType: underlyingInstrumentType, calendar: calendar, dayCount: dayCount,
                  settlementCcy: settlementCcy, payoffCcy: payoffCcy, exerciseDates: exerciseDates, observationDates:observationDates,
                  notional: notional, settlementGap: settlementGap, optionPremiumPaymentDate: optionPremiumPaymentDate, optionPremium: optionPremium)
        {
            ResetStrikeType = resetStrikeType;
            StrikeFixingDate = strikefixingDate;
            
        }

        public override Cashflow[] GetPayoff(double[] price)
        {
            throw new NotImplementedException();
        }

        //this one is used in Monte Carlo, and Monte carlo is used to Pricing European option
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
