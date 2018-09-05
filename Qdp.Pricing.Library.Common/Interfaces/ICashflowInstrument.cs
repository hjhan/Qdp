using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Interfaces;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface ICashflowInstrument : IInstrument
	{
		Cashflow[] GetCashflows(IMarketCondition market, bool netted = true);
		double GetAccruedInterest(Date calcDate, IMarketCondition market, bool isEod = true);
	}
}
