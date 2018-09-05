using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Equity.Interfaces;

namespace Qdp.Pricing.Library.Exotic.Engines
{
	public class SimpleConvertibleBondEngine<TOptionEngine> : Engine<ConvertibleBond>
		where TOptionEngine : Engine<IOption>
	{
		private readonly TOptionEngine _optionEngine;

		public SimpleConvertibleBondEngine(TOptionEngine optionEngine)
		{
			_optionEngine = optionEngine;
		} 

		public override IPricingResult Calculate(ConvertibleBond convertibleBond, IMarketCondition market, PricingRequest request)
		{
			var result = new PricingResult(market.ValuationDate, request);

		    if (convertibleBond.ConversionOption == null)
		    {
		        result.Pv = market.MktQuote.Value[convertibleBond.Bond.Id].Item2;
		    }
		    else
		    {
		        var optionParts = _optionEngine.Calculate(convertibleBond.ConversionOption,
		            market.UpdateCondition(
		                new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, market.RiskfreeCurve.Value)),
		            PricingRequest.All);

                //TODO:  refactor calculation here
                //(jira: http://139.196.190.223:8888/browse/QDP-255)
                result.Pv = market.MktQuote.Value[convertibleBond.Bond.Id].Item2 -
		                    optionParts.Pv * convertibleBond.ConversionRatio;
		        result.Delta = optionParts.Delta * convertibleBond.ConversionRatio;
		        result.Gamma = optionParts.Gamma * convertibleBond.ConversionRatio;
		        result.Vega = optionParts.Vega * convertibleBond.ConversionRatio;
		        result.Rho = optionParts.Rho * convertibleBond.ConversionRatio;
		    }
            //(jira: http://139.196.190.223:8888/browse/QDP-251)
            //Disable unnecessary and offending calc below
            //This will fail  14宝钢-E pricing and many other pricing when spot is high

            //var newMarket = market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, market.MktQuote.Value.UpdateKey(convertibleBond.Bond.Id, Tuple.Create(PriceQuoteType.Dirty, result.Pv))));
            //var zeroSpread = BondPricingFunctions.ZeroSpread(convertibleBond.Bond, newMarket);
            //var bondMarket =
            //	market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve,
            //		market.DiscountCurve.Value.GetSpreadedCurve(new ZeroSpread(zeroSpread))));
            //var bondEngine = new BondEngine();
            //var bondResults = bondEngine.Calculate(convertibleBond.Bond, bondMarket, PricingRequest.All);

            return result;
		}
	}
}
