using Qdp.Pricing.Base.Enums;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IMarketInstrument
	{
		ICalibrationSupportedInstrument Instrument { get; }
		double TargetValue { get; }

		MktInstrumentCalibMethod CalibMethod { get; }
	}
}
