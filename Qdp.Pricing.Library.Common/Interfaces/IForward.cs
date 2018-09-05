using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Interfaces;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IForward<out TUnderlying> : IInstrument
		where TUnderlying : IInstrument, IUnderlyingInstrument
	{
		double Strike { get; }
		TUnderlying Underlying { get; }
		CurrencyCode Currency { get; }
		Cashflow[] GetReplicatingCashflows(IMarketCondition market);
	}
}
