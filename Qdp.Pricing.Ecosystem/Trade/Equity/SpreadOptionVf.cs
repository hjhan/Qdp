using System.Linq;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
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
using Qdp.Pricing.Library.Common.MathMethods.VolTermStructure;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity;
using System;

namespace Qdp.Pricing.Ecosystem.Trade.Equity
{
    public class SpreadOptionVf : ValuationFunction<SpreadOptionInfo, SpreadOption>
    {
        public SpreadOptionVf(SpreadOptionInfo tradeInfo)
            : base(tradeInfo)
        {
        }

        public override SpreadOption GenerateInstrument()
        {
            var startDate = TradeInfo.StartDate.ToDate();
            var calendar = TradeInfo.Calendar.ToCalendarImpl();
            TradeUtil.GenerateOptionDates(TradeInfo, out Date[] exerciseDates, out Date[] obsDates, out DayGap settlementGap);

            return new SpreadOption(
                startDate: startDate,
                maturityDate: exerciseDates.Last(),
                exercise: TradeInfo.Exercise.ToOptionExercise(),
                optionType: TradeInfo.OptionType.ToOptionType(),
                spreadType: TradeInfo.SpreadType.ToSpreadType(),
                strike: TradeInfo.Strike,
                weights: TradeInfo.Weights,
                underlyingInstrumentType: TradeInfo.UnderlyingInstrumentType.ToInstrumentType(),
                calendar: TradeInfo.Calendar.ToCalendarImpl(),
                dayCount: TradeInfo.DayCount.ToDayCountImpl(),
                payoffCcy: string.IsNullOrEmpty(TradeInfo.PayoffCurrency) ? CurrencyCode.CNY : TradeInfo.PayoffCurrency.ToCurrencyCode(),
                settlementCcy: string.IsNullOrEmpty(TradeInfo.SettlementCurrency) ? CurrencyCode.CNY : TradeInfo.SettlementCurrency.ToCurrencyCode(),
                exerciseDates: exerciseDates,
                observationDates: obsDates,
                underlyingTickers: TradeInfo.UnderlyingTickers,
                notional: TradeInfo.Notional,
                settlementGap: settlementGap,
                optionPremiumPaymentDate: string.IsNullOrEmpty(TradeInfo.OptionPremiumPaymentDate) ? null : TradeInfo.OptionPremiumPaymentDate.ToDate(),
                optionPremium: TradeInfo.OptionPremium ?? 0.0
            );
        }

        public override IEngine<SpreadOption> GenerateEngine()
        {
            var exercise = TradeInfo.Exercise.ToOptionExercise();
            if (exercise == OptionExercise.European)
            {
                //TODO:  monte carlo is not supported for spread option yet
                if (TradeInfo.MonteCarlo)
                {
                    return new GenericMonteCarloEngine(TradeInfo.ParallelDegree ?? 2, TradeInfo.NSimulations ?? 50000);
                }
                else
                {
                    if (TradeInfo.PricingStrategy == "Bjerksund&Stensland")
                    {
                        return new AnalyticalSpreadOptionBjerksundEngine();
                    }
                    return new AnalyticalSpreadOptionKirkEngine();
                }
            }
            else
            {
                throw new PricingBaseException("American/Bermudan exercise is not supported in BinaryOption!");
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
                var ticker3 = TradeInfo.UnderlyingTickers[2];
                var ticker4 = TradeInfo.UnderlyingTickers[3];
                double price1 = Double.NaN;
                double price2 = Double.NaN;
                double price3 = Double.NaN;
                double price4 = Double.NaN;
                try
                {
                    price1 = market.GetData<StockMktData>(ticker1).Price;
                    price2 = market.GetData<StockMktData>(ticker2).Price;
                    price3 = market.GetData<StockMktData>(ticker3).Price;
                    price4 = market.GetData<StockMktData>(ticker4).Price;
                }
                catch (Exception ex)
                {
                    throw new PricingEcosystemException($"{ticker1} or {ticker2} or {ticker3} or {ticker4} missing spot price on date {market.ReferenceDate}");
                }
                var prices = new Dictionary<string, double> { { ticker1, price1 }, { ticker2, price2 }, { ticker3, price3 }, { ticker4, price4 } };
                var divs = prepareDividends(valuationParameter.DividendCurveNames, market);

                return new MarketCondition(
                    x => x.ValuationDate.Value = market.ReferenceDate,
                    x => x.DiscountCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
                    x => x.DividendCurves.Value = divs,
                    x => x.VolSurfaces.Value = vols,
                    x => x.SpotPrices.Value = prices
                    );
            }
        }

        private Dictionary<string, IVolSurface> prepareVols(string[] volSurfNames, QdpMarket market)
        {
            try
            {
                var tickers = volSurfNames; //;
                var vol1 = market.GetData<VolSurfMktData>(tickers[0]).ToImpliedVolSurface(market.ReferenceDate);
                var vol2 = market.GetData<VolSurfMktData>(tickers[1]).ToImpliedVolSurface(market.ReferenceDate);
                var vol3 = market.GetData<VolSurfMktData>(tickers[2]).ToImpliedVolSurface(market.ReferenceDate);
                var vol4 = market.GetData<VolSurfMktData>(tickers[3]).ToImpliedVolSurface(market.ReferenceDate);
                return new Dictionary<string, IVolSurface> { { tickers[0], vol1 }, { tickers[1], vol2 }, { tickers[2], vol3 }, { tickers[3], vol4 } };
            }
            catch (Exception ex)
            {
                throw new PricingEcosystemException($"{volSurfNames[0]} or {volSurfNames[1]} or {volSurfNames[2]} or {volSurfNames[3]} missing on date {market.ReferenceDate}");
            }
        }

        private Dictionary<string, IYieldCurve> prepareDividends(string[] dividendNames, QdpMarket market)
        {
            var tickers = dividendNames; //;

            var div1 = market.GetData<CurveData>(tickers[0]).YieldCurve;
            var div2 = market.GetData<CurveData>(tickers[1]).YieldCurve;
            var div3 = market.GetData<CurveData>(tickers[2]).YieldCurve;
            var div4 = market.GetData<CurveData>(tickers[3]).YieldCurve;
            return new Dictionary<string, IYieldCurve> { { tickers[0], div1 }, { tickers[1], div2 }, { tickers[2], div3 }, { tickers[3], div4 } };
        }


        private IMarketCondition GenerateMarketConditionFromPrebuilt(PrebuiltQdpMarket prebuiltMarket)
        {
            var valuationParameter = TradeInfo.ValuationParamter;
            var ticker1 = TradeInfo.UnderlyingTickers[0];
            var ticker2 = TradeInfo.UnderlyingTickers[1];
            string ticker3 = "", ticker4 = "";
            ImpliedVolSurface vol3 = null, vol4 = null,  corr13 = null, corr23 = null, corr14 = null, corr24 = null, corr34 = null;
            IYieldCurve div3 = null, div4 = null ;
            Dictionary<string, IYieldCurve> divs;
            double price3 = 0.0, price4 = 0.0;

            if (!prebuiltMarket.VolSurfaces.ContainsKey(valuationParameter.VolSurfNames[0])
                || prebuiltMarket.VolSurfaces.ContainsKey(valuationParameter.VolSurfNames[1]))
            {
                throw new PricingEcosystemException($"VolSurface of {ticker1} or {ticker2} is missing.", TradeInfo);
            }

            if (!prebuiltMarket.StockPrices.ContainsKey(ticker1)
                || !prebuiltMarket.StockPrices.ContainsKey(ticker2))
            {
                throw new PricingEcosystemException($"Spot price of {ticker1} or {ticker2} is missing.", TradeInfo);
            }

            if (!prebuiltMarket.CorrSurfaces.ContainsKey(valuationParameter.CorrSurfNames[0]))
            {
                throw new PricingEcosystemException($"Correlation of {valuationParameter.CorrSurfNames[0]} is missing", TradeInfo);
            }

            var vol1 = prebuiltMarket.VolSurfaces[valuationParameter.VolSurfNames[0]];
            var vol2 = prebuiltMarket.VolSurfaces[valuationParameter.VolSurfNames[1]];
            var price1 = prebuiltMarket.StockPrices[ticker1];
            var price2 = prebuiltMarket.StockPrices[ticker2];
            var corr12 = prebuiltMarket.CorrSurfaces[valuationParameter.CorrSurfNames[0]];
            var div1 = prebuiltMarket.YieldCurves[valuationParameter.DividendCurveNames[0]];
            var div2 = prebuiltMarket.YieldCurves[valuationParameter.DividendCurveNames[1]];

            var vols = new Dictionary<string, IVolSurface>() { { ticker1, vol1 }, { ticker2, vol2 } };
            var prices = new Dictionary<string, double>() { { ticker1, price1 }, { ticker2, price2 } };
            if (valuationParameter.DividendCurveNames.Length == 1)
            {
                divs= new Dictionary<string, IYieldCurve>() { { ticker1, null }, { ticker2, null } };
            }
            else
            {
                divs = new Dictionary<string, IYieldCurve>() { { ticker1, div1 }, { ticker2, div2 } };
            }
            var corrs = new Dictionary<string, IVolSurface>() { { ticker1 + ticker2, corr12 } };

            if (TradeInfo.UnderlyingTickers.Length > 2)
            {
                ticker3 = TradeInfo.UnderlyingTickers[2];

                if (!prebuiltMarket.VolSurfaces.ContainsKey(valuationParameter.VolSurfNames[2]))
                {
                    throw new PricingEcosystemException($"VolSurface of {ticker3} is missing.", TradeInfo);
                }

                if (!prebuiltMarket.StockPrices.ContainsKey(ticker3))
                {
                    throw new PricingEcosystemException($"Spot price of {ticker3} is missing.", TradeInfo);
                }

                if (!prebuiltMarket.CorrSurfaces.ContainsKey(valuationParameter.CorrSurfNames[1])
                    || !prebuiltMarket.CorrSurfaces.ContainsKey(valuationParameter.CorrSurfNames[2]))
                {
                    throw new PricingEcosystemException($"Correlation of {valuationParameter.CorrSurfNames[1]} {valuationParameter.CorrSurfNames[2]} is missing", TradeInfo);
                }

                vol3 = prebuiltMarket.VolSurfaces[valuationParameter.VolSurfNames[2]];
                price3 = prebuiltMarket.StockPrices[ticker3];
                corr13 = prebuiltMarket.CorrSurfaces[valuationParameter.CorrSurfNames[1]];
                corr23 = prebuiltMarket.CorrSurfaces[valuationParameter.CorrSurfNames[2]];
                div3 = prebuiltMarket.YieldCurves[valuationParameter.DividendCurveNames[2]];
                vols = new Dictionary<string, IVolSurface>() { { ticker1, vol1 }, { ticker2, vol2 }, { ticker3, vol3 }};
                prices = new Dictionary<string, double> { { ticker1, price1 }, { ticker2, price2 }, { ticker3, price3 } };
                if (valuationParameter.DividendCurveNames.Length == 1)
                {
                    divs = new Dictionary<string, IYieldCurve>() { { ticker1, null }, { ticker2, null }, { ticker3, null } };
                }
                else
                {
                    divs = new Dictionary<string, IYieldCurve> { { ticker1, div1 }, { ticker2, div2 }, { ticker3, div3 } };
                }              
                corrs = new Dictionary<string, IVolSurface>() { { ticker1+ticker2, corr12 }, { ticker1+ticker3, corr13 },{ ticker2+ticker3, corr23 }};

            }

            if (TradeInfo.UnderlyingTickers.Length > 3)
            {
                ticker4 = TradeInfo.UnderlyingTickers[3];

                if (!prebuiltMarket.VolSurfaces.ContainsKey(valuationParameter.VolSurfNames[3]))
                {
                    throw new PricingEcosystemException($"VolSurface of {ticker4} is missing.", TradeInfo);
                }

                if (!prebuiltMarket.StockPrices.ContainsKey(ticker4))
                {
                    throw new PricingEcosystemException($"Spot price of {ticker4} is missing.", TradeInfo);
                }

                if (!prebuiltMarket.CorrSurfaces.ContainsKey(valuationParameter.CorrSurfNames[3])
                    || !prebuiltMarket.CorrSurfaces.ContainsKey(valuationParameter.CorrSurfNames[4])
                    || !prebuiltMarket.CorrSurfaces.ContainsKey(valuationParameter.CorrSurfNames[5]))
                {
                    throw new PricingEcosystemException($"Correlation of {valuationParameter.CorrSurfNames[3]} {valuationParameter.CorrSurfNames[4]} {valuationParameter.CorrSurfNames[5]} is missing.", TradeInfo);
                }

                vol4 = prebuiltMarket.VolSurfaces[valuationParameter.VolSurfNames[3]];
                price4 = prebuiltMarket.StockPrices[ticker4];
                corr14 = prebuiltMarket.CorrSurfaces[valuationParameter.CorrSurfNames[3]];
                corr24 = prebuiltMarket.CorrSurfaces[valuationParameter.CorrSurfNames[4]];
                corr34 = prebuiltMarket.CorrSurfaces[valuationParameter.CorrSurfNames[5]];
                div4 = prebuiltMarket.YieldCurves[valuationParameter.DividendCurveNames[3]];
                vols = new Dictionary<string, IVolSurface>() { { ticker1, vol1 }, { ticker2, vol2 }, { ticker3, vol3 }, { ticker4, vol4 } };
                prices = new Dictionary<string, double> { { ticker1, price1 }, { ticker2, price2 }, { ticker3, price3 }, { ticker4, price4 } };
                if (valuationParameter.DividendCurveNames.Length == 1)
                {
                    divs = new Dictionary<string, IYieldCurve>() { { ticker1, null }, { ticker2, null }, { ticker3, null } , { ticker4, null } };
                }
                else
                {
                    divs = new Dictionary<string, IYieldCurve> { { ticker1, div1 }, { ticker2, div2 }, { ticker3, div3 }, { ticker4, div4 } };
                }              
                corrs = new Dictionary<string, IVolSurface>() { { ticker1+ticker2, corr12 }, { ticker1+ticker3, corr13 },
                { ticker2+ticker3, corr23 }, { ticker1+ticker4, corr14 },{ ticker2+ticker4, corr24 }, { ticker3+ticker4, corr34 } };
            }                   

            return new MarketCondition(
                    x => x.ValuationDate.Value = prebuiltMarket.ReferenceDate,
                    x => x.DiscountCurve.Value = prebuiltMarket.YieldCurves[valuationParameter.DiscountCurveName],
                    x => x.DividendCurves.Value = divs,
                    x => x.VolSurfaces.Value = vols,
                    x => x.SpotPrices.Value = prices,
                    x => x.Correlations.Value = corrs
                    );
        }

    }
}