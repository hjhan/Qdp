using System;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Library.Common.MathMethods.Maths
{
	public interface IFunctionOfOneVarialbe
	{
		double F(double x);
	}
	/// <summary>
	/// Solve one variable equation f(x)=0 using Brent's method.
	/// http://en.wikipedia.org/wiki/Brent%27s_method
	/// Code adapted from John D. Cook's implementation, 
	/// see http://www.codeproject.com/Articles/79541/Three-Methods-for-Root-finding-in-C
	/// </summary>
	public static class BrentZero
	{
		const int MaxIterations = 50;

        // to make it similar to IMSL.ZeroFunction.IFunction
        // solve f(x) = 0 
        public static double Solve 
		(
			IFunctionOfOneVarialbe f,
			double left,
			double right,
			double tolerance = 1e-6
		)
		{
            // extra info that callers may not always want
            int iterationsUsed;
            double errorEstimate;
            return Solve(f, left, right, tolerance, out iterationsUsed, out errorEstimate);

            //Bad
            //return SolveGolden(f, left, right, atol: tolerance * 1e-4, rtol: tolerance * 1e-4, 
            //    iterationsUsed: out iterationsUsed, errorEstimate: out errorEstimate);

            //same result, but slower convergence
            //return SolveJin(f, left, right, tolerance * 1e-4, tolerance);

        }

        //Note: this worst solver,  bugs inside to fix
        //https://github.com/limix/brent-search/blob/master/brent_search/brent.py
        public static double SolveGolden(
            IFunctionOfOneVarialbe f,
             double a,
             double b,
             double rtol,
             double atol,
             out int iterationsUsed,
             out double errorEstimate,
             int MaxIter = MaxIterations
            ) {

            var _golden = 0.381966011250105097;
            var x0 = a + _golden * (b - a);
            var fa = f.F(a);
            var fb = f.F(b);
            var f0 = f.F(x0);

            var x1 = x0;
            var x2 = x1;
            int niters = -1;
            var d = 0.0;
            var e = 0.0;
            var f1 = f0;
            var f2 = f1;

            double m, tol, tol2;
            double r, q, p, u, fu;
            for (; niters < MaxIter; niters++) {
                m = 0.5*(a + b);
                tol = rtol * Math.Abs(x0) + atol;
                tol2 = 2.0 * tol;

                if (Math.Abs(x0 - m) <= tol2 - 0.5 * (b-a))
                    break;

                r = 0.0;
                q = r;
                p = q;

                if (tol < Math.Abs(e))
                {
                    r = (x0 - x1) * (f0 - f2);
                    q = (x0 - x2) * (f0 - f1);
                    p = (x0 - x2) * q - (x0 - x1) * r;
                    q = 2.0 * (q - r);
                    if (0.0 < q)
                        p = -p;
                    q = Math.Abs(q);
                    r = e;
                    e = d;
                }

                if ((Math.Abs(p) < Math.Abs(0.5 * q * r)) &&
                    (q * (a - x0) < p) &&
                    (p < q * (b - x0)))
                {
                    d = p / q;
                    u = x0 + d;

                    if ((u - a) < tol2 || (b - u) < tol2)
                    {
                        if (x0 < m)
                            d = tol;
                        else
                            d = -tol;
                    }
                }
                else {
                    if (x0 < m)
                        e = b - x0;
                    else
                        e = a - x0;

                    d = _golden * e;
                }

                if (tol <= Math.Abs(e))
                    u = x0 + d;
                else if (0.0 < d) {
                    u = x0 + tol;
                }else
                    u = x0 - tol;

                fu = f.F(u);

                //House keeping
                if (fu <= f0)
                {
                    if (u < x0)
                    {
                        if (b != x0)
                        {
                            b = x0;
                            fb = f0;
                        }
                    }
                    else
                    {
                        if (a != x0)
                        {
                            a = x0;
                            fa = f0;
                        }
                    }
                    x2 = x1;
                    f2 = f1;
                    x1 = x0;
                    f1 = f0;
                    x0 = u;
                    f0 = fu;
                }
                else {
                    if (u < x0)
                    {
                        if (a != u)
                        {
                            a = u;
                            fa = fu;
                        }
                    }
                    else {
                        if (b != u)
                        {
                            b = u;
                            fb = fu;
                        }
                    }

                    if (fu < f1 || (x1 == x0))
                    {
                        x2 = x1;
                        f2 = f1;
                        x1 = u;
                        f1 = fu;
                    }
                    else if ((f1 <= f2) || (x2 == x0) || (x2 == x1)) {
                        x2 = u;
                        f2 = fu;
                    }
                }

            }
            iterationsUsed = niters + 1;
            errorEstimate = f0;
            return x0;
        }

        //Note: This is to find global min, worse than existing Brent
        //same as ApacheBrent
        //http://blog.mmast.net/brent-julia
        public static double SolveJin(
             IFunctionOfOneVarialbe f,
             double x0,
             double x1,
             double xtol,
             double ytol,
             out int iterationsUsed,
             out double errorEstimate,
             int MaxIter = 500
            ) {
            const double EPS = Double.Epsilon;
            var y0 = f.F(x0);
            var y1 = f.F(x1);
            if (Math.Abs(y0) < Math.Abs(y1)) {
                Swap(ref x0, ref x1);
                Swap(ref y0, ref y1);
            }
            var x2 = x0;
            var y2 = y0;
            var x3 = x2;

            var bisection = true;
            double x;
            double y;
            double delta;
            double min1, min2, min3;

            for (int i = 1; i <= MaxIter; i++) {
                iterationsUsed = i;
                errorEstimate = y1;
                if (Math.Abs(x1 - x0) < xtol)
                    return x1;

                if (Math.Abs(y0 - y2) > ytol && Math.Abs(y1 - y2) > ytol)
                    x = x0 * y1 * y2 / ((y0 - y1) * (y0 - y2)) +
                        x1 * y0 * y2 / ((y1 - y0) * (y1 - y2)) +
                        x2 * y0 * y1 / ((y2 - y0) * (y2 - y1));
                else
                    x = x1 - y1 * (x1 - x0) / (y1 - y0);

                delta = Math.Abs(2.0 * EPS * Math.Abs(x1));
                min1 = Math.Abs(x - x1);
                min2 = Math.Abs(x1 - x2);
                min3 = Math.Abs(x2 - x3);
                if ((x < (3*x0 + x1) / 4.0) && (x > x1) ||
                    (bisection && (min1 >= min2 / 2.0)) ||
                    (!bisection && (min1 >= min3 / 2.0)) ||
                    (bisection && (min2 < delta)) ||
                    (!bisection && (min3 < delta)))
                {
                    x = (x0 + x1) / 2.0;
                    bisection = true;
                }
                else
                    bisection = false;


                y = f.F(x);
                errorEstimate = y;
                if (Math.Abs(y) < ytol)
                    return x;

                x3 = x2;
                x2 = x1;

                if (Math.Sign(y0) != Math.Sign(y))
                {
                    x1 = x;
                    y1 = y;
                }
                else {
                    x0 = x;
                    y0 = y;
                }

                if (Math.Abs(y0) < Math.Abs(y1)) {
                    Swap(ref x0, ref x1);
                    Swap(ref y0, ref y1);
                }

            }
            throw new PricingBaseException("Max iteration exceeded. Failed to solve");
        }

        private static void Swap(ref double x0, ref double x1) {
            double temp = x0;
            x0 = x1;
            x1 = temp;
        }

		// solve f(x) = 0 
		public static double Solve 
		(
			IFunctionOfOneVarialbe f,
			double left,
			double right,
			double tolerance,
			out int iterationsUsed,
			out double errorEstimate
		)
		{
			if (tolerance <= 0.0)
			{
				string msg = string.Format("Tolerance must be positive. Recieved {0}.", tolerance);
				throw new CalibrationException(msg);
			}

			errorEstimate = double.MaxValue;

			// Standardize the problem.  To solve f(x) = 0,

			// Implementation and notation based on Chapter 4 in
			// "Algorithms for Minimization without Derivatives"
			// by Richard Brent.

			double e;
			double m;

			// set up aliases to match Brent's notation
			var a = left; 
			var b = right; 
			var t = tolerance;
			iterationsUsed = 0;

			var fa = f.F(a);
			var fb = f.F(b);

			if (fa * fb > 0.0)
			{
				const string str = "Invalid starting bracket. Function must be  positive on one end and negative on the other end.";
				var msg = string.Format("{0} :  f(left) = {1}. f(right) = {2}", str, fa, fb);
				throw new CalibrationException(msg);
			}

		label_int:
			var c = a; var fc = fa; var d = e = b - a;
		label_ext:
			if (Math.Abs(fc) < Math.Abs(fb))
			{
				a = b; b = c; c = a;
				fa = fb; fb = fc; fc = fa;
			}

			iterationsUsed++;

			var tol = 2.0 * t * Math.Abs(b) + t;
			errorEstimate = m = 0.5 * (c - b);
			if (Math.Abs(m) > tol && fb != 0.0) // exact comparison with 0 is OK here
			{
				// See if bisection is forced
				if (Math.Abs(e) < tol || Math.Abs(fa) <= Math.Abs(fb))
				{
					d = e = m;
				}
				else
				{
					var s = fb / fa;
					double p;
					double q;
					if (a == c)
					{
						// linear interpolation
						p = 2.0 * m * s; q = 1.0 - s;
					}
					else
					{
						// Inverse quadratic interpolation
						q = fa / fc; var r = fb / fc;
						p = s * (2.0 * m * q * (q - r) - (b - a) * (r - 1.0));
						q = (q - 1.0) * (r - 1.0) * (s - 1.0);
					}
					if (p > 0.0)
						q = -q;
					else
						p = -p;
					s = e; e = d;
					if (2.0 * p < 3.0 * m * q - Math.Abs(tol * q) && p < Math.Abs(0.5 * s * q))
						d = p / q;
					else
						d = e = m;
				}
				a = b; fa = fb;
				if (Math.Abs(d) > tol)
					b += d;
				else if (m > 0.0)
					b += tol;
				else
					b -= tol;
				if (iterationsUsed == MaxIterations)
					return b;

				fb = f.F(b);
				if ((fb > 0.0 && fc > 0.0) || (fb <= 0.0 && fc <= 0.0))
					goto label_int;
				else
					goto label_ext;
			}
			else
				return b;
		}
	}
}
