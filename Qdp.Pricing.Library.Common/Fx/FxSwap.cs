using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities;

namespace Qdp.Pricing.Library.Common.Fx
{
	class FxSwap : IFxCashflowInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "FxSwap"; } }
        public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public Date SpotDate { get; private set; }
		public Date NearStrikeDate { get; private set; }
		public DayGap SettlmentGap { get; private set; }
		public double Notional { get; set; }
		public CurrencyCode DomCcy { get; private set; }
		public CurrencyCode FgnCcy { get; private set; }
		public CurrencyCode SettlementCcy { get; private set; }
		public ICalendar DomCalendar { get; private set; }
		public ICalendar FgnCalendar { get; private set; }
		public double NotionalInFgnCcy { get; private set; }
		public double NearStrikeFxRate { get; private set; } //quoted as FgnCcy/DomCcy
		public double FarStrikeFxRate { get; private set; } //quoted as FgnCcy/DomCcy

		public FxSwap(Date startDate,
			Date maturityDate,
			Date spotDate,
			Date nearStrikeDate,
			DayGap dayGap,
			double notional,
			ICalendar domCalendar,
			CurrencyCode domCcy,
			double fgnNotional,
			ICalendar fgnCalendar,
			CurrencyCode fgnCcy,
			double nearStrikeFxRate,
			double farStrikeFxRate,
			CurrencyCode settlementCcy
			)
		{
			StartDate = startDate;
			UnderlyingMaturityDate = maturityDate;
			SpotDate = spotDate;
			NearStrikeDate = nearStrikeDate;
			SettlmentGap = dayGap ?? new DayGap("0BD");
			
			Notional = notional;
			DomCalendar = domCalendar;
			DomCcy = domCcy;

			NotionalInFgnCcy = fgnNotional;
			FgnCalendar = fgnCalendar;
			FgnCcy = fgnCcy;

			NearStrikeFxRate = nearStrikeFxRate;
			FarStrikeFxRate = farStrikeFxRate;

			SettlementCcy = settlementCcy;
		}

		public Cashflow[] GetCashflows(IMarketCondition market, bool netted = true)
		{
			//TODO 
			return new[]
			{
				new Cashflow(StartDate, UnderlyingMaturityDate, UnderlyingMaturityDate, -NotionalInFgnCcy*NearStrikeFxRate, DomCcy, CashflowType.Gross, true, market.GetDf(UnderlyingMaturityDate), null),
				new Cashflow(StartDate, UnderlyingMaturityDate, UnderlyingMaturityDate, NotionalInFgnCcy, FgnCcy, CashflowType.Gross, true, market.GetDf(UnderlyingMaturityDate), null),
			};
		}

		public double GetAccruedInterest(Date calcDate, IMarketCondition market, bool isEod = true)
		{
			throw new PricingBaseException("GetAccruedInterest shall not called in Fx cash flow products");
		}

	}
}
