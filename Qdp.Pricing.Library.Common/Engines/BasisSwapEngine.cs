using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities;

namespace Qdp.Pricing.Library.Common.Engines
{
	public class BasisSwapEngine : Engine<InterestRateSwap>
	{
		public override IPricingResult Calculate(InterestRateSwap trade, IMarketCondition market, PricingRequest request)
		{
			var cfEngine = new CashflowProductEngine<SwapLeg>();
			var leg1Result = cfEngine.Calculate(trade.FixedLeg, market, PricingRequest.All);

			var mkt4Leg2 = market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, market.FgnDiscountCurve.Value),
					new UpdateMktConditionPack<IYieldCurve>(x => x.FixingCurve, market.FgnFixingCurve.Value));
			var leg2Result = cfEngine.Calculate(trade.FloatingLeg, mkt4Leg2, PricingRequest.All);

			var result = new PricingResult(market.ValuationDate, request);

			if (result.IsRequested(PricingRequest.Pv))
			{
				result.Pv = leg1Result.Pv + leg2Result.Pv;
			}

			if (result.IsRequested(PricingRequest.Dv01))
			{
				result.Dv01 = leg1Result.Dv01 + leg2Result.Dv01;
			}

			if (result.IsRequested(PricingRequest.Pv01))
			{
				var bumpedIrs = trade.Bump(1);
				var bumpedPv = new CashflowProductEngine<InterestRateSwap>().Calculate(bumpedIrs, market, PricingRequest.Pv).Pv;
				result.Pv01 = bumpedPv - result.Pv;
			}

			if (result.IsRequested(PricingRequest.Ai))
			{
				result.Ai = leg1Result.Ai + leg2Result.Ai;
			}

			if (result.IsRequested(PricingRequest.Cashflow))
			{
				result.Cashflows = leg1Result.Cashflows.Union(leg2Result.Cashflows).ToArray();
			}

			if (result.IsRequested(PricingRequest.KeyRateDv01))
			{
				result.KeyRateDv01 = PricingResultExtension.Aggregate(leg1Result.KeyRateDv01, leg2Result.KeyRateDv01);
			}

			return result;
		}
	}
}
