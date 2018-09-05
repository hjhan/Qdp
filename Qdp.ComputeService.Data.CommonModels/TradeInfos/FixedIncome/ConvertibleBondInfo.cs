using System;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity;
using Qdp.ComputeService.Data.CommonModels.ValuationParams;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.Equity;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
    [DataContract]
    [Serializable]
    [KnownType(typeof(FixedRateBondInfo))]
    [KnownType(typeof(FloatingRateBondInfo))]
    public class ConvertibleBondInfo : TradeInfoBase
    {
        public ConvertibleBondInfo()
            : this(Guid.NewGuid().ToString())
        {
            
        }

        public ConvertibleBondInfo(string tradeId,
            double tradeprice = double.NaN,
            string counterparty = null,
            string tradeDate = null,
            string tradeTime = null)
            : base(tradeId, "ConvertibleBond", tradeprice, counterparty, tradeDate, tradeTime)
        {
        }

        [DataMember]
        public BondInfoBase BondPart { get; set; }

        [DataMember]
        public VanillaOptionInfo ConversionOption { get; set; }

        [DataMember]
        public VanillaOptionInfo[] EmbeddedOptions { get; set; }

        [DataMember]
        public string[] EboStrikeQuoteTypes { get; set; }
        
        [DataMember]
        public OptionValuationParameters ValuationParameters { get; set; }

        public override ValuationParameters GetValuationParamters()
        {
            return ValuationParameters;
        }

        [DataMember]
        public bool TreatAsCommonBond { get; set; }

        #region Upgrade

        //protected override IDayCount Day_Count
        //{
        //    get
        //    {
        //        return new Act365();
        //    }
        //}
        protected override string Maturity_Date
        {
            get
            {
                return BondPart.MaturityDate;
            }
        }
        #endregion
    }
}
