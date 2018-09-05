using System;
using System.Linq;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.FiniteDifference;
using Qdp.Pricing.Library.Common.MathMethods.Processes;
using Qdp.Pricing.Library.Equity.Interfaces;

namespace Qdp.Pricing.Library.Equity.Engines.Numerical
{
    /// <summary>
    /// 美式期权的有限差分计算引擎
    /// </summary>
	public class FdAmericanOptionEngine : BaseNumericalOptionEngine
	{
		private readonly int _steps;
		private readonly int _gridSize;
		private readonly double _safetyRangeFactor;

		public FdAmericanOptionEngine(int steps = 500, int gridSize = 500, double safetyRagenFactor = 1.1)
		{
			_steps = steps;
			_gridSize = gridSize;
			_safetyRangeFactor = safetyRagenFactor;
		}

		protected override double CalcPv(IOption option, IMarketCondition market, double timeIncrement = 0.0)
		{
			var dayCount = market.DiscountCurve.Value.DayCount;
			var startDate = market.ValuationDate;
			var maturityDate = option.ExerciseDates.Last();
			var t = dayCount.CalcDayCountFraction(market.ValuationDate, maturityDate) + timeIncrement;
			var bsProcess = new BlackScholesProcess(
				market.DiscountCurve.Value.ZeroRate(startDate, maturityDate),
				market.DividendCurves.Value.Values.First().ZeroRate(startDate, maturityDate),
				market.VolSurfaces.Value.Values.First().GetValue(maturityDate, option.Strike));

			var pdeSolver = new SecondOrderPdeCrankNicolson(
				bsProcess.Drift, 
				(t0, s) => 0.5*Math.Pow(bsProcess.Diffusion(t0, s), 2.0),
				market.DiscountCurve.Value.GetSpotRate);

			double[] x;
			double[] xGrid;
			InitializeGrid(market.SpotPrices.Value.Values.First(), option.Strike, bsProcess.Diffusion(0, 0), t, out x, out xGrid);
			var dt = t/_steps;
			return pdeSolver.Solve(
				Enumerable.Range(0, _steps + 1).Select(i => i*dt).ToArray(),
				xGrid,
				x,
				price => option.GetPayoff(new[] { price })[0].PaymentAmount
				)[0][_gridSize];
		}

		private void InitializeGrid(double spotPrice, double strike, double vol, double t, out double[] x, out double[] xGrid)
		{
			var volT = vol*Math.Sqrt(t);
			var factor = Math.Exp(7.0*(1 + 0.02/volT)*volT);
			var sMin = spotPrice/factor;
			var sMax = spotPrice*factor;

			if (sMax < _safetyRangeFactor*strike)
			{
				sMin = strike/_safetyRangeFactor;
				sMax = spotPrice/(sMin/spotPrice);
			}

			if (sMin > strike/_safetyRangeFactor)
			{
				sMax = strike*_safetyRangeFactor;
				sMin = spotPrice/(sMax/spotPrice);
			}

			var start = Math.Log(sMin);
			var dx = (Math.Log(sMax) - start)/(2.0*_gridSize);
			xGrid = Enumerable.Range(0, 2*_gridSize + 1)
				.Select(i => start + i*dx)
				.ToArray();
			x = xGrid.Select(Math.Exp).ToArray();
		}
	}
}
