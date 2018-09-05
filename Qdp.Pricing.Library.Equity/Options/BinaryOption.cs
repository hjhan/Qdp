using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Equity.Interfaces;
using System;

/// <summary>
/// Qdp.Pricing.Library.Equity.Options
/// </summary>
namespace Qdp.Pricing.Library.Equity.Options
{
    /// <summary>
    /// 二元期权
    /// </summary>
	public class BinaryOption : OptionBase
	{
        /// <summary>
        /// 类型名称
        /// </summary>
        public override string TypeName { get { return "BinaryOption"; } }

        /// <summary>
        /// 二元期权收益类型
        /// </summary>
        public BinaryOptionPayoffType BinaryOptionPayoffType { get; private set; }

        /// <summary>
        /// 现金金额
        /// </summary>
		public double CashOrNothingAmount { get; private set; }

        /// <summary>
        /// 二元期权补偿方式
        /// </summary>
        public BinaryRebateType BinaryRebateType { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="maturityDate">到期日</param>
        /// <param name="exercise">行权方式</param>
        /// <param name="optionType">看涨看跌</param>
        /// <param name="strike">行权价</param>
        /// <param name="underlyingProductType">标的资产类型</param>
        /// <param name="binaryOptionPayoffType">二元期权收益类型</param>
        /// <param name="cashOrNothingAmount">现金金额</param>
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
        /// <param name="binaryRebateType">二元期权补偿方式</param>
        /// <param name="hasNightMarket">标的资产是否有夜盘交易</param>
        /// <param name="commodityFuturesPreciseTimeMode">是否启用精确时间模式</param>
        public BinaryOption(Date startDate, 
			Date maturityDate, 
			OptionExercise exercise, 
			OptionType optionType, 
			double strike,
			InstrumentType underlyingProductType,
			BinaryOptionPayoffType binaryOptionPayoffType,
			double cashOrNothingAmount,
			ICalendar calendar, 
			IDayCount dayCount,
			CurrencyCode payoffCcy,
			CurrencyCode settlementCcy, 
			Date[] exerciseDates, 
			Date[] observationDates,
			double notional = 1, 
			DayGap settlementGap = null, 
			Date optionPremiumPaymentDate = null, 
			double optionPremium = 0,
            bool isMoneynessOption = false,
            double initialSpotPrice = 0.0,
            Dictionary<Date, double> dividends = null,
            BinaryRebateType binaryRebateType = BinaryRebateType.AtHit,
            bool hasNightMarket = false,
            bool commodityFuturesPreciseTimeMode = false)
			: base(startDate, maturityDate, exercise, optionType,  new double[] { strike } , underlyingProductType, calendar, dayCount, 
                  payoffCcy, settlementCcy, exerciseDates, observationDates, notional, settlementGap, 
                  optionPremiumPaymentDate, optionPremium, 
                  isMoneynessOption: isMoneynessOption, initialSpotPrice: initialSpotPrice, dividends: dividends, hasNightMarket: hasNightMarket,
                  commodityFuturesPreciseTimeMode: commodityFuturesPreciseTimeMode)
		{
			if (exercise==OptionExercise.European && ExerciseDates.Length != 1)
			{
				throw new PricingLibraryException("Binary option cannot have more than 1 exercise dates!");
			}
			BinaryOptionPayoffType = binaryOptionPayoffType;
			CashOrNothingAmount = cashOrNothingAmount;
            BinaryRebateType = binaryRebateType;
		}

        /// <summary>
        /// 获取行权收益
        /// </summary>
        /// <param name="price">标的资产价格</param>
        /// <returns>行权收益</returns>
		public override Cashflow[] GetPayoff(double[] price)
		{
			double amount;
            var effectiveStrike = IsMoneynessOption ? Strike * InitialSpotPrice : Strike;

            if (BinaryOptionPayoffType == BinaryOptionPayoffType.AssetOrNothing)
			{
				if (OptionType == OptionType.Call)
				{
					amount = price[0] > effectiveStrike ? price[0] : 0.0;
				}
				else if (OptionType == OptionType.Put)
				{
					amount = price[0] < effectiveStrike ? price[0] : 0.0;
				}
				else
				{
					throw new PricingLibraryException( "Unknown option type" + OptionType);
				}
			}
			else if (BinaryOptionPayoffType == BinaryOptionPayoffType.CashOrNothing)
			{
				if (OptionType == OptionType.Call)
				{
					amount = (price[0] > Strike ? 1.0 : 0.0)*CashOrNothingAmount;
				}
				else if (OptionType == OptionType.Put)
				{
					amount = (price[0] < Strike ? 1.0 : 0.0) * CashOrNothingAmount;
				}
				else
				{
					throw new PricingLibraryException("Unknown option type" + OptionType);
				}
			}
			else
			{
				throw new PricingLibraryException("Unknown binary option payoff type" + BinaryOptionPayoffType);
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
