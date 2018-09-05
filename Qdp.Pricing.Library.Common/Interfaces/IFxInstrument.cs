using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Interfaces;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IFxCashflowInstrument : ICashflowInstrument
	{
		CurrencyCode DomCcy { get; }
		CurrencyCode FgnCcy { get; }
		CurrencyCode SettlementCcy { get; }
		ICalendar DomCalendar { get; }
		ICalendar FgnCalendar { get; }
		double NotionalInFgnCcy { get; }
		Date SpotDate { get; }
	}
}
