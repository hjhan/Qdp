namespace Qdp.Pricing.Library.Base.Curves.Interfaces
{
	public interface IInterpolator
	{
		double GetValue(double x);
		double GetIntegral(double x);
	}

	public interface IInterpolator2D
	{
		double GetValue(double x, double y);
	}
}
