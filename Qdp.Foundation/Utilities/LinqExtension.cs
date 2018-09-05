using System;
using System.Collections.Generic;
using System.Linq;

namespace Qdp.Foundation.Utilities
{
	public static class LinqExtension
	{
		public static T MaxBy<T, TV>(this IEnumerable<T> collection, Func<T, TV> func)
		{
			var array = collection as T[] ?? collection.ToArray();
			return array.Length == 0 ? default(T) : array.OrderByDescending(func).First();
		}

		public static T MinBy<T, TV>(this IEnumerable<T> collection, Func<T, TV> func)
		{
			var array = collection as T[] ?? collection.ToArray();
			return array.Length == 0 ? default(T) : array.OrderBy(func).First();
		}
	}
}
