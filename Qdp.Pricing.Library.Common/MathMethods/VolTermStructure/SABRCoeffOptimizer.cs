using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.MathMethods.MPFitLib;

namespace Qdp.Pricing.Library.Common.MathMethods.VolTermStructure
{
	/// <summary>
	/// Parameter optimizer for each maturity
	/// </summary>
	internal sealed class SabrCoeffOptimizer 
	{
		private readonly double _initBeta;

		public double MaturityInYears { get; private set; }
		public double AtmVol { get; private set; }

		public SabrCoeffOptimizerResult Result { get; private set; }

		private readonly double _spotPrice;
		private readonly bool _estimateAlpha;
		private readonly bool _useFineTune;
		private readonly IList<double> _strikes;
		private readonly IList<double> _marketVols;

		public SabrCoeffOptimizer(
			double maturity,
			double spotPrice,
			double atmVol,
			IList<double> strikes,
			IList<double> marketVols,
			bool estimateAlpha,
			bool useFineTune,
			double initAlpha = 0.3,
			double initBeta = 0.5,
			double initNu = 0.3,
			double initRho = 0.3)
		{
			MaturityInYears = maturity;
			_spotPrice = spotPrice;
			AtmVol = atmVol;
			_strikes = strikes;
			_marketVols = marketVols;
			_estimateAlpha = estimateAlpha;
			_useFineTune = useFineTune;

			_initBeta = initBeta;

			if (strikes.Count != marketVols.Count)
			{
				throw new PricingLibraryException("strikes and marketVols should have same number of elements!");
			}

			double bestAlpha, bestBeta, bestRho, bestNu;
			var nDataPoints = _marketVols.Count();
			double[] ey = new double[nDataPoints];
			for (int i = 0; i < nDataPoints; ++i )
			{
				ey[i] = 0.1;  // error for data points, we do not need to use this
			}
			const int nParams = 3;
			mp_par[] pars = new mp_par[nParams]   // parameter constraints
			{
				new mp_par(),   // no constraint
				new mp_par(),   
				new mp_par()
			};
			if (!estimateAlpha) // fix alpha
			{
				pars[2] = new mp_par(){isFixed = 1};
			}
			CustomUserVariable v = new CustomUserVariable() { X = _strikes.ToArray(), Y = _marketVols.ToArray(), Ey = ey };
			double[] p = new double[nParams] { initRho, initNu, initAlpha };
			mp_result result = new mp_result(nParams);
			int status = MPFit.Solve(SabrFunc, nDataPoints, nParams, p, pars, null, v, ref result);
			bestRho = p[0];
			bestNu = p[1];
			bestAlpha = estimateAlpha ? p[2] : FindAlpha(_initBeta, p[0], p[1], MaturityInYears, _spotPrice, AtmVol);
			bestBeta = _initBeta;
			Result = new SabrCoeffOptimizerResult(MaturityInYears, bestAlpha, bestBeta, bestRho, bestNu);
		}

		public int SabrFunc(double[] p, double[] dy, IList<double>[] dvec, object vars)
		{
			var b = _initBeta;		// beta = 0.5
			var r = p[0];	// rho
			var nv = p[1];	// nu
			var a = _estimateAlpha // alpha
				? p[2]
				: FindAlpha(b, r, nv, MaturityInYears, _spotPrice, AtmVol);

			CustomUserVariable v = (CustomUserVariable)vars;
			double[] x, y, ey;
			x = v.X;
			y = v.Y;
			ey = v.Ey;

			for (var i = 0; i < dy.Length; i++)
			{
				var estVol = SabrVolSurface.SABRVolFineTune(a, b, r, nv, _spotPrice, _strikes[i], MaturityInYears, _useFineTune);
				dy[i] = (Math.Abs(r)>1 || nv<0) ? 1e100 : estVol - _marketVols[i];  //Impose the constraint that -1<=rho <=1 and v>0
			}

			return 0;
		}

		private double FindAlpha(double beta, double rho, double v, double T, double f0, double atm)
		{
			const double epsilon = 1.0e-8;
			// prepare for the coefficients
			// a0 x^3 + b0 x^2 + c0 x + d0 = 0
			double x;
			// a0 can not be zero
			var a0 = (1.0 - beta) * (1.0 - beta) * T / 24.0 / Math.Pow(f0, 2.0 - 2.0 * beta);
			var b0 = rho * beta * v * T / 4.0 / Math.Pow(f0, 1 - beta);
			var c0 = 1.0 + (2.0 - 3.0 * rho * rho) * v * v * T / 24.0;
			var d0 = -atm * Math.Pow(f0, 1 - beta);

			var a = b0 / a0;
			var b = c0 / a0;
			var c = d0 / a0;
			var Q = (a * a - 3.0 * b) / 9.0;
			var R = (2.0 * a * a * a - 9.0 * a * b + 27.0 * c) / 54.0;

			if (R * R < Q * Q * Q)
			{              // three real roots
				var theta = Math.Acos(R / Math.Sqrt(Q * Q * Q));
				var x1 = -2.0 * Math.Sqrt(Q) * Math.Cos(theta / 3.0) - a / 3.0;
				var x2 = -2.0 * Math.Sqrt(Q) * Math.Cos((theta + 2.0 * Math.PI) / 3.0) - a / 3.0;
				var x3 = -2.0 * Math.Sqrt(Q) * Math.Cos((theta - 2.0 * Math.PI) / 3.0) - a / 3.0;

				x = Math.Max(x1, Math.Max(x2, x3));
				if (x1 > 0.0)
					x = Math.Min(x, x1);
				if (x2 > 0.0)
					x = Math.Min(x, x2);
				if (x3 > 0.0)
					x = Math.Min(x, x3);
			}
			else
			{                // one real root
				var sgn = R > 0.0 ? 1.0 : -1.0;
				var aa = -sgn * Math.Pow(Math.Abs(R) + Math.Sqrt(R * R - Q * Q * Q), 1.0 / 3.0);
				var bb = Math.Abs(aa) < epsilon ? 0.0 : Q / aa;
				x = (aa + bb) - a / 3.0;
			}

			return Math.Max(x, 0.0);
		}

	}


}