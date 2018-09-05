using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Common.Utilities.Mortgage;

namespace Qdp.Pricing.Library.Common
{
	public class Loan : ICashflowInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "Loan"; } }
        public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public Date FirstPaymentDate { get; private set; }
		public double Notional { get; set; }
		public int NumOfPayment { get; private set; }
		public IDayCount DayCount { get; private set; }
		public Frequency Frequency { get; private set; }
		public double Coupon { get; private set; }
		public double TaxRate { get; private set; }
		public bool IsFloatingRate { get; private set; }
		public Date ResetDate { get; private set; }
		public IndexType IndexType { get; private set; }
		public CurrencyCode Currency { get; private set; }
		public AmortizationType AmortizationType { get; private set; }
		public Date[] Accruals { get; private set; }
		public double FloatingRateMultiplier { get; private set; }
		public IMortgageCalculator MortgageCalculator { get; private set; }
		private readonly Date _implicitAccStartDate;
		private readonly int _numPastPayment;
		public Loan(Date startDate,
			Date maturityDate,
			Date firstPaymentDate,
			double notional,
			int numOfPayment,
			IDayCount dayCount,
			Frequency frequency,
			double coupon,
			Date resetDate,
			bool isFloatingRate,
			IndexType indexType,
			double floatingRateMultiplier,
			AmortizationType amortizationType,
			CurrencyCode currency = CurrencyCode.CNY,
			IMortgageCalculator mortgageCalculator = null,
			double taxRate = 0.0
			)
		{
			StartDate = startDate;
			UnderlyingMaturityDate = maturityDate;
			FirstPaymentDate = firstPaymentDate;
			NumOfPayment = numOfPayment;
			Notional = notional;
			DayCount = dayCount;
			Frequency = frequency;
			Coupon = coupon;
			TaxRate = taxRate;
			ResetDate = resetDate;
			IsFloatingRate = isFloatingRate;
			IndexType = indexType;
			FloatingRateMultiplier = floatingRateMultiplier;
			Currency = currency;
			_implicitAccStartDate = Frequency.GetTerm().Prev(FirstPaymentDate);
			AmortizationType = amortizationType;
			MortgageCalculator = mortgageCalculator ?? new SimpleMortgageCalculator(new Psa(), new Sda());

			Accruals = new Schedule(_implicitAccStartDate, UnderlyingMaturityDate, Frequency.GetTerm(), Stub.ShortEnd).ToArray();
			_numPastPayment = NumOfPayment - (Accruals.Length - 1);
		}

		public Cashflow[] GetCashflows(IMarketCondition market, bool netted = true)
		{
			//var payTerm = Frequency.GetTerm();
			//var accDates = new List<Date> {StartDate};
			//accDates.AddRange(new Schedule(FirstPaymentDate, MaturityDate, payTerm, Stub.ShortEnd).ToArray())
			//var Accruals = new Schedule(_implicitAccStartDate, MaturityDate, payTerm, Stub.ShortEnd).ToArray();
			var len = Accruals.Length;
			var tArray = new double[len-1];
			for (var i = 0; i < len-1; ++i)
			{
				tArray[i] = DayCount.CalcDayCountFraction(Accruals[i], Accruals[i + 1]);
			}
			double[] begPrincipal;
			double[] interest;
			double[] principalPay;
			double[] prepayment;
			double[] defaultPrincipal;
			if (!IsFloatingRate)
			{

				MortgageCalculator.GetPaymentDetails(tArray, Notional, Coupon, Frequency, AmortizationType, out begPrincipal, out interest, out principalPay, out prepayment, out defaultPrincipal, _numPastPayment);
				var beg = Accruals[0];
				var end = Accruals[1];
				if (StartDate != beg)
				{
					//first interest should be adjusted
					interest[0] = interest[0] * (end - StartDate) / (end - beg);
				}
			}
			else
			{
				double[] begPrincipal1;
				double[] interest1;
				double[] principalPay1;
				double[] prepayment1;
				double[] defaultPrincipal1;
				MortgageCalculator.GetPaymentDetails(tArray, Notional, Coupon, Frequency, AmortizationType, out begPrincipal1, out interest1, out principalPay1, out prepayment1, out defaultPrincipal1, _numPastPayment);
			
				var ratePairs = market.HistoricalIndexRates.Value[IndexType].Select(x => Tuple.Create(x.Key, x.Value)).OrderBy(x => x.Item1).ToArray();
				int index;
				for (index = ratePairs.Length - 1; index > 0; --index)
				{
					if (ratePairs[index].Item1 <= ResetDate)
					{
						break;
					}
				}
				var newRate = ratePairs[index].Item2*FloatingRateMultiplier;

				double[] begPrincipal2;
				double[] interest2;
				double[] principalPay2;
				double[] prepayment2;
				double[] defaultPrincipal2;
				var n = Accruals.FirstIndexOf(x => x > ResetDate);
				var p = n < 2 ? Notional : begPrincipal1[n - 2];
				MortgageCalculator.GetPaymentDetails(tArray.Skip(n - 1).ToArray(), p, newRate, Frequency, AmortizationType, out begPrincipal2, out interest2, out principalPay2, out prepayment2, out defaultPrincipal2, _numPastPayment);

				//accDate[n-1] accDate[n]
				var start = Accruals[n - 1];
				var end = Accruals[n];
				
				var newInterst = p * (DayCount.CalcDayCountFraction(start, ResetDate)*Coupon + DayCount.CalcDayCountFraction(ResetDate, end)*newRate);
				interest1[n - 1] = newInterst;
				principalPay1[n - 1] = principalPay2[0];
				prepayment1[n - 1] = prepayment2[0];
				defaultPrincipal1[n - 1] = defaultPrincipal2[0];
				begPrincipal1[n - 1] = p - principalPay1[n - 1] - prepayment1[n - 1] - defaultPrincipal1[n - 1];

				double[] begPrincipal3;
				double[] interest3;
				double[] principalPay3;
				double[] prepayment3;
				double[] defaultPrincipal3;
				MortgageCalculator.GetPaymentDetails(tArray.Skip(n).ToArray(), begPrincipal1[n - 1], newRate, Frequency, AmortizationType, out begPrincipal3, out interest3, out principalPay3, out prepayment3, out defaultPrincipal3, _numPastPayment);

				var beg = Accruals[0];
				end = Accruals[1];
				if (StartDate != beg)
				{
					//first interest should be adjusted
					interest1[0] = interest1[0]*(end-StartDate)/(end - beg);
				}

				var tempList = begPrincipal1.Take(n).ToList();
				tempList.AddRange(begPrincipal3);
				begPrincipal = tempList.ToArray();

				tempList = interest1.Take(n).ToList();
				tempList.AddRange(interest3);
				interest = tempList.ToArray();

				tempList = principalPay1.Take(n).ToList();
				tempList.AddRange(principalPay3);
				principalPay = tempList.ToArray();

				tempList = prepayment1.Take(n).ToList();
				tempList.AddRange(prepayment3);
				prepayment = tempList.ToArray();

				tempList = defaultPrincipal1.Take(n).ToList();
				tempList.AddRange(defaultPrincipal3);
				defaultPrincipal = tempList.ToArray();
			}

			var cashflows = new List<Cashflow>();
			for (var i = 0; i < begPrincipal.Length; ++i)
			{
				var df = market.GetDf(Accruals[i + 1]);
				cashflows.Add(new Cashflow(Accruals[i], Accruals[i + 1], Accruals[i + 1], principalPay[i], Currency, CashflowType.Principal, true, df, null));
				cashflows.Add(new Cashflow(Accruals[i], Accruals[i + 1], Accruals[i + 1], interest[i], Currency, CashflowType.Coupon, true, df, null));
				cashflows.Add(new Cashflow(Accruals[i], Accruals[i + 1], Accruals[i + 1], -interest[i] * TaxRate, Currency, CashflowType.Tax, true, df, null));
				cashflows.Add(new Cashflow(Accruals[i], Accruals[i + 1], Accruals[i + 1], prepayment[i], Currency, CashflowType.Prepayment, true, df, null));
				cashflows.Add(new Cashflow(Accruals[i], Accruals[i + 1], Accruals[i + 1], defaultPrincipal[i], Currency, CashflowType.PrincipalLossOnDefault, true, df, null));
			}
			return cashflows.ToArray();
		}

		public double GetAccruedInterest(Date calcDate, IMarketCondition market, bool isEod = true)
		{
			throw new NotImplementedException();
		}

		public DayGap SettlmentGap { get; private set; }
	}
}
