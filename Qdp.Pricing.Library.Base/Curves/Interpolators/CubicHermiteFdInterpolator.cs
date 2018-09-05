using System;
using System.Linq;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace Qdp.Pricing.Library.Base.Curves.Interpolators
{
	public class CubicHermiteFdInterpolator : IInterpolator
	{
		private readonly Tuple<double, double>[] _keyPoints;
		private readonly double[] _firstOrderDerivatives;

		public CubicHermiteFdInterpolator(Tuple<double, double>[] keyPoint)
		{
			_keyPoints = keyPoint.OrderBy(x => x.Item1).ToArray();
			if (_keyPoints.Length < 2)
			{
				throw new PricingBaseException("Cubic Hermite Fd interpolar requires at least 2 points");
			}

			_firstOrderDerivatives = new double[_keyPoints.Length];
			var cDerivative = new double[_keyPoints.Length - 1];
			for (var i = 0; i < _keyPoints.Length - 1; ++i)
			{
				cDerivative[i] = (_keyPoints[i + 1].Item2 - _keyPoints[i].Item2)/(_keyPoints[i + 1].Item1 - _keyPoints[i].Item1);
			}
			_firstOrderDerivatives[0] = cDerivative[0];
			_firstOrderDerivatives[_keyPoints.Length - 1] = cDerivative.Last();
			for (var i = 1; i < _keyPoints.Length - 1; ++i)
			{
				_firstOrderDerivatives[i] = (cDerivative[i - 1] + cDerivative[i])/2;
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

			for (var i = 0; i < _keyPoints.Length; ++i)
			{
				if (x < _keyPoints[i + 1].Item1)
				{
					var dx = _keyPoints[i + 1].Item1 - _keyPoints[i].Item1;
					var t = (x - _keyPoints[i].Item1)/dx;
					var t2 = t*t;
					var t3 = t2*t;
					var h00 = 2*t3 - 3*t2 + 1;
					var h10 = t3 - 2*t2 + t;
					var h01 = -2*t3 + 3*t2;
					var h11 = t3 - t2;
					return h00*_keyPoints[i].Item2 + h10*dx*_firstOrderDerivatives[i] + h01*_keyPoints[i + 1].Item2 +
					       h11*dx*_firstOrderDerivatives[i + 1];
				}
			}

			return 0.0;
		}

		public double GetIntegral(double x)
		{
			throw new NotImplementedException();
		}
	}
}
