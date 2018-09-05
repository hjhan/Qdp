using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.Statistics;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Maths;

namespace UnitTest.CommonTests
{
	public class HullWhiteParameterEstimationTest
	{
		private readonly IYieldCurve _yieldCurve;
		private readonly IYieldCurve[] _historicalCurves;
		

		public HullWhiteParameterEstimationTest(IYieldCurve yieldCurves, IYieldCurve[] historicalCurves)
		{
			_yieldCurve = yieldCurves;
			_historicalCurves = historicalCurves;
		}

		//https://www.sitmo.com/?p=134
		//Use Ornstein Uhlenbeck process to approximately estimate a and sigma
		public void CalcOrnsteinUhlenbeck(out double lambda, out double sigma, out double sigma2, out double sigma3)
		{
			var lambdas = new List<double>();
			var sigmas = new List<double>();
			var sigmas2 = new List<double>();
			var r = _historicalCurves.Select(x => x.GetSpotRate(0.0)).ToArray();
			sigma3 = r.StandardDeviation()*Math.Sqrt(252);
			foreach (var curve in _historicalCurves)
			{
				var dt = 0.1;
				var sarray = new List<double>();
				for (var t = 0.0; t < 15; t += dt)
				{
					sarray.Add(curve.GetInstantaneousForwardRate(t, Compound.Continuous));
				}
				var sx = sarray.Sum() - sarray.Last();
				var sy = sarray.Sum() - sarray[0];
				var sxx = sarray.Select(x => x*x).Sum() - sarray.Last()*sarray.Last();
				var syy = sarray.Select(x => x*x).Sum() - sarray[0]*sarray[0];
				var sxy = 0.0;
				for (var i = 0; i < sarray.Count - 1; ++i)
				{
					sxy += sarray[i]*sarray[i + 1];
				}

				var n = sarray.Count - 1;
				var a = (n*sxy - sx*sy)/(n*sxx - sx*sx);
				var b = (sy - a*sx)/n;
				var sdepsilon = Math.Sqrt((n*syy - sy*sy - a*(n*sxy - sx*sy))/(n*(n - 2)));

				lambdas.Add(-Math.Log(a)/dt);
				sigmas.Add(sdepsilon*Math.Sqrt((-2*Math.Log(a))/(dt*(1 - a*a))));

				//residual as sigma
				var epsilons = sarray.Skip(1).Select((x, i) => Math.Pow(sarray[i + 1] - (sarray[i]*a + b), 2));
				sigmas2.Add(Math.Sqrt(epsilons.Average()));
			}

			lambda = lambdas.Average();
			sigma = sigmas.Average();
			sigma2 = sigmas2.Average();
		}

		public double CalculateA(double t)
		{
			var solver = new CalculateA(_yieldCurve, t, GetSigmaSma());
			return BrentZero.Solve(solver, 1e-5, 1e10, 1e-06);
		}

		public double CalculateSigma(double t)
		{
			var a = CalculateA(t);
			return 2*a*GetSigmaSma()/(1 - Math.Exp(-2*a*t));
		}

		public double GetSigmaSma()
		{
			var forwardRate = _historicalCurves.Select(x => x.GetForwardRate(x.ReferenceDate, new Term("7D"))).ToArray();
			return Math.Sqrt(forwardRate.Variance()) * Math.Sqrt(250);
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

		private double GetShortRate(double t)
		{
			return 0.0;
		}

		private double GetFm(double t)
		{
			return (_yieldCurve.GetDf(t - 0.0001) - _yieldCurve.GetDf(t + 0.0001)) / 0.0002;
		}
	}
}
