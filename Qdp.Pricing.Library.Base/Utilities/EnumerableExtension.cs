using System;
using System.Collections.Generic;
using System.Linq;

namespace Qdp.Pricing.Library.Base.Utilities
{
	public static class EnumerableExtension
	{
		public static int FirstIndexOf<T>(this IEnumerable<T> collection, Func<T, bool> func)
		{
			return collection.Select((x, i) => new {x, i}).First(y => func(y.x)).i;
		}
		public static int? FirstOrDefaultIndexOf<T>(this IEnumerable<T> collection, Func<T, bool> func)
		{
			var firstOrDefault = collection.Select((x, i) => new { x, i }).FirstOrDefault(y => func(y.x));
			if (firstOrDefault != null)
				return firstOrDefault.i;
			return null;
		}
		public static IEnumerable<T> Remove<T>(this IEnumerable<T> collection, Func<T, bool> func)
		{
			var list = collection.ToList();
			list.Remove(collection.FirstOrDefault(func));
			return list;
		}

		public static List<T> ToListT<T>(this T obj)
		{
			return new List<T>{obj};
		}

		public static T[] ToArrayT<T>(this T obj)
		{
			return new[] {obj};
		}
	}
}
