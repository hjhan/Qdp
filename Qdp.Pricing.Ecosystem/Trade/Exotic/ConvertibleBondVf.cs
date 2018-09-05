using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Ecosystem.Trade.Equity;
using Qdp.Pricing.Ecosystem.Trade.FixedIncome;
using Qdp.Pricing.Ecosystem.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Equity.Engines.Numerical;
using Qdp.Pricing.Library.Exotic;
using Qdp.Pricing.Library.Exotic.Engines;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Pricing.Library.Equity.Engines.MonteCarlo;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace Qdp.Pricing.Ecosystem.Trade.Exotic
{
    public class ConvertibleBondVf : ValuationFunction<ConvertibleBondInfo, ConvertibleBond>
    {
        public ConvertibleBondVf(ConvertibleBondInfo tradeInfo) : base(tradeInfo)
        {
        }

        public override ConvertibleBond GenerateInstrument()
        {
            var bond = new BondVf(TradeInfo.BondPart).GenerateInstrument();
            var conversionOption = new VanillaOptionVf(TradeInfo.ConversionOption).GenerateInstrument();
            var embeddedOptions = TradeInfo.EmbeddedOptions == null ? null : TradeInfo.EmbeddedOptions.Select(x => new VanillaOptionVf(x).GenerateInstrument()).ToArray();
            var eboStrikeQuoteTypes = TradeInfo.EboStrikeQuoteTypes == null ? null : TradeInfo.EboStrikeQuoteTypes.Select(x => (PriceQuoteType)Enum.Parse(typeof(PriceQuoteType), x)).ToArray();
            if (TradeInfo.TreatAsCommonBond)
            {
                return new ConvertibleBond(bond, null, null, null);
            }
            return new ConvertibleBond(bond, conversionOption, embeddedOptions, eboStrikeQuoteTypes);
        }

        public override IEngine<ConvertibleBond> GenerateEngine()
        {
            var optionInfo = TradeInfo.ConversionOption;
            var exercise = optionInfo.Exercise.ToOptionExercise();
            IEngine<VanillaOption> optionEngine = null;

            if (exercise == OptionExercise.European)
            {
                //不该用MonteCarlo 直接判断 逻辑更合理
                if (optionInfo.MonteCarlo)
                {
                    optionEngine = new GenericMonteCarloEngine(optionInfo.ParallelDegree ?? 2, optionInfo.NSimulations ?? 50000);
                    return new SimpleConvertibleBondEngine<GenericMonteCarloEngine>(optionEngine as GenericMonteCarloEngine);
                }
                else
                {
                    optionEngine = new AnalyticalVanillaEuropeanOptionEngine();
                    return new SimpleConvertibleBondEngine<AnalyticalVanillaEuropeanOptionEngine>(optionEngine as AnalyticalVanillaEuropeanOptionEngine);
                }
            }
            else if (exercise == OptionExercise.American)
            {
                optionEngine = new BinomialTreeAmericanEngine(BinomialTreeType.CoxRossRubinstein, 100);
                return new SimpleConvertibleBondEngine<BinomialTreeAmericanEngine>(optionEngine as BinomialTreeAmericanEngine);
            }
            else
            {
                throw new PricingBaseException("Conversion option exercise type should be European or American!");
            }
        }

        public override IMarketCondition GenerateMarketCondition(QdpMarket market)
        {
            var prebulitMarket = market as PrebuiltQdpMarket;
            if (prebulitMarket != null)
            {
                return GenerateMarketConditionFromPrebuilt(prebulitMarket);
            }
            else
            {
                var valuationParameter = TradeInfo.ValuationParameters;
                var bondMktData = market.GetData<BondMktData>(TradeInfo.TradeId);
                var volsurf = market.GetData<VolSurfMktData>(valuationParameter.VolSurfNames[0]).ToImpliedVolSurface(market.ReferenceDate);
                return new MarketCondition(
                    x => x.ValuationDate.Value = market.ReferenceDate,
                    x => x.DiscountCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
                    x => x.FixingCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
                    x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(valuationParameter.DividendCurveNames[0]).YieldCurve } },
                    x => x.RiskfreeCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
                    x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                    x => x.MktQuote.Value = new Dictionary<string, Tuple<PriceQuoteType, double>> { { TradeInfo.TradeId, Tuple.Create((bondMktData == null || string.IsNullOrEmpty(bondMktData.PriceQuoteType)) ? PriceQuoteType.None : bondMktData.PriceQuoteType.ToPriceQuoteType(), (bondMktData == null ? 100.0 : bondMktData.Quote)) } },
                    x => x.SpotPrices.Value = new Dictionary<string, double> { { "",market.GetData<StockMktData>(valuationParameter.UnderlyingId).Price } },
                    x => x.HistoricalIndexRates.Value = market.HistoricalIndexRates
                );
            }
        }

        private IMarketCondition GenerateMarketConditionFromPrebuilt(PrebuiltQdpMarket prebuiltMarket)
        {
            var valuationParameter = TradeInfo.ValuationParameters;
            var bondMktData = prebuiltMarket.BondPrices[TradeInfo.TradeId];
            var volsurf = prebuiltMarket.VolSurfaces[valuationParameter.VolSurfNames[0]];
            return new MarketCondition(
                    x => x.ValuationDate.Value = prebuiltMarket.ReferenceDate,
                    x => x.DiscountCurve.Value = prebuiltMarket.YieldCurves[valuationParameter.DiscountCurveName],
                    x => x.FixingCurve.Value = prebuiltMarket.YieldCurves[valuationParameter.DiscountCurveName],
                    x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", prebuiltMarket.YieldCurves[valuationParameter.DividendCurveNames[0]] } },
                    x => x.RiskfreeCurve.Value = prebuiltMarket.YieldCurves[valuationParameter.DiscountCurveName],
                    x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                    x => x.MktQuote.Value = new Dictionary<string, Tuple<PriceQuoteType, double>> { { TradeInfo.TradeId, Tuple.Create((bondMktData == null || string.IsNullOrEmpty(bondMktData.PriceQuoteType)) ? PriceQuoteType.None : bondMktData.PriceQuoteType.ToPriceQuoteType(), (bondMktData == null ? 100.0 : bondMktData.Quote)) } },
                    x => x.SpotPrices.Value = new Dictionary<string, double> { { "", prebuiltMarket.StockPrices[valuationParameter.UnderlyingId] } },
                    x => x.HistoricalIndexRates.Value = prebuiltMarket.HistoricalIndexRates
                );
        }
    }
}
