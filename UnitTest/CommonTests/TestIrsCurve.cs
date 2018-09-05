using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Ecosystem.ExcelWrapper;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;
using Qdp.ComputeService.Data.CommonModels.TradeInfos;
using UnitTest.Utilities;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Base.Enums;
using Qdp.Foundation.Implementations;

namespace UnitTest.CommonTests
{
    [TestClass]
    public class TestIrsCurve
    {
        //TODO:  now we use prebuiltMarket, update this example to reflect the latest state
        [TestMethod]
        public void TestSwapCurveRepricing() {
            var ReferenceDate = "2017-11-15";
            const string t0MarketName = "Market2017-11-15";
            var curveName = "Fr007SwapCurve";

            //Calibrate curve
            var curveDefinitions = GetTestIrsCurveDefinition(curveName);
            var t0MarketInfo = new MarketInfo(t0MarketName)
            {
                ReferenceDate = ReferenceDate,
                YieldCurveDefinitions = curveDefinitions.ToArray(),
                HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates
            };
            XlManager.LoadMarket(t0MarketInfo);
            var market = XlManager.GetXlMarket(t0MarketName).QdpMarket;
            var tCurves = XlManager.GetXlMarket(t0MarketName).MarketInfo.YieldCurveDefinitions.Select(x => x.Name)
                    .Select(x => market.GetData<CurveData>(x).YieldCurve).ToDictionary(x => x.Name, x => x);
            var curve = tCurves.First().Value;
            var inputInstruments = curve.MarketInstruments;
            inputInstruments.Select(p => p.TargetValue);

            //Repricing curve building instruments
            //Repricing threshold 
            var threshold = 1e-7;

            var expected = inputInstruments.Select(p => p.TargetValue ).ToArray();
            var actual = new double[inputInstruments.Length];
            var marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>(curveName).YieldCurve,
                x => x.FixingCurve.Value = market.GetData<CurveData>(curveName).YieldCurve,
                x => x.RiskfreeCurve.Value = market.GetData<CurveData>(curveName).YieldCurve,
                x => x.HistoricalIndexRates.Value = new Dictionary<IndexType, SortedDictionary<Date, double>>()
                );
            for (int i = 0; i < inputInstruments.Length; i++) {
                actual[i] = inputInstruments[i].Instrument.ModelValue(marketCondition, MktInstrumentCalibMethod.Default);
            }
            var diffReport = expected.Zip(actual, (first, second) => Tuple.Create<double, double, double, bool>(first, second, first - second, Math.Abs(first - second)> threshold ));

            foreach (Tuple<double, double, double, bool> diffLine in diffReport) {
                Console.WriteLine($"Repricing diff : {diffLine}");
            }
            var breaches = diffReport.Where(x => x.Item4 == true).Count();
            Assert.AreEqual(true, breaches == 0);
        }

        [TestMethod]
        public void TestBondCurvePnLFromUnChangedMkt()
        {
            const string tradeId = "TEST_TRADE";
            const string t0MarketName = "Market2017-11-15";
            const string t1MarketName = "Market2017-11-16";

            var curveName = "BondCurve";
            var curveDefinitions = GetTestBondPnLCurveDefinition(curveName);

            //var t0MarketInfo = new MarketInfo(t0MarketName, 
            //    bondDataDefinitions: new[] { new BondMktData(tradeId, "Dirty", 97.0) })
            //{
            //    ReferenceDate = "2017-11-15",
            //    YieldCurveDefinitions = curveDefinitions.ToArray(),
            //    HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates
            //};

            //var t1MarketInfo = new MarketInfo(t1MarketName)
            //{
            //    ReferenceDate = "2017-11-16",
            //    YieldCurveDefinitions = curveDefinitions.ToArray(),
            //    HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates
            //};

            //XlManager.LoadMarket(t0MarketInfo);
            //XlManager.LoadMarket(t1MarketInfo);

            //XlManager.AddTrades(new TradeInfoBase[] { GetTestBondTrade(tradeId, curveName: curveName) });

            //var result = XlManager.xl_PnL(new string[] { tradeId }, t0MarketName, t1MarketName);
            //var curvePnL = result[tradeId].YieldCurvePnL[curveName].Sum(x=> x.Risk);
            //var curveRisk = result[tradeId].YieldCurvePnL[curveName + "KeyRateDv01"].Sum(x => x.Risk);

            //Assert.AreEqual(0.0, curvePnL, double.Epsilon);
            //Assert.AreEqual(-5.30227200201989, curveRisk, 1e-8);
            XlManager.AddHistoricalIndexRates(HistoricalDataLoadHelper.HistoricalIndexRatesMarket);
            XlManager.CreatePrebuiltMarket(t0MarketName, "2017-11-15");
            XlManager.AddBondPrice(tradeId, t0MarketName, "Dirty", 97.0);
            foreach (var curveDefinition in curveDefinitions)
            {
                XlManager.AddYieldCurve(curveDefinition.Name, t0MarketName, "2017-11-15", curveDefinition);
            }

            XlManager.CreatePrebuiltMarket(t1MarketName, "2017-11-16");
            foreach (var curveDefinition in curveDefinitions)
            {
                XlManager.AddYieldCurve(curveDefinition.Name, t1MarketName, "2017-11-16", curveDefinition);
            }

            XlManager.AddTrades(new TradeInfoBase[] { GetTestBondTrade(tradeId, curveName: curveName) });

            var result = XlManager.xl_PnLWithPreBuiltMarket(new string[] { tradeId }, t0MarketName, t1MarketName);
            var curvePnL = result[tradeId].YieldCurvePnL[curveName].Sum(x => x.Risk);
            var curveRisk = result[tradeId].YieldCurvePnL[curveName + "KeyRateDv01"].Sum(x => x.Risk);

            Assert.AreEqual(0.0, curvePnL, double.Epsilon);
            Assert.AreEqual(-5.30227200201989, curveRisk, 1e-8);
        }

        [TestMethod]
        public void TestIrsCurveZeroPnLFromUnChangedMkt()
        {
            const string tradeId = "TEST_TRADE";
            const string t0MarketName = "Market2017-11-15";
            const string t1MarketName = "Market2017-11-16";

            var curveName = "Fr007SwapCurve";
            var curveDefinitions = GetTestIrsCurveDefinition(curveName);

            //var t0MarketInfo = new MarketInfo(t0MarketName)
            //{
            //    ReferenceDate = "2017-11-15",                
            //    YieldCurveDefinitions = curveDefinitions.ToArray(),
            //    HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates
            //};

            //var t1MarketInfo = new MarketInfo(t1MarketName)
            //{
            //    ReferenceDate = "2017-11-16",
            //    YieldCurveDefinitions = curveDefinitions.ToArray(),
            //    HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates
            //};            

            //XlManager.LoadMarket(t0MarketInfo);
            //XlManager.LoadMarket(t1MarketInfo);
            //XlManager.AddTrades(new TradeInfoBase[] { GetTestIrsTrade(tradeId, curveName) });

            //var result = XlManager.xl_PnL(new string[] { tradeId }, t0MarketName, t1MarketName);
            //Assert.AreEqual(0.0, result[tradeId].YieldCurvePnL["Fr007SwapCurve"][2].Risk, double.Epsilon);

            XlManager.AddHistoricalIndexRates(HistoricalDataLoadHelper.HistoricalIndexRatesMarket);
            XlManager.CreatePrebuiltMarket(t0MarketName, "2017-11-15");
            foreach (var curveDefinition in curveDefinitions)
            {
                XlManager.AddYieldCurve(curveDefinition.Name, t0MarketName, "2017-11-15", curveDefinition);
            }

            XlManager.CreatePrebuiltMarket(t1MarketName, "2017-11-16");
            foreach (var curveDefinition in curveDefinitions)
            {
                XlManager.AddYieldCurve(curveDefinition.Name, t1MarketName, "2017-11-16", curveDefinition);
            }
            
            XlManager.AddTrades(new TradeInfoBase[] { GetTestIrsTrade(tradeId, curveName) });

            var result = XlManager.xl_PnLWithPreBuiltMarket(new string[] { tradeId }, t0MarketName, t1MarketName);
            Assert.AreEqual(0.0, result[tradeId].YieldCurvePnL["Fr007SwapCurve"][2].Risk, double.Epsilon);
        }

        private FixedRateBondInfo GetTestBondTrade(string tradeId, string curveName )
        {
            return new FixedRateBondInfo(tradeId) {
                StartDate = "2017-03-16",
                MaturityDate = "2024-03-16",
                Notional = 10000,
                Currency = "CNY",
                FixedCoupon = 0.032,
                Calendar = "chn_ib",
                PaymentFreq = "Annual",
                PaymentStub = "ShortStart",
                AccrualDC = "Act365NoLeap",
                DayCount = "ModifiedAfb",
                AccrualBD = "None",
                PaymentBD = "None",
                TradingMarket = "ChinaExShg",
                Settlement = "+0BD",
                ValuationParamters = new SimpleCfValuationParameters(curveName, null, curveName)
            };
        }

        private InterestRateSwapInfo GetTestIrsTrade(string tradeId, string curveName)
        {
            return new InterestRateSwapInfo(tradeId)
            {
                StartDate = "2017-11-15",
                MaturityDate = "2018-11-15",
                Tenor = "1Y",
                Notional = 1000000.0,
                Currency = "CNY",
                SwapDirection = "Payer",
                Calendar = "chn_ib",
                FixedLegDC = "Act365",
                FixedLegFreq = "Quarterly",
                FixedLegBD = "ModifiedFollowing",
                FixedLegStub = "ShortEnd",
                FixedLegCoupon = 0.03,
                FloatingLegDC = "Act365",
                FloatingLegFreq = "Quarterly",
                FloatingLegBD = "ModifiedFollowing",
                FloatingLegStub = "ShortEnd",
                Index = "Fr007",
                ResetTerm = "1W",
                ResetStub = "ShortEnd",
                ResetBD = "None",
                ResetToFixingGap = "-1BD",
                ResetCompound = "Compounded",
                ValuationParamters = new SimpleCfValuationParameters(curveName, curveName, curveName)
            };
        }

        private List<InstrumentCurveDefinition> GetTestBondPnLCurveDefinition(string curveName)
        {
            var curveConvention = new CurveConvention(Guid.NewGuid().ToString(),
                "CNY",
                "ModifiedFollowing",
                "Chn_ib",
                "Act365",
                "Annual",
                "Linear");
            var curveDefinitions = new List<InstrumentCurveDefinition>();

            var trait = "Spot";
            var type = "None";
            var rates = new[]
            {
                new RateMktData("1D", 0.0189, trait, type, curveName),
                new RateMktData("7D", 0.024, trait, type, curveName),
                new RateMktData("3M", 0.0239, trait, type, curveName),
                new RateMktData("6M", 0.02385, trait, type, curveName),
                new RateMktData("9M", 0.02385, trait, type, curveName),
                new RateMktData("1Y", 0.0239, trait, type, curveName),
                new RateMktData("2Y", 0.02405, trait, type, curveName),
                new RateMktData("3Y", 0.02495, trait, type, curveName),
                new RateMktData("4Y", 0.0259, trait, type, curveName),
                new RateMktData("5Y", 0.0267, trait, type, curveName),
                new RateMktData("7Y", 0.0283, trait, type, curveName),
                new RateMktData("10Y", 0.0297, trait, type, curveName),
                new RateMktData("15Y", 0.032, trait, type, curveName)
            };

            var curve = new InstrumentCurveDefinition(
                curveName,
                curveConvention,
                rates,
                "SpotCurve");

            curveDefinitions.Add(curve);

            return curveDefinitions;
        }

        private List<InstrumentCurveDefinition> GetTestIrsCurveDefinition( string curveName)
        {
            var curveConvention = new CurveConvention(Guid.NewGuid().ToString(),
                "CNY",
                "ModifiedFollowing",
                "Chn_ib",
                "Act365",
                "Continuous",
                "CubicHermiteMonotic");
            var curveDefinitions = new List<InstrumentCurveDefinition>();
            
            var rates = new[]
            {
                new RateMktData("1D", 0.0189, "Fr001", "Deposit", curveName),
                new RateMktData("7D", 0.024, "Fr001", "Deposit", curveName),
                new RateMktData("3M", 0.0239, "Fr007", "InterestRateSwap", curveName),
                new RateMktData("6M", 0.02385, "Fr007", "InterestRateSwap", curveName),
                new RateMktData("9M", 0.02385, "Fr007", "InterestRateSwap", curveName),
                new RateMktData("1Y", 0.0239, "Fr007", "InterestRateSwap", curveName),
                new RateMktData("2Y", 0.02405, "Fr007", "InterestRateSwap", curveName),
                new RateMktData("3Y", 0.02495, "Fr007", "InterestRateSwap", curveName),
                new RateMktData("4Y", 0.0259, "Fr007", "InterestRateSwap", curveName),
                new RateMktData("5Y", 0.0267, "Fr007", "InterestRateSwap", curveName),
                new RateMktData("7Y", 0.0283, "Fr007", "InterestRateSwap", curveName),
                new RateMktData("10Y", 0.0297, "Fr007", "InterestRateSwap", curveName),
                new RateMktData("15Y", 0.032, "Fr007", "InterestRateSwap", curveName)
            };

            var fr007Curve = new InstrumentCurveDefinition(
                curveName,
                curveConvention,
                rates,
                "SpotCurve");

            curveDefinitions.Add(fr007Curve);

            return curveDefinitions;
        }
    }
}
