using System;
using System.Runtime.InteropServices;

namespace Qdp.Pricing.Base.Utilities
{
	

	public static class DoubleExtension
	{
		private static double _epsilon = 1e-8;
		public static bool IsAlmostZero(this double d)
		{
			return Math.Abs(d) < _epsilon;
		}

		public static bool AlmostEqual(this double d, double anotherD)
		{
			return Math.Abs(d - anotherD) < _epsilon;
		}
		public static double Round(this double value, int digit)
		{
			var digits = Convert.ToDouble("1e-" + digit);
			var parseInt = value / digits;
			var roundResult = Math.Round(parseInt, 0, MidpointRounding.AwayFromZero);
			return roundResult * digits;
		}
	}
}
