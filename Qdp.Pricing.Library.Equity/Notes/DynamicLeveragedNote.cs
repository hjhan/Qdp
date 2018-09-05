using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Interfaces;

namespace Qdp.Pricing.Library.Equity.Notes
{
	public class DynamicLeveragedNote : IInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "DynamicLeverageNote"; } }
        public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public DayGap SettlmentGap { get; private set; }
		public double Notional { get; set; }
		public double TargetLeverage { get; private set; }
		public double LeverageLowerRange { get; private set; }
		public double LeverageUpperRange { get; private set; }
		public double FundingRate { get; private set; }
		public double RebalaceCostRate { get; private set; }
		public Dictionary<Date, double> Fixings { get; private set; }

		public DynamicLeveragedNote(Date startDate,
			Date maturityDate,
			double notional,
			double targetLeverage,
			double leverageLowerRange,
			double leverageUpperRange,
			double fundingRate,
			double rebalanceCostRate,
			Dictionary<Date, double> fixings)
		{
			StartDate = startDate;
			UnderlyingMaturityDate = maturityDate;
			Notional = notional;
			TargetLeverage = targetLeverage;
			LeverageLowerRange = leverageLowerRange;
			LeverageUpperRange = leverageUpperRange;
			RebalaceCostRate = rebalanceCostRate;
			Fixings = fixings;
			FundingRate = fundingRate;
		}
	}
}
