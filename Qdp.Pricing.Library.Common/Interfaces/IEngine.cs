using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Interfaces;

/// <summary>
/// Qdp.Pricing.Library.Common.Interfaces
/// </summary>
namespace Qdp.Pricing.Library.Common.Interfaces
{
    /// <summary>
    /// 计算引擎接口
    /// </summary>
	public interface IEngine
	{
        /// <summary>
        /// 计算一个金融衍生品交易的定价和风险指标
        /// </summary>
        /// <param name="trade">交易</param>
        /// <param name="market">市场数据对象</param>
        /// <param name="request">计算请求类型</param>
        /// <returns>计算结果</returns>
		IPricingResult Calculate(object trade, IMarketCondition market, PricingRequest request);
	}

    /// <summary>
    /// 带泛型的计算引擎接口
    /// </summary>
    /// <typeparam name="TTrade">交易</typeparam>
	public interface IEngine<in TTrade> : IEngine
		where TTrade : IInstrument
	{
        /// <summary>
        /// 计算一个金融衍生品交易的定价和风险指标
        /// </summary>
        /// <param name="trade">交易</param>
        /// <param name="market">市场数据对象</param>
        /// <param name="request">计算请求类型</param>
        /// <returns>计算结果</returns>
		IPricingResult Calculate(TTrade trade, IMarketCondition market, PricingRequest request);
	}

    /// <summary>
    /// 估值引擎虚基类
    /// </summary>
    /// <typeparam name="TTrade"></typeparam>
	public abstract class Engine<TTrade> : IEngine<TTrade>
		where TTrade : IInstrument
	{
        /// <summary>
        /// 计算一个金融衍生品交易的定价和风险指标
        /// </summary>
        /// <param name="trade">交易</param>
        /// <param name="market">市场数据对象</param>
        /// <param name="request">计算请求类型</param>
        /// <returns>计算结果</returns>
        public abstract IPricingResult Calculate(TTrade trade, IMarketCondition market, PricingRequest request);

        /// <summary>
        /// 计算一个金融衍生品交易的定价和风险指标
        /// </summary>
        /// <param name="trade">交易</param>
        /// <param name="market">市场数据对象</param>
        /// <param name="request">计算请求类型</param>
        /// <returns>计算结果</returns>
        public IPricingResult Calculate(object trade, IMarketCondition market, PricingRequest request)
		{
			return Calculate((TTrade)trade, market, request);
		}
	}
}
