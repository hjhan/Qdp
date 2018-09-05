using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Market
{
	public class MarketInstrument : IMarketInstrument
	{
		public ICalibrationSupportedInstrument Instrument { get; private set; }
		public double TargetValue { get; private set; }
		public MktInstrumentCalibMethod CalibMethod { get; private set; }

		public MarketInstrument(ICalibrationSupportedInstrument instrument,
			double targetValue,
			MktInstrumentCalibMethod calibMethod)
		{
			Instrument = instrument;
			TargetValue = targetValue;
			CalibMethod = calibMethod;
		}
	}
}
