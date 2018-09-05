using System;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Processes.Trees;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes.ShortRate
{
	// dr_t = a(b - r_t)dt + \sigma dW_t
	public class Vasicek : IOneFactorModel
	{
		private double _r0, _a, _b, _sigma, _lambda;

		public Vasicek(double r0 = 0.05, double a = 0.1, double b = 0.05, double sigma = 0.01, double lambda = 0.0)
		{
			_r0 = r0;
			_a = a;
			_b = b;
			_sigma = sigma;
			_lambda = lambda;
		}

		public double A(double t, double T, IYieldCurve yieldCurve = null)
		{
			if (_a < double.Epsilon * 100)
				return 0.0;

			double sigma2 = _sigma * _sigma;
			double bt = B(t, T);
			return Math.Exp((_b + _lambda * _sigma / _a
				- 0.5 * sigma2 / (_a * _a)) * (bt - (T - t))
				- 0.25 * sigma2 * bt * bt / _a);
		}

		public double B(double t, double T)
		{
			if (_a < double.Epsilon * 100)
				return T - t;

			return (1.0 - Math.Exp(-_a * (T - t))) / _a;
		}

		public double DiscountBondOption(OptionType type, double strike, double maturity, double bondMaturity)
		{
			double v;
			if (Math.Abs(maturity) < double.Epsilon * 100)
			{
				v = 0.0;
			}
			else if (_a < double.Epsilon * 100)
			{
				v = _sigma * B(maturity, bondMaturity) * Math.Sqrt(maturity);
			}
			else
			{
				v = _sigma * B(maturity, bondMaturity) * Math.Sqrt(0.5 * (1.0 - Math.Exp(-2.0 * _a * maturity)) / _a);
			}

			double f = DiscountBond(0.0, bondMaturity, _r0);
			double k = DiscountBond(0.0, maturity, _r0) * strike;

			return BlackFormula.BlackPrice(type, k, f, v);
		}

		public double Discount(double t)
		{
			throw new NotImplementedException();
		}

		public virtual double DiscountBond(double now, double maturity, double rate, IYieldCurve yieldCurve = null)
		{
			return A(now, maturity) * Math.Exp((-B(now, maturity) * rate));
		}

		public virtual NumericTree Tree(double[] times, double r0, IYieldCurve yieldCurve = null)
		{
			throw new NotImplementedException();
		}
	}
}
