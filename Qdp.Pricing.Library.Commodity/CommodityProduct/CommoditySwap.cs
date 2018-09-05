using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Interfaces;
using System.Collections.Generic;

namespace Qdp.Pricing.Library.Commodity.CommodityProduct
{
    public class CommoditySwap : IInstrument
    {
        public string Id { get; private set; }
        public string RecTicker { get; set; }
        public string PayTicker { get; set; }
        public string FxTicker { get; set; }
        public virtual string TypeName { get { return "CommoditySwap"; } }
        public Date StartDate { get; private set; }
        public Date MaturityDate { get; private set; }
        public double RecNotional { get; set; }
        public double PayNotional { get; set; }
        public double FxNotional { get; set; }
        public CurrencyCode RecCcy { get; set; }
        public CurrencyCode PayCcy { get; set; }
        public double Notional { get; set; }

        public Date UnderlyingMaturityDate { get; private set; }
        public DayGap SettlmentGap { get; private set; }

        public CommoditySwap(Date startDate,
            Date maturityDate,
            string recTicker,
            string payTicker,
            string fxTicker,
            double recNotional,
            double payNotional,
            double fxNotional,
            CurrencyCode recCcy,
            CurrencyCode payCcy,
            DayGap settlementGap = null,
            Date underlyingMaturityDate = null
            )
        {
            StartDate = startDate;
            MaturityDate = maturityDate;
            RecTicker = recTicker;
            PayTicker = payTicker;
            FxTicker = fxTicker;
            RecNotional = recNotional;
            PayNotional = payNotional;
            FxNotional = fxNotional;
            RecCcy = recCcy;
            PayCcy = payCcy;
            SettlmentGap = settlementGap;
            UnderlyingMaturityDate = underlyingMaturityDate;
        }
    }
}
