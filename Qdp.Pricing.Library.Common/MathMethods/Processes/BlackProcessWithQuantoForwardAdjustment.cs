using System;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes
{
	/// <summary>
	/// Generalized Black-Scholes stochastic process
	///		This class describes the stochastic process governed by 
	///   dS(t, S) = S*(r(t) - q(t) - \rho \sigma_x \sigma) dt + \sigma dW_t.
	/// </summary>
	public class BlackScholesProcessWithQuantoForwardAdjustment : StochasticProcess1D
	{
		private readonly IYieldCurve _spotRateCurve;
		private readonly IYieldCurve _dividendCurve;
		private readonly IVolSurface _volSurf;
		private readonly IVolSurface _fxVolSurf;
		private readonly double _correlation;

		private readonly double _r;
		private readonly double _q;
		private readonly double _vol;
		private readonly double _fxVol;

		private readonly bool _isConstantRate;
		private readonly bool _isConstantVol;

		public BlackScholesProcessWithQuantoForwardAdjustment(IYieldCurve spotRateCurve, IYieldCurve divCurve, IVolSurface volSurf, double rho, IVolSurface fxVolSurf)
			: base()
		{
			_spotRateCurve = spotRateCurve;
			_dividendCurve = divCurve;
			_volSurf = volSurf;
			_correlation = rho;
			_fxVolSurf = fxVolSurf;
			_isConstantRate = false;
			_isConstantVol = false;
		}

		public BlackScholesProcessWithQuantoForwardAdjustment(IYieldCurve spotRateCurve, IYieldCurve divCurve, double sigma, double rho, double sigmaX)
			: base()
		{
			_spotRateCurve = spotRateCurve;
			_dividendCurve = divCurve;
			_vol = sigma;
			_correlation = rho;
			_fxVol = sigmaX;
			_isConstantRate = false;
			_isConstantVol = true;
		}

		public BlackScholesProcessWithQuantoForwardAdjustment(double r, double q, double sigma, double rho, double sigmaX)
			: base()
		{
			_r = r;
			_q = q;
			_vol = sigma;
			_correlation = rho;
			_fxVol = sigmaX;
			_isConstantRate = true;
			_isConstantVol = true;
		}

		public override double Evolve(double t0, double x0, double dt, double dw)
		{
			return x0 * Math.Exp(Drift(t0, x0, dt)* dt + StdDeviation(t0, x0, dt) * dw);
		}

		public override double Drift(double t, double x, double dt)
		{
			var sigma = Diffusion(t, x);
			var sigmaX = _isConstantVol ? _fxVol : _fxVolSurf.GetValue(t, 0.0);
			return _isConstantRate 
				? (_r - _q -_correlation * sigma * sigmaX - 0.5 * sigma * sigma) 
				: _spotRateCurve.GetInstantaneousForwardRate(t) 
					- _dividendCurve.GetInstantaneousForwardRate(t) 
					- _correlation * sigma * sigmaX
					- 0.5 * sigma * sigma;
		}

		public override double Diffusion(double t, double x)
		{
			return _isConstantVol ? _vol : _volSurf.GetValue(t, x);
		}
	}
}
