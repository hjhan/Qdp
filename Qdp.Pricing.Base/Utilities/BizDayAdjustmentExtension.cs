using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Interfaces;

namespace Qdp.Pricing.Base.Utilities
{
	public static class BusinessDayConventionExtension
	{
		public static Date Adjust(this BusinessDayConvention bda, ICalendar calendar, Date date)
		{
			if (calendar.IsHoliday(date))
			{
				switch (bda)
				{
					case BusinessDayConvention.Following:
						return calendar.NextBizDay(date);
					case BusinessDayConvention.ModifiedFollowing:
						var d = calendar.NextBizDay(date);
						return Date.InSameMonth(date, d) ? d : calendar.PrevBizDay(date);
					case BusinessDayConvention.ModifiedPrevious:
						d = calendar.PrevBizDay(date);
						return Date.InSameMonth(date, d) ? d : calendar.NextBizDay(date);
					case BusinessDayConvention.Previous:
						return calendar.PrevBizDay(date);
				}
			}
			return date;
		}
	}
}
