using System.Linq;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Engines
{
	public class LoanEngine : Engine<Loan>
	{
		public override IPricingResult Calculate(Loan trade, IMarketCondition market, PricingRequest request)
		{
			var result = new PricingResult(market.ValuationDate, request);

			if (result.IsRequested(PricingRequest.Cashflow))
			{
				result.Cashflows = trade.GetCashflows(market);
				result.CashflowDict = result.Cashflows.ToDictionary(x => x.ToCfKey(), x => x.PaymentAmount);
			}

			return result;
		}
	}
}
