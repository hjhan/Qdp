using System;
using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Equity.Interfaces;

/// <summary>
/// Qdp.Pricing.Library.Equity.Options 
/// </summary>
namespace Qdp.Pricing.Library.Equity.Options
{
    /// <summary>
    /// 价差期权
    /// </summary>
    public class SpreadOption : OptionBase
    {
        /// <summary>
        /// 类型名称
        /// </summary>
        public override string TypeName { get { return "SpreadOption"; } }

        /// <summary>
        /// 价差类型
        /// </summary>
        public SpreadType SpreadType { get; private set; }

        /// <summary>
        /// 权重
        /// </summary>
        public double[] Weights { get; private set; }


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="maturityDate">到期日</param>
        /// <param name="exercise">行权方式</param>
        /// <param name="optionType">看涨看跌</param>
        /// <param name="spreadType">价差期权类型</param>
        /// <param name="weights">权重</param>
        /// <param name="strike">行权价</param>
        /// <param name="underlyingInstrumentType">标的资产类型</param>
        /// <param name="calendar">交易日历</param>
        /// <param name="dayCount">日期规则</param>
        /// <param name="payoffCcy">收益计算币种</param>
        /// <param name="settlementCcy">结算币种</param>
        /// <param name="exerciseDates">行权日</param>
        /// <param name="observationDates">观察日</param>
        /// <param name="underlyingTickers">标的代码</param>
        /// <param name="notional">名义本金</param>
        /// <param name="settlementGap">结算日规则</param>
        /// <param name="optionPremiumPaymentDate">权利金支付日</param>
        /// <param name="optionPremium">权利金</param>
        /// <param name="hasNightMarket">标的资产是否有夜盘交易</param>
        /// <param name="commodityFuturesPreciseTimeMode">是否启用精确时间计算模式</param>
        public SpreadOption(Date startDate,
            Date maturityDate,
            OptionExercise exercise,
            OptionType optionType,
            SpreadType spreadType,
            double[] weights,
            double strike,
            InstrumentType underlyingInstrumentType,
            ICalendar calendar,
            IDayCount dayCount,
            CurrencyCode payoffCcy,
            CurrencyCode settlementCcy,
            Date[] exerciseDates,
            Date[] observationDates,
            string[] underlyingTickers,
            double notional = 1,
            DayGap settlementGap = null,
            Date optionPremiumPaymentDate = null,
            double optionPremium = 0,
            bool hasNightMarket = false,
            bool commodityFuturesPreciseTimeMode = false)
            : base(startDate: startDate, maturityDate: maturityDate, exercise: exercise, optionType: optionType, strike: new double[] { strike },
                  underlyingInstrumentType: underlyingInstrumentType, calendar: calendar, dayCount: dayCount,
                  settlementCcy: settlementCcy, payoffCcy: payoffCcy, exerciseDates: exerciseDates, observationDates: observationDates,
                  notional: notional, settlementGap: settlementGap, optionPremiumPaymentDate: optionPremiumPaymentDate, optionPremium: optionPremium, underlyingTickers: underlyingTickers,
                  hasNightMarket: hasNightMarket, commodityFuturesPreciseTimeMode: commodityFuturesPreciseTimeMode)
        {
            SpreadType = spreadType;
            Weights = weights;
        }
        /// <summary>
        /// 获取行权收益
        /// </summary>
        /// <param name="price">标的资产价格</param>
        /// <returns>行权收益</returns>
        public override Cashflow[] GetPayoff(double[] price)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 获取行权收益
        /// </summary>
        /// <param name="pricePath">价格路径</param>
        /// <returns>行权收益</returns>
        //this one is used in Monte Carlo, and Monte carlo is used to Pricing European option
        public override Cashflow[] GetPayoff(Dictionary<Date, double> pricePath)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 根据行权方式拷贝生成一个新的期权
        /// </summary>
        /// <param name="exercise">行权方式</param>
        /// <returns>新的期权</returns
        public override IOption Clone(OptionExercise exercise)
        {
            throw new NotImplementedException();
        }
    }
}
