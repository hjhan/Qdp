using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Options;

namespace Qdp.Pricing.Library.Equity.Engines.Note
{
	class EquityLinkedNoteEngine<TOption, TOptionEngine> : Engine<EquityLinkedNote<TOption>>
		where TOption : OptionBase
		where TOptionEngine : Engine<OptionBase>
	{
		public TOptionEngine OptionEngine { get; private set; }

		public override IPricingResult Calculate(EquityLinkedNote<TOption> trade, IMarketCondition market, PricingRequest request)
		{
			var result = new PricingResult(market.ValuationDate, request);

			var optionResult = OptionEngine.Calculate(trade.Option, market, request);

			if (result.IsRequested(PricingRequest.Pv)) result.Pv = optionResult.Pv + trade.Notional * market.DiscountCurve.Value.GetDf(market.ValuationDate, trade.UnderlyingMaturityDate);
			if (result.IsRequested(PricingRequest.Delta)) result.Delta = optionResult.Delta;
			if (result.IsRequested(PricingRequest.Gamma)) result.Gamma = optionResult.Gamma;
			if (result.IsRequested(PricingRequest.Rho)) result.Rho = optionResult.Rho;
			if (result.IsRequested(PricingRequest.Vega)) result.Vega = optionResult.Vega;
			if (result.IsRequested(PricingRequest.Theta)) result.Theta = optionResult.Theta;
			return result;
		}

	}
}
