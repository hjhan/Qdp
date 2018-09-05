using Qdp.Pricing.Base.Enums;
using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.Commodity
{
    [DataContract]
    [Serializable]
    public class CommoditySwapInfo : TradeInfoBase
    {
        public CommoditySwapInfo(string tradeId,
             string recTicker,
             string payTicker,
             string fxTicker,
             double recNotional,
             double payNotional,
             double fxNotional,
             string startDate,
             string maturityDate,
             string recCcy,
             string payCcy,
             double tradeprice = double.NaN,
             string counterparty = null,
             string tradeDate = null,
             string tradeTime = null
             ) : base(tradeId, "CommoditySwap", tradeprice, counterparty, tradeDate, tradeTime)
        {
            RecTicker = recTicker;
            PayTicker = payTicker;
            FxTicker = fxTicker;
            RecNotional = recNotional;
            PayNotional = payNotional;
            FxNotional = fxNotional;
            StartDate = startDate;
            MaturityDate = maturityDate;
            RecCcy = recCcy;
            PayCcy = payCcy;
        }

        [DataMember]
        public string RecTicker { get; set; }
        [DataMember]
        public string PayTicker { get; set; }
        [DataMember]
        public string FxTicker { get; set; }
        [DataMember]
        public double RecNotional { get; set; }
        [DataMember]
        public double PayNotional { get; set; }
        [DataMember]
        public double FxNotional { get; set; }
        [DataMember]
        public string StartDate { get; set; }
        
        public string MaturityDate { get; set; }
        [DataMember]
        public string RecCcy { get; private set; }
        [DataMember]
        public string PayCcy { get; private set; }


    }
}
