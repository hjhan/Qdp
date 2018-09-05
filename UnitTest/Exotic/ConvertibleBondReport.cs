using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using UnitTest.Utilities;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Ecosystem.Utilities;
using Qdp.Pricing.Base.Enums;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Utilities.Coupons;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Exotic;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using System.Linq;

namespace UnitTest.Exotic
{
    [TestClass]
    public class ConvertibleBondReport
    {
        [TestMethod]
        public void TestConvertibleBondReport()
        {
            //run in batch mode,  one cb ticker-> one market + one analytics row
            var market = setupMarket("2017-11-01");
            var r = calculatOneRow(market, "110030.SH");

            //TODO: feed data to a formatted xls template
            string fileName = @"D:\ConvertibleBondReport.csv";

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileName, append: false, encoding: System.Text.Encoding.UTF8))
            {
                file.WriteLine("转债代码,转债名,现价,昨日涨幅,正股名,正股价,正股涨幅,转股价,转股价值,转股溢价,套利空间,转债类型,债券评级,剩余期限,到期收益率,纯债价值,平价底价溢价率,期权价值,隐含波动率,回售价值,回售触发价,强赎触发价,修正触发价,到期日,回售日,可赎回日,修正条款");
                var firstPart = $"{r.cBCode},{r.cBName},{r.cBspot},{r.cBMove}, {r.stockName}, {r.stockPrice}, {r.stockMove}, {r.convertPrice}, {r.conversionValue}, {r.premiumToShareInPct}, {r.arbitragePL},";
                var secondPart = $"{r.cbStatus}, {r.bondRating}, {r.timeToMaturity},{r.yieldToMaturity},{r.bondFloor}, {r.parityFloorPremiumInPct},{r.optionValue},{r.impliedVol},";
                var thirdPart = $"{r.returnOnPut}, {r.putTrigger}, {r.callTrigger}, {r.resetTrigger}, {r.maturityDay}, {r.puttableDay}, {r.callableDay}, {r.resetFeature}";
                file.WriteLine(firstPart + secondPart + thirdPart);
            }

            Console.WriteLine("Simple example 转债代码{0}， 转债名{1}, 股票价格{2}", r.cBCode, r.cBName, r.stockPrice);
        }

        static private string compoundConvention = "Annual";
        static private string curveTrait = "SpotCurve";
        static private string rateTrait = "Spot";

        //TODO: download real data from WIND
        static private InstrumentCurveDefinition getRiskFreeCurve(CurveConvention curveConvention, String rfCurveName) {
            double flatRiskFreeRate = 0.05;
            var rates = new[]
            {
                new RateMktData("1D", flatRiskFreeRate, rateTrait, "None", rfCurveName),
                new RateMktData("15Y", flatRiskFreeRate, rateTrait, "None", rfCurveName),
            };
            return new InstrumentCurveDefinition(
                 rfCurveName,
                 curveConvention,
                 rates,
                 curveTrait);
        }

        //TODO: download real data from WIND
        static private InstrumentCurveDefinition getBondCreditCurve(CurveConvention curveConvention, String creditCurveName) {
            double flatCreditRate = 0.05;
            var rates2 = new[]
            {
                new RateMktData("1D", flatCreditRate, rateTrait, "None", creditCurveName),
                new RateMktData("15Y", flatCreditRate, rateTrait, "None", creditCurveName),
            };
            return new InstrumentCurveDefinition(
                 creditCurveName,
                 curveConvention,
                 rates2,
                 curveTrait);
        }

        //No need to change,  just zero dividend here
        static private InstrumentCurveDefinition getDividendCurve(CurveConvention curveConvention, String dCurveName) {
            var rates3 = new[]
            {
                new RateMktData("1D", 0.0, rateTrait, "None", dCurveName),
                new RateMktData("15Y", 0.0, rateTrait, "None", dCurveName),
            };
            return new InstrumentCurveDefinition(
                 dCurveName,
                 curveConvention,
                 rates3,
                 curveTrait);
        }

        //TODO: assuming flat 50% vol here
        static private IMarketCondition DailyReportMarket_generic(string referenceDate, double spotPrice, double cbDirtyPrice, String cbTicker)
        {
            var curveDefinitions = new List<InstrumentCurveDefinition>();

            var curveConvention = new CurveConvention("curveConvention","CNY","ModifiedFollowing","Chn_ib","Act365", compoundConvention,"Linear");

            var rfCurveName = "Fr007";
            var riskFreeCurve = getRiskFreeCurve(curveConvention, rfCurveName);
            curveDefinitions.Add(riskFreeCurve);

            var creditCurveName = "AAACreditCurve";
            var bondCreditCurve = getBondCreditCurve(curveConvention, creditCurveName);
            curveDefinitions.Add(bondCreditCurve);

            //assuming zero dividend,  this curve is almost useless for China
            var dCurveName = "dividendCurve";
            var dividendCurve = getDividendCurve(curveConvention, dCurveName);
            curveDefinitions.Add(dividendCurve);

            //TODO:  perhaps one vol for each bond? actually we plan to backout implied vol from price, on Jin Tao's list
            var volName = "VolSurf";
            var vol = 0.5;
            var volSurf = new[] { new VolSurfMktData(volName, vol), };

            var marketInfo = new MarketInfo("DailyCBReportMarket")
            {
                ReferenceDate = referenceDate,
                YieldCurveDefinitions = curveDefinitions.ToArray(),
                VolSurfMktDatas = volSurf,
                HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates
            };

            QdpMarket market;
            MarketFunctions.BuildMarket(marketInfo, out market);
            var volsurf = market.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate);

            return new MarketCondition(x => x.ValuationDate.Value = market.ReferenceDate,
                 x => x.DiscountCurve.Value = market.GetData<CurveData>(creditCurveName).YieldCurve,
                 x => x.FixingCurve.Value = market.GetData<CurveData>(creditCurveName).YieldCurve,
                 x => x.RiskfreeCurve.Value = market.GetData<CurveData>(rfCurveName).YieldCurve,
                 x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(dCurveName).YieldCurve } },
                 x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                 x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spotPrice } },
                 x => x.MktQuote.Value = new Dictionary<string, Tuple<PriceQuoteType, double>> { { cbTicker, Tuple.Create(PriceQuoteType.Dirty, cbDirtyPrice) } },
                 x => x.HistoricalIndexRates.Value = new Dictionary<IndexType, SortedDictionary<Date, double>>()
                 );
        }

        //TODO: get all latest curve + stock prices
        static private IMarketCondition setupMarket(String referenceDate) {
            var stockSpotPrice = 6.07;
            var convertiblePrice = 109.69;
            return DailyReportMarket_generic(referenceDate: referenceDate, spotPrice: stockSpotPrice, cbDirtyPrice: convertiblePrice, cbTicker: "110030.SH");
        }
        
        private static Date DateFromStr(String dateStr)
        {
            var dt = Convert.ToDateTime(dateStr);
            return new Date(dt.Year, dt.Month, dt.Day);
        }

        private static VanillaOption createOption(Double strike, Date expiry, Date firstConversionDate, Boolean American = false)
        {
            //var americanNoticeDates = CalendarImpl.Get("chn").BizDaysBetweenDates(new Date(2015, 06, 30), new Date(2019, 12, 25)).ToArray();
            //var exerciseStyle = American ? OptionExercise.American : OptionExercise.European;
            //var noticeDates = American ? americanNoticeDates : new Date[] { expiry };
            var exerciseStyle = OptionExercise.European;
            var noticeDates = new Date[] { expiry };
            return new VanillaOption(firstConversionDate,
                 expiry,
                 exerciseStyle,
                 OptionType.Call,
                 strike,
                 InstrumentType.Stock,
                 CalendarImpl.Get("chn"),
                 new Act365(),
                 CurrencyCode.CNY,
                 CurrencyCode.CNY,
                 noticeDates,
                 noticeDates
                 );
        }

        //TODO:  from ticker, load bond details from DB, set 
        private static ConvertibleBond getCB(String cbTicker)
        {
            var bond = GeliBondPart(cbTicker);
            var conversionPrice = 7.24;
            var bondMaturityDate = DateFromStr("2019-12-24");
            var conversionStartDate = DateFromStr("2015-06-30");
            var option = createOption(strike: conversionPrice, expiry: bondMaturityDate, firstConversionDate: conversionStartDate, American: false);
            return new ConvertibleBond(bond, option, null, null);
        }

        private static Bond GeliBondPart(String cbTicker)
        {
            var bond = new Bond(
                 cbTicker,
                 new Date(2014, 12, 25),
                 new Date(2019, 12, 25),
                 100,
                 CurrencyCode.CNY,
                 new StepWiseCoupon(new Dictionary<Date, double>
                {
                    {new Date(2014, 12, 25), 0.006},
                    {new Date(2015, 12, 25), 0.008},
                    {new Date(2016, 12, 25), 0.01},
                    {new Date(2017, 12, 25), 0.015},
                    {new Date(2018, 12, 25), 0.02}
                },
                      CalendarImpl.Get("chn_ib")
                      ),
                 CalendarImpl.Get("chn_ib"),
                 Frequency.Annual,
                 Stub.LongEnd,
                 new Act365NoLeap(),
                 new ModifiedAfb(),
                 BusinessDayConvention.None,
                 BusinessDayConvention.ModifiedFollowing,
                 null,
                 TradingMarket.ChinaExShg,
                 redemption: new Redemption(1.06, RedemptionType.SeparatePrincipalWithLastCoupon)
                 );

            return bond;
        }

        private enum ConvertibleBondStatus
        {
            StockLike = 0,
            Balance = 1,
            BondLike = 2
        }

        //TODO:  connect to bond db and WIND API, retrieve bond fundamental data + market data
        private static ReportRowData calculatOneRow(IMarketCondition market, String cbCode)
        {
            //TODO: given a ticker, retrieve from bond DB
            //Note: original "30/30,70%"  needs to convert to"30/30-70%" here
            var cbName = "格力转债";
            var cb = getCB(cbCode);
            var cbMove = 0.0;
            var stockName = "格力地产";
            var stockMove = 0.0;
            var bondRating = "AA";
            var convertPrice = cb.ConversionOption.Strike;
            var puttableDate = "2016-12-25"; // + "30/30-70%";
            var callableDate = "2015-06-30"; // + "15/30-130%";
            var resetFeature = "10/20/90%";
            var PutConditionPriceThreshold = 0.7;
            var CallConditionPriceThreshold = 1.3;
            var ConvertPriceChangeThreshold = 0.9;

            var putTrigger = PutConditionPriceThreshold * cb.ConversionOption.Strike;  // 0.7* convertPrice = 0.7* 7.24 = 5.068
            
            var callTrigger = CallConditionPriceThreshold * cb.ConversionOption.Strike; // 1.3* convertPrice = 1.3* 7.24 = 9.412
            
            var resetTrigger = ConvertPriceChangeThreshold * cb.ConversionOption.Strike;  // 0.9* convertPrice = 0.9 * 7.24 = 6.516

            //PutPrice
            var putRedemptionPrice = 103.0;

            //No need to change here
            var stockPrice = market.SpotPrices.Value.Values.First();
            var maturityDate = cb.UnderlyingMaturityDate.ToString();
            var cbSpot = market.MktQuote.Value[cb.Bond.Id].Item2;
            
            //analytics calc, no need to change
            var a = computeAnalytics(market, cb, putRedemptionPrice);

            //reporting data
            return new ReportRowData(
                cBCode: cbCode,
                cBName: cbName,
                cBspot: cbSpot,
                cBMove: cbMove,
                stockName: stockName,
                stockPrice: stockPrice,
                stockMove: stockMove,
                convertPrice: convertPrice,
                conversionRatio: cb.ConversionRatio,
                conversionValue: a.conversionValue,
                premiumToShareInPct: a.premiumToShareInPct,
                arbitragePL: a.arbitragePL,
                cbStatus: a.cbStatus,
                bondRating: bondRating,
                timeToMaturity: a.timeToMaturity,
                yieldToMaturity: a.yieldToMaturity,
                bondFloor: a.bondFloor,
                premiumToBondInPct: a.premiumToBondInPct,
                optionValue: a.optionValue,
                impliedVol: a.impliedVol,
                returnOnPut: a.returnOnPut,
                putTrigger: putTrigger,
                callTrigger: callTrigger,
                resetTrigger: resetTrigger,
                maturityDay: maturityDate,
                puttableDay: puttableDate,
                callableDay: callableDate,
                resetFeature: resetFeature,
                parityFloorPremiumInPct: a.parityFloorPremiumInPct);
        }

        private static ConvertibleBondAnalytics computeAnalytics(IMarketCondition market, ConvertibleBond cb, double putRedemptionPrice) {
            //analytics part
            var cbSpot = market.MktQuote.Value[cb.Bond.Id].Item2;
            var stockPrice = market.SpotPrices.Value.Values.First();
            var conversionValue = stockPrice * cb.ConversionRatio;
            var premiumToShareInPct = (cbSpot / conversionValue - 1) * 100; // in Pct
            var arbitragePL = conversionValue - cbSpot; //buy cb, sell stock
            var timeToMaturity = (cb.UnderlyingMaturityDate - market.ValuationDate) / 365.0;

            //bond analytics
            var bondengine = new BondEngine();
            var yieldToMaturity = bondengine.Calculate(cb.Bond, market, PricingRequest.Ytm).Ytm * 100.0;
            var bondFloor = bondengine.Calculate(cb.Bond, market, PricingRequest.Pv).Pv;
            double premiumToBondInPct = (cbSpot / bondFloor - 1) * 100;  //in Pct

            //parity vs bond floor premium
            var parityFloorPremiumInPct = (conversionValue / bondFloor - 1) * 100.0;


            //option analytics
            var impliedOptionValue = cbSpot - bondFloor;
            var impliedUnitOptionPremium = impliedOptionValue / cb.ConversionRatio;

            var option = createOption(strike: cb.ConversionOption.Strike, expiry: cb.ConversionOption.UnderlyingMaturityDate, firstConversionDate: cb.ConversionOption.StartDate, American: false);
            var engine = new AnalyticalVanillaEuropeanOptionEngine();
            var impliedVol = engine.ImpliedVol(option, market, impliedUnitOptionPremium);


            //Note: assume simple calc here:  putPrice 103, current price 109.69, then if we can put the bond now, returnOnPut = (103 - 109.69)/109.69
            var returnOnPut = (putRedemptionPrice/cbSpot - 1.0) *100;

            //cb style based on  parity/ bond floor relationship
            //if parity is worth 20% more than bond floor, then stock like
            //if parity is worth -20% less than bond floor, then bond like
            //otherwise,  cb is in balance mode
            var cbStatus = ConvertibleBondStatus.Balance;
            if (parityFloorPremiumInPct > 20)
                cbStatus = ConvertibleBondStatus.StockLike;
            else if(parityFloorPremiumInPct < -20)
                cbStatus = ConvertibleBondStatus.BondLike;

            return new ConvertibleBondAnalytics(
                conversionValue: conversionValue,
                premiumToShareInPct: premiumToShareInPct,
                arbitragePL: arbitragePL,
                timeToMaturity: timeToMaturity,
                yieldToMaturity: yieldToMaturity,
                bondFloor: bondFloor,
                premiumToBondInPct: premiumToBondInPct,
                parityFloorPremiumInPct : parityFloorPremiumInPct,
                optionValue: impliedOptionValue,
                impliedVol: impliedVol,
                returnOnPut: returnOnPut,
                cbStatus: cbStatus);
        }

        private class ConvertibleBondAnalytics {
            public double conversionValue { get; private set; }
            public double premiumToShareInPct { get; private set; }
            public double arbitragePL { get; private set; }

            public double timeToMaturity { get; private set; }
            public double yieldToMaturity { get; private set; }
            public double bondFloor { get; private set; }
            public double premiumToBondInPct { get; private set; }

            public double parityFloorPremiumInPct { get; private set; }

            public double optionValue { get; private set; }
            public double impliedVol { get; private set; }
            public double returnOnPut { get; private set; }
            public ConvertibleBondStatus cbStatus { get; private set; }

            public ConvertibleBondAnalytics(
                double conversionValue,
                double premiumToShareInPct,
                double arbitragePL,
                double timeToMaturity,
                double yieldToMaturity,
                double bondFloor,
                double premiumToBondInPct,
                double parityFloorPremiumInPct,
                double optionValue,
                double impliedVol,
                double returnOnPut,
                ConvertibleBondStatus cbStatus)
            {
                this.conversionValue = conversionValue;
                this.premiumToShareInPct = premiumToShareInPct;
                this.arbitragePL = arbitragePL;
                this.timeToMaturity = timeToMaturity;
                this.yieldToMaturity = yieldToMaturity;
                this.bondFloor = bondFloor;
                this.premiumToBondInPct = premiumToBondInPct;
                this.parityFloorPremiumInPct = parityFloorPremiumInPct;
                this.optionValue = optionValue;
                this.impliedVol = impliedVol;
                this.returnOnPut = returnOnPut;
                this.cbStatus = cbStatus;
            }
        }

        class ReportRowData {
            //代码， 转债名，现价，昨日涨幅，正股名，正股价，正股涨幅，转股价，转股价值，转股溢价，套利空间，
            //转债类型（偏股，偏债，平衡），债券评级，剩余期限，到期收益率，纯债价值, 平价底价溢价率
            //期权价值，回售收益
            //回售触发价，强赎触发价, 修正触发价
            //到期日，回售日，可赎回日，修正条款

            public string cBCode { get; private set; }
            public string cBName { get; private set; }
            public double cBspot { get; private set; }
            public double cBMove { get; private set; }
            public string stockName { get; private set; }
            public double stockPrice{ get; private set; }
            public double stockMove{ get; private set; }
            public double conversionRatio { get; private set; }
            public double convertPrice{ get; private set; }
            public double conversionValue{ get; private set; }
            public double premiumToShareInPct { get; private set; }
            public double arbitragePL{ get; private set; }

            public ConvertibleBondStatus cbStatus { get; private set; } //Share-Like, Balance, Bond-Like
            public string bondRating{ get; private set; }
            public double timeToMaturity{ get; private set; }
            public double yieldToMaturity{ get; private set; }
            public double bondFloor{ get; private set; }
            public double premiumToBondInPct { get; private set; }

            public double parityFloorPremiumInPct { get; private set; }

            public double optionValue{ get; private set; }
            public double impliedVol { get; private set; }
            public double returnOnPut { get; private set; }
            public double putTrigger{ get; private set; }
            public double callTrigger{ get; private set; }
            public double resetTrigger { get; private set; }

            public string maturityDay{ get; private set; }
            public string puttableDay{ get; private set; }
            public string callableDay{ get; private set; }

            public string resetFeature { get; private set; }

            public ReportRowData(string cBCode,
                    string cBName,
                    double cBspot,
                    double cBMove,
                    string stockName,
                    double stockPrice,
                    double stockMove,
                    double convertPrice,
                    double conversionRatio,
                    double conversionValue,
                    double premiumToShareInPct,
                    double arbitragePL,
                    ConvertibleBondStatus cbStatus,
                    string bondRating,
                    double timeToMaturity,
                    double yieldToMaturity,
                    double bondFloor,
                    double premiumToBondInPct,
                    double optionValue,
                    double impliedVol,
                    double returnOnPut,
                    double putTrigger,
                    double callTrigger,
                    double resetTrigger,
                    string maturityDay,
                    string puttableDay,
                    string callableDay,
                    string resetFeature,
                    double parityFloorPremiumInPct) {
                this.cBCode = cBCode;
                this.cBName = cBName;
                this.cBspot = cBspot;
                this.cBMove = cBMove;
                this.stockName = stockName;
                this.stockPrice = stockPrice;
                this.stockMove = stockMove;
                this.convertPrice = convertPrice;
                this.conversionRatio = conversionRatio;
                this.conversionValue = conversionValue;
                this.premiumToShareInPct = premiumToShareInPct;
                this.arbitragePL = arbitragePL;
                this.cbStatus = cbStatus;
                this.bondRating = bondRating;
                this.timeToMaturity = timeToMaturity;
                this.yieldToMaturity = yieldToMaturity;
                this.bondFloor = bondFloor;
                this.premiumToBondInPct = premiumToBondInPct;
                this.optionValue = optionValue;
                this.impliedVol = impliedVol;
                this.returnOnPut = returnOnPut;
                this.putTrigger = putTrigger;
                this.callTrigger = callTrigger;
                this.resetTrigger = resetTrigger;
                this.maturityDay = maturityDay;
                this.puttableDay = puttableDay;
                this.callableDay = callableDay;
                this.resetFeature = resetFeature;
                this.parityFloorPremiumInPct = parityFloorPremiumInPct;
            }
        }

        
    }
}
