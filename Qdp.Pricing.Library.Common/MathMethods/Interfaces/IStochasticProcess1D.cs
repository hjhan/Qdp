namespace Qdp.Pricing.Library.Common.MathMethods.Interfaces
{
	public interface IStochasticProcess1D
	{
		double Drift(double t, double x, double dt);
		double Diffusion(double t, double x);
		double Expectation(double t0, double x0, double dt);
		double StdDeviation(double t0, double x0, double dt);
		double Variance(double t0, double x0, double dt);
		double Evolve(double t0, double x0, double dt, double dw);
	}
}