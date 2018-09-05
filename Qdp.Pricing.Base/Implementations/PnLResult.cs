using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Qdp.Pricing.Base.Implementations
{
    /// <summary>
    /// 盈亏计算结果基类
    /// </summary>
    [Serializable]
    [DataContract]
    public abstract class PnLResultBase
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public PnLResultBase()
        {
            
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tPv">T日现值</param>
        /// <param name="t1Pv">T+1日现值</param>
        /// <param name="pnl">T+1日当日盈亏</param>
        /// <param name="pnlTime">盈亏时间分解</param>
        /// <param name="t1Cf">T+1日现金流</param>
        /// <param name="yieldCurvePnL">收益率曲线盈亏分解</param>
        public PnLResultBase(
            double tPv,
            double t1Pv,
            double pnl,
            double pnlTime,
            double t1Cf,
            Dictionary<string, CurveRisk[]> yieldCurvePnL = null
        )
        {
            TPv = tPv;
            T1Pv = t1Pv;
            PnL = pnl;
            T1Cf = t1Cf;
            PnLExcludeT1Cf = PnL - T1Cf;
            PnLTime = pnlTime;
            YieldCurvePnL = yieldCurvePnL;
            PnLPrice = (yieldCurvePnL == null ? 0.0 : yieldCurvePnL.Where(x => !x.Key.EndsWith("KeyRateDv01")).Sum(x => x.Value.Sum(i => i.Risk)));
            PnLVol = 0.0;
            PnLPriceVolCross = 0.0;
            ExplainedPnL = t1Cf + PnLTime + PnLPrice + PnLVol + PnLPriceVolCross;
            UnExplainedPnL = PnL - ExplainedPnL;
        }

        [DataMember]
        public double TPv { get; set; }

        [DataMember]
        public double T1Pv { get; set; }

        [DataMember]
        public double T1Cf { get; set; }

        [DataMember]
        public double PnL { get; set; }

        [DataMember]
        public double PnLExcludeT1Cf { get; set; }

        [DataMember]
        public double PnLTime { get; set; }

        [DataMember]
        public double PnLPrice { get; set; }

        [DataMember]
        public double PnLVol { get; set; }

        [DataMember]
        public double PnLPriceVolCross { get; set; }

        [DataMember]
        public double ExplainedPnL { get; set; }

        [DataMember]
        public double UnExplainedPnL { get; set; }
        [DataMember]
        public Dictionary<string, CurveRisk[]> YieldCurvePnL { get; set; }
    }

    [Serializable]
    [DataContract]
    public class BondLikePnLResult : PnLResultBase
    {
        public BondLikePnLResult()
        {

        }

        public BondLikePnLResult(
            double tPv,
            double t1Pv,
            double pnlTime,
            double t1Cf,
            double pnlPv01,
            double pnlZspread,
            double pnlBasis,
            double pnlConvexity
            ): base(tPv, t1Pv, t1Pv - tPv, pnlTime, t1Cf)
        {
            PnLPv01 = pnlPv01;
            PnLZspread = pnlZspread;
            PnLBasis = pnlBasis;
            PnLConvexity = pnlConvexity;
        }

        [DataMember]
        public double PnLPv01 { get; set; }

        [DataMember]
        public double PnLZspread { get; set; }

        [DataMember]
        public double PnLBasis { get; set; }

        [DataMember]
        public double PnLConvexity { get; set; }

    }

    [Serializable]
    [DataContract]
    public class SwapPnLResult : PnLResultBase
    {
        public SwapPnLResult()
        {
        }

        public SwapPnLResult(
            double tPv,
            double t1Pv,
            double pnlTime,
            double t1Cf,
            double pnlPv01,
            double pnlCarry,
            double pnlRolldown)
            : base(tPv, t1Pv, t1Pv - tPv, pnlTime, t1Cf)
        {
            PnLPv01 = pnlPv01;
            PnLCarry = pnlCarry;
            PnLRolldown = pnlRolldown;
        }

        [DataMember]
        public double PnLPv01 { get; set; }

        [DataMember]
        public double PnLCarry { get; set; }

        [DataMember]
        public double PnLRolldown { get; set; }
    }

    [Serializable]
    [DataContract]
    public class BondPnLResult : BondLikePnLResult
    {
        public BondPnLResult()
        {   
        }

        public BondPnLResult(
            double tPv,
            double t1Pv,
            double pnlTime,
            double t1Cf,
            double pnlPv01,
            double pnlZspread,
            double pnlCarry,
            double pnlRolldown,
            double pnlDuration,
            double pnlConvexity)
            : base(tPv, t1Pv, pnlTime, t1Cf, pnlPv01, pnlZspread, 0, pnlConvexity)
        {
            PnLCarry = pnlCarry;
            PnLRolldown = pnlRolldown;
            PnLDuration = pnlDuration;
            PnLPrice = PnLPv01 + PnLConvexity + PnLZspread;
            ExplainedPnL = t1Cf + PnLTime + PnLPrice; //carry and rolldown is to explain timePnL
            UnExplainedPnL = PnL - ExplainedPnL;
        }

        [DataMember]
        public double PnLCarry { get; set; }

        [DataMember]
        public double PnLRolldown { get; set; }

        //the following are not used in book level pnl attribution
        [DataMember]
        public double PnLDuration { get; set; }
        
    }

    [Serializable]
    [DataContract]
    public class BondFuturesPnLResult : BondLikePnLResult
    {
        public BondFuturesPnLResult()
        {

        }

        public BondFuturesPnLResult(
            double tPv,
            double t1Pv,
            double pnlPv01,
            double pnlZspread,
            double pnlBasis,
            double pnlConvexity,
            double pnlTime,
            Dictionary<string, CurveRisk[]> curveRisks)
            : base(tPv, t1Pv, pnlTime, 0, pnlPv01, pnlZspread, pnlBasis, pnlConvexity)
        {
            PnLPv01 = pnlPv01;
            PnLBasis = pnlBasis;
            PnLZspread = pnlZspread;
            PnLConvexity = pnlConvexity;
            PnLTime = pnlTime;
            PnLPrice = pnlPv01 + pnlBasis + pnlZspread;
            ExplainedPnL = PnLTime + PnLPrice;
            UnExplainedPnL = PnL - ExplainedPnL;
        }
    }

    [Serializable]
    [DataContract]
    public class CommonPnLResult : PnLResultBase
    {
        public CommonPnLResult()
        {

        }

        public CommonPnLResult(
            double tPv,
            double t1Pv,
            double pnlTime,
            double t1Cf,
            Dictionary<string, CurveRisk[]> curveRisks)
            : base(tPv, t1Pv, t1Pv-tPv, pnlTime, t1Cf, curveRisks)
        {
        }
    }

    [Serializable]
    [DataContract]
    public class OptionPnLResult : PnLResultBase
    {
        public OptionPnLResult()
        { }

        public OptionPnLResult(
            double tPv,
            double t1Pv,
            double pnlTime,
            double t1Cf,
            double pnlPrice,
            double pnlVol,
            double pnlPriceVolCross,
            double pnlDelta,
            double pnlGamma,
            double pnlVega,
            double pnlTheta,
            double pnlRho,
            double pnlHighOrder,
            double pnlDDeltaDt,
            double pnlDVegaDt,
            double pnlDVegaDvol,
            double pnlDDeltaDvol
            )
                :base(tPv, t1Pv, t1Pv - tPv, pnlTime, t1Cf)
        {
            PnLDelta = pnlDelta;
            PnLGamma = pnlGamma;
            PnLVega = pnlVega;
            PnLTheta = pnlTheta;
            PnLRho = pnlRho;
            PnLDDeltaDt = pnlDDeltaDt;
            PnLDVegaDt = pnlDVegaDt;
            PnLDVegaDvol = pnlDVegaDvol;
            PnLDDeltaDvol = pnlDDeltaDvol;
            PnLHighOrder = pnlHighOrder;
            PnLPrice = pnlPrice;   // use PnLDelta + PnLGamma  to explain this item
            PnLVol = pnlVol;       // use vega to explain this
            PnLPriceVolCross = pnlPriceVolCross;   //pnlHighOrder to explain this.  
            ExplainedPnL = PnLRho + PnLTheta + PnLPrice + PnLVol + PnLPriceVolCross; //PnLDelta + PnLGamma + pnlVega + pnlTheta + pnlRho + pnlHighOrder;
            UnExplainedPnL = PnL - ExplainedPnL;
        }

        [DataMember]
        public double PnLDelta { get; set; }

        [DataMember]
        public double PnLGamma { get; set; }

        [DataMember]
        public double PnLVega { get; set; }

        [DataMember]
        public double PnLTheta { get; set; }

        [DataMember]
        public double PnLRho { get; set; }

        [DataMember]
        public double PnLHighOrder { get; set; }

        [DataMember]
        public double PnLDDeltaDt { get; set; }

        [DataMember]
        public double PnLDVegaDt { get; set; }

        [DataMember]
        public double PnLDVegaDvol { get; set; }

        [DataMember]
        public double PnLDDeltaDvol { get; set; }
    }
}
