using System;
using System.Linq;
using System.Linq.Expressions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Cds;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;

using Qdp.Pricing.Library.Common.MathMethods.Maths;

namespace Qdp.Pricing.Library.Common.Utilities
{
	public static class YieldCurveCalibrator
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
			IMarketCondition baseMarket = null,
			Expression<Func<IMarketCondition, object>>[] expression = null,
			double initialValue = double.NaN,
			double initialGuess = 0.05,
            //double xmin = -0.9999,
            double xmin = -0.5,
            double xmax = 1.0
			)
		{
			var accuracy = 1.0e-14;
			if (marketInstruments.Any(x => x.Instrument is CreditDefaultSwap) || trait == YieldCurveTrait.DiscountCurve)
			{
				initialValue = 1.0;
				initialGuess = 1.0;
				xmin = 1.0e-64;
			}

			var len = marketInstruments.Length;
			var points = marketInstruments.Select(x => Tuple.Create(x.Instrument.GetCalibrationDate(), initialGuess)).ToList();

			var calibrationStartIndex = 0;

			if (!double.IsNaN(initialValue))
			{
				points.Insert(0, Tuple.Create(referenceDate, initialValue));
				calibrationStartIndex = 1;
			}

			if (interpolation == Interpolation.ForwardFlat)
			{
				//forward flat interpolation, the final instrument is not needed in calibration
				points = points.Take(len - 1).ToList();
				points.Insert(0, Tuple.Create(referenceDate, 0.05));
				calibrationStartIndex = 0;
			}
			else if (trait == YieldCurveTrait.ForwardCurve)
			{
				points.Insert(0, Tuple.Create(referenceDate, 0.05));
				calibrationStartIndex = 1;
			}

			baseMarket = baseMarket ?? new MarketCondition(x => x.ValuationDate.Value = referenceDate);
			expression = expression ?? new Expression<Func<IMarketCondition, object>>[]
			{
				x => x.DiscountCurve,
				x => x.FixingCurve
			};

			IYieldCurve calibratedCurve = new YieldCurve(name, referenceDate, points.ToArray(), bda, daycount, calendar, currency, compound, interpolation, trait, baseMarket);
			var finalInstrument = marketInstruments.Last();
			var smooth = (finalInstrument.Instrument is InterestRateSwap);
			
			double preCalibrationValue = 0.0;
			if (smooth)
			{
				preCalibrationValue = finalInstrument.Instrument.ModelValue(
					baseMarket.UpdateCondition(expression.Select(ex => (IUpdateMktConditionPack)new UpdateMktConditionPack<IYieldCurve>(ex, calibratedCurve)).ToArray()),
					finalInstrument.CalibMethod);
			}

			while (true)
			{
				for (var i = 0; i < len; ++i)
				{
                    var calibrateFunc = new CalibrateMarketInstrument(marketInstruments[i], calibratedCurve, baseMarket, i + calibrationStartIndex, expression);
                    //Note: alternative brent solver gives same result, but slower convergence, 
                    //var calibrateFunc = new CalibrateMarketInstrument2(marketInstruments[i], calibratedCurve, baseMarket, i + calibrationStartIndex, expression);
                    double finalR;
					try
					{
                        finalR = BrentZero.Solve(calibrateFunc, xmin, xmax, accuracy);
                        //finalR = BrentZero2<IUnivariateFunction>.DoSolve(calibrateFunc, xmin, xmax, 0.05, 0);
					}
					catch (Exception ex)
					{
						throw new PricingLibraryException(string.Format("Error when bootstrapping {0}th point", i));
					}
					calibratedCurve = UpdateKeyRate(calibratedCurve, i + calibrationStartIndex, finalR);
				}

				if (!smooth)
				{
					break;
				}
				else
				{
					var postCalibrationValue = finalInstrument.Instrument.ModelValue(
					baseMarket.UpdateCondition(expression.Select(ex => (IUpdateMktConditionPack)new UpdateMktConditionPack<IYieldCurve>(ex, calibratedCurve)).ToArray()),
					finalInstrument.CalibMethod);
					if (Math.Abs(postCalibrationValue - preCalibrationValue) < 1e-12)
					{
						break;
					}
					preCalibrationValue = postCalibrationValue;
				}
			}

			//if initial value is provided for 0D, 不插0D
			if (!double.IsNaN(initialValue))
			{
				return calibratedCurve.KeyPoints.ToArray();
			}

			//NEVER EVER insert points at the end, inset at MOST ONE point at the beginning
			return new[] { Tuple.Create(referenceDate, calibratedCurve.KeyPoints[0].Item2)} // Insert 0D point to avoid interpolation jump at the beginning
				.Union(calibratedCurve.KeyPoints)
				.ToArray();

		}

		internal class CalibrateMarketInstrument : IFunctionOfOneVarialbe
		{
			private readonly IMarketCondition _market;
			private readonly IYieldCurve _calibratedCurve;
			private readonly IMarketInstrument _marketInstrument;
			private readonly int _nPoint;
			private readonly Expression<Func<IMarketCondition, object>>[] _expressions;

			public CalibrateMarketInstrument(IMarketInstrument marketInstrument, IYieldCurve calibratedCurve, IMarketCondition market, int nPoint, Expression<Func<IMarketCondition, object>>[] expressions)
			{
				_marketInstrument = marketInstrument;
				_market = market;
				_nPoint = nPoint;
				_expressions = expressions;
				_calibratedCurve = calibratedCurve;
			}

			public double F(double x)
			{
				var newCurve = UpdateKeyRate(_calibratedCurve, _nPoint, x);
				var conditions = _expressions.Select(ex => (IUpdateMktConditionPack)new UpdateMktConditionPack<IYieldCurve>(ex, newCurve)).ToArray();
				var updatedMkt = _market.UpdateCondition(conditions);

				var modelValue = _marketInstrument.Instrument.ModelValue(updatedMkt, _marketInstrument.CalibMethod);
				
				var error = modelValue - _marketInstrument.TargetValue;

				return error;
			}
		}

        //Note: no improvement using this over BrentZero, as both are typical brent solver
        internal class CalibrateMarketInstrument2 : IUnivariateFunction
        {
            private readonly IMarketCondition _market;
            private readonly IYieldCurve _calibratedCurve;
            private readonly IMarketInstrument _marketInstrument;
            private readonly int _nPoint;
            private readonly Expression<Func<IMarketCondition, object>>[] _expressions;

            public CalibrateMarketInstrument2(IMarketInstrument marketInstrument, IYieldCurve calibratedCurve, IMarketCondition market, int nPoint, Expression<Func<IMarketCondition, object>>[] expressions)
            {
                _marketInstrument = marketInstrument;
                _market = market;
                _nPoint = nPoint;
                _expressions = expressions;
                _calibratedCurve = calibratedCurve;
            }

            public double Value(double x, int changeIndex)
            {
                var newCurve = UpdateKeyRate(_calibratedCurve, _nPoint, x);
                var conditions = _expressions.Select(ex => (IUpdateMktConditionPack)new UpdateMktConditionPack<IYieldCurve>(ex, newCurve)).ToArray();
                var updatedMkt = _market.UpdateCondition(conditions);

                var modelValue = _marketInstrument.Instrument.ModelValue(updatedMkt, _marketInstrument.CalibMethod);

                var error = modelValue - _marketInstrument.TargetValue;

                return error;
            }
        }

        private static IYieldCurve UpdateKeyRate(IYieldCurve curve, int ind, double newRate)
		{
			var newPoints = curve.KeyPoints.Select((x, i) => i == ind ? Tuple.Create(x.Item1, newRate) : x).ToArray();
			return new YieldCurve(curve.Name, curve.ReferenceDate, newPoints, curve.Bda, curve.DayCount, curve.Calendar, curve.Currency, curve.Compound, curve.Interpolation, curve.Trait, curve.BaseMarket, null, curve.Spread);
		}
	}
}
