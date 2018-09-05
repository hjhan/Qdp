using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Base.Implementations
{
    /// <summary>
    /// 期限类
    /// </summary>
	[DataContract]
	[Serializable]
	public class Term : ITerm
	{
		public static readonly ITerm Infinity = new Term(Int32.MaxValue, Period.Year);

        /// <summary>
        /// 期限长度
        /// </summary>
		public double Length { get; private set; }

        /// <summary>
        /// 期限单位
        /// </summary>
		public Period Period { get; private set; }


		private static readonly Regex[] TermPatterns = {
			new Regex("^(-?\\d+)([dDmMwWyYzZ])"),
			new Regex("^(-?\\d+\\.\\d+)([dDmMwWyYzZ])"),
		};

		//These are specal term conventions
		private static readonly Regex[] TermStrs =
		{
			new Regex("^O/N"), //overnight T + 1BD
			new Regex("^T/N"), //tomorrow next T+2BD
			new Regex("^S/N"), // spot next T+3BD
			new Regex("^TODAY"), //trading day, T
			new Regex("^TOM"), // tomorrow, T+1BD
			new Regex("^SPOT"), //T+2BD 
		};

		private static readonly Regex YmTermPattern = new Regex("^(-?\\d+)[yY](\\d+)[mM]");

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="length">期限长度</param>
        /// <param name="period">期限单位</param>
		public Term(double length, Period period)
		{
			Length = length;
			Period = period;
		}

        /// <summary>
        /// 从字符串构造期限对象。
        /// 
        /// 例如：
        /// 1D - 1天
        /// 2W - 2周
        /// 3M - 3个月
        /// 4Y - 4年
        /// 
        /// </summary>
        /// <param name="term">期限字符串</param>
		public Term(string term)
		{
			switch (term.ToUpper())
			{
				case "O/N":
					Length = 1;
					Period = Period.Day;
					return;
				case "T/N":
					Length = 2;
					Period = Period.Day;
					return;
				case "S/N":
					Length = 3;
					Period = Period.Day;
					return;
				case "TODAY":
					Length = 0;
					Period = Period.Day;
					return;
				case "TOM":
					Length = 1;
					Period = Period.Day;
					return;
				case "SPOT":
					Length = 2;
					Period = Period.Day;
					return;
			}

			foreach (var pattern in TermPatterns)
			{
				var matches = pattern.Match(term);
				if (matches.Success && matches.Groups.Count == 3)
				{
					Length = Convert.ToDouble(matches.Groups[1].Value);
					Period = matches.Groups[2].Value.ToPeriod();
					return;
				}
			}

			var ymMathces = YmTermPattern.Match(term);
			if (ymMathces.Success && ymMathces.Groups.Count == 3)
			{
				Length = Convert.ToDouble(ymMathces.Groups[1].Value) * 12;
				var addition = Convert.ToDouble(ymMathces.Groups[2].Value);
				if (Length > 0)
				{
					Length += addition;
				}
				else
				{
					Length -= addition;
				}
				Period = Period.Month;
				return;
			}

			Length = 0;
			Period = Period.Zero;
			return;
		}

        /// <summary>
        /// 以某一日期为基准，向后间隔一定期限所得到的日期
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <param name="count">期限乘数</param>
        /// <param name="eom">是否总在月末</param>
        /// <returns>目标日期</returns>
        /// <example>
        /// <code language="cs">
        /// var term = new Term("1M");
        /// var date1 = new Date(2018, 3, 20);
        /// var date2 = term.Next(date1); // 2018-04-20
        /// date2 = term.Next(date1, 3); // 2018-06-20
        /// 
        /// date1 = new Date(2018, 6, 30);
        /// date2 = term.Next(date1, 1, true); // 2018-7-31
        /// </code>
        /// </example>
		public Date Next(Date date, int count = 1, bool eom = false)
		{
			if (count < 0) throw new PricingBaseException("Use Term.Prev when count is less than 0 in Term.Next");
			return Adjust(date, Length * count, Period, true, eom);
		}

        /// <summary>
        /// 以某一日期为基准，向前间隔一定期限所得到的日期
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <param name="count">期限乘数</param>
        /// <param name="eom">是否总在月末</param>
        /// <returns>目标日期</returns>
        /// <example>
        /// <code language="cs">
        /// var term = new Term("1M");
        /// var date1 = new Date(2018, 3, 20);
        /// var date2 = term.Prev(date1); // 2018-02-20
        /// date2 = term.Prev(date1, 3); // 2017-12-20
        /// 
        /// date1 = new Date(2018, 6, 30);
        /// date2 = term.Prev(date1, 1, true); // 2018-5-31
        /// </code>
        /// </example>
		public Date Prev(Date date, int count = 1, bool eom = false)
		{
			if (count < 0) throw new PricingBaseException("Use Term.Next when count is less than 0 in Term.Prev");
			return Adjust(date, Length * count, Period, false, eom);
		}
        
		private Date Adjust(Date date, double length, Period period, bool forward, bool eom = false)
		{
			if (period == Period.Zero) return date;

			var dateTime = new DateTime(date.Year, date.Month, date.Day);
			var realAdjustment = forward ? length : -length;

			if (period == Period.Day)
			{
				dateTime = dateTime.AddDays(realAdjustment);
			}
			else if (period == Period.Week)
			{
				dateTime = dateTime.AddDays(7 * realAdjustment);
			}
			else if (period == Period.Month)
			{
				var months = (int)realAdjustment;
				var timeSpan = dateTime.AddMonths(1) - dateTime;
				dateTime = dateTime.AddMonths(months);
				var days = timeSpan.Days * (realAdjustment - months);
				dateTime = dateTime.AddDays(days);
				if (eom)
				{
					if (date.IsEndOfMonth())
					{
						//if start date IsEndOfMonth and eom=true, adjust dateTime to end of month
						var ndays = DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
						dateTime = new DateTime(dateTime.Year, dateTime.Month, ndays);
					}
				}
			}
			else if (period == Period.Year)
			{
				var years = (int)realAdjustment;
				var timeSpan = dateTime.AddYears(1) - dateTime;
				dateTime = dateTime.AddYears(years);
				var days = timeSpan.Days * (realAdjustment - years);
				dateTime = dateTime.AddDays(days);
			}

			return new Date(dateTime);
		}

        /// <summary>
        /// 判断字符串是否为合法的期限字符串
        /// </summary>
        /// <param name="input">字符串</param>
        /// <returns>是否为合法的期限字符串</returns>
		public static bool IsTerm(string input)
		{
			return input != null &&
						 TermPatterns.Union(TermStrs).Union(new[] { YmTermPattern }).Any(termPattern => termPattern.Match(input).Success);
		}

        /// <summary>
        /// 判断两个期限是否相等
        /// </summary>
        /// <param name="t">期限</param>
        /// <returns>是否相等</returns>
		public bool Equals(Term t)
		{
			if (ReferenceEquals(null, t)) return false;

			return t.Length.Equals(Length) && t.Period.Equals(Period);
		}

        /// <summary>
        /// 判断两个对象是否相等
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns>是否相等</returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;

			return Equals(obj as Term);
		}

        /// <summary>
        /// 获取HashCode
        /// </summary>
        /// <returns>HashCode</returns>
		public override int GetHashCode()
		{
			return Length.GetHashCode() & Period.GetHashCode();
		}

        /// <summary>
        /// 转换为期限字符串。如： 1D、2M等
        /// </summary>
        /// <returns>期限字符串</returns>
		public override string ToString()
		{
			return string.Format("{0}{1}", Length, Period.ToShortName());
		}

        /// <summary>
        /// 与另一个期限比较。
        /// 大于另一个期限，返回1
        /// 等于另一个期限，返回0
        /// 小于另一个期限，返回-1
        /// </summary>
        /// <param name="other">另一个期限</param>
        /// <returns>比较结果</returns>
		public int CompareTo(ITerm other)
		{
			var refDate = new Date(DateTime.Now);
			return Convert.ToInt32(this.Next(refDate) - other.Next(refDate));
		}

		public static Term operator *(Term a, int factor)
		{
			return new Term(a.Length * factor, a.Period);
		}

        /// <summary>
        /// 相等判断操作符
        /// </summary>
        /// <param name="a">期限</param>
        /// <param name="b">期限</param>
        /// <returns>是否相等</returns>
		public static bool operator ==(Term a, Term b)
		{
			if (ReferenceEquals(a, b))
			{
				return true;
			}

			if ((object)a == null || (object)b == null)
			{
				return false;
			}

			return a.Equals(b);
		}

        /// <summary>
        /// 不相等判断操作符
        /// </summary>
        /// <param name="a">期限</param>
        /// <param name="b">期限</param>
        /// <returns>是否不相等</returns>
		public static bool operator !=(Term a, Term b)
		{
			return !(a == b);
		}
	}
}
