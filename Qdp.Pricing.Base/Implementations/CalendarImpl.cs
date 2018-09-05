using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Qdp.Foundation.ConfigFileReaders;
using Qdp.Foundation.Implementations;
using Qdp.Foundation.Serializer;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Base.Implementations
{
    /// <summary>
    /// 日历假日类
    /// </summary>
	[DataContract]
	public class CalendarHoliday
	{
        /// <summary>
        /// 假日列表
        /// </summary>
		[DataMember]
		public List<string> HolidayList { get; set; }
	}

    /// <summary>
    /// 日历实现类
    /// </summary>
	public class CalendarImpl : ICalendar
	{
		private static Dictionary<string, ICalendar> AllCalendars;
        public static ConfigFileLocationType FileLocationType = ConfigFileLocationType.Default;
        private static bool _cacheCalendarData = false;

        /// <summary>
        /// 初始化日历数据
        /// </summary>
        /// <param name="calendars">日历数据</param>
        public static void InitializeCalendarData(Dictionary<string, CalendarHoliday> calendars)
        {
            if (calendars != null)
            {
                AllCalendars = 
                    calendars.Where(x => x.Value != null && x.Value.HolidayList != null && x.Value.HolidayList.Count > 0) // make sure it's not an empty calendar
                    .ToDictionary(x => x.Key.ToUpper(), x => new CalendarImpl(x.Key, x.Value) as ICalendar);
                _cacheCalendarData = true;
            }
        }

        static CalendarImpl()
		{
            if (!_cacheCalendarData)
            {
                const string filenameSuffix = ".txt";
#if !NETCOREAPP2_1
                //TODO: fix it
                try
                {
                    if (System.AppDomain.CurrentDomain.DomainManager.HostExecutionContextManager.ToString().IndexOf("System.Web") >= 0)
                    {
                        FileLocationType = ConfigFileLocationType.Web;
                    }
                }
                catch
                {
                    // ignore the error for now.
                }
#endif

                var calendarFiles =
                    ConfigFilePathHelper.GetFiles(FileLocationType, "Data", "Calendars")
                        .Where(x => x.EndsWith(filenameSuffix))
                        .ToArray();

                var calendars = calendarFiles.Select(x =>
                {
                    var calendarName = Path.GetFileNameWithoutExtension(x).ToUpper();
                    var s = File.ReadAllText(x);
                    var calendarHolidays = DataContractJsonObjectSerializer.Deserialize<CalendarHoliday>(s);
                    return new CalendarImpl(calendarName, calendarHolidays);
                });

                AllCalendars = calendars.ToDictionary(x => x.CalendarName, x => x as ICalendar);
            }
		}

        /// <summary>
        /// 获取某个日历
        /// </summary>
        /// <param name="calendar">日历枚举值</param>
        /// <returns>日历对象</returns>
		public static ICalendar Get(Calendar calendar)
		{
			ICalendar ret;
			if (AllCalendars.TryGetValue(calendar.ToString().ToUpper(), out ret))
			{
				return ret;
			}

			throw new PricingBaseException(string.Format("Cannot find calendar {0}", calendar.ToString()));
		}

        /// <summary>
        /// 获取某个日历
        /// </summary>
        /// <param name="calendar">日历名称</param>
        /// <returns>日历对象</returns>
		public static ICalendar Get(string calendar)
		{
			ICalendar ret;
			if (AllCalendars.TryGetValue(calendar.ToUpper(), out ret))
			{
				return ret;
			}

			throw new PricingBaseException(string.Format("Cannot find calendar {0}", calendar));
		}

        public string CalendarName { get; private set; }
		private readonly HashSet<Date> _holidays;
		private readonly Dictionary<Date, int> _bizDayIndex;
		private readonly List<Date> _bizDayList;

		private CalendarImpl(string calendarName, CalendarHoliday holidays)
		{
			CalendarName = calendarName;
			_holidays = new HashSet<Date>(holidays.HolidayList.Select(h => new Date(DateTime.Parse(h))));

			_bizDayIndex = new Dictionary<Date, int>();
			_bizDayList = new List<Date>();

			var date = _holidays.Min();
			var endDate = _holidays.Max();
			var index = 0;

			while (date < endDate)
			{
				if (IsBizDay(date))
				{
					_bizDayList.Add(date);
					_bizDayIndex.Add(date, index++);
				}
				date = date.AddDays(1);
			}

		}

        /// <summary>
        /// 根据日历在某日期上增加自然日天数得到的日期
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <param name="offset">调整天数，可以为负数</param>
        /// <param name="bda">交易日规则</param>
        /// <returns>调整后的日期</returns>
		public Date AddDays(Date date, int offset, BusinessDayConvention bda = BusinessDayConvention.None)
		{
			return bda.Adjust(this, date.AddDays(offset));
		}

        /// <summary>
        /// 根据日历在某日期上增加交易日天数得到的日期
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <param name="offset">调整天数，可以为负数</param>
        /// <param name="bda">交易日规则</param>
        /// <returns>调整后的日期</returns>
		public Date AddBizDays(Date date, int offset, BusinessDayConvention bda = BusinessDayConvention.None)
		{
			return AddBizDayFast(bda.Adjust(this, date), offset);
		}

        /// <summary>
        /// 根据日历在某日期上按指定日期间隔调整得到的日期
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <param name="term">日期间隔</param>
        /// <param name="bda">交易日规则</param>
        /// <returns>调整后的日期</returns>
		public Date AddBizDays(Date date, ITerm term, BusinessDayConvention bda = BusinessDayConvention.None)
		{
			var nextDate = term.Next(date);
			return bda.Adjust(this, nextDate);
		}

        /// <summary>
        /// 某日期的下一个交易日
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <returns>下一个交易日</returns>
		public Date NextBizDay(Date date)
		{
			return AddBizDaySlow(date, 1);
		}

        /// <summary>
        /// 某日期的上一个交易日
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <returns>上一个交易日</returns>
		public Date PrevBizDay(Date date)
		{
			return AddBizDaySlow(date, -1);
		}

        /// <summary>
        /// 两个日期之间的所有交易日。包含startDate，但不包含endDate
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>两个日期之间的所有交易日</returns>
		public List<Date> BizDaysBetweenDates(Date startDate, Date endDate)
		{
			return _bizDayList.Where(x => x >= startDate && x < endDate).ToList();
		}

        /// <summary>
        /// 两个日期之间的所有交易日。包含startDate，且包含endDate
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>两个日期之间的所有交易日</returns>
        public List<Date> BizDaysBetweenDatesInclEndDay(Date startDate, Date endDate)
        {
            return _bizDayList.Where(x => x >= startDate && x <= endDate).ToList();
        }

        /// <summary>
        /// 两个日期之间的所有交易日。不包含startDate，但包含endDate
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>两个日期之间的所有交易日</returns>
        public List<Date> BizDaysBetweenDatesExcluStartDay(Date startDate, Date endDate)
        {
            return _bizDayList.Where(x => x > startDate && x <= endDate).ToList();
        }

        /// <summary>
        /// 两个日期之间的所有交易日的数量。
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="excludeEndDate">是否去除endDate</param>
        /// <returns>交易日的数量</returns>
        public int NumberBizDaysBetweenDate(Date startDate, Date endDate, bool excludeEndDate = false)
		{
			var includeEndDate = 0;
			if (!IsBizDay(startDate))
			{
				startDate = NextBizDay(startDate);
			}
			if (!IsBizDay(endDate))
			{
				endDate = PrevBizDay(endDate);
				includeEndDate = 1;
			}
			if (startDate > endDate)
			{
				return 0;
			}
			if (startDate == endDate)
			{
				return 0 + includeEndDate;
			}
            var extra = excludeEndDate ? 0 : includeEndDate;
            return _bizDayIndex[endDate] - _bizDayIndex[startDate] + extra;
		}

        /// <summary>
        /// 判断日期是否为交易日
        /// </summary>
        /// <param name="date">日期</param>
        /// <returns>是否为交易日</returns>
		public bool IsBizDay(Date date)
		{
			return !IsHoliday(date);
		}

        /// <summary>
        /// 判断日期是否为非交易日
        /// </summary>
        /// <param name="date">日期</param>
        /// <returns>是否为非交易日</returns>
		public bool IsHoliday(Date date)
		{
			return _holidays.Contains(date);
		}

        /// <summary>
        /// 将日期调整到交易日
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <param name="bda">交易日规则</param>
        /// <returns>调整后的日期</returns>
		public Date Adjust(Date date, BusinessDayConvention bda = BusinessDayConvention.None)
		{
			return bda.Adjust(this, date);
		}

		private Date AddBizDaySlow(Date date, int offset)
		{
			if (offset == 0) return date;

			var shift = offset < 0 ? -1 : 1;
			do
			{
				date = date.AddDays(shift);
				if (IsBizDay(date))
				{
					offset -= shift;
				}
			} while (offset != 0);

			return date;
		}

		private Date AddBizDayFast(Date date, int offset)
		{
			if (offset == 0) return date;
			var rawDate = date;

			if (IsHoliday(date))
			{
				date = NextBizDay(date);
				offset = offset - 1;
			}

			if (_bizDayIndex.ContainsKey(date))
			{
				var index = _bizDayIndex[date] + offset;
				if (index < _bizDayList.Count && index > 0)
				{
					return _bizDayList[index];
				}
			}

			return AddBizDaySlow(rawDate, offset);
		}
	}
}
