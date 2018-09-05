using System;
using Qdp.Pricing.Library.Common.MathMethods.Interfaces;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes
{
	public abstract class StochasticProcess1D : IStochasticProcess1D
	{
		protected StochasticProcess1D()
		{
		
		}

		public abstract double Drift(double t, double x, double dt);
		public abstract double Diffusion(double t, double x);

		public virtual double Evolve(double t0, double x0, double dt, double dw)
		{
			return Expectation(t0, x0, dt) + StdDeviation(t0, x0, dt) * dw;
		}

		public virtual double Expectation(double t0, double x0, double dt)
		{
			return x0 + Drift(t0, x0, dt)*dt;
		}

		public virtual double StdDeviation(double t0, double x0, double dt)
		{
			return Diffusion(t0, x0)*Math.Sqrt(dt);
		}

		public virtual double Variance(double t0, double x0, double dt)
		{
			return Math.Pow(Diffusion(t0, x0), 2.0)* dt;
		}
	}
}
