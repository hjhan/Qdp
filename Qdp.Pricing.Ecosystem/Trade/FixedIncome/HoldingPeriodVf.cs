using System;
using System.Collections.Generic;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;

namespace Qdp.Pricing.Ecosystem.Trade.FixedIncome
{
	public class HoldingPeriodVf : ValuationFunction<HoldingPeriodInfo, HoldingPeriod>
	{
		private readonly BondVf _bondVf;
		public HoldingPeriodVf(HoldingPeriodInfo tradeInfo)
			: base(tradeInfo)
		{
			_bondVf = new BondVf(TradeInfo.UnderlyingBondInfo);
		}

		public override HoldingPeriod GenerateInstrument()
		{
			var underlyingBond = _bondVf.GenerateInstrument();

			return new HoldingPeriod(
				TradeInfo.TradeId
				,TradeInfo.Notional
				,TradeInfo.StartDate.ToDate()
				,TradeInfo.EndDate.ToDate()
				,underlyingBond
				,TradeInfo.Direction.ToDirection()
				,TradeInfo.InterestTaxRate
				,TradeInfo.BusinessTaxRate
				,TradeInfo.HoldingCost
				,TradeInfo.StartFrontCommission
				,TradeInfo.StartBackCommission
				,TradeInfo.EndFrontCommission
				,TradeInfo.EndBackCommission
				,TradeInfo.PaymentBusinessDayCounter.ToDayCountImpl()
				,TradeInfo.StartFixingRate
				,TradeInfo.EndFixingRate);
		}

		public override IEngine<HoldingPeriod> GenerateEngine()
		{
			return new HoldingPeriodEngine();
		}

		public override IMarketCondition GenerateMarketCondition(QdpMarket market)
		{
			var parseMarket = _bondVf.GenerateMarketCondition(market);

			var mktQuote = new Dictionary<string, Tuple<PriceQuoteType, double>>();
			try
			{
				var startBondId = TradeInfo.TradeId + "_Start";
				var startBondMktData = market.GetData<BondMktData>(startBondId);
				if (startBondMktData != null)
				{
					mktQuote[startBondId] = Tuple.Create(startBondMktData.PriceQuoteType.ToPriceQuoteType(), startBondMktData.Quote);
				}

				var endBondId = TradeInfo.TradeId + "_End";
				var endBondMktData = market.GetData<BondMktData>(endBondId);
				if (endBondMktData != null)
				{
					mktQuote[endBondId] = Tuple.Create(endBondMktData.PriceQuoteType.ToPriceQuoteType(), endBondMktData.Quote);
				}

				foreach (var key in parseMarket.MktQuote.Value.Keys)
				{
					mktQuote[key] = parseMarket.MktQuote.Value[key];
				}
				parseMarket = parseMarket.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, Tuple<PriceQuoteType, double>>>(x => x.MktQuote, mktQuote));
			}
			catch (Exception ex)
			{
				throw new PricingBaseException(string.Format("Market price is missing, detailed message: {0}", ex.Message));
			}
			return parseMarket;
		}
	}
}
