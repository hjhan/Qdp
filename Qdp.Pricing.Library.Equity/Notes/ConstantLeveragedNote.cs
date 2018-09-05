using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Interfaces;

namespace Qdp.Pricing.Library.Equity.Notes
{
	public class ConstantLeveragedNote : IInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "ConstantLeverageNote"; } }
        public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public DayGap SettlmentGap { get; private set; }
		public double Notional { get; set; }
		public double TargetLeverage { get; private set; }
		public double FundingRate { get; private set; }
		public Dictionary<Date, double> Fixings { get; private set; }

		public Dictionary<Date, double> FxRates { get; private set; }

		public ConstantLeveragedNote(Date startDate,
			Date maturityDate,
			double notional,
			double targetLeverage,
			double fundingRate,
			Dictionary<Date, double> fixings,
			Dictionary<Date, double> fxRate)
		{
			StartDate = startDate;
			UnderlyingMaturityDate = maturityDate;
			Notional = notional;
			TargetLeverage = targetLeverage;
			FundingRate = fundingRate;
			Fixings = fixings;
			FundingRate = fundingRate;
			FxRates = fxRate;
		}
	}
}
