using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;

namespace Qdp.Pricing.Base.Interfaces
{
    /// <summary>
    /// 日历接口
    /// </summary>
	public interface ICalendar
	{
        /// <summary>
        /// 根据日历在某日期上增加自然日天数得到的日期
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <param name="offset">调整天数，可以为负数</param>
        /// <param name="bda">交易日规则</param>
        /// <returns>调整后的日期</returns>
		Date AddDays(Date date, int offset, BusinessDayConvention bda = BusinessDayConvention.None);

        /// <summary>
        /// 根据日历在某日期上增加交易日天数得到的日期
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <param name="offset">调整天数，可以为负数</param>
        /// <param name="bda">交易日规则</param>
        /// <returns>调整后的日期</returns>
		Date AddBizDays(Date date, int offset, BusinessDayConvention bda = BusinessDayConvention.None);

        /// <summary>
        /// 根据日历在某日期上按指定日期间隔调整得到的日期
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <param name="term">日期间隔</param>
        /// <param name="bda">交易日规则</param>
        /// <returns>调整后的日期</returns>
		Date AddBizDays(Date date, ITerm term, BusinessDayConvention bda = BusinessDayConvention.None);

        /// <summary>
        /// 某日期的下一个交易日
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <returns>下一个交易日</returns>
		Date NextBizDay(Date date);

        /// <summary>
        /// 某日期的上一个交易日
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <returns>上一个交易日</returns>
		Date PrevBizDay(Date date);

        /// <summary>
        /// 两个日期之间的所有交易日。包含startDate，但不包含endDate
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>两个日期之间的所有交易日</returns>
		List<Date> BizDaysBetweenDates(Date startDate, Date endDate);

        /// <summary>
        /// 两个日期之间的所有交易日。包含startDate，且包含endDate
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>两个日期之间的所有交易日</returns>
        List<Date> BizDaysBetweenDatesInclEndDay(Date startDate, Date endDate);

        /// <summary>
        /// 两个日期之间的所有交易日。不包含startDate，但包含endDate
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>两个日期之间的所有交易日</returns>
        List<Date> BizDaysBetweenDatesExcluStartDay(Date startDate, Date endDate);

        /// <summary>
        /// 两个日期之间的所有交易日的数量。
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="excludeEndDate">是否去除endDate</param>
        /// <returns>交易日的数量</returns>
        int NumberBizDaysBetweenDate(Date startDate, Date endDate, bool excludeEndDate);

        /// <summary>
        /// 判断日期是否为交易日
        /// </summary>
        /// <param name="date">日期</param>
        /// <returns>是否为交易日</returns>
		bool IsBizDay(Date date);

        /// <summary>
        /// 判断日期是否为非交易日
        /// </summary>
        /// <param name="date">日期</param>
        /// <returns>是否为非交易日</returns>
		bool IsHoliday(Date date);

        /// <summary>
        /// 将日期调整到交易日
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <param name="bda">交易日规则</param>
        /// <returns>调整后的日期</returns>
		Date Adjust(Date date, BusinessDayConvention bda = BusinessDayConvention.None);
	}
}
