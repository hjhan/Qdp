using System;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes
{
	/// <summary>
	/// Ornstein-Uhlenbeck process class.
	///		This class describes the Ornstein-Uhlenbeck process governed by dx = a (r - x_t) dt + \sigma dW_t.
	/// </summary>
	public class OrnsteinUhlenbeckProcess : StochasticProcess1D
	{
		public double X0 { get; private set; }
		public double Speed { get; private set; }
		public double Level { get; private set; }
		public double Volatility { get; private set; }

		public OrnsteinUhlenbeckProcess(double speed, double vol, double x0 = 0.0, double level = 0.0)
			: base()
		{
			X0 = x0;
			Speed = speed;
			Level = level;
			Volatility = vol;
		}


		public override double Drift(double t, double x, double dt)
		{
			return Speed * (Level - x);
		}

		public override double Diffusion(double t, double x)
		{
			return Volatility;
		}

		public override double Expectation(double t, double x0, double dt)
		{
			return Level + (x0 - Level) * Math.Exp(-Speed * dt);
		}

		public override double StdDeviation(double t, double x0, double dt)
		{
			return Math.Sqrt(Variance(t, x0, dt));
		}

		public override double Variance(double t, double x0, double dt)
		{
			if (Speed.IsAlmostZero())
			{
				// algebraic limit for small speed
				return Volatility * Volatility * dt;
			}
			else
			{
				//return Volatility * Volatility * dt;
				return 0.5 * Volatility * Volatility / Speed * (1.0 - Math.Exp(-2.0 * Speed * dt));
			}
		}

	}
}





