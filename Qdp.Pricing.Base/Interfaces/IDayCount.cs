using Qdp.Foundation.Implementations;

namespace Qdp.Pricing.Base.Interfaces
{
    /// <summary>
    /// 日期规则接口
    /// </summary>
	public interface IDayCount
	{
        /// <summary>
        /// 两个日期之间的天数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <returns>天数</returns>
		double DaysInPeriod(Date startDate, Date endDate);

        /// <summary>
        /// 两个日期的差，以年为单位
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>日期差</returns>
		double CalcDayCountFraction(Date startDate, Date endDate, Date referenceStartDate = null, Date referenceEndDate = null);

        /// <summary>
        /// 通过基准日期和日期差，计算目标日期
        /// </summary>
        /// <param name="startDate">基准日期</param>
        /// <param name="dayCountFraction">日期差</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>目标日期</returns>
        Date CalcEndDateFromDayCountFraction(Date startDate, double dayCountFraction, Date referenceStartDate, Date referenceEndDate);
    }
}
