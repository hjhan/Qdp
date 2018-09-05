using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common
{
	public class AbsWithRepurchase : ICashflowInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "AbsWithRepurchase"; } }
        public Loan Loan { get; set; }
		public Bond[] Tranches { get; set; }
		public Date[] PayOutDates { get; set; }
		public double RepurchaseRatio { get; set; }
		public double MaintenanceFeeRate { get; set; }
		public string ProtectionFeeRate { get; set; }
		public AbsWithRepurchase(Loan loan, 
			double repurchaseRatio,
			Bond[] bonds
			)
		{
			Loan = loan;
			Tranches = bonds;
			RepurchaseRatio = repurchaseRatio;

			StartDate = Loan.StartDate;
			UnderlyingMaturityDate = Loan.UnderlyingMaturityDate;
			SettlmentGap = Loan.SettlmentGap;
			Notional = 1.0;

			PayOutDates = Tranches.SelectMany(x => x.PaymentSchedule).ToArray();
			//StartDate = Loan.Select(x => x.StartDate).Min();
			//MaturityDate = Loan.Select(x => x.MaturityDate).Min();
			//SettlmentGap = new DayGap("+0D");
			//Notional = Loan.Sum(x => x.Notional);
		}

		public Cashflow[] GetCashflows(IMarketCondition market, bool netted = true)
		{
			var cfs = Loan.GetCashflows(market, false);
			return cfs.ToArray();
		}

		public double GetAccruedInterest(Date calcDate, IMarketCondition market, bool isEod = true)
		{
			throw new PricingLibraryException("Accrued interest is N.A. for this product!");
		}

		public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public DayGap SettlmentGap { get; private set; }
		public double Notional { get; set; }
	}
}

