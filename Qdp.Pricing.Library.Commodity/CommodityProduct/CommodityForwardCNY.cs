using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Interfaces;


namespace Qdp.Pricing.Library.Commodity.CommodityProduct
{
    public class CommodityForwardCNY : IInstrument
    {
        public string Id { get; private set; }
        public string UnderlyingTicker { get; set; }
        public virtual string TypeName { get { return "CommodityForward"; } }
        public Date StartDate { get; private set; }
        public Date MaturityDate { get; private set; }
        public double Notional { get; set; }
        public double Basis { get; set; }
        public CurrencyCode PayoffCcy { get; private set; } // curency unit of payoff calculation
        public CurrencyCode SettlementCcy { get; private set; } // currency unit of settlement      
        public Date UnderlyingMaturityDate { get; private set; } 
        public DayGap SettlmentGap { get; private set; } 

        public CommodityForwardCNY(Date startDate,
            Date maturityDate,  
            double notional,
            double basis,
            CurrencyCode payoffCcy,
            CurrencyCode settlementCcy,
            DayGap settlementGap = null,
            Date underlyingMaturityDate = null
            )
		{
            StartDate = startDate;
            MaturityDate = maturityDate;
            Notional = notional;
            Basis = basis;
            PayoffCcy = payoffCcy;
            SettlementCcy = settlementCcy;
            SettlmentGap = settlementGap;
            UnderlyingMaturityDate = underlyingMaturityDate;
        }
    }
}
