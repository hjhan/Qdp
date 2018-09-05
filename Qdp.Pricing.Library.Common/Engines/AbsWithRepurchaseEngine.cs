using System.Collections.Generic;
using System.Linq;
using System.Security;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities;

namespace Qdp.Pricing.Library.Common.Engines
{
	public class AbsWithRepurchaseEngine : Engine<AbsWithRepurchase>
	{
		public override IPricingResult Calculate(AbsWithRepurchase trade, IMarketCondition market, PricingRequest request)
		{
			var result = new PricingResult(market.ValuationDate, request);

			if (result.IsRequested(PricingRequest.Cashflow))
			{
				var underlyingLoanCfs = trade.GetCashflows(market, false).ToArray();

				var surplus = 0.0;
				var repurchasedLoanCfs = new List<Cashflow[]> {underlyingLoanCfs};
				var underlyingLoanPayDates = repurchasedLoanCfs.SelectMany(x => x.Select(cf => cf.PaymentDate)).Distinct().ToArray();

				var payOutCashflows = trade.Tranches.SelectMany(x => x.GetCashflows(market, false)).ToArray();

				//assumption: repurchased loans have same features as the original loan excepth the startDate and mautirytDate
				for (var i = 0; i < underlyingLoanPayDates.Length-1; ++i)
				{
					//repurchase prior to the final cash flow
					var tCfs = repurchasedLoanCfs.SelectMany(x => x).Where(x => x.PaymentDate == underlyingLoanPayDates[i] && (x.CashflowType == CashflowType.Principal || x.CashflowType == CashflowType.Coupon || x.CashflowType == CashflowType.Tax)).ToArray();
					surplus += tCfs.Sum(x => x.PaymentAmount);
					var payOutAmount =payOutCashflows.Where(x => x.PaymentDate >= underlyingLoanPayDates[i] && x.PaymentDate < underlyingLoanPayDates[i + 1]).Sum(x => x.PaymentAmount);

					surplus -= payOutAmount;
					var endDate = trade.PayOutDates.Where(x => x > underlyingLoanPayDates[i]);
					if (endDate.Any())
					{
						repurchasedLoanCfs.Add(RepurchasedLoanCashflow(underlyingLoanPayDates[i+1], surplus*trade.RepurchaseRatio, trade.Loan, market, underlyingLoanPayDates[i], endDate.First()));
					}

					surplus *= (1.0 - trade.RepurchaseRatio);

				}

				var payOutCf =
					payOutCashflows.Select(
						x =>
							new Cashflow(x.AccrualEndDate, x.AccrualEndDate, x.PaymentDate, -x.PaymentAmount, x.PaymentCurrency,
								x.CashflowType, x.IsFixed, market.GetDf(x.PaymentDate), x.CalculationDetails, x.RefStartDate, x.RefEndDate, x.StartPrincipal, x.CouponRate));
				result.Cashflows = repurchasedLoanCfs.SelectMany(x => x).Union(payOutCf).ToArray();
			}

			return result;
		}

		private Cashflow[] RepurchasedLoanCashflow(
			Date repurchasedLoanFirstPaymentDate,
			double repurchasedLoanAmount,
			Loan loan,
			IMarketCondition market,
			Date startDate,
			Date endDate
			)
		{
			var newLoan = new Loan(startDate,
						endDate,
						repurchasedLoanFirstPaymentDate,
						repurchasedLoanAmount,
						loan.NumOfPayment,
						loan.DayCount,
						loan.Frequency,
						loan.Coupon,
						loan.ResetDate,
						loan.IsFloatingRate,
						loan.IndexType,
						loan.FloatingRateMultiplier,
						loan.AmortizationType,
						loan.Currency,
						loan.MortgageCalculator,
						loan.TaxRate);
			var cfs = new[] { new Cashflow(startDate, startDate, startDate, -repurchasedLoanAmount, CurrencyCode.CNY, CashflowType.Repurchase, true, market.GetDf(startDate), null) }
			.Union(newLoan.GetCashflows(market, false)).ToList();
			return cfs.ToArray();
		}
	}
}
