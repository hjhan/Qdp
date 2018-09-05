using Qdp.ComputeService.Data.CommonModels.TradeInfos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qdp.Pricing.Ecosystem.Utilities
{
    public class PricingEcosystemException : Exception
    {
        public PricingEcosystemException()
            : base()
        {
        }

        public PricingEcosystemException(string msg)
            : base(msg)
        {
        }

        public PricingEcosystemException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }

        public PricingEcosystemException(string msg, TradeInfoBase tradeInfo)
            : base(msg)
        {
            TradeInfo = tradeInfo;
        }

        public TradeInfoBase TradeInfo { get; private set; }
    }
}
