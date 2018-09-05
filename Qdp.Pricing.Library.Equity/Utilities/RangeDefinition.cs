using Qdp.Foundation.Implementations;

namespace Qdp.Pricing.Library.Equity.Utilities
{
	public class RangeDefinition
	{
		public double LowerRange { get; private set; }
		public double UpperRange { get; private set; }
		public double BonusRate { get; private set; }
		public Date[] ObservationDates { get; private set; }
		public Date SettlementDate { get; private set; }

		public RangeDefinition(double lowerRange, double upperRange, double bonusRate, Date settlementDate, Date[] observationDates)
		{
			LowerRange = lowerRange;
			UpperRange = upperRange;
			BonusRate = bonusRate;
			SettlementDate = settlementDate;
			ObservationDates = observationDates;
		}
	}
}
