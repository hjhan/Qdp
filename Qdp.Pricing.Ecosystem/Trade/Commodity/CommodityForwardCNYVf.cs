using Qdp.ComputeService.Data.CommonModels.TradeInfos.Commodity;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Commodity.CommodityProduct;
using Qdp.Pricing.Library.Common.Interfaces;
using System.Collections.Generic;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Commodity.Engines.Analytical;

namespace Qdp.Pricing.Ecosystem.Trade.Commodity
{
    public class CommodityForwardCNYVf : ValuationFunction<CommodityForwardCNYInfo, CommodityForwardCNY>
    {
        public CommodityForwardCNYVf(CommodityForwardCNYInfo tradeInfo) : base(tradeInfo)
        {
        }

        public override CommodityForwardCNY GenerateInstrument()
        {
            var startDate = TradeInfo.StartDate.ToDate();
            var maturityDate= TradeInfo.MaturityDate.ToDate();
            return new CommodityForwardCNY(
                startDate: startDate,
                maturityDate: maturityDate,
                notional: TradeInfo.Notional,
                basis:TradeInfo.Basis,
                payoffCcy: string.IsNullOrEmpty(TradeInfo.PayoffCurrency) ? CurrencyCode.CNY : TradeInfo.PayoffCurrency.ToCurrencyCode(),
                settlementCcy: string.IsNullOrEmpty(TradeInfo.SettlementCurrency) ? CurrencyCode.CNY : TradeInfo.SettlementCurrency.ToCurrencyCode()
            );
        }

        public override IEngine<CommodityForwardCNY> GenerateEngine()
        {
            return new AnalyticalCommodityForwardEngine();
        }

        public override IMarketCondition GenerateMarketCondition(QdpMarket market)
        {
            var prebuiltMarket = market as PrebuiltQdpMarket;
            if (prebuiltMarket != null)
            {
                return GenerateMarketConditionFromPrebuilt(prebuiltMarket);
            }
            else
            {
                var ticker = TradeInfo.UnderlyingTicker;
                var spot = market.GetData<StockMktData>(ticker).Price;
                return new MarketCondition(
                    x => x.ValuationDate.Value = market.ReferenceDate,
                    x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } }
                    );
            }
        }

        private IMarketCondition GenerateMarketConditionFromPrebuilt(PrebuiltQdpMarket prebuiltMarket)
        {
            var ticker = TradeInfo.UnderlyingTicker;
            var spot = prebuiltMarket.StockPrices[ticker];
            return new MarketCondition(
                    x => x.ValuationDate.Value = prebuiltMarket.ReferenceDate,                   
                    x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } }

                    );
        }

    }
}
