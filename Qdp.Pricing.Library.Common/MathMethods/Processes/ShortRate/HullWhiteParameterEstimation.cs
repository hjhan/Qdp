using System;
using System.Linq;
using MathNet.Numerics.Statistics;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Maths;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes.ShortRate
{
	public class HullWhiteParameterEstimation
	{
		private readonly IYieldCurve _yieldCurve;
		private readonly IYieldCurve[] _historicalCurves;
		

		public HullWhiteParameterEstimation(IYieldCurve yieldCurves, IYieldCurve[] historicalCurves)
		{
			_yieldCurve = yieldCurves;
			_historicalCurves = historicalCurves;
		}

		public double CalculateA(double t)
		{
			var solver = new CalculateA(_yieldCurve, t, GetVar(t));
			return BrentZero.Solve(solver, 1e-5, 1e10, 1e-06);
		}

		public double CalculateSigma(double t)
		{
			var a = CalculateA(t);
			return 2*a*GetVar(t)/(1 - Math.Exp(-2*a*t));
		}

		public double GetVar(double t)
		{
			return _historicalCurves.Select(x => x.GetSpotRate(t)).Variance();
		}

		public double Expectation(double var, double a, double t)
		{
			var fac = Math.Exp(-a * t);
			return _yieldCurve.GetSpotRate(0)*fac + GetFm(t) - GetFm(0)*fac + var/a*(1 - 2*fac/(1 - fac*fac));
		}

		private double GetFm(double t)
		{
			return (_yieldCurve.GetDf(t - 0.0001) - _yieldCurve.GetDf(t + 0.0001)) / 0.0002;
		}



	}


	public class CalculateA : IFunctionOfOneVarialbe
	{
		private readonly double _t;
		private readonly IYieldCurve _yieldCurve;
		private readonly double _var;

		public CalculateA(IYieldCurve yieldCurve, double t, double var)
		{
			_yieldCurve = yieldCurve;
			_t = t;
			_var = var;
		}

		public double F(double a)
		{
			var fac = Math.Exp(-a * _t);
			return _yieldCurve.GetSpotRate(0) * fac + GetFm(_t) - GetFm(0) * fac + _var / a * (1 - 2 * fac / (1 - fac * fac)) - _yieldCurve.GetSpotRate(_t);
		}

		private double GetFm(double t)
		{
			return (_yieldCurve.GetDf(t - 0.0001) - _yieldCurve.GetDf(t + 0.0001)) / 0.0002;
		}
	}
}
