using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Equity.Interfaces;

/// <summary>
/// Qdp.Pricing.Library.Equity.Options
/// </summary>
namespace Qdp.Pricing.Library.Equity.Options
{
    /// <summary>
    /// 香草期权
    /// </summary>
	public class VanillaOption : OptionBase
	{
        /// <summary>
        /// 类型名称
        /// </summary>
        public override string TypeName { get { return "VanillaOption"; } }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="maturityDate">到期日</param>
        /// <param name="exercise">行权方式</param>
        /// <param name="optionType">看涨看跌</param>
        /// <param name="strike">行权价</param>
        /// <param name="underlyingInstrumentType">标的资产类型</param>
        /// <param name="calendar">交易日历</param>
        /// <param name="dayCount">日期规则</param>
        /// <param name="payoffCcy">收益计算币种</param>
        /// <param name="settlementCcy">结算币种</param>
        /// <param name="exerciseDates">行权日</param>
        /// <param name="observationDates">观察日</param>
        /// <param name="notional">名义本金</param>
        /// <param name="settlementGap">结算日规则</param>
        /// <param name="optionPremiumPaymentDate">权利金支付日</param>
        /// <param name="optionPremium">权利金</param>
        /// <param name="isMoneynessOption">是否为相对行权价期权</param>
        /// <param name="initialSpotPrice">标的资产期初价格</param>
        /// <param name="dividends">标的资产分红</param>
        /// <param name="hasNightMarket">标的资产是否有夜盘交易</param>
        /// <param name="commodityFuturesPreciseTimeMode">是否启用精确时间计算模式</param>
        public VanillaOption(Date startDate,
            Date maturityDate,  //underlying maturity,  for option on forward
            OptionExercise exercise,
            OptionType optionType,
            double strike,
            InstrumentType underlyingInstrumentType,
            ICalendar calendar,
            IDayCount dayCount,
            CurrencyCode payoffCcy,
            CurrencyCode settlementCcy,
            Date[] exerciseDates,
            Date[] observationDates,
            double notional = 1,
            DayGap settlementGap = null,
            Date optionPremiumPaymentDate = null,
            double optionPremium = 0.0,
            bool isMoneynessOption = false,
            double initialSpotPrice = 0.0,
            Dictionary<Date, double> dividends = null,
            bool hasNightMarket = false,
            bool commodityFuturesPreciseTimeMode = false
            )
			: base(startDate, maturityDate, exercise, optionType, new double[] { strike }, underlyingInstrumentType, 
                  calendar, dayCount, payoffCcy, settlementCcy, exerciseDates, observationDates, notional, settlementGap, 
                  optionPremiumPaymentDate, optionPremium, 
                  isMoneynessOption : isMoneynessOption, initialSpotPrice: initialSpotPrice, dividends: dividends,  hasNightMarket:hasNightMarket,
                  commodityFuturesPreciseTimeMode: commodityFuturesPreciseTimeMode)
		{
			if (Exercise == OptionExercise.European &&  exerciseDates.Length != 1)
			{
				throw new PricingLibraryException("Vanilla European option can have only 1 observation date");
			}
		}

        /// <summary>
        /// 获取行权收益
        /// </summary>
        /// <param name="price">标的资产价格</param>
        /// <returns>行权收益</returns>
		public override Cashflow[] GetPayoff(double[] price)
		{
			var payoff = 0.0;
            var effectiveStrike = IsMoneynessOption? Strike * InitialSpotPrice : Strike;
            
			switch (OptionType)
			{
				case OptionType.Call:
					payoff = Math.Max(0.0, price[0] - effectiveStrike);
					break;
				case OptionType.Put:
					payoff = Math.Max(0.0, effectiveStrike - price[0]);
					break;
				default:
					throw new PricingBaseException("Unknow/illegal option type!");
			}

			var actualPaymentDate = SettlmentGap.Get(Calendar, UnderlyingMaturityDate);

			return new[]
			{
				new Cashflow(StartDate, UnderlyingMaturityDate, actualPaymentDate, payoff*Notional, PayoffCcy, CashflowType.Net, false, double.NaN, null), 
				//new Cashflow(StartDate, UnderlyingMaturityDate, OptionPremiumPaymentDate ?? actualPaymentDate, OptionPremium, PayoffCcy, CashflowType.Net, true, double.NaN, null)
			};
		}

        /// <summary>
        /// 获取行权收益
        /// </summary>
        /// <param name="pricePath">价格路径</param>
        /// <returns>行权收益</returns>
		public override Cashflow[] GetPayoff(Dictionary<Date, double> pricePath)
		{
			//for European exercise
			return GetPayoff(new[] { pricePath[ExerciseDates[0]] });
		}

        /// <summary>
        /// 根据行权方式拷贝生成一个新的期权
        /// </summary>
        /// <param name="exercise">行权方式</param>
        /// <returns>新的期权</returns>
        public override IOption Clone(OptionExercise exercise) {
            var newExerciseSchedule = (exercise == OptionExercise.European) ? new Date[] { ExerciseDates.Last() } : ExerciseDates;
            return new VanillaOption(
                startDate: this.StartDate,
                maturityDate: this.UnderlyingMaturityDate,
                exercise: exercise,
                optionType: this.OptionType,
                strike: this.Strike,
                underlyingInstrumentType: this.UnderlyingProductType,
                calendar: this.Calendar,
                dayCount: this.DayCount,
                payoffCcy: this.PayoffCcy,
                settlementCcy: this.SettlementCcy,
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
