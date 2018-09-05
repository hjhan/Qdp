using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Fx
{
	class FxForward : IFxCashflowInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "FxForward"; } }
        public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public DayGap SettlmentGap { get; private set; }
		public double Notional { get; set; }
		public CurrencyCode DomCcy { get; private set; }
		public CurrencyCode FgnCcy { get; private set; }
		public CurrencyCode SettlementCcy { get; private set; }
		public ICalendar DomCalendar { get; private set; }
		public ICalendar FgnCalendar { get; private set; }
		public double NotionalInFgnCcy { get; private set; }
		public double StrikeFxRate { get; private set; } //quoted as FgnCcy/DomCcy
		public Date SpotDate { get; private set; }

		public FxForward(Date startDate,
			Date maturityDate,
			Date spotDate,
			DayGap dayGap,
			double notional,
			ICalendar domCalendar,
			CurrencyCode domCcy,
			double fgnNotional,
			ICalendar fgnCalendar,
			CurrencyCode fgnCcy,
			double strikeFxRate,
			CurrencyCode settlementCcy
			)
		{
			StartDate = startDate;
			UnderlyingMaturityDate = maturityDate;
			SpotDate = spotDate;
			SettlmentGap = dayGap ?? new DayGap("0BD");
			
			Notional = notional;
			DomCalendar = domCalendar;
			DomCcy = domCcy;

			NotionalInFgnCcy = fgnNotional;
			FgnCalendar = fgnCalendar;
			FgnCcy = fgnCcy;

			StrikeFxRate = strikeFxRate;
			SettlementCcy = settlementCcy;
		}
		public Cashflow[] GetCashflows(IMarketCondition market, bool netted = true)
		{
			return new[]
			{
				new Cashflow(StartDate, UnderlyingMaturityDate, UnderlyingMaturityDate, -NotionalInFgnCcy*StrikeFxRate, DomCcy, CashflowType.Gross, true, market.DiscountCurve.Value.GetDf(UnderlyingMaturityDate), null),
				new Cashflow(StartDate, UnderlyingMaturityDate,UnderlyingMaturityDate, NotionalInFgnCcy, FgnCcy, CashflowType.Gross, true, market.DiscountCurve.Value.GetDf(UnderlyingMaturityDate), null),
			};
		}

		public double GetAccruedInterest(Date calcDate, IMarketCondition market, bool isEod = true)
		{
			throw new PricingBaseException("GetAccruedInterest shall not called in Fx cash flow products");
		}

	}
}
