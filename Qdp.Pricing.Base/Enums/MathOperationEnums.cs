using System;

/// <summary>
/// Qdp.Pricing.Base.Enums
/// </summary>
namespace Qdp.Pricing.Base.Enums
{
    /// <summary>
    /// 平均值计算类型
    /// </summary>
	public enum Average
	{
        /// <summary>
        /// 算术平均
        /// </summary>
		Arithmetic,
        /// <summary>
        /// 几何平均
        /// </summary>
		Geometric
	}

    /// <summary>
    /// 插值方法
    /// </summary>
	public enum Interpolation
	{
		Linear,
		LogLinear,
		CubicSpline,
		LinearCubicSpline,
		LogCubicSpline,
		CubicHermiteMonotic,
		CubicHermiteFd,
		ForwardFlat,
		ConvexMonotic,
		ExponentialSpline,
	}

    /// <summary>
    /// 二维插值方法
    /// </summary>
	public enum Interpolation2D
	{
		BiLinear,
        BiCubicSpline,
        VarianceBiLinear,
        VarianceBiCubicSpline,
    }

    /// <summary>
    /// 外推方法
    /// </summary>
	public enum Extrapolation
	{
        /// <summary>
        /// 水平
        /// </summary>
		Flat,
        /// <summary>
        /// 自然指数
        /// </summary>
		Natural
	}

    /// <summary>
    /// 复利方法
    /// </summary>
	public enum Compound
	{
        /// <summary>
        /// 连续复利
        /// </summary>
		Continuous = 0,
        /// <summary>
        /// 单利
        /// </summary>
		Simple,
        /// <summary>
        /// 半年复利
        /// </summary>
		SemiAnnual,
        /// <summary>
        /// 年复利
        /// </summary>
		Annual,
	}

    /// <summary>
    /// 波动率曲面类型
    /// </summary>
    public enum VolSurfaceType
    {
        /// <summary>
        /// 绝对行权价
        /// </summary>
        StrikeVol = 0,
        /// <summary>
        /// 相对行权价
        /// </summary>
        MoneynessVol,
    }

    public static class CompoundExtension
	{
		public static double CalcCompoundRate(this Compound compound, double t, double rate)
		{
			switch (compound)
			{
				case Compound.Continuous:
					return Math.Exp(rate * t);
				case Compound.Annual:
					return Math.Pow(1 + rate, t);
				case Compound.Simple:
					return 1 + rate * t;
				case Compound.SemiAnnual:
					return Math.Pow(1 + rate / 2.0, 2.0 * t);
			}

			return 1;
		}

		public static double CalcRateFromDf(this Compound compound, double df, double t)
		{
            if (t == 0.0)
                return df;

			switch (compound)
			{
				case Compound.Continuous:
					return -Math.Log(df) / t;
				case Compound.Annual:
					return Math.Pow(1.0 / df, 1.0 / t) - 1.0;
				case Compound.Simple:
					return (1.0 / df - 1.0) / t;
				case Compound.SemiAnnual:
					return (Math.Pow(1.0 / df, 1.0 / (2.0 * t)) - 1.0) * 2.0;
			}

			return 1;
		}

        public static double CalcDfFromZeroRate(this Compound compound, double rate, double t) {
            switch (compound)
            {
                case Compound.Continuous:
                    return  Math.Exp(-1*rate*t);
                case Compound.Annual:
                    return  1.0 / Math.Pow(1.0 + rate,  t);
                case Compound.Simple:
                    return  1.0 / ( t* rate + 1.0 );
                case Compound.SemiAnnual:
                    return  1.0 / Math.Pow(rate/2.0 + 1, 2*t);
            }
            return 1.0;
        }

	}
}
