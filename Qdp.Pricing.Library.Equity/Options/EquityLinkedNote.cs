using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Interfaces;

namespace Qdp.Pricing.Library.Equity.Options
{
	public class EquityLinkedNote<T> : IInstrument
	 where T : OptionBase
	{
        public string Id { get; private set; }
        public string TypeName { get { return "EquityLinkedNote"; } }
        public T Option { get; private set; }
		public double PrincipalProtectionRate { get; private set; }

		public EquityLinkedNote(T option,
			double principalProtectionRate)
		{
			Option = option;
			PrincipalProtectionRate = principalProtectionRate;
			StartDate = option.StartDate;
			UnderlyingMaturityDate = option.UnderlyingMaturityDate;
			SettlmentGap = option.SettlmentGap;
			Notional = Option.Notional;
		}

		public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public DayGap SettlmentGap { get; private set; }
		public double Notional { get; set; }
	}
}
