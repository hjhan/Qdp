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
using Qdp.Pricing.Library.Common.MathMethods.VolTermStructure;

namespace Qdp.Pricing.Ecosystem.Trade.Equity
{
	public class BinaryOptionVf : ValuationFunction<BinaryOptionInfo, BinaryOption>
	{
		public BinaryOptionVf(BinaryOptionInfo tradeInfo)
			: base(tradeInfo)
		{
		}

		public override BinaryOption GenerateInstrument()
		{
			var startDate = TradeInfo.StartDate.ToDate();
			var maturityDate = TradeInfo.UnderlyingMaturityDate.ToDate();
            TradeUtil.GenerateOptionDates(TradeInfo, out Date[] exerciseDates, out Date[] obsDates, out DayGap settlementGap);

            return new BinaryOption(
				startDate,
                exerciseDates.Last(),
                TradeInfo.Exercise.ToOptionExercise(),
				TradeInfo.OptionType.ToOptionType(),
				TradeInfo.Strike,
				TradeInfo.UnderlyingInstrumentType.ToInstrumentType(),
				TradeInfo.BinaryOptionPayoffType.ToBinaryOptionPayoffType(),
				TradeInfo.CashOrNothingAmount,
				TradeInfo.Calendar.ToCalendarImpl(),
				TradeInfo.DayCount.ToDayCountImpl(),
				string.IsNullOrEmpty(TradeInfo.PayoffCurrency) ? CurrencyCode.CNY : TradeInfo.PayoffCurrency.ToCurrencyCode(),
				string.IsNullOrEmpty(TradeInfo.SettlementCurrency) ? CurrencyCode.CNY : TradeInfo.SettlementCurrency.ToCurrencyCode(),
				exerciseDates,
				obsDates,
				TradeInfo.Notional,
                settlementGap,
				string.IsNullOrEmpty(TradeInfo.OptionPremiumPaymentDate) ? null : TradeInfo.OptionPremiumPaymentDate.ToDate(),
				TradeInfo.OptionPremium ?? 0.0,
                binaryRebateType:string.IsNullOrEmpty(TradeInfo.BinaryRebateType) ? BinaryRebateType.AtHit : TradeInfo.BinaryRebateType.ToBinaryRebateType(),
                isMoneynessOption: TradeInfo.IsMoneynessOption,
                initialSpotPrice: TradeInfo.InitialSpotPrice,
                hasNightMarket: TradeInfo.HasNightMarket,
                commodityFuturesPreciseTimeMode: TradeInfo.CommodityFuturesPreciseTimeMode
            );
		}

		public override IEngine<BinaryOption> GenerateEngine()
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
					if (TradeInfo.BinaryOptionReplicationStrategy.ToBinaryOptionReplicationStrategy() != BinaryOptionReplicationStrategy.None)
					{
						//return new AnalyticalBinaryEuropeanOptionReplicationEngine(TradeInfo.ReplicationShiftSize, TradeInfo.BinaryOptionReplicationStrategy.ToBinaryOptionReplicationStrategy());
                        return new AnalyticalBinaryEuropeanOptionReplicationEngine(TradeInfo.ReplicationShiftSize, TradeInfo.BinaryOptionReplicationStrategy.ToBinaryOptionReplicationStrategy());
                    }
					return new AnalyticalBinaryEuropeanOptionEngine();
				}
			}
			else if (exercise == OptionExercise.American)
            {
                return new AnalyticalBinaryAmericanOptionEngine();
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
                ImpliedVolSurface volsurf = null;
                double spotPrice = Double.NaN;
                try
                {
                    volsurf = market.GetData<VolSurfMktData>(valuationParameter.VolSurfNames[0]).ToImpliedVolSurface(market.ReferenceDate);
                    spotPrice = market.GetData<StockMktData>(valuationParameter.UnderlyingId).Price;
                }
                catch (Exception ex)
                {
                    throw new PricingEcosystemException($"{TradeInfo.UnderlyingTicker} missing vol or spot price on date {market.ReferenceDate}");
                }
                return new MarketCondition(
                    x => x.ValuationDate.Value = market.ReferenceDate,
                    x => x.DiscountCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
                    x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(valuationParameter.DividendCurveNames[0]).YieldCurve } },
                    x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                    x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spotPrice } }
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
            var spotPrice = prebuiltMarket.StockPrices[valuationParameter.UnderlyingId];
            return new MarketCondition(
                    x => x.ValuationDate.Value = prebuiltMarket.ReferenceDate,
                    x => x.DiscountCurve.Value = prebuiltMarket.YieldCurves[valuationParameter.DiscountCurveName],
                    x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", prebuiltMarket.YieldCurves[valuationParameter.DividendCurveNames[0]] } },
                    x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                    x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spotPrice } }
                    );
        }

    }
}
