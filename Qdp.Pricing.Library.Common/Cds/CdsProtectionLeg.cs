using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Interfaces;

namespace Qdp.Pricing.Library.Common.Cds
{
	public class CdsProtectionLeg : IInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "CdsProtectionLeg"; } }
        public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public DayGap SettlmentGap { get; private set; }
		public double Notional { get; set; }
		public double RecoveryRate { get; private set; }
		public CurrencyCode Currency { get; private set; }

		public CdsProtectionLeg(Date startDate,
			Date maturityDate,
			DayGap settlementGap,
			CurrencyCode currency,
			double notional,
			double recoveryRate
			)
		{
			StartDate = startDate;
			UnderlyingMaturityDate = maturityDate;
			SettlmentGap = settlementGap;
			Currency = currency;
			Notional = notional;
			RecoveryRate = recoveryRate;
		}
	}
}
