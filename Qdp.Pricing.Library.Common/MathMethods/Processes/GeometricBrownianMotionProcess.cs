namespace Qdp.Pricing.Library.Common.MathMethods.Processes
{
	/// <summary>
	/// Geometric brownian-motion process
	///		This class describes the stochastic process governed by dS(t, S)= \mu S dt + \sigma S dW_t.
	/// </summary>
	public class GeometricBrownianMotionProcess : StochasticProcess1D
	{
		public double X0 { get; private set; }
		private readonly double _mu;
		private readonly double _sigma;

		public GeometricBrownianMotionProcess(double x0, double mu, double sigma)
			: base()
		{
			X0 = x0;
			_mu = mu;
			_sigma = sigma;
		}

		public override double Drift(double t, double x, double dt)
		{
			return _mu * x;
		}

		public override double Diffusion(double t, double x)
		{
			return _sigma * x;
		}
	}
}
