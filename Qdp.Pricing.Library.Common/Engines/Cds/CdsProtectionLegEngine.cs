using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Cds;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Engines.Cds
{
	public class CdsProtectionLegEngine : Engine<CdsProtectionLeg>
	{
		public int NumIntegrationInterval { get; private set; }

		public CdsProtectionLegEngine(int numIntegrationIntervals)
		{
			NumIntegrationInterval = numIntegrationIntervals;
		}

		public override IPricingResult Calculate(CdsProtectionLeg protectionLeg, IMarketCondition market,
			PricingRequest request)
		{
			var result = new PricingResult(market.ValuationDate, request);

			var start = protectionLeg.StartDate;
			var maturity = protectionLeg.UnderlyingMaturityDate;
			var valuationDate = market.ValuationDate;
			if (market.ValuationDate >= maturity)
			{
				result.Pv = 0.0;
			}
			else
			{
				var tmpStart = valuationDate <= start ? start : valuationDate;
				var step = (maturity - tmpStart)/NumIntegrationInterval;
				var term = new Term(step, Period.Day);
				var pv = 0.0;
				var df = market.DiscountCurve.Value.GetDf(valuationDate, tmpStart);
				var prob = market.SurvivalProbabilityCurve.Value.GetSpotRate(tmpStart);

				for (var i = 0; i < NumIntegrationInterval; ++i)
				{
					var tmpDate = term.Next(tmpStart, i + 1);
					var dfTmp = market.DiscountCurve.Value.GetDf(valuationDate, tmpDate);
					var probTmp = market.SurvivalProbabilityCurve.Value.GetSpotRate(tmpDate);
					pv += (df + dfTmp)*(prob - probTmp);
					df = dfTmp;
					prob = probTmp;
				}
				pv *= 0.5*(1 - protectionLeg.RecoveryRate);
				result.Pv = protectionLeg.Notional*pv;
			}

			return result;
		}
	}
}
