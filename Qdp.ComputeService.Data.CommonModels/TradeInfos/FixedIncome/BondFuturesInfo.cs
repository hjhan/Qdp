using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;
using Qdp.Foundation.TableWithHeader;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	[DataContract]
	[Serializable]
	public class BondFuturesInfo : TradeInfoBase
	{
		public BondFuturesInfo(string tradeId, string futuresId = null)
			: base(tradeId, "BondFutures")
		{
			FuturesId = futuresId ?? tradeId;
		}

		public BondFuturesInfo()
			: base(Guid.NewGuid().ToString(), "BondFutures")
		{
			FuturesId = Guid.NewGuid().ToString();
		}

		[DataMember]
		[Column(0)]
		public string FuturesId { get; set; }

		[DataMember]
		[Column(2)]
		public string StartDate { get; set; }

		[DataMember]
		[Column(3)]
		public string MaturityDate { get; set; }

		[DataMember]
		[Column(4)]
		public double Notional { get; set; }

		[DataMember]
		[Column(5)]
		public string Calendar { get; set; }

		[DataMember]
		[Column(6)]
		public string Currency { get; set; }

		[DataMember]
		[Column(7)]
		public string DayCount { get; set; }

		[DataMember, DefaultValue(0.03)]
		[Column(8)]
		public double NominalCoupon { get; set; }

		[DataMember]
		[Column(8)]
		public FixedRateBondInfo[] DeliverableBondInfos { get; set; }

		[DataMember]
		public SimpleCfValuationParameters ValuationParamters { get; set; }

		public object Clone(string tradeId)
		{
			var bondFuturesinfo = (BondFuturesInfo) MemberwiseClone();
			bondFuturesinfo.TradeId = tradeId;

			return bondFuturesinfo;
		}

		public override TradeInfoBase[] GetDependenciesTrades()
		{
			return DeliverableBondInfos;
		}

		public string DeliverableBondInfosToLabelData()
		{
			return DeliverableBondInfos == null ? null : DeliverableBondInfos.Select(x=>x.TradeId).Aggregate("", (current, x) => string.Format("{0},{1}", current, x)).Substring(1);
		}

		public FixedRateBondInfo[] DeliverableBondInfosConverter()
		{
			return null;
		}

		public override ValuationParameters GetValuationParamters()
		{
			return ValuationParamters;
		}

        #region Upgrade

        /// <summary>
        /// todo MaturityDate 是否可以提取到 tradeInfobase?
        /// </summary>
        protected override string Maturity_Date
        {
            get
            {
                return MaturityDate;
            }
        }
        #endregion
    }
}