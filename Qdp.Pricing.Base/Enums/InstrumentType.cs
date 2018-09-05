/// <summary>
/// Qdp.Pricing.Base.Enums
/// </summary>
namespace Qdp.Pricing.Base.Enums
{
    /// <summary>
    /// 金融工具类型
    /// </summary>
	public enum InstrumentType
	{
		Dummy,
		None,
		Repo,
		Ibor,
		Deposit,
		FixedLeg,
		FloatingLeg,
		InterestRateSwap,
		BasisSwap,
		CommoditySpot,
		CommodityForward,
		Loan,
		Bond,
		FixedRateBond,
		FloatingRateBond,
		FixedDateCouonAdjustedBond,
		CallableBond,
		ConvertibleBond,
		HoldingPeriod,
		Forward,
		BondForward,
		BondFutures,
		FxSpot,
		FxForward,
		FxOption,	
		CreditDefaultSwap,		
		EquityIndex,
		Basket,
		Spot,
		Futures,
		AbsWithRepurchase,
		Stock,
		Option,	
		AsianOption,
		BarrierOption,
		BinaryOption,
		RangeAccrual,
		VanillaOption,
        CommodityFutures
    }
}
