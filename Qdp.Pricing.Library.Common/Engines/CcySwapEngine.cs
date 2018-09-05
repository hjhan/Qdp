using System;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Engines
{
	public class CcySwapEngine : Engine<InterestRateSwap>
	{
		public override IPricingResult Calculate(InterestRateSwap trade, IMarketCondition market, PricingRequest request)
		{
			throw new NotImplementedException();
		}
	}
}
