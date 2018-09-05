namespace Qdp.Pricing.Base.Enums
{
    /// <summary>
    /// 债券类型
    /// </summary>
	public enum BondType
	{
		FixedRateBond,
		FloatingRateBond,
		FixedDateCouponAdjustedBond
	}

    /// <summary>
    /// 利息复利计算方式
    /// </summary>
	public enum CouponCompound
	{
		Simple,
		Compounded
	}

    /// <summary>
    /// 互换方向
    /// </summary>
	public enum SwapDirection
	{
		Payer,
		Receiver
	}

    /// <summary>
    /// 期权看涨看跌
    /// </summary>
	public enum OptionType
	{
		Call,
		Put
	}

    /// <summary>
    /// 持仓买入卖出
    /// </summary>
    public enum Position
    {
        Buy,
        Sell
    }

    /// <summary>
    /// 固定日期利息调整方式
    /// </summary>
    public enum FixedDateAdjustedCouponStyle
	{
		SpecifiedDates,
		Follow
	}

    /// <summary>
    /// 本金摊还方式
    /// </summary>
	public enum AmortizationType
	{
		None = 0,
		EqualPrincipal,
		EqualPrincipalAndInterest,
		Bond,
	}

    /// <summary>
    /// 赎回方式
    /// </summary>
    public enum RedemptionType
    {
        None = 0,
        SeparatePrincipal,
        SeparatePrincipalWithLastCoupon
    }

    /// <summary>
    /// 二元期权收益类型
    /// </summary>
	public enum BinaryOptionPayoffType
	{
		CashOrNothing,
		AssetOrNothing
	}

    /// <summary>
    /// 二元期权补偿方式
    /// </summary>
    public enum BinaryRebateType
    {
        AtHit,
        AtEnd
    }

    /// <summary>
    /// 二元期权复制方式
    /// </summary>
    public enum BinaryOptionReplicationStrategy
	{
		Down = -1,
		Middle = 0,
		Up = 1,
		None = 2
	}

    /// <summary>
    /// 障碍期权类型
    /// </summary>
	public enum BarrierType
	{
		DoubleTouchOut,
		DoubleTouchIn,
		UpAndOut,
		DownAndOut,
		UpAndIn,
		DownAndIn
	}

    /// <summary>
    /// 障碍期权的障碍状态
    /// </summary>
    public enum BarrierStatus
    {
        Monitoring,
        KnockedIn,
        KnockedOut
    }

    /// <summary>
    /// 彩虹期权的彩虹类型
    /// </summary>
    public enum RainbowType
    {
        Max,
        Min,
        BestOfAssetsOrCash,
        BestCashOrNothing,
        WorstCashOrNothing,
        TwoAssetsCashOrNothing,
        TwoAssetsCashOrNothingUpDown,
        TwoAssetsCashOrNothingDownUp,

    }

    /// <summary>
    /// 价差期权的价差类型
    /// </summary>
    public enum SpreadType
    {
        TwoAssetsSpread,
        ThreeAssetsSpread,
        ThreeAssetsSpreadBasket,
        FourAssetsSpread,
        FourAssetsSpreadBasketType1,
        FourAssetsSpreadBasketType2,
    }

    /// <summary>
    /// 亚式期权均价计算方法
    /// </summary>
    public enum AsianType
    {
        DiscreteArithmeticAverage,
        ArithmeticAverage,
        GeometricAverage,
    }

    /// <summary>
    /// 亚式期权行权价类型
    /// </summary>
    public enum StrikeStyle
    {
        Fixed,
        Floating,
    }

    /// <summary>
    /// 行权方式
    /// </summary>
    public enum OptionExercise
	{
		European,
		American,
		Bermudan,
		Asian
	}

    /// <summary>
    /// 重置行权价方式
    /// </summary>
    public enum ResetStrikeType
    {
        PercentagePayoff,
        NormalPayoff
    }

    /// <summary>
    /// 交易市场
    /// </summary>
    public enum TradingMarket
	{
		ChinaExShg,
		ChinaExShe,
		ChinaInterBank,
		OTC,
	    SHFE,
        CFE,
        DCE,
        CZCE
	}

    /// <summary>
    /// 浮息计算方式
    /// </summary>
	public enum FloatingCouponCalcType
	{
		SimpleFrn,
		ZzFrn
	}

    /// <summary>
    /// 交易方向
    /// </summary>
	public enum Direction
	{
		BuyThenSell,
		SellThenBuy
	}
}
