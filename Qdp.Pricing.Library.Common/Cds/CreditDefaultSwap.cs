using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Common.Engines.Cds;

namespace Qdp.Pricing.Library.Common.Cds
{
	public class CreditDefaultSwap : ICalibrationSupportedInstrument, ICashflowInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "CreditDefaultSwap"; } }
        public SwapLeg PremiumLeg { get; private set; }
		public CdsProtectionLeg ProtectionLeg { get; private set; }
		public Date StartDate { get { return PremiumLeg.StartDate; } }
		public Date UnderlyingMaturityDate { get { return PremiumLeg.UnderlyingMaturityDate; } }
		public DayGap SettlmentGap { get { return PremiumLeg.SettlmentGap; } }
		public double Notional { get { return PremiumLeg.Notional; } set { PremiumLeg.Notional = value; } }
		public SwapDirection SwapDirection { get; private set; }
		public string Tenor { get; private set; }
		public int PremiumLegIntegrationIntervals { get; private set; }
		public CreditDefaultSwap(SwapLeg premiumLeg,
			CdsProtectionLeg protectionLeg,
			SwapDirection swapDirection,
			string tenor = null,
			int premiumLegIntegrationIntervals = 60)
		{
			PremiumLeg = premiumLeg;
			ProtectionLeg = protectionLeg;
			SwapDirection = swapDirection;
			Tenor = tenor ?? new Term(UnderlyingMaturityDate - StartDate, Period.Day).ToString();
			PremiumLegIntegrationIntervals = premiumLegIntegrationIntervals;
		}

		public Date GetCalibrationDate()
		{
			return UnderlyingMaturityDate;
		}

		public ICalibrationSupportedInstrument Bump(int bp)
		{
			return this;
		}

		public ICalibrationSupportedInstrument Bump(double resetRate)
		{
			return this;
		}

		public double ModelValue(IMarketCondition market, MktInstrumentCalibMethod calibMethod = MktInstrumentCalibMethod.Default)
		{
			var engine = new CdsEngine(PremiumLegIntegrationIntervals);
			return engine.ParSpread(this, market);
		}

		public Cashflow[] GetCashflows(IMarketCondition market, bool netted = true)
		{
			return PremiumLeg.GetCashflows(market, netted)
				.Select(x => new Cashflow(x.AccrualStartDate, x.AccrualEndDate, x.PaymentDate, x.PaymentAmount * SwapDirection.Sign(), x.PaymentCurrency, x.CashflowType, x.IsFixed, market.GetDf(x.PaymentDate), x.CalculationDetails))
				.ToArray();
		}

		public double GetAccruedInterest(Date calcDate, IMarketCondition market, bool isEod = true)
		{
			return PremiumLeg.GetAccruedInterest(calcDate, market, isEod);
		}
	}
}
