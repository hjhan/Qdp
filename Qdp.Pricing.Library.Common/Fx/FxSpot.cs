using System.Runtime.InteropServices;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities;

namespace Qdp.Pricing.Library.Common.Fx
{
	public class FxSpot : IFxCashflowInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "FxSpot"; } }
        public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public double FxRate { get; private set; } //quoted as DomCcy/FgnCcy
		public CurrencyCode DomCcy { get; private set; }
		public ICalendar DomCalendar { get; private set; }
		public CurrencyCode FgnCcy { get; private set; }
		public ICalendar FgnCalendar { get; private set; }
		public double NotionalInDomCcy { get { return Notional; } }
		public double NotionalInFgnCcy { get; private set; }
		public CurrencyCode SettlementCcy { get; private set; }
		public DayGap SettlmentGap { get; private set; }
		public double Notional { get; set; }

		public Date SpotDate
		{
			get { return UnderlyingMaturityDate; }
		}

		public FxSpot(Date startDate,
			Date maturityDate,
			double fxRate,
			ICalendar domCalendar,
			CurrencyCode domCcy,
			double notionalInDomCcy,
			ICalendar fgnCalendar,
			CurrencyCode fgnCcy, 
			double notionalInFgnCcy,
			CurrencyCode settlementCcy
			)
		{
			StartDate = startDate;
			UnderlyingMaturityDate = maturityDate;
			FxRate = fxRate;

			DomCalendar = domCalendar;
			DomCcy = domCcy;
			Notional = notionalInDomCcy;

			FgnCalendar = fgnCalendar;
			FgnCcy = fgnCcy;
			NotionalInFgnCcy = notionalInFgnCcy;

			SettlementCcy = settlementCcy;
			SettlmentGap = new DayGap("0BD");

			if (!notionalInDomCcy.AlmostEqual(notionalInFgnCcy/fxRate))
			{
				throw new PricingBaseException("Fx spot's domCcyAmt, fgnCcyAmt, and Fx spot rate does not match!");
			}
		}

		public Cashflow[] GetCashflows(IMarketCondition market, bool netted = true)
		{
			return new[]
			{
				new Cashflow(StartDate, UnderlyingMaturityDate, UnderlyingMaturityDate, -NotionalInFgnCcy/FxRate, DomCcy, CashflowType.Gross, true, market.GetDf(UnderlyingMaturityDate),null),
				new Cashflow(StartDate, UnderlyingMaturityDate, UnderlyingMaturityDate, NotionalInFgnCcy, FgnCcy, CashflowType.Gross, true, market.GetDf(UnderlyingMaturityDate), null),
			};
		}

		public double GetAccruedInterest(Date calcDate, IMarketCondition market, bool isEod = true)
		{
			throw new PricingBaseException("GetAccruedInterest shall not called in Fx spot products");
		}
	}
}
