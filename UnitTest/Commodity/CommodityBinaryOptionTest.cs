using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using Qdp.Pricing.Library.Equity.Engines.MonteCarlo;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Ecosystem.Utilities;
using UnitTest.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.Equity;
using Qdp.Pricing.Ecosystem.Trade.Equity;
using Qdp.Pricing.Library.Common.Utilities;

namespace UnitTest.Commodity
{
    [TestClass]
    public class CommodityBinaryOptionTest
    {
        //Takes too long time
        //[TestMethod]
        public void CommodityBinaryEuropeanOptionMcTest()
        {
            var goldOption = new BinaryOption(
                new Date(2015, 03, 20),
                new Date(2015, 06, 16),
                OptionExercise.European,
                OptionType.Put,
                231.733,
                InstrumentType.Futures,
                BinaryOptionPayoffType.CashOrNothing,
                10.0,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { new Date(2015, 06, 16) },
                new[] { new Date(2015, 06, 16) },
                1.0
                );

            var market = GetMarket();
            var volsurf = market.GetData<VolSurfMktData>("goldVolSurf").ToImpliedVolSurface(market.ReferenceDate);
            var yc = market.GetData<CurveData>("Fr007").YieldCurve;
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = yc,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", yc } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { {"", 240.6 } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }
                );

            // set first point on dividend curve to 0
            //MC pricing
            var dc = ZeroRateCurve(marketCondition.DividendCurves.Value.Values.First());
            marketCondition = marketCondition.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, IYieldCurve>>(x => x.DividendCurves, new Dictionary<string, IYieldCurve> { {"",dc } } ));
            var engine = new GenericMonteCarloEngine(2, 500000);
            var result = engine.Calculate(goldOption, marketCondition, PricingRequest.All);

            //analytical pricing
            var analyticalResult = new AnalyticalBinaryEuropeanOptionEngine().Calculate(goldOption, marketCondition, PricingRequest.All);
            Console.WriteLine("Pv:{0},{1}", analyticalResult.Pv, result.Pv);
            Console.WriteLine("Delta: {0},{1}", analyticalResult.Delta, result.Delta);
            Console.WriteLine("Gamma: {0},{1}", analyticalResult.Gamma, result.Gamma);
            Console.WriteLine("Vega: {0},{1}", analyticalResult.Vega, result.Vega);
            Console.WriteLine("Rho: {0},{1}", analyticalResult.Rho, result.Rho); //TODO binary option monte carlo rho
            Console.WriteLine("Theta: {0},{1}", analyticalResult.Theta, result.Theta);
            //Assert.AreEqual(analyticalResult.Pv, result.Pv, 0.02);
            //Assert.AreEqual(analyticalResult.Delta, result.Delta, 0.02);
            //Assert.AreEqual(analyticalResult.Gamma, result.Gamma, 0.05);
            //Assert.AreEqual(analyticalResult.Vega, result.Vega, 1e-2);
            //Assert.AreEqual(analyticalResult.Rho, result.Rho, 1e-6);
            //Assert.AreEqual(analyticalResult.Theta, result.Theta, 1e-5);
        }

        


        //TODO:  number diff to investigate and fix
        [TestMethod]
        public void CommodityJinTaoBinaryEuropeanOptionTest()
        {
            var strike = 3100.0;
            var spot = 2900;
            var expiry = new Date(2017, 12, 17);
            var valuationDay = "2017-12-01";
            var option = new BinaryOption(
                new Date(2017, 12, 1),
                expiry,
                OptionExercise.European,
                OptionType.Call,
                strike,
                InstrumentType.CommodityFutures,
                BinaryOptionPayoffType.CashOrNothing,
                2,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { expiry },
                new[] { expiry },
                notional: 1
                );

            var optionInfo = new BinaryOptionInfo(
                tradeId: "",
                valuationParameter: new OptionValuationParameters("Fr007", "diviCurve", "volSurf", "000300.SH"),
                underlyingTicker: "000300.SH",
                strike: strike,
                underlyingInstrumentType: "CommodityFutures",
                optionType: "Call",
                notional: 1,
                startDate: "2017-12-01",
                exerciseDates: "2017-12-17"
                )
            {
                CashOrNothingAmount = 2,
                BinaryOptionPayoffType = "CashOrNothing",
                BinaryOptionReplicationStrategy = "Down",
                ReplicationShiftSize = 2,
            };



            var curveName = "Fr007";
            var volName = "volSurf";
            var diviCurveName = "diviCurve";
            var market = GetMarketJinTao(valuationDay, rate: 0.04, vol: 0.4, dividendRate: 0, curveName: curveName, volName: volName, diviCurveName: diviCurveName, spot:spot);
            var volsurf = market.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate);
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>(curveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(diviCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { {"", spot } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }
                );

            var engine = new AnalyticalBinaryEuropeanOptionEngine();
            var result = engine.Calculate(option, marketCondition, PricingRequest.All);
            Assert.AreEqual(0.401208951727572, result.Pv, 1e-8);
            Assert.AreEqual(0.00230806033816866, result.Delta, 1e-8);
            Assert.AreEqual(7.16979364767667E-06, result.Gamma, 1e-8);
            Assert.AreEqual(0.0105751548032081, result.Vega, 1e-8);
            Assert.AreEqual(-0.0149236357909549, result.Theta, 1e-8);
            Assert.AreEqual(-1.75872031726865E-06, result.Rho, 1e-8);



            var engine2 = new AnalyticalBinaryEuropeanOptionReplicationEngine(2.0, BinaryOptionReplicationStrategy.Down);
            var result2 = engine2.Calculate(option, marketCondition, PricingRequest.All);
            Assert.AreEqual(0.403373182249897, result2.Pv, 1e-7);
            Assert.AreEqual(0.00231550673674974, result2.Delta, 1e-7);
            Assert.AreEqual(7.16340764483903E-06, result2.Gamma, 1e-7);
            Assert.AreEqual(0.0105550810113932, result2.Vega, 1e-7);
            Assert.AreEqual(-0.0149004654797067, result2.Theta, 1e-7);
            Assert.AreEqual(-1.74927811613657E-06, result2.Rho, 1e-7);

            var vfResult = new BinaryOptionVf(optionInfo).ValueTrade(market, PricingRequest.Pv);
            Assert.AreEqual(vfResult.Pv, result2.Pv, 1e-7);
            Assert.AreEqual(result2.Pv > result.Pv, true);

        }

        [TestMethod]
        public void CommodityBinaryAmericanOptionTest()
        {
            var strike = 3100.0;
            var spot = 2900;
            var expiry = new Date(2018, 04, 16);
            var valuationDay = "2018-03-28";
            var option = new BinaryOption(
                new Date(2018, 03, 28),
                expiry,
                OptionExercise.European,
                OptionType.Call,
                strike,
                InstrumentType.CommodityFutures,
                BinaryOptionPayoffType.CashOrNothing,
                2,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { expiry },
                new[] { expiry },
                notional: 1000
                );

            var option2 = new BinaryOption(
                new Date(2018, 03, 28),
                expiry,
                OptionExercise.European,
                OptionType.Call,
                strike,
                InstrumentType.CommodityFutures,
                BinaryOptionPayoffType.CashOrNothing,
                2,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { expiry },
                new[] { expiry },
                notional: 1000,
                binaryRebateType:BinaryRebateType.AtEnd
                );



            var curveName = "Fr007";
            var volName = "volSurf";
            var diviCurveName = "diviCurve";
            var market = GetMarketJinTao(valuationDay, rate: 0.05, vol: 0.3, dividendRate: 0, curveName: curveName, volName: volName, diviCurveName: diviCurveName);
            var volsurf = market.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate);
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>(curveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(diviCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } }
                );

            var engine = new AnalyticalBinaryAmericanOptionEngine();
            var result = engine.Calculate(option, marketCondition, PricingRequest.All);
            var result3= engine.Calculate(option2, marketCondition, PricingRequest.All);
            Assert.AreEqual(result.Pv!= result3.Pv, true);
            Assert.AreEqual(637.071254168821, result.Pv, 1e-8);
            Assert.AreEqual(4.94251151619665, result.Delta, 1e-8);
            Assert.AreEqual(0.0237525887314405, result.Gamma, 1e-8);
            Assert.AreEqual(31.1432441274033, result.Vega, 1e-8);
            Assert.AreEqual(-25.2234472794418, result.Theta, 1e-8);
            Assert.AreEqual(-0.00171280700558327, result.Rho, 1e-8);

            var engine2 = new AnalyticalBinaryEuropeanOptionEngine();
            var result2 = engine2.Calculate(option, marketCondition, PricingRequest.All);
            Assert.AreEqual(result.Pv >= result2.Pv, true);
            Assert.AreEqual(result.Delta >= result2.Delta, true);
            Assert.AreEqual(result.Gamma >= result2.Gamma, true);
            Assert.AreEqual(result.Vega >= result2.Vega, true);
            Assert.AreEqual(Math.Abs(result.Theta) >= Math.Abs(result2.Theta), true);
        }



        [TestMethod]
        public void CommodityEuropeanBinaryOptionPnLTest()
        {
            CommodityBinaryOptionGreekTest(vol: 0.4, strike: 3100, spot: 2900, isCall: true, isCashOrNothing: true, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 5);
            CommodityBinaryOptionGreekTest(vol: 0.4, strike: 3100, spot: 2900, isCall: true, isCashOrNothing: false, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 5);
            CommodityBinaryOptionGreekTest(vol: 0.4, strike: 3100, spot: 2900, isCall: false, isCashOrNothing: true, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 5);
            CommodityBinaryOptionGreekTest(vol: 0.4, strike: 3100, spot: 2900, isCall: false, isCashOrNothing: false, volMove: 0.10, mktMove: 10e-4, toleranceInPct: 5);
        }

        public void CommodityBinaryOptionGreekTest(double vol, double spot, double strike, Boolean isCall = true, Boolean isCashOrNothing = true,
            string t0 = "2017-12-01", string t1 = "2017-12-02", double volMove = 0.10, double mktMove = 1e-4, double toleranceInPct = 2)
        {
            var T0 = DateFromStr(t0);
            var T1 = DateFromStr(t1);
            var spotNew = spot + spot * mktMove;
            var volNew = vol + volMove;


            var expiry = new Date(2017, 12, 17);
            var valuationDay = t0;
            var valuationDayNew = t1;
            var option = new BinaryOption(
                T0,
                expiry,
                OptionExercise.European,
                isCall ? OptionType.Call : OptionType.Put,
                strike,
                InstrumentType.CommodityFutures,
                isCashOrNothing ? BinaryOptionPayoffType.CashOrNothing : BinaryOptionPayoffType.AssetOrNothing,
                2,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { expiry },
                new[] { expiry },
                notional: 1
                );


            var curveName = "Fr007";
            var volName = "volSurf";
            var diviCurveName = "diviCurve";
            var market = GetMarketJinTao(valuationDay, rate: 0.04, vol: vol, dividendRate: 0, curveName: curveName, volName: volName, diviCurveName: diviCurveName);
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>(curveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(diviCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot} },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", market.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate) } }
                );

            var marketNew = GetMarketJinTao(valuationDayNew, rate: 0.04, vol: volNew, dividendRate: 0, curveName: curveName, volName: volName, diviCurveName: diviCurveName);
            IMarketCondition marketConditionNew = new MarketCondition(
                x => x.ValuationDate.Value = marketNew.ReferenceDate,
                x => x.DiscountCurve.Value = marketNew.GetData<CurveData>(curveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "",marketNew.GetData<CurveData>(diviCurveName).YieldCurve }},
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spotNew } } ,
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", marketNew.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate) } }
                );

 
            IMarketCondition marketConditionPI = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>(curveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(diviCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spotNew } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", market.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate) } }
                );

            var marketVI = GetMarketJinTao(valuationDay, rate: 0.04, vol: volNew, dividendRate: 0, curveName: curveName, volName: volName, diviCurveName: diviCurveName);
            IMarketCondition marketConditionVI = new MarketCondition(
                x => x.ValuationDate.Value = marketVI.ReferenceDate,
                x => x.DiscountCurve.Value = marketVI.GetData<CurveData>(curveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", marketVI.GetData<CurveData>(diviCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spot } } ,
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", marketVI.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate) } }
                );

            var marketPVC = GetMarketJinTao(valuationDay, rate: 0.04, vol: volNew, dividendRate: 0, curveName: curveName, volName: volName, diviCurveName: diviCurveName);
            IMarketCondition marketConditionPVC = new MarketCondition(
                x => x.ValuationDate.Value = marketPVC.ReferenceDate,
                x => x.DiscountCurve.Value = marketPVC.GetData<CurveData>(curveName).YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", marketPVC.GetData<CurveData>(diviCurveName).YieldCurve } },
                x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spotNew } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", marketPVC.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate) } }
                );

            var engine = new AnalyticalBinaryEuropeanOptionEngine();
            var result = engine.Calculate(option, marketCondition, PricingRequest.All);
            var resultNew = engine.Calculate(option, marketConditionNew, PricingRequest.All);
            var resultPI = engine.Calculate(option, marketConditionPI, PricingRequest.All);
            var resultVI = engine.Calculate(option, marketConditionVI, PricingRequest.All);
            var resultPVC = engine.Calculate(option, marketConditionPVC, PricingRequest.All);

            var actualPL = resultNew.Pv - result.Pv;

            //price Impact
            //PI = PV(t-1, priceNew) - Pv(t-1)
            var basePv = result.Pv;
            var PI = resultPI.Pv - basePv;
            var thetapl = result.Theta * (T1 - T0);

            //vol impact
            //VI = PV(t-1. volNew) - Pv (t-1)
            var VI = resultVI.Pv - basePv;

            //price vol cross impact
            //PVC = PV(t-1. volNew, PriceNew) - Pv (t-1) - (PI+VI)
            var PVC = resultPVC.Pv - basePv - PI - VI;

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
            Assert.AreEqual(true, Math.Abs(newUnexplained / actualPL) * 100.0 < toleranceInPct);



            var engine2 = new AnalyticalBinaryEuropeanOptionReplicationEngine(2.0, BinaryOptionReplicationStrategy.Up);
            var result2 = engine2.Calculate(option, marketCondition, PricingRequest.All);
            var resultNew2 = engine2.Calculate(option, marketConditionNew, PricingRequest.All);
            var resultPI2 = engine2.Calculate(option, marketConditionPI, PricingRequest.All);
            var resultVI2 = engine2.Calculate(option, marketConditionVI, PricingRequest.All);
            var resultPVC2 = engine2.Calculate(option, marketConditionPVC, PricingRequest.All);

            var actualPL2 = resultNew2.Pv - result2.Pv;

            //price Impact
            //PI = PV(t-1, priceNew) - Pv(t-1)
            var basePv2 = result2.Pv;
            var PI2 = resultPI2.Pv - basePv2;
            var thetapl2 = result2.Theta * (T1 - T0);

            //vol impact
            //VI = PV(t-1. volNew) - Pv (t-1)
            var VI2 = resultVI2.Pv - basePv2;

            //price vol cross impact
            //PVC = PV(t-1. volNew, PriceNew) - Pv (t-1) - (PI+VI)
            var PVC2 = resultPVC2.Pv - basePv2 - PI2 - VI2;

            var newEstimate2 = PI2 + VI2 + PVC2 + thetapl2;
            var newUnexplained2 = actualPL2 - newEstimate2;



            Assert.AreEqual(true, Math.Abs(newUnexplained2 / actualPL2) * 100.0 < toleranceInPct);

        }

        [TestMethod]
        public void CommodityBinaryEuropeanOptionTest()
        {
            var goldOption = new BinaryOption(
                new Date(2015, 03, 20),
                new Date(2015, 06, 16),
                OptionExercise.European,
                OptionType.Put,
                231.733,
                InstrumentType.Futures,
                BinaryOptionPayoffType.CashOrNothing,
                10.0,
                CalendarImpl.Get("chn"),
                new Act365(),
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { new Date(2015, 06, 16) },
                new[] { new Date(2015, 06, 16) },
                5.5
                );

            var market = GetMarket();
            IMarketCondition marketCondition = new MarketCondition(
                x => x.ValuationDate.Value = market.ReferenceDate,
                x => x.DiscountCurve.Value = market.GetData<CurveData>("Fr007").YieldCurve,
                x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> {{ "", market.GetData<CurveData>("Fr007").YieldCurve } } ,
                x => x.SpotPrices.Value = new Dictionary<string, double> { {"", 240.6 } },
                x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", market.GetData<VolSurfMktData>("goldVolSurf").ToImpliedVolSurface(market.ReferenceDate) } }
                );

            var engine = new AnalyticalBinaryEuropeanOptionEngine();
            var result = engine.Calculate(goldOption, marketCondition, PricingRequest.All);
            Assert.AreEqual(0.615985804, result.Pv, 1e-8);
            Assert.AreEqual(-0.409184000310747, result.Delta, 1e-8);
            Assert.AreEqual(0.239395655872165, result.Gamma, 1e-8);
            Assert.AreEqual(0.265082040475919, result.Vega, 1e-8);
            Assert.AreEqual(-8.43816170E-07, result.Rho, 1e-8);

            var engine2 = new AnalyticalBinaryEuropeanOptionReplicationEngine(2.0, BinaryOptionReplicationStrategy.Up);
            result = engine2.Calculate(goldOption, marketCondition, PricingRequest.All);
            Assert.AreEqual(1.25655313, result.Pv, 1e-8);
            Assert.AreEqual(-0.745647330608376, result.Delta, 1e-8);
            Assert.AreEqual(0.377738685022888, result.Gamma, 1e-8);
            Assert.AreEqual(0.417379971819684, result.Vega, 1e-8);
            Assert.AreEqual(-1.72130447839702E-06, result.Rho, 1e-8);
        }

        private QdpMarket GetMarketJinTao(String valueDate, double rate, double vol, double dividendRate, string curveName, string volName, string diviCurveName, double spot = 1.0)
        {
            var curveConvention = new CurveConvention("curveConvention",
                    "CNY",
                    "ModifiedFollowing",
                    "Chn",
                    "Act365",
                    "Continuous",
                    "CubicHermiteMonotic");
            var curveDefinitions = new List<InstrumentCurveDefinition>();
            var rates = new[]
            {
                new RateMktData("1D", rate, "Spot", "None",curveName),
                new RateMktData("1Y", rate, "Spot", "None",curveName),
            };

            var fr007Curve = new InstrumentCurveDefinition(
                curveName,
                curveConvention,
                rates,
                "SpotCurve");
            curveDefinitions.Add(fr007Curve);

            var curveConvention2 = new CurveConvention("curveConvention2",
                    "CNY",
                    "ModifiedFollowing",
                    "Chn",
                    "Act365",
                    "Continuous",
                    "CubicHermiteFd");
            //var diviCurveName = "diviCurve";
            var goldRates = new[]
            {
                new RateMktData("1D", dividendRate, "Spot", "None", diviCurveName),
                new RateMktData("1Y", dividendRate, "Spot", "None", diviCurveName),
            };
            var goldConvenienceYieldCurve = new InstrumentCurveDefinition(diviCurveName, curveConvention2, goldRates, "SpotCurve");
            curveDefinitions.Add(goldConvenienceYieldCurve);

            var volSurfMktData = new[] { new VolSurfMktData(volName, vol), };
            var stockMktData = new[] { new StockMktData("000300.SH", spot), };
            var marketInfo2 = new MarketInfo("TestMarket",         
                referenceDate: valueDate,
                yieldCurveDefinitions: curveDefinitions.ToArray(),
                historicalIndexRates: HistoricalDataLoadHelper.HistoricalIndexRates,
                volSurfaceDefinitions: volSurfMktData,
                stockDataDefinitions: stockMktData
            );

            MarketFunctions.BuildMarket(marketInfo2, out QdpMarket market);
            return market;
        }

        private QdpMarket GetMarket()
        {
            var referenceDate = "2015-06-11";
            var curveConvention = new CurveConvention("curveConvention",
                    "CNY",
                    "ModifiedFollowing",
                    "Chn",
                    "Act365",
                    "Continuous",
                    "CubicHermiteMonotic");
            var curveDefinitions = new List<InstrumentCurveDefinition>();
            // 1 - discount curve
            var rates = new[]
            {
                new RateMktData("0D", 0.0114, "Spot", "None","Fr007"),
                new RateMktData("1D", 0.0114, "Spot", "None","Fr007"),
                new RateMktData("7D", 0.021, "Spot", "None","Fr007"),
                new RateMktData("98D", 0.0228, "Spot", "None","Fr007"),
                new RateMktData("186D", 0.0231, "Spot", "None","Fr007"),
                new RateMktData("277D", 0.024, "Spot", "None","Fr007"),
                new RateMktData("368D", 0.0241, "Spot", "None","Fr007"),
                new RateMktData("732D", 0.0248, "Spot", "None","Fr007"),
            };

            var fr007Curve = new InstrumentCurveDefinition(
                "Fr007",
                curveConvention,
                rates,
                "SpotCurve");
            curveDefinitions.Add(fr007Curve);

            var curveConvention2 = new CurveConvention("curveConvention2",
                    "CNY",
                    "ModifiedFollowing",
                    "Chn",
                    "Act365",
                    "Continuous",
                    "CubicHermiteFd");
            var goldRates = new[]
            {
                new RateMktData("0D", 0.0, "Spot", "None", "goldYield"),
                new RateMktData("187D", 0.0, "Spot", "None", "goldYield"),
                new RateMktData("370D", 0.0, "Spot", "None", "goldYield"),
            };
            var goldConvenienceYieldCurve = new InstrumentCurveDefinition("goldYield", curveConvention2, goldRates, "SpotCurve");
            curveDefinitions.Add(goldConvenienceYieldCurve);

            var volSurfMktData = new[] { new VolSurfMktData("goldVolSurf", 0.14), };
            var marketInfo2 = new MarketInfo("TestMarket")
            {
                ReferenceDate = referenceDate,
                YieldCurveDefinitions = curveDefinitions.ToArray(),
                HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates,
                VolSurfMktDatas = volSurfMktData
            };

            MarketFunctions.BuildMarket(marketInfo2, out QdpMarket market);
            return market;
        }

        private IYieldCurve ZeroFirstPoint(IYieldCurve yieldCurve)
        {
            return new YieldCurve(
                yieldCurve.Name,
                yieldCurve.ReferenceDate,
                yieldCurve.KeyPoints.Select((x, i) => i == 0 ? Tuple.Create(x.Item1, 0.0) : x).ToArray(),
                yieldCurve.Bda,
                yieldCurve.DayCount,
                yieldCurve.Calendar,
                yieldCurve.Currency,
                yieldCurve.Compound,
                yieldCurve.Interpolation,
                YieldCurveTrait.SpotCurve,
                yieldCurve.BaseMarket,
                ((YieldCurve)yieldCurve).CalibrateMktUpdateCondition,
                yieldCurve.Spread
                );
        }

        private IYieldCurve ZeroRateCurve(IYieldCurve yieldCurve)
        {
            return new YieldCurve(
                yieldCurve.Name,
                yieldCurve.ReferenceDate,
                yieldCurve.KeyPoints.Select((x, i) => Tuple.Create(x.Item1, 0.0)).ToArray(),
                yieldCurve.Bda,
                yieldCurve.DayCount,
                yieldCurve.Calendar,
                yieldCurve.Currency,
                yieldCurve.Compound,
                yieldCurve.Interpolation,
                YieldCurveTrait.SpotCurve,
                yieldCurve.BaseMarket,
                ((YieldCurve)yieldCurve).CalibrateMktUpdateCondition,
                yieldCurve.Spread
                );
        }

        private Date DateFromStr(String dateStr)
        {
            var dt = Convert.ToDateTime(dateStr);
            return new Date(dt.Year, dt.Month, dt.Day);
        }

        private QdpMarket TestMarket(String referenceDate = "2017-10-25", double vol = 0.2, double spot = 1.0, double rate = 0.05, double dividend = 0.0)
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
                new RateMktData("1D", rate, "Spot", "None", fr007CurveName),
                new RateMktData("5Y", rate, "Spot", "None", fr007CurveName),
            };

            var dividendCurveName = "Dividend";
            var dividendRateDefinition = new[]
            {
                new RateMktData("1D", dividend, "Spot", "None", dividendCurveName),
                new RateMktData("5Y", dividend, "Spot", "None", dividendCurveName),
            };

            var curveDefinition = new[]
            {
                new InstrumentCurveDefinition(fr007CurveName, curveConvention, fr007RateDefinition, "SpotCurve"),
                new InstrumentCurveDefinition(dividendCurveName, curveConvention, dividendRateDefinition, "SpotCurve"),
            };

            var volSurf = new[] { new VolSurfMktData("VolSurf", vol), };
            var stockMktData = new[] { new StockMktData("000300.SH", spot), };

            var marketInfo = new MarketInfo("tmpMarket",
                referenceDate: referenceDate,
                yieldCurveDefinitions: curveDefinition,
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
