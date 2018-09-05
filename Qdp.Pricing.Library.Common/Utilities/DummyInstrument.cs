using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities
{
	public class DummyInstrument : ICalibrationSupportedInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "DummyInstrument"; } }
        public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public DayGap SettlmentGap { get; private set; }
		public double Notional { get; set; }
		public string Tenor { get; private set; }
		public double Rate { get; private set; }

		public DummyInstrument(
			Date startDate,
			Date maturityDate,
			double rate)
		{
			StartDate = startDate;
			UnderlyingMaturityDate = maturityDate;
			Rate = rate;

			Notional = 1.0;
			Tenor = new Term(maturityDate-StartDate, Period.Day).ToString();
			SettlmentGap = new DayGap("+0BD");

		}
		public Date GetCalibrationDate()
		{
			return UnderlyingMaturityDate;
		}

		public ICalibrationSupportedInstrument Bump(int bp)
		{
			return new DummyInstrument(StartDate, UnderlyingMaturityDate, Rate+bp*0.0001);
		}

		public ICalibrationSupportedInstrument Bump(double resetRate)
		{
			return new DummyInstrument(StartDate, UnderlyingMaturityDate, resetRate);
		}

		public double ModelValue(IMarketCondition market, MktInstrumentCalibMethod calibMethod = MktInstrumentCalibMethod.Default)
		{
			return market.DiscountCurve.Value.GetSpotRate(UnderlyingMaturityDate);
		}
	}
}
