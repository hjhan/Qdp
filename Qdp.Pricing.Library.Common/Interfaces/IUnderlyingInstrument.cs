using Qdp.Pricing.Library.Base.Interfaces;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IUnderlyingInstrument : IInstrument
	{
		double GetSpotPrice(IMarketCondition market);
	}
}
