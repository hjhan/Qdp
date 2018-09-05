using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities;

namespace Qdp.Pricing.Library.Common
{
	public class Deposit : ICashflowInstrument, ICalibrationSupportedInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "Deposit"; } }
        public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public double DepositRate { get; private set; }
		public IDayCount DayCount { get; private set; }
		public ICalendar Calendar { get; private set; }
		public BusinessDayConvention Bda { get; private set; }
		public double Notional { get; set; }
		public CurrencyCode Currency { get; private set; }
		public DayGap SettlmentGap { get; private set; }

		public Deposit(
			Date startDate,
			Date maturityDate,
			double depositRate,
			IDayCount dayCount,
			ICalendar calendar,
			BusinessDayConvention bda,
			CurrencyCode currency,
			double notional = 1.0,
			string tenor = null
			)
		{
			StartDate = startDate;
			
			DepositRate = depositRate;
			DayCount = dayCount;
			Calendar = calendar;
			Bda = bda;
			UnderlyingMaturityDate = Bda.Adjust(Calendar, maturityDate);
			Currency = currency;
			Notional = notional;

			Tenor = tenor ?? string.Format("{0},{1}", (int) (UnderlyingMaturityDate - StartDate), "D");
		}

		
		public Cashflow[] GetCashflows(IMarketCondition market, bool netted = true)
		{
			var coupon = Notional*DepositRate*DayCount.CalcDayCountFraction(StartDate, UnderlyingMaturityDate);

			if (netted)
			{
				return new[] { new Cashflow(StartDate, UnderlyingMaturityDate, UnderlyingMaturityDate, coupon + Notional, Currency, CashflowType.Principal, true, market.GetDf(UnderlyingMaturityDate), null) };
			}
			else
			{
				return new[]
				{
					new Cashflow(StartDate, UnderlyingMaturityDate, UnderlyingMaturityDate, coupon, Currency, CashflowType.Coupon, true, market.GetDf(UnderlyingMaturityDate), null),
					new Cashflow(StartDate, UnderlyingMaturityDate,UnderlyingMaturityDate, Notional, Currency, CashflowType.Principal, true, market.GetDf(UnderlyingMaturityDate), null)
				};
			}
		}

		public double GetAccruedInterest(Date calcDate, IMarketCondition market, bool isEod = true)
		{
			var totalCoupon = DepositRate*DayCount.CalcDayCountFraction(StartDate, UnderlyingMaturityDate)*Notional;
			return totalCoupon*(calcDate - StartDate + (isEod ? 1 : 0))/(UnderlyingMaturityDate - StartDate);
		}

		public string Tenor { get; private set; }
		public Date GetCalibrationDate()
		{
			return Bda.Adjust(Calendar, UnderlyingMaturityDate);
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
			return (1.0/market.DiscountCurve.Value.GetDf(market.ValuationDate, UnderlyingMaturityDate) - 1.0)/
			       DayCount.CalcDayCountFraction(market.ValuationDate, UnderlyingMaturityDate);
		}
	}
}
