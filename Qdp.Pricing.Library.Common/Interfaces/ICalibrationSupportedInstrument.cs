using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Base.Interfaces;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface ICalibrationSupportedInstrument : IInstrument
	{
		string Tenor { get; }
		Date GetCalibrationDate();
		ICalibrationSupportedInstrument Bump(int bp);
		ICalibrationSupportedInstrument Bump(double resetRate);
		double ModelValue(IMarketCondition market, MktInstrumentCalibMethod calibMethod = MktInstrumentCalibMethod.Default);
	}
}
