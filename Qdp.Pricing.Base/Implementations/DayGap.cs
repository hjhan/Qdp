using System;
using System.Text.RegularExpressions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Base.Implementations
{
    /// <summary>
    /// 日期差异类，用来表示日期差异规则，常用于结算交收日期规则。
    /// 例如: +2BD，表示两个交易日之后
    /// </summary>
	public class DayGap
	{
        /// <summary>
        /// 日期偏移天数
        /// </summary>
		public int Offset { get; private set; }

        /// <summary>
        /// 时段单位
        /// </summary>
		public Period Period { get; private set; }

        /// <summary>
        /// 是否交易日
        /// </summary>
		public bool IsBizDay { get; private set; }

        /// <summary>
        /// 交易日调整规则
        /// </summary>
		public BusinessDayConvention BusinessDayConvention { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="offset">日期偏移天数</param>
        /// <param name="period">时段单位</param>
        /// <param name="isBizDay">是否交易日</param>
        /// <param name="bda">交易日调整规则</param>
		public DayGap(int offset, Period period, bool isBizDay, BusinessDayConvention bda = BusinessDayConvention.None)
		{
			Offset = offset;
			Period = period;
			IsBizDay = isBizDay;
			BusinessDayConvention = bda;
		}

        /// <summary>
        /// 从字符串构造对象。例如：
        /// +2BD，两个交易日之后
        /// +2D，两个自然日之后
        /// -2BD，两个交易日之前
        /// </summary>
        /// <param name="dayGap">字符串</param>
		public DayGap(string dayGap)
		{
			var pattern = new Regex(@"^(\+?-?\d+)([bB])([dD])");
			var matches = pattern.Match(dayGap);
			
			if (matches.Success && matches.Groups.Count == 4)
			{
				Offset = Convert.ToInt32(matches.Groups[1].Value);
				IsBizDay = matches.Groups[2].Value.ToLower() == "b";
				Period =  matches.Groups[3].Value.ToPeriod();
				BusinessDayConvention = BusinessDayConvention.Following;
				return;
			}

			pattern = new Regex(@"^(\+?-?\d+)([dD])");
			matches = pattern.Match(dayGap);

			if (matches.Success && matches.Groups.Count == 3)
			{
				Offset = Convert.ToInt32(matches.Groups[1].Value);
				IsBizDay = false;
				Period = matches.Groups[2].Value.ToPeriod();
				BusinessDayConvention = BusinessDayConvention.None;
				return;
			}

			throw new PricingBaseException("unrecognized day gap pattern, use i.e. 2bd/-2bd/2d/3d");
		}

        /// <summary>
        /// 根据日历和基准日期得到目标日期
        /// </summary>
        /// <param name="calendar">日历</param>
        /// <param name="date">基准日期</param>
        /// <returns>目标日期</returns>
		public Date Get(ICalendar calendar, Date date)
		{
			return IsBizDay ?
				calendar.AddBizDays(date, Offset, BusinessDayConvention)
				: calendar.AddDays(date, Offset, BusinessDayConvention);
		}
	}
}
