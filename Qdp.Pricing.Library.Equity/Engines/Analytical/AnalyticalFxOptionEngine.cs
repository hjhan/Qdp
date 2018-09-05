using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Equity.Fx;

namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{
	public class AnalyticalFxOptionEngine : Engine<FxOption>
	{
		public override IPricingResult Calculate(FxOption fxOption, IMarketCondition market, PricingRequest request)
		{
			var newMarket = market.UpdateCondition(
				new UpdateMktConditionPack<IYieldCurve>(x => x.DividendCurves, market.FgnDiscountCurve.Value),
				new UpdateMktConditionPack<double>(x => x.SpotPrices.Value.Values.First(), market.GetFxRate(GetSpotDate(market.ValuationDate, fxOption), fxOption.DomCcy, fxOption.FgnCcy))
				);
			return new AnalyticalVanillaEuropeanOptionEngine().Calculate(fxOption, newMarket, request);
		}

		private Date GetSpotDate(Date startDate, FxOption fxOption)
		{
			return MarketExtensions.GetFxSpotDate(startDate,
				fxOption.UnderlyingFxSpotSettlement,
				fxOption.FgnCcy,
				fxOption.DomCcy,
				fxOption.FgnCalendar,
				fxOption.DomCalendar);
		}
	}
}
