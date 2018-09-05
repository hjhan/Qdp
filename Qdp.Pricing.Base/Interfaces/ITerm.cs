using System;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;

namespace Qdp.Pricing.Base.Interfaces
{
    /// <summary>
    /// 期限接口
    /// </summary>
	public interface ITerm : IComparable<ITerm>
	{
        /// <summary>
        /// 期限长度
        /// </summary>
		double Length { get; }

        /// <summary>
        /// 期限单位
        /// </summary>
		Period Period { get; }

        /// <summary>
        /// 以某一日期为基准，向后间隔一定期限所得到的日期
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <param name="count">期限乘数</param>
        /// <param name="eom">是否总在月末</param>
        /// <returns>目标日期</returns>
		Date Next(Date date, int count = 1, bool eom = false);

        /// <summary>
        /// 以某一日期为基准，向前间隔一定期限所得到的日期
        /// </summary>
        /// <param name="date">基准日期</param>
        /// <param name="count">期限乘数</param>
        /// <param name="eom">是否总在月末</param>
        /// <returns>目标日期</returns>
		Date Prev(Date date, int count = 1, bool eom = false);
	}
}
