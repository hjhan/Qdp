using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace Qdp.Pricing.Library.Base.Curves.Interpolators
{
	public class LogCubicInterpolator : IInterpolator
	{
		private readonly List<Tuple<double, double>> _keyPoints;	
		private readonly CubicHermiteMonotonicInterpolator _interpolator;
		
		public LogCubicInterpolator(IEnumerable<Tuple<double, double>> keyPoints, Extrapolation extrapolation = Extrapolation.Natural)
		{
			_keyPoints = keyPoints.OrderBy(x => x.Item1).ToList();

			_interpolator = new CubicHermiteMonotonicInterpolator(_keyPoints.Select(x => Tuple.Create(x.Item1, Math.Log(x.Item2))));			
		}

		public double GetValue(double x)
		{
			return Math.Exp(_interpolator.GetValue(x));
		}

		public double GetIntegral(double x)
		{
			throw new PricingBaseException("Integral of LogLinear interpolation is not implemented!");
		}
	}
}
