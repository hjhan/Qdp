using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Library.Base.Curves.Interpolators
{
	//Interpolator for forward curve
	//If this interpolator is used, a continuous compound forward curve will be built
	public class ForwardFlatInterpolator : IInterpolator
	{
		private readonly List<Tuple<double, double>> _keyPoints;

		private readonly List<double> _integrals;
		public ForwardFlatInterpolator(Tuple<double, double>[] keyPoints)
		{
			_keyPoints = keyPoints.ToList();
			if (_keyPoints.Count < 1)
			{
				throw new PricingLibraryException("LinearInterpolator must have at least 1 point");
			}
			_integrals = new List<double>();
			var integral = 0.0;

			for (var i = 0; i < keyPoints.Length; ++i)
			{
				var dx = i ==0 ? keyPoints[i].Item1 : (keyPoints[i].Item1-keyPoints[i-1].Item1);
				var f = i == 0 ? keyPoints[i].Item2 : keyPoints[i - 1].Item2;
				integral += f*dx;
				_integrals.Add(integral);
			}
		}

		public double GetValue(double x)
		{
			if (x < _keyPoints.First().Item1)
			{
				return 0.0;
			}
			if (x >= _keyPoints.Last().Item1)
			{
				return _keyPoints.Last().Item2;
			}

			for (var i = 0; i < _keyPoints.Count - 1; ++i)
			{
				if (x < _keyPoints[i + 1].Item1)
				{
					return _keyPoints[i].Item2;
				}
			}

			return 0;
		}

		public double GetIntegral(double x)
		{
			if (x < _keyPoints.First().Item1)
			{
				return 0.0;
			}
			if (x >= _keyPoints.Last().Item1)
			{
				return _integrals.Last() + (x - _keyPoints.Last().Item1) * _keyPoints.Last().Item2;
			}

			for (var i = 0; i < _keyPoints.Count - 1; ++i)
			{
				if (x < _keyPoints[i + 1].Item1)
				{
					return _integrals[i] + (x - _keyPoints[i].Item1) * _keyPoints[i].Item2;
				}
			}

			return 0;
		}
	}
}
