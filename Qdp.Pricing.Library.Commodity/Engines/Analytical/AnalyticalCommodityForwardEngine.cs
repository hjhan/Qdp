using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using System.Linq;


namespace Qdp.Pricing.Library.Commodity.Engines.Analytical
{
    public class AnalyticalCommodityForwardEngine: Engine<CommodityProduct.CommodityForwardCNY>
    {
        public override IPricingResult Calculate(CommodityProduct.CommodityForwardCNY trade, IMarketCondition market, PricingRequest request)
        {
            var result = new PricingResult(market.ValuationDate, request);
            var valuationDate = market.ValuationDate;
            var maturityDate = trade.MaturityDate;

            if(valuationDate> maturityDate)
            {
                result.Pv = 0.0;
                result.Delta = 0.0;
            }
            else
            {
                result.Pv = (market.SpotPrices.Value.Values.First()  + trade.Basis) * trade.Notional;
                result.Delta = trade.Notional;
            }
              
            return result;
        }
    }
}
