using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.Utilities;

namespace Qdp.Pricing.Library.Common.Fx
{
	public class CrossCurrencySwap : IFxCashflowInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "CrossCurrencySwap"; } }
        public Date StartDate
		{
			get { return DomCcyLeg.StartDate; }
		}

		public Date UnderlyingMaturityDate
		{
			get { return DomCcyLeg.UnderlyingMaturityDate; }
		}

		public DayGap SettlmentGap
		{
			get { return DomCcyLeg.SettlmentGap; }
		}

		public double Notional
		{
			get { return DomCcyLeg.Notional; }
            set { DomCcyLeg.Notional = value; }
		}

		public CurrencyCode DomCcy
		{
			get { return DomCcyLeg.Currency; }
		}

		public CurrencyCode FgnCcy
		{
			get { return FgnCcyLeg.Currency; }
		}

		public CurrencyCode SettlementCcy { get; private set; }

		public ICalendar DomCalendar
		{
			get { return DomCcyLeg.Calendar; }
		}

		public ICalendar FgnCalendar
		{
			get { return FgnCcyLeg.Calendar; }
		}
		public double NotionalInFgnCcy {
			get { return FgnCcyLeg.Notional; }
		}

		public Date SpotDate
		{
			get { return DomCcyLeg.StartDate; }
		}

		public SwapLeg DomCcyLeg { get; private set; }
		public SwapLeg FgnCcyLeg { get; private set; }

		public CrossCurrencySwap(SwapLeg domCcyLeg, SwapLeg fgnCcyLeg)
		{
			DomCcyLeg = domCcyLeg;
			FgnCcyLeg = fgnCcyLeg;
		}

		public Cashflow[] GetCashflows(IMarketCondition market, bool netted = true)
		{
			return DomCcyLeg.GetCashflows(market, netted)
				.Union(FgnCcyLeg.GetCashflows(
					market.UpdateCondition(
						new UpdateMktConditionPack<IYieldCurve>(x => x.DiscountCurve, market.FgnDiscountCurve.Value),
						new UpdateMktConditionPack<IYieldCurve>(x => x.FixingCurve, market.FgnFixingCurve.Value)
						),
					netted)
				).ToArray();
		}

		public double GetAccruedInterest(Date calcDate, IMarketCondition market, bool isEod = true)
		{
			throw new PricingBaseException("GetAccruedInterest shall not called in Fx cash flow products");
		}
	}
}
