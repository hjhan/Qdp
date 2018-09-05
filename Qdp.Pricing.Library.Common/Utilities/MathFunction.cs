using System;

namespace Qdp.Pricing.Library.Common.Utilities
{
	public static class MathFunction
	{
		public static double InfiniteFunction(Func<int, double[], double> func, double[] paras, double deviation)
		{
			int n = 1;
			var before = 0.0;
			var sum = 0.0;
			while (true)
			{
				var result = func(n, paras);
				sum += result;
				if (n != 1 && Math.Abs(result - before) <= deviation)
				{
					break;
				}
				before = result;
				n++;
			}

			return sum;
		}
	}
}
