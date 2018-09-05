using System;
using System.Collections.Generic;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Processes.Trees;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes.ShortRate
{
	/// <summary>
	/// Single-factor Hull-White (extended %Vasicek) model class.
	///		This class implements the standard single-factor Hull-White model	defined by
	///			dr_t = (\theta(t) - \alpha r_t)dt + \sigma dW_t
	///			, where \f$ \alpha \f$ and \f$ \sigma \f$ are constants.
	/// </summary>
	public class HullWhite : Vasicek
	{
		private readonly double _a;
		private readonly double _sigma;

		private readonly Dynamics _dynamics;

		public HullWhite(double a = 0.1, double sigma = 0.01)
			: base(0.0, a, 0.0, sigma, 0.0)
		{
			_a = a;
			
			_sigma = sigma;
			if (Math.Abs(_sigma) < Double.Epsilon)
			{
				_sigma = 1.0e-12;
			}
		}

		/// <summary>
		/// Build a trinomial tree for given yield curve and time grids.
		/// For better pricing, one tree shall be rebuilt for each trade because of different coupon time and call/put dates.
		/// </summary>
		/// <param name="times">The times.</param>
		/// <param name="r0">The r0.</param>
		/// <param name="yieldCurve">The yield curve.</param>
		/// <returns></returns>
		public override NumericTree Tree(double[] times, double r0, IYieldCurve yieldCurve = null)
		{
			var tree = new TrinomialTree(new OrnsteinUhlenbeckProcess(_a, _sigma), times, 0);
			return new NumericTree(this, tree, yieldCurve);
		}

		public double A(double t, double T, IYieldCurve yieldCurve)
		{
			var df1 = yieldCurve.GetDf(t);
			var df2 = yieldCurve.GetDf(T);
			var forward = yieldCurve.GetInstantaneousForwardRate(t, Compound.Continuous);

			var temp = _sigma*B(t, T);
			var value = B(t, T)*forward - 0.25*temp*temp*B(0.0, 2.0*t);
			return Math.Exp(value)*df2/df1;
		}

		public override double DiscountBond(double now, double maturity, double rate, IYieldCurve yieldCurve = null)
		{
			return A(now, maturity, yieldCurve) * Math.Exp((-B(now, maturity) * rate));
		}
	}

	/// <summary>
	/// Analytical term-structure fitting parameter \f$ \varphi(t) \f$.
	/// \f$ \varphi(t) \f$ is analytically defined by
	///				\varphi(t) = f(t) + \frac{1}{2}[\frac{\sigma(1-e^{-at})}{a}]^2,
	///		, where \f$ f(t) \f$ is the instantaneous forward rate at \f$ t \f$.
	/// </summary>
	internal class FittingParameter : Parameter
	{
		private readonly IYieldCurve _yieldCurve;
		private readonly double _a;
		private readonly double _sigma;

		public FittingParameter(IYieldCurve yieldCurve, double a, double sigma)
		{
			_yieldCurve = yieldCurve;
			_a = a;
			_sigma = sigma;
		}

		public override double Value(List<double> array, double t)
		{
			var forwardRate = _yieldCurve.GetInstantaneousForwardRate(t);
			var temp = _a.IsAlmostZero()
				? _sigma * t
				: _sigma * (1.0 - Math.Exp(-_a * t)) / _a;
			return (forwardRate + 0.5 * temp * temp);
		}
	}

	/// <summary>
	/// Short-rate dynamics in the Hull-White model
	///		The short-rate is here r_t = \varphi(t) + x_t
	///		, where \f$ \varphi(t) \f$ is the deterministic time-dependent parameter used for term-structure fitting and \f$ x_t \f$ is the state variable following an Ornstein-Uhlenbeck process.
	/// </summary>
	internal class Dynamics
	{
		public StochasticProcess1D Process { get; private set; }
		public Parameter Fitting { get; private set; }

		public Dynamics(Parameter fitting, double a, double sigma)
		{
			Process = new OrnsteinUhlenbeckProcess(a, sigma);
			Fitting = fitting;
		}

		public double Variable(double t, double r)
		{
			return r - Fitting.At(t);
		}

		public double ShortRate(double t, double x)
		{
			return x + Fitting.At(t);
		}
	}


};







