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
using Qdp.Pricing.Library.Equity.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace Qdp.Pricing.Ecosystem.Trade.Equity
{
	public class RangeAccrualVf : ValuationFunction<RangeAccrualInfo, RangeAccrual>
	{
		public RangeAccrualVf(RangeAccrualInfo tradeInfo)
			: base(tradeInfo)
		{
		}

		public override RangeAccrual GenerateInstrument()
		{
			var startDate = TradeInfo.StartDate.ToDate();
			var calendar = TradeInfo.Calendar.ToCalendarImpl();
            TradeUtil.GenerateOptionDates(TradeInfo, out Date[] exerciseDates, out Date[] obsDates, out DayGap settlementGap);

            var settlementDate = settlementGap.Get(calendar, exerciseDates.Single());

			var ranges = TradeInfo.Ranges.Split(QdpConsts.Semilicon)
				.Select(x =>
				{
					var splits = x.Split(QdpConsts.Comma);
					return new RangeDefinition(double.Parse(splits[0]), double.Parse(splits[1]), double.Parse(splits[2]), settlementDate, obsDates);
				}).ToArray();
			var fixings = string.IsNullOrEmpty(TradeInfo.Fixings)
				? new Dictionary<Date, double>() :
				TradeInfo.Fixings.Split(QdpConsts.Semilicon)
					.Select(x =>
					{
						var splits = x.Split(QdpConsts.Comma);
						return Tuple.Create(splits[0].ToDate(), double.Parse(splits[1]));
					}).ToDictionary(x => x.Item1, x => x.Item2);

			return new RangeAccrual(
				startDate,
                exerciseDates.Last(),
				TradeInfo.Exercise.ToOptionExercise(),
				TradeInfo.OptionType.ToOptionType(),
				TradeInfo.Strike,
				ranges,
				TradeInfo.UnderlyingInstrumentType.ToInstrumentType(),
				calendar,
				TradeInfo.DayCount.ToDayCountImpl(),
				string.IsNullOrEmpty(TradeInfo.PayoffCurrency) ? CurrencyCode.CNY : TradeInfo.PayoffCurrency.ToCurrencyCode(),
				string.IsNullOrEmpty(TradeInfo.SettlementCurrency) ? CurrencyCode.CNY : TradeInfo.SettlementCurrency.ToCurrencyCode(),
				exerciseDates,
				obsDates,
				fixings,
				TradeInfo.Notional,
                settlementGap,
				string.IsNullOrEmpty(TradeInfo.OptionPremiumPaymentDate) ? null : TradeInfo.OptionPremiumPaymentDate.ToDate(),
				TradeInfo.OptionPremium ?? 0.0
			);
		}

		public override IEngine<RangeAccrual> GenerateEngine()
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
					return new AnalyticalRangeAccrualEngine();
				}
			}
			else
			{
				throw new PricingBaseException("American/Bermudan exercise is not supported in Range accrual!");
			}
		}

		public override IMarketCondition GenerateMarketCondition(QdpMarket market)
		{
			var valuationParameter = TradeInfo.ValuationParamter;
            var volsurf = market.GetData<VolSurfMktData>(valuationParameter.VolSurfNames[0]).ToImpliedVolSurface(market.ReferenceDate);
            var spotPrice = market.GetData<StockMktData>(valuationParameter.UnderlyingId).Price;
            return new MarketCondition(
				x => x.ValuationDate.Value = market.ReferenceDate,
				x => x.DiscountCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
				x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(valuationParameter.DividendCurveNames[0]).YieldCurve } },
				x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spotPrice } }
                );
		}
	}
}
