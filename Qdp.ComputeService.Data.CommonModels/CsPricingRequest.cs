using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
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
	[KnownType(typeof (VanillaOptionInfo))]
	[KnownType(typeof (InterestRateSwapInfo))]
	[KnownType(typeof (FxOptionInfo))]
	[KnownType(typeof (FixedLegInfo))]
	[KnownType(typeof (FloatingLegInfo))]
	[KnownType(typeof (BondFuturesInfo))]
	[KnownType(typeof (FixedDateCouonAdjustedBondInfo))]
	[KnownType(typeof (LoanInfo))]
	[KnownType(typeof (HoldingPeriodInfo))]
	public class CsPricingRequest
	{
		public CsPricingRequest(TradeInfoBase[] tradeInfos,
			MarketInfo marketInfo,
			List<PricingRequest[]> pricingRequests,
			Guid clientId = default(Guid),
			int countPerUpdate = 300,
			bool[] doNotBatchRun = null,
			bool aggregateResult = false
			)
		{
			TradeInfos = tradeInfos;
			MarketInfo = marketInfo;
			PricingRequests = pricingRequests;
			ClientId = clientId;
			CountPerUpdate = countPerUpdate;
			RequestId = Guid.NewGuid();
			DoNotBatchRun = doNotBatchRun ?? TradeInfos.Select(x => false).ToArray();
			AggregateResult = aggregateResult;
		}

		private CsPricingRequest()
		{
		}

		[DataMember]
		public Guid ClientId { get; set; }
		[DataMember]
		public Guid RequestId { get; set; }
		[DataMember]
		public TradeInfoBase[] TradeInfos { get; set; }
		[DataMember]
		public MarketInfo MarketInfo { get; set; }
		[DataMember]
		public List<PricingRequest[]> PricingRequests { get; set; }
		[DataMember]
		public bool[] DoNotBatchRun { get; set; }

		[DataMember]
		public int CountPerUpdate { get; set; }
		[DataMember]
		public bool AggregateResult { get; set; }
	}
}