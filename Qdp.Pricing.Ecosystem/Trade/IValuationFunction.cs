using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Ecosystem.Market;

namespace Qdp.Pricing.Ecosystem.Trade
{
	public interface IValuationFunction
	{
		IPricingResult ValueTrade(QdpMarket market, PricingRequest request); 
	}
}
