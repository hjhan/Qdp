using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities;

namespace Qdp.Pricing.Library.Common.Engines
{
	public class FxCashflowProductEngine : Engine<IFxCashflowInstrument>
	{
		public override IPricingResult Calculate(IFxCashflowInstrument fxTrade, IMarketCondition market, PricingRequest request)
		{
			var result = new PricingResult(market.ValuationDate, request);

			result.ComponentPvs = GetComponentPvs(fxTrade, market);

			return result;
		}

		public ComponentPv[] GetComponentPvs(IFxCashflowInstrument fxTrade, IMarketCondition market)
		{
			
			var discountCurves = new Dictionary<CurrencyCode, IYieldCurve>();
			discountCurves[fxTrade.DomCcy] = market.DiscountCurve.Value;
			discountCurves[fxTrade.FgnCcy] = market.FgnDiscountCurve.Value;
			if (fxTrade.SettlementCcy != fxTrade.DomCcy && fxTrade.SettlementCcy != fxTrade.FgnCcy)
			{
				discountCurves[fxTrade.SettlementCcy] = market.SettlementCcyDiscountCurve.Value;
			}

			var cashflows = fxTrade.GetCashflows(market, false);
			if (market.ValuationDate >= fxTrade.UnderlyingMaturityDate)
			{
				cashflows = cashflows.Select(
						x => new Cashflow(x.AccrualStartDate, x.AccrualEndDate, x.PaymentDate, 0.0, x.PaymentCurrency, x.CashflowType, x.IsFixed, market.DiscountCurve.Value.GetDf(x.PaymentDate), x.CalculationDetails)
					).ToArray();
			}

			var componentPvs = new List<ComponentPv>();

			var spotDate = fxTrade.SpotDate;
			var componentPvGroup = cashflows.GroupBy(x => x.PaymentCurrency).ToArray();
			foreach (var cfGroup in componentPvGroup)
			{
				var componentCurrency = cfGroup.Key;
				var componentAmount = cfGroup.Sum(x => x.PaymentAmount * discountCurves[x.PaymentCurrency].GetDf(spotDate, x.PaymentDate));
				componentPvs.Add(new ComponentPv{
						ComponentCcy = cfGroup.Key,
						ComponentAmount = componentAmount*discountCurves[componentCurrency].GetDf(market.ValuationDate, spotDate),
						SettlementCcy = fxTrade.SettlementCcy,
						SettlementAmount = market.ConvertCcy(spotDate, componentAmount, componentCurrency, fxTrade.SettlementCcy)*discountCurves[fxTrade.SettlementCcy].GetDf(market.ValuationDate, spotDate)
				});
			}

			return componentPvs.ToArray();
		}
	}
}
