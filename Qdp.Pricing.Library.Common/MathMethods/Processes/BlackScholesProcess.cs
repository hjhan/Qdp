using System;
using System.Collections.Generic;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes
{
	/// <summary>
	/// Generalized Black-Scholes stochastic process
	///		This class describes the stochastic process governed by dS(t, S) = (r(t) - q(t)) S dt + \sigma dW_t.
	/// </summary>
	public class BlackScholesProcess : StochasticProcess1D
	{
		private readonly IYieldCurve _spotRateCurve;
		private readonly IYieldCurve _dividendCurve;
		private readonly IVolSurface _volSurf;
		private readonly Dictionary<double, double> _spotRateCurveForwardrateDict;
		private readonly Dictionary<double, double> _dividendCurveForwardrateDict;

		private readonly double _r;
		private readonly double _q;
		private readonly double _vol;

		private readonly bool _isConst;
		private readonly bool _isCached;

		private const int Precision = 7;
		public BlackScholesProcess(IYieldCurve spotRateCurve, IYieldCurve divCurve, IVolSurface volSurf, double[] times = null)
		{
			_spotRateCurve = spotRateCurve;
			_dividendCurve = divCurve;
			_volSurf = volSurf;
			_isConst =  false;

			if (times == null) return;

			//cache forward rate
			_spotRateCurveForwardrateDict = new Dictionary<double, double>();
			_dividendCurveForwardrateDict = new Dictionary<double, double>();
			for(var i = 0; i < times.Length-1; ++i)
			{
				var t0 = times[i];
				var dt = times[i + 1] - times[i];
				_spotRateCurveForwardrateDict[Math.Round(t0, Precision)] = _spotRateCurve.GetForwardRate(t0, dt, _spotRateCurve.Compound);
				_dividendCurveForwardrateDict[Math.Round(t0, Precision)] = _dividendCurve.GetForwardRate(t0, dt, _dividendCurve.Compound);
			}
			_isCached = true;
		}

		public BlackScholesProcess(double r, double q, double sigma)
		{
			_r = r;
			_q = q;
			_vol = sigma;
			_isConst = true;
			_isCached = false;
		}

		public double GetDiscountRate(double t0)
		{
			return _r;
		}

		public double GetDividendRate(double t0)
		{
			return _q;
		}

		public override double Evolve(double t0, double x0, double dt, double dw)
		{
			return x0 * Math.Exp(Drift(t0, x0, dt)*dt + StdDeviation(t0, x0, dt)* dw);
		}

		public override double Drift(double t, double x, double dt)
		{
			var sigma = Diffusion(t, x);
			if (_isConst)
			{
				return (_r - _q - 0.5*sigma*sigma);
			}
			
			if (!_isCached)
			{
				return _spotRateCurve.GetForwardRate(t, dt, _spotRateCurve.Compound) - _dividendCurve.GetForwardRate(t, dt, _dividendCurve.Compound) - 0.5 * sigma * sigma;
			}

			return _spotRateCurveForwardrateDict[Math.Round(t, Precision)] - _dividendCurveForwardrateDict[Math.Round(t, Precision)] - 0.5 * sigma * sigma;
		}

		public override double Diffusion(double t, double x)
		{
			return _isConst ? _vol : _volSurf.GetValue(t, x);
		}
	}
}
