using System;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.TradeInfos;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Pricing.Base.Implementations;

namespace Qdp.ComputeService.Data.CommonModels
{
	[DataContract]
	[Serializable]
	[KnownType(typeof (FloatingRateBondInfo))]
	[KnownType(typeof (FixedRateBondInfo))]
	[KnownType(typeof (FixedDateCouonAdjustedBondInfo))]
	[KnownType(typeof (VanillaOptionInfo))]
	[KnownType(typeof (InterestRateSwapInfo))]
	[KnownType(typeof (FxOptionInfo))]
	[KnownType(typeof (FixedLegInfo))]
	[KnownType(typeof (FloatingLegInfo))]
	[KnownType(typeof (BondFuturesInfo))]
	[KnownType(typeof (LoanInfo))]
	[KnownType(typeof (HoldingPeriodInfo))]
	public class CsPricingResult
	{
		public CsPricingResult(Guid clientId,
			Guid requestId,
			PricingResult result,
			TradeInfoBase tradeInfo)
		{
			ClientId = clientId;
			RequestId = requestId;
			TradeInfo = tradeInfo;
			Result = result;
		}

		[DataMember]
		public Guid ClientId { get; set; }

		[DataMember]
		public Guid RequestId { get; set; }

		[DataMember]
		public TradeInfoBase TradeInfo { get; set; }

		[DataMember]
		public PricingResult Result { get; set; }
	}
}