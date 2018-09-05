using System.Linq;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Notes;

namespace Qdp.Pricing.Library.Equity.Engines.Note
{
	public class ConstantLeveragedNoteEngine : Engine<ConstantLeveragedNote>
	{
		public override IPricingResult Calculate(ConstantLeveragedNote cln, IMarketCondition market, PricingRequest request)
		{
			var result = new PricingResult(market.ValuationDate, request);

			double finalNoteValue;
			result.Pv = CalcPv(cln, market, out finalNoteValue);

			var startFxRate = cln.FxRates[cln.StartDate];
			var endFxRate = cln.FxRates[market.ValuationDate];
			result.Delta = finalNoteValue * cln.TargetLeverage * endFxRate / startFxRate;
			return result;
		}

		private double CalcPv(ConstantLeveragedNote cln, IMarketCondition market, out double finalNoteValue)
		{
			if (cln.Notional.IsAlmostZero())
			{
				finalNoteValue = 0.0;
				return 0.0;
			}

			var valueDate = market.ValuationDate;
			finalNoteValue = cln.Notional;
			if (valueDate < cln.StartDate || valueDate > cln.UnderlyingMaturityDate)
			{
				return 0.0;
			}

			var fixings = cln.Fixings.Where(x => x.Key >= cln.StartDate && x.Key <= valueDate).ToDictionary(x => x.Key, x => x.Value);

			var fixingDate = fixings.First().Key;
			var underlyingPrice = fixings.First().Value;
			var noteValue = cln.Notional;

			foreach (var curDate in fixings.Keys.Skip(1))
			{
				var newUnderlyingPrice = fixings[curDate];
				var dt = curDate - fixingDate;
				var r = newUnderlyingPrice / underlyingPrice - 1.0;
				noteValue *= 1 + cln.TargetLeverage * r - (cln.TargetLeverage - 1) * cln.FundingRate * dt / 365.0;
				fixingDate = curDate;
				underlyingPrice = newUnderlyingPrice;
			}

			var startFxRate = cln.FxRates[cln.StartDate];
			var endFxRate = cln.FxRates[market.ValuationDate];
			var pv = 1.0 + (noteValue / cln.Notional - 1.0) * endFxRate / startFxRate;

			finalNoteValue = noteValue;
			return pv * cln.Notional;

		}
	}
}
