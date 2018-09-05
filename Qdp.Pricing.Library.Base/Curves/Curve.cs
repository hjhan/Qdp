using System;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Base.Curves.Interpolators;

namespace Qdp.Pricing.Library.Base.Curves
{
	public class Curve<TX> : ICurve<TX>
	{
		public TX Start { get; private set; }
		public Tuple<TX, double>[] KeyPoints { get; private set; }

		private readonly Func<TX, double> _x2DoubleFunc;
		private readonly IInterpolator _interpolator;

		public Curve(TX start, Tuple<TX, double>[] keyPoints, Func<TX, double> x2DoubleFunc, Interpolation interpolation)
		{
			Start = start;
			KeyPoints = keyPoints;
			_x2DoubleFunc = x2DoubleFunc;
			_interpolator = interpolation.GetInterpolator(keyPoints.Select(x => Tuple.Create(_x2DoubleFunc(x.Item1), x.Item2)).ToArray());
		} 
		public double GetValue(TX x)
		{
			return _interpolator.GetValue(_x2DoubleFunc(x));
		}

		public double GetIntegral(TX x)
		{
			return _interpolator.GetIntegral(_x2DoubleFunc(x));
		}
	}
}
