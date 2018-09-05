using System.Collections.Generic;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes.ShortRate
{
	public abstract class Parameter
	{
		private readonly List<double> _params;

		protected Parameter()
		{
			_params = new List<double>();
		}

		public List<double> Params()
		{
			return _params;
		}

		public void SetParam(int i, double value)
		{
			_params[i] = value;
		}

		public double At(double t)
		{
			return Value(_params, t);
		}

		public abstract double Value(List<double> array, double t);
	}

	public class NullParameter: Parameter
	{
		public override double Value(List<double> array, double t)
		{
			return 0.0;
		}
	}
}
