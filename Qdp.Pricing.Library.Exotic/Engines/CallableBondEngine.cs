using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Market.Spread;
using Qdp.Pricing.Library.Common.MathMethods.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Maths;
using Qdp.Pricing.Library.Common.MathMethods.Utilities;
using Qdp.Pricing.Library.Common.Utilities;

namespace Qdp.Pricing.Library.Exotic.Engines
{
	public class CallableBondEngine<TInterestRateModel> : Engine<CallableBond>
		where TInterestRateModel : class, IOneFactorModel
	{
		private readonly TInterestRateModel _model;
		private readonly bool _adjustCouponDates;
		private readonly int _steps;

		public CallableBondEngine(TInterestRateModel model,
			bool adjustCouponDates,
			int steps)
		{
			_model = model;
			_adjustCouponDates = adjustCouponDates;
			_steps = steps;
		}
		public override IPricingResult Calculate(CallableBond callableBond, IMarketCondition market, PricingRequest request)
		{
			var result = new PricingResult(market.ValuationDate, request);

			if (result.IsRequested(PricingRequest.Pv))
			{
				result.Pv = CalcPv(callableBond, market);
			}

			return result;
		}

		public double CalcPv(CallableBond callableBond, IMarketCondition market)
		{
			var referenceDate = market.ValuationDate.Value;
			var yieldCurve = market.DiscountCurve.Value;
			var dayCount = yieldCurve.DayCount;
			var gridTimes = GetGridTimes(callableBond, market);
			var tree = _model.Tree(gridTimes, 0.0, yieldCurve);
			var cashflows = GetCashflows(callableBond, market)
				.Where(cf => cf.PaymentDate >= market.ValuationDate.Value)
				.Select(cf => Tuple.Create(dayCount.CalcDayCountFraction(referenceDate, cf.PaymentDate), cf.PaymentAmount)
				).ToList();

			var exerciseInfos = callableBond.GetExerciseInfo(market)
				.Select(x => Tuple.Create(
					dayCount.CalcDayCountFraction(referenceDate, x.Item1), 
					(INumericCondition)new CallPutCondition(x.Item2, x.Item3))
					).ToList();

			var priceOnGrids = tree.ReverseInduction(cashflows, exerciseInfos);
			return priceOnGrids[0][0];
		}

		private double OasSpread(CallableBond callableBond, 
			IMarketCondition market,
			CallableBondEngine<TInterestRateModel> engine)
		{
			return BrentZero.Solve(new OasSpreadSolver<TInterestRateModel>(callableBond, market, engine), -1.0, 1.0, 1e-12);
		}

		private double[] GetGridTimes(CallableBond callableBond, IMarketCondition market)
		{
			var grids = new[] { market.ValuationDate.Value }
				.Union(GetGridDates(callableBond, market))
				.Select(x => market.DiscountCurve.Value.DayCount.CalcDayCountFraction(market.ValuationDate.Value, x))
				.ToArray();

			if (_steps == 0)
			{
				return grids;
			}
			else
			{
				var newGrids = new List<double>();
				var dtMax = (grids.Last() - grids[0]) / (_steps);
				newGrids.Add(grids[0]);
				for (var i = 1; i < grids.Length; ++i)
				{
					var begin = grids[i - 1];
					var end = grids[i];
					var n = (int)((end - begin) / dtMax + 0.5);
					if (n == 0) n = 1;
					var dt = (end - begin) / n;
					for (var j = 1; j <= n; ++j)
					{
						newGrids.Add(begin + j * dt);
					}
				}
				return newGrids.ToArray();
			}
		}

		private Cashflow[] GetCashflows(CallableBond callableBond, IMarketCondition market)
		{
			var exerciseDates = callableBond.EmbededOptions.SelectMany(x => x.ExerciseDates).ToArray();
			if (_adjustCouponDates)
			{
				return callableBond.GetCashflows(market)
					.Select(cf =>
						new Cashflow(
							cf.AccrualStartDate,
							cf.AccrualEndDate,
							AdjustToGrid(new[] { cf.PaymentDate }, exerciseDates)[0],
							cf.PaymentAmount,
							cf.PaymentCurrency,
							cf.CashflowType,
							cf.IsFixed,
							market.GetDf(AdjustToGrid(new[] { cf.PaymentDate }, exerciseDates)[0]),
							cf.CalculationDetails)
					).ToArray();

			}
			return callableBond.GetCashflows(market);
		}

		private Date[] GetGridDates(CallableBond callableBond, IMarketCondition market)
		{
			var dates = new List<Date> { callableBond.UnderlyingMaturityDate }
				.Union(callableBond.Bond.GetCashflows(market)
				.Select(cf => cf.PaymentDate))
				.ToArray();
			var exerciseDates = callableBond.EmbededOptions.SelectMany(x => x.ExerciseDates).ToArray();
			if (_adjustCouponDates)
			{
				dates = AdjustToGrid(dates, exerciseDates);
			}

			return dates.Union(exerciseDates).Where(x => x >= market.ValuationDate.Value).OrderBy(x => x).ToArray();
		}

		private Date[] AdjustToGrid(Date[] datesToBeAdjusted, Date[] targetDates)
		{
			var tempDates = datesToBeAdjusted.Select(x => new Date(x)).ToArray();
			//as an approximation, coupon dates within 7 days to call/put are adjusted to call/put dates
			for (var i = 0; i < tempDates.Length; ++i)
			{
				foreach (var targetDate in targetDates)
				{
					if (tempDates[i] > targetDate && tempDates[i] - targetDate < 7.0)
					{
						tempDates[i] = targetDate;
					}
				}
			}
			return tempDates;
		}
	}

	internal class OasSpreadSolver<TInterestRateModel> : IFunctionOfOneVarialbe
		where TInterestRateModel : class, IOneFactorModel
	{
		private readonly CallableBond _callableBond;
		private readonly IMarketCondition _market;
		private readonly CallableBondEngine<TInterestRateModel> _engine; 

		public OasSpreadSolver(CallableBond callableBond,
			IMarketCondition market,
			CallableBondEngine<TInterestRateModel> engine)
		{
			_callableBond = callableBond;
			_market = market;
			_engine = engine;
		}

		public double F(double x)
		{
			var market = _market.UpdateCondition(
				new UpdateMktConditionPack<IYieldCurve>(m => m.DiscountCurve, _market.DiscountCurve.Value.GetSpreadedCurve(new ZeroSpread(x)))
				);
			return _market.MktQuote.Value[_callableBond.Bond.Id].Item2 - _engine.CalcPv(_callableBond, market);
		}
	}
}
