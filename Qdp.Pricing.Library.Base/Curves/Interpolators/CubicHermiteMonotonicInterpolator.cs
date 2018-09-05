using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace Qdp.Pricing.Library.Base.Curves.Interpolators
{
	/// <summary>
	/// Cubic hermite interpolation with monotone-adjusted slopes
	/// </summary>
	public class CubicHermiteMonotonicInterpolator : IInterpolator
	{
		private readonly double[] _a;
		private readonly double[] _b;
		private readonly double[] _c;
		private readonly Tuple<double, double>[] _keyPoints;

		public CubicHermiteMonotonicInterpolator(IEnumerable<Tuple<double, double>> points)
		{
			_keyPoints = points.ToArray();
			if (_keyPoints.Length < 2)
			{
				throw new ArgumentException("CubicHermiteMonotonicInterpolator needs 2 points at least.");
			}
			var n = _keyPoints.Length;
			_a = new double[n - 1];
			_b = new double[n - 1];
			_c = new double[n - 1];
			var primitiveConst = new double[n - 1].Select(e => 0.0).ToArray();

			var dx = new double[n - 1];
			var s = new double[n - 1];
			for (var i = 0; i < n - 1; ++i)
			{
				dx[i] = _keyPoints[i + 1].Item1 - _keyPoints[i].Item1;
				s[i] = (_keyPoints[i + 1].Item2 - _keyPoints[i].Item2)/dx[i];
			}

			var tmp = Slope(_keyPoints.Select(x => x.Item1).ToArray(), n, s);
			var monotonicityAdjustment = new bool[n].Select(b => false).ToArray();

			for (var i = 0; i < n; ++i)
			{
				double correction;
				if (i == 0)
				{
					if (tmp[i]*s[0] > 0.0)
					{
						correction = tmp[i]/Math.Abs(tmp[i])*Math.Min(Math.Abs(tmp[i]),Math.Abs(3.0*s[0]));
					}
					else
					{
						correction = 0.0;
					}
					if (correction != tmp[i])
					{
						tmp[i] = correction;
						monotonicityAdjustment[i] = true;
					}
				}
				else if (i == n - 1)
				{
					if (tmp[i]*s[n - 2] > 0.0)
					{
						correction = tmp[i]/Math.Abs(tmp[i])*Math.Min(Math.Abs(tmp[i]), Math.Abs(3.0*s[n - 2]));
					}
					else
					{
						correction = 0.0;
					}
					if (correction != tmp[i])
					{
						tmp[i] = correction;
						monotonicityAdjustment[i] = true;
					}
				}
				else
				{
					var pm = (s[i - 1]*dx[i] + s[i]*dx[i - 1])/(dx[i - 1] + dx[i]);
					var M = 3.0*Math.Min(Math.Min(Math.Abs(s[i - 1]), Math.Abs(s[i])), Math.Abs(pm));
					if (i > 1)
					{
						if ((s[i - 1] - s[i - 2])*(s[i] - s[i - 1]) > 0.0)
						{
							var pd = (s[i - 1]*(2.0*dx[i - 1] + dx[i - 2]) - s[i - 2]*dx[i - 1])/(dx[i - 2] + dx[i - 1]);
							if (pm*pd > 0.0 && pm*(s[i - 1] - s[i - 2]) > 0.0)
							{
								M = Math.Max(M, 1.5*Math.Min(Math.Abs(pm), Math.Abs(pd)));
							}
						}
					}
					if (i < n - 2)
					{
						if ((s[i] - s[i - 1])*(s[i + 1] - s[i]) > 0.0)
						{
							var pu = (s[i]*(2.0*dx[i] + dx[i + 1]) - s[i + 1]*dx[i])/(dx[i] + dx[i + 1]);
							if (pm*pu > 0.0 && -pm*(s[i] - s[i - 1]) > 0.0)
							{
								M = Math.Max(M, 1.5*Math.Min(Math.Abs(pm), Math.Abs(pu)));
							}
						}
					}
					if (tmp[i]*pm > 0.0)
					{
						correction = tmp[i]/Math.Abs(tmp[i])*Math.Min(Math.Abs(tmp[i]), M);
					}
					else
					{
						correction = 0.0;
					}
					if (correction != tmp[i])
					{
						tmp[i] = correction;
						monotonicityAdjustment[i] = true;
					}
				}
			}

			for (var i = 0; i < n - 1; ++i)
			{
				_c[i] = -2 * (_keyPoints[i + 1].Item2 - _keyPoints[i].Item2) / Math.Pow(_keyPoints[i+1].Item1 - _keyPoints[i].Item1, 3)
						+ (tmp[i] + tmp[i + 1]) / Math.Pow(_keyPoints[i + 1].Item1 - _keyPoints[i].Item1, 2);

				_a[i] = tmp[i];

				_b[i] = 3 * (_keyPoints[i + 1].Item2 - _keyPoints[i].Item2) / Math.Pow(_keyPoints[i + 1].Item1 - _keyPoints[i].Item1, 2)
						- (2 * tmp[i] + tmp[i + 1]) / (_keyPoints[i + 1].Item1 - _keyPoints[i].Item1);
			}

			for (var i = 1; i < n - 1; ++i)
			{
				primitiveConst[i] = primitiveConst[i - 1] + dx[i - 1] *
					(_keyPoints[i-1].Item2 + dx[i - 1] * (_a[i - 1] / 2.0 + dx[i - 1] * (_b[i - 1] / 3.0 + dx[i - 1] * _c[i - 1] / 4.0)));
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

			for (var i = 0; i < _keyPoints.Length - 1; ++i)
			{
				if (x < _keyPoints[i + 1].Item1)
				{
					var dx = x - _keyPoints[i].Item1;
					return _keyPoints[i].Item2 + dx * (_a[i] + dx * (_b[i] + dx * _c[i]));
				}
			}

			return 0.0;
		}

		public double GetIntegral(double x)
		{
			throw new NotImplementedException();
		}

		private int Sign(double value)
		{
			return value.IsAlmostZero() ? 0 : value < 0 ? -1 : 1;
		}

		private double[] Slope(double[] x, int length, double[] del)
		{
			if (length == 2)
			{
				return new double[] {del[0], del[0]};
			}
			
			var sameDirection = new List<int>();
			for (var i = 0; i < length - 2; ++i)
			{
				if (Sign(del[i]) * Sign(del[i + 1]) > 0)
				{
					sameDirection.Add(i);
				}
			}

			var h = new double[length - 1];
			for (var i = 0; i < length - 1; ++i) 
			{
				h[i] = x[i + 1] - x[i];
			}

			var len = sameDirection.Count;
			var temp = new double[len];
			for (var i = 0; i < len; ++i)
			{
				var k = sameDirection[i];
				var hk = h[k];
				var hk1 = h[k+1];

				var delk = del[k];
				var delk1 = del[k + 1];
					 
				temp[i] = (3 * (hk + hk1) * delk * delk1 / ((2 * hk + hk1) * delk + (hk + 2 * hk1) * delk1));
			}

			var ret = new double[length].Select(e => 0.0).ToArray();
			for (var i = 0; i < len; ++i)
			{
				ret[sameDirection[i] + 1] = temp[i];
			}
			ret[0] = ((2*h[0] + h[1])*del[0] - (h[0]*del[1]))/(h[0] + h[1]);
			
			if (Sign(ret[0]) != Sign(del[0]))
			{
				ret[0] = 0.0;
			}
			else if ((Sign(del[0]) != Sign(del[1])) && System.Math.Abs(ret[0]) > System.Math.Abs(3 * del[0]))
			{
				ret[0] = 3 * del[0];
			}

			var n = length - 1;
			ret[n] = ((2 * h[n - 1] + h[n - 2]) * del[n - 1] - h[n - 1] * del[n - 2]) / (h[n - 1] + h[n - 2]);

			if (Sign(ret[n]) != Sign(del[n - 1]))
			{
				ret[n] = 0.0;
			}
			else if ((Sign(del[n - 1]) != Sign(del[n - 2])) && (System.Math.Abs(ret[n]) > System.Math.Abs(3 * del[n - 1])))
			{
				ret[n] = 3 * del[n - 1];
			}

			return ret;
		}
	}
}
