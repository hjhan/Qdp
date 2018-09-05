using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;
using Qdp.Foundation.TableWithHeader;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Implementations;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
    [DataContract]
    [Serializable]
    [KnownType(typeof(FixedRateBondInfo))]
    [KnownType(typeof(FloatingRateBondInfo))]
    [KnownType(typeof(FixedDateCouonAdjustedBondInfo))]
    [KnownType(typeof(HoldingPeriodInfo))]
    public class BondInfoBase : TradeInfoBase
    {
        protected BondInfoBase(string tradeId,
            string instrumentType,
            string bondId = null,
            double tradeprice = double.NaN,
            string counterparty = null,
            string tradeDate = null,
            string tradeTime = null)
            : base(tradeId, instrumentType, tradeprice, counterparty, tradeDate, tradeTime)
        {
            BondId = bondId ?? tradeId;
        }

        protected BondInfoBase()
        {
        }

        [DataMember]
        [Column(0)]
        public string BondId { get; set; }

        [DataMember]
        [Column(1)]
        public string StartDate { get; set; }

        [DataMember]
        [Column(2)]
        public string MaturityDate { get; set; }

        [DataMember]
        [Column(3)]
        public string Calendar { get; set; }

        [DataMember, DefaultValue("SemiAnnual")]
        [Column(4)]
        public string PaymentFreq { get; set; }

        [DataMember, DefaultValue(false)]
        [Column(5)]
        public bool StickToEom { get; set; }

        [DataMember, DefaultValue("LongEnd")]
        [Column(6)]
        public string PaymentStub { get; set; }

        [DataMember, DefaultValue(100.0)]
        [Column(7)]
        public double Notional { get; set; }

        [DataMember, DefaultValue("CNY")]
        [Column(8)]
        public string Currency { get; set; }
        [DataMember, DefaultValue("")]
        [Column(9)]
        public string AccrualDC { get; set; }
        [DataMember, DefaultValue("Act365")]
        [Column(10)]
        public string DayCount { get; set; }

        [DataMember, DefaultValue("None")]
        [Column(11)]
        public string AccrualBD { get; set; }
        [DataMember, DefaultValue("ModifiedFollowing")]
        public string PaymentBD { get; set; }

        [DataMember(Name = "Settlment"), DefaultValue("+0D")]
        [Column(12)]
        public string Settlement { get; set; }
        [DataMember, DefaultValue(double.NaN)]
        [Column(13)]
        public double SettlementCoupon { get; set; }
        [DataMember]
        [Column(14)]
        public string TradingMarket { get; set; }

        [DataMember]
        public bool IsZeroCouponBond { get; set; }

        [DataMember]
        public double IssuePrice { get; set; }

        [DataMember]
        public string FirstPaymentDate { get; set; }

        [DataMember]
        public Dictionary<string, double> Amoritzation { get; set; }

        [DataMember]
        public string AmortizationType { get; set; }

        [DataMember]
        public Dictionary<string, double> AmoritzationInDate { get; set; }

        [DataMember]
        public Dictionary<int, double> AmoritzationInIndex { get; set; } //amortization by index of payment starting from 1 !!!!
        [DataMember]
        public bool RenormAmortization { get; set; }
        [DataMember]
        public Dictionary<int, double> CompensationRate { get; set; }
        [DataMember, DefaultValue(double.NaN)]
        public double IssueRate { get; set; }
        [DataMember]
        public SimpleCfValuationParameters ValuationParamters { get; set; }
        [DataMember]
        public Dictionary<string, double> OptionToCall { get; set; }

        [DataMember]
        public Dictionary<string, double> OptionToPut { get; set; }

        [DataMember]
        public Dictionary<string, double> OptionToAssPut { get; set; }

        [DataMember]
        public bool RoundCleanPrice { get; set; }

        [DataMember]
        public double RedemptionRate { get; set; }

        [DataMember]
        public bool RedemptionIncludeLastCoupon { get; set; }

        public override ValuationParameters GetValuationParamters()
        {
            return ValuationParamters;
        }

        public string AmoritzationInDateToLabelData()
        {
            return DictionaryToLableData(AmoritzationInDate);
        }

        public string AmoritzationInIndexToLabelData()
        {
            return DictionaryToLableData(AmoritzationInIndex);
        }

        public string CompensationRateToLabelData()
        {
            return DictionaryToLableData(CompensationRate);
        }

        public string OptionToCallToLabelData()
        {
            return DictionaryToLableData(OptionToCall);
        }

        public string OptionToPutToLabelData()
        {
            return DictionaryToLableData(OptionToPut);
        }

        public string OptionToAssPutToLabelData()
        {
            return DictionaryToLableData(OptionToAssPut);
        }

        public Dictionary<string, double> AmoritzationInDateConverter(string value)
        {
            return ConverterDictionaryStringDouble(value);
        }

        public Dictionary<int, double> AmoritzationInIndexConverter(string value)
        {
            return ConverterDictionaryIntDouble(value);
        }

        public Dictionary<int, double> CompensationRateConverter(string value)
        {
            return ConverterDictionaryIntDouble(value);
        }

        public Dictionary<string, double> OptionToCallConverter(string value)
        {
            return ConverterDictionaryStringDouble(value);
        }

        public Dictionary<string, double> OptionToPutConverter(string value)
        {
            return ConverterDictionaryStringDouble(value);
        }

        public Dictionary<string, double> OptionToAssPutConverter(string value)
        {
            return ConverterDictionaryStringDouble(value);
        }

        #region Upgrade
         
        protected override string Maturity_Date
        {
            get
            {
                return MaturityDate;
            }
        }
        #endregion
    }
}