using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Library.Common.MathMethods.Interfaces
{
	public interface INumericCondition
	{
		double Apply(double price);
		double[] Apply(double[] prices);
	}

	public abstract class NumericCondition : INumericCondition
	{
		public virtual double Apply(double price)
		{
			throw new PricingLibraryException("This function shall not be called if not override in inherited class");
		}

		public virtual double[] Apply(double[] prices)
		{
			throw new PricingLibraryException("This function shall not be called if not override in inherited class");
		}
	}
}
 