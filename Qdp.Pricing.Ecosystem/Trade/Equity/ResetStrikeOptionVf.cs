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

namespace Qdp.Pricing.Ecosystem.Trade.Equity
{
    public class ResetStrikeOptionVf : ValuationFunction<ResetStrikeOptionInfo, ResetStrikeOption>
    {
        public ResetStrikeOptionVf(ResetStrikeOptionInfo tradeInfo)
            : base(tradeInfo)
        {
        }

        public override ResetStrikeOption GenerateInstrument()
        {
            var startDate = TradeInfo.StartDate.ToDate();
            var calendar = TradeInfo.Calendar.ToCalendarImpl();
            var strikefixingDate = TradeInfo.StrikeFixingDate.ToDate();

            TradeUtil.GenerateOptionDates(TradeInfo, out Date[] exerciseDates, out Date[] obsDates, out DayGap settlementGap);

            var settlementDate = settlementGap.Get(calendar, exerciseDates.Single());

            return new ResetStrikeOption(
                startDate: startDate,
                maturityDate: exerciseDates.Last(),
                exercise: TradeInfo.Exercise.ToOptionExercise(),
                optionType: TradeInfo.OptionType.ToOptionType(),
                resetStrikeType: TradeInfo.ResetStrikeType.ToResetStrikeType(),
                strike: TradeInfo.Strike,
                underlyingInstrumentType: TradeInfo.UnderlyingInstrumentType.ToInstrumentType(),
                calendar: calendar,
                dayCount: TradeInfo.DayCount.ToDayCountImpl(),
                payoffCcy: string.IsNullOrEmpty(TradeInfo.PayoffCurrency) ? CurrencyCode.CNY : TradeInfo.PayoffCurrency.ToCurrencyCode(),
                settlementCcy: string.IsNullOrEmpty(TradeInfo.SettlementCurrency) ? CurrencyCode.CNY : TradeInfo.SettlementCurrency.ToCurrencyCode(),
                exerciseDates: exerciseDates,
                observationDates: obsDates,
                strikefixingDate: strikefixingDate,
                notional: TradeInfo.Notional,
                settlementGap: settlementGap,
                optionPremiumPaymentDate: string.IsNullOrEmpty(TradeInfo.OptionPremiumPaymentDate) ? null : TradeInfo.OptionPremiumPaymentDate.ToDate(),
                optionPremium: TradeInfo.OptionPremium ?? 0.0
            );
        }

        public override IEngine<ResetStrikeOption> GenerateEngine()
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
                    //return new AnalyticalAsianOptionEngineLegacy();
                    return new AnalyticalResetStrikeOptionEngine();
                }
            }
            else
            {
                throw new PricingBaseException("American/Bermudan exercise is not supported in ResetStrikeOption!");
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
                var volSurf = market.GetData<VolSurfMktData>(valuationParameter.VolSurfNames[0]).ToImpliedVolSurface(market.ReferenceDate);
                var spot = market.GetData<StockMktData>(valuationParameter.UnderlyingId).Price;
                return new MarketCondition(
                    x => x.ValuationDate.Value = market.ReferenceDate,
                    x => x.DiscountCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
                    x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(valuationParameter.DividendCurveNames[0]).YieldCurve } },
                    x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volSurf } },
                    x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } }
                    );
            }
        }

        private IMarketCondition GenerateMarketConditionFromPrebuilt(PrebuiltQdpMarket prebuiltMarket)
        {
            var valuationParameter = TradeInfo.ValuationParamter;
            var volsurf = prebuiltMarket.VolSurfaces[valuationParameter.VolSurfNames[0]];
            var spot = prebuiltMarket.StockPrices[valuationParameter.UnderlyingId];
            return new MarketCondition(
                    x => x.ValuationDate.Value = prebuiltMarket.ReferenceDate,
                    x => x.DiscountCurve.Value = prebuiltMarket.YieldCurves[valuationParameter.DiscountCurveName],
                    x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", prebuiltMarket.YieldCurves[valuationParameter.DividendCurveNames[0]] } },
                    x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                    x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } }
                    );
        }
    }


}
