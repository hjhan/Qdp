using System;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;

/// <summary>
/// Qdp.Pricing.Base.Enums
/// </summary>
namespace Qdp.Pricing.Base.Enums
{
    /// <summary>
    /// 交易日调整规则
    /// </summary>
	public enum BusinessDayConvention
	{
        /// <summary>
        /// 无
        /// </summary>
		None,
        /// <summary>
        /// 遇非交易日调整为下一个交易日
        /// </summary>
		Following,
        /// <summary>
        /// 遇非交易日调整为下一个交易日，但如果下一个交易日与原日期不在同一月份，则调整为上一交易日
        /// </summary>
		ModifiedFollowing,
        /// <summary>
        /// 遇非交易日调整为上一个交易日
        /// </summary>
		Previous,
        /// <summary>
        /// 遇非交易日调整为上一个交易日，但如果下一个交易日与原日期不在同一月份，则调整为下一交易日
        /// </summary>
		ModifiedPrevious
    }

    /// <summary>
    /// 日期规则
    /// </summary>
	public enum DayCount
	{
		Act360,
		Act365,
		ActAct,
		Act365M,
		Act365NoLeap,
		B30360,
		InterBankBond,
		ActActAfb,
		ActActIsma,
		ModifiedAfb,
        Act365Wind,
        Bus244
    }

    /// <summary>
    /// 日期规则模式枚举
    /// </summary>
    public enum DayCountMode
    {
        /// <summary>
        /// 交易日
        /// </summary>
        TradingDay,
        /// <summary>
        /// 自然日（日历日）
        /// </summary>
        CalendarDay
    }

    /// <summary>
    /// 计息残段类型枚举
    /// </summary>
    public enum Stub
	{
		ShortStart,
		ShortEnd,
		LongStart,
		LongEnd
	}

    /// <summary>
    /// 支付频率枚举
    /// </summary>
	public enum Frequency
	{
        /// <summary>
        /// 无
        /// </summary>
		None = -1,

        /// <summary>
        /// 连续
        /// </summary>
		Continuous = 0,

        /// <summary>
        /// 每年
        /// </summary>
		Annual = 1,

        /// <summary>
        /// 每半年
        /// </summary>
		SemiAnnual = 2,

        /// <summary>
        /// 每4个月
        /// </summary>
		SubTriple = 3,

        /// <summary>
        /// 每季度
        /// </summary>
		Quarterly = 4,

        /// <summary>
        /// 每两个月
        /// </summary>
		BiMonthly = 6,

        /// <summary>
        /// 每个月
        /// </summary>
		Monthly = 12,

        /// <summary>
        /// 每周
        /// </summary>
		Weekly = 7,

        /// <summary>
        /// 每四周
        /// </summary>
		FourWeekly = 28,
	}

    /// <summary>
    /// 时段枚举
    /// </summary>
	public enum Period
	{        
		Zero,
		Day,
		Week,
		Month,
		Year
	}

    /// <summary>
    /// 报价类型枚举
    /// </summary>
	public enum PriceQuoteType
	{
		Clean,
		Dirty,
		Ytm,
		YtmExecution,
		YtmCallExecution,
		YtmPutExecution,
		Irr,
		Basis,
		NetBasis,
		NetPnl,
		None
	}

    /// <summary>
    /// 日历枚举值
    /// </summary>
	public enum Calendar
	{
        /// <summary>
        /// 中国交易所日历
        /// </summary>
		Chn,
        /// <summary>
        /// 中国银行间日历
        /// </summary>
		Chn_ib,
        /// <summary>
        /// 美元日历
        /// </summary>
		Usd,
        /// <summary>
        /// 欧元日历
        /// </summary>
		Eur,
        /// <summary>
        /// 日元日历
        /// </summary>
		Jpy,
        /// <summary>
        /// 港币日历
        /// </summary>
		Hkd,
        /// <summary>
        /// 英镑日历
        /// </summary>
		Gbp,
        /// <summary>
        /// 瑞士法郎日历
        /// </summary>
		Chf,
        /// <summary>
        /// 澳元日历
        /// </summary>
		Aud,
        /// <summary>
        /// 新加坡元日历
        /// </summary>
		Sgd,
        /// <summary>
        /// 加元日历
        /// </summary>
		Cad,
        /// <summary>
        /// 新西兰元日历
        /// </summary>
		Nzd,
        /// <summary>
        /// 马来西亚林吉特日历
        /// </summary>
		Myr,
        /// <summary>
        /// 卢布日历
        /// </summary>
		Rub,
        /// <summary>
        /// 泰铢日历
        /// </summary>
		Thb
	}

    public enum CalendarConvention
	{
		Adjusted,
		Business
	}

    /// <summary>
    /// 指数类型
    /// </summary>
	public enum IndexType
	{
		None,
		Fr001,
		Fr007,
		Fr014,
		Shibor1D,
		Shibor1W,
		Shibor2W,
		Shibor1M,
		Shibor3M,
		Shibor6M,
		Shibor9M,
		Shibor1Y,
		Depo3M,
		Depo6M,
		Depo1Y,
		Depo2Y,
		Depo3Y,
		Depo5Y,
		B1D,
		B1W,
		B2W,
		B1M,
		B2M,
		B3M,
		B6M,
		B_1W,
		B_2W,
		B_1M,
		B_2M,
		B_3M,
		B_6M,
		Lrb6M,
		Lrb1Y,
		Lrb1Y3Y,
		Lrb3Y5Y,
		Lrb5Y,
		UsdLibor1D,
		UsdLibor1W,
		UsdLibor2W,
		UsdLibor1M,
		UsdLibor2M,
		UsdLibor3M,
		UsdLibor4M,
		UsdLibor5M,
		UsdLibor6M,
		UsdLibor7M,
		UsdLibor8M,
		UsdLibor9M,
		UsdLibor10M,
		UsdLibor11M,
		UsdLibor12M,
		R_001,
		R_002,
		R_003,
		R_004,
		R_007,
		R_014,
		R_028,
		R_091,
		R_182,
		R001,
		R007,
		R014,
		R021,
		R1M,
		R2M,
		R3M,
		R4M,
		R6M,
		R9M,
		R1Y,
		GC001,
		GC002,
		GC003,
		GC004,
		GC007,
		GC014,
		GC028,
		GC091,
		GC182,
		Libor3M,
		Spc,
		Spot,
		SwapParRate,
		Gold,
		Commodity,
		ConvenienceYield,
		Cdc_GK,
		Regridded,
        /// <summary>
        /// 个人住房公积金贷款(五年以上)
        /// </summary>
		Gjj5YAbove,
        /// <summary>
        /// 个人住房公积金贷款(五年以下)
        /// </summary>
        Gjj5Y,
		CommercialMortgage1M6M,
		CommercialMortgage6M,
		CommercialMortgage1Y,
		CommercialMortgage1Y3Y,
		CommercialMortgage3Y5Y,
		CommercialMortgage5YAbove,

		Customized
	}

    /// <summary>
    /// 币种枚举
    /// </summary>
	public enum CurrencyCode
	{
		CNY,
		USD,
		EUR,
		JPY,
		HKD,
		GBP,
		AUD,
		CHF,
		SGD,
		CAD,
		NZD,
		MYR,
		RUB,
		THB
	}

    /// <summary>
    /// 收益率曲线类型枚举
    /// </summary>
	public enum YieldCurveTrait
	{
		SpotCurve,
		ForwardCurve,
		DiscountCurve,
	}

    /// <summary>
    /// 曲线拟合方法枚举
    /// </summary>
	public enum MktInstrumentCalibMethod
	{
		IrsFloatingPvConst1,
		IrsFloatingPvReal,
		IrsNetPv,
		Default,
	}
	#region extensions to create instance from enums

    /// <summary>
    /// DayCount扩展类
    /// </summary>
	public static class DayCountExtension
	{
        /// <summary>
        /// 根据日期规则枚举值获得相应的日期规则对象
        /// </summary>
        /// <param name="daycount">日期规则</param>
        /// <returns>日期规则对象</returns>
		public static IDayCount Get(this DayCount daycount)
		{
			switch (daycount)
			{
				case DayCount.Act360:
					return new Act360();
				case DayCount.Act365:
					return new Act365();
				case DayCount.Act365M:
					return new Act365M();
				case DayCount.ActAct:
					return new ActAct();
				case DayCount.InterBankBond:
					return new InterBankBond();
				case DayCount.ActActAfb:
					return new ActActAfb();
				case DayCount.B30360:
					return new B30360();
				case DayCount.ActActIsma:
					return new ActActIsma();
				case DayCount.Act365NoLeap:
					return new Act365NoLeap();
                case DayCount.ModifiedAfb:
					return new ModifiedAfb();
                case DayCount.Act365Wind:
                    return new Act365Wind();
                case DayCount.Bus244:
                    return new Bus244();
                default:
					return new Act365Wind();
			}
		}
	}

    /// <summary>
    /// 时段扩展类
    /// </summary>
	public static class PeriodExtension
	{
        /// <summary>
        /// 根据时段字符串获得相应的时段对象，不区分大小写
        /// Z - Zero
        /// D - Day
        /// W - Week
        /// M - Month
        /// Y - Year
        /// </summary>
        /// <param name="period">时段字符串</param>
        /// <returns>时段对象</returns>
		public static Period ToPeriod(this string period)
		{
			switch (period.ToLower())
			{
				case "z":
					return Period.Zero;
				case "d":
					return Period.Day;
				case "w":
					return Period.Week;
				case "m":
					return Period.Month;
				case "y":
					return Period.Year;
			}
			throw new PricingBaseException("Unrecognized period value, use z/d/w/m/y");
		}

        /// <summary>
        /// 时段对象的字符串简称：
        /// Zero - Z
        /// Day - D
        /// Week - W
        /// Month - M
        /// Year - Y
        /// </summary>
        /// <param name="period">时段对象</param>
        /// <returns>字符串简称</returns>
		public static string ToShortName(this Period period)
		{
			return period.ToString().Substring(0, 1);
		}
	}

    /// <summary>
    /// 支付频率的扩展类
    /// </summary>
	public static class FrequencyExtension
	{
        /// <summary>
        /// 根据支付频率获得对应的期限。
        /// None - Infinity
        /// Continuous - 1Z
        /// Annual - 1Y
        /// SemiAnnual - 6M
        /// Quarterly - 3M
        /// SubTriple - 4M
        /// BiMonthly - 2M
        /// Monthly - 1M
        /// Weekly - 1W
        /// FourWeekly - 4W
        /// </summary>
        /// <param name="frequency">支付频率</param>
        /// <returns>期限</returns>
		public static ITerm GetTerm(this Frequency frequency)
		{
			switch (frequency)
			{
				case Frequency.None:
					return Term.Infinity;
				case Frequency.Continuous:
					return new Term(1, Period.Zero);
				case Frequency.Annual:
					return new Term(1, Period.Year);
				case Frequency.SemiAnnual:
					return new Term(6, Period.Month);
				case Frequency.Quarterly:
					return new Term(3, Period.Month);
				case Frequency.SubTriple:
					return new Term(4, Period.Month);
                case Frequency.BiMonthly:
                    return new Term(2, Period.Month);
				case Frequency.Monthly:
					return new Term(1, Period.Month);
				case Frequency.Weekly:
					return new Term(1, Period.Week);
				case Frequency.FourWeekly:
					return new Term(4, Period.Week);
				default:
					throw new ArgumentOutOfRangeException("frequency");
			}
		}

        /// <summary>
        /// 支付频率对应的一年中支付的次数
        /// </summary>
        /// <param name="frequency">支付频率</param>
        /// <param name="daysInYear">一年按多少天计算。会影响Weekly和FourWeekly的计算</param>
        /// <returns>计算结果</returns>
		public static double CountPerYear(this Frequency frequency, double daysInYear = 365.25)
		{
			switch (frequency)
			{
				case Frequency.None:
					return 0;
				case Frequency.Continuous:
					return Int32.MaxValue;
				case Frequency.Annual:
				case Frequency.SemiAnnual:
				case Frequency.Quarterly:
				case Frequency.SubTriple:
				case Frequency.Monthly:
					return (double)frequency;
				case Frequency.Weekly:
				case Frequency.FourWeekly:
					return daysInYear / (int)frequency;
				default:
					throw new ArgumentOutOfRangeException("frequency");
			}
		}
	}

    /// <summary>
    /// 指数的扩展类
    /// </summary>
	public static class IndexExtension
	{
        /// <summary>
        /// 获得指数对应的远期期限
        /// </summary>
        /// <param name="index">指数</param>
        /// <returns>远期期限</returns>
		public static ITerm ForwardTerm(this IndexType index)
		{
			switch (index)
			{

				case IndexType.Depo3M:
					return new Term("3M");
				case IndexType.Depo6M:
					return new Term("6M");
				case IndexType.Depo1Y:
					return new Term("1Y");
				case IndexType.Depo2Y:
					return new Term("2Y");
				case IndexType.Depo3Y:
					return new Term("3Y");
				case IndexType.Depo5Y:
					return new Term("5Y");

				case IndexType.Shibor1D:
					return new Term("1D");
				case IndexType.Shibor1W:
					return new Term("1W");
				case IndexType.Shibor2W:
					return new Term("2W");
				case IndexType.Shibor1M:
					return new Term("1M");
				case IndexType.Shibor3M:
					return new Term("3M");
				case IndexType.Shibor6M:
					return new Term("6M");
				case IndexType.Shibor9M:
					return new Term("9M");
				case IndexType.Shibor1Y:
					return new Term("1Y");

				case IndexType.Fr001:
					return new Term("1D");
				case IndexType.Fr007:
					return new Term("7D");

				case IndexType.B1M:
					return new Term("1M");
				case IndexType.B1W:
					return new Term("1W");
				case IndexType.B2W:
					return new Term("2W");
				case IndexType.B_1M:
					return new Term("1M");
				case IndexType.B_1W:
					return new Term("1W");
				case IndexType.B_2W:
					return new Term("2W");

				case IndexType.Lrb1Y:
					return new Term("1Y");
				case IndexType.Lrb3Y5Y:
					return new Term("3Y");
				case IndexType.Lrb1Y3Y:
					return new Term("3Y");
				case IndexType.Lrb5Y:
					return new Term("5Y");
				case IndexType.Cdc_GK:
					return new Term("5Y");
				case IndexType.UsdLibor3M:
					return new Term("3M");
				case IndexType.UsdLibor6M:
					return new Term("6M");

				default:
					throw new PricingBaseException("Unrecognized index");
			}
		}

        /// <summary>
        /// 获得指数的平均天数
        /// </summary>
        /// <param name="index">指数</param>
        /// <returns>平均天数</returns>
		public static int AverageDays(this IndexType index)
		{
			switch (index)
			{
				case IndexType.B1M:
					return 30;
				case IndexType.B1W:
					return 7;
				case IndexType.B2W:
					return 14;
				case IndexType.B_1M:
					return 30;
				case IndexType.B_1W:
					return 7;
				case IndexType.B_2W:
					return 14;
				default:
					return 1;
			}
		}

        /// <summary>
        /// 获得指数的日期规则名称
        /// </summary>
        /// <param name="index">指数</param>
        /// <returns>日期规则</returns>
		public static string DayCountEnum(this IndexType index)
		{
			switch (index)
			{
				case IndexType.Depo1Y:
				case IndexType.Depo3M:
				case IndexType.Depo6M:
				case IndexType.Depo2Y:
				case IndexType.Depo3Y:
				case IndexType.Depo5Y:
					return "B30360";

				case IndexType.Shibor1D:
				case IndexType.Shibor1W:
				case IndexType.Shibor2W:
				case IndexType.Shibor1M:
				case IndexType.Shibor3M:
				case IndexType.Shibor6M:
				case IndexType.Shibor9M:
				case IndexType.Shibor1Y:
					return "Act360";

				case IndexType.Fr001:
				case IndexType.Fr007:
					return "Act365";

				case IndexType.B1M:
				case IndexType.B1W:
				case IndexType.B2W:
				case IndexType.B_1M:
				case IndexType.B_1W:
				case IndexType.B_2W:
					return "Act365";

				default:
					return "Act365";
			}
		}

        /// <summary>
        /// 获得指数的日期规则
        /// </summary>
        /// <param name="index">指数</param>
        /// <returns>日期规则</returns>
		public static IDayCount DayCount(this IndexType index)
		{
			switch (index)
			{
				case IndexType.Depo1Y:
				case IndexType.Depo3M:
				case IndexType.Depo6M:
				case IndexType.Depo2Y:
				case IndexType.Depo3Y:
				case IndexType.Depo5Y:
					return new B30360();

				case IndexType.Shibor1D:
				case IndexType.Shibor1W:
				case IndexType.Shibor2W:
				case IndexType.Shibor1M:
				case IndexType.Shibor3M:
				case IndexType.Shibor6M:
				case IndexType.Shibor9M:
				case IndexType.Shibor1Y:
					return new Act360();

				case IndexType.Fr001:
				case IndexType.Fr007:
					return new Act365();

				case IndexType.B1M:
				case IndexType.B1W:
				case IndexType.B2W:
				case IndexType.B_1M:
				case IndexType.B_1W:
				case IndexType.B_2W:
					return new Act365();

				default:
					return new Act365();
			}
		}

        /// <summary>
        /// 获得指数的远期复利方式
        /// </summary>
        /// <param name="index">指数</param>
        /// <returns>远期复利方式</returns>
		public static Compound ForwardCompound(this IndexType index)
		{
			switch (index)
			{
				case IndexType.Fr007:
				case IndexType.Shibor1D:
				case IndexType.Shibor3M:
				case IndexType.Depo1Y:
					return Compound.Simple;
				default:
					throw new PricingBaseException("Unrecognized index");
			}
		}
	}
	#endregion
}
