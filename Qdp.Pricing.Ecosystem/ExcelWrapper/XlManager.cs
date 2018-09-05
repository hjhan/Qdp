using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.TradeInfos;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.Equity;
using Qdp.Foundation.Serializer;
using Qdp.Foundation.Utilities;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Trade.FixedIncome;
using Qdp.Pricing.Ecosystem.Utilities;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Base.Enums;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Pricing.Library.Common.MathMethods.VolTermStructure;
using Qdp.Pricing.Ecosystem.Market.YieldCurveDependentObjects;
using Qdp.Pricing.Ecosystem.Trade;

namespace Qdp.Pricing.Ecosystem.ExcelWrapper
{
	public static partial class XlManager
	{
		private static Dictionary<string, XlMarket> _xlMarkets;
        private static Dictionary<string, PrebuiltQdpMarket> _prebuiltMarkets;
		private static XlTradeCache _xlTradeCache;
        private static object _prebuiltMarketLock = new object();
	    private static Dictionary<IndexType, SortedDictionary<Date, double>> _historicalIndexRates = new Dictionary<IndexType, SortedDictionary<Date, double>>();

	    public static Dictionary<IndexType, SortedDictionary<Date, double>> HistoricalIndexRates
	    {
	        get { return _historicalIndexRates; }
	    }

        /// <summary>
        /// All names of markets in memory.
        /// </summary>
        public static string[] AllMktNames
		{
			get { return _xlMarkets.Keys.ToArray(); }
		}

		/// <summary>
		/// All trade IDs in memory.
		/// </summary>
		public static string[] AllTradeIds
		{
			get
			{
				if (_xlTradeCache == null)
				{
					return new string[0];
				}
				return _xlTradeCache.AllTradeIds;
			}
		}

		static XlManager()
		{
			_xlMarkets = new Dictionary<string, XlMarket>();
            _prebuiltMarkets = new Dictionary<string, PrebuiltQdpMarket>();
			_xlTradeCache = new XlTradeCache(null);
		}

        #region DoPnLExplain
        public static Dictionary<string, PnLResultBase> DoPnLExplain(string[] tradeIds,
            PrebuiltQdpMarket t0Mkt,
            PrebuiltQdpMarket t1Mkt,
            PrebuiltQdpMarket t0MktDateRolledFwd,
            PrebuiltQdpMarket t0MktRolldownForBond,
            PrebuiltQdpMarket t0MktPriceNow = null,
            PrebuiltQdpMarket t0MktPriceVolNow = null,
            PrebuiltQdpMarket t0MktVolNow = null) {
            //TODO: add the following for more exotic product where rate/price interplay is more pronounced
            //resultRateNow,  resultPriceRateNow, resultVolRateNow, resultPriceVolRateNow

            var useRevalPnLFramework = (t0MktPriceNow != null && t0MktVolNow != null && t0MktPriceVolNow != null);
            //use reval for pnl attribution 
            //PnL = PriceImpact + VolImpact + PriceVolCrossImpact + Unexplained

            var ret = new Dictionary<string, PnLResultBase>();

            var tMarketName = t0Mkt.MarketName;
            var t1MarketName = t1Mkt.MarketName;

            var t0Vals = new Dictionary<string, IPricingResult>();
            var t0ValsRolledForward = new Dictionary<string, IPricingResult>();
            var t1Vals = new Dictionary<string, IPricingResult>();

            var t0ValsPriceNow = new Dictionary<string, IPricingResult>();
            var t0RolldownPriceForBond = new Dictionary<string, IPricingResult>();
            var t0ValsVolNow = new Dictionary<string, IPricingResult>();
            var t0ValsPriceVolNow = new Dictionary<string, IPricingResult>();

            //use minimium price request to make it faster
            #region requests
            var pricingRequest =
                PricingRequest.Pv |
                PricingRequest.Cashflow |
                PricingRequest.DirtyPrice |
                PricingRequest.KeyRateDv01 |
                PricingRequest.DollarDuration |
                PricingRequest.DollarConvexity |
                PricingRequest.Delta |
                PricingRequest.Gamma |
                PricingRequest.Vega |
                PricingRequest.Theta |
                PricingRequest.Rho |
                PricingRequest.DVegaDvol | //one high order risk                
                PricingRequest.ZeroSpread |
                PricingRequest.ZeroSpreadDelta |
                PricingRequest.AiEod |
                PricingRequest.Basis |
                PricingRequest.CheapestToDeliver |
                PricingRequest.Ytm | 
                PricingRequest.UnderlyingPv |
                PricingRequest.Carry;
            #endregion requests

            foreach (var tradeId in tradeIds)
            {
                t0Vals[tradeId] = xl_ValueTrade(tradeId, t0Mkt, pricingRequest);
                var t1Request = PricingRequest.Pv | PricingRequest.DirtyPrice | PricingRequest.ZeroSpread | PricingRequest.AiEod 
                    | PricingRequest.Ytm | PricingRequest.Basis | PricingRequest.Cashflow;
                t1Vals[tradeId] = xl_ValueTrade(tradeId, t1Mkt, t1Request);
                t0ValsRolledForward[tradeId] = xl_ValueTrade(tradeId, t0MktDateRolledFwd, PricingRequest.Pv | PricingRequest.DirtyPrice | PricingRequest.UnderlyingPv);

                //reval framework for better pnl attribution
                if (useRevalPnLFramework) {
                    t0ValsPriceNow[tradeId] = xl_ValueTrade(tradeId, t0MktPriceNow, PricingRequest.Pv);
                    t0ValsVolNow[tradeId] = xl_ValueTrade(tradeId, t0MktVolNow, PricingRequest.Pv);
                    t0ValsPriceVolNow[tradeId] = xl_ValueTrade(tradeId, t0MktPriceVolNow, PricingRequest.Pv);
                    t0RolldownPriceForBond[tradeId] = xl_ValueTrade(tradeId, t0MktRolldownForBond, PricingRequest.Pv| PricingRequest.ZeroSpread);
                }
                
            }

            //For old interface:
            //var tCurves = GetXlMarket(tMarketName).MarketInfo.YieldCurveDefinitions.Select(x => x.Name)
            //            .Select(x => t0Mkt.GetData<CurveData>(x).YieldCurve).ToDictionary(x => x.Name, x => x);
            //var t1Curves = GetXlMarket(t1MarketName).MarketInfo.YieldCurveDefinitions.Select(x => x.Name)
            //    .Select(x => t1Mkt.GetData<CurveData>(x).YieldCurve).ToDictionary(x => x.Name, x => x);
            var tCurves = t0Mkt.YieldCurves;
            var t1Curves = t1Mkt.YieldCurves;

            var curveMoveScaling = 1e4;

            foreach (var tradeId in tradeIds)
            {
                var t1cf = (t1Vals[tradeId].Cashflows != null) ?
                    t1Vals[tradeId].Cashflows.Where(x => x.PaymentDate <= t1Mkt.ReferenceDate && x.PaymentDate > t0Mkt.ReferenceDate).Sum(x => x.PaymentAmount) :
                    0.0;

                var tradeInfo = GetTrade(tradeId);
                if ( tradeInfo is InterestRateSwapInfo || tradeInfo is BondInfoBase || tradeInfo is BondFuturesInfo)
                {
                    //PnL using bond discounted cash flows
                    //curve risk is between T0_{prime} and T1
                    var _tPv = t0Vals[tradeId].Pv;
                    var _t1Pv = t1Vals[tradeId].Pv;
                    var _tPvRecalib = t0ValsRolledForward[tradeId].Pv;
                    var curvePnL = new Dictionary<string, CurveRisk[]>();

                    foreach (var curveRiskse in t0Vals[tradeId].KeyRateDv01)
                    {
                        var tCurve = tCurves[curveRiskse.Key];
                        var t1Curve = t1Curves[curveRiskse.Key];

                        curvePnL[curveRiskse.Key] =
                            curveRiskse.Value.Select(x =>
                                new CurveRisk(
                                    x.Tenor,
                                    x.Risk * (t1Curve[x.Tenor] - tCurve[x.Tenor]) * curveMoveScaling
                                )).ToArray();

                    }

                    //include raw t1 curve risks in result
                    foreach (var curveRisks in t0Vals[tradeId].KeyRateDv01)
                    {
                        curvePnL[curveRisks.Key + "KeyRateDv01"] = curveRisks.Value;
                    }

                    var pnLCurve = new CommonPnLResult(_tPv, _t1Pv, _tPvRecalib - _tPv, t1cf, curvePnL);
                    ret[tradeId] = pnLCurve;
                }

                if (tradeInfo is InterestRateSwapInfo)
                {
                    var swap = tradeInfo as InterestRateSwapInfo;
                    //carry & roll down
                    var rollDown = t0ValsRolledForward[tradeId].Pv - t0Vals[tradeId].Pv;
                    var carry = t0Vals[tradeId].Carry;
                    var pnlTime = rollDown + carry;

                    var commonPnl = ret[tradeId];
                    var pnlPv01 =
                        ret.ContainsKey(tradeId) && ret[tradeId].YieldCurvePnL.Count > 0
                        ? ret[tradeId].YieldCurvePnL.First().Value.Sum(x => x.Risk)
                        : 0.0;
                    var pnl = new SwapPnLResult(commonPnl.TPv, commonPnl.T1Pv, pnlTime: pnlTime, t1Cf: t1cf, pnlPv01: pnlPv01, 
                        pnlCarry: carry, pnlRolldown: rollDown);

                    ret[tradeId + "durationConvexity"] = pnl;
                }

                if (tradeInfo is BondInfoBase)
                {
                    var bond = tradeInfo as BondInfoBase;
                    var tPv = t0Vals[tradeId].DirtyPrice;
                    var t1Pv = t1Vals[tradeId].DirtyPrice;
                    var tPvRecalib = t0ValsRolledForward[tradeId].DirtyPrice;

                    //bond market PnL
                    var pnlPv01 =
                        ret.ContainsKey(tradeId) && ret[tradeId].YieldCurvePnL.Count > 0
                        ? ret[tradeId].YieldCurvePnL.First().Value.Sum(x => x.Risk)
                        : 0.0;

                    //bond specific pnl
                    var tZSpread = t0Vals[tradeId].ZeroSpread;
                    var t1ZSpread = t1Vals[tradeId].ZeroSpread;
                    var tZSpreadDelta = t0Vals[tradeId].ZeroSpreadDelta;
                    var zSpreadPnl = tZSpreadDelta * (t1ZSpread - tZSpread) * curveMoveScaling;

                    var pnlCarry = t1Vals[tradeId].Ai - t0Vals[tradeId].Ai;
                    //bond roll down effect:  cashflow less, but benefit from still curve,  note that zspread also changes due to rolling down the curve
                    var pnlRolldown = t0RolldownPriceForBond[tradeId].Pv - t0Vals[tradeId].Pv +
                        tZSpreadDelta * (t0RolldownPriceForBond[tradeId].ZeroSpread - tZSpread) * curveMoveScaling;
                    
                    var pnlTime = pnlCarry + pnlRolldown;

                    //duration pnl is  not used in book level pnl explain
                    var pnlDuration = t0Vals[tradeId].ModifiedDuration * (t1Vals[tradeId].Ytm - t0Vals[tradeId].Ytm) * bond.Notional;
                    var pnlConverixy = 0.5 * Math.Pow(t1Vals[tradeId].Ytm - t0Vals[tradeId].Ytm, 2.0) * t0Vals[tradeId].DollarConvexity /100.0;

                    var explainedPriceImpact = pnlPv01 + zSpreadPnl + pnlConverixy + pnlTime;

                    var pnl = new BondPnLResult(tPv, t1Pv, pnlTime: pnlTime, t1Cf: t1cf, pnlPv01: pnlPv01, pnlZspread: zSpreadPnl, pnlCarry: pnlCarry, pnlRolldown: pnlRolldown,
                        pnlDuration: pnlDuration, pnlConvexity: pnlConverixy);
                    ret[tradeId + "durationConvexity"] = pnl;
                }
                if (tradeInfo is BondFuturesInfo)
                {
                    var tPv = t0Vals[tradeId].DirtyPrice;
                    var t1Pv = t1Vals[tradeId].DirtyPrice;

                    //curve pnl
                    var pnlPv01 =
                        ret.ContainsKey(tradeId) && ret[tradeId].YieldCurvePnL.Count > 0
                        ? ret[tradeId].YieldCurvePnL.First().Value.Sum(x => x.Risk)
                        : 0.0;

                    //zspread pnl from CTD, converted to future equivalen t
                    var zspreadT0 = t0Vals[tradeId].ZeroSpread;
                    var zspreadT1 = t1Vals[tradeId].ZeroSpread;
                    var zspreadPnl = (zspreadT1 - zspreadT0) * curveMoveScaling * t0Vals[tradeId].ZeroSpreadDelta;

                    //basis pnl from  CTD/cf - Future
                    var basisT0 = t0Vals[tradeId].Basis;
                    var basisT1 = t1Vals[tradeId].Basis;
                    var bondFut = XlManager.GetTrade(tradeId) as BondFuturesInfo;
                    var futPosScaling = 1.0 / 100.0 * bondFut.Notional;
                    var basisPnL = (basisT1 - basisT0) * futPosScaling;

                    //convexity pnl from CTD
                    var ctdId = t0Vals[tradeId].CheapestToDeliver;
                    var pnlConvexity = 0.0;
                    var bondMktData = t0Mkt.BondPrices[ctdId];
                    if (bondMktData != null)
                    {
                        var ctdCleanPriceT0 = bondMktData.CleanPrice;
                        var ctdInfo = bondFut.DeliverableBondInfos.Where(x => x.BondId == ctdId).First();
                        var ctdResultT0 = XlUdf.BondEngineCalc(ctdId, t0Mkt.ReferenceDate.ToString(),
                            PriceQuoteType.Clean, ctdCleanPriceT0, PricingRequest.DirtyPrice,
                            fixedBond: ctdInfo) as PricingResult;
                        pnlConvexity = 0.5 * Math.Pow(t1Vals[tradeId].Ytm - t0Vals[tradeId].Ytm, 2.0) * t0Vals[tradeId].DollarConvexity /100.0 ;
                    }

                    //time pnl from CTD
                    var timePnL = t0ValsRolledForward[tradeId].UnderlyingPv - t0Vals[tradeId].UnderlyingPv;

                    var pnl = new BondFuturesPnLResult(tPv, t1Pv, pnlPv01: pnlPv01, pnlZspread: zspreadPnl, pnlBasis: basisPnL,
                        pnlTime: timePnL, pnlConvexity: pnlConvexity, curveRisks: null);

                    ret[tradeId] = pnl;
                }
                if (tradeInfo is VanillaOptionInfo || tradeInfo is BinaryOptionInfo || GetTrade(tradeId) is BarrierOptionInfo || tradeInfo is AsianOptionInfo)
                {
                    OptionValuationParameters valuationParameters = null;
                    Date exerciseDate = null;
                    double strike = 0.0;

                    var trade = tradeInfo as OptionInfoBase;
                    valuationParameters = trade.ValuationParamter;
                    TradeUtil.GenerateOptionDates(trade, out Date[] exerciseDates, out Date[] obsDates, out DayGap settlementGap);
                    exerciseDate = exerciseDates[0];
                    strike = trade.Strike;

                    //if (tradeInfo is VanillaOptionInfo)
                    //{
                    //    var trade = tradeInfo as VanillaOptionInfo;
                    //    valuationParameters = trade.ValuationParamter;
                    //    TradeUtil.GenerateOptionDates(trade, out Date[] exerciseDates, out Date[] obsDates, out DayGap settlementGap);
                    //    exerciseDate = exerciseDates[0];
                    //    strike = (tradeInfo as VanillaOptionInfo).Strike;
                    //}
                    //else if (tradeInfo is BinaryOptionInfo)
                    //{
                    //    var trade = tradeInfo as BinaryOptionInfo;
                    //    valuationParameters = trade.ValuationParamter;
                    //    TradeUtil.GenerateOptionDates(trade, out Date[] exerciseDates, out Date[] obsDates, out DayGap settlementGap);
                    //    exerciseDate = exerciseDates[0];
                    //    strike = trade.Strike;
                    //}
                    //else if (tradeInfo is BarrierOptionInfo)
                    //{
                    //    var trade = tradeInfo as BarrierOptionInfo;
                    //    valuationParameters = trade.ValuationParamter;
                    //    TradeUtil.GenerateOptionDates(trade, out Date[] exerciseDates, out Date[] obsDates, out DayGap settlementGap);
                    //    exerciseDate = exerciseDates[0];
                    //    strike = trade.Strike;
                    //}

                    //for old interface
                    //var mktMove = (t1Mkt.GetData<StockMktData>(valuationParameters.UnderlyingId).Price - t0Mkt.GetData<StockMktData>(valuationParameters.UnderlyingId).Price);
                    //var volMove = (t1Mkt.GetData<VolSurfMktData>(valuationParameters.VolSurfName).ToImpliedVolSurface(t1Mkt.ReferenceDate).GetValue(exerciseDate, strike) -
                    //    t0Mkt.GetData<VolSurfMktData>(valuationParameters.VolSurfName).ToImpliedVolSurface(t0Mkt.ReferenceDate).GetValue(exerciseDate, strike));
                    //var rateMove = t1Mkt.GetData<CurveData>(valuationParameters.DiscountCurveName).YieldCurve.GetSpotRate(exerciseDate) -
                    //    t0Mkt.GetData<CurveData>(valuationParameters.DiscountCurveName).YieldCurve.GetSpotRate(exerciseDate);

                    var mktMove = t1Mkt.StockPrices[valuationParameters.UnderlyingId] -t0Mkt.StockPrices[valuationParameters.UnderlyingId];
                    var volMove = t1Mkt.VolSurfaces[valuationParameters.VolSurfNames[0]].GetValue(exerciseDate, strike) -
                        t0Mkt.VolSurfaces[valuationParameters.VolSurfNames[0]].GetValue(exerciseDate, strike);

                    var rateMove = t1Mkt.YieldCurves[valuationParameters.DiscountCurveName].GetSpotRate(exerciseDate) -
                        t0Mkt.YieldCurves[valuationParameters.DiscountCurveName].GetSpotRate(exerciseDate);

                    var tPv = t0Vals[tradeId].Pv;
                    var t1Pv = t1Vals[tradeId].Pv;
                    var pnlDelta = t0Vals[tradeId].Delta * mktMove;
                    var pnlGamma = 0.5 * t0Vals[tradeId].Gamma * Math.Pow(mktMove, 2);
                    var pnlVega = t0Vals[tradeId].Vega * volMove * 100.0;
                    var pnlTheta = t0Vals[tradeId].Theta * (t1Mkt.ReferenceDate - t0Mkt.ReferenceDate);
                    var pnlRho = t0Vals[tradeId].Rho * rateMove;

                    double priceImpact, volImpact, priceVolCrossImpact;
                    var timeImpact = pnlTheta;
                    var rateImpact = pnlRho;
                    if (useRevalPnLFramework)
                    {
                        priceImpact = t0ValsPriceNow[tradeId].Pv - tPv;
                        volImpact = t0ValsVolNow[tradeId].Pv - tPv;
                        priceVolCrossImpact = t0ValsPriceVolNow[tradeId].Pv - tPv - priceImpact - volImpact;
                    }
                    else {
                        priceImpact = pnlDelta + pnlGamma;
                        volImpact = pnlVega;
                        priceVolCrossImpact = 0;
                    }

                    double pnlDvegaDvol, pnlDvegaDt, pnlDdeltaDt, pnlDdeltaDvol, pnlHighOrder;
                    if (tradeInfo is BarrierOptionInfo)
                    {
                        pnlDvegaDvol = t0Vals[tradeId].DVegaDvol * Math.Pow(volMove * 100.0, 2);
                        pnlDvegaDt = t0Vals[tradeId].DVegaDt * volMove * 100 * (t1Mkt.ReferenceDate - t0Mkt.ReferenceDate);
                        pnlDdeltaDt = t0Vals[tradeId].DDeltaDt * mktMove * (t1Mkt.ReferenceDate - t0Mkt.ReferenceDate);
                        pnlDdeltaDvol = t0Vals[tradeId].DDeltaDvol * mktMove * volMove * 100;
                        pnlHighOrder = pnlDvegaDvol + pnlDvegaDt + pnlDdeltaDt;
                    }
                    else
                    {
                        pnlDvegaDvol = 0;
                        pnlDvegaDt = 0;
                        pnlDdeltaDt = 0;
                        pnlDdeltaDvol = 0;
                        pnlHighOrder = 0;
                    }

                    var pnl = new OptionPnLResult(tPv, t1Pv, t1Cf: t1cf,
                        pnlTime: timeImpact, pnlPrice: priceImpact, pnlVol: volImpact, pnlPriceVolCross: priceVolCrossImpact,
                        pnlDelta: pnlDelta, pnlGamma: pnlGamma,  //to break down PriceImpact
                        pnlVega: pnlVega,   // to break down VolImpact
                        pnlTheta: pnlTheta,  //to break down TimeImpact
                        pnlRho: pnlRho,      //rateImpact, part of priceImpact
                        pnlHighOrder: 0, pnlDDeltaDt: 0, pnlDVegaDt: 0, pnlDVegaDvol: 0, pnlDDeltaDvol: 0);
                    ret[tradeId] = pnl;
                }
            }
            return ret;
        }
        #endregion

        #region PnL
        /// <summary>
        /// PnLExplainer
        /// </summary>
        /// <param name="tradeIds"></param>
        /// <param name="tMarketName"></param>
        /// <param name="t1MarketName"></param>
        /// <returns></returns>
     //   public static Dictionary<string, PnLResultBase> xl_PnL(string[] tradeIds, string tMarketName, string t1MarketName)
     //   {
     //       var t0Mkt = GetXlMarket(tMarketName).QdpMarket;
     //       var t1Mkt = GetXlMarket(t1MarketName).QdpMarket;
     //       var t0MarketDateRecalibrated = GetXlMarket(tMarketName).ReCalibrateToDate("tmpMarket", t1Mkt.ReferenceDate.ToString()).QdpMarket;

     //       var t0MktInfo = GetXlMarket(tMarketName).MarketInfo;
     //       var t1MktInfo = GetXlMarket(t1MarketName).MarketInfo;

     //       //PriceImpact: everything T1, except T0 vol, T0 curve and T0 valuation day
     //       var t0MktInfoPriceNow = t1MktInfo.Copy(marketName: t0MktInfo.MarketName + "PriceNow",
     //           referenceDateNew: t0MktInfo.ReferenceDate,
     //           volSurfaceDefinitionsNew: t0MktInfo.VolSurfMktDatas,
     //           yieldCurveDefinitionsNew: t0MktInfo.YieldCurveDefinitions,
     //           spcCurveDefinitionsNew: t0MktInfo.SpcCurveDefinitions);

     //       //VolImpact: everything T0, except T1 vol
     //       var t0MktInfoVolNow = t0MktInfo.Copy(marketName: t0MktInfo.MarketName + "VolNow",
     //           volSurfaceDefinitionsNew: t1MktInfo.VolSurfMktDatas);

     //       //PriceVolCrossImpact: everything T1, except T0 curves and T0 valuation day
     //       var t0MktInfoPriceVolNow = t1MktInfo.Copy(marketName: t0MktInfo.MarketName + "PriceVolNow",
     //           referenceDateNew: t0MktInfo.ReferenceDate,
     //           yieldCurveDefinitionsNew: t0MktInfo.YieldCurveDefinitions,
     //           spcCurveDefinitionsNew: t0MktInfo.SpcCurveDefinitions);

     //       var t0MktPriceNow = LoadAndReturnMarket(t0MktInfoPriceNow);
     //       var t0MktVolNow = LoadAndReturnMarket(t0MktInfoVolNow);
     //       var t0MktPriceVolNow = LoadAndReturnMarket(t0MktInfoPriceVolNow);

     //       return DoPnLExplain(tradeIds, t0Mkt, t1Mkt, t0MarketDateRecalibrated, t0MktPriceNow: t0MktPriceNow, t0MktPriceVolNow: t0MktPriceVolNow, t0MktVolNow: t0MktVolNow);
	    //}


        //TODO: do the same pnl change in here

        /// <summary>
        /// Calculate PnL with Prebuilt Market objects
        /// </summary>
        /// <param name="tradeIds"></param>
        /// <param name="tMarketName"></param>
        /// <param name="t1MarketName"></param>
        /// <returns></returns>
        public static Dictionary<string, PnLResultBase> xl_PnLWithPreBuiltMarket(string[] tradeIds, string tMarketName, string t1MarketName)
        {
            var t0Mkt = GetPrebuildQdpMarket(tMarketName);
            var t1Mkt = GetPrebuildQdpMarket(t1MarketName);
            var t0MktRolledForward = t0Mkt.ReCalibratePrebuiltMarketToDate("tmpMarket", t1Mkt.ReferenceDate.ToString());

            //PriceImpact: everything T1, except T0 vol, T0 curve and T0 valuation day
            var t0MktPriceNow = t0Mkt.Copy("t0MktPriceT1", t0Mkt.ReferenceDate,
                BondPricesNew: t1Mkt.BondPrices, CommodityPricesNew: t1Mkt.CommodityPrices, 
                StockPricesNew: t1Mkt.StockPrices, BondFuturePricesNew: t1Mkt.BondFuturePrices);

            //for bond rolldown
            var t0MktRolldownForBond = t0Mkt.Copy("t0MktPriceT1", t1Mkt.ReferenceDate);

            //VolImpact: everything T0, except T1 vol
            var t0MktVolNow = t0Mkt.Copy("t0MktPriceT1", t0Mkt.ReferenceDate,
                VolRawDataNew: t1Mkt.VolRawData, VolSurfacesNew: t1Mkt.VolSurfaces);

            //PriceVolCrossImpact: everything T1, except T0 curves and T0 valuation day
            var t0MktPriceVolNow = t0Mkt.Copy("t0MktPriceT1", t0Mkt.ReferenceDate,
                BondPricesNew: t1Mkt.BondPrices, CommodityPricesNew: t1Mkt.CommodityPrices,
                StockPricesNew: t1Mkt.StockPrices, BondFuturePricesNew: t1Mkt.BondFuturePrices,
                VolRawDataNew: t1Mkt.VolRawData, VolSurfacesNew: t1Mkt.VolSurfaces);

            return DoPnLExplain(tradeIds: tradeIds, t0Mkt: t0Mkt, t1Mkt: t1Mkt, 
                t0MktDateRolledFwd: t0MktRolledForward, t0MktRolldownForBond: t0MktRolldownForBond,
                t0MktPriceNow: t0MktPriceNow, t0MktVolNow: t0MktVolNow,  t0MktPriceVolNow: t0MktPriceVolNow);
        }
        #endregion


        #region get vol from volSurface
        public static double xl_volFromVolSurface(QdpMarket market, string volSurfaceName, double strike, Date expiryDate, double spot) {
            var prebuiltMarket = market as PrebuiltQdpMarket;
            ImpliedVolSurface impliedVol;
            if (prebuiltMarket != null)
            {
                impliedVol = prebuiltMarket.VolSurfaces[volSurfaceName];
            }
            else {
                impliedVol = market.GetData<VolSurfMktData>(volSurfaceName).ToImpliedVolSurface(market.ReferenceDate);
            }
            return impliedVol.GetValue(expiryDate, strike, spot);
        }

        #endregion 

        #region Value trades
        //Note:  not used by current xls interface.  xl_ValueTradeWithPrebuiltMarket is used instead.
        /// <summary>
        /// Aggregated pricing result of an array of trades.
        /// </summary>
        /// <param name="tradeIds">Trade IDs to be priced.</param>
        /// <param name="marketName">Name of market used to price trades.</param>
        /// <param name="pricingRequest">The pricing requests.</param>
        /// <returns>Return the aggregated pricing results.</returns>
        public static Dictionary<string, IPricingResult> xl_ValueTrades(string[] tradeIds, string marketName, string pricingRequest)
		{
			var market = GetXlMarket(marketName).QdpMarket;
			var requests = pricingRequest.Split(',').Select(s => s.ToPricingRequest()).ToArray().ToSinglePricingRequest();
			var results = new Dictionary<string, IPricingResult>();
			foreach (var tradeId in tradeIds)
			{
				results[tradeId] = xl_ValueTrade(tradeId, market, requests);
			}
			return results;
		}

        public static Dictionary<string, IPricingResult> xl_ValueTradesWithPrebuiltMarket(string[] tradeIds, PrebuiltQdpMarket market, string pricingRequest)
        {            
            var requests = pricingRequest.Split(',').Select(s => s.ToPricingRequest()).ToArray().ToSinglePricingRequest();
            var results = new Dictionary<string, IPricingResult>();
            foreach (var tradeId in tradeIds)
            {
                results[tradeId] = xl_ValueTradeWithPrebuiltMarket(tradeId, market, requests);
            }
            return results;
        }

        public static IPricingResult xl_ValueTradeWithPrebuiltMarket(string tradeId, PrebuiltQdpMarket market, PricingRequest pricingRequest)
        {
            var trade = _xlTradeCache.GetTradeInfo(tradeId);
            if (trade == null)
            {
                return null;
            }
            var vf = VfFactory.ToVf(trade);

            return vf.ValueTrade(market, pricingRequest);
        }


        private static IPricingResult xl_ValueTrade(string tradeId, QdpMarket market, PricingRequest pricingRequest)
		{
			var trade = _xlTradeCache.GetTradeInfo(tradeId);
			if (trade == null)
			{
				return null;
			}
			var vf = VfFactory.ToVf(trade);

			return vf.ValueTrade(market, pricingRequest);
		}

        public static Dictionary<string, IPricingResult> xl_valueIrsFixedLeg(string tradeId, QdpMarket market, PricingRequest pricingRequest)
	    {
	        var results = new Dictionary<string, IPricingResult>();
            var trade = _xlTradeCache.GetTradeInfo(tradeId);
	        if (trade == null)
	        {
	            return null;
	        }

	        var vf = VfFactory.ToVf(trade);

	        if (!(vf is InterestRateSwapVf))
	        {
                results[tradeId] = new PricingResult()
	            {
	                ErrorMessage = "xl_valueIrsFixedLeg is used to value fixed leg of an interest rate swap, but " +
	                               tradeId + " is not an IRS trade!"
	            };
	        }
	        var instrument =  (vf as InterestRateSwapVf).GenerateInstrument();
	        
	        var marketCondition = (vf as InterestRateSwapVf).GenerateMarketCondition(market);
	        var engine = new  CashflowProductEngine<SwapLeg>();
	        results[tradeId] = engine.Calculate(instrument.FixedLeg, marketCondition, pricingRequest);
            return results;
        }

	    public static Dictionary<string, IPricingResult> xl_valueIrsFloatingLeg(string tradeId, QdpMarket market, PricingRequest pricingRequest)
	    {
	        var results = new Dictionary<string, IPricingResult>();
            var trade = _xlTradeCache.GetTradeInfo(tradeId);
	        if (trade == null)
	        {
	            return null;
	        }

	        var vf = VfFactory.ToVf(trade);

	        if (!(vf is InterestRateSwapVf))
	        {
                results[tradeId] = new PricingResult()
	            {
	                ErrorMessage = "xl_valueIrsFloatingLeg is used to value floating leg of an interest rate swap, but " +
	                               tradeId + " is not an IRS trade!"
	            };
	        }
	        var instrument = (vf as InterestRateSwapVf).GenerateInstrument();

	        var marketCondition = (vf as InterestRateSwapVf).GenerateMarketCondition(market);
	        var engine = new CashflowProductEngine<SwapLeg>();
	        results[tradeId] = engine.Calculate(instrument.FloatingLeg, marketCondition, pricingRequest);
	        return results;
	    }

        #endregion

        #region Trade Cache operations
        /// <summary>
        /// Add trades into existing trade cache.
        /// </summary>
        /// <param name="tradeInfos"></param>
        /// <returns></returns>
        public static bool AddTrades(TradeInfoBase[] tradeInfos)
		{
			_xlTradeCache.AddTrades(tradeInfos);
			return true;
		}

		/// <summary>
		/// Remove trades from existing trade cache.
		/// </summary>
		/// <param name="tradeIds"></param>
		/// <returns></returns>
		public static bool RemoveTrades(string[] tradeIds)
		{
			_xlTradeCache.RemoveTrades(tradeIds);
			return true;
		}

		/// <summary>
		/// Get TradeInfoBase by tradeId from TradeCache
		/// </summary>
		/// <param name="tradeIDs"></param>
		/// <returns></returns>
		public static TradeInfoBase[] GetTrades(string[] tradeIDs)
		{
			return tradeIDs.Select(tradeId => _xlTradeCache.GetTradeInfo(tradeId)).ToArray();
		}

		/// <summary>
		/// Return trade info as an Qdp object.
		/// </summary>
		/// <param name="tradeId">Target trade ID.</param>
		/// <returns></returns>
		public static TradeInfoBase GetTrade(string tradeId)
		{
			return _xlTradeCache.GetTradeInfo(tradeId);
		}

		/// <summary>
		/// Return trade info in two column array {label, value} paris, where labels are given in "outputLabels" array.
		/// </summary>
		/// <param name="tradeId">Target trade ID.</param>
		/// <param name="outputLabels">Target output labels.</param>
		/// <returns></returns>
		public static object GetTradeInLabelValue(string tradeId, string[] outputLabels)
		{
			if (_xlTradeCache == null)
			{
				return "There are no trades in memory.";
			}
			
			return _xlTradeCache.GetTradeInfoInLabelData(tradeId, outputLabels);
		}
		/// <summary>
		/// Return all trade info of tradeCache in 2D array {{label}, {value}} paris.
		/// </summary>
		/// <returns></returns>
		public static object GetTradeCacheInLabelData()
		{
			if (_xlTradeCache == null)
			{
				return "There are no trades in memory.";
			}

			return _xlTradeCache.GetTradeCacheInLabelData();
		}
		#endregion


		#region Market operations
		//Note: not used by current sheets
        /// <summary>
		/// Return Xl market given by market name.
		/// </summary>
		/// <param name="marketName"></param>
		/// <returns>XlMarket with given name.</returns>
		public static XlMarket GetXlMarket(string marketName)
		{
			if (!HasMarketInMemory(marketName))
			{
				return null;
			}
			return _xlMarkets[marketName];
		}

		/// <summary>
		/// A boolean indicating whethere a market is in memory or not.
		/// </summary>
		/// <param name="marketName"></param>
		/// <returns></returns>
		public static bool HasMarketInMemory(string marketName)
		{
			return _xlMarkets.ContainsKey(marketName);
		}

		/// <summary>
		/// A boolean indicating whether loading market is succeed or not.
		/// </summary>
		/// <param name="marketInfo"></param>
		/// <returns></returns>
		public static bool LoadMarket(MarketInfo marketInfo)
		{
			var xlMarket = new XlMarket(marketInfo);
			_xlMarkets[xlMarket.MarketName] = xlMarket;
			return true;
		}

        //Note:  not used by current sheets
        /// <summary>
		/// Load market and return qdpMarket
		/// </summary>
		/// <param name="marketInfo"></param>
		/// <returns></returns>
		public static QdpMarket LoadAndReturnMarket(MarketInfo marketInfo)
        {
            LoadMarket(marketInfo);
            return GetXlMarket(marketInfo.MarketName).QdpMarket;
        }

        //Note:  not used by current sheets
        /// <summary>
        /// Return names of all market objects in a market.
        /// </summary>
        /// <param name="marketName">The name of market.</param>
        /// <returns>String array of names of all market objects in a market.</returns>
        public static string[] GetAllMarketObjNames(string marketName)
		{
			var xlMkt = GetXlMarket(marketName);
			if (xlMkt == null)
			{
				return new []{ string.Format("Market {0} is not available!", marketName)};
			}
			return xlMkt.GetMktObjNames();
		}

		/// <summary>
		/// Add market data definition to an existing market.
		/// </summary>
		/// <param name="marketName">The name of the existing market.</param>
		/// <param name="mktDataDef">The market data definition to be added.</param>
		/// <param name="isOverride">Whether override if a market data definition already exists.</param>
		/// <returns></returns>
		public static object MergeMarketInfo(string marketName, object mktDataDef, bool isOverride = true)
		{
			var xlMarket = GetXlMarket(marketName);
			if (xlMarket == null)
			{
				return string.Format("Market {0} is not available!", marketName);
			}
			xlMarket.MergeDefinitions(mktDataDef, isOverride);
			return true;
		}

        //Note: not used by current sheet
        /// <summary>
        /// Remove market data definition to an existing market.
        /// </summary>
        /// <param name="marketName">The name of the existing market.</param>
        /// <param name="mktDataDef">The name of get market data definition.</param>
        /// <returns></returns>
        public static object RemoveMarketInfo(string marketName, MarketDataDefinition mktDataDef = null)
		{
			var xlMarket = GetXlMarket(marketName);
			if (xlMarket == null)
			{
				return string.Format("Market {0} is not available!", marketName);
			}
			if (mktDataDef == null)
			{
				return _xlMarkets.Remove(marketName);
			}
			xlMarket.RemoveDefinitions(mktDataDef);
			return true;
		}

		/// <summary>
		/// Save a market into a file.
		/// </summary>
		/// <param name="marketName">The name of the market to be saved.</param>
		/// <param name="outputFileName">The output file name.</param>
		/// <returns></returns>
		public static object SaveMarket(string marketName, string outputFileName)
		{
			if (HasMarketInMemory(marketName))
			{
				var marketInfo = GetXlMarket(marketName);
				try
				{
					var ofile = new StreamWriter(File.Open(outputFileName, FileMode.Create));
					var str = DataContractJsonObjectSerializer.Serialize(marketInfo);
					ofile.Write(str);
					ofile.Close();
				}
				catch (Exception ex)
				{
					return ex.GetDetail();
				}
				return true;
			}
			return string.Format("There is no market names {0} in memory.", marketName);
		}
        #endregion

        #region PrebuiltMarket
        //TODO: move to a separate class
        public static void CreatePrebuiltMarket(string marketName, string referenceDate)
        {
            lock (_prebuiltMarketLock)
            {
                _prebuiltMarkets[marketName] = new PrebuiltQdpMarket(marketName, referenceDate.ToDate());
                _prebuiltMarkets[marketName].HistoricalIndexRates = HistoricalIndexRates;
            }
        }

        public static PrebuiltQdpMarket GetPrebuildQdpMarket(string marketName)
        {
            PrebuiltQdpMarket market = null;
            lock (_prebuiltMarketLock)
            {
                if (_prebuiltMarkets.ContainsKey(marketName))
                {
                    market = _prebuiltMarkets[marketName];
                }
            }
            return market;
        }

        public static void RemovePrebuiltMarket(string marketName)
        {
            lock (_prebuiltMarketLock)
            {
                if (_prebuiltMarkets.ContainsKey(marketName))
                {
                    _prebuiltMarkets.Remove(marketName);
                }
            }
        }

	    public static void RemoveAllPrebuiltMarket()
	    {
	        lock (_prebuiltMarketLock)
	        {
	            _prebuiltMarkets.Clear();
	        }
	    }

        public static void AddStockPrice(string ticker, string marketName, double price)
        {
            lock (_prebuiltMarketLock)
            {                
                if (_prebuiltMarkets.ContainsKey(marketName))
                {
                    _prebuiltMarkets[marketName].StockPrices[ticker] = price;
                }
            }
        }

        public static void AddBondPrice(string bondId, string marketName, string quoteType, double price)
        {
            lock (_prebuiltMarketLock)
            {
                if (_prebuiltMarkets.ContainsKey(marketName))
                {
                    _prebuiltMarkets[marketName].BondPrices[bondId] = new BondMktData(bondId, quoteType, price);
                }
            }
        }

        public static void AddBondFuturePrice(string futureId, string marketName, double price)
        {
            lock (_prebuiltMarketLock)
            {
                if (_prebuiltMarkets.ContainsKey(marketName))
                {
                    _prebuiltMarkets[marketName].BondFuturePrices[futureId] = price;
                }
            }
        }

        public static void AddCommodityPrice(string ticker, string marketName, double price)
        {
            lock (_prebuiltMarketLock)
            {
                if (_prebuiltMarkets.ContainsKey(marketName))
                {
                    _prebuiltMarkets[marketName].CommodityPrices[ticker] = price;
                }
            }
        }

        public static void AddYieldCurve(string curveName, string marketName, string valueDate, InstrumentCurveDefinition definition)
        {
            lock (_prebuiltMarketLock)
            {
                if (_prebuiltMarkets.ContainsKey(marketName))
                {
                    _prebuiltMarkets[marketName].YieldCurves[curveName] = CurveBuildHelper.BuildYieldCurve(_prebuiltMarkets[marketName], valueDate.ToDate(), definition);
                }
            }
        }

        public static double GetVol(string marketName, string underlyingTicker, double strike, string maturityDate)
        {
            lock (_prebuiltMarketLock)
            {
                var volSurfaceName = underlyingTicker + "_VolSurface";

                if (_prebuiltMarkets.ContainsKey(marketName) && _prebuiltMarkets[marketName].VolSurfaces.ContainsKey(volSurfaceName))
                {
                    return _prebuiltMarkets[marketName].VolSurfaces[volSurfaceName].GetValue(maturityDate.ToDate(), strike);
                }
                else
                {
                    return double.NaN;
                }
            }
        }

        public static void AddVolSurface(string marketName, string valueDate, string volSurfaceName, VolSurfMktData volSurface)
        {
            lock (_prebuiltMarketLock)
            {
                if (_prebuiltMarkets.ContainsKey(marketName))
                {
                    _prebuiltMarkets[marketName].VolRawData[volSurfaceName] = volSurface;
                    _prebuiltMarkets[marketName].VolSurfaces[volSurfaceName] = volSurface.ToImpliedVolSurface(valueDate.ToDate());
                }
            }
        }

        public static void AddCorrelationSurface(string marketName, string valueDate, string corrSurfaceName, CorrSurfMktData corrSurface)
        {
            lock (_prebuiltMarketLock)
            {
                if (_prebuiltMarkets.ContainsKey(marketName))
                {
                    _prebuiltMarkets[marketName].CorrRawData[corrSurfaceName] = corrSurface;
                    _prebuiltMarkets[marketName].CorrSurfaces[corrSurfaceName] = corrSurface.ToImpliedVolSurface(valueDate.ToDate());
                }
            }
        }

        public static void AddHistoricalIndexRates(Dictionary<IndexType, SortedDictionary<Date, double>> historicalIndexRates, bool isOverride = true)
	    {
	        foreach (var indexType in historicalIndexRates.Keys)
	        {
	            if (_historicalIndexRates.ContainsKey(indexType))
	            {
	                foreach (var indexDate in historicalIndexRates[indexType].Keys)
	                {
	                    if ((_historicalIndexRates[indexType].ContainsKey(indexDate) && isOverride) ||
	                        (!_historicalIndexRates[indexType].ContainsKey(indexDate)))
	                    {
	                        _historicalIndexRates[indexType][indexDate] = historicalIndexRates[indexType][indexDate];
	                    }
	                }
	            }
	            else if (!_historicalIndexRates.ContainsKey(indexType))
	            {
	                _historicalIndexRates[indexType] = historicalIndexRates[indexType];
	            }
	        }
	    }
        #endregion
    }
}
