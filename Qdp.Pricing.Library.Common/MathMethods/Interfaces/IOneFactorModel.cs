using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Processes.Trees;

namespace Qdp.Pricing.Library.Common.MathMethods.Interfaces
{
	/// <summary>
	/// Single-factor affine base class
	///		Single-factor models with an analytical formula for discount bonds should inherit from this class. They must then implement the functions \f$ A(t,T) \f$ and \f$ B(t,T) \f$ such that
	///		P(t, T, r_t) = A(t,T)e^{ -B(t,T) r_t}.
	/// </summary>
	public interface IOneFactorModel
	{
		double A(double t, double T, IYieldCurve yieldCurve = null);
		double B(double t, double T);
		double Discount(double t);
		double DiscountBond(double now, double maturity, double rate, IYieldCurve yieldCurve = null);
		NumericTree Tree(double[] times, double r0, IYieldCurve yieldCurve = null);
	}
}
