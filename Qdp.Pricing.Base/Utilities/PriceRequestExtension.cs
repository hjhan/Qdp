using System.Linq;
using Qdp.Pricing.Base.Implementations;

namespace Qdp.Pricing.Base.Utilities
{
	public static class PriceRequestExtension
	{
		public static PricingRequest ToSinglePricingRequest(this PricingRequest[] pricingRequests)
		{
			return pricingRequests.Aggregate(PricingRequest.None, (current, pricingRequest) => current | pricingRequest);
		}
	}
}
