using System;
using System.Collections.Generic;
using Qdp.Pricing.Library.Common.MathMethods.Maths;

namespace Qdp.Pricing.Library.Common.Utilities
{
	public class FunctionVariable
	{
		internal class UnivariateFunction : IUnivariateFunction
		{
			private readonly double _x1;
			private readonly double _x2;
			private readonly double[] _x;
			private Dictionary<string, double> _result;

			private Func<Dictionary<string, double>, double[], double> _func;

			public UnivariateFunction(Dictionary<string, double> result, Func<Dictionary<string, double>, double[], double> func, params double[] x)
			{
				_func = func;
				_x = x;
				_result = result;
			}

			public double Value(double x, int index)
			{
				_x[index] = x;
				return _func(_result, _x);
			}
		}
	}
}
