using System;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;

namespace Qdp.Pricing.Base.Utilities
{
	public static class StringExtension
	{
		public static bool IsTerm(this string str)
		{
			var temp = new Term(str);
			return !(temp.Length.IsAlmostZero() && temp.Period == Period.Zero);
		}
	}

	public static class DateTimeExtension
	{
		public static bool IsEndOfMonth(this DateTime dateTime)
		{
			return dateTime.Day == DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
		}
	}
}
