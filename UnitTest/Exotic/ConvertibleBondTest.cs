using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Market.BuiltObjects;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities.Coupons;
using Qdp.Pricing.Library.Equity.Engines.Numerical;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.Pricing.Library.Exotic;
using Qdp.Pricing.Library.Exotic.Engines;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Ecosystem.Utilities;
using UnitTest.Utilities;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using Qdp.Pricing.Library.Common.Engines;
using Qdp.Pricing.Library.Equity.Engines.MonteCarlo;
using Qdp.Pricing.Library.Common.Utilities;
using System.Linq;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace UnitTest.Exotic
{
	[TestClass]
	public class ConvertibleBondTest
	{
		[TestMethod]
		public void TestConvertibleBond_TreePricer()
		{
            var convertibleBond = GeliCB();
            var engine = new SimpleConvertibleBondEngine<BinomialTreeAmericanEngine>(
				 new BinomialTreeAmericanEngine(BinomialTreeType.CoxRossRubinstein, 2000)
				 );
            var market = TestMarket_Jin("2017-10-20", spot: 6.07);
            var result = engine.Calculate(convertibleBond, market, PricingRequest.All);
            var optionPrem = market.MktQuote.Value[convertibleBond.Bond.Id].Item2 - result.Pv;
            Assert.AreEqual(convertibleBond.ConversionRatio * 1.598263004, optionPrem , 1e-7);
		}

        [TestMethod]
        public void TestConvertibleBond_BSPricer()
        {
            var convertibleBond = GeliCB(American: false);
            var optionEngine = new AnalyticalVanillaEuropeanOptionEngine();
            var engine = new SimpleConvertibleBondEngine<AnalyticalVanillaEuropeanOptionEngine>(optionEngine as AnalyticalVanillaEuropeanOptionEngine);
            var market = TestMarket_Jin("2017-10-25", spot: 6.14);
            var result = engine.Calculate(convertibleBond, market, PricingRequest.All);
            var optionPrem = market.MktQuote.Value[convertibleBond.Bond.Id].Item2 - result.Pv;
            Assert.AreEqual(convertibleBond.ConversionRatio * 1.634147761, optionPrem, 1e-7);
        }

        [TestMethod]
        public void TestBSoptionPremium()
        {
            BsOptionTest(expiryStr: "2019-12-25", firstDayStr: "2015-06-30", strike: 7.24, spot: 6.14, thirdPartyPrice: 120.4448 - 97.8386, valueDay: "2017-10-25"); //geli,  120.4448 - 97.8386
            BsOptionTest(expiryStr: "2021-06-12", firstDayStr: "2015-12-14", strike: 42.8000, spot: 18.87, thirdPartyPrice: 101.4779 - 93.3689, valueDay: "2017-10-25"); //hangxin
            BsOptionTest(expiryStr: "2022-01-04", firstDayStr: "2016-07-04", strike: 7.43, spot: 8.32, thirdPartyPrice: 145.45 - 90.5987, valueDay: "2017-10-25"); //sanyi
            BsOptionTest(expiryStr: "2022-01-05", firstDayStr: "2016-07-05", strike: 8.8100, spot: 11.2900, thirdPartyPrice: 160.1445 - 92.1023, valueDay: "2017-10-25"); //guomao
            BsOptionTest(expiryStr: "2022-01-15", firstDayStr: "2016-07-21", strike: 18.65, spot: 21.4800, thirdPartyPrice: 148.5769 - 90.9897, valueDay: "2017-10-25"); //jiuzhou
            BsOptionTest(expiryStr: "2021-02-02", firstDayStr: "2015-08-03", strike: 10.46, spot: 8.2400, thirdPartyPrice: 120.1935 - 94.6644, valueDay: "2017-10-25"); //dianqi
            BsOptionTest(expiryStr: "2022-01-22", firstDayStr: "2016-07-22", strike: 21.4300, spot: 28.1500, thirdPartyPrice: 161.2975 - 90.2956, valueDay: "2017-10-25"); //guangqi
            BsOptionTest(expiryStr: "2022-03-18", firstDayStr: "2016-09-19", strike: 9.3000, spot: 6.3900, thirdPartyPrice: 115.7403 - 91.5036, valueDay: "2017-10-25"); //jiangnan
            BsOptionTest(expiryStr: "2023-03-17", firstDayStr: "2017-09-18", strike: 4.26, spot: 4.06, thirdPartyPrice: 132.7099 - 85.0467, valueDay: "2017-10-25"); //guangda
            BsOptionTest(expiryStr: "2023-03-24", firstDayStr: "2017-09-25", strike: 16.72, spot: 14.95, thirdPartyPrice: 127.8560 - 84.6537, valueDay: "2017-10-25"); //luotuo
            BsOptionTest(expiryStr: "2023-07-06", firstDayStr: "2018-01-08", strike: 20.2, spot: 20.79, thirdPartyPrice: 138.8117 - 83.7829, valueDay: "2017-10-25"); //guojun
            BsOptionTest(expiryStr: "2021-04-18", firstDayStr: "2017-04-20", strike: 17.8, spot: 17.18, thirdPartyPrice: 132.9307 - 93.9935, valueDay: "2017-10-25"); //yiling
            BsOptionTest(expiryStr: "2021-12-18", firstDayStr: "2016-06-27", strike: 9.7700, spot: 7.75, thirdPartyPrice: 123.0610 - 92.8098, valueDay: "2017-10-25"); //lanbiao
            BsOptionTest(expiryStr: "2022-06-08", firstDayStr: "2017-12-16", strike: 5.2500, spot: 3.68, thirdPartyPrice: 118.3591 - 92.2978, valueDay: "2017-10-25"); //haiyin
            BsOptionTest(expiryStr: "2023-06-02", firstDayStr: "2017-12-04", strike: 8.0, spot: 6.91, thirdPartyPrice: 130.1204 - 88.4591, valueDay: "2017-10-25"); //mosu
            BsOptionTest(expiryStr: "2022-01-22", firstDayStr: "2016-07-29", strike: 9.32, spot: 9.7, thirdPartyPrice: 141.2179 - 92.2955, valueDay: "2017-10-25"); //shunchang
            BsOptionTest(expiryStr: "2022-04-21", firstDayStr: "2016-10-28", strike: 7.74, spot: 5.95, thirdPartyPrice: 116.7641 - 86.6654, valueDay: "2017-10-25"); //huifeng
            BsOptionTest(expiryStr: "2022-07-29", firstDayStr: "2017-02-06", strike: 10.03, spot: 6.13, thirdPartyPrice: 110.7986 - 89.9877, valueDay: "2017-10-25"); //hongtao
            BsOptionTest(expiryStr: "2023-04-17", firstDayStr: "2017-10-23", strike: 20.46, spot: 22.23, thirdPartyPrice: 146.1505 - 87.4726, valueDay: "2017-10-25"); //yongdong
            BsOptionTest(expiryStr: "2023-06-08", firstDayStr: "2017-12-15", strike: 12.9, spot: 12.72, thirdPartyPrice: 137.3286 - 86.0802, valueDay: "2017-10-25"); //jiuqi
            BsOptionTest(expiryStr: "2023-09-25", firstDayStr: "2018-03-29", strike: 38.48, spot: 39.45, thirdPartyPrice: 138.9874 - 83.3357, valueDay: "2017-10-25"); //yuhong
            BsOptionTest(expiryStr: "2017-12-10", firstDayStr: "2015-12-14", strike: 42.31, spot: 60.26, thirdPartyPrice: 145.5966 - 102.4247, valueDay: "2017-10-25"); //baogang
            BsOptionTest(expiryStr: "2020-06-08", firstDayStr: "2016-06-08", strike: 56.02, spot: 37.09, thirdPartyPrice: 105.5806 - 90.8388, valueDay: "2017-10-25"); //tianji

            //bsOptionTest(expiryStr: "2018-10-26", firstDayStr: "2016-10-26", strike: 16.75, spot: 11.31, thirdPartyPrice: 101.9696 - 96.2407, valueDay: "2017-10-25"); //qingkong  0.6% diff

            BsOptionTest(expiryStr: "2021-11-05", firstDayStr: "2016-11-05", strike: 6.76, spot: 3.82, thirdPartyPrice: 104.7105 - 89.2127, valueDay: "2017-10-25"); //guosheng
            BsOptionTest(expiryStr: "2020-12-08", firstDayStr: "2016-12-08", strike: 37.58, spot: 40.55, thirdPartyPrice: 143.8584 - 98.6763, valueDay: "2017-10-25"); //guozi
            BsOptionTest(expiryStr: "2021-06-23", firstDayStr: "2017-06-23", strike: 16.19, spot: 12.4, thirdPartyPrice: 122.6049 - 96.5159, valueDay: "2017-10-25"); //wanxin
            BsOptionTest(expiryStr: "2021-10-31", firstDayStr: "2017-10-31", strike: 15.75, spot: 9.31, thirdPartyPrice: 107.8323 - 90.9176, valueDay: "2017-10-25"); //fenghuang
            BsOptionTest(expiryStr: "2022-04-24", firstDayStr: "2018-04-26", strike: 9.8, spot: 5.99, thirdPartyPrice: 112.5964 - 92.7272, valueDay: "2017-10-25"); //shangao
            BsOptionTest(expiryStr: "2022-07-13", firstDayStr: "2018-07-18", strike: 8.92, spot: 8.17, thirdPartyPrice: 129.6275 - 87.8707, valueDay: "2017-10-25"); //zhongyou
            BsOptionTest(expiryStr: "2020-08-03", firstDayStr: "2018-08-03", strike: 17.12, spot: 16.62, thirdPartyPrice: 126.8272 - 91.9095, valueDay: "2017-10-25"); //tongkun
            BsOptionTest(expiryStr: "2022-08-17", firstDayStr: "2018-08-17", strike: 25.0, spot: 17.11, thirdPartyPrice: 111.5956 - 85.8780, valueDay: "2017-10-25"); //zhebao
            BsOptionTest(expiryStr: "2020-09-04", firstDayStr: "2018-09-04", strike: 13.49, spot: 12.22, thirdPartyPrice: 122.4918 - 91.5172, valueDay: "2017-10-25"); //juhua
        }

        private Date DateFromStr(String dateStr)
        {
            var dt = Convert.ToDateTime(dateStr);
            return new Date(dt.Year, dt.Month, dt.Day);
        }

        private void BsOptionTest(String expiryStr, String firstDayStr, Double strike = 7.24,  Double spot = 6.14, Double thirdPartyPrice = 20.0, String valueDay = "2017-10-25" )
        {
            Date expiry = DateFromStr(expiryStr);
            Date firstConversionDate = DateFromStr(firstDayStr);
            var option = CreateOption(strike: strike, expiry: expiry, firstConversionDate: firstConversionDate, American: false);
            var engine = new AnalyticalVanillaEuropeanOptionEngine();
            var market = TestMarket_Jin(valueDay, spot: spot);
            var result = engine.Calculate(option, market, PricingRequest.All);
            var premiumPart = result.Pv * 100.0 / strike;
            Console.WriteLine("YieldChain premium: {0}, wind: {1}", premiumPart, thirdPartyPrice);
            var diff = Math.Abs(premiumPart - thirdPartyPrice) / thirdPartyPrice * 100;
            //Assert.AreEqual(premiumPart, expected);
            Assert.AreEqual(true, diff< 0.3);  // < 0.3%
        }


        [TestMethod]
        public void TestConvertibleBond_14BaoGangE_BSPricer_NoFailure()
        {
            var convertibleBond = BaoGangCB();
            var optionEngine = new AnalyticalVanillaEuropeanOptionEngine();
            var engine = new SimpleConvertibleBondEngine<AnalyticalVanillaEuropeanOptionEngine>(optionEngine as AnalyticalVanillaEuropeanOptionEngine);
            var market = TestMarket_Jin("2017-10-24", spot: 58.6800, CBprice: 141.97);
            var result = engine.Calculate(convertibleBond, market, PricingRequest.All);
            var optionPrem = market.MktQuote.Value[convertibleBond.Bond.Id].Item2 - result.Pv;
            Assert.AreEqual(convertibleBond.ConversionRatio * 16.74444583, optionPrem, 1e-7);
        }

        //TODO: enable true MC pricer after CB MC model improvement
        public void TestConvertibleBond_American_MCPricer()
        {
            var convertibleBond = GeliCB(American: true);
            var optionEngine = new GenericMonteCarloEngine(2, 50000);
            var engine = new SimpleConvertibleBondEngine<GenericMonteCarloEngine>(optionEngine as GenericMonteCarloEngine);
            var market = TestMarket_Jin("2017-10-20", spot: 6.07);
            var result = engine.Calculate(convertibleBond, market, PricingRequest.All);
            var optionPrem = market.MktQuote.Value[convertibleBond.Bond.Id].Item2 - result.Pv;
            Assert.AreEqual(convertibleBond.ConversionRatio * 1.603780133, optionPrem, 1e-7);
        }

        [TestMethod]
        public void TestPureBondValue()
        {
            DoTestPureBond(GeliBondPart(), 97.7732, "2017-10-25", 1e-4);    //wind: 97.7863, sheet  is 97.7732
            DoTestPureBond(BaoGangBondPart(), 102.3773, "2017-10-25", 1e-4); //Wind: 102.3690, sheet 102.3910
            DoTestPureBond(YongDongBondPart(), 87.4021, "2017-10-25", 1e-4);  //wind price: 87.4258, slight diff
            DoTestPureBond(ZheBaoBondPart(), 85.8328, "2017-10-25", 1e-4); //Wind: 85.8321
            DoTestPureBond(TianJiBondPart(), 90.7660, "2017-10-25", 1e-4); //Wind: 90.7903,  this contract has no redemption clause
            DoTestPureBond(HangXinBondPart(), 93.2584, "2017-10-25", 1e-4); //Wind: 93.3190
        }

        [TestMethod]
        public void PureBondPnLTest()
        {
            var bond = GeliBondPart();
            var t = "2017-10-25";
            var t1 = "2017-10-26";
            var bondengine = new BondEngine();

            var tClose = 97.5;
            var t1Close = 96.5; 

            var tMarket = TestMarket_Jin(t, spot: 6.07, CBprice: tClose, yield: 0.05);  //actually bond price
            var tMarketRollToT1 = TestMarket_Jin(t1, spot: 6.07, CBprice: tClose, yield: 0.05);
            var t1Market = TestMarket_Jin(t1, spot: 6.07, CBprice: t1Close, yield: 0.055);

            var actualPnl = t1Close - tClose;

            var pricingRequests = PricingRequest.Pv | PricingRequest.ZeroSpread | PricingRequest.ZeroSpreadDelta | PricingRequest.Ytm | PricingRequest.AiEod | PricingRequest.KeyRateDv01 | PricingRequest.Convexity;
            var tResult = bondengine.Calculate(bond, tMarket, pricingRequests);
            var t1Result = bondengine.Calculate(bond, t1Market, pricingRequests);
            var tResultRecalibrated = bondengine.Calculate(bond, tMarketRollToT1, PricingRequest.Pv);
            
            //1.
            var timePnL = tResultRecalibrated.Pv - tResult.Pv;
            
            var curveMove = (0.055 - 0.05)*1e4;
            var pv01 = tResult.KeyRateDv01.First();
            var pv01PnL = pv01.Value.Select(x => new CurveRisk(x.Tenor, 1.0* x.Risk* curveMove));
            var curvePnL = pv01PnL.Sum(x => x.Risk);

            var zspreadPnL = (t1Result.ZeroSpread - tResult.ZeroSpread) * tResult.ZeroSpreadDelta * 1e4;

            var carryPnL = t1Result.Ai - tResult.Ai;
            var rolllDown = timePnL - carryPnL;

            var pnlConverixy = 0.5 * Math.Pow(t1Result.Ytm - tResult.Ytm, 2.0) * tResult.Convexity* tClose;  //originally without

            var estimatedPL = timePnL + curvePnL + zspreadPnL + pnlConverixy;
            Assert.AreEqual(true, Math.Abs((estimatedPL - actualPnl)/ actualPnl) < 0.01 );

        }


        private void DoTestPureBond(Bond bond,Double expected, String date = "2017-10-20", Double precision = 1e-4)
        {
            var bondengine = new BondEngine();
            var market = TestMarket_Jin(date, spot: 6.07, CBprice: 97.5);
            var res = bondengine.Calculate(bond, market, PricingRequest.Pv| PricingRequest.ZeroSpread| PricingRequest.ZeroSpreadDelta | PricingRequest.Ytm);
            var pv = res.Pv;
            Assert.AreEqual(expected, pv, precision);
        }

        private Bond YongDongBondPart()
        {
            var bond = new Bond(
                "010002",
                 new Date(2017, 4, 17),
                 new Date(2023, 4, 17),
                 100,
                 CurrencyCode.CNY,
                 new StepWiseCoupon(new Dictionary<Date, double>
                {
                    {new Date(2017, 4, 17), 0.005},
                    {new Date(2018, 4, 17), 0.007},
                    {new Date(2019, 4, 17), 0.01},
                    {new Date(2020, 4, 17), 0.015},
                    {new Date(2021, 4, 17), 0.018},
                    {new Date(2022, 4, 17), 0.08}
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
                 redemption: new Redemption(1.08, RedemptionType.SeparatePrincipalWithLastCoupon)
                 );

            return bond;
        }

        private Bond GeliBondPart()
        {
            var bond = new Bond(
                "010002",
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
                 redemption: new Redemption(1.06, RedemptionType.SeparatePrincipalWithLastCoupon )
                 );

            return bond;
        }

        private Bond BaoGangBondPart()
        {
            var bond = new Bond(
                "010002",
                 new Date(2014, 12, 10),
                 new Date(2017, 12, 10),
                 100,
                 CurrencyCode.CNY,
                 new StepWiseCoupon(new Dictionary<Date, double>
                {
                    {new Date(2014, 12, 10), 0.015},
                    {new Date(2015, 12, 25), 0.015},
                    {new Date(2016, 12, 25), 0.015},
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
                 redemption: new Redemption(1.015, RedemptionType.SeparatePrincipal)
                 );

            return bond;
        }

        private Bond TianJiBondPart()
        {
            var bond = new Bond(
                "010002",
                 new Date(2015, 6, 8),
                 new Date(2020, 6, 8),
                 100,
                 CurrencyCode.CNY,
                 new StepWiseCoupon(new Dictionary<Date, double>
                {
                    {new Date(2015, 6, 8), 0.01},
                    {new Date(2016, 6, 8), 0.01},
                    {new Date(2017, 6, 8), 0.01},
                    {new Date(2018, 6, 8), 0.01},
                    {new Date(2019, 6, 8), 0.01},
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
                 TradingMarket.ChinaExShg
                 );

            return bond;
        }

        private Bond HangXinBondPart()
        {
            var bond = new Bond(
                "010002",
                 new Date(2015, 6, 12),
                 new Date(2021, 6, 12),
                 100,
                 CurrencyCode.CNY,
                 new StepWiseCoupon(new Dictionary<Date, double>
                {
                    {new Date(2015, 6, 12), 0.002},
                    {new Date(2016, 6, 12), 0.005},
                    {new Date(2017, 6, 12), 0.01},
                    {new Date(2018, 6, 12), 0.015},
                    {new Date(2019, 6, 12), 0.015},
                    {new Date(2020, 6, 12), 0.016},
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
                 redemption: new Redemption(1.07, RedemptionType.SeparatePrincipalWithLastCoupon)
                 );

            return bond;
        }

        private Bond ZheBaoBondPart()
        {
            var bond = new Bond(
                "010002",
                 new Date(2017, 8, 17),
                 new Date(2022, 8, 17),
                 100.0,
                 CurrencyCode.CNY,
                 new StepWiseCoupon(new Dictionary<Date, double>
                {
                    {new Date(2017, 8, 17), 0.01},
                    {new Date(2018, 8, 17), 0.01},
                    {new Date(2019, 8, 17), 0.01},
                    {new Date(2020, 8, 17), 0.01},
                    {new Date(2021, 8, 17), 0.01},
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
                 redemption: new Qdp.Pricing.Library.Common.Utilities.Redemption(1.03, RedemptionType.SeparatePrincipal )
                 );

            return bond;
        }

        private ConvertibleBond HangXinCB(Boolean American = true)
        {
            var bond = HangXinBondPart();
            var option = CreateOption(strike: 42.8, expiry: new Date(2021, 6, 12), firstConversionDate: new Date(2015, 12, 14), American: false);
            return new ConvertibleBond(bond, option, null, null);
        }

        private VanillaOption CreateOption(Double strike, Date expiry, Date firstConversionDate, Boolean American = true) {
            var americanNoticeDates = CalendarImpl.Get("chn").BizDaysBetweenDates(new Date(2015, 06, 30), new Date(2019, 12, 25)).ToArray();
            var exerciseStyle = American ? OptionExercise.American : OptionExercise.European;
            var noticeDates = American ? americanNoticeDates : new Date[] { expiry };
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

        private ConvertibleBond GeliCB(Boolean American = true)
        {
            var bond = GeliBondPart();
            var option = CreateOption(strike: 7.24, expiry: new Date(2019, 12, 24), firstConversionDate: new Date(2015, 06, 30), American: false);
            return new ConvertibleBond(bond, option, null, null);
        }

        private ConvertibleBond BaoGangCB()
        {
            var bond = BaoGangBondPart();
            var option = CreateOption(strike: 42.31, expiry: new Date(2017, 12, 10), firstConversionDate: new Date(2015, 12, 14), American: false);
            return new ConvertibleBond(bond, option, null, null);
        }


        private IMarketCondition TestMarket_Jin(String date, double spot, double CBprice = 111.74, double yield = 0.05 )
        {
            var vol = 0.5;
            return TestMarket_generic(date, flatRiskFreeRate: yield, flatCreditRate: yield, vol: vol, spotPrice: spot, cbDirtyPrice: CBprice);
        }

        private IMarketCondition TestMarket_generic(string referenceDate, 
            double flatRiskFreeRate, double flatCreditRate,
            double vol, double spotPrice, double cbDirtyPrice)
        {
            
            var compoundConvention = "Annual";   //quite close for most bonds
            //var compoundConvention = "Quarterly"; //fails
            //var compoundConvention = "BiMonthly"; //fails 
            //var compoundConvention = "SubTriple"; //too big, 96.15
            //var compoundConvention = "SemiAnnual"; //too big  96.1506

            //var compoundConvention = "Continuous"; //bad, too small
            var curveTrait = "SpotCurve";
            var rateTrait = "Spot";

            var curveConvention = new CurveConvention("curveConvention",
                 "CNY",
                 "ModifiedFollowing",
                 "Chn_ib",
                 "Act365",
                 compoundConvention,
                 "Linear");
            var curveDefinitions = new List<InstrumentCurveDefinition>();
            // 1 - discount curve
            var rfCurveName = "Fr007";
            var rates = new[]
            {
                new RateMktData("1D", flatRiskFreeRate, rateTrait, "None", rfCurveName),
                new RateMktData("2Y", flatRiskFreeRate, rateTrait, "None", rfCurveName),
                new RateMktData("3Y", flatRiskFreeRate, rateTrait, "None", rfCurveName),
                new RateMktData("15Y", flatRiskFreeRate, rateTrait, "None", rfCurveName),
            };

            var fr007Curve = new InstrumentCurveDefinition(
                 rfCurveName,
                 curveConvention,
                 rates,
                 curveTrait);
            curveDefinitions.Add(fr007Curve);

            var creditCurveName = "bondCreditCurve";
            var rates2 = new[]
            {
                new RateMktData("1D", flatCreditRate, rateTrait, "None", creditCurveName),
                new RateMktData("2Y", flatCreditRate, rateTrait, "None", creditCurveName),
                new RateMktData("3Y", flatCreditRate, rateTrait, "None", creditCurveName),
                new RateMktData("15Y", flatCreditRate, rateTrait, "None", creditCurveName),
            };
            var bondCreditCurve = new InstrumentCurveDefinition(
                 creditCurveName,
                 curveConvention,
                 rates2,
                 curveTrait);
            curveDefinitions.Add(bondCreditCurve);

            //assuming zero, almost useless for China
            var dCurveName = "dividendCurve";
            var rates3 = new[]
            {
                new RateMktData("1D", 0, rateTrait, "None", dCurveName),
                new RateMktData("15Y", 0, rateTrait, "None", dCurveName),
            };
            var dividendCurve = new InstrumentCurveDefinition(
                 dCurveName,
                 curveConvention,
                 rates3,
                 curveTrait);
            curveDefinitions.Add(dividendCurve);

            var volName = "VolSurf";
            var volSurf = new[] { new VolSurfMktData(volName, vol), };

            var marketInfo = new MarketInfo("TestMarket")
            {
                ReferenceDate = referenceDate,
                YieldCurveDefinitions = curveDefinitions.ToArray(),
                VolSurfMktDatas = volSurf,
                HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates
            };

            MarketFunctions.BuildMarket(marketInfo, out QdpMarket market);
            var volsurf = market.GetData<VolSurfMktData>(volName).ToImpliedVolSurface(market.ReferenceDate);
            return new MarketCondition(x => x.ValuationDate.Value = market.ReferenceDate,
                 x => x.DiscountCurve.Value = market.GetData<CurveData>(creditCurveName).YieldCurve,
                 x => x.FixingCurve.Value = market.GetData<CurveData>(creditCurveName).YieldCurve,
                 x => x.RiskfreeCurve.Value = market.GetData<CurveData>(rfCurveName).YieldCurve,
                 x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>(dCurveName).YieldCurve } },
                 x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                 x => x.SpotPrices.Value = new Dictionary<string, double> { { "", spotPrice } },
                 x => x.MktQuote.Value = new Dictionary<string, Tuple<PriceQuoteType, double>> { { "010002", Tuple.Create(PriceQuoteType.Dirty, cbDirtyPrice) } },
                 x => x.HistoricalIndexRates.Value = new Dictionary<IndexType, SortedDictionary<Date, double>>()
                 );
        }

        private IMarketCondition TestMarket()
		{
			var referenceDate = "2017-10-18";
			var curveConvention = new CurveConvention("curveConvention",
				 "CNY",
				 "ModifiedFollowing",
				 "Chn_ib",
				 "Act365",
				 "Continuous",
				 "Linear");
			var curveDefinitions = new List<InstrumentCurveDefinition>();
			// 1 - discount curve
			var rates = new[]
			{
				new RateMktData("1D", 0.015, "Spot", "None", "Fr007"),
				new RateMktData("15Y", 0.015, "Spot", "None", "Fr007"),
			};

			var fr007Curve = new InstrumentCurveDefinition(
				 "Fr007",
				 curveConvention,
				 rates, 
				 "SpotCurve");
			curveDefinitions.Add(fr007Curve);

			var rates2 = new[]
			{
				new RateMktData("1D", 0.015, "Spot", "None", "bondCreditCurve"),
				new RateMktData("15Y", 0.015, "Spot", "None", "bondCreditCurve"),
			};
			var bondCreditCurve = new InstrumentCurveDefinition(
				 "bondCreditCurve",
				 curveConvention,
				 rates2, 
				 "SpotCurve");
			curveDefinitions.Add(bondCreditCurve);

			var rates3 = new[]
			{
				new RateMktData("1D", 0, "Spot", "None", "dividendCurve"),
				new RateMktData("15Y", 0, "Spot", "None", "dividendCurve"),
			};
			var dividendCurve = new InstrumentCurveDefinition(
				 "dividendCurve",
				 curveConvention,
				 rates3,
				 "SpotCurve");
			curveDefinitions.Add(dividendCurve);

			var volSurf = new[] { new VolSurfMktData("VolSurf", 0.255669),  };

			var marketInfo = new MarketInfo("TestMarket")
			{
				ReferenceDate = referenceDate,
				YieldCurveDefinitions = curveDefinitions.ToArray(),
				VolSurfMktDatas = volSurf,
				HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates
			};

            MarketFunctions.BuildMarket(marketInfo, out QdpMarket market);
            var volsurf = market.GetData<VolSurfMktData>("VolSurf").ToImpliedVolSurface(market.ReferenceDate);
            return new MarketCondition(x => x.ValuationDate.Value = market.ReferenceDate,
				 x => x.DiscountCurve.Value = market.GetData<CurveData>("bondCreditCurve").YieldCurve,
				 x => x.FixingCurve.Value = market.GetData<CurveData>("bondCreditCurve").YieldCurve,
				 x => x.RiskfreeCurve.Value = market.GetData<CurveData>("Fr007").YieldCurve,
				 x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>("dividendCurve").YieldCurve } },
				 x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                 x => x.SpotPrices.Value = new Dictionary<string, double> { { "", 6.21 } },
				 x => x.MktQuote.Value = new Dictionary<string, Tuple<PriceQuoteType, double>> { { "010002", Tuple.Create(PriceQuoteType.Dirty, 140.65) } },
				 x => x.HistoricalIndexRates.Value = new Dictionary<IndexType, SortedDictionary<Date, double>>()
				 );
		}

        private IMarketCondition TestMarket2()
        {
            var referenceDate = "2017-10-20";
            var curveConvention1 = new CurveConvention("discountCurveConvention",
                 "CNY",
                 "None",
                 "Chn_ib",
                 "Act360",
                 "Continuous",
                 "Linear");
            var curveDefinitions = new List<InstrumentCurveDefinition>();
            // 1 - discount curve
            var rates = new[]
            {
                new RateMktData("0Y",0.041004, "Spot", "None", "DiscountCurve"),
                new RateMktData("1M",0.045247, "Spot", "None", "DiscountCurve"),
                new RateMktData("3M",0.049065, "Spot", "None", "DiscountCurve"),
                new RateMktData("6M",0.048518, "Spot", "None", "DiscountCurve"),
                new RateMktData("9M",0.049887, "Spot", "None", "DiscountCurve"),
                new RateMktData("1Y",0.050482, "Spot", "None", "DiscountCurve"),
                new RateMktData("2Y",0.050935, "Spot", "None", "DiscountCurve"),
                new RateMktData("3Y",0.05185, "Spot", "None", "DiscountCurve"),
                new RateMktData("4Y",0.052519, "Spot", "None", "DiscountCurve"),
                new RateMktData("5Y",0.053361, "Spot", "None", "DiscountCurve"),
                new RateMktData("6Y",0.055701, "Spot", "None", "DiscountCurve"),
                new RateMktData("7Y",0.055637, "Spot", "None", "DiscountCurve"),
                new RateMktData("8Y",0.055674, "Spot", "None", "DiscountCurve"),
                new RateMktData("9Y",0.056529, "Spot", "None", "DiscountCurve"),
                new RateMktData("10Y",0.056457, "Spot", "None", "DiscountCurve"),
                new RateMktData("15Y",0.05672, "Spot", "None", "DiscountCurve"),
                new RateMktData("20Y",0.058323, "Spot", "None", "DiscountCurve"),
                new RateMktData("30Y",0.058335, "Spot", "None", "DiscountCurve")
            };
            

            var discountCurve = new InstrumentCurveDefinition(
                 "DiscountCurve",
                 curveConvention1,
                 rates,
                 "SpotCurve");
            curveDefinitions.Add(discountCurve);

            var curveConvention2 = new CurveConvention("riskFreeCurveConvention",
                 "CNY",
                 "ModifiedFollowing",
                 "Chn_ib",
                 "Act365",
                 "Continuous",
                 "CubicHermiteMonotic");
            var rates2 = new[]
            {
                new RateMktData("1D", 0.0283, "Spot", "None", "Fr007SwapCurve"),
                new RateMktData("7D", 0.0344, "Spot", "None", "Fr007SwapCurve"),
                new RateMktData("3M", 0.0349, "Spot", "None", "Fr007SwapCurve"),
                new RateMktData("6M", 0.035411, "Spot", "None", "Fr007SwapCurve"),
                new RateMktData("9M", 0.035567, "Spot", "None", "Fr007SwapCurve"),
                new RateMktData("1Y", 0.035503, "Spot", "None", "Fr007SwapCurve"),
                new RateMktData("2Y", 0.036372, "Spot", "None", "Fr007SwapCurve"),
                new RateMktData("3Y", 0.037521, "Spot", "None", "Fr007SwapCurve"),
                new RateMktData("4Y", 0.037916, "Spot", "None", "Fr007SwapCurve"),
                new RateMktData("5Y", 0.038606, "Spot", "None", "Fr007SwapCurve"),
                new RateMktData("7Y", 0.039, "Spot", "None", "Fr007SwapCurve"),
                new RateMktData("10Y", 0.0397, "Spot", "None", "Fr007SwapCurve")

            };
            var riskFreeCurve = new InstrumentCurveDefinition(
                 "Fr007SwapCurve",
                 curveConvention2,
                 rates2,
                 "SpotCurve");
            curveDefinitions.Add(riskFreeCurve);
            
            var rates3 = new[]
            {
                new RateMktData("1D", 0, "Spot", "None", "DividendCurve"),
                new RateMktData("50Y", 0, "Spot", "None", "DividendCurve"),
            };

            var curveConvention3 = new CurveConvention("dividendCurveConvention",
                 "CNY",
                 "ModifiedFollowing",
                 "Chn_ib",
                 "Act365",
                 "Continuous",
                 "Linear");
            var dividendCurve = new InstrumentCurveDefinition(
                 "DividendCurve",
                 curveConvention3,
                 rates3,
                 "SpotCurve");
            curveDefinitions.Add(dividendCurve);

            var volSurf = new[] { new VolSurfMktData("VolSurf", 0.5885), };

            var marketInfo = new MarketInfo("TestMarket")
            {
                ReferenceDate = referenceDate,
                YieldCurveDefinitions = curveDefinitions.ToArray(),
                VolSurfMktDatas = volSurf,
                HistoricalIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates
            };

            MarketFunctions.BuildMarket(marketInfo, out QdpMarket market);
            var volsurf = market.GetData<VolSurfMktData>("VolSurf" ).ToImpliedVolSurface(market.ReferenceDate);
            return new MarketCondition(x => x.ValuationDate.Value = market.ReferenceDate,
                 x => x.DiscountCurve.Value = market.GetData<CurveData>("DiscountCurve").YieldCurve,
                 x => x.FixingCurve.Value = market.GetData<CurveData>("DiscountCurve").YieldCurve,
                 x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", market.GetData<CurveData>("DividendCurve").YieldCurve } },
                 x => x.RiskfreeCurve.Value = market.GetData<CurveData>("Fr007SwapCurve").YieldCurve,
                 x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { { "", volsurf } },
                 x => x.SpotPrices.Value = new Dictionary<string, double> { {"", 7 }},
                 x => x.MktQuote.Value = new Dictionary<string, Tuple<PriceQuoteType, double>> { { "110030.SH", Tuple.Create(PriceQuoteType.Dirty, 110.8192) } },
                 x => x.HistoricalIndexRates.Value = new Dictionary<IndexType, SortedDictionary<Date, double>>()
                 );
        }
    }
}

