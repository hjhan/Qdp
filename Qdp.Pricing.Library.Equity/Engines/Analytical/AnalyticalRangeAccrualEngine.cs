using System;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Options;

namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{
	public class AnalyticalRangeAccrualEngine : Engine<RangeAccrual>
	{

		public override IPricingResult Calculate(RangeAccrual rangeAccrual, IMarketCondition market, PricingRequest request)
		{
			if (market == null) throw new ArgumentNullException("market");
			var valueDate = market.ValuationDate;

			var pv = 0.0;
			var delta = 0.0;
			var gamma = 0.0;
			var theta = 0.0;
			var vega = 0.0;
			var rho = 0.0;
			var maturityDate = rangeAccrual.ExerciseDates.Last();

			var binaryOptionEngine = new AnalyticalBinaryEuropeanOptionEngine();

			foreach (var range in rangeAccrual.Ranges)
			{
				var nBizDays = range.ObservationDates.Length;
				var dictObsDates = range.ObservationDates.ToDictionary(x => x, x => true);
				// fixing only affects PV
				foreach (var date in rangeAccrual.Fixings.Keys)
				{
					var tmpDate = date;
					var tmpVal = rangeAccrual.Fixings[date];
					if (dictObsDates.ContainsKey(tmpDate) && tmpVal >= range.LowerRange && tmpVal <= range.UpperRange)
					{
						pv += range.BonusRate / nBizDays;
					}
				}

				// random parts
				var restObsDates = range.ObservationDates.Where(x => x > market.ValuationDate);
				foreach (var restObsDate in restObsDates)
				{

					var bo1 = new BinaryOption(valueDate,
						maturityDate,
						OptionExercise.European,
						OptionType.Call,
						range.LowerRange,
						rangeAccrual.UnderlyingProductType,
						BinaryOptionPayoffType.CashOrNothing,
						1.0,
						CalendarImpl.Get("chn"),
						rangeAccrual.DayCount,
						rangeAccrual.PayoffCcy,
						rangeAccrual.SettlementCcy,
						new[] { restObsDate },
						null
						); // will handel maturityDate>exerciseDate
					var bo2 = new BinaryOption(valueDate,
						maturityDate,
						OptionExercise.European,
						OptionType.Call,
						range.UpperRange,
						rangeAccrual.UnderlyingProductType,
						BinaryOptionPayoffType.CashOrNothing,
						1.0,
						CalendarImpl.Get("chn"),
						rangeAccrual.DayCount,
						rangeAccrual.PayoffCcy,
						rangeAccrual.SettlementCcy,
						new[] { restObsDate },
						null
						);

					var re1 = binaryOptionEngine.Calculate(bo1, market, PricingRequest.All);
					var re2 = binaryOptionEngine.Calculate(bo2, market, PricingRequest.All);
					pv += (re1.Pv - re2.Pv) * range.BonusRate / nBizDays;
					delta += (re1.Delta - re2.Delta) * range.BonusRate / nBizDays;
					gamma += (re1.Gamma - re2.Gamma) * range.BonusRate / nBizDays;
					theta += (re1.Theta - re2.Theta) * range.BonusRate / nBizDays;
					vega += (re1.Vega - re2.Vega) * range.BonusRate / nBizDays;
					rho += (re1.Rho - re2.Rho) * range.BonusRate / nBizDays;
				}
			}

			var result = new PricingResult(market.ValuationDate, request)
			{
				Pv = pv*rangeAccrual.Notional,
				Delta = delta * rangeAccrual.Notional,
				Gamma = gamma * rangeAccrual.Notional,
				Theta = theta * rangeAccrual.Notional,
				Vega = vega * rangeAccrual.Notional,
				Rho = rho * rangeAccrual.Notional
			};

			return result;
		}

	}
}
