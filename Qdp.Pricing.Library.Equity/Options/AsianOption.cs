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
    /// 亚式期权
    /// </summary>
	public class AsianOption : OptionBase
	{
        /// <summary>
        /// 类型名称
        /// </summary>
        public override string TypeName { get { return "AsianOption"; } }

        /// <summary>
        /// 标的资产价格序列，用于计算均价
        /// </summary>
        public Dictionary<Date, double> Fixings { get; private set; }

        /// <summary>
        /// 亚式期权均价计算方法
        /// </summary>
        public AsianType AsianType { get; private set; }

        /// <summary>
        /// 亚式期权行权价类型
        /// </summary>
        public StrikeStyle StrikeStyle { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="maturityDate">到期日</param>
        /// <param name="exercise">行权方式</param>
        /// <param name="optionType">看涨看跌</param>
        /// <param name="asianType">亚式期权均价计算方法</param>
        /// <param name="strikeStyle">亚式期权行权价类型</param>
        /// <param name="strike">行权价</param>
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
        /// <param name="isMoneynessOption">是否为相对行权价期权</param>
        /// <param name="initialSpotPrice">标的资产期初价格</param>
        /// <param name="dividends">标的资产分红</param>
        /// <param name="hasNightMarket">标的资产是否有夜盘交易</param>
        /// <param name="commodityFuturesPreciseTimeMode">是否启用精确时间模式</param>
        public AsianOption(Date startDate,
			Date maturityDate,
			OptionExercise exercise,
			OptionType optionType,
            AsianType asianType,
            StrikeStyle strikeStyle,
			double strike,
			InstrumentType underlyingInstrumentType,
			ICalendar calendar,
			IDayCount dayCount,
			CurrencyCode payoffCcy,
			CurrencyCode settlementCcy,
			Date[] exerciseDates,
			Date[] observationDates,
			Dictionary<Date, double> fixings,
			double notional = 1,
			DayGap settlementGap = null,
			Date optionPremiumPaymentDate = null,
			double optionPremium = 0,
            bool isMoneynessOption = false,
            double initialSpotPrice = 0.0,
            Dictionary<Date, double> dividends = null,
            bool hasNightMarket = false,
            bool commodityFuturesPreciseTimeMode = false
            )
			: base(startDate, maturityDate, exercise, optionType,new double[] { strike }, underlyingInstrumentType, calendar, dayCount,
                  settlementCcy, payoffCcy, exerciseDates, observationDates, notional, settlementGap, 
                  optionPremiumPaymentDate, optionPremium, 
                  isMoneynessOption: isMoneynessOption, initialSpotPrice: initialSpotPrice, dividends: dividends, hasNightMarket: hasNightMarket,
                  commodityFuturesPreciseTimeMode: commodityFuturesPreciseTimeMode)
		{
			Fixings = fixings;
            AsianType = asianType;
            StrikeStyle = strikeStyle;
        }

        /// <summary>
        /// 获取行权收益
        /// </summary>
        /// <param name="price">标的资产价格</param>
        /// <returns>行权收益</returns>
		public override Cashflow[] GetPayoff(double[] price)
		{
			//used in approximation
			var amount = 0.0;
            var effectiveStrike = IsMoneynessOption ? Strike * InitialSpotPrice : Strike;

            if (OptionType == OptionType.Call)
            {
                if (StrikeStyle == StrikeStyle.Fixed)
                {
                    if (this.AsianType == AsianType.GeometricAverage)
                    {
                        var n = this.Fixings.Count;
                        var finalPrice = Math.Pow( this.Fixings.Select(x => x.Value).Aggregate(func: (result, item) => result * item), 1.0/n);
                        amount = Math.Max(finalPrice - effectiveStrike, 0);
                    }
                    else if (AsianType == AsianType.ArithmeticAverage || AsianType == AsianType.DiscreteArithmeticAverage)
                    {
                        var finalPrice = this.Fixings.Select(x => x.Value).Average();
                        amount = Math.Max(finalPrice - effectiveStrike, 0);
                    }
                }
                else
                {
                    if (this.AsianType == AsianType.GeometricAverage)
                    {
                        var strike = this.Fixings.Select(x => x.Value).Aggregate(func: (result, item) => result * item);
                        amount = Math.Max(price[0] - strike, 0);
                    }
                    else if (AsianType == AsianType.ArithmeticAverage || AsianType == AsianType.DiscreteArithmeticAverage)
                    {
                        var strike = this.Fixings.Select(x => x.Value).Average();
                        amount = Math.Max(price[0] - strike, 0);
                    }
                }
            }
            else // Put
            {
                if (StrikeStyle == StrikeStyle.Fixed)
                {
                    if (this.AsianType == AsianType.GeometricAverage)
                    {
                        var n = this.Fixings.Count;
                        var finalPrice = Math.Pow(this.Fixings.Select(x => x.Value).Aggregate(func: (result, item) => result * item), 1.0 / n);
                        amount = Math.Max(effectiveStrike - finalPrice, 0);
                    }
                    else if (AsianType == AsianType.ArithmeticAverage || AsianType == AsianType.DiscreteArithmeticAverage)
                    {
                        var finalPrice = this.Fixings.Select(x => x.Value).Average();
                        amount = Math.Max(effectiveStrike - finalPrice, 0);
                    }
                }
                else
                {
                    if (this.AsianType == AsianType.GeometricAverage)
                    {
                        var strike = this.Fixings.Select(x => x.Value).Aggregate(func: (result, item) => result * item);
                        amount = Math.Max(strike - price[0], 0);
                    }
                    else if (AsianType == AsianType.ArithmeticAverage || AsianType == AsianType.DiscreteArithmeticAverage)
                    {
                        var strike = this.Fixings.Select(x => x.Value).Average();
                        amount = Math.Max(strike - price[0], 0);
                    }
                }
            }

			var settlementDate = SettlmentGap.Get(Calendar, UnderlyingMaturityDate);

			return new[]
			{
				new Cashflow(StartDate, UnderlyingMaturityDate, settlementDate, amount*Notional, SettlementCcy, CashflowType.Net, false, double.NaN, null),
				//new Cashflow(StartDate, UnderlyingMaturityDate, OptionPremiumPaymentDate ?? settlementDate, OptionPremium, SettlementCcy, CashflowType.Net, false, double.NaN, null)
			};
		}

        /// <summary>
        /// 获取行权收益
        /// </summary>
        /// <param name="pricePath">价格路径</param>
        /// <returns>行权收益</returns>
		public override Cashflow[] GetPayoff(Dictionary<Date, double> pricePath)
		{
			var prices = new Dictionary<Date, double>(pricePath);

			//merges fixings and monte carlo path
			foreach (var key in Fixings.Keys)
			{
				prices[key] = Fixings[key];
			}

			var avgPrice = ObservationDates.Average(x => prices[x]);
            return GetPayoff(new[] { avgPrice, pricePath[ExerciseDates[0]] });
		}

        /// <summary>
        /// 根据行权方式拷贝生成一个新的期权（尚未实现）
        /// </summary>
        /// <param name="exercise">行权方式</param>
        /// <returns>新的期权</returns>
        public override IOption Clone(OptionExercise exercise)
        {
            var newExerciseSchedule = (exercise == OptionExercise.European) ? new Date[] { ExerciseDates.Last() } : ExerciseDates;
            return new AsianOption(
                startDate: this.StartDate,
                maturityDate: this.UnderlyingMaturityDate,
                exercise: exercise,
                asianType:this.AsianType,
                strikeStyle:this.StrikeStyle,
                optionType: this.OptionType,
                strike: this.Strike,
                underlyingInstrumentType: this.UnderlyingProductType,
                calendar: this.Calendar,
                dayCount: this.DayCount,
                payoffCcy: this.PayoffCcy,
                settlementCcy: this.SettlementCcy,
                fixings:this.Fixings,
                exerciseDates: newExerciseSchedule,
                observationDates: this.ObservationDates,
                notional: this.Notional,
                settlementGap: this.SettlmentGap,
                optionPremiumPaymentDate: this.OptionPremiumPaymentDate,
                optionPremium: this.OptionPremium,
                isMoneynessOption: this.IsMoneynessOption,
                initialSpotPrice: this.InitialSpotPrice,
                dividends: this.Dividends,
                hasNightMarket: this.HasNightMarket,
                commodityFuturesPreciseTimeMode: this.CommodityFuturesPreciseTimeMode);
        }
    }
    
}
