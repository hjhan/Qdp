using System.Collections.Generic;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;

namespace Qdp.Pricing.Base.Interfaces
{
    /// <summary>
    /// 计算结果接口
    /// </summary>
	public interface IPricingResult
	{
        /// <summary>
        /// 估值日期
        /// </summary>
		Date ValuationDate { get; }

        /// <summary>
        /// 计算所花费的实际，单位为毫秒
        /// </summary>
		double CalcTimeInMilliSecond { get; }

        /// <summary>
        /// 现值
        /// </summary>
		double Pv { get; }

        /// <summary>
        /// 现值现价比
        /// </summary>
        double PctPv { get; }

        /// <summary>
        /// 基点价值（利率曲线变动）
        /// </summary>
        double Dv01 { get; } //基点，利率曲线变动

        /// <summary>
        /// 基点价值（固定端利率变动）
        /// </summary>
		double Pv01 { get; } //固定端利率变动

        /// <summary>
        /// 应计利息
        /// </summary>
		double Ai { get; }

        /// <summary>
        /// 应计利息天数
        /// </summary>
		int AiDays { get; }

        /// <summary>
        /// 到期收益率
        /// </summary>
		double Ytm { get; }

        /// <summary>
        /// 全价
        /// </summary>
		double DirtyPrice { get; }

        /// <summary>
        /// 净价
        /// </summary>
		double CleanPrice { get; }

        /// <summary>
        /// Delta
        /// </summary>
		double Delta { get; }

        /// <summary>
        /// Gamma
        /// </summary>
		double Gamma { get; }

        /// <summary>
        /// Delta * 价格
        /// </summary>
        double DeltaCash { get; }

        /// <summary>
        /// Gamma * 价格
        /// </summary>
        double GammaCash { get; }

        /// <summary>
        /// Rho
        /// </summary>
        double Rho { get; }

        /// <summary>
        /// RhoForeign
        /// </summary>
		double RhoForeign { get; }

        /// <summary>
        /// Theta
        /// </summary>
		double Theta { get; }

        /// <summary>
        /// Theta盈亏
        /// </summary>
        double ThetaPnL { get; }

        /// <summary>
        /// 自然日Theta盈亏
        /// </summary>
        double CalenderThetaPnL { get; }

        /// <summary>
        /// Vega
        /// </summary>
        double Vega { get; }

        /// <summary>
        /// FwdDiffVega
        /// </summary>
        double FwdDiffVega { get; }

        /// <summary>
        /// 标的基点价值
        /// </summary>
        double Dv01Underlying { get; }

        /// <summary>
        /// 麦考利久期
        /// </summary>
		double MacDuration { get; }

        /// <summary>
        /// 修正久期
        /// </summary>
		double ModifiedDuration { get; }

        /// <summary>
        /// 凸性
        /// </summary>
		double Convexity { get; }

        /// <summary>
        /// 修正久期 * 全价
        /// </summary>
        double DollarModifiedDuration { get; }   // modifiedDuration

        /// <summary>
        /// 凸性 * 全价
        /// </summary>
        double DollarConvexity { get; }  //
        //double Sp01 { get; }

        /// <summary>
        /// Carry
        /// </summary>
        double Carry { get; }

        /// <summary>
        /// ZeroSpread
        /// </summary>
        double ZeroSpread { get; }

        /// <summary>
        /// FairQuote
        /// </summary>
		double FairQuote { get; }

        /// <summary>
        /// CallDate
        /// </summary>
		Date CallDate { get; set; }

        /// <summary>
        /// YieldToCall
        /// </summary>
		double YieldToCall { get; set; }

        /// <summary>
        /// PutDate
        /// </summary>
		Date PutDate { get; set; }

        /// <summary>
        /// YieldToPut
        /// </summary>
		double YieldToPut { get; set; }

        /// <summary>
        /// 关键点基点价值
        /// </summary>
		Dictionary<string, CurveRisk[]> KeyRateDv01 { get; }

        /// <summary>
        /// 现金流
        /// </summary>
		Cashflow[] Cashflows { get; }

        /// <summary>
        /// 现金流
        /// </summary>
		Dictionary<string, double> CashflowDict { get; }

        /// <summary>
        /// ComponentPvs
        /// </summary>
		ComponentPv[] ComponentPvs { get; }

        /// <summary>
        /// 产品特有输出字段
        /// </summary>
		Dictionary<string, Dictionary<string, RateRecord>> ProductSpecific { get; }

        /// <summary>
        /// 是否成功
        /// </summary>
		bool Succeeded { get; }

        /// <summary>
        /// 错误信息
        /// </summary>
		string ErrorMessage { get; }

        /// <summary>
        /// ZeroSpreadDelta
        /// </summary>
        double ZeroSpreadDelta { get; }

        /// <summary>
        /// 期权估值日到标的到期日的时间
        /// </summary>
        double StoppingTime { get; }

        /// <summary>
        /// DDeltaDt
        /// </summary>
        double DDeltaDt { get; }

        /// <summary>
        /// DVegaDt
        /// </summary>
        double DVegaDt { get; }

        /// <summary>
        /// DDeltaDvol
        /// </summary>
        double DDeltaDvol { get; }

        /// <summary>
        /// DVegaDvol
        /// </summary>
        double DVegaDvol { get; }

        /// <summary>
        /// 标的资产现值
        /// </summary>
        double UnderlyingPv { get; set; }

        /// <summary>
        /// Basis
        /// </summary>
        double Basis { get; set; }

        /// <summary>
        /// CheapestToDeliver
        /// </summary>
        string CheapestToDeliver { get; set; }

        /// <summary>
        /// 估值使用的波动率
        /// </summary>
        double PricingVol { get; }

        /// <summary>
        /// 标的资产1使用的波动率
        /// </summary>
        double asset1PricingVol { get; }

        /// <summary>
        /// 标的资产2使用的波动率
        /// </summary>
        double asset2PricingVol { get; }

        /// <summary>
        /// 标的资产3使用的波动率
        /// </summary>
        double asset3PricingVol { get; }

        /// <summary>
        /// 标的资产4使用的波动率
        /// </summary>
        double asset4PricingVol { get; }

        /// <summary>
        /// 标的资产1的Delta
        /// </summary>
        double asset1Delta { get;  }

        /// <summary>
        /// 标的资产2的Delta
        /// </summary>
        double asset2Delta { get; }

        /// <summary>
        /// 标的资产3的Delta
        /// </summary>
        double asset3Delta { get; }

        /// <summary>
        /// 标的资产4的Delta
        /// </summary>
        double asset4Delta { get; }

        /// <summary>
        /// 标的资产1的DeltaCash
        /// </summary>
        double asset1DeltaCash { get; }

        /// <summary>
        /// 标的资产2的DeltaCash
        /// </summary>
        double asset2DeltaCash { get; }

        /// <summary>
        /// 标的资产3的DeltaCash
        /// </summary>
        double asset3DeltaCash { get; }

        /// <summary>
        /// 标的资产4的DeltaCash
        /// </summary>
        double asset4DeltaCash { get; }

        /// <summary>
        /// 标的资产1的PartialDelta
        /// </summary>
        double asset1PartialDelta { get; set; }

        /// <summary>
        /// 标的资产2的PartialDelta
        /// </summary>
        double asset2PartialDelta { get; set; }

        /// <summary>
        /// 标的资产3的PartialDelta
        /// </summary>
        double asset3PartialDelta { get; set; }

        /// <summary>
        /// 标的资产4的PartialDelta
        /// </summary>
        double asset4PartialDelta { get; set; }

        /// <summary>
        /// 标的资产1的Gamma
        /// </summary>
        double asset1Gamma { get; }

        /// <summary>
        /// 标的资产2的Gamma
        /// </summary>
        double asset2Gamma { get;  }

        /// <summary>
        /// 标的资产3的Gamma
        /// </summary>
        double asset3Gamma { get; }

        /// <summary>
        /// 标的资产4的Gamma
        /// </summary>
        double asset4Gamma { get; }

        /// <summary>
        /// 标的资产1的GammaCash
        /// </summary>
        double asset1GammaCash { get; }

        /// <summary>
        /// 标的资产2的GammaCash
        /// </summary>
        double asset2GammaCash { get; }

        /// <summary>
        /// 标的资产3的GammaCash
        /// </summary>
        double asset3GammaCash { get; }

        /// <summary>
        /// 标的资产4的GammaCash
        /// </summary>
        double asset4GammaCash { get; }

        /// <summary>
        /// 标的资产1的Vega
        /// </summary>
        double asset1Vega { get;  }

        /// <summary>
        /// 标的资产2的Vega
        /// </summary>
        double asset2Vega { get; }

        /// <summary>
        /// 标的资产3的Vega
        /// </summary>
        double asset3Vega { get; }

        /// <summary>
        /// 标的资产4的Vega
        /// </summary>
        double asset4Vega { get; }

        /// <summary>
        /// 标的资产1的DDeltaDt
        /// </summary>
        double asset1DDeltaDt { get; }

        /// <summary>
        /// 标的资产2的DDeltaDt
        /// </summary>
        double asset2DDeltaDt { get; }

        /// <summary>
        /// 标的资产3的DDeltaDt
        /// </summary>
        double asset3DDeltaDt { get; }

        /// <summary>
        /// 标的资产4的DDeltaDt
        /// </summary>
        double asset4DDeltaDt { get; }

        /// <summary>
        /// 标的资产1的DVegaDvol
        /// </summary>
        double asset1DVegaDvol { get; }

        /// <summary>
        /// 标的资产2的DVegaDvol
        /// </summary>
        double asset2DVegaDvol { get;  }

        /// <summary>
        /// 标的资产3的DVegaDvol
        /// </summary>
        double asset3DVegaDvol { get; }

        /// <summary>
        /// 标的资产4的DVegaDvol
        /// </summary>
        double asset4DVegaDvol { get; }

        /// <summary>
        /// 标的资产1的DDeltaDvol
        /// </summary>
        double asset1DDeltaDvol { get;}

        /// <summary>
        /// 标的资产2的DDeltaDvol
        /// </summary>
        double asset2DDeltaDvol { get;  }

        /// <summary>
        /// 标的资产3的DDeltaDvol
        /// </summary>
        double asset3DDeltaDvol { get; }

        /// <summary>
        /// 标的资产4的DDeltaDvol
        /// </summary>
        double asset4DDeltaDvol { get; }

        /// <summary>
        /// 标的资产1的DVegaDt
        /// </summary>
        double asset1DVegaDt { get; }

        /// <summary>
        /// 标的资产2的DVegaDt
        /// </summary>
        double asset2DVegaDt { get;  }

        /// <summary>
        /// 标的资产3的DVegaDt
        /// </summary>
        double asset3DVegaDt { get; }

        /// <summary>
        /// 标的资产4的DVegaDt
        /// </summary>
        double asset4DVegaDt { get; }

        /// <summary>
        /// crossGamma
        /// </summary>
        double crossGamma { get; }

        /// <summary>
        /// crossGamma12
        /// </summary>
        double crossGamma12 { get; }

        /// <summary>
        /// crossGamma13
        /// </summary>
        double crossGamma13 { get; }

        /// <summary>
        /// crossGamma14
        /// </summary>
        double crossGamma14 { get; }

        /// <summary>
        /// crossGamma23
        /// </summary>
        double crossGamma23 { get; }

        /// <summary>
        /// crossGamma24
        /// </summary>
        double crossGamma24 { get; }

        /// <summary>
        /// crossGamma34
        /// </summary>
        double crossGamma34 { get; }

        /// <summary>
        /// crossVomma
        /// </summary>
        double crossVomma { get; }

        /// <summary>
        /// crossVomma12
        /// </summary>
        double crossVomma12 { get; }

        /// <summary>
        /// crossVomma13
        /// </summary>
        double crossVomma13 { get; }

        /// <summary>
        /// crossVomma14
        /// </summary>
        double crossVomma14 { get; }

        /// <summary>
        /// crossVomma23
        /// </summary>
        double crossVomma23 { get; }

        /// <summary>
        /// crossVomma24
        /// </summary>
        double crossVomma24 { get; }

        /// <summary>
        /// crossVomma34
        /// </summary>
        double crossVomma34 { get; }

        /// <summary>
        /// crossVanna1
        /// </summary>
        double crossVanna1 { get; }

        /// <summary>
        /// crossVanna2
        /// </summary>
        double crossVanna2 { get;  }

        /// <summary>
        /// correlationVega
        /// </summary>
        double correlationVega { get; }

        /// <summary>
        /// correlationVega12
        /// </summary>
        double correlationVega12 { get; }

        /// <summary>
        /// correlationVega13
        /// </summary>
        double correlationVega13 { get; }

        /// <summary>
        /// correlationVega14
        /// </summary>
        double correlationVega14 { get; }

        /// <summary>
        /// correlationVega23
        /// </summary>
        double correlationVega23 { get; }

        /// <summary>
        /// correlationVega24
        /// </summary>
        double correlationVega24 { get; }

        /// <summary>
        /// correlationVega34
        /// </summary>
        double correlationVega34 { get; }

        /// <summary>
        /// 转换因子
        /// </summary>
        Dictionary<string, double> ConvertFactors { get; set; }

    }
}
