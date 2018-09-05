using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Library.Base.Curves.Interpolators
{
	public class LogLinearInterpolator : IInterpolator
	{
		private readonly List<Tuple<double, double>> _keyPoints;		
		private readonly Extrapolation _extrapolation;

		private readonly double[] _slopes;
		public LogLinearInterpolator(IEnumerable<Tuple<double, double>> keyPoints, Extrapolation extrapolation = Extrapolation.Natural)
		{
			_keyPoints = keyPoints.OrderBy(x => x.Item1).ToList();
			_extrapolation = extrapolation;
			if (!_keyPoints.Any())
			{
				throw new PricingLibraryException("Loglinear interpolation requires at least 1 point");
			}
			_slopes = new double[_keyPoints.Count - 1];
			for (var i = 0; i < _slopes.Length; ++i)
			{
				_slopes[i] = (Math.Log(_keyPoints[i + 1].Item2) - Math.Log(_keyPoints[i].Item2))/(_keyPoints[i + 1].Item1 - _keyPoints[i].Item1);
			}
		}

		public double GetValue(double x)
		{
			if (x < _keyPoints.First().Item1)
			{
				if (_extrapolation == Extrapolation.Natural)
				{
					return Math.Exp(Math.Log(_keyPoints.First().Item2) + _slopes[0]*(x - _keyPoints.First().Item1));
				}
				else
				{
					return _keyPoints.First().Item2;
				}
			}
			if (x >= _keyPoints.Last().Item1)
			{
				if (_extrapolation == Extrapolation.Natural)
				{
					return Math.Exp(Math.Log(_keyPoints.Last().Item2) + _slopes.Last()*(x - _keyPoints.Last().Item1));
				}
				else
				{
					return _keyPoints.Last().Item2;
				}
			}

			for (var i = 0; i < _keyPoints.Count - 1; ++i)
			{
				if (x < _keyPoints[i + 1].Item1)
				{
					return Math.Exp(Math.Log(_keyPoints[i].Item2) + _slopes[i]*(x - _keyPoints[i].Item1));
				}
			}

			return 0;
		}

		public double GetIntegral(double x)
		{
			throw new PricingBaseException("Integral of LogLinear interpolation is not implemented!");
		}
	}
}
