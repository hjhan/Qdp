using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.MathMethods.Processes.ShortRate;

namespace Qdp.Pricing.Ecosystem.Trade.FixedIncome
{
	public class BondFuturesVf : ValuationFunction<BondFuturesInfo, BondFutures>
	{
		public BondFuturesVf(BondFuturesInfo tradeInfo)
			: base(tradeInfo)
		{
		}

		public override BondFutures GenerateInstrument()
		{
			return new BondFutures(
				TradeInfo.TradeId,
				TradeInfo.StartDate.ToDate(),
				TradeInfo.MaturityDate.ToDate(),
				TradeInfo.MaturityDate.ToDate(),
				TradeInfo.Calendar.ToCalendarImpl(),
				TradeInfo.DeliverableBondInfos.Select(x => new BondVf(x).GenerateInstrument()).ToArray(),
				TradeInfo.DayCount.ToDayCountImpl(),
				TradeInfo.Currency.ToCurrencyCode(),
				TradeInfo.Notional,
				TradeInfo.NominalCoupon
				);
		}

		public override IEngine<BondFutures> GenerateEngine()
		{
			return new BondFuturesEngine<HullWhite>(new HullWhite(), 30);
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
                var valuationParameter = TradeInfo.ValuationParamters;
                var mktQuote = new Dictionary<string, Tuple<PriceQuoteType, double>>();
                try
                {
                    var futMktData = market.GetData<FuturesMktData>(TradeInfo.TradeId);
                    mktQuote[TradeInfo.TradeId] = futMktData != null ? Tuple.Create(PriceQuoteType.Dirty, futMktData.FuturesPrice) : null;
                    foreach (var deliverableBondInfo in TradeInfo.DeliverableBondInfos)
                    {
                        var bondMktData = market.GetData<BondMktData>(deliverableBondInfo.TradeId);
                        if (bondMktData != null)
                        {
                            mktQuote[deliverableBondInfo.TradeId] = Tuple.Create(bondMktData.PriceQuoteType.ToPriceQuoteType(), bondMktData.Quote);
                        }
                        var tfBondId = TradeInfo.TradeId + "_" + deliverableBondInfo.TradeId;
                        var tfMktData = market.GetData<TreasuryFutureMktData>(tfBondId);
                        if (tfMktData != null)
                        {
                            mktQuote[tfBondId] = Tuple.Create(tfMktData.PriceQuoteType.ToPriceQuoteType(), tfMktData.Quote);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new PricingBaseException(string.Format("Market price is missing, detailed message: {0}", ex.Message));
                }

                var fixingCurveName = valuationParameter.FixingCurveName;
                if (string.IsNullOrEmpty(fixingCurveName))
                {
                    fixingCurveName = valuationParameter.DiscountCurveName;
                }

                return new MarketCondition(
                    x => x.ValuationDate.Value = market.ReferenceDate,
                    x => x.DiscountCurve.Value = market.GetData<CurveData>(valuationParameter.DiscountCurveName).YieldCurve,
                    x => x.FixingCurve.Value = market.GetData<CurveData>(fixingCurveName).YieldCurve,
                    x => x.RiskfreeCurve.Value = market.GetData<CurveData>(valuationParameter.RiskfreeCurveName).YieldCurve,
                    x => x.MktQuote.Value = mktQuote,
                    x => x.HistoricalIndexRates.Value = market.HistoricalIndexRates
                    );
            }
		}

        private IMarketCondition GenerateMarketConditionFromPrebuilt(PrebuiltQdpMarket prebuiltMarket)
        {
            var valuationParameter = TradeInfo.ValuationParamters;
            var mktQuote = new Dictionary<string, Tuple<PriceQuoteType, double>>();
            try
            {
                if (prebuiltMarket.BondFuturePrices.ContainsKey(TradeInfo.TradeId))
                {
                    var futurePrice = prebuiltMarket.BondFuturePrices[TradeInfo.TradeId];
                    mktQuote[TradeInfo.TradeId] = Tuple.Create(PriceQuoteType.Dirty, futurePrice);
                }
                else
                {
                    mktQuote[TradeInfo.TradeId] = null;
                }
                foreach (var deliverableBondInfo in TradeInfo.DeliverableBondInfos)
                {
                    //var bondMktData = 
                    BondMktData bondMktData = null;
                    if (prebuiltMarket.BondPrices.ContainsKey(deliverableBondInfo.TradeId))
                    {
                        bondMktData = prebuiltMarket.BondPrices[deliverableBondInfo.TradeId];
                    }
                    if (bondMktData != null)
                    {
                        mktQuote[deliverableBondInfo.TradeId] = Tuple.Create(bondMktData.PriceQuoteType.ToPriceQuoteType(), bondMktData.Quote);
                    }
                    var tfBondId = TradeInfo.TradeId + "_" + deliverableBondInfo.TradeId;
                    BondMktData tfMktData = null;
                    if (prebuiltMarket.BondPrices.ContainsKey(tfBondId))
                    {
                        tfMktData = prebuiltMarket.BondPrices[tfBondId];
                    }
                    if (tfMktData != null)
                    {
                        mktQuote[tfBondId] = Tuple.Create(tfMktData.PriceQuoteType.ToPriceQuoteType(), tfMktData.Quote);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new PricingBaseException(string.Format("Market price is missing, detailed message: {0}", ex.Message));
            }

            var fixingCurveName = valuationParameter.FixingCurveName;
            if (string.IsNullOrEmpty(fixingCurveName))
            {
                fixingCurveName = valuationParameter.DiscountCurveName;
            }

            return new MarketCondition(
                x => x.ValuationDate.Value = prebuiltMarket.ReferenceDate,
                x => x.DiscountCurve.Value = prebuiltMarket.YieldCurves[valuationParameter.DiscountCurveName],
                x => x.FixingCurve.Value = prebuiltMarket.YieldCurves[fixingCurveName],
                x => x.RiskfreeCurve.Value = prebuiltMarket.YieldCurves[valuationParameter.RiskfreeCurveName],
                x => x.MktQuote.Value = mktQuote,
                x => x.HistoricalIndexRates.Value = prebuiltMarket.HistoricalIndexRates
                );
        }
	}
}
