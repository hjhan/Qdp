using System;
using System.Reflection;
using System.Runtime.Serialization;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Base.Implementations
{
    /// <summary>
    /// 现金流计算细节对象
    /// </summary>
	[DataContract]
	[Serializable]
	public class CfCalculationDetail
	{
        /// <summary>
        /// 重置开始日
        /// </summary>
		[DataMember]
		public Date ResetStartDate { get; set; }

        /// <summary>
        /// 重置结束日
        /// </summary>
		[DataMember]
		public Date ResetEndDate { get; set; }

        /// <summary>
        /// 定盘日
        /// </summary>
		[DataMember]
		public Date FixingDate { get; set; }

        /// <summary>
        /// 定盘利率
        /// </summary>
		[DataMember]
		public double FixingRate { get; set; }

        /// <summary>
        /// 定盘折现因子
        /// </summary>
		[DataMember]
		public double FixingDcf { get; set; }

        /// <summary>
        /// 是否为固定现金流
        /// </summary>
		[DataMember]
		public bool IsFixed { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="resetStartDate">重置开始日</param>
        /// <param name="resetEndDate">重置结束日</param>
        /// <param name="fixingDate">定盘日期</param>
        /// <param name="fixingRate">定盘利率</param>
        /// <param name="fixingDcf">定盘折现因子</param>
        /// <param name="isFixed">是否为固定现金流</param>
		public CfCalculationDetail(
			Date resetStartDate,
			Date resetEndDate,
			Date fixingDate,
			double fixingRate,
			double fixingDcf,
			bool isFixed)
		{
			ResetStartDate = resetStartDate;
			ResetEndDate = resetEndDate;
			FixingDate = fixingDate;
			FixingRate = fixingRate;
			FixingDcf = fixingDcf;
			IsFixed = isFixed;
		}
	}

    /// <summary>
    /// 现金流类
    /// </summary>
	[DataContract]
	[Serializable]
	public class Cashflow
	{
        /// <summary>
        /// 计息开始日
        /// </summary>
		[DataMember]
		public Date AccrualStartDate { get; set; }

        /// <summary>
        /// 计息结束日
        /// </summary>
		[DataMember]
		public Date AccrualEndDate { get; set; }

        /// <summary>
        /// 参考开始日
        /// </summary>
		[DataMember]
		public Date RefStartDate { get; set; }

        /// <summary>
        /// 参考结束日
        /// </summary>
		[DataMember]
		public Date RefEndDate { get; set; }

        /// <summary>
        /// 支付日
        /// </summary>
		[DataMember]
		public Date PaymentDate { get; set; }

        /// <summary>
        /// 支付金额
        /// </summary>
		[DataMember]
		public double PaymentAmount { get; set; }

        /// <summary>
        /// 折现因子
        /// </summary>
		[DataMember]
		public double DiscountFactor { get; set; }

        /// <summary>
        /// 利率
        /// </summary>
		[DataMember]
		public double CouponRate { get; set; }

        /// <summary>
        /// 开始本金
        /// </summary>
		[DataMember]
		public double StartPrincipal { get; set; }

        /// <summary>
        /// 支付币种
        /// </summary>
		[DataMember]
		public CurrencyCode PaymentCurrency { get; set; }

        /// <summary>
        /// 现金流类型
        /// </summary>
		[DataMember]
		public CashflowType CashflowType { get; set; }

        /// <summary>
        /// 是否固定现金流
        /// </summary>
		[DataMember]
		public bool IsFixed { get; set; }

        /// <summary>
        /// 计算细节信息
        /// </summary>
		[DataMember]
		public CfCalculationDetail[] CalculationDetails { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="accStartDate">计息开始日</param>
        /// <param name="accEndDate">计息结束日</param>
        /// <param name="paymentDate">支付日</param>
        /// <param name="paymentAmount">支付金额</param>
        /// <param name="paymentCurrency">支付币种</param>
        /// <param name="cashflowType">现金流类型</param>
        /// <param name="isFixed">是否固定现金流</param>
        /// <param name="df">折现因子</param>
        /// <param name="cfCalcDetails">计算细节</param>
        /// <param name="refStartDate">参考开始日</param>
        /// <param name="refEndDate">参考结束日</param>
        /// <param name="startPrincipal">开始本金</param>
        /// <param name="couponRate">利率</param>
		public Cashflow(
			Date accStartDate,
			Date accEndDate,
			Date paymentDate,
			double paymentAmount,
			CurrencyCode paymentCurrency,
			CashflowType cashflowType,
			bool isFixed,
			double df,
			CfCalculationDetail[] cfCalcDetails,
			Date refStartDate = null,
			Date refEndDate = null,
			double startPrincipal = 100.0,
			double couponRate = 0.0)
		{
			AccrualStartDate = accStartDate;
			AccrualEndDate = accEndDate;
			PaymentDate = paymentDate;
			PaymentAmount = paymentAmount;
			PaymentCurrency = paymentCurrency;
			CashflowType = cashflowType;
			DiscountFactor = df;
			IsFixed = isFixed;
			CalculationDetails = cfCalcDetails;
			RefStartDate = refStartDate;
			RefEndDate = refEndDate;
			CouponRate = couponRate;
			StartPrincipal = startPrincipal;
		}

        /// <summary>
        /// 拷贝构造函数
        /// </summary>
        /// <param name="cf">现金流对象</param>
		public Cashflow(Cashflow cf)
		{
			AccrualStartDate = cf.AccrualStartDate;
			AccrualEndDate = cf.AccrualEndDate;
			PaymentDate = cf.PaymentDate;
			PaymentAmount = cf.PaymentAmount;
			PaymentCurrency = cf.PaymentCurrency;
			CashflowType = cf.CashflowType;
			IsFixed = cf.IsFixed;
			CalculationDetails = cf.CalculationDetails;
			RefStartDate = cf.RefStartDate;
			RefEndDate = cf.RefEndDate;
			CouponRate = cf.CouponRate;
			StartPrincipal = cf.StartPrincipal;
			DiscountFactor = cf.DiscountFactor;
		}

        /// <summary>
        /// 转换为字符串：{CashflowType} {PaymentAmount} {PaymenCurrency}@{PaymentDate}。
        /// 例如：
        /// Coupon 0.0025 CNY@2018-03-20
        /// </summary>
        /// <returns></returns>
		public override string ToString()
		{
			return string.Format("{0} {1} {2}@{3}", CashflowType, Math.Round(PaymentAmount, 4), PaymentCurrency, PaymentDate);
		}

        /// <summary>
        /// 转换为现金流关键字字符串，用作唯一标识某金融工具的某一现金流，例如：
        /// 2018-03-20,CNY,Coupon
        /// </summary>
        /// <returns></returns>
		public string ToCfKey()
		{
			return string.Format("{0},{1},{2}", PaymentDate, PaymentCurrency, CashflowType);
		}

        /// <summary>
        /// 将现金流关键字和金额转换为现金流对象
        /// </summary>
        /// <param name="key">现金流关键字</param>
        /// <param name="amount">金额</param>
        /// <returns>现金流对象</returns>
		public static Cashflow KeyToCf(string key, double amount)
		{
			var splits = key.Split(',');
			return new Cashflow(null ,null ,splits[0].ToDate(), amount, splits[1].ToCurrencyCode(), splits[2].ToCashflowType(), true, double.NaN, null);
		}

        /// <summary>
        /// 将所有属性转换为键值对数组
        /// </summary>
        /// <param name="labels">需要转换的键列表</param>
        /// <returns>键值对数组</returns>
		public object[] ToLabelData(PropertyInfo[] labels)
		{
			return labels.ToLabelData(this);
		}

        /// <summary>
        /// 构造函数
        /// </summary>
		public Cashflow()
		{

		}
	}
}
