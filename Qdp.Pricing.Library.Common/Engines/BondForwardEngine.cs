using System;
using System.Collections.Generic;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Market.Spread;
using Qdp.Pricing.Library.Common.Utilities;

namespace Qdp.Pricing.Library.Common.Engines
{
	public class BondForwardEngine : ForwardEngine<Bond>
	{
		public override double CalcPv(Forward<Bond> trade, IMarketCondition market)
		{
			var zeroSpread = GetUnderlyingBondZeroSpread(trade.Underlying, market);
			var bfMarket = market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.UnderlyingDiscountCurve, market.UnderlyingDiscountCurve.Value.GetSpreadedCurve(zeroSpread)));
			return base.CalcPv(trade, bfMarket);
		}

		public override IPricingResult GetRisks(Forward<Bond> trade, IMarketCondition market, PricingRequest pricingRequest)
		{
			var result = new PricingResult(market.ValuationDate, pricingRequest);
			var bondEngine = new BondEngine();
			var bMarket = market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, market.UnderlyingDiscountCurve.Value));
			
			if (result.IsRequested(PricingRequest.Dv01))
			{
				var bondZeroSpread = BondPricingFunctions.ZeroSpread(trade.Underlying, bMarket);
				IMarketCondition bondMktUp;
				IMarketCondition bondMktDown;
				if (market.FixingCurve.HasValue)
				{
					bondMktUp = bMarket.UpdateCondition(
							new UpdateMktConditionPack<IYieldCurve>(x => x.FixingCurve, bMarket.FixingCurve.Value.Shift(1)),
							new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, bMarket.DiscountCurve.Value.Shift(1).GetSpreadedCurve(new ZeroSpread(bondZeroSpread)))
							);
					bondMktDown = bMarket.UpdateCondition(
							new UpdateMktConditionPack<IYieldCurve>(x => x.FixingCurve, bMarket.FixingCurve.Value.Shift(-1)),
							new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, bMarket.DiscountCurve.Value.Shift(-1).GetSpreadedCurve(new ZeroSpread(bondZeroSpread)))
							);
				}
				else
				{
					bondMktUp = bMarket.UpdateCondition(
							new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, bMarket.DiscountCurve.Value.Shift(1).GetSpreadedCurve(new ZeroSpread(bondZeroSpread)))
							);
					bondMktDown = bMarket.UpdateCondition(
							new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, bMarket.DiscountCurve.Value.Shift(-1).GetSpreadedCurve(new ZeroSpread(bondZeroSpread)))
							);
				}

				var fwdMarket = market.UpdateCondition(new UpdateMktConditionPack<ISpread>(x => x.CreditSpread, new ZeroSpread(bondZeroSpread)));
				var upPv = bondEngine.Calculate(trade.Underlying, bondMktUp, PricingRequest.Pv).Pv;
				var downPv = bondEngine.Calculate(trade.Underlying, bondMktDown, PricingRequest.Pv).Pv;

				if (fwdMarket.FixingCurve.HasValue)
				{
					var fwdMktUp = fwdMarket.UpdateCondition(
							new UpdateMktConditionPack<IYieldCurve>(x => x.FixingCurve, bMarket.FixingCurve.Value.Shift(1)),
							new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, bMarket.DiscountCurve.Value.Shift(1)),
							new UpdateMktConditionPack<IYieldCurve>(x => x.UnderlyingDiscountCurve, bMarket.UnderlyingDiscountCurve.Value.Shift(1)),
							new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, bMarket.MktQuote.Value.UpdateKey(trade.Underlying.Id, Tuple.Create(PriceQuoteType.Dirty, upPv)))
							);
					var fwdMktDown = fwdMarket.UpdateCondition(
							new UpdateMktConditionPack<IYieldCurve>(x => x.FixingCurve, bMarket.FixingCurve.Value.Shift(-1)),
							new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, bMarket.DiscountCurve.Value.Shift(-1)),
							new UpdateMktConditionPack<IYieldCurve>(x => x.UnderlyingDiscountCurve, bMarket.UnderlyingDiscountCurve.Value.Shift(-1)),
							new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, bMarket.MktQuote.Value.UpdateKey(trade.Underlying.Id, Tuple.Create(PriceQuoteType.Dirty, downPv)))
							);
					result.Dv01 = (CalcPv(trade, fwdMktDown) - CalcPv(trade, fwdMktUp))/2.0;
				}
				else
				{
					var fwdMktUp = fwdMarket.UpdateCondition(
							new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, bMarket.DiscountCurve.Value.Shift(1)),
							new UpdateMktConditionPack<IYieldCurve>(x => x.UnderlyingDiscountCurve, bMarket.UnderlyingDiscountCurve.Value.Shift(1)),
							new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, bMarket.MktQuote.Value.UpdateKey(trade.Underlying.Id, Tuple.Create(PriceQuoteType.Dirty, upPv)))
							);
					var fwdMktDown = fwdMarket.UpdateCondition(
							new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, bMarket.DiscountCurve.Value.Shift(-1)),
							new UpdateMktConditionPack<IYieldCurve>(x => x.UnderlyingDiscountCurve, bMarket.UnderlyingDiscountCurve.Value.Shift(-1)),
							new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, bMarket.MktQuote.Value.UpdateKey(trade.Underlying.Id, Tuple.Create(PriceQuoteType.Dirty, downPv)))
							);
					result.Dv01 = (CalcPv(trade, fwdMktDown) - CalcPv(trade, fwdMktUp)) / 2.0;
				}
			}

			if (result.IsRequested(PricingRequest.Dv01Underlying))
			{
				var factor = trade.Notional/trade.Underlying.Notional;
				result.Dv01Underlying = bondEngine.Calculate(trade.Underlying, bMarket, PricingRequest.Dv01).Dv01*factor;
			}

			return result;
		}

		private ISpread GetUnderlyingBondZeroSpread(Bond bond, IMarketCondition market)
		{
			if (market.CreditSpread.HasValue)
			{
				return market.CreditSpread.Value;
			}
			else
			{
				var bMarket = market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, market.UnderlyingDiscountCurve.Value));
				return new ZeroSpread(BondPricingFunctions.ZeroSpread(bond, bMarket));
			}
		}
	}
}
