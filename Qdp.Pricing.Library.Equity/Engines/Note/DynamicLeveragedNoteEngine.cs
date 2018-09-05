using System;
using System.Linq;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Notes;

namespace Qdp.Pricing.Library.Equity.Engines.Note
{
	public class DynamicLeveragedNoteEngine : Engine<DynamicLeveragedNote>
	{
		public override IPricingResult Calculate(DynamicLeveragedNote dln, IMarketCondition market, PricingRequest request)
		{
			var result = new PricingResult(market.ValuationDate, request);

			double finalLeverage;
			result.Pv = CalcPv(dln, market, out finalLeverage);
			result.Delta = finalLeverage*result.Pv;
			return result;
		}

		private double CalcPv(DynamicLeveragedNote dln, IMarketCondition market, out double finalLeverage)
		{
			if (dln.Notional.IsAlmostZero())
			{
				finalLeverage = 0.0;
				return 0.0;
			}

			var valueDate = market.ValuationDate;
			finalLeverage = dln.TargetLeverage;
			if (valueDate < dln.StartDate || valueDate > dln.UnderlyingMaturityDate)
			{
				return 0.0;
			}

			var fixings = dln.Fixings.Where(x => x.Key >= dln.StartDate && x.Key <= valueDate).ToDictionary(x => x.Key, x => x.Value);

			var fixingDate = fixings.First().Key;
			var underlyingPrice = fixings.First().Value;
			var equityValue = dln.Notional*dln.TargetLeverage;
			var noteValue = dln.Notional;
			var leverage = dln.TargetLeverage;
			var fundingAmount = dln.Notional*(dln.TargetLeverage - 1.0);
			var unpaidInterest = 0.0;
			var paidInterest = 0.0;
			var rebalance = false;
			var dt = 0.0;

			foreach (var curDate in fixings.Keys)
			{
				var newUnderlyingPrice = fixings[curDate];
				dt = curDate - fixingDate;
				var r = newUnderlyingPrice/underlyingPrice - 1.0;
				var newUnpaidInterest = unpaidInterest + fundingAmount*dln.FundingRate*dt/365 - paidInterest;
				double newEquityValue;
				double newFundingAmount;
				double newPaidInterest;
				double newRebalanceCost;

				if (rebalance && curDate < dln.UnderlyingMaturityDate)
				{
					newEquityValue = noteValue*dln.TargetLeverage*(1 + r);
					var diff = newEquityValue - equityValue*(1 + r);
					newPaidInterest = newUnpaidInterest;
					newRebalanceCost = Math.Abs(diff)*dln.RebalaceCostRate;
					newFundingAmount = fundingAmount + diff + newUnpaidInterest + newRebalanceCost;
				}
				else
				{
					newEquityValue = equityValue*(1 + r);
					newFundingAmount = fundingAmount;
					newPaidInterest = 0.0;
					newRebalanceCost = 0.0;
				}

				var newNoteValue = equityValue*(1 + r) - fundingAmount - newUnpaidInterest - newRebalanceCost;
				leverage = newEquityValue/newNoteValue;
				if (leverage > dln.LeverageUpperRange || leverage < dln.LeverageLowerRange)
				{
					rebalance = true;
				}
				else
				{
					rebalance = false;
				}

				underlyingPrice = newUnderlyingPrice;
				fixingDate = curDate;
				equityValue = newEquityValue;
				noteValue = newNoteValue;
				fundingAmount = newFundingAmount;
				unpaidInterest = newUnpaidInterest;
				paidInterest = newPaidInterest;
			}

			finalLeverage = leverage;
			return noteValue;

		}
	}
}
