using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Base.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities.Coupons;

namespace Qdp.Pricing.Library.Common.Utilities
{
	public class SwapLeg : ICashflowInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "SwapLeg"; } }
        public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public double Notional { get; set; }
		public CurrencyCode Currency { get; private set; }
		public ICoupon Coupon { get; private set; }
		public ICalendar Calendar { get; private set; }
		public Frequency PaymentFreq { get; private set; }
		public Stub PaymentStub { get; private set; }
		public BusinessDayConvention PaymentBda { get; private set; }
		public IDayCount DayCount { get; private set; }
		public bool NotionalExchange { get; private set; }
		public DayGap SettlmentGap { get; private set; }
		public Date FirstPaymentDate { get; private set; }
		public Date TerminationDate { get; private set; }
		public double TerminationAmount { get; private set; }
		public Schedule Accruals { get; private set; }
		public Schedule PaymentSchedule { get; private set; }
		public SwapLeg(Date startDate,
			Date maturityDate,
			double notional,
			bool notionalExchange,
			CurrencyCode currency,
			ICoupon coupon,
			ICalendar calendar,
			Frequency paymentFreq,
			Stub paymentStub,
			IDayCount dayCount,
			BusinessDayConvention paymentBda,
			DayGap settlementGap = null,
			Date firstPaymenetDate = null,
			Date terminationDate = null,
			double terminateAmount = double.NaN
			)
		{
			StartDate = startDate;
			UnderlyingMaturityDate = maturityDate;
			Notional = notional;
			NotionalExchange = notionalExchange;
			Currency = currency;
			Coupon = coupon;
			Calendar = calendar;
			PaymentFreq = paymentFreq;
			PaymentStub = paymentStub;
			DayCount = dayCount;
			PaymentBda = paymentBda;
			SettlmentGap = settlementGap ?? new DayGap("+0BD");
			FirstPaymentDate = firstPaymenetDate;
			TerminationDate = terminationDate;
			TerminationAmount = terminateAmount;

			if (FirstPaymentDate != null)
			{
				var temp = new Schedule(FirstPaymentDate, UnderlyingMaturityDate, PaymentFreq.GetTerm(), PaymentStub, Calendar, PaymentBda);
				Accruals = new Schedule(new[] {StartDate}.Union(temp));
			}
			else
			{
				Accruals = new Schedule(StartDate, UnderlyingMaturityDate, PaymentFreq.GetTerm(), PaymentStub, Calendar, PaymentBda);
			}

			if (TerminationDate != null)
			{
				Accruals = new Schedule(Accruals.Where(x => x < TerminationDate));
			}

			PaymentSchedule = new Schedule(Accruals.Skip(1));
		}

		public Cashflow[] GetCashflows(IMarketCondition market, bool netted = true)
		{
			var accDates = Accruals.ToArray();
			var cashflowType = (Coupon is FixedCoupon) ? CashflowType.FixedLegInterest : CashflowType.FloatingLegInterest;
			var cashflows = new List<Cashflow>();
			for (var i = 0; i < accDates.Length - 1; ++i)
			{
				CfCalculationDetail[] temp;
				var paymentRate=Coupon.GetCoupon(accDates[i], accDates[i+1], market.FixingCurve.Value, market.HistoricalIndexRates, out temp) * DayCount.CalcDayCountFraction(accDates[i], accDates[i + 1]);
				cashflows.Add(
					new Cashflow(
						accDates[i],
						accDates[i+1],
						accDates[i+1],
						Notional *paymentRate,
						Currency,
						cashflowType,
						temp.Aggregate(true, (current, x) => current && x.IsFixed),
						market.DiscountCurve.Value.GetDf(accDates[i+1]),
						temp
						)
					);
			}

			if (NotionalExchange)
			{
				if (cashflows.Count >= 1)
				{
					var last = cashflows[cashflows.Count - 1];
					if (netted)
					{
						cashflows[cashflows.Count - 1] = new Cashflow(last.AccrualStartDate, last.AccrualEndDate, last.PaymentDate, last.PaymentAmount + Notional, Currency, CashflowType.Principal, last.IsFixed, market.DiscountCurve.Value.GetDf(last.PaymentDate), last.CalculationDetails);
					}
					else
					{
						cashflows.Add(new Cashflow(last.AccrualStartDate, last.AccrualEndDate, last.PaymentDate, Notional, Currency, CashflowType.Principal, true, market.DiscountCurve.Value.GetDf(last.PaymentDate), null));
					}
				}
			}

			if (TerminationDate != null && !double.IsNaN(TerminationAmount))
			{
				return cashflows.Where(x => x.PaymentDate < TerminationDate).ToArray()
					.Union(new[] { new Cashflow(TerminationDate, TerminationDate, TerminationDate, TerminationAmount, Currency, CashflowType.TerminationFee, true, market.DiscountCurve.Value.GetDf(TerminationDate), null) })
					.ToArray();
			}
			else
			{
				return cashflows.ToArray();
			}
		}

		public double GetAccruedInterest(Date calcDate, IMarketCondition market, bool isEod = true)
		{
			var accs = Accruals.ToArray();
			if (market.ValuationDate < accs[0] || market.ValuationDate > accs.Last())
			{
				return 0.0;
			}

			var cashflow = GetCashflows(market,false).ToArray();
			if (!cashflow.Any())
			{
				return 0.0;
			}

			var nextCashflow = cashflow.FirstIndexOf(x => x.PaymentDate >= calcDate);
			var startDate = nextCashflow == 0 ? accs[0] : cashflow[nextCashflow - 1].PaymentDate;
			var amount = cashflow[nextCashflow].PaymentAmount;
			var endDate = cashflow[nextCashflow].PaymentDate;
            //return (calcDate - startDate + (isEod ? 1 : 0)) / (endDate - startDate) * amount;
            return (calcDate - startDate ) / (endDate - startDate) * amount;
		}
		
		public Tuple<Date, double>[] GetPaymentRates(IMarketCondition market)
		{
			List<CfCalculationDetail[]> temp;
			var coupons = Coupon.GetCoupon(Accruals, market.FixingCurve.Value, market.HistoricalIndexRates, out temp);
			var accDates = Accruals.ToList();
			var ret = new List<Tuple<Date, double>> ();
			for (var i = 1; i < accDates.Count; ++i)
			{
				ret.Add(
					Tuple.Create(accDates[i], coupons[i - 1]*DayCount.CalcDayCountFraction(accDates[i - 1], accDates[i]))
				);
			}
			return ret.ToArray();
		}

		public SwapLeg Bump(int bp)
		{
			if (!(Coupon is FixedCoupon))
			{
				return this;
			}
			else
			{
				var newCoupon = new FixedCoupon((Coupon as FixedCoupon).FixedRate+bp*0.0001);
				return new SwapLeg(StartDate, UnderlyingMaturityDate, Notional, NotionalExchange, Currency, newCoupon, Calendar, PaymentFreq, PaymentStub, DayCount, PaymentBda, SettlmentGap, FirstPaymentDate, TerminationDate, TerminationAmount);
			}
		}

		internal SwapLeg Stretch(Date startDate, Date maturityDate)
		{
			return new SwapLeg(startDate, maturityDate, Notional, NotionalExchange, Currency, Coupon, Calendar, PaymentFreq, PaymentStub, DayCount, PaymentBda, SettlmentGap, FirstPaymentDate, TerminationDate, TerminationAmount);
		}
	}
}
