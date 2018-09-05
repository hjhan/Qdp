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
    /// 期权基类
    /// </summary>
	public abstract class OptionBase : IOption
	{
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// 类型名称
        /// </summary>
        public virtual string TypeName { get { return "Option"; } }

        /// <summary>
        /// 开始日期
        /// </summary>
        public Date StartDate { get; private set; }

        /// <summary>
        /// 标的资产到期日
        /// </summary>
		public Date UnderlyingMaturityDate { get; private set; } // the date when the payment is made

        /// <summary>
        /// 结算日规则
        /// </summary>
		public DayGap SettlmentGap { get; private set; } // possible gap between actual payment date to maturity date

        /// <summary>
        /// 名义本金
        /// </summary>
		public double Notional { get; set; }

        /// <summary>
        /// 行权方式
        /// </summary>
		public OptionExercise Exercise { get; private set; }

        /// <summary>
        /// 看涨看跌
        /// </summary>
		public OptionType OptionType { get; private set; }

        /// <summary>
        /// 行权价
        /// </summary>
		public double Strike { get; private set; }

        /// <summary>
        /// 多个标的行权价
        /// </summary>
        public double[] Strikes { get;set; }

        /// <summary>
        /// 交易日历
        /// </summary>
        public ICalendar Calendar { get; private set; }

        /// <summary>
        /// 日期规则
        /// </summary>
		public IDayCount DayCount { get; private set; }

        /// <summary>
        /// 收益计算币种
        /// </summary>
		public CurrencyCode PayoffCcy { get; private set; } // curency unit of payoff calculation

        /// <summary>
        /// 结算币种
        /// </summary>
		public CurrencyCode SettlementCcy { get; private set; } // currency unit of settlement

        /// <summary>
        ///行权日
        /// </summary>
		public Date[] ExerciseDates { get; private set; } // the dates to exercise the option, payoff is calculated on the exercise dates

        /// <summary>
        /// 观察日
        /// </summary>
		public Date[] ObservationDates { get; private set; } // the dates to observe the underlying prices

        /// <summary>
        /// 权利金支付日
        /// </summary>
		public Date OptionPremiumPaymentDate { get; private set; }

        /// <summary>
        /// 权利金
        /// </summary>
		public double OptionPremium { get; private set; }

        /// <summary>
        /// 标的资产代码
        /// </summary>
        public string[] UnderlyingTickers { get; set; }

        /// <summary>
        /// 标的资产类型
        /// </summary>
        public InstrumentType UnderlyingProductType { get; private set; }

        /// <summary>
        /// 是否为相对行权价期权
        /// </summary>
        public bool IsMoneynessOption { get; set; }

        /// <summary>
        /// 标的期初价格
        /// </summary>
        public double InitialSpotPrice { get; set; }

        /// <summary>
        /// 标的资产分红
        /// </summary>
        public Dictionary<Date, double> Dividends { get; set; }

        /// <summary>
        /// 标的资产是否有夜盘交易
        /// </summary>
        public virtual bool HasNightMarket { get; } = false;

        /// <summary>
        /// 是否启用精确时间计算模式
        /// </summary>
        public virtual bool CommodityFuturesPreciseTimeMode { get; } = false;

        protected OptionBase(Date startDate,
            Date maturityDate,
            OptionExercise exercise,
            OptionType optionType,
            double[] strike,
            InstrumentType underlyingInstrumentType,
            ICalendar calendar,
            IDayCount dayCount,
            CurrencyCode payoffCcy,
            CurrencyCode settlementCcy,
            Date[] exerciseDates,
            Date[] observationDates,
            double notional = 1.0,
            DayGap settlementGap = null,
            Date optionPremiumPaymentDate = null,
            double optionPremium = 0.0,
            string[] underlyingTickers = null,
            bool isMoneynessOption = false,
            double initialSpotPrice = 0.0,
            Dictionary<Date, double> dividends = null,
            bool hasNightMarket = false,
            bool commodityFuturesPreciseTimeMode = false)
		{
			StartDate = startDate;
			
			Exercise = exercise;
			OptionType = optionType;
			Strike = strike[0];
            Strikes = strike;
			UnderlyingProductType = underlyingInstrumentType;

			Calendar = calendar;
			DayCount = dayCount;
			SettlmentGap = settlementGap ?? new DayGap("+0BD");
			UnderlyingMaturityDate = SettlmentGap.Get(Calendar, maturityDate);

			ExerciseDates = exerciseDates;
			ObservationDates = observationDates;

            UnderlyingTickers = underlyingTickers;

			Notional = notional;
			PayoffCcy = payoffCcy;
			SettlementCcy = settlementCcy;

			OptionPremiumPaymentDate = optionPremiumPaymentDate;
			OptionPremium = optionPremium;

            IsMoneynessOption = isMoneynessOption;
            InitialSpotPrice = initialSpotPrice;
            Dividends = dividends;

            HasNightMarket = hasNightMarket;
            CommodityFuturesPreciseTimeMode = commodityFuturesPreciseTimeMode;
        }

        /// <summary>
        /// 获取行权收益
        /// </summary>
        /// <param name="price">标的资产价格</param>
        /// <returns>行权收益</returns>
		public abstract Cashflow[] GetPayoff(double[] price);

        /// <summary>
        /// 获取行权收益
        /// </summary>
        /// <param name="pricePath">价格路径</param>
        /// <returns>行权收益</returns>
		public abstract Cashflow[] GetPayoff(Dictionary<Date, double> pricePath);

        /// <summary>
        /// 根据行权方式拷贝生成一个新的期权
        /// </summary>
        /// <param name="exercise">行权方式</param>
        /// <returns>新的期权</returns>
        public abstract IOption Clone(OptionExercise exercise);
    }
}
