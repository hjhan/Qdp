using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Commodity;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Library.Commodity.CommodityProduct;
using Qdp.Pricing.Library.Commodity.Engines.Analytical;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using System.Collections.Generic;

namespace Qdp.Pricing.Ecosystem.Trade.Commodity
{
    public class CommoditySwapVf : ValuationFunction<CommoditySwapInfo, CommoditySwap>
    {
        public CommoditySwapVf(CommoditySwapInfo tradeInfo) : base(tradeInfo)
        {
        }

        public override CommoditySwap GenerateInstrument()
        {
            var startDate = TradeInfo.StartDate.ToDate();
            var maturityDate = TradeInfo.MaturityDate.ToDate();
            return new CommoditySwap(
                startDate: startDate,
                maturityDate: maturityDate,
                recTicker: TradeInfo.RecTicker,
                payTicker: TradeInfo.PayTicker,
                fxTicker:TradeInfo.FxTicker,
                recNotional:TradeInfo.RecNotional,
                payNotional:TradeInfo.PayNotional,
                fxNotional:TradeInfo.FxNotional,
                recCcy: string.IsNullOrEmpty(TradeInfo.RecCcy) ? CurrencyCode.CNY : TradeInfo.RecCcy.ToCurrencyCode(),
                payCcy: string.IsNullOrEmpty(TradeInfo.PayCcy) ? CurrencyCode.CNY : TradeInfo.PayCcy.ToCurrencyCode()
            );
        }

        public override IEngine<CommoditySwap> GenerateEngine()
        {
            return new AnalyticalCommoditySwapEngine();
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
                var recTicker = TradeInfo.RecTicker;
                var payTicker = TradeInfo.PayTicker;
                var fxTicker = TradeInfo.FxTicker;
                var recLegSpot = market.GetData<StockMktData>(recTicker).Price;
                var payLegSpot = market.GetData<StockMktData>(payTicker).Price;
                var fx = market.GetData<FxFixingMktData>(fxTicker).Price;

                return new MarketCondition(
                    x => x.ValuationDate.Value = market.ReferenceDate,
                    x => x.SpotPrices.Value = new Dictionary<string, double> { { recTicker, recLegSpot }, { payTicker, payLegSpot } },
                    x => x.FxSpot.Value = new Dictionary<string, double> { { fxTicker, fx } }
                    );
            }
        }

        private IMarketCondition GenerateMarketConditionFromPrebuilt(PrebuiltQdpMarket prebuiltMarket)
        {
            var recTicker = TradeInfo.RecTicker;
            var payTicker = TradeInfo.PayTicker;
            var fxTicker = TradeInfo.FxTicker;
            var recLegSpot = prebuiltMarket.StockPrices[recTicker];
            var payLegSpot = prebuiltMarket.StockPrices[payTicker];
            var fx = prebuiltMarket.FxSpots[fxTicker];

            return new MarketCondition(
                    x => x.ValuationDate.Value = prebuiltMarket.ReferenceDate,
                    x => x.SpotPrices.Value = new Dictionary<string, double> { { recTicker, recLegSpot }, { payTicker, payLegSpot } },
                    x => x.FxSpot.Value = new Dictionary<string, double> { { fxTicker, fx } }
                    );
        }

    }
}
