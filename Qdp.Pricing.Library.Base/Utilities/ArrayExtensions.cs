namespace Qdp.Pricing.Library.Base.Utilities
{
	public static class ArrayExtensions
	{
		//x0, x1, x2, x3, x4
		//zone0: x < x0
		//zone1: x0 <= x < x1
		//zone2: x1 <= x < x2
		// ......
		public static int Locate(this double[] array, double value)
		{
			int i;
			for (i = 0; i < array.Length; ++i)
			{
				if (value < array[i])
				{
					return i;
				}
			}
			return i;
		}

		public static int UpperBound(this double[] array, double value)
		{
			int i;
			for (i = 0; i < array.Length; ++i)
			{
				if (value < array[i])
				{
					return i;
				}
			}
			return i;
		}
	}
}
