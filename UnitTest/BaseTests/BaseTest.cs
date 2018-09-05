using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.MktCalibrationInstruments;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Foundation.ConfigFileReaders;
using Qdp.Foundation.Implementations;
using Qdp.Foundation.Serializer;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Implementations;

namespace UnitTest.BaseTests
{
    [TestClass]
    public class BaseTest
    {
        [TestMethod]
        public void TestDayGap()
        {
            var x = new DayGap("+2BD");
            Assert.AreEqual(x.Offset, 2);
            Assert.AreEqual(x.IsBizDay, true);
            Assert.AreEqual(x.Period, Period.Day);

            var y = new DayGap("+2D");
            Assert.AreEqual(y.Offset, 2);
            Assert.AreEqual(y.IsBizDay, false);
            Assert.AreEqual(y.Period, Period.Day);
        }

        [TestMethod]
        public void TestJsonSerializerFormat()
        {
            var x = new[]
            {
                new MktDepositJson
                {
                    IndexType = "Fr007",
                    DepositInfo = new DepositInfo
                    {
                        StartDate = "2015-02-02"
                    }
                },
                new MktDepositJson
                {
                    IndexType = "Fr001",
                    DepositInfo = new DepositInfo
                    {
                        StartDate = "2015-02-22"
                    }
                },
            };

            var strX = DataContractJsonObjectSerializer.Serialize(x);
            Console.WriteLine(strX);

            var configReader = new ConfigFileTextReader("Configurations", "MarketInstrumentConventions", "Deposit.cfg");
            var depositRule2 = DataContractJsonObjectSerializer.Deserialize<MktDepositJson[]>(configReader.ReadAllText())
                .ToDictionary(item => item.IndexType, item => item);
            Console.WriteLine(depositRule2.Count);
            Assert.AreEqual(depositRule2.Count, 9);
        }

        [TestMethod]
        public void TestScheduleEom()
        {
            var schedule = new Schedule(new Date(2016, 04, 30), new Date(2016, 12, 31), new Term("1M"), Stub.ShortEnd, CalendarImpl.Get("chn"), BusinessDayConvention.None, true).ToArray();
            Assert.AreEqual(schedule[0], new Date(2016, 04, 30));
            Assert.AreEqual(schedule[1], new Date(2016, 05, 31));
            Assert.AreEqual(schedule[2], new Date(2016, 06, 30));
            Assert.AreEqual(schedule[3], new Date(2016, 07, 31));
            Assert.AreEqual(schedule[4], new Date(2016, 08, 31));
            Assert.AreEqual(schedule[5], new Date(2016, 09, 30));
            Assert.AreEqual(schedule[6], new Date(2016, 10, 31));
            Assert.AreEqual(schedule[7], new Date(2016, 11, 30));
            Assert.AreEqual(schedule[8], new Date(2016, 12, 31));
        }

        //[TestMethod]
        //public void BondDataTest()  //Fails
        //{
        //    var text = File.ReadAllText("D:\\SourceCode\\QT\\QuantumTunneling\\data\\bond.json");
        //    var bonds = DataContractJsonObjectSerializer.Deserialize<BondInfoBase[]>(text);
        //    var today = DateTime.Now;

        //    var convertedBonds = new List<BondInfoBase>();
        //    foreach (var bond in bonds)
        //    {
        //        if (bond.MaturityDate == null)
        //        {
        //            continue;
        //        }

        //        var maturity = DateTime.ParseExact(bond.MaturityDate, "yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture);
        //        if (maturity < today)
        //        {
        //            continue;
        //        }

        //        // convert yyyyMMdd to yyyy-MM-dd
        //        bond.StartDate = DateTime.ParseExact(bond.StartDate, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture).ToString("yyyy-MM-dd");

        //        convertedBonds.Add(bond);
        //    }
            
        //    var convertedText = DataContractJsonObjectSerializer.Serialize<BondInfoBase[]>(convertedBonds.ToArray());
        //    using (var outputStream = new StreamWriter(@"D:\\SourceCode\\QT\\QuantumTunneling\\data\\new_bond.json"))
        //    {
        //        outputStream.Write(convertedText);
        //    }
        //}

        //[TestMethod]
        //public void TempTest()   //Fails
        //{
        //    var text = File.ReadAllText("E:\\Doc\\Strategy\\Excel Tools\\bond.json");
        //    var bonds = DataContractJsonObjectSerializer.Deserialize<BondInfoBase[]>(text);
        //    var today = DateTime.Now;
        //    using (var outputStream = new StreamWriter(@"E:\\Doc\\Strategy\\Excel Tools\\bond.csv"))
        //    {
        //        foreach (var bond in bonds)
        //        {
        //            if (bond.MaturityDate == null)
        //            {
        //                continue;
        //            }

        //            var maturity = DateTime.ParseExact(bond.MaturityDate, "yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture);
        //            if (maturity < today)
        //            {
        //                continue;
        //            }

        //            if (bond.InstrumentType == "FixedRateBond")
        //            {
        //                FixedRateBondInfo fixedRateBond = bond as FixedRateBondInfo;
        //                var line = string.Format("{0},,{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34}",
        //                    bond.BondId,
        //                    100,
        //                    DateTime.ParseExact(bond.StartDate, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture).ToString("yyyy-MM-dd"),
        //                    bond.MaturityDate,
        //                    fixedRateBond.FixedCoupon,
        //                    bond.Calendar,
        //                    bond.PaymentFreq,
        //                    bond.StickToEom ? 1 : 0,
        //                    bond.PaymentStub,
        //                    bond.Notional,
        //                    bond.Currency,
        //                    bond.AccrualDC,
        //                    bond.DayCount,
        //                    bond.AccrualBD,
        //                    bond.Settlement,
        //                    bond.SettlementCoupon,
        //                    bond.TradingMarket,
        //                    bond.PaymentBD,
        //                    bond.IsZeroCouponBond ? 1 : 0,
        //                    bond.IssuePrice,
        //                    bond.FirstPaymentDate,
        //                    bond.Amoritzation == null ? "" : string.Join(";", bond.Amoritzation.Select(x => x.Key.ToString() + ":" + x.Value.ToString()).ToArray()),
        //                    bond.AmortizationType,
        //                    bond.AmoritzationInDate == null ? "" : string.Join(";", bond.AmoritzationInDate.Select(x => x.Key.ToString() + ":" + x.Value.ToString()).ToArray()),
        //                    bond.AmoritzationInIndex == null ? "" : string.Join(";", bond.AmoritzationInIndex.Select(x => x.Key.ToString() + ":" + x.Value.ToString()).ToArray()),
        //                    bond.RenormAmortization ? 1 : 0,
        //                    bond.CompensationRate == null ? "" : string.Join(";", bond.CompensationRate.Select(x => x.Key.ToString() + ":" + x.Value.ToString()).ToArray()),
        //                    bond.IssueRate,
        //                    string.Format("DiscountCurveName:{0};FixingCurveName:{1};RiskfreeCurveName:{2}", bond.ValuationParamters.DiscountCurveName, bond.ValuationParamters.FixingCurveName, bond.ValuationParamters.RiskfreeCurveName),
        //                    bond.OptionToCall == null ? "" : string.Join(";", bond.OptionToCall.Select(x => x.Key + ":" + x.Value.ToString()).ToArray()),
        //                    bond.OptionToPut == null ? "" : string.Join(";", bond.OptionToPut.Select(x => x.Key + ":" + x.Value.ToString()).ToArray()),
        //                    bond.OptionToAssPut == null ? "" : string.Join(";", bond.OptionToAssPut.Select(x => x.Key + ":" + x.Value.ToString()).ToArray()),
        //                    bond.RoundCleanPrice ? 1 : 0,
        //                    bond.InstrumentType,
        //                    bond.Counterparty
        //                    );
        //                outputStream.WriteLine(line);
        //            }
        //            else
        //            {
        //                var line = string.Format("{0},,{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34}",
        //                    bond.BondId,
        //                    100,
        //                    DateTime.ParseExact(bond.StartDate, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture).ToString("yyyy-MM-dd"),
        //                    bond.MaturityDate,
        //                    0.0,
        //                    bond.Calendar,
        //                    bond.PaymentFreq,
        //                    bond.StickToEom ? 1 : 0,
        //                    bond.PaymentStub,
        //                    bond.Notional,
        //                    bond.Currency,
        //                    bond.AccrualDC,
        //                    bond.DayCount,
        //                    bond.AccrualBD,
        //                    bond.Settlement,
        //                    bond.SettlementCoupon,
        //                    bond.TradingMarket,
        //                    bond.PaymentBD,
        //                    bond.IsZeroCouponBond ? 1 : 0,
        //                    bond.IssuePrice,
        //                    bond.FirstPaymentDate,
        //                    bond.Amoritzation == null ? "" : string.Join(";", bond.Amoritzation.Select(x => x.Key.ToString() + ":" + x.Value.ToString()).ToArray()),
        //                    bond.AmortizationType,
        //                    bond.AmoritzationInDate == null ? "" : string.Join(";", bond.AmoritzationInDate.Select(x => x.Key.ToString() + ":" + x.Value.ToString()).ToArray()),
        //                    bond.AmoritzationInIndex == null ? "" : string.Join(";", bond.AmoritzationInIndex.Select(x => x.Key.ToString() + ":" + x.Value.ToString()).ToArray()),
        //                    bond.RenormAmortization ? 1 : 0,
        //                    bond.CompensationRate == null ? "" : string.Join(";", bond.CompensationRate.Select(x => x.Key.ToString() + ":" + x.Value.ToString()).ToArray()),
        //                    bond.IssueRate,
        //                    string.Format("DiscountCurveName:{0};FixingCurveName:{1};RiskfreeCurveName:{2}", bond.ValuationParamters.DiscountCurveName, bond.ValuationParamters.FixingCurveName, bond.ValuationParamters.RiskfreeCurveName),
        //                    bond.OptionToCall == null ? "" : string.Join(";", bond.OptionToCall.Select(x => x.Key + ":" + x.Value.ToString()).ToArray()),
        //                    bond.OptionToPut == null ? "" : string.Join(";", bond.OptionToPut.Select(x => x.Key + ":" + x.Value.ToString()).ToArray()),
        //                    bond.OptionToAssPut == null ? "" : string.Join(";", bond.OptionToAssPut.Select(x => x.Key + ":" + x.Value.ToString()).ToArray()),
        //                    bond.RoundCleanPrice ? 1 : 0,
        //                    bond.InstrumentType,
        //                    bond.Counterparty
        //                    );
        //                outputStream.WriteLine(line);
        //            }
        //        }
        //    }
        //}
    }
}
