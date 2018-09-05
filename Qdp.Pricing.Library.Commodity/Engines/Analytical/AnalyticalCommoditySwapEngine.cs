using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Commodity.Engines.Analytical
{
    public class AnalyticalCommoditySwapEngine : Engine<CommodityProduct.CommoditySwap>
    {
        public override IPricingResult Calculate(CommodityProduct.CommoditySwap trade, IMarketCondition market, PricingRequest request)
        {
            var result = new PricingResult(market.ValuationDate, request);
            var valuationDate = market.ValuationDate;
            var maturityDate = trade.MaturityDate;

            var recTicker = trade.RecTicker;
            var payTicker = trade.PayTicker;
            var fxTicker = trade.FxTicker;

            var recLegSpot = market.SpotPrices.Value[recTicker];
            var paylegSpot = market.SpotPrices.Value[payTicker];

            //to do: retrieve fx from market
            var fx = market.FxSpot.Value[fxTicker];
            var recFx = (trade.RecCcy != CurrencyCode.CNY) ? fx : 1.0;
            var payFx = (trade.PayCcy != CurrencyCode.CNY) ? fx : 1.0;


            if (valuationDate > maturityDate)
            {
                result.Pv = 0.0;
                result.asset1Delta = result.asset2Delta = result.asset3Delta = 0.0;
            }
            else
            {
                //To do: fx leg
                result.Pv = trade.RecNotional * recLegSpot * recFx - trade.PayNotional * paylegSpot * payFx + trade.FxNotional * fx;
                result.asset1Delta = trade.RecNotional * recFx;
                result.asset2Delta = -1.0 * trade.PayNotional * payFx;
            }

            return result;
        }
    }
}
