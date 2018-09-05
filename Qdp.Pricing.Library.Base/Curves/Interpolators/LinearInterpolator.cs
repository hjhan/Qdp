using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Library.Base.Curves.Interpolators
{
	public class LinearInterpolator : IInterpolator
	{
		private readonly List<Tuple<double, double>> _keyPoints;
		private readonly Extrapolation _extrapolation;

		private readonly List<double> _integrals;
		public LinearInterpolator(Tuple<double, double>[] keyPoints,
			Extrapolation extrapolation = Extrapolation.Flat)
		{
			_keyPoints = keyPoints.ToList();
			if (_keyPoints.Count < 1)
			{
				throw new PricingLibraryException("LinearInterpolator must have at least 1 point");
			}
			_extrapolation = extrapolation;
			_integrals = new List<double> {0.0};
			for (var i = 1; i < _keyPoints.Count; ++i)
			{
				var dx = _keyPoints[i].Item1 - _keyPoints[i - 1].Item1;
				var slope = (_keyPoints[i].Item2 - _keyPoints[i - 1].Item2)/dx;
				_integrals.Add(_integrals[i - 1] + dx*(_keyPoints[i - 1].Item2 + 0.5*dx*slope));
			}
		}

		public double GetValue(double x)
		{
			if (x < _keyPoints.First().Item1)
			{
				return _keyPoints.First().Item2;
			}
			if (x >= _keyPoints.Last().Item1)
			{
				return _keyPoints.Last().Item2;
			}

			for (var i = 0; i < _keyPoints.Count - 1; ++i)
			{
				if (x < _keyPoints[i + 1].Item1)
				{
					var slope = (_keyPoints[i + 1].Item2 - _keyPoints[i].Item2)/(_keyPoints[i + 1].Item1 - _keyPoints[i].Item1);
					return _keyPoints[i].Item2 + slope*(x - _keyPoints[i].Item1);
				}
			}

			return 0;
		}

		public double GetIntegral(double x)
		{
			var arrX = _keyPoints.Select(p => p.Item1).ToArray();
			var i = arrX.Locate(x);
			var dx = x - _keyPoints[i].Item1;
			var slope = (_keyPoints[i + 1].Item2 - _keyPoints[i].Item2)/(_keyPoints[i + 1].Item1 - _keyPoints[i].Item1);
			return _integrals[i] + dx*(_keyPoints[i].Item2 + 0.5 * dx * slope);
		}
	}
}
