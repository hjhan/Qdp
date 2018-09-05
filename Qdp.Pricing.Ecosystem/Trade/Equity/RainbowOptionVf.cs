using System.Linq;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Ecosystem.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using Qdp.Pricing.Library.Equity.Engines.MonteCarlo;
using Qdp.Pricing.Library.Equity.Options;
using System.Collections.Generic;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using System;

namespace Qdp.Pricing.Ecosystem.Trade.Equity
{
    public class RainbowOptionVf : ValuationFunction<RainbowOptionInfo, RainbowOption>
    {
        public RainbowOptionVf(RainbowOptionInfo tradeInfo)
            : base(tradeInfo)
        {
        }

        public override RainbowOption GenerateInstrument()
        {
            var startDate = TradeInfo.StartDate.ToDate();
            var calendar = TradeInfo.Calendar.ToCalendarImpl();
            TradeUtil.GenerateOptionDates(TradeInfo, out Date[] exerciseDates, out Date[] obsDates, out DayGap settlementGap);

            return new RainbowOption(
                startDate: startDate,
                maturityDate: exerciseDates.Last(),
                exercise: TradeInfo.Exercise.ToOptionExercise(),
                optionType: TradeInfo.OptionType.ToOptionType(),
                rainbowType: TradeInfo.RainbowType.ToRainbowType(),
                strikes: TradeInfo.Strikes,
                cashAmount: TradeInfo.CashAmount,
                underlyingInstrumentType: TradeInfo.UnderlyingInstrumentType.ToInstrumentType(),
                calendar: TradeInfo.Calendar.ToCalendarImpl(),
                dayCount: TradeInfo.DayCount.ToDayCountImpl(),
                payoffCcy: string.IsNullOrEmpty(TradeInfo.PayoffCurrency) ? CurrencyCode.CNY : TradeInfo.PayoffCurrency.ToCurrencyCode(),
                settlementCcy: string.IsNullOrEmpty(TradeInfo.SettlementCurrency) ? CurrencyCode.CNY : TradeInfo.SettlementCurrency.ToCurrencyCode(),
                exerciseDates: exerciseDates,
                observationDates: obsDates,
                underlyingTickers : TradeInfo.UnderlyingTickers,
                notional: TradeInfo.Notional,
                settlementGap: settlementGap,
                optionPremiumPaymentDate: string.IsNullOrEmpty(TradeInfo.OptionPremiumPaymentDate) ? null : TradeInfo.OptionPremiumPaymentDate.ToDate(),
                optionPremium: TradeInfo.OptionPremium ?? 0.0
            );
        }

        public override IEngine<RainbowOption> GenerateEngine()
        {
            var exercise = TradeInfo.Exercise.ToOptionExercise();
            if (exercise == OptionExercise.European)
            {
                if (TradeInfo.MonteCarlo)
                {
                    return new GenericMonteCarloEngine(TradeInfo.ParallelDegree ?? 2, TradeInfo.NSimulations ?? 50000);
                }
                else
                {
                    return new AnalyticalRainbowOptionEngine();
                }
            }
            else
            {
                throw new PricingBaseException("American/Bermudan exercise is not supported in RainbowOption!");
            }
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
                var valuationParameter = TradeInfo.ValuationParamter;
                var vols = prepareVols(valuationParameter.VolSurfNames, market);
                var ticker1 = TradeInfo.UnderlyingTickers[0];
                var ticker2 = TradeInfo.UnderlyingTickers[1];
                double price1 = Double.NaN;
                double price2 = Double.NaN;
                try
                {
                    price1 = market.GetData<StockMktData>(ticker1).Price;
                    price2 = market.GetData<StockMktData>(ticker2).Price;
                }
                catch (Exception ex)
                {
                    throw new PricingEcosystemException($"{ticker1} or {ticker2} missing spot price on date {market.ReferenceDate}");
                }
                var prices = new Dictionary<string, double> { { ticker1, price1 }, { ticker2, price2 } };
                var divs = prepareDividends(valuationParameter.DividendCurveNames, market);

                return new MarketCondition(
                    x => x.ValuationDate.Value = market.ReferenceDate,
                    x => x.DiscountCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
                    x => x.DividendCurves.Value =  divs,
                    x => x.VolSurfaces.Value = vols,
                    x => x.SpotPrices.Value = prices
                    );
            }
        }

        private Dictionary<string, IVolSurface>  prepareVols( string[] volSurfNames, QdpMarket market) {
            try
            {
                var tickers = volSurfNames; //;
                var vol1 = market.GetData<VolSurfMktData>(tickers[0]).ToImpliedVolSurface(market.ReferenceDate);
                var vol2 = market.GetData<VolSurfMktData>(tickers[1]).ToImpliedVolSurface(market.ReferenceDate);
                return new Dictionary<string, IVolSurface> { { tickers[0], vol1 }, { tickers[1], vol2 } };
            }
            catch(Exception ex)
            {
                throw new PricingEcosystemException($"{volSurfNames[0]} or {volSurfNames[1]} missing on date {market.ReferenceDate}");
            }
        }

        private Dictionary<string, IYieldCurve> prepareDividends(string[] dividendNames, QdpMarket market)
        {
            var tickers = dividendNames; //;

            var div1 = market.GetData<CurveData>(tickers[0]).YieldCurve;
            var div2 = market.GetData<CurveData>(tickers[1]).YieldCurve;
            return new Dictionary<string, IYieldCurve> { { tickers[0], div1 }, { tickers[1], div2 } };
        }


        private IMarketCondition GenerateMarketConditionFromPrebuilt(PrebuiltQdpMarket prebuiltMarket)
        {
            var valuationParameter = TradeInfo.ValuationParamter;
            var ticker1 = TradeInfo.UnderlyingTickers[0];
            var ticker2 = TradeInfo.UnderlyingTickers[1];

            if (!prebuiltMarket.VolSurfaces.ContainsKey(valuationParameter.VolSurfNames[0])
                || prebuiltMarket.VolSurfaces.ContainsKey(valuationParameter.VolSurfNames[1]))
            {
                throw new PricingEcosystemException($"VolSurface of {ticker1} {ticker2} is missing.", TradeInfo);
            }

            if (!prebuiltMarket.StockPrices.ContainsKey(ticker1)
                || !prebuiltMarket.StockPrices.ContainsKey(ticker2))
            {
                throw new PricingEcosystemException($"Spot price of {ticker1} {ticker2} is missing.", TradeInfo);
            }

            var vol1 = prebuiltMarket.VolSurfaces[valuationParameter.VolSurfNames[0]];
            var vol2 = prebuiltMarket.VolSurfaces[valuationParameter.VolSurfNames[1]];
            var vols = new Dictionary<string, IVolSurface>() { { ticker1, vol1 }, { ticker2, vol2 } };

            var price1 = prebuiltMarket.StockPrices[ticker1];
            var price2 = prebuiltMarket.StockPrices[ticker2];
            var prices = new Dictionary<string, double> { { ticker1, price1 }, { ticker2, price2} };

            Dictionary<string, IYieldCurve> divs;

            if (valuationParameter.DividendCurveNames.Length < 2)
            {
                divs = new Dictionary<string, IYieldCurve> { { ticker1, null }, { ticker2, null } };
            }
            else
            {
                var div1 = prebuiltMarket.YieldCurves[valuationParameter.DividendCurveNames[0]];
                var div2 = prebuiltMarket.YieldCurves[valuationParameter.DividendCurveNames[1]];
                divs = new Dictionary<string, IYieldCurve> { { ticker1, div1 }, { ticker2, div2 } };
            }           
            var corrName = valuationParameter.CorrSurfNames[0];
            IVolSurface corr = null;
            if (prebuiltMarket.CorrSurfaces[corrName] == null)
            {
                corr = prebuiltMarket.CorrSurfaces.Values.First();
            }
            else
            {
                corr = prebuiltMarket.CorrSurfaces[corrName];
            }

            return new MarketCondition(
                    x => x.ValuationDate.Value = prebuiltMarket.ReferenceDate,
                    x => x.DiscountCurve.Value = prebuiltMarket.YieldCurves[valuationParameter.DiscountCurveName],
                    x => x.DividendCurves.Value = divs,
                    x => x.VolSurfaces.Value = vols,
                    x => x.SpotPrices.Value = prices,
                    x => x.Correlations.Value = new Dictionary<string, IVolSurface>() { { ticker1, corr } }
                    );
        }

    }
}