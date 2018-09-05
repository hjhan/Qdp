using System;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams;
using Qdp.Foundation.TableWithHeader;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	[DataContract]
	[Serializable]
	public class FixedRateBondInfo : BondInfoBase
	{
		public FixedRateBondInfo(string tradeId,
			string bondId = null,
			double tradeprice = double.NaN,
			string counterparty = null,
			string tradeDate = null,
			string tradeTime = null)
			: base(tradeId, "FixedRateBond", bondId, tradeprice, counterparty, tradeDate, tradeTime)
		{
		}

		public FixedRateBondInfo()
			: base(Guid.NewGuid().ToString(), "FixedRateBond")
		{
		}

		[DataMember]
		[Column(3)]
		public double FixedCoupon { get; set; }

		public object Clone(string tradeId)
		{
			var bondinfo = (FixedRateBondInfo) MemberwiseClone();
			bondinfo.TradeId = tradeId;

			return bondinfo;
		}
		public override ValuationParameters GetValuationParamters()
		{
			return ValuationParamters;
		}
	}
}