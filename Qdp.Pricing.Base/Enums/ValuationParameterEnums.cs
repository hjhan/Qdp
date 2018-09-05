namespace Qdp.Pricing.Base.Enums
{
    /// <summary>
    /// 二叉树类型
    /// </summary>
	public enum BinomialTreeType
	{
		CoxRossRubinstein,
		LeisenReimer
	}

    /// <summary>
    /// Abs早偿模型
    /// </summary>
	public enum AbsPrepaymentModel
	{
		Psa,
		Cpr
	}

    /// <summary>
    /// Abs违约模型
    /// </summary>
	public enum AbsDefaultModel
	{
		Sda,
		Cdr
	}

    /// <summary>
    /// Mortgage计算方法
    /// </summary>
	public enum MortgageCalcMethod
	{
		Simple,
		TimeWeighted
	}
}
