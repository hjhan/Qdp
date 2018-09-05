using System;
using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Library.Common.MathMethods.Maths
{
	public interface IUnivariateFunction
	{
		double Value(double x, int changeIndex);
	}

	public static class BrentZero2<TFunc>
		where TFunc : IUnivariateFunction
	{
		private static TFunc _f;
        private const double defaultAccuracy = 1.0e-14;

        public static double DoSolve(TFunc f, double min, double max, int changeIndex, double accuracy = defaultAccuracy)
		{
			return DoSolve(f, min, max, min + 0.5D*(max - min), changeIndex, accuracy);
		}

        //essentially same as Apache Brent solver
		public static double DoSolve(TFunc f, double min, double max, double initial, int changeIndex, double accuracy = defaultAccuracy)
		{
			_f = f;
			if (min >= initial || max <= initial || min >= max)
			{
				string msg = string.Format("endpoints do not specify an interval: [{0}, {1}, {2}]", min, max, initial);
				throw new CalibrationException(msg);
			}
			// Return the initial guess if it is good enough.
			double yInitial = f.Value(initial, changeIndex);
			if (Math.Abs(yInitial) <= 1e-15)
			{
				return initial;
			}

			// Return the first endpoint if it is good enough.
			double yMin = f.Value(min, changeIndex);
			if (Math.Abs(yMin) <= 1e-15)
			{
				return min;
			}

			// Reduce interval if min and initial bracket the root.
			if (yInitial * yMin < 0)
			{
				return Brent(min, initial, yMin, yInitial, changeIndex, accuracy);
			}

			// Return the second endpoint if it is good enough.
			double yMax = f.Value(max, changeIndex);
			if (Math.Abs(yMax) <= 1e-15)
			{
				return max;
			}

			// Reduce interval if initial and max bracket the root.
			if (yInitial * yMax < 0)
			{
				return Brent(initial, max, yInitial, yMax, changeIndex, accuracy);
			}

			throw new CalibrationException(string.Format("Function values at endpoints do not have different signs, endpoints: [{0}, {1}], values: [{2}, {3}]", min, yMin, max, yMax));
		}

		private static double Brent(double lo, double hi,
			double fLo, double fHi, int changeIndex, double accuracy)
		{
			double a = lo;
			double fa = fLo;
			double b = hi;
			double fb = fHi;
			double c = a;
			double fc = fa;
			double d = b - a;
			double e = d;

            const double t = 1e-9;
            const double eps = 1e-20;  //machine precision

            while (true)
			{
				if (Math.Abs(fc) < Math.Abs(fb))
				{
					a = b;
					b = c;
					c = a;
					fa = fb;
					fb = fc;
					fc = fa;
				}

				double tol = 2*eps*Math.Abs(b) + t;
				double m = 0.5*(c - b);

                if (Math.Abs(m) <= tol || fb.Equals(0))
                {
					return b;
				}
				if (Math.Abs(e) < tol ||
				    Math.Abs(fa) <= Math.Abs(fb))
				{
					// Force bisection.
					d = m;
					e = d;
				}
				else
				{
					double s = fb/fa;
					double p;
					double q;
					// The equality test (a == c) is intentional,
					// it is part of the original Brent's method and
					// it should NOT be replaced by proximity test.
					if (a == c)
					{
						// Linear interpolation.
						p = 2*m*s;
						q = 1 - s;
					}
					else
					{
						// Inverse quadratic interpolation.
						q = fa/fc;
						double r = fb/fc;
						p = s*(2*m*q*(q - r) - (b - a)*(r - 1));
						q = (q - 1)*(r - 1)*(s - 1);
					}
					if (p > 0)
					{
						q = -q;
					}
					else
					{
						p = -p;
					}
					s = e;
					e = d;
					if (p >= 1.5 * m * q - Math.Abs(tol * q) ||
						p >= Math.Abs(0.5 * s * q))
					{
						// Inverse quadratic interpolation gives a value
						// in the wrong direction, or progress is slow.
						// Fall back to bisection.
						d = m;
						e = d;
					}
					else
					{
						d = p/q;
					}
				}
				a = b;
				fa = fb;

				if (Math.Abs(d) > tol)
				{
					b += d;
				}
				else if (m > 0)
				{
					b += tol;
				}
				else
				{
					b -= tol;
				}
				fb = _f.Value(b, changeIndex);
				if ((fb > 0 && fc > 0) ||
				    (fb <= 0 && fc <= 0))
				{
					c = a;
					fc = fa;
					d = b - a;
					e = d;
				}
			}
		}
	}
}
