using System;
using System.Runtime.Serialization;

namespace Qdp.Pricing.Base.Implementations
{
    /// <summary>
    /// 利率记录类
    /// </summary>
	[DataContract]
	[Serializable]
	public class RateRecord
	{
        /// <summary>
        /// 连续复利利率
        /// </summary>
		[DataMember]
		public double ContinuousRate { get; set; }

        /// <summary>
        /// 利率日期
        /// </summary>
		[DataMember]
		public string Date { get; set; }

        /// <summary>
        /// 折现因子
        /// </summary>
		[DataMember]
		public double DiscountFactor { get; set; }

        /// <summary>
        /// 利率
        /// </summary>
		[DataMember]
		public double Rate { get; set; }

        /// <summary>
        /// 期限
        /// </summary>
		[DataMember]
		public string Term { get; set; }

        /// <summary>
        /// 产品类型
        /// </summary>
		[DataMember]
		public string ProductType { get; set; }

        /// <summary>
        /// ZeroRate
        /// </summary>
		[DataMember]
		public double ZeroRate { get; set; }

	}
}
