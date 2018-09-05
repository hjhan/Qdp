using System;
using System.Runtime.Serialization;


namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.Commodity
{
    [DataContract]
    [Serializable]
    public class CommodityForwardCNYInfo : TradeInfoBase
    {
        public CommodityForwardCNYInfo(string tradeId,
             string underlyingTicker,
             double notional,
             double basis,
             string startDate,       
             string maturityDate,
             string payoffCurrency = "CNY",
             string settlementCurrency = "CNY",
             string settlement = "Cash",
             double tradeprice = double.NaN,
             string counterparty = null,
             string tradeDate = null,
             string tradeTime = null
             ) : base(tradeId, "CommodityForward", tradeprice, counterparty, tradeDate, tradeTime)
        {
            UnderlyingTicker = underlyingTicker;
            Notional = notional;
            Basis = basis;
            StartDate = startDate;
            MaturityDate = maturityDate;

            PayoffCurrency = payoffCurrency;
            SettlementCurrency = settlementCurrency;
            Settlement = settlement;
        }

        [DataMember]
        public string UnderlyingTicker { get; set; }

        [DataMember]
        public double Notional { get; set; }

        [DataMember]
        public string StartDate { get; set; }

        [DataMember]
        public string MaturityDate { get; set; }


        [DataMember]
        public string PayoffCurrency { get; set; }

        [DataMember]
        public string SettlementCurrency { get; set; }

        [DataMember]
        public string Settlement { get; set; }
        [DataMember]
        public double Basis  { get; set; }
    }
}
