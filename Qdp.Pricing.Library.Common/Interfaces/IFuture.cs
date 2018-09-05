using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Interfaces;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IFuture<out TDeliverable> : IInstrument
		where TDeliverable : IUnderlyingInstrument, IInstrument
	{
		Date FinalTradeDate { get; }
		IDayCount DayCount { get; }
		TDeliverable[] Deliverables { get; }
		CurrencyCode Currency { get; }
	}
}
