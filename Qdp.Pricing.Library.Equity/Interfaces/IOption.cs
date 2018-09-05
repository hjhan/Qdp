using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Interfaces;

namespace Qdp.Pricing.Library.Equity.Interfaces
{
	public interface IOption : IInstrument
	{
		OptionType OptionType { get; }
		ICalendar Calendar { get; }
		IDayCount DayCount { get; }
		CurrencyCode PayoffCcy { get;}
		CurrencyCode SettlementCcy { get; }
		OptionExercise Exercise { get; }
		Date[] ExerciseDates { get; }
		Date[] ObservationDates { get; } // the final observation date is the payoff calculation date
		Date OptionPremiumPaymentDate { get; }
		double OptionPremium { get; }
		InstrumentType UnderlyingProductType { get; }
        double Strike { get;  }
        double[] Strikes { get; }

        bool IsMoneynessOption { get; }
        double InitialSpotPrice{ get;  }
        Dictionary<Date, double> Dividends { get; }

        Cashflow[] GetPayoff(double[] price);
		Cashflow[] GetPayoff(Dictionary<Date, double> pricePath);

        IOption Clone(OptionExercise exercise);
        bool HasNightMarket { get; }

        bool CommodityFuturesPreciseTimeMode { get; }
    }
}
