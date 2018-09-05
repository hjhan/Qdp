using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Pricing.Library.Equity.Interfaces;

namespace Qdp.Pricing.Library.Equity.Fx
{
	public class FxOption : OptionBase
	{
        public override string TypeName { get { return "FxOption"; } }
        public ICalendar FgnCalendar { get; private set; }
		public CurrencyCode FgnCcy { get; private set; }
		public CurrencyCode DomCcy { get; private set; }

		public ICalendar DomCalendar
		{
			get { return Calendar; }
		}
		public double FgnNotional { get; private set; }
		public DayGap UnderlyingFxSpotSettlement { get; private set; }
		public FxOption(Date startDate, 
			Date maturityDate, 
			OptionExercise exercise, 
			OptionType optionType, 
			double strikeFxRate, 
			InstrumentType underlyingType,
            ICalendar domCalendar, 
			CurrencyCode domCcy,
			ICalendar fgnCalendar,
			CurrencyCode fgnCcy,
			IDayCount dayCount, 
			CurrencyCode payoffCcy, 
			CurrencyCode settlementCcy, 
			Date[] exerciseDates, 
			Date[] observationDates,
			DayGap underlyingFxSpotSettlement,
			double notionalInFgnCcy = 1, 
			DayGap settlementGap = null, 
			Date optionPremiumPaymentDate = null, 
			double optionPremium = 0,
            bool hasNightMarket = false)
			: base(
                  startDate: startDate, 
                  maturityDate: maturityDate, 
                  exercise: exercise, 
                  optionType: optionType,
                  strike: new double[] { strikeFxRate } ,
                  underlyingTickers: new string[]{ "fgnCcy:domCcy" },
                  underlyingInstrumentType: underlyingType,
                  calendar: domCalendar, 
                  dayCount: dayCount, 
                  payoffCcy: payoffCcy, 
                  settlementCcy: settlementCcy, 
                  exerciseDates: exerciseDates, 
                  observationDates: observationDates,
                  notional: notionalInFgnCcy,
                  settlementGap: settlementGap,
                  optionPremiumPaymentDate: optionPremiumPaymentDate, 
                  optionPremium: optionPremium,
                  hasNightMarket: hasNightMarket)
		{
			FgnCalendar = fgnCalendar;
			FgnCcy = fgnCcy;
			DomCcy = domCcy;
			UnderlyingFxSpotSettlement = underlyingFxSpotSettlement;
		}

		public override Cashflow[] GetPayoff(double[] price)
		{
			throw new System.NotImplementedException();
		}

		public override Cashflow[] GetPayoff(Dictionary<Date, double> pricePath)
		{
			throw new System.NotImplementedException();
		}

        public override IOption Clone(OptionExercise exercise)
        {
            throw new System.NotImplementedException();
        }
    }
}
