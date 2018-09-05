using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Equity.Interfaces;

/// <summary>
/// Qdp.Pricing.Library.Equity.Options
/// </summary>
namespace Qdp.Pricing.Library.Equity.Options
{
    /// <summary>
    /// 障碍期权
    /// </summary>
	public class BarrierOption : OptionBase
	{
        /// <summary>
        /// 类型名称
        /// </summary>
        public override string TypeName { get { return "BarrierOption"; } }

        /// <summary>
        /// 障碍价格。如果是双重障碍，则是障碍下限
        /// </summary>
        public double Barrier { get; private set; } //default to LowerBarrier in case of double barrier

        /// <summary>
        /// 障碍上限价格
        /// </summary>
		public double UpperBarrier { get; private set; }

        /// <summary>
        /// 障碍类型
        /// </summary>
		public BarrierType BarrierType { get; private set; }

        /// <summary>
        /// 是否为离散观察
        /// </summary>
        public bool IsDiscreteMonitored { get; private set; }

        /// <summary>
        /// 名义本金有效比例
        /// </summary>
        public double ParticipationRate { get; private set; } // participation rate

        /// <summary>
        /// 敲出后补偿收益
        /// </summary>
		public double Rebate { get; private set; } // payoff if knocked out

        /// <summary>
        /// 敲出后额外收益
        /// </summary>
		public double Coupon { get; private set; } // additional payoff if not knocked out

        /// <summary>
        /// 观察价格序列
        /// </summary>
		public Dictionary<Date, double> Fixings { get; private set; }

        /// <summary>
        /// 持仓方向
        /// </summary>
        public Position Position { get; private set; } 
        
        /// <summary>
        /// 障碍价格偏移
        /// </summary>
        public double BarrierShift { get; private set; }

        /// <summary>
        /// 敲入敲出状态
        /// </summary>
        public BarrierStatus BarrierStatus { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="maturityDate">到期日</param>
        /// <param name="exercise">行权方式</param>
        /// <param name="optionType">看涨看跌</param>
        /// <param name="strike">行权价</param>
        /// <param name="rebate">敲出补偿收益</param>
        /// <param name="coupon">敲出额外收益</param>
        /// <param name="participationRate">名义本金有效比例</param>
        /// <param name="barrierType">障碍类型</param>
        /// <param name="lowerBarrier">障碍价格下限</param>
        /// <param name="upperBarrier">障碍价格上限</param>
        /// <param name="isDiscreteMonitored">是否离散观察</param>
        /// <param name="underlyingType">标的资产类型</param>
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
        /// <param name="position">持仓方向</param>
        /// <param name="barrierShift">障碍价格偏移</param>
        /// <param name="barrierStatus">敲入敲出状态</param>
        /// <param name="hasNightMarket">标的资产是否有夜盘交易</param>
        /// <param name="commodityFuturesPreciseTimeMode">是否启用精确时间模式</param>
        public BarrierOption(Date startDate,
            Date maturityDate,
            OptionExercise exercise,
            OptionType optionType,
            double strike,
            double rebate,
            double coupon,
            double participationRate,
            BarrierType barrierType,
            double lowerBarrier,
            double upperBarrier,
            bool isDiscreteMonitored,
			InstrumentType underlyingType, 
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
            Position position = Position.Buy,
            double barrierShift = 0.0,
            BarrierStatus barrierStatus = BarrierStatus.Monitoring,
            bool hasNightMarket = false,
            bool commodityFuturesPreciseTimeMode = false
            )
			: base(startDate, maturityDate, exercise, optionType, new double[] { strike }, underlyingType, calendar, dayCount, 
                  payoffCcy, settlementCcy, exerciseDates, observationDates, notional, settlementGap, 
                  optionPremiumPaymentDate, optionPremium, 
                  isMoneynessOption: isMoneynessOption, initialSpotPrice: initialSpotPrice, dividends: dividends, hasNightMarket: hasNightMarket,
                  commodityFuturesPreciseTimeMode: commodityFuturesPreciseTimeMode)
		{
			Rebate = rebate;
			Coupon = coupon;
			ParticipationRate = participationRate;
			BarrierType = barrierType;
			Barrier = lowerBarrier;
            IsDiscreteMonitored = isDiscreteMonitored;
            UpperBarrier = upperBarrier;
			Fixings = fixings;
            Position = position;
            BarrierShift = barrierShift;
            BarrierStatus = barrierStatus;

        }

        /// <summary>
        /// 获取行权收益
        /// </summary>
        /// <param name="price">标的资产价格</param>
        /// <returns>行权收益</returns>
		public override Cashflow[] GetPayoff(double[] price)
		{
            var actualPaymentDate = SettlmentGap.Get(Calendar, UnderlyingMaturityDate);
            double payoff;

            double vanillaPayoff;
            var effectiveStrike = IsMoneynessOption ? Strike * InitialSpotPrice : Strike;
            switch (OptionType)
            {
                case OptionType.Call:
                    vanillaPayoff = Math.Max(price[0] - effectiveStrike, 0.0) * ParticipationRate + Coupon;
                    break;
                case OptionType.Put:
                    vanillaPayoff = Math.Max(effectiveStrike - price[0], 0.0) * ParticipationRate + Coupon;
                    break;
                default:
                    throw new PricingBaseException("Unknow/illegal option type!");
            }

            if (BarrierStatus == BarrierStatus.Monitoring)
            {
                switch (BarrierType)
                {
                    case BarrierType.UpAndIn:
                    case BarrierType.DownAndIn:
                        payoff = Rebate;
                        break;
                    default:
                        payoff = vanillaPayoff;
                        break;
                }
            }
            else if (BarrierStatus == BarrierStatus.KnockedOut)
            {
                switch (BarrierType)
                {
                    case BarrierType.UpAndOut:
                    case BarrierType.DownAndOut:
                        payoff = Rebate;
                        break;
                    default:
                        payoff = 0.0;
                        break;
                }
            }
            else {
                payoff = vanillaPayoff;
                //if knocked in, barrier reduce to vanilla
            }
		
			return new[]
			{
				new Cashflow(StartDate, UnderlyingMaturityDate, actualPaymentDate, payoff*Notional, PayoffCcy, CashflowType.Net, false, double.NaN,null),
				//new Cashflow(StartDate, UnderlyingMaturityDate, OptionPremiumPaymentDate ?? actualPaymentDate, OptionPremium, PayoffCcy, CashflowType.Net, true, double.NaN,null)
			};
		}

        /// <summary>
        /// 获取行权收益
        /// </summary>
        /// <param name="pricePath">价格路径</param>
        /// <returns>行权收益</returns>
		public override Cashflow[] GetPayoff(Dictionary<Date, double> pricePath)
		{
			var prices = new SortedDictionary<Date, double>(pricePath);

			//merges fixings and monte carlo path
			foreach (var key in Fixings.Keys)
			{
				prices[key] = Fixings[key];
			}

			var simulatedValuesToCheck = ObservationDates.Select(x => prices[x]).ToArray();
			var actualPaymentDate = SettlmentGap.Get(Calendar, UnderlyingMaturityDate);
			var voidCf = new[]
			{
				new Cashflow(StartDate, UnderlyingMaturityDate, actualPaymentDate, Rebate*Notional, PayoffCcy, CashflowType.Net, false, double.NaN,null),
				new Cashflow(StartDate, UnderlyingMaturityDate, OptionPremiumPaymentDate ?? actualPaymentDate, OptionPremium, PayoffCcy, CashflowType.Net, true, double.NaN,null)
			};
			switch (BarrierType)
			{
				case BarrierType.UpAndOut:
					return simulatedValuesToCheck.Any(val => val > Barrier)
						? voidCf
						: GetPayoff(new[] { prices.Last().Value });
				case BarrierType.UpAndIn:
					return simulatedValuesToCheck.Any(val => val > Barrier)
						? GetPayoff(new[] { prices.Last().Value })
						: voidCf;
				case BarrierType.DownAndOut:
					return simulatedValuesToCheck.Any(val => val < Barrier)
						? voidCf
						: GetPayoff(new[] { prices.Last().Value });
				case BarrierType.DownAndIn:
					return simulatedValuesToCheck.Any(val => val < Barrier)
						? GetPayoff(new[] { prices.Last().Value })
						: voidCf;
				case BarrierType.DoubleTouchOut:
					return simulatedValuesToCheck.Any(val => (val > UpperBarrier || val < Barrier))
						? voidCf
						: GetPayoff(new[] { prices.Last().Value });
				case BarrierType.DoubleTouchIn:
					return simulatedValuesToCheck.Any(val => (val > UpperBarrier || val < Barrier))
						? GetPayoff(new[] { prices.Last().Value })
						: voidCf;
				default:
					return voidCf;
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
