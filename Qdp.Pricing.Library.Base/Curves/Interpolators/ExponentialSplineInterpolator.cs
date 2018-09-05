using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace Qdp.Pricing.Library.Base.Curves.Interpolators
{
	//used to fit discount curve only
	public class ExponentialSplineInterpolator : IInterpolator
	{
		private readonly Tuple<double, double>[] _keyPoints;

		// 0 alpha
		// 0 b1
		// 0 b2
		// 0 b3
		// t1 b4
		// t2 b5
		public ExponentialSplineInterpolator(IEnumerable<Tuple<double, double>> keyPoints)
		{
			_keyPoints = keyPoints.ToArray();
		}

		//return df
		public double GetValue(double x)
		{
			if (x <= 0)
			{
				return 1.0;
			}
			var variables = _keyPoints.Select(p => p.Item2).ToArray();
			var keyTs = _keyPoints.Select(p => p.Item1).ToArray();

			var f = Math.Exp(-variables[0] * x);
			var df = 1 + variables[1] * (1 - f) + variables[2] * (1 - f * f) + variables[3] * (1 - f * f * f);

			for (var i = 4; i < keyTs.Length; ++i)
			{
				if (x >= keyTs[i])
				{
					var tmpF = Math.Exp(-variables[0] * (x - keyTs[i]));
					df += variables[i] * ((1 - tmpF) - (1 - tmpF * tmpF) + 1 / 3.0 * (1 - tmpF * tmpF * tmpF));
				}
			}

			return df;
		}

		public double GetIntegral(double x)
		{
			throw new NotImplementedException();
		}
	}
}
