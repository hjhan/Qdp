using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Processes;
using Qdp.Pricing.Library.Common.MathMethods.Processes.Trees;
using Qdp.Pricing.Library.Equity.Options;

namespace Qdp.Pricing.Library.Exotic.Engines
{
	public class BinomialConvertibleBondEngine : Engine<ConvertibleBond>
	{
		private readonly int _steps;
		private readonly BinomialTreeType _binomialTreeType;
		public BinomialConvertibleBondEngine(BinomialTreeType binomialTreeType = BinomialTreeType.CoxRossRubinstein, 
			int steps = 40)
		{
			_binomialTreeType = binomialTreeType;
			_steps = steps;
		}
		
		public override IPricingResult Calculate(ConvertibleBond convertibleBond, IMarketCondition market, PricingRequest request)
		{
			var result = new PricingResult(market.ValuationDate, request);

			if (result.IsRequested(PricingRequest.Pv))
			{
				result.Pv = CalcPv(convertibleBond, market);
                //TODO: return pricing vol
            }

            return result;
		}

		private double CalcPv(ConvertibleBond convertibleBond, IMarketCondition market)
		{
			var valueDate = market.ValuationDate;
			var yieldCurve = market.DiscountCurve.Value;
			var dayCount = yieldCurve.DayCount;

			var tEnd = dayCount.CalcDayCountFraction(market.ValuationDate, convertibleBond.UnderlyingMaturityDate);
			var dt = tEnd/_steps;
			var binomialTree = BuildTree(convertibleBond, market);


			double[] callExercisePrices;
			double[] putExercisePrices;
			GetExercisePrices(convertibleBond, market, dayCount, valueDate, dt, convertibleBond.EmbeddedOptions, convertibleBond.StrikePriceType, out callExercisePrices, out putExercisePrices);
	
			var conversionOption = convertibleBond.ConversionOption;
			var isConvertible = new bool[_steps + 1];
			for (var i = 0; i < isConvertible.Length; ++i) isConvertible[i] = false;
			if (conversionOption != null)
			{
				var convStartDate = conversionOption.ExerciseDates.First();
				var convEndDate = conversionOption.ExerciseDates.Last();
				var convStartIdx = convStartDate <= valueDate
					? 0
					: (int)Math.Round(dayCount.CalcDayCountFraction(valueDate, convStartDate) / dt);
				var convEndIdx = convEndDate >= convertibleBond.UnderlyingMaturityDate
					? _steps
					: (int)Math.Round(dayCount.CalcDayCountFraction(valueDate, convEndDate) / dt);
				for (var i = convStartIdx; i <= convEndIdx; ++i) isConvertible[i] = true;
			}

			// set values at the end nodes of the tree
			var debtMat = new double[_steps + 1][];  // debt part of the convertible bond on the tree
			var equityMat = new double[_steps + 1][];   // equity part of the convertible bond on the tree
			var cbMat = new double[_steps + 1][];    // convertible bond on the tree
			var bondCashflows = convertibleBond.GetCashflows(market)
				.Where(x => x.PaymentDate >= valueDate)
				.Select(x => new Tuple<double, double>(dayCount.CalcDayCountFraction(valueDate, x.PaymentDate), x.PaymentAmount))
				.ToList();
			var lastBondCf = Math.Max(bondCashflows.Last().Item2, putExercisePrices[_steps]);  // the bond or the put value, whichever is larger
			equityMat[_steps] = new double[_steps + 1];
			debtMat[_steps] = new double[_steps + 1];
			cbMat[_steps] = new double[_steps + 1];
			for (var j = 0; j < _steps + 1; ++j)  // loop over nodes for the last step
			{
				var convertedStockValue = convertibleBond.ConversionRatio * binomialTree.StateValue(_steps, j);
				if (convertedStockValue < lastBondCf)
				{
					equityMat[_steps][j] = 0;
					debtMat[_steps][j] = lastBondCf;
				}
				else
				{
					equityMat[_steps][j] = convertedStockValue;
					debtMat[_steps][j] = 0;
				}
				cbMat[_steps][j] = equityMat[_steps][j] + debtMat[_steps][j];
			}

			// rollback on the tree
			for (var i = _steps - 1; i >= 0; --i)  // loop over steps backwards
			{
				equityMat[i] = new double[i + 1];
				debtMat[i] = new double[i + 1];
				cbMat[i] = new double[i + 1];

				for (int j = 0; j <= i; ++j) // loop over nodes for step i
				{
					// calculate the rollback value
					var pu = binomialTree.Probability(i, j, BranchDirection.Up);
					var pd = binomialTree.Probability(i, j, BranchDirection.Down);
					var discountFact = yieldCurve.GetDf((i + 1) * dt) / yieldCurve.GetDf(i * dt); // from i*dt to (i+1)*dt
					var riskFreeRate = -Math.Log(discountFact) / dt;
					var creditSpread = market.CreditSpread.Value.Spread;
					equityMat[i][j] = (pu * equityMat[i + 1][j + 1] + pd * equityMat[i + 1][j]) * discountFact;
					debtMat[i][j] = (pu * debtMat[i + 1][j + 1] + pd * debtMat[i + 1][j]) *
						Math.Exp(-(riskFreeRate + creditSpread) * dt);
					var couponCf = bondCashflows.Where(x => (x.Item1 >= i * dt && x.Item1 < (i + 1) * dt));  // TODO: not sure whether should use >= or >
					foreach (var cf in couponCf)
					{
						// add coupon PV happening between i*dt and (i+1)*dt
						debtMat[i][j] += cf.Item2 * Math.Exp(-(riskFreeRate + creditSpread) * (cf.Item1 - i * dt));
					}
					cbMat[i][j] = debtMat[i][j] + equityMat[i][j];

					// if rollback > convertedStocks > callStrike, then forced conversion will be made (after the call)
					var convertedStockValue = convertibleBond.ConversionRatio * binomialTree.StateValue(i, j);
					if (cbMat[i][j] > convertedStockValue && convertedStockValue > callExercisePrices[i])
					{
						if (isConvertible[i])
						{
							equityMat[i][j] = convertedStockValue;
							debtMat[i][j] = 0;
						}
						else
						{
							equityMat[i][j] = callExercisePrices[i];
							debtMat[i][j] = 0;
						}
					}

					// if rollback < convertedStocks, then voluntary conversion will be made (no matter whether a call is made)
					if (cbMat[i][j] < convertedStockValue)
					{
						if (isConvertible[i])
						{
							equityMat[i][j] = convertedStockValue;
							debtMat[i][j] = 0;
						}
					}

					// if rollback < putStrike, then the bond holder will put the bond
					if (cbMat[i][j] < putExercisePrices[i])
					{
						equityMat[i][j] = 0;
						debtMat[i][j] = putExercisePrices[0];  // put still has credit risk?
					}
				}

			}

			return cbMat[0][0];
		}

		private void GetExercisePrices(ConvertibleBond convertibleBond,
			IMarketCondition market,
			IDayCount dayCount,
			Date valueDate,
			double dt,
			VanillaOption[] options,
            PriceQuoteType[] optionStrikeType,
			out double[] callExercisePrices,
			out double[] putExercisePrices
			)
		{
			var infinity = 1.0e20;
			callExercisePrices = Enumerable.Range(0, _steps + 1).Select(x => infinity).ToArray();
			putExercisePrices = Enumerable.Range(0, _steps + 1).Select(x => -infinity).ToArray();
			if (options == null)
			{
				return;
			}

			for(var i = 0; i < options.Length; ++i)
			{
				var option = options[i];
				var start = option.ExerciseDates.First();
				var end = option.ExerciseDates.Last();
				var startInd = start <= valueDate ? 0 : (int) Math.Round(dayCount.CalcDayCountFraction(valueDate, start)/dt);
				var endInd = start >= convertibleBond.UnderlyingMaturityDate ? _steps : (int)Math.Round(dayCount.CalcDayCountFraction(valueDate, end) / dt);
				for (var j = startInd; j <= endInd; ++j)
				{
					if (option.OptionType == OptionType.Call)
					{
						callExercisePrices[j] = option.Strike;
						if (optionStrikeType[i] == PriceQuoteType.Clean)
						{
							var date = new Term(j * dt, Period.Year).Next(valueDate);
							callExercisePrices[j] += convertibleBond.GetAccruedInterest(date, market);
						}
					}
					else
					{
						putExercisePrices[j] = option.Strike;
						if (optionStrikeType[i] == PriceQuoteType.Clean)
						{
							var date = new Term(j * dt, Period.Year).Next(valueDate);
							putExercisePrices[j] += convertibleBond.GetAccruedInterest(date, market);
						}
					}
				}
			}
		}

		private BinomialTree BuildTree(ConvertibleBond convertibleBond, IMarketCondition market)
		{
			var endDate = convertibleBond.UnderlyingMaturityDate;
            //assuming we use a strike vol
			var process = new BlackScholesProcess(
				market.DiscountCurve.Value.ZeroRate(market.ValuationDate, endDate),
				market.DividendCurves.Value.Values.First().ZeroRate(market.ValuationDate, endDate),
				market.VolSurfaces.Value.Values.First().GetValue(endDate, market.SpotPrices.Value.Values.First())
				);
			var dayCount = market.DiscountCurve.Value.DayCount;
			var tEnd = dayCount.CalcDayCountFraction(market.ValuationDate, convertibleBond.UnderlyingMaturityDate);
			return new CoxRossRubinsteinBinomialTree(process, market.SpotPrices.Value.Values.First(), tEnd, _steps);
		}
	}
}
