using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Qdp.Pricing.Base.Implementations
{
    /// <summary>
    /// 曲线风险点
    /// </summary>
	[DataContract]
	public class CurveRisk
	{
        /// <summary>
        /// 期限
        /// </summary>
		[DataMember] public string Tenor { get; set; }

        /// <summary>
        /// 风险值
        /// </summary>
		[DataMember]
		public double Risk { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
		public CurveRisk()
		{
			
		}

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="tenor">期限</param>
        /// <param name="risk">风险值</param>
		public CurveRisk(string tenor, double risk)
		{
			Tenor = tenor;
			Risk = risk;
		}
	}
}
