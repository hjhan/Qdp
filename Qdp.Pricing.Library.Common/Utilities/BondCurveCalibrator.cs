using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.MathMethods.NlOptWrapper;

namespace Qdp.Pricing.Library.Common.Utilities
{
	public class BondCurveCalibrator
	{
		public static Tuple<Date, double>[] Calibrate(
			string name,
			Date referenceDate,
			IMarketInstrument[] marketInstruments,
			BusinessDayConvention bda,
			IDayCount daycount,
			ICalendar calendar,
			Compound compound,
			Interpolation interpolation,
			YieldCurveTrait trait,
			CurrencyCode currency,
			Date[] knotPoints,
			out double fittingError,
			IMarketCondition baseMarket = null,
			Expression<Func<IMarketCondition, object>>[] expression = null,
			double initialValue = double.NaN,
			double initialGuess = 0.05,
			double xmin = -3,
			double xmax = 3.0
			)
		{
			var accuracy = 1.0e-13;
			if (marketInstruments.Any(x => !(x.Instrument is Bond)))
			{
				throw new PricingLibraryException("All instruments must be bond to build a bond curve!");
			}

			var keyTs = knotPoints.Select(x => daycount.CalcDayCountFraction(referenceDate, x)).ToArray();
			var len = keyTs.Length;

			baseMarket =
				baseMarket
				??
				new MarketCondition(
					x => x.ValuationDate.Value = referenceDate,
					x => x.DiscountCurve.Value = null,
					x => x.FixingCurve.Value = null,
					x => x.HistoricalIndexRates.Value = new Dictionary<IndexType, SortedDictionary<Date, double>>()
					);

			var bondYieldPricer = new BondYieldPricer();
			var bonds = marketInstruments.Select(x => x.Instrument as Bond);
			var bondCf = bonds.Select(x => x.GetCashflows(baseMarket, true)).ToList();
			var bondPrices =
				bonds.Select(
					(x, i) =>
						bondYieldPricer.FullPriceFromYield(bondCf[i], x.PaymentDayCount, x.PaymentFreq, x.StartDate, referenceDate,
							marketInstruments[i].TargetValue, x.BondTradeingMarket, x.IrregularPayment)).ToArray();

			var rand = new Random();
			// variables : alpha, b1, b2, b3, b4, b5 ...
			var numVars = 1 + 2 + len;
			var lowerBounds = new double[numVars];
			var upperBounds = new double[numVars];
			var initials = new double[numVars];
			double? finalScore = double.NaN;

			for (var i = 0; i < numVars; ++i)
			{
				lowerBounds[i] = i == 0 ? 0.0 : xmin;
				upperBounds[i] = i == 0 ? 1.0 : xmax;
				initials[i] = rand.NextDouble() > 0 ? rand.NextDouble() : -rand.NextDouble();
			}

			var globalSolver = new NLoptSolver(NLoptAlgorithm.GN_DIRECT, (ushort)numVars, accuracy, 100000, NLoptAlgorithm.LN_COBYLA);
			globalSolver.SetLowerBounds(lowerBounds);
			globalSolver.SetUpperBounds(upperBounds);
			globalSolver.SetMinObjective((variables, gradients) =>
			{
				var error =
					bondCf.Select((cfs, i) => GetModelPrice(referenceDate, cfs, daycount, variables, keyTs) - bondPrices[i]).ToArray();
				return error.Sum(x => x * x);
			});
			double? globalFinalScore;
			var globalResult = globalSolver.Optimize(initials, out globalFinalScore);

			var localSolvers = new[] { NLoptAlgorithm.LN_BOBYQA, NLoptAlgorithm.LD_AUGLAG, NLoptAlgorithm.LN_COBYLA};
			for (var k = 0; k < 3; ++k)
			{
				var localSolver = new NLoptSolver(localSolvers[k], (ushort)numVars, accuracy, 100000, NLoptAlgorithm.LN_COBYLA);
				localSolver.SetLowerBounds(lowerBounds);
				localSolver.SetUpperBounds(upperBounds);
				localSolver.SetMinObjective((variables, gradients) =>
				{
					var error = bondCf.Select((cfs, i) => GetModelPrice(referenceDate, cfs, daycount, variables, keyTs) - bondPrices[i]).ToArray();
					return error.Sum(x => x*x);
				});
				
				var result = localSolver.Optimize(initials, out finalScore);
			}
			fittingError = finalScore.Value;

			var coeffes = new[]
			{
				Tuple.Create(referenceDate, initials[0]),
				Tuple.Create(referenceDate, -1.0 - initials[1] - initials[2] - initials.Skip(3).Sum()*1/3.0),
				Tuple.Create(referenceDate, initials[1]),
				Tuple.Create(referenceDate, initials[2]),
			}
				.Union(knotPoints.Select((x, i) => Tuple.Create(x, initials[i + 3])))
				.ToArray();

			//var errors = bondCf.Select((cfs, i) => GetModelPrice(referenceDate, cfs, daycount, initials, keyTs)).ToArray();

			//for (var i = 0; i < errors.Length; ++i)
			//{

			//	Console.WriteLine("{0},{1},{2}", errors[i], bondPrices[i], errors[i] - bondPrices[i]);
			//}

			return coeffes; // Insert 0D point to avoid interpolation jump at the beginning

		}

		private static double GetModelPrice(Date referenceDate, Cashflow[] cfs, IDayCount dayCount, double[] variables, double[] keyTs)
		{
			var points = cfs.Where(x => x.PaymentDate > referenceDate).Select(x =>
			{
				var t = dayCount.CalcDayCountFraction(referenceDate, x.PaymentDate);
				return Tuple.Create(t, x.PaymentAmount);
			}).ToArray();

			return points.Sum(p => GetDf(variables, keyTs, p.Item1) * p.Item2);
		}

		private static double GetDf(double[] variables, double[] keyTs, double t)
		{
			var f = Math.Exp(-variables[0] * t);

			var coeff = -1.0 - variables[1] - variables[2] - variables.Skip(3).Sum() * 1 / 3.0;
			var df = 1 + coeff * (1 - f) + variables[1] * (1 - f * f) + variables[2] * (1 - f * f * f);

			for (var i = 0; i < keyTs.Length; ++i)
			{
				if (t >= keyTs[i])
				{
					var tmpF = Math.Exp(-variables[0] * (t - keyTs[i]));
					df += variables[i + 3] * ((1 - tmpF) - (1 - tmpF * tmpF) + 1 / 3.0 * (1 - tmpF * tmpF * tmpF));
				}
			}

			return df;
		}
	}
}
