using System;
using System.Collections.Generic;
using System.Linq;

namespace Qdp.Pricing.Library.Base.Utilities
{
	public static class EnumerableDoubleExtension
	{
		public static int GetClosestIndexOf(this IEnumerable<double> array, double targetValue)
		{
			var arr = array.ToArray();
			var minDistance = Math.Abs(targetValue - arr[0]);
			var minInd = 0;

			for (var i = 1; i < arr.Length; ++i)
			{
				var distance = Math.Abs(targetValue - arr[i]);
				if (distance < minDistance)
				{
					minDistance = distance;
					minInd = i;
				}
			}

			return minInd;
		}

		public static int GetRangeLeftIndexOf(this IEnumerable<double> sortedArray, double targetValue)
		{
			var arrValue = sortedArray.ToArray();
			int i;
			for (i = 0; i < arrValue.Length-1; ++i)
			{
				if ((arrValue[i] <= targetValue && targetValue < arrValue[i + 1]) ||
				    (arrValue[i + 1] <= targetValue && targetValue < arrValue[i]))
				{
					return i;
				}
			}

			return i;

			throw new PricingLibraryException(string.Format("Value {0} is outside the range of array [{1},{2}]", targetValue, arrValue.Min(), arrValue.Max()));
		}
	}
}
