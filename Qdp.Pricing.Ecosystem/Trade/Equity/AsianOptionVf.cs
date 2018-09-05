using System;
using System.Collections.Generic;
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
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.VolTermStructure;

namespace Qdp.Pricing.Ecosystem.Trade.Equity
{
	public class AsianOptionVf : ValuationFunction<AsianOptionInfo, AsianOption>
	{
		public AsianOptionVf(AsianOptionInfo tradeInfo)
			: base(tradeInfo)
		{
		}

		public override AsianOption GenerateInstrument()
		{
			var startDate = TradeInfo.StartDate.ToDate();
            var calendar = TradeInfo.Calendar.ToCalendarImpl();
            TradeUtil.GenerateOptionDates(TradeInfo, out Date[] exerciseDates, out Date[] obsDates, out DayGap settlementGap);

            if (TradeInfo.AveragingPeriodStartDate != null)
            {
                obsDates = calendar.BizDaysBetweenDatesInclEndDay(TradeInfo.AveragingPeriodStartDate.ToDate(), exerciseDates.Last()).ToArray();
            }

            var fixings = string.IsNullOrEmpty(TradeInfo.Fixings)
				? new Dictionary<Date, double>() :
				TradeInfo.Fixings.Split(QdpConsts.Semilicon)
					.Select(x =>
					{
						var splits = x.Split(QdpConsts.Comma);
						return Tuple.Create(splits[0].ToDate(), double.Parse(splits[1]));
					}).ToDictionary(x => x.Item1, x => x.Item2);

			return new AsianOption(
				startDate,
                exerciseDates.Last(),
				TradeInfo.Exercise.ToOptionExercise(),
				TradeInfo.OptionType.ToOptionType(),
                TradeInfo.AsianType.ToAsianType(),
                TradeInfo.StrikeStyle.ToStrikeStyle(),
				TradeInfo.Strike,
				TradeInfo.UnderlyingInstrumentType.ToInstrumentType(),
				calendar,
				TradeInfo.DayCount.ToDayCountImpl(),
                payoffCcy: string.IsNullOrEmpty(TradeInfo.PayoffCurrency) ? CurrencyCode.CNY : TradeInfo.PayoffCurrency.ToCurrencyCode(),
                settlementCcy: string.IsNullOrEmpty(TradeInfo.SettlementCurrency) ? CurrencyCode.CNY : TradeInfo.SettlementCurrency.ToCurrencyCode(),
                exerciseDates: exerciseDates,
                observationDates: obsDates,
                fixings: fixings,
				notional: TradeInfo.Notional,
                settlementGap: settlementGap,
                optionPremiumPaymentDate: string.IsNullOrEmpty(TradeInfo.OptionPremiumPaymentDate) ? null : TradeInfo.OptionPremiumPaymentDate.ToDate(),
                optionPremium: TradeInfo.OptionPremium ?? 0.0,
                isMoneynessOption: TradeInfo.IsMoneynessOption,
                initialSpotPrice: TradeInfo.InitialSpotPrice,
                hasNightMarket: TradeInfo.HasNightMarket,
                commodityFuturesPreciseTimeMode: TradeInfo.CommodityFuturesPreciseTimeMode
            );
		}

		public override IEngine<AsianOption> GenerateEngine()
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
                    return new AnalyticalAsianOptionEngine();
                }
            }
            else if (exercise == OptionExercise.American) {
                //2018/07/11, use european engine to price american style averaging option for now
                return new AnalyticalAsianOptionEngine();
            }
            else
            {
                throw new PricingBaseException("American/Bermudan exercise is not supported in AsianOption!");
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
                ImpliedVolSurface volSurf = null;
                double spot = Double.NaN;
                try
                {
                    volSurf = market.GetData<VolSurfMktData>(valuationParameter.VolSurfNames[0]).ToImpliedVolSurface(market.ReferenceDate);
                    spot = market.GetData<StockMktData>(valuationParameter.UnderlyingId).Price;
                }
                catch (Exception ex)
                {
                    throw new PricingEcosystemException($"{TradeInfo.UnderlyingTicker} missing vol or spot price on date {market.ReferenceDate}");
                }
                return new MarketCondition(
                    x => x.ValuationDate.Value = market.ReferenceDate,
                    x => x.DiscountCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
                    x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(valuationParameter.DividendCurveNames[0]).YieldCurve } },
                    x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface >{ {"", volSurf } },
                    x => x.SpotPrices.Value = new Dictionary<string, double> { { "",spot} }
                    );
            }
        }

        private IMarketCondition GenerateMarketConditionFromPrebuilt(PrebuiltQdpMarket prebuiltMarket)
        {
            var valuationParameter = TradeInfo.ValuationParamter;

            if (!prebuiltMarket.VolSurfaces.ContainsKey(valuationParameter.VolSurfNames[0]))
            {
                throw new PricingEcosystemException($"VolSurface of {TradeInfo.UnderlyingTicker} is missing.", TradeInfo);
            }

            if (!prebuiltMarket.StockPrices.ContainsKey(valuationParameter.UnderlyingId))
            {
                throw new PricingEcosystemException($"Spot price of {TradeInfo.UnderlyingTicker} is missing.", TradeInfo);
            }

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
