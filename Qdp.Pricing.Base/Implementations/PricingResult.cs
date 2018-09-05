using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Interfaces;

namespace Qdp.Pricing.Base.Implementations
{
    /// <summary>
    /// 估值计算结果类
    /// </summary>
	[DataContract]
	[Serializable]
	public class PricingResult : IPricingResult
	{
		private readonly Dictionary<PricingRequest, bool> _pricingStatus;

        /// <summary>
        /// 估值日期
        /// </summary>
		[DataMember]
		public Date ValuationDate { get; private set; }

        /// <summary>
        /// 计算所花费的实际，单位为毫秒
        /// </summary>
		[DataMember]
		public double CalcTimeInMilliSecond { get; set; }

        /// <summary>
        /// 现值
        /// </summary>
		[DataMember]
		public double Pv { get; set; }

        /// <summary>
        /// 现值现价比
        /// </summary>
        [DataMember]
        public double PctPv { get; set; }

        /// <summary>
        /// 基点价值（利率曲线变动）
        /// </summary>
        [DataMember]
		public double Dv01 { get; set; }

        /// <summary>
        /// 基点价值（固定端利率变动）
        /// </summary>
		[DataMember]
		public double Pv01 { get; set; }

        /// <summary>
        /// 应计利息
        /// </summary>
		[DataMember]
		public double Ai { get; set; }

        /// <summary>
        /// 应计利息天数
        /// </summary>
		[DataMember]
		public int AiDays { get; set; }

        /// <summary>
        /// 到期收益率
        /// </summary>
		[DataMember]
		public double Ytm { get; set; }

        /// <summary>
        /// 全价
        /// </summary>
		[DataMember]
		public double DirtyPrice { get; set; }

        /// <summary>
        /// 净价
        /// </summary>
		[DataMember]
		public double CleanPrice { get; set; }

        /// <summary>
        /// Delta
        /// </summary>
		[DataMember]
		public double Delta { get; set; }

        /// <summary>
        /// Delta * 价格
        /// </summary>
		[DataMember]
        public double DeltaCash { get; set; }

        /// <summary>
        /// Gamma
        /// </summary>
        [DataMember]
        public double Gamma { get; set; }

        /// <summary>
        /// Gamma * 价格
        /// </summary>
		[DataMember]
        public double GammaCash { get; set; }

        /// <summary>
        /// Rho
        /// </summary>
        [DataMember]
        public double Rho { get; set; }

        /// <summary>
        /// RhoForeign
        /// </summary>
		[DataMember]
		public double RhoForeign { get; set; }

        /// <summary>
        /// Theta
        /// </summary>
		[DataMember]
		public double Theta { get; set; }

        /// <summary>
        /// Vega
        /// </summary>
		[DataMember]
		public double Vega { get; set; }

        /// <summary>
        /// FwdDiffVega
        /// </summary>
        [DataMember]
        public double FwdDiffVega { get; set; }

        /// <summary>
        /// 标的基点价值
        /// </summary>
        [DataMember]
		public double Dv01Underlying { get; set; }

        /// <summary>
        /// 麦考利久期
        /// </summary>
		[DataMember]
		public double MacDuration { get; set; }

        /// <summary>
        /// 修正久期
        /// </summary>
		[DataMember]
		public double ModifiedDuration { get; set; }

        /// <summary>
        /// 凸性
        /// </summary>
		[DataMember]
		public double Convexity { get; set; }

        /// <summary>
        /// 修正久期 * 全价
        /// </summary>
        [DataMember]
        public double DollarModifiedDuration { get; set; }

        /// <summary>
        /// 凸性 * 全价
        /// </summary>
        [DataMember]
        public double DollarConvexity { get; set; }

        /// <summary>
        /// Carry
        /// </summary>
        [DataMember]
        //public double Sp01 { get; set; }
        public double Carry { get; set; }

        /// <summary>
        /// ZeroSpread
        /// </summary>
        [DataMember]
		public double ZeroSpread { get; set; }

        /// <summary>
        /// FairQuote
        /// </summary>
		[DataMember]
		public double FairQuote { get; set; }

        /// <summary>
        /// CallDate
        /// </summary>
		[DataMember]
		public Date CallDate { get; set; }

        /// <summary>
        /// YieldToCall
        /// </summary>
		[DataMember]
		public double YieldToCall { get; set; }

        /// <summary>
        /// PutDate
        /// </summary>
		[DataMember]
		public Date PutDate { get; set; }

        /// <summary>
        /// YieldToPut
        /// </summary>
		[DataMember]
		public double YieldToPut { get; set; }

        /// <summary>
        /// 关键点基点价值
        /// </summary>
		[DataMember]
		public Dictionary<string, CurveRisk[]> KeyRateDv01 { get; set; }

        /// <summary>
        /// 现金流
        /// </summary>
		[DataMember]
		public Cashflow[] Cashflows { get; set; }

        /// <summary>
        /// 现金流
        /// </summary>
		[DataMember]
		public Dictionary<string, double> CashflowDict { get; set; }

        /// <summary>
        /// ComponentPvs
        /// </summary>
		[DataMember]
		public ComponentPv[] ComponentPvs { get; set; }

        /// <summary>
        /// 产品特有输出字段
        /// </summary>
		[DataMember]
		public Dictionary<string, Dictionary<string, RateRecord>> ProductSpecific { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
		[DataMember]
		public bool Succeeded { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
		[DataMember]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 转换因子
        /// </summary>
        [DataMember]
        public Dictionary<string, double> ConvertFactors { get; set; }

        /// <summary>
        /// ZeroSpreadDelta
        /// </summary>
        [DataMember]
        public double ZeroSpreadDelta { get; set; }

        /// <summary>
        /// 期权估值日到标的到期日的时间
        /// </summary>
        [DataMember]
        public double StoppingTime { get; set; }

        /// <summary>
        /// DDeltaDt
        /// </summary>
        [DataMember]
        public double DDeltaDt{ get; set; }

        /// <summary>
        /// Theta盈亏
        /// </summary>
        [DataMember]
        public double ThetaPnL { get; set; }

        /// <summary>
        /// 自然日Theta盈亏
        /// </summary>
        [DataMember]
        public double CalenderThetaPnL { get; set; }

        /// <summary>
        /// DVegaDt
        /// </summary>
        [DataMember]
        public double DVegaDt { get; set; }

        /// <summary>
        /// DDeltaDvol
        /// </summary>
        [DataMember]
        public double DDeltaDvol { get; set; }

        /// <summary>
        /// DVegaDvol
        /// </summary>
        [DataMember]
        public double DVegaDvol { get; set; }

        /// <summary>
        /// 标的资产现值
        /// </summary>
        [DataMember]
        public double UnderlyingPv { get; set; }

        /// <summary>
        /// Basis
        /// </summary>
        [DataMember]
        public double Basis { get; set; }

        /// <summary>
        /// CheapestToDeliver
        /// </summary>
        [DataMember]
        public string CheapestToDeliver { get; set; }

        /// <summary>
        /// 估值使用的波动率
        /// </summary>
        [DataMember]
        public double PricingVol { get; set; }

        /// <summary>
        /// 标的资产1使用的波动率
        /// </summary>
        [DataMember]
        public double asset1PricingVol { get; set; }

        /// <summary>
        /// 标的资产2使用的波动率
        /// </summary>
        [DataMember]
        public double asset2PricingVol { get; set; }

        /// <summary>
        /// 标的资产3使用的波动率
        /// </summary>
        [DataMember]
        public double asset3PricingVol { get; set; }

        /// <summary>
        /// 标的资产4使用的波动率
        /// </summary>
        [DataMember]
        public double asset4PricingVol { get; set; }

        /// <summary>
        /// 标的资产1的Delta
        /// </summary>
        [DataMember]
        public double asset1Delta { get; set; }

        /// <summary>
        /// 标的资产2的Delta
        /// </summary>
        [DataMember]
        public double asset2Delta { get; set; }

        /// <summary>
        /// 标的资产3的Delta
        /// </summary>
        [DataMember]
        public double asset3Delta { get; set; }

        /// <summary>
        /// 标的资产4的Delta
        /// </summary>
        [DataMember]
        public double asset4Delta { get; set; }

        /// <summary>
        /// 标的资产1的DeltaCash
        /// </summary>
        [DataMember]
        public double asset1DeltaCash { get; set; }

        /// <summary>
        /// 标的资产2的DeltaCash
        /// </summary>
        [DataMember]
        public double asset2DeltaCash { get; set; }

        /// <summary>
        /// 标的资产3的DeltaCash
        /// </summary>
        [DataMember]
        public double asset3DeltaCash { get; set; }

        /// <summary>
        /// 标的资产4的DeltaCash
        /// </summary>
        [DataMember]
        public double asset4DeltaCash { get; set; }

        /// <summary>
        /// 标的资产1的PartialDelta
        /// </summary>
        [DataMember]
        public double asset1PartialDelta { get; set; }

        /// <summary>
        /// 标的资产2的PartialDelta
        /// </summary>
        [DataMember]
        public double asset2PartialDelta { get; set; }

        /// <summary>
        /// 标的资产3的PartialDelta
        /// </summary>
        [DataMember]
        public double asset3PartialDelta { get; set; }

        /// <summary>
        /// 标的资产4的PartialDelta
        /// </summary>
        [DataMember]
        public double asset4PartialDelta { get; set; }

        /// <summary>
        /// 标的资产1的Gamma
        /// </summary>
        [DataMember]
        public double asset1Gamma { get; set; }

        /// <summary>
        /// 标的资产2的Gamma
        /// </summary>
        [DataMember]
        public double asset2Gamma { get; set; }

        /// <summary>
        /// 标的资产3的Gamma
        /// </summary>
        [DataMember]
        public double asset3Gamma { get; set; }

        /// <summary>
        /// 标的资产4的Gamma
        /// </summary>
        [DataMember]
        public double asset4Gamma { get; set; }

        /// <summary>
        /// 标的资产1的GammaCash
        /// </summary>
        [DataMember]
        public double asset1GammaCash { get; set; }

        /// <summary>
        /// 标的资产2的GammaCash
        /// </summary>
        [DataMember]
        public double asset2GammaCash { get; set; }

        /// <summary>
        /// 标的资产3的GammaCash
        /// </summary>
        [DataMember]
        public double asset3GammaCash { get; set; }

        /// <summary>
        /// 标的资产4的GammaCash
        /// </summary>
        [DataMember]
        public double asset4GammaCash { get; set; }

        /// <summary>
        /// 标的资产1的Vega
        /// </summary>
        [DataMember]
        public double asset1Vega { get; set; }

        /// <summary>
        /// 标的资产2的Vega
        /// </summary>
        [DataMember]
        public double asset2Vega { get; set; }

        /// <summary>
        /// 标的资产3的Vega
        /// </summary>
        [DataMember]
        public double asset3Vega { get; set; }

        /// <summary>
        /// 标的资产4的Vega
        /// </summary>
        [DataMember]
        public double asset4Vega { get; set; }

        /// <summary>
        /// 标的资产1的DDeltaDt
        /// </summary>
        [DataMember]
        public double asset1DDeltaDt { get; set; }

        /// <summary>
        /// 标的资产2的DDeltaDt
        /// </summary>
        [DataMember]
        public double asset2DDeltaDt { get; set; }

        /// <summary>
        /// 标的资产3的DDeltaDt
        /// </summary>
        [DataMember]
        public double asset3DDeltaDt { get; set; }

        /// <summary>
        /// 标的资产4的DDeltaDt
        /// </summary>
        [DataMember]
        public double asset4DDeltaDt { get; set; }

        /// <summary>
        /// 标的资产1的DVegaDvol
        /// </summary>
        [DataMember]
        public double asset1DVegaDvol { get; set; }

        /// <summary>
        /// 标的资产2的DVegaDvol
        /// </summary>
        [DataMember]
        public double asset2DVegaDvol { get; set; }

        /// <summary>
        /// 标的资产3的DVegaDvol
        /// </summary>
        [DataMember]
        public double asset3DVegaDvol { get; set; }

        /// <summary>
        /// 标的资产4的DVegaDvol
        /// </summary>
        [DataMember]
        public double asset4DVegaDvol { get; set; }

        /// <summary>
        /// 标的资产1的DDeltaDvol
        /// </summary>
        [DataMember]
        public double asset1DDeltaDvol { get; set; }

        /// <summary>
        /// 标的资产2的DDeltaDvol
        /// </summary>
        [DataMember]
        public double asset2DDeltaDvol { get; set; }

        /// <summary>
        /// 标的资产3的DDeltaDvol
        /// </summary>
        [DataMember]
        public double asset3DDeltaDvol { get; set; }

        /// <summary>
        /// 标的资产4的DDeltaDvol
        /// </summary>
        [DataMember]
        public double asset4DDeltaDvol { get; set; }

        /// <summary>
        /// 标的资产1的DVegaDt
        /// </summary>
        [DataMember]
        public double asset1DVegaDt { get; set; }

        /// <summary>
        /// 标的资产2的DVegaDt
        /// </summary>
        [DataMember]
        public double asset2DVegaDt { get; set; }

        /// <summary>
        /// 标的资产3的DVegaDt
        /// </summary>
        [DataMember]
        public double asset3DVegaDt { get; set; }

        /// <summary>
        /// 标的资产4的DVegaDt
        /// </summary>
        [DataMember]
        public double asset4DVegaDt { get; set; }

        /// <summary>
        /// crossGamma
        /// </summary>
        [DataMember]
        public double crossGamma { get; set; }

        /// <summary>
        /// crossGamma12
        /// </summary>
        [DataMember]
        public double crossGamma12 { get; set; }

        /// <summary>
        /// crossGamma13
        /// </summary>
        [DataMember]
        public double crossGamma13 { get; set; }

        /// <summary>
        /// crossGamma14
        /// </summary>
        [DataMember]
        public double crossGamma14 { get; set; }

        /// <summary>
        /// crossGamma23
        /// </summary>
        [DataMember]
        public double crossGamma23 { get; set; }

        /// <summary>
        /// crossGamma24
        /// </summary>
        [DataMember]
        public double crossGamma24 { get; set; }

        /// <summary>
        /// crossGamma34
        /// </summary>
        [DataMember]
        public double crossGamma34 { get; set; }

        /// <summary>
        /// crossVomma
        /// </summary>
        [DataMember]
        public double crossVomma { get; set; }

        /// <summary>
        /// crossVomma12
        /// </summary>
        [DataMember]
        public double crossVomma12 { get; set; }

        /// <summary>
        /// crossVomma13
        /// </summary>
        [DataMember]
        public double crossVomma13 { get; set; }

        /// <summary>
        /// crossVomma14
        /// </summary>
        [DataMember]
        public double crossVomma14 { get; set; }

        /// <summary>
        /// crossVomma23
        /// </summary>
        [DataMember]
        public double crossVomma23 { get; set; }

        /// <summary>
        /// crossVomma24
        /// </summary>
        [DataMember]
        public double crossVomma24 { get; set; }

        /// <summary>
        /// crossVomma34
        /// </summary>
        [DataMember]
        public double crossVomma34 { get; set; }

        /// <summary>
        /// crossVanna1
        /// </summary>
        [DataMember]
        public double crossVanna1 { get; set; }

        /// <summary>
        /// crossVanna2
        /// </summary>
        [DataMember]
        public double crossVanna2 { get; set; }

        /// <summary>
        /// correlationVega
        /// </summary>
        [DataMember]
        public double correlationVega { get; set; }

        /// <summary>
        /// correlationVega12
        /// </summary>
        [DataMember]
        public double correlationVega12 { get; set; }

        /// <summary>
        /// correlationVega13
        /// </summary>
        [DataMember]
        public double correlationVega13 { get; set; }

        /// <summary>
        /// correlationVega14
        /// </summary>
        [DataMember]
        public double correlationVega14 { get; set; }

        /// <summary>
        /// correlationVega23
        /// </summary>
        [DataMember]
        public double correlationVega23 { get; set; }

        /// <summary>
        /// correlationVega24
        /// </summary>
        [DataMember]
        public double correlationVega24 { get; set; }

        /// <summary>
        /// correlationVega34
        /// </summary>
        [DataMember]
        public double correlationVega34 { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="valuationDate">估值日期</param>
        /// <param name="requests">计算请求</param>
        public PricingResult(Date valuationDate, PricingRequest requests)
		{
			ValuationDate = valuationDate;
			_pricingStatus = requests.Split().ToDictionary(x => x, x => false);

			#region set default
			Pv = double.NaN;
			Dv01 = double.NaN;
			Pv01 = double.NaN;
			Ai = double.NaN;
			Ytm = double.NaN;
			DirtyPrice = double.NaN;
			CleanPrice = double.NaN;
			YieldToCall = double.NaN;
			YieldToPut = double.NaN;
			CallDate = null;
			PutDate = null;
			Delta = double.NaN;
			Gamma = double.NaN;
			Rho = double.NaN;
			RhoForeign = double.NaN;
			Vega = double.NaN;
			Theta = double.NaN;
			Dv01Underlying = double.NaN;
			MacDuration = double.NaN;
			ModifiedDuration = double.NaN;
			Convexity = double.NaN;
            DollarModifiedDuration = double.NaN;
            DollarConvexity = double.NaN;
            //Sp01 = double.NaN;
            Carry = double.NaN;
            ZeroSpread = double.NaN;
            FairQuote = double.NaN;
			KeyRateDv01 = new Dictionary<string, CurveRisk[]>();
			Cashflows = null;
			ComponentPvs = null;
			ProductSpecific = new Dictionary<string, Dictionary<string, RateRecord>>();
			CashflowDict = new Dictionary<string, double>();
			Succeeded = true;
			ErrorMessage = null;
            ConvertFactors = new Dictionary<string, double>();
            ZeroSpreadDelta = double.NaN;
            StoppingTime = double.NaN;
            DDeltaDt = double.NaN;
            DVegaDt = double.NaN;
            DDeltaDvol = double.NaN;
            DVegaDvol = double.NaN;
            UnderlyingPv = double.NaN;
            Basis = double.NaN;
            CheapestToDeliver = null;

            asset1Delta = double.NaN;
            asset2Delta = double.NaN;
            asset1Gamma = double.NaN;
            asset2Gamma = double.NaN;
            asset1Vega = double.NaN;
            asset2Vega = double.NaN;

            asset1DDeltaDt = double.NaN;
            asset2DDeltaDt = double.NaN;
            asset1DVegaDvol = double.NaN;
            asset2DVegaDvol = double.NaN;
            asset1DDeltaDvol = double.NaN;
            asset2DDeltaDvol = double.NaN;
            asset1DVegaDt = double.NaN;
            asset2DVegaDt = double.NaN;
            crossGamma = double.NaN;
            crossVomma = double.NaN;
            crossVanna1 = double.NaN;
            crossVanna2 = double.NaN;
            correlationVega = double.NaN;

            DeltaCash = double.NaN;
            GammaCash = double.NaN;
            PctPv = double.NaN;
            PricingVol = double.NaN;
            #endregion
        }

        /// <summary>
        /// 判断计算结果是否包含特定的指标
        /// </summary>
        /// <param name="request">计算请求</param>
        /// <returns>是否包含特定的指标</returns>
        public bool IsRequested(PricingRequest request)
		{
			return _pricingStatus.ContainsKey(request) && !_pricingStatus[request];
		}

		public void Tag(PricingRequest request)
		{
			_pricingStatus[request] = true;
		}

        /// <summary>
        /// 构造函数
        /// </summary>
		public PricingResult()
		{

		}

		public static Dictionary<string, bool> SummableProperties = new Dictionary<string, bool>
		{
			{"Pv", true},
			{"Dv01", true},
			{"Pv01", true},
			{"Ai", true},
			{"Delta", true},
			{"Gamma", true},
			{"Rho", true},
			{"RhoForeign", true},
			{"Theta", true},
			{"Vega", true},
			{"Dv01Underlying", true},
			{"MacDuration", true},
			{"ModifiedDuration", true},
			{"Convexity", true},
			//{"Sp01", true},
            {"Carry", true},
            {"Cashflows", true},
		};
	}
}
