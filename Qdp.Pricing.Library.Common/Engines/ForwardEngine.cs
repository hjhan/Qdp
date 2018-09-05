using System.Linq;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Engines
{
	public class ForwardEngine<TUnderlying> : Engine<Forward<TUnderlying>>
		where TUnderlying :  IUnderlyingInstrument
	{
		public override IPricingResult Calculate(Forward<TUnderlying> trade, IMarketCondition market, PricingRequest request)
		{
			
			var result = new PricingResult(market.ValuationDate, request);

			if(result.IsRequested(PricingRequest.Pv))
			{
				result.Pv = CalcPv(trade, market);
			}

			if(result.IsRequested(PricingRequest.Dv01))
			{
				result.Dv01 = GetRisks(trade, market, PricingRequest.Dv01).Dv01;
			}

			if (result.IsRequested(PricingRequest.Dv01Underlying))
			{
				result.Dv01 = GetRisks(trade, market, PricingRequest.Dv01Underlying).Dv01Underlying;
			}

			return result;
		}

		public virtual double CalcPv(Forward<TUnderlying> trade, IMarketCondition market)
		{
			if (market.ValuationDate >= trade.UnderlyingMaturityDate)
			{
				return 0.0;
			}
			var cfs = trade.GetReplicatingCashflows(market);
			
			return cfs[0].PaymentAmount * (market.DividendCurves.HasValue ? market.DividendCurves.Value.Values.First().GetDf(market.ValuationDate, trade.UnderlyingMaturityDate) : 1.0)
					+ cfs.Last().PaymentAmount * market.DiscountCurve.Value.GetDf(market.ValuationDate, trade.UnderlyingMaturityDate)
					+ cfs.Where((x, i) => !(i == 0 || i == cfs.Length-1)).Sum(x => x.PaymentAmount * market.UnderlyingDiscountCurve.Value.GetDf(market.ValuationDate, x.PaymentDate));
		}

		public virtual IPricingResult GetRisks(Forward<TUnderlying> trade, IMarketCondition market, PricingRequest pricingRequest)
		{
			return new PricingResult(market.ValuationDate, pricingRequest);
		}
	}
}
