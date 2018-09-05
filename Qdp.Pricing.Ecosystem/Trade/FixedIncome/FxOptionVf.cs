using System.Linq;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Ecosystem.Utilities;
using Qdp.Pricing.Library.Common.Fx;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using Qdp.Pricing.Library.Equity.Fx;
using Qdp.Pricing.Library.Equity.Options;
using System.Collections.Generic;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace Qdp.Pricing.Ecosystem.Trade.FixedIncome
{
	public class FxOptionVf : ValuationFunction<FxOptionInfo, FxOption>
	{
		public FxOptionVf(FxOptionInfo tradeInfo) 
			: base(tradeInfo)
		{
		}

		public override FxOption GenerateInstrument()
		{
			return new FxOption(TradeInfo.StartDate.ToDate(),
				TradeInfo.MaturityDate.ToDate(),
				TradeInfo.Exercise.ToOptionExercise(),
				TradeInfo.OptionType.ToOptionType(),
				TradeInfo.StrikeFxRate,
				TradeInfo.UnderlyingInstrumentType.ToInstrumentType(),
				TradeInfo.BaseCcyCalendar.ToCalendarImpl(),
				TradeInfo.BaseCurrency.ToCurrencyCode(),
				TradeInfo.QuoteCcyCalendar.ToCalendarImpl(),
				TradeInfo.QuoteCurrency.ToCurrencyCode(),
				TradeInfo.DayCount.ToDayCountImpl(),
				TradeInfo.PayoffCurrency.ToCurrencyCode(),
				TradeInfo.SettlementCurrency.ToCurrencyCode(),
				TradeInfo.ExerciseDates.Select(x => x.ToDate()).ToArray(),
				TradeInfo.ObservationDates.Select(x => x.ToDate()).ToArray(),
				TradeInfo.UnderlyingFxSpotSettlement.ToDayGap(),
				TradeInfo.NotionalInQuoteCcy,
				TradeInfo.Settlment.ToDayGap(),
				TradeInfo.OptionPremiumPaymentDate.ToDate(),
				TradeInfo.OptionPremium
			);
		}

		public override IEngine<FxOption> GenerateEngine()
		{
			return new AnalyticalFxOptionEngine();
		}

		public override IMarketCondition GenerateMarketCondition(QdpMarket market)
		{
			var valuationParameter = TradeInfo.ValuationParamters;
            var volsurf = market.GetData<VolSurfMktData>(valuationParameter.FxVolSurfName).ToImpliedVolSurface(market.ReferenceDate);


            return new MarketCondition(
				x => x.ValuationDate.Value = market.ReferenceDate,
				x => x.DiscountCurve.Value = market.GetData<CurveData>(valuationParameter.DomCcyDiscountCurveName).YieldCurve,
				x => x.FgnDiscountCurve.Value = market.GetData<CurveData>(valuationParameter.FgnCcyDiscountCurveName).YieldCurve,
				x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }

                );
		}
	}
}
