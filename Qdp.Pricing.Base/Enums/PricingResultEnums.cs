/// <summary>
/// Qdp.Pricing.Base.Enums
/// </summary>
namespace Qdp.Pricing.Base.Enums
{
    /// <summary>
    /// 现金流类型枚举
    /// </summary>
	public enum CashflowType
	{
		Coupon,
		Principal,
		TerminationFee,
		FixedLegInterest,
		FloatingLegInterest,
		Net,
		Gross,
		Prepayment,
		PrincipalLossOnDefault,
		Repurchase,
		Tax,
		ManagementFee,
		TrustFee,
	}

    /// <summary>
    /// 现值类型枚举
    /// </summary>
	public enum PvType
	{
		Component,
		Net
	}

    /// <summary>
    /// 风险值类型枚举
    /// </summary>
	public enum RiskType
	{
		Component,
		Net
	}
}
