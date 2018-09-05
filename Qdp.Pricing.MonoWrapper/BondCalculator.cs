using System;
using System.IO;
using System.Collections.Generic;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Ecosystem.Market;
using Qdp.Pricing.Ecosystem.Trade.FixedIncome;

namespace Qdp.Pricing.MonoWrapper
{
    public class BondCalculator
    {
        public BondCalculator()
        {
            var bond = new FixedDateCouonAdjustedBondInfo("313100015")
            {
                StartDate = "2015-03-23",
                MaturityDate = "2018-11-17",
                PaymentFrequency = "Quarterly",
                Notional = 32000,
                AccrualDayCount = "Act365",
                AccrualBusinessDayConvention = "None",
                DayCount = "Act365",
                PaymentBusinessDayConvention = "None",
                FirstPaymentDate = "2015-06-21",
                Index = "Lrb5Y",
                FloatingRateMultiplier = 0.9,
                FixedDateCouponAdjustedStyle = "SpecifiedDates",
                AdjustMmDd = "11-18",
                AmoritzationInDate = new Dictionary<string, double>
                {
                    {"2015-06-20", 0.125}, //percentage of initial Notional
					{"2015-12-20", 0.125},
                    {"2016-06-20", 0.125},
                    {"2016-12-20", 0.125},
                    {"2017-06-20", 0.125},
                    {"2017-12-20", 0.125},
                    {"2018-06-20", 0.125},
                    {"2018-11-17", 0.125},
                },                
            };

            _market = TestMarket("2015-03-23", new BondMktData(bond.TradeId, "Dirty", bond.Notional));
        }

        public double PriceToYield()
        {
            var bond = new FixedDateCouonAdjustedBondInfo("313100015")
            {
                StartDate = "2015-03-23",
                MaturityDate = "2018-11-17",
                PaymentFrequency = "Quarterly",
                Notional = 32000,
                AccrualDayCount = "Act365",
                AccrualBusinessDayConvention = "None",
                DayCount = "Act365",
                PaymentBusinessDayConvention = "None",
                FirstPaymentDate = "2015-06-21",
                Index = "Lrb5Y",
                FloatingRateMultiplier = 0.9,
                FixedDateCouponAdjustedStyle = "SpecifiedDates",
                AdjustMmDd = "11-18",
                AmoritzationInDate = new Dictionary<string, double>
                {
                    {"2015-06-20", 0.125}, //percentage of initial Notional
					{"2015-12-20", 0.125},
                    {"2016-06-20", 0.125},
                    {"2016-12-20", 0.125},
                    {"2017-06-20", 0.125},
                    {"2017-12-20", 0.125},
                    {"2018-06-20", 0.125},
                    {"2018-11-17", 0.125},
                },
            };
            
            var bondVf = new BondVf(bond);            
            var result = bondVf.ValueTrade(_market, PricingRequest.Ytm);            
            return result.Ytm;
        }        

        public QdpMarket TestMarket(string valueDate, BondMktData bondMktData)
        {            
            var historiclIndexRates = HistoricalDataLoadHelper.HistoricalIndexRates;
            Console.WriteLine("HistoricalIndexRates");
            var curveConvention = new CurveConvention("fr007CurveConvention",
                "CNY",
                "ModifiedFollowing",
                "Chn_ib",
                "Act365",
                "Continuous",
                "CubicHermiteMonotic");
            
            var fr007CurveName = "Fr007";
            var fr007RateDefinition = new[]
            {
                new RateMktData("1D", 0.035, "Spot", "None", fr007CurveName),
                new RateMktData("5Y", 0.035, "Spot", "None", fr007CurveName),
            };

            
            var curveDefinition = new[]
            {
                new InstrumentCurveDefinition(fr007CurveName, curveConvention, fr007RateDefinition, "SpotCurve"),
            };
            
            var marketInfo = new MarketInfo("tmpMarket", valueDate, curveDefinition)
            {
                BondMktDatas = new[] { bondMktData },
                HistoricalIndexRates = historiclIndexRates
            };
            
            QdpMarket market;
            MarketFunctions.BuildMarket(marketInfo, out market);
            
            return market;
        }

        private QdpMarket _market;
    }

    public class HistoricalDataLoadHelper
    {
        public static readonly Dictionary<string, Dictionary<string, double>> HistoricalIndexRates;

        static HistoricalDataLoadHelper()
        {
            HistoricalIndexRates = new Dictionary<string, Dictionary<string, double>>();
            var files = Directory.GetFiles(@"./Data/HistoricalIndexRates");

            foreach (var file in files)
            {
                var shortName = Path.GetFileNameWithoutExtension(file);
                IndexType indexType;
                if (Enum.TryParse(shortName, out indexType))
                {
                    var temp = new Dictionary<string, double>();
                    var lines = File.ReadAllLines(file);
                    foreach (var line in lines)
                    {
                        var splits = line.Split(',');
                        temp[splits[0]] = Convert.ToDouble(splits[1]);
                    }
                    HistoricalIndexRates[shortName] = temp;
                }
            }
        }

        public static Dictionary<string, double> GetIndexRates(string indexType)
        {
            return HistoricalIndexRates[indexType];
        }
    }
}
