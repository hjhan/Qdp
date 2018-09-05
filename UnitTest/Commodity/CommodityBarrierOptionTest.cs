using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using UnitTest.Utilities;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.Equity;
using Qdp.Pricing.Ecosystem.Trade.Equity;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Ecosystem.Utilities;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Common.Utilities;

namespace UnitTest.Commodity
{
	[TestClass]
	public class CommodityBarrierOptionTest
	{
        //TODO B: use MC pricer

        [TestMethod]
        public void CommodityBarrierOptionRebatePricingTest()
        {
            var valuationDate = "2017-10-31";
            var expiryDate = "2018-11-30";
            var discreteMonitored = false;
            var callVol = (1.84 + 2.34) / 2.0 / 100;
            //var putVol = (6.81 + 7.32) / 2 / 100;

            //UO call, strike < barrier
            var uoCall = BarrierOptionGreekCalc(vol: callVol, spot: 2800, strike: 2850, barrier: 2900, barrierStyle: "UpAndOut", isCall: true, 
                notional: defaultNotional, valuationDate: valuationDate, expiryStr: expiryDate, discreteMonitored: discreteMonitored);
            Assert.AreEqual(true, uoCall.Pv > 0);

            //UO call, strike > barrier, spot closer to barrier,  should be worth less
            var uoCall2 = BarrierOptionGreekCalc(vol: callVol, spot: 2800, strike: 2850, barrier: 2820, barrierStyle: "UpAndOut", isCall: true,
                notional: defaultNotional, valuationDate: valuationDate, expiryStr: expiryDate, discreteMonitored: discreteMonitored, rebate: 2.0);
            Assert.AreEqual(true, uoCall2.Pv < uoCall.Pv);

            //UO call, crossed = american barrier
            var uoCall3 = BarrierOptionGreekCalc(vol: callVol, spot: 3000, strike: 2950, barrier: 2900, barrierStyle: "UpAndOut", isCall: true,
                notional: defaultNotional, valuationDate: valuationDate, expiryStr: expiryDate, discreteMonitored: discreteMonitored, rebate: 2.0);
            Assert.AreEqual(true, uoCall3.Pv.Equals( 2.0* defaultNotional) );

            //DO call, strike > barrier
            var doCall = BarrierOptionGreekCalc(vol: callVol, spot: 3000, strike: 2950, barrier: 2900, barrierStyle: "DownAndOut", isCall: true,
                notional: defaultNotional, valuationDate: valuationDate, expiryStr: expiryDate, discreteMonitored: discreteMonitored, rebate: 2.0);
            Assert.AreEqual(true, doCall.Pv > 0);

            //DO call, strike < barrier,  spot closer to barrier, should be worth less
            var doCall2 = BarrierOptionGreekCalc(vol: callVol, spot: 3000, strike: 2950, barrier: 2980, barrierStyle: "DownAndOut", isCall: true,
                notional: defaultNotional, valuationDate: valuationDate, expiryStr: expiryDate, discreteMonitored: discreteMonitored, rebate: 2.0);
            Assert.AreEqual(true, doCall2.Pv < doCall.Pv);

            //DO call, crossed
            var doCall3 = BarrierOptionGreekCalc(vol: callVol, spot: 2900, strike: 2950, barrier: 2980, barrierStyle: "DownAndOut", isCall: true,
                notional: defaultNotional, valuationDate: valuationDate, expiryStr: expiryDate, discreteMonitored: discreteMonitored, rebate: 2.0);
            Assert.AreEqual(true, doCall3.Pv.Equals(2.0 * defaultNotional));


            //TODO: add put knock out tests
        }

        


        //TODO:  8 examples to weiming
        [TestMethod]
        public void CommodityBarrierOptionPricingTest()
        {
            //螺纹钢1801
            //maturity day:  2018-01-15, spot around 3628,    2017-10-31 close
            //OTC european,    
            //strike 3723
            //call  vol: 1.84% (bid)  2.34% (off)
            //put:  vol: 6.81% (bid) vs 7.32% (offer)

            var valuationDate = "2017-10-31";
            var strike = 3723;
            var spot = 3628;
            var expiryDate = "2018-11-30";

            var callVol = (1.84 + 2.34)/2.0 / 100;
            var putVol = (6.81 + 7.32) /2 / 100 ;
            var barrier = strike * 0.75;
            //defaultNotional = 1e6, 1 miollion

            //Vanilla call
            var vanillaCall = VaniilaOptionTest(expiryStr: expiryDate, strike: strike, spot: spot, vol: callVol, isCall: true, valuationDate: valuationDate, 
                expectedPv: 4344514.60983901, expectedDelta: 113122.273580357, expectedGamma: 2393.95059645176, expectedVega: 6600610.33549788, expectedTheta: -18257.1642252905);

            //DI call
            BarrierOptionCalcTest(vol: callVol, strike: strike, barrier: barrier, spot: spot, isCall: true, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "DownAndIn");

            //DO call
            BarrierOptionCalcTest(vol: callVol, strike: strike, barrier: barrier, spot: spot, isCall: true, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "DownAndOut",
                expectedPv: 4344514.60983899, expectedDelta: 113122.27356378, expectedGamma: 2393.95035430788, expectedVega: 6600610.33549789, expectedTheta: -18257.1642253035);

            //UI call
            BarrierOptionCalcTest(vol: callVol, strike: strike, barrier: barrier, spot: spot, isCall: true, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "UpAndIn",
                expectedPv: 4344514.60983901, expectedDelta: 113122.273580357, expectedGamma: 2393.95059645175, expectedVega: 6600610.33549788, expectedTheta: -18257.1642252905);

            //UO call
            BarrierOptionCalcTest(vol: callVol, strike: strike, barrier: barrier, spot: spot, isCall: true, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "UpAndOut");


            //Vanilla put
            var vanillaPut = VaniilaOptionTest(expiryStr: expiryDate, strike: strike, spot: spot, vol: callVol, isCall: false, valuationDate: valuationDate,
                expectedPv: 94340701.5912403, expectedDelta: -834206.0104385021, expectedGamma: 2393.94962787628, expectedVega: 6600610.3354983, expectedTheta: -5928.07499441504);

            //TODO:  catch 2 bug on 2018-07/09
            //DI put
            BarrierOptionCalcTest(vol: putVol, strike: strike, barrier: barrier, spot: spot, isCall: false, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "DownAndIn",
                expectedPv: 418571.605225466, expectedDelta: -5870.54765549256, expectedGamma: 78.4092780668288, expectedVega: 991740.686301433, expectedTheta: -6958.28104903741);

            //DO put
            BarrierOptionCalcTest(vol: putVol, strike: strike, barrier: barrier, spot: spot, isCall: false, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "DownAndOut",
                expectedPv: 152987243.000077, expectedDelta: -586110.26545465, expectedGamma: 1285.60423851013, expectedVega: 12761302.2910209, expectedTheta: -94863.4832484722);

            //UI put
            BarrierOptionCalcTest(vol: putVol, strike: strike, barrier: barrier, spot: spot, isCall: false, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "UpAndIn",
                expectedPv: 153311949.217949, expectedDelta: -590746.515575051, expectedGamma: 1348.7720489502, expectedVega: 13556971.5906605, expectedTheta: -100469.067977011);

            //UO put
            BarrierOptionCalcTest(vol: putVol, strike: strike, barrier: barrier, spot: spot, isCall: false, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "UpAndOut");

        }

        
        public Tuple<double, double, double, double, double> VaniilaOptionTest(String expiryStr, double strike, double vol,  double spot,
            double expectedPv = 0, double expectedDelta = 0, double expectedGamma = 0, double expectedVega = 0, double expectedTheta = 0,
            double notional = defaultNotional, bool isCall = false, 
            String firstExerciseDateStr = defaultFirstExerciseDay, String valuationDate = "2017-10-25")
        {
            var firstExerciseDay = DateFromStr(firstExerciseDateStr);
            var expiry = DateFromStr(expiryStr);

            var vanillaCall = createVanillaOption(firstExerciseDay, expiry, strike, notional, isCall: isCall);
            var marketCondition = createMarketCondition(valuationDate,vol: vol, spot: spot);
            var engine = new AnalyticalVanillaEuropeanOptionEngine();
            var res = engine.Calculate(vanillaCall, marketCondition, PricingRequest.All);
            var pv = res.Pv;
            var delta = res.Delta;
            var gamma = res.Gamma;
            var vega = res.Vega;
            var theta = res.Theta;

            Assert.AreEqual(expectedPv, pv, 1e-6);
            Assert.AreEqual(expectedDelta, delta, 1e-6);
            Assert.AreEqual(expectedGamma, gamma, 1e-6);
            Assert.AreEqual(expectedVega, vega, 1e-6);
            Assert.AreEqual(expectedTheta, theta, 1e-6);

            return  new Tuple<double, double, double, double, double>(res.Pv, res.Delta, res.Gamma, res.Vega, res.Theta);
        }

        public void BarrierOptionCalcTest(Double vol, Double spot, Double strike = 1.05, Double barrier = 0.75, string barrierStyle = "DownAndOut", 
            Boolean isCall = true, Double notional = defaultNotional,
            String valuationDate = "2017-10-25", String expiryStr = defaultExpiry, String firstExerciseDateStr = defaultFirstExerciseDay,
            double expectedPv = 0.0, double expectedDelta = 0.0, double expectedGamma = 0.0, double expectedVega = 0.0,  double expectedTheta = 0.0)
        {
            var res = BarrierOptionGreekCalc(vol, spot, strike, barrier, barrierStyle, isCall, notional,valuationDate, expiryStr);
            var pv = res.Pv;
            var delta = res.Delta;
            var gamma = res.Gamma;
            var vega = res.Vega;
            var theta = res.Theta;
            Assert.AreEqual(expectedPv, pv, 1e-6);
            Assert.AreEqual(expectedDelta, delta, 1e-6);
            Assert.AreEqual(expectedGamma, gamma, 1e-6);
            Assert.AreEqual(expectedVega, vega, 1e-6);
            Assert.AreEqual(expectedTheta, theta, 1e-6);
        }


        private IPricingResult BarrierOptionGreekCalc(Double vol, Double spot, Double strike = 1.05, Double barrier = 0.75, string barrierStyle = "DownAndOut", Boolean isCall = true, Double notional = defaultNotional,
            String valuationDate = "2017-10-25", String expiryStr = defaultExpiry, String firstExerciseDateStr = defaultFirstExerciseDay, bool testStoppingTime = false, bool discreteMonitored = true, double rebate = 0.0)
        {

            var firstExerciseDay = DateFromStr(firstExerciseDateStr);
            var expiry = DateFromStr(expiryStr);
            var market = TestMarket(referenceDate: valuationDate, vol: vol, spot: spot);
            var putCallType = isCall ? "Call" : "Put";

            var outBarrierInfo = createBarrierOption(firstExerciseDay.ToString(), expiry.ToString(), putCallType, strike: strike, barrier: barrier, 
                barrierType: barrierStyle, notional: notional, discreteMonitored: discreteMonitored, rebate: rebate);
            var vf = new BarrierOptionVf(outBarrierInfo);

            //var res = vf.ValueTrade(market, PricingRequest.All);
            var res = vf.ValueTrade(market, PricingRequest.All);
            var pv = res.Pv;
            var delta = res.Delta;
            var gamma = res.Gamma;
            var vega = res.Vega;
            var theta = res.Theta;

            //price vanilla call, for comparison during debug
            var vanillaCall = createVanillaOption(firstExerciseDay, expiry, strike, notional, isCall: isCall);
            var marketCondition = vf.GenerateMarketCondition(market);
            var engine = new AnalyticalVanillaEuropeanOptionEngine();
            var vanillaResult = engine.Calculate(vanillaCall, marketCondition, PricingRequest.All);
            var vanillaExpiry = vanillaCall.DayCount.CalcDayCountFraction(market.ReferenceDate, vanillaCall.UnderlyingMaturityDate);

            var inst = vf.GenerateInstrument();
            var timeToExpiry = inst.DayCount.CalcDayCountFraction(market.ReferenceDate, inst.ExerciseDates.Last());
            var diff = timeToExpiry - res.StoppingTime;

            if (testStoppingTime)
                Assert.AreEqual(true, diff >= 0);

            return res;
        }

        public void BarrierOptionCalcRequestTest(Double vol, Double spot, Double strike = 1.05, Double barrier = 0.75, string barrierStyle = "DownAndOut", Boolean isCall = true, Double notional = defaultNotional, 
            String valuationDate = "2017-10-25", String expiryStr = defaultExpiry, String firstExerciseDateStr = defaultFirstExerciseDay) {
            var res = BarrierOptionGreekCalc(vol, spot, strike, barrier, barrierStyle, isCall, notional, valuationDate, expiryStr, firstExerciseDateStr);
            Assert.AreEqual(true, res.Pv!= Double.NaN);
            Assert.AreEqual(true, res.Delta != Double.NaN);
            Assert.AreEqual(true, res.Gamma != Double.NaN);
            Assert.AreEqual(true, res.Vega != Double.NaN);
            Assert.AreEqual(true, res.Theta != Double.NaN);
        }

        [TestMethod]
        public void CommodityBarrierPricingGeneralTest() {
            //down and in  call
            BarrierOptionCalcRequestTest(vol: 0.2, strike: 1.05, barrier: 0.75, spot: 1.0, isCall: true, barrierStyle: "DownAndIn");

            //down and out call
            BarrierOptionCalcRequestTest(vol: 0.2, strike: 1.05, barrier: 0.75, spot: 1.0, isCall: true, barrierStyle: "DownAndOut");

            //up and in call
            BarrierOptionCalcRequestTest(vol: 0.2, strike: 1.05, barrier:  0.9, spot: 0.8, isCall: true, barrierStyle: "UpAndIn");

            //up and out call
            BarrierOptionCalcRequestTest(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.0, isCall: true, barrierStyle: "UpAndOut");

            //down and in put
            BarrierOptionCalcRequestTest(vol: 0.2, strike: 0.6, barrier: 0.75, spot: 1.0, isCall: false, barrierStyle: "DownAndIn");

            //down and out put
            BarrierOptionCalcRequestTest(vol: 0.2, strike: 0.6, barrier: 0.75, spot: 1.0, isCall: false, barrierStyle: "DownAndOut");

            //up and in put
            BarrierOptionCalcRequestTest(vol: 0.2, strike: 0.9, barrier: 1.2, spot: 1.0, isCall: false, barrierStyle: "UpAndIn");

            //up and out put
            BarrierOptionCalcRequestTest(vol: 0.2, strike: 0.9, barrier: 1.2, spot: 1.0, isCall: false, barrierStyle: "UpAndOut");
        }

        [TestMethod]
        public void CommodityEuropeanBarrierStoppingTimeTest() {
            var valuationDate = "2017-10-31";
            var strike = 3723;
            var spot = 3628;
            var expiryDate = "2018-11-30";

            var callVol = (1.84 + 2.34) / 2.0 / 100;
            var putVol = (6.81 + 7.32) / 2 / 100;
            var barrier = strike * 0.75;

            //Test to ensure, stopping Time <= expiry

            //degenerate to vanilla call,  stoppingTime = expiry
            BarrierOptionGreekCalc(vol: callVol, strike: strike, barrier: barrier, spot: barrier - 1, isCall: true, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "DownAndIn", testStoppingTime: true);

            //degenerate to vanilla put,  stoppingTime = expiry
            BarrierOptionGreekCalc(vol: putVol, strike: strike, barrier: barrier, spot: barrier - 1, isCall: false, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "DownAndIn", testStoppingTime: true);

            //DI call
            BarrierOptionGreekCalc(vol: callVol, strike: strike, barrier: barrier, spot: spot, isCall: true, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "DownAndIn", testStoppingTime: true);

            //DO call
            BarrierOptionGreekCalc(vol: callVol, strike: strike, barrier: barrier, spot: spot, isCall: true, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "DownAndOut", testStoppingTime: true);

            //UI call
            BarrierOptionGreekCalc(vol: callVol, strike: strike, barrier: barrier, spot: spot, isCall: true, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "UpAndIn", testStoppingTime: true);

            //UO call
            BarrierOptionGreekCalc(vol: callVol, strike: strike, barrier: barrier, spot: spot, isCall: true, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "UpAndOut", testStoppingTime: true);

            //DI put
            BarrierOptionGreekCalc(vol: putVol, strike: strike, barrier: barrier, spot: spot, isCall: false, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "DownAndIn", testStoppingTime: true);

            //DO put
            BarrierOptionGreekCalc(vol: putVol, strike: strike, barrier: barrier, spot: spot, isCall: false, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "DownAndOut", testStoppingTime: true);

            //UI put
            BarrierOptionGreekCalc(vol: putVol, strike: strike, barrier: barrier, spot: spot, isCall: false, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "UpAndIn", testStoppingTime: true);

            //UO put
            BarrierOptionGreekCalc(vol: putVol, strike: strike, barrier: barrier, spot: spot, isCall: false, expiryStr: expiryDate, valuationDate: valuationDate, barrierStyle: "UpAndOut", testStoppingTime: true);

            //TODO: test vanilla call, put
        }


        [TestMethod]
        public void CommodityEuropeanBarrierOptionPnLTest() {

            //UO call
            //pnl attribution is Quite off, high order doesnt seem to help
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 1.3, spot: 1.0, isCall: true, barrierStyle: "UpAndOut", mktMove: 10e-4, toleranceInPct: 1);
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 1.3, spot: 1.0, isCall: true, barrierStyle: "UpAndOut", mktMove: 100e-4, toleranceInPct: 1);
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 1.3, spot: 1.0, isCall: true, barrierStyle: "UpAndOut", mktMove: 300e-4, toleranceInPct: 1);
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 1.3, spot: 1.0, isCall: true, barrierStyle: "UpAndOut", mktMove: 300e-4, volMove: 0.3, toleranceInPct: 4.1);

            //DO-put Pass
            //TODO:  provide sensible explanation
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 0.9, spot: 1.0, isCall: false, barrierStyle: "DownAndOut", mktMove: 10e-4, toleranceInPct: 1);
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 0.9, spot: 1.0, isCall: false, barrierStyle: "DownAndOut", mktMove: 100e-4, toleranceInPct: 1);
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 0.9, spot: 1.0, isCall: false, barrierStyle: "DownAndOut", mktMove: 300e-4, toleranceInPct: 1);

            //DI put
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 0.9, spot: 1.0, isCall: false, barrierStyle: "DownAndIn", mktMove: 10e-4, toleranceInPct: 2);

            //Test theta Pnl over the weekend, reasonable
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 0.9, spot: 1.0, isCall: false, barrierStyle: "DownAndOut", mktMove: 100e-4, toleranceInPct: 2, t0: "2017-10-27", t1: "2017-10-30");
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 0.9, spot: 1.0, isCall: false, barrierStyle: "DownAndOut", mktMove: 300e-4, toleranceInPct: 2, t0: "2017-10-27", t1: "2017-10-30");

            //PASS
            //DO-Call Pass, nice work
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 0.9, spot: 1.0, isCall: true, barrierStyle: "DownAndOut", mktMove: 10e-4, toleranceInPct: 2);
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 0.9, spot: 1.0, isCall: true, barrierStyle: "DownAndOut", mktMove: 100e-4, toleranceInPct: 2);
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 0.9, spot: 1.0, isCall: true, barrierStyle: "DownAndOut", mktMove: 300e-4, toleranceInPct: 2);
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 0.9, spot: 1.0, isCall: true, barrierStyle: "DownAndOut", mktMove: 300e-4, volMove: 0.3, toleranceInPct: 2);  //need high order risk to explain pnl

            //UO put
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.0, isCall: false, barrierStyle: "UpAndOut", mktMove: 10e-4, toleranceInPct: 2);
            
            //DI call
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 0.9, spot: 1.0, isCall: true, barrierStyle: "DownAndIn", mktMove: 10e-4, toleranceInPct: 10);
            //UI call
            //EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 1.3, spot: 1.0, isCall: true, barrierStyle: "UpAndIn", mktMove: 10e-4, toleranceInPct: 10);
            //UI put
            EuropeanBarrierOptionGreekTest(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.0, isCall: false, barrierStyle: "UpAndIn", mktMove: 10e-4, toleranceInPct: 14);
        }

        //Test greek and pnl attribution
        private void EuropeanBarrierOptionGreekTest(double vol, double spot, double strike = 1.05, double barrier = 0.75, string barrierStyle = "DownAndIn", bool isCall = true, double notional = defaultNotional,
            string t0 = "2017-10-25", string t1 = "2017-10-26", double volMove = 0.10, string expiryStr = defaultExpiry, string firstExerciseDateStr = defaultFirstExerciseDay, double mktMove= 1e-4, double toleranceInPct = 2)
        {
            var T0 = DateFromStr(t0);
            var T1 = DateFromStr(t1);
            var firstExerciseDay = DateFromStr(firstExerciseDateStr);
            var expiry = DateFromStr(expiryStr);

            var marketT0 = TestMarket(referenceDate: t0, vol: vol, spot: spot);
            var putCallType = isCall ? "Call" : "Put";
            var outBarrierInfo = createBarrierOption(firstExerciseDay.ToString(), expiry.ToString(), putCallType, strike: strike, barrier: barrier, barrierType: barrierStyle, notional: notional);

            //binary valuation
            var vf = new BarrierOptionVf(outBarrierInfo);
            var tResult = vf.ValueTrade(marketT0, PricingRequest.All);

            //post market move, barrier valuation
            var marketT1 = TestMarket(referenceDate: t1, vol: vol + volMove, spot: spot+ mktMove);
            var t1Result = vf.ValueTrade(marketT1, PricingRequest.All);

            //base scenario, vanilla valuation
            var vanillaCall = createVanillaOption(firstExerciseDay, expiry, strike, notional, isCall: isCall);
            var marketCondition = vf.GenerateMarketCondition(marketT0);
            var engine = new AnalyticalVanillaEuropeanOptionEngine();
            var vanillaResult = engine.Calculate(vanillaCall, marketCondition, PricingRequest.All);
            
            //before after diff
            var deltaDiff = t1Result.Delta - tResult.Delta;
            var gammaDiff = t1Result.Gamma - tResult.Gamma;

            //simple numerical check,  barrier vs vanilla
            var vanillaDeltaDiff = vanillaResult.Delta - tResult.Delta;
            var vanillaGammaDiff = vanillaResult.Gamma - tResult.Gamma;
            var vanillaVegaDiff = vanillaResult.Vega - tResult.Vega;

            var actualPL = t1Result.Pv - tResult.Pv;
            var deltapl = tResult.Delta * mktMove;
            var gammapl = 0.5 * tResult.Gamma * Math.Pow(mktMove, 2);
            var thetapl = tResult.Theta * (T1 - T0);
            var vegapl = t1Result.Vega * volMove *100.0;

            //high order
            var dvegadvol = tResult.DVegaDvol * 0.5 * Math.Pow(volMove * 100, 2);
            var ddeltadvol = tResult.DDeltaDvol * mktMove * volMove * 100; //TODO: scaling problem here, to understand, even though its not used
            var dvegadt = tResult.DVegaDt * volMove;
            var ddeltadt = tResult.DDeltaDt * mktMove;
            var highOrder = dvegadvol + dvegadt + ddeltadt;

            var esimstatedPL = deltapl + gammapl + thetapl + vegapl + highOrder;


            //new framework

            var marketPI = TestMarket(referenceDate: t0, vol: vol, spot: spot + mktMove);
            var marketVI = TestMarket(referenceDate: t0, vol: vol+ volMove, spot: spot);
            var marketPVC = TestMarket(referenceDate: t0, vol: vol + volMove, spot: spot+ mktMove);

            //price Impact
            //PI = PV(t-1, priceNew) - Pv(t-1)
            var basePv = tResult.Pv;
            var PI = vf.ValueTrade(marketPI, PricingRequest.Pv).Pv - basePv;

            //vol impact
            //VI = PV(t-1. volNew) - Pv (t-1)
            var VI = vf.ValueTrade(marketVI, PricingRequest.Pv).Pv - basePv;

            //price vol cross impact
            //PVC = PV(t-1. volNew, PriceNew) - Pv (t-1) - (PI+VI)
            var PVC = vf.ValueTrade(marketPVC, PricingRequest.Pv).Pv - basePv - PI - VI;

            var newEstimate = PI + VI + PVC + thetapl;
            var newUnexplained = actualPL - newEstimate;

            //Time impact
            //TI = PV(t, all OldInfo) - Pv(t-1)

            //TODO:
            //Time/ price cross Impact
            //TPC = PV(t, priceNew) - pv(t-1) - (TI +PI)

            //Time/vol cross impact
            //TVC = PV(t, volNew) - pv(t-1) -(TI+VI)



            //TODO: 
            //in case of big move ( vol and spot), we need high order risk to explain pnl
            //var diff = actualPL - esimstatedPL;
            //Assert.AreEqual(true, Math.Abs(diff / actualPL) * 100.0 < toleranceInPct); //pnl well explained in not too extreme moves
            Assert.AreEqual(true, Math.Abs(newUnexplained / actualPL) * 100.0 < toleranceInPct); //pnl well explained in not too extreme moves
        }


        //TODO:  same test for put

        private const InstrumentType instrument = InstrumentType.CommodityFutures; //InstrumentType.CommoditySpot; barrier on both spot and futures are now arbitrage free
        private VanillaOption createVanillaOption(Date firstExerciseDay,Date expiry, Double strike,  Double notional, Boolean isCall = true ) {
            var optType = isCall ? OptionType.Call : OptionType.Put;
            return new VanillaOption(
                firstExerciseDay,
                expiry,
                OptionExercise.European,
                optType,
                strike,
                instrument,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { expiry },
                new[] { expiry },
                notional: notional
                );
        }

        private BarrierOptionInfo createBarrierOption(String firstExerciseDateStr, String expiryStr, String optionType, Double strike, Double barrier,
            String barrierType, Double notional, bool discreteMonitored = true, double rebate = 0.0, string barrierStatus="Monitoring") {
            return new BarrierOptionInfo(
                tradeId: "",
                strike: strike,
                underlyingTicker: "AU1712.SHF",   //沪金1712
                underlyingInstrumentType: instrument.ToString(),
                valuationParameter: new OptionValuationParameters("Fr007", "Dividend", "VolSurf", "000300.SH"),
                optionType:  optionType,
                notional: notional,
                startDate: firstExerciseDateStr,
                underlyingMaturityDate: "2017-12-15",
                exerciseDates: expiryStr,
                observationDates: "",
                calendar: "chn",
                dayCount: "Act365",
                exercise: "European",
                parallelDegree: 1)
            {
                IsDiscreteMonitored = discreteMonitored,
                BarrierType = barrierType,
                Barrier = barrier,
                ParticipationRate = 1.0,
                Rebate = rebate,
                Fixings = "2016-12-12,0.995;2016-12-13,1.0",  // mostly useless
                UseFourier = true,
                BarrierStatus = barrierStatus
            };
        }

        const String defaultExpiry = "2017-12-13";
        const String defaultFirstExerciseDay = "2017-11-27";

        [TestMethod]
		public void CommodityBarrierDIDO_vs_VanillaCallTest()
		{
            //down-in/down out vs vanilla call
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 0.75, spot: 1.0, isCall: true, isDownBarrier: true);  // down and in is worth money, PASS
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 0.75, spot: 1.03, isCall: true, isDownBarrier: true); // down and in is worth money, PASS
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 0.75, spot: 1.1, isCall: true, isDownBarrier: true);  // down and in becomes pure call, PASS
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 0.75, spot: 0.9, isCall: true, isDownBarrier: true);  // similar situation to 1.03, PASS
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 0.75, spot: 0.6, isCall: true, isDownBarrier: true);  // crossed
        }

        [TestMethod]
        public void CommodityBarrierDIDO_vs_VanillaPutTest()
        {
            //down-in/down out vs vanilla put
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 0.75, spot: 1.0, isCall: false, isDownBarrier: true); //potentially in the money
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 0.75, spot: 1.03, isCall: false, isDownBarrier: true);//potentially in the money
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 0.75, spot: 1.1, isCall: false, isDownBarrier: true); //potentially in the money
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 0.75, spot: 0.9, isCall: false, isDownBarrier: true);//potentially in the money
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 0.75, spot: 0.6, isCall: false, isDownBarrier: true);  //crossed
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 0.75, spot: 1.5, isCall: false, isDownBarrier: true); //potentially in the money
        }
        
        [TestMethod]
        public void CommodityBarrierUIUO_vs_VanillaCallTest()
        {
            //up-in/up-out vs vanilla call
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.3, isCall: true, isDownBarrier: false);   //crossed
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.0, isCall: true, isDownBarrier: false);  // down and in is worth money, PASS
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.03, isCall: true, isDownBarrier: false); // down and in is worth money, PASS
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.1, isCall: true, isDownBarrier: false);  // down and in becomes pure call, PASS
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 0.9, isCall: true, isDownBarrier: false);  // similar situation to 1.03, PASS
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 0.6, isCall: true, isDownBarrier: false); // down and out is worthless
        }

        [TestMethod]
        public void CommodityBarrierUIUO_vs_VanillaPutTest()
        {
            //up-in/up-out vs vanilla put
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.3, isCall: false, isDownBarrier: false);   //crossed
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.0, isCall: false, isDownBarrier: false);  // down and in is worth money, PASS
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.03, isCall: false, isDownBarrier: false); // down and in is worth money, PASS
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.1, isCall: false, isDownBarrier: false);  // down and in becomes pure call, PASS
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 0.9, isCall: false, isDownBarrier: false);  // similar situation to 1.03, PASS
            verifyBarrierVannilaParityRelationship(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 0.6, isCall: false, isDownBarrier: false); // down and out is worthless
        }

        [TestMethod]
        public void CommodityBarrierDiscreteContinuousCheckTest() {
            //1.test down and out call
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.3, barrierStyle: "DownAndOut", isCall: true);   // spot > barrier > strike, still live
            DiscreteContinuousCheck(vol: 0.2, strike: 1.25, barrier: 1.2, spot: 1.3, barrierStyle: "DownAndOut", isCall: true);   // spot> strike > barrier, still live
            DiscreteContinuousCheck(vol: 0.2, strike: 1.5, barrier: 1.2, spot: 1.3, barrierStyle: "DownAndOut", isCall: true);   // strike> spot>  barrier, still live
            DiscreteContinuousCheck(vol: 0.2, strike: 1.4, barrier: 1.2, spot: 1.0, barrierStyle: "DownAndOut", isCall: true);   // spot< barrier, out

            //2.test down and out put
            DiscreteContinuousCheck(vol: 0.2, strike: 1.25, barrier: 1.2, spot: 1.0, barrierStyle: "DownAndOut", isCall: false);  // spot < barrier < strike,  out 
            DiscreteContinuousCheck(vol: 0.2, strike: 1.25, barrier: 1.2, spot: 1.0, barrierStyle: "DownAndOut", isCall: false);  //  barrier < spot < strike,  live
            DiscreteContinuousCheck(vol: 0.2, strike: 1.25, barrier: 1.2, spot: 1.3, barrierStyle: "DownAndOut", isCall: false);  //  barrier < strike < spot,  live

            //3.test up and out call
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.0, barrierStyle: "upandout", isCall: true);     // spot < strike < barrier, live
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.1, barrierStyle: "upandout", isCall: true);     // strike < spot < barrier, live
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.3, barrierStyle: "upandout", isCall: true);     // strike < barrier < spot, out
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.0, barrierStyle: "upandout", isCall: true);     //  spot < barrier < strike , live, but tricky 

            //4.test up and out put
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.0, barrierStyle: "UpAndOut", isCall: false);   // spot < strike < barrier, live, 
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.1, barrierStyle: "UpAndOut", isCall: false);   // strike < spot <  barrier, live
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.3, barrierStyle: "UpAndOut", isCall: false);   // strike <  barrier < spot, out
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.0, barrierStyle: "UpAndOut", isCall: false);   // spot< barrier < strike, live

            //TODO:  to verify  in barrier impact more closely,  discrete vs  continous barrier
            //5.test up and in call
            //Explain: For UpAndIn Call, discrete barrier will be lower than continous barrier which will increase the option price.
            //The relationship of discrete<continous may no longer be stricly fulfilled.
            //The same for UpAndIn Put,DownAndIn Call and DownAndIn Put
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.05, spot: 1.0, barrierStyle: "UpAndIn", isCall: true);  // spot< strike < barrier, live
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.05, spot: 1.1, barrierStyle: "UpAndIn", isCall: true);  // strike< spot < barrier, live
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.6, barrierStyle: "UpAndIn", isCall: true);  // strike< barrier < spot , becomes vanilla, equal value
            DiscreteContinuousCheck(vol: 0.2, strike: 1.3, barrier: 1.2, spot: 1.4, barrierStyle: "UpAndIn", isCall: true);  //  barrier < strike < spot , becomes vanilla, equal value
            DiscreteContinuousCheck(vol: 0.2, strike: 1.3, barrier: 1.2, spot: 1.0, barrierStyle: "UpAndIn", isCall: true);  //  spot< barrier < strike, live
            DiscreteContinuousCheck(vol: 0.2, strike: 1.3, barrier: 1.2, spot: 1.25, barrierStyle: "UpAndIn", isCall: true);  //  barrier< spot < strike, becomes vanilla

            //6.test up and in put
            //Explain: For UpAndIn Put, discrete barrier will be lower than continous barrier which will increase the option price.
            //DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.0, barrierStyle: "UpAndIn", isCall: false); // spot< strike < barrier, live
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.05, spot: 1.1, barrierStyle: "UpAndIn", isCall: false); // strike < spot < barrier, live
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.3, barrierStyle: "UpAndIn", isCall: false); // strike < barrier < spot, becomes vanilla
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.3, barrierStyle: "UpAndIn", isCall: false); // spot < barrier < strike , live

            //7.test down and in call
            //Explain: For DownAndIn Call, discrete barrier will be higher than continous barrier which will increase the option price.
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.0, barrierStyle: "DownAndIn", isCall: true);  // strike < spot < barrier, become vanilla
            //DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.05, spot: 1.3, barrierStyle: "DownAndIn", isCall: true);  // strike <  barrier < spot, live
            //DiscreteContinuousCheck(vol: 0.2, strike: 1.3, barrier: 0.8, spot: 1.4, barrierStyle: "DownAndIn", isCall: true);  //  barrier < strike < spot, live
            DiscreteContinuousCheck(vol: 0.2, strike: 1.3, barrier: 1.2, spot: 1.0, barrierStyle: "DownAndIn", isCall: true);  //  spot< barrier < strike, live

            //8.test down and in put
            //Explain: For DownAndIn Put, discrete barrier will be higher than continous barrier which will increase the option price.
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.0, barrierStyle: "DownAndIn", isCall: false);   // spot< strike< barrier,  becomes vanilla
            DiscreteContinuousCheck(vol: 0.2, strike: 1.05, barrier: 1.2, spot: 1.3, barrierStyle: "DownAndIn", isCall: false);   // strike< barrier < spot, live
            DiscreteContinuousCheck(vol: 0.2, strike: 1.3, barrier: 1.3, spot: 1.4, barrierStyle: "DownAndIn", isCall: false);   // barrier < strike < spot, live
        }

        private const String defaultReferenceDate = "2017-10-25";
        //TODO:  support checking down/up and in   discrete vs continous checking
        private void DiscreteContinuousCheck(Double vol, Double spot, Double strike, Double barrier, string barrierStyle = "DownAndOut", double notional = defaultNotional, 
            String firstExerciseDay = defaultFirstExerciseDay, String expiryDay = defaultExpiry, bool isCall = true, String referenceDate = defaultReferenceDate ) {
            var market = TestMarket(referenceDate: referenceDate, vol: vol, spot: spot);
            var putCallType = isCall ? "Call" : "Put";
            var discrete = createBarrierOption(defaultFirstExerciseDay, defaultExpiry, putCallType, strike: strike, barrier: barrier, barrierType: barrierStyle, notional: notional, discreteMonitored : true);
            var continous = createBarrierOption(defaultFirstExerciseDay, defaultExpiry, putCallType, strike: strike, barrier: barrier, barrierType: barrierStyle, notional: notional, discreteMonitored: false);

            var discreteResult = new BarrierOptionVf(discrete).ValueTrade(market, PricingRequest.Pv);
            var discretePV = discreteResult.Pv;
            var continuousResult = new BarrierOptionVf(continous).ValueTrade(market, PricingRequest.Pv);
            var continuousPv = continuousResult.Pv;

            if (barrierStyle.Contains("In"))
            {
                Assert.AreEqual(true, discretePV - continuousPv <= 0);
            }
            else {
                Assert.AreEqual(true, discretePV - continuousPv >= 0);
            }
        }

        private Date DateFromStr(String dateStr) {
            var dt = Convert.ToDateTime(dateStr);
            return new Date(dt.Year, dt.Month, dt.Day);
        }

        const double precision = 1e-8;
        const double defaultNotional = 1E6;
        private void verifyBarrierVannilaParityRelationship(Double vol, Double spot, Double strike= 1.05, Double barrier= 0.75, bool isDownBarrier = true ,Boolean isCall = true, Double notional = defaultNotional, String referenceDate= "2017-10-25", String expiryStr = defaultExpiry, String firstExerciseDateStr= defaultFirstExerciseDay)
        {
            var firstExerciseDay = DateFromStr(firstExerciseDateStr); 
            var expiry = DateFromStr(expiryStr);

            var market = TestMarket(referenceDate: referenceDate, vol: vol, spot: spot);

            var putCallType = isCall ? "Call" : "Put";

            var vanillaCall = createVanillaOption(firstExerciseDay, expiry, strike, notional, isCall: isCall);

            //down and out call
            var outBarrierInfo = createBarrierOption(firstExerciseDay.ToString(), expiry.ToString(), putCallType, strike: strike, barrier: barrier, barrierType: isDownBarrier? "DownAndOut": "UpAndOut", notional: notional, discreteMonitored:false);

            //down and in call
            var inBarrierInfo = createBarrierOption(firstExerciseDay.ToString(), expiry.ToString(), putCallType, strike: strike, barrier: barrier, barrierType: isDownBarrier ? "DownAndIn" :"UpAndIn", notional: notional, discreteMonitored:false);

            //market
            var marketCondition = new BarrierOptionVf(outBarrierInfo).GenerateMarketCondition(market);

            //price vanilla call
            var engine = new AnalyticalVanillaEuropeanOptionEngine();
            var vanillaResult = engine.Calculate(vanillaCall, marketCondition, PricingRequest.All);
            var premiumVanilla = vanillaResult.Pv;
            Console.WriteLine("{0}", premiumVanilla);

            //price downAndOut call
            var outResult = new BarrierOptionVf(outBarrierInfo).ValueTrade(market, PricingRequest.All);
            var premiumOut = outResult.Pv;

            //price downAndIn call
            var inResult = new BarrierOptionVf(inBarrierInfo).ValueTrade(market, PricingRequest.All);
            var premiumIn = inResult.Pv;

            //Note:  using approximation pricing, we cannot achieve the following
            var pureCallWorthMore = approximateBigger(premiumVanilla, premiumOut, precision) && approximateBigger(premiumVanilla, premiumIn, precision);
            Assert.AreEqual(true, pureCallWorthMore);

            var parity = premiumIn + premiumOut;
            Assert.AreEqual(true, approximateEqual(premiumVanilla, parity, 1e-8));
        }



        [TestMethod]
        public void CommodityBarrierStatusPvPayoffCheckTest()
        {
            BarrierStatusCalcTest(strike: 100, spot: 105, barrier: 103, barrierStatus: "KnockedOut", barrierStyle:  "UpAndOut");
            BarrierStatusCalcTest(strike: 105, spot: 100, barrier: 103, barrierStatus: "KnockedOut", barrierStyle: "UpAndOut");
            BarrierStatusCalcTest(strike: 100, spot: 105, barrier: 103, barrierStatus: "KnockedOut", barrierStyle: "DownAndOut");
            BarrierStatusCalcTest(strike: 105, spot: 100, barrier: 103, barrierStatus: "KnockedOut", barrierStyle: "DownAndOut");
            BarrierStatusCalcTest(strike: 100, spot: 105, barrier: 103, barrierStatus: "KnockedIn", barrierStyle: "UpAndIn");
            BarrierStatusCalcTest(strike: 105, spot: 100, barrier: 103, barrierStatus: "KnockedIn", barrierStyle: "UpAndIn");
            BarrierStatusCalcTest(strike: 100, spot: 105, barrier: 103, barrierStatus: "KnockedIn", barrierStyle: "DownAndIn");
            BarrierStatusCalcTest(strike: 150, spot: 100, barrier: 103, barrierStatus: "KnockedIn", barrierStyle: "DownAndOut");
        }


        private void BarrierStatusCalcTest(Double strike, Double spot, Double barrier,string barrierStatus, Double vol = 0.3, string barrierStyle = "DownAndOut", Boolean isCall = true, Double notional = defaultNotional,
            String valuationDate = "2017-10-25", String expiryStr = defaultExpiry, String firstExerciseDateStr = defaultFirstExerciseDay, bool testStoppingTime = false, bool discreteMonitored = true, double rebate = 5.0)
        {           
            var firstExerciseDay = DateFromStr(firstExerciseDateStr);
            var expiry = DateFromStr(expiryStr);
            var market = TestMarket(referenceDate: valuationDate, vol: vol, spot: spot);
            var putCallType = isCall ? "Call" : "Put";
            var outBarrierInfo = createBarrierOption(firstExerciseDay.ToString(), expiry.ToString(), putCallType, strike: strike, barrier: barrier,
                barrierType: barrierStyle, notional: notional, discreteMonitored: discreteMonitored, rebate: rebate,barrierStatus:barrierStatus);
            var vf = new BarrierOptionVf(outBarrierInfo);

            var res = vf.ValueTrade(market, PricingRequest.All);
            var pv = res.Pv;
            var payoff = vf.GenerateInstrument().GetPayoff(new double[] { spot })[0].PaymentAmount;

            //price vanilla call
            var vanillaCall = createVanillaOption(firstExerciseDay, expiry, strike, notional, isCall: isCall);
            var marketCondition = vf.GenerateMarketCondition(market);
            var engine = new AnalyticalVanillaEuropeanOptionEngine();
            var vanillaResult = engine.Calculate(vanillaCall, marketCondition, PricingRequest.All);
            var vanillaPayoff = vanillaCall.GetPayoff(new double[] { spot })[0].PaymentAmount;

            var expectedPv = (barrierStatus == "KnockedIn") ? vanillaResult.Pv : rebate * notional;
            var expectedPayoff = (barrierStatus == "KnockedIn") ? vanillaPayoff : rebate * notional;

            Assert.AreEqual(pv, expectedPv, 1e-8);
            Assert.AreEqual(payoff, expectedPayoff, 1e-8);
        }


        private bool approximateBigger(double v1, double v2, double precision) => v1 + precision > v2;

        private bool approximateEqual(double v1, double v2, double precision) => Math.Abs(v1 - v2) <= precision;

        // above is testing parity relationship

        private IMarketCondition createMarketCondition(String valuationDateStr, Double vol = 0.2, Double spot = 1.0) {
            var market = TestMarket(valuationDateStr, vol, spot);
            var valuationDate = DateFromStr(valuationDateStr);
            var volsurf = market.GetData<VolSurfMktData>("VolSurf").ToImpliedVolSurface(valuationDate);
            return new MarketCondition(
                x => x.ValuationDate.Value = valuationDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>("Fr007").YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>("Dividend").YieldCurve } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { {"", spot } }
                );
        }

        private QdpMarket TestMarket(String referenceDate = "2017-10-25", Double vol = 0.2, Double spot = 1.0)
        {
            var historiclIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates;

            var curveConvention = new CurveConvention("fr007CurveConvention",
                    "CNY",
                    "ModifiedFollowing",
                    "Chn",
                    "Act365",
                    "Continuous",
                    "CubicHermiteMonotic");

            var fr007CurveName = "Fr007";
            var fr007RateDefinition = new[]
            {
                new RateMktData("1D", 0.05, "Spot", "None", fr007CurveName),
                new RateMktData("5Y", 0.05, "Spot", "None", fr007CurveName),
            };

            var dividendCurveName = "Dividend";
            var dividendRateDefinition = new[]
            {
                new RateMktData("1D", 0.0, "Spot", "None", dividendCurveName),
                new RateMktData("5Y", 0.0, "Spot", "None", dividendCurveName),
            };

            var curveDefinition = new[]
            {
                new InstrumentCurveDefinition(fr007CurveName, curveConvention, fr007RateDefinition, "SpotCurve"),
                new InstrumentCurveDefinition(dividendCurveName, curveConvention, dividendRateDefinition, "SpotCurve"),
            };

            var volSurf = new[] { new VolSurfMktData("VolSurf", vol), };
            var stockMktData = new[] { new StockMktData("000300.SH", spot), };

            var marketInfo=  new MarketInfo("tmpMarket", 
                referenceDate:referenceDate,
                yieldCurveDefinitions:curveDefinition,
                historicalIndexRates: historiclIndexRates,
                volSurfaceDefinitions: volSurf,
                stockDataDefinitions: stockMktData
                );
            QdpMarket market;
            MarketFunctions.BuildMarket(marketInfo, out market);
            return market;
        }
	}
}

