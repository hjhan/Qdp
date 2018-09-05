using System;
using System.Collections.Generic;
using System.Linq;
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
    /// 彩虹期权
    /// </summary>
    public class RainbowOption : OptionBase
    {
        /// <summary>
        /// 类型名称
        /// </summary>
        public override string TypeName { get { return "RainbowOption"; } }

        /// <summary>
        /// 彩虹期权类型
        /// </summary>
        public RainbowType RainbowType { get; private set; }

        /// <summary>
        /// 现金金额
        /// </summary>
        public double CashAmount { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="maturityDate">到期日</param>
        /// <param name="exercise">行权方式</param>
        /// <param name="optionType">看涨看跌</param>
        /// <param name="rainbowType">彩虹期权类型</param>
        /// <param name="strikes">行权价</param>
        /// <param name="cashAmount">现金金额</param>
        /// <param name="underlyingInstrumentType">标的资产类型</param>
        /// <param name="calendar">交易日历</param>
        /// <param name="dayCount">日期规则</param>
        /// <param name="payoffCcy">收益计算币种</param>
        /// <param name="settlementCcy">结算币种</param>
        /// <param name="exerciseDates">行权日</param>
        /// <param name="observationDates">观察日</param>
        /// <param name="fixings">观察价格序列</param>
        /// <param name="notional">名义本金</param>
        /// <param name="settlementGap">结算日规则</param>
        /// <param name="optionPremiumPaymentDate">权利金支付日</param>
        /// <param name="optionPremium">权利金</param>
        /// <param name="hasNightMarket">标的资产是否有夜盘交易</param>
        /// <param name="commodityFuturesPreciseTimeMode">是否启用精确时间模式</param>
        public RainbowOption(Date startDate,
            Date maturityDate,
            OptionExercise exercise,
            OptionType optionType,
            RainbowType rainbowType,
            double[] strikes, 
            double cashAmount,
            InstrumentType underlyingInstrumentType,
            ICalendar calendar,
            IDayCount dayCount,
            CurrencyCode payoffCcy,
            CurrencyCode settlementCcy,
            Date[] exerciseDates,
            Date[] observationDates,
            string [] underlyingTickers,
            double notional = 1,
            DayGap settlementGap = null,
            Date optionPremiumPaymentDate = null,
            double optionPremium = 0,
            bool hasNightMarket = false,
            bool commodityFuturesPreciseTimeMode = false)
            : base(startDate:startDate, maturityDate:maturityDate, exercise:exercise, optionType:optionType, strike:strikes, 
                  underlyingInstrumentType:underlyingInstrumentType, calendar:calendar, dayCount:dayCount,
                  settlementCcy:settlementCcy, payoffCcy:payoffCcy, exerciseDates:exerciseDates, observationDates:observationDates,
                  notional:notional, settlementGap:settlementGap, optionPremiumPaymentDate:optionPremiumPaymentDate, optionPremium:optionPremium, underlyingTickers: underlyingTickers,
                  hasNightMarket: hasNightMarket, commodityFuturesPreciseTimeMode: commodityFuturesPreciseTimeMode)
        {
            RainbowType = rainbowType;
            CashAmount = cashAmount;
            Strikes = strikes;
        }

        /// <summary>
        /// 获取行权收益
        /// </summary>
        /// <param name="price">标的资产价格</param>
        /// <returns>行权收益</returns>
        public override Cashflow[] GetPayoff(double[] price)
        {
            double amount = 0;

            if (RainbowType == RainbowType.Max)
            {
                amount = OptionType == OptionType.Call ? Math.Max(Math.Max(price[0], price[1]) - Strikes[0], 0) : 
                    Math.Max(Strikes[0] - Math.Max(price[0], price[1]), 0);
            }

            if (RainbowType == RainbowType.Min)
            {
                amount = OptionType == OptionType.Call ? Math.Max(Math.Min(price[0], price[1]) - Strikes[0], 0) :
                    Math.Max(Strikes[0] - Math.Min(price[0], price[1]), 0);
            }

            if (RainbowType == RainbowType.BestOfAssetsOrCash)
            {
                amount = Math.Max(Math.Max(Strikes[0], price[0]), price[1]);
            }

            if (RainbowType == RainbowType.BestCashOrNothing)
            {
                amount = OptionType == OptionType.Call ? (Math.Max(price[0], price[1]) >= Strikes[0] ? 1.0 : 0.0) * CashAmount :
                    (Math.Min(price[0], price[1]) <= Strikes[0] ? 1.0 : 0.0) * CashAmount;
            }

            if (RainbowType == RainbowType.WorstCashOrNothing)
            {
                amount = OptionType == OptionType.Call ? (Math.Min(price[0], price[1]) >= Strikes[0] ? 1.0 : 0.0) * CashAmount :
                    (Math.Max(price[0], price[1]) <= Strikes[0] ? 1.0 : 0.0) * CashAmount;
            }

            if (RainbowType == RainbowType.TwoAssetsCashOrNothing)
            {
                amount = OptionType == OptionType.Call ? ((price[0] > Strikes[0] & price[1] > Strikes[1]) ? 1.0 : 0.0) * CashAmount :
                    ((price[0] < Strikes[0] & price[1] < Strikes[1]) ? 1.0 : 0.0) * CashAmount;
            }

            if (RainbowType == RainbowType.TwoAssetsCashOrNothingUpDown)
            {
                amount = ((price[0] > Strikes[0] & price[1] < Strikes[1]) ? 1.0 : 0.0) * CashAmount;
            }

            if (RainbowType == RainbowType.TwoAssetsCashOrNothingDownUp)
            {
                amount = ((price[0] < Strikes[0] & price[1] > Strikes[1]) ? 1.0 : 0.0) * CashAmount;
            }

            return new[]
            {
                new Cashflow(StartDate, UnderlyingMaturityDate, UnderlyingMaturityDate, amount*Notional, PayoffCcy, CashflowType.Net, false, double.NaN,null)
            };
        }

        /// <summary>
        /// 获取行权收益
        /// </summary>
        /// <param name="pricePath">价格路径</param>
        /// <returns>行权收益</returns>
        //this one is used in Monte Carlo, and Monte carlo is used to Pricing European option
        public override Cashflow[] GetPayoff(Dictionary<Date, double> pricePath)
        {
            if (Exercise == OptionExercise.European)
            {
                return GetPayoff(new[] { pricePath[ExerciseDates.Single()] });
            }
            else
            {
                //American or Bermudan
                return GetPayoff(new[] { pricePath[ExerciseDates.Single()] });
            }
        }

        /// <summary>
        /// 根据行权方式拷贝生成一个新的期权（尚未实现）
        /// </summary>
        /// <param name="exercise">行权方式</param>
        /// <returns>新的期权</returns>
        public override IOption Clone(OptionExercise exercise)
        {
            throw new NotImplementedException();
        }

    }
}
