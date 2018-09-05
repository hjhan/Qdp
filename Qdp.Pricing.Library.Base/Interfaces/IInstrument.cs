using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;

/// <summary>
/// Qdp.Pricing.Library.Base.Interfaces
/// </summary>
namespace Qdp.Pricing.Library.Base.Interfaces
{
    /// <summary>
    /// 金融产品接口
    /// </summary>
	public interface IInstrument
	{
        /// <summary>
        /// Id
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 类型名称
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// 开始日期
        /// </summary>
		Date StartDate { get; } // the start date 

        /// <summary>
        /// 结束日期
        /// </summary>
		Date UnderlyingMaturityDate { get; } // the maturity date

        /// <summary>
        /// 结算日规则
        /// </summary>
		DayGap SettlmentGap { get; } // Day gap between cash flow calculation day and actual settlement day

        /// <summary>
        /// 名义本金
        /// </summary>
		double Notional { get; set; }
	}
}
