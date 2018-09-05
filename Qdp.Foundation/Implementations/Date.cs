using System;
using System.Runtime.Serialization;

/// <summary>
/// Qdp.Foundation.Implementations
/// </summary>
namespace Qdp.Foundation.Implementations
{
    /// <summary>
    /// Qdp日期类    
    /// </summary>
	[DataContract]
    [Serializable]
	public class Date : IComparable
	{
        /// <summary>
        /// 最小日期
        /// </summary>
		public static readonly Date MinValue = new Date(DateTime.MinValue);

        /// <summary>
        /// 最大日期
        /// </summary>
		public static readonly Date MaxValue = new Date(DateTime.MaxValue);

        /// <summary>
        /// 内置的C# DateTime对象
        /// </summary>
		[DataMember]
		public DateTime DateTime { get; set; }

        /// <summary>
        /// 年份
        /// </summary>
		public int Year
		{
			get { return DateTime.Year; }
		}

        /// <summary>
        /// 月份
        /// </summary>
		public int Month
		{
			get { return DateTime.Month; }
		}

        /// <summary>
        /// 天
        /// </summary>
		public int Day
		{
			get { return DateTime.Day; }
		}

        /// <summary>
        /// 一周的某天
        /// </summary>
		public DayOfWeek WeekDay
		{
			get { return DateTime.DayOfWeek; }
		}

        /// <summary>
        /// 是否为周末
        /// </summary>
		public bool IsWeekend
		{
			get
			{
				var dayOfWeek = WeekDay;
				return dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;
			}
		}

        /// <summary>
        /// 构造函数。        
        /// </summary>
        /// <param name="year">年份</param>
        /// <param name="month">月份</param>
        /// <param name="day">天</param>
        /// <example>
        /// <code language="cs">
        /// var date = new Date(2008, 3, 12);
        /// </code>
        /// </example>
		public Date(int year, int month, int day)
		{
			DateTime = new DateTime(year, month, day);
		}

        /// <summary>
        /// 构造函数。从C#的DateTime对象构造
        /// </summary>
        /// <param name="dateTime"></param>
		public Date(DateTime dateTime)
		{
			DateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
		}

        /// <summary>
        /// 构造函数。拷贝对象
        /// </summary>
        /// <param name="date"></param>
		public Date(Date date)
			: this(date.DateTime)
		{

		}

        /// <summary>
        /// 构造函数。从OLE自动化日期值构造
        /// </summary>
        /// <param name="oaDate"></param>
		public Date(double oaDate)
			: this(DateTime.FromOADate(oaDate))
		{

		}

		private Date()
		{
			
		}

        /// <summary>
        /// 某一年包含多少天
        /// </summary>
        /// <param name="year">年份</param>
        /// <returns>该年的天数</returns>
		public static int DaysInYear(int year)
		{
			return DateTime.IsLeapYear(year) ? 366 : 365;
		}

        /// <summary>
        /// 是否为闰年
        /// </summary>
        /// <param name="year">年份</param>
        /// <returns是否为闰年></returns>
		public static bool IsLeapYear(int year)
		{
			return DateTime.IsLeapYear(year);
		}

        /// <summary>
        /// 判断两个日期是否在同一年的同一个月份
        /// </summary>
        /// <param name="a">日期a</param>
        /// <param name="b">日期b</param>
        /// <returns>是否在同一年的同一个月份</returns>
		public static bool InSameMonth(Date a, Date b)
		{
			return a.Year == b.Year && a.Month == b.Month;
		}

        /// <summary>
        /// 日期是该年的第几天
        /// </summary>
        /// <returns>第几天</returns>
		public int DayOfYear()
		{
			return DateTime.DayOfYear;
		}

        /// <summary>
        /// 判断日期是否在指定的两个日期之间。两个日期区间是一个左闭右开的区间：[start, end)        
        /// </summary>
        /// <param name="start">开始日期</param>
        /// <param name="end">结束日期</param>
        /// <returns>是否在两个日期之间</returns>
        /// <example>
        /// <code language="cs">
        /// var date = new Date(2018, 3, 12);
        /// var result = date.Between(new Date(2018, 3, 11), new Date(2018, 3, 13)); // true
        /// result = date.Between(new Date(2018, 3, 12), new Date(2018, 3, 13)); // true
        /// result = date.Between(new Date(2018, 3, 11), new Date(2018, 3, 12)); // false
        /// </code>
        /// </example>
		public bool Between(Date start, Date end)
		{
			return this >= start && this < end;
		}

        /// <summary>
        /// 将日期转换为OLE自动化日期
        /// </summary>
        /// <returns>OLE自动化日期</returns>
		public double ToOADate()
		{
			return DateTime.ToOADate();
		}

        /// <summary>
        /// 将日期增加指定天数后得到的日期
        /// </summary>
        /// <param name="days">增加的天数。如果是负数，则减去对应天数。</param>
        /// <returns>调整后的日期</returns>
		public Date AddDays(int days)
		{
			return new Date(DateTime.AddDays(days));
		}

        /// <summary>
        /// 判断日期是否为月末
        /// </summary>
        /// <returns>是否为月末</returns>
		public bool IsEndOfMonth()
		{
			return DateTime.Day == DateTime.DaysInMonth(DateTime.Year, DateTime.Month);
		}

        /// <summary>
        /// 大于操作符
        /// </summary>
        /// <param name="a">日期</param>
        /// <param name="b">日期</param>
        /// <returns>判断结果</returns>
		public static bool operator >(Date a, Date b)
		{
			return a.DateTime > b.DateTime;
		}

        /// <summary>
        /// 小于操作符
        /// </summary>
        /// <param name="a">日期</param>
        /// <param name="b">日期</param>
        /// <returns>判断结果</returns>
		public static bool operator <(Date a, Date b)
		{
			return a.DateTime < b.DateTime;
		}

        /// <summary>
        /// 大于等于操作符
        /// </summary>
        /// <param name="a">日期</param>
        /// <param name="b">日期</param>
        /// <returns>判断结果</returns>
		public static bool operator >=(Date a, Date b)
		{
			return a.DateTime >= b.DateTime;
		}

        /// <summary>
        /// 小于等于操作符
        /// </summary>
        /// <param name="a">日期</param>
        /// <param name="b">日期</param>
        /// <returns>判断结果</returns>
		public static bool operator <=(Date a, Date b)
		{
			return a.DateTime <= b.DateTime;
		}

        /// <summary>
        /// 两个日期相减
        /// </summary>
        /// <param name="a">日期</param>
        /// <param name="b">日期</param>
        /// <returns>判断结果</returns>
		public static double operator -(Date a, Date b)
		{
			return a.DateTime.ToOADate() - b.DateTime.ToOADate();
		}

        /// <summary>
        /// 判断两个日期是否相等
        /// </summary>
        /// <param name="a">日期</param>
        /// <param name="b">日期</param>
        /// <returns>判断结果</returns>
		public static bool operator ==(Date a, Date b)
		{
			if (ReferenceEquals(a, b)) 
				return true;

			if (((object) a == null) || ((object) b == null))
				return false;

			return a.DateTime == b.DateTime;
		}

        /// <summary>
        /// 判断两个日期是否不相等
        /// </summary>
        /// <param name="a">日期</param>
        /// <param name="b">日期</param>
        /// <returns>判断结果</returns>
		public static bool operator !=(Date a, Date b)
		{
			return !(a == b);
		}

        /// <summary>
        /// 与另一日期比较
        /// </summary>
        /// <param name="obj">另一日期</param>
        /// <returns>大于返回1，等于返回0，小于返回-1</returns>
		public int CompareTo(object obj)
		{
			if (ReferenceEquals(null, obj)) return 1;
			if (ReferenceEquals(this, obj)) return 1;

			return DateTime.CompareTo((obj as Date).DateTime);
		}

		protected bool Equals(Date other)
		{
			if (ReferenceEquals(null, other)) return false;
			return DateTime.Equals(other.DateTime);
		}

		public override bool Equals(object other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return Equals(other as Date);
		}

		public override int GetHashCode()
		{
			return DateTime.GetHashCode();
		}

        /// <summary>
        /// 转换成yyyy-MM-dd格式的字符串，如：2008-03-09
        /// </summary>
        /// <returns>字符串</returns>
		public override string ToString()
		{
			return string.Format("{0:D4}-{1:D2}-{2:D2}", DateTime.Year, DateTime.Month, DateTime.Day);
		}
	}
}
