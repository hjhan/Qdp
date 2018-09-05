using System;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Cds;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities.Coupons;

namespace Qdp.Pricing.Library.Common.Engines.Cds
{
	public class CdsEngine : Engine<CreditDefaultSwap>
	{
		public int NumIntegrationInterval { get; private set; }
		private readonly CdsPremiumLegEngine _premiumLegEngine;
		private readonly CdsProtectionLegEngine _protectionLegEngine;

		public CdsEngine(int numIntegrationInterval)
		{
			NumIntegrationInterval = numIntegrationInterval;
			_premiumLegEngine = new CdsPremiumLegEngine();
			_protectionLegEngine = new CdsProtectionLegEngine(NumIntegrationInterval);
		}

		public override IPricingResult Calculate(CreditDefaultSwap creditDefaultSwap, IMarketCondition market, PricingRequest request)
		{
			var valuationDate = market.ValuationDate;

			var result = new PricingResult(valuationDate, request);

			if (result.IsRequested(PricingRequest.Pv))
			{
				result.Pv = _premiumLegEngine.Calculate(creditDefaultSwap.PremiumLeg, market, PricingRequest.Pv).Pv
							+ _protectionLegEngine.Calculate(creditDefaultSwap.ProtectionLeg, market, PricingRequest.Pv).Pv;
			}

			if (result.IsRequested(PricingRequest.Ai))
			{
				result.Ai = _premiumLegEngine.Calculate(creditDefaultSwap.PremiumLeg, market, PricingRequest.Ai).Ai;
			}

			return result;
		}

		public double ParSpread(CreditDefaultSwap creditDefaultSwap, IMarketCondition market)
		{
			if (market.ValuationDate >= creditDefaultSwap.UnderlyingMaturityDate)
			{
				throw new PricingBaseException("Instrument has matured!");
			}
			var coupon = ((FixedCoupon) creditDefaultSwap.PremiumLeg.Coupon).FixedRate;
			var rpv01 = _premiumLegEngine.Calculate(creditDefaultSwap.PremiumLeg, market, PricingRequest.Pv).Pv / coupon;
			var protectionLegPv =
					_protectionLegEngine.Calculate(creditDefaultSwap.ProtectionLeg, market, PricingRequest.Pv).Pv;
			return Math.Abs(protectionLegPv / rpv01);
		}
	}
}
