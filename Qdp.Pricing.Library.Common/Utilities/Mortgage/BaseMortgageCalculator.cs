using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities.Mortgage
{
	public abstract class BaseMortgageCalculator : IMortgageCalculator
	{
		public IMortgagePrepayment PrepaymentModel { get; set; }
		public  IMortgageDefault DefaultModel { get; set; }

		protected BaseMortgageCalculator(IMortgagePrepayment prepaymentmodel,
			IMortgageDefault defaultModel)
		{
			PrepaymentModel = prepaymentmodel;
			DefaultModel = defaultModel;
		}

		public void GetPaymentDetails(double[] tArray,
			double principal,
			double rate,
			Frequency frequency,
			AmortizationType amortizationType,
			out double[] begPrincipal,
			out double[] interest,
			out double[] scheduledPrincipalPay,
			out double[] prepayment,
			out double[] defaultPrincipal,
			int numPastPayment
			)
		{
			var length = tArray.Length;

			begPrincipal = new double[length];
			interest = new double[length];
			scheduledPrincipalPay = new double[length];
			prepayment = new double[length];
			defaultPrincipal = new double[length];

			var remainingPrincipal = principal;

			var c = amortizationType == AmortizationType.EqualPrincipal ? remainingPrincipal / (length) : GetLevelPayment(tArray.ToArray(), remainingPrincipal, rate, frequency);
			var recalcC = PrepaymentModel.NeedRecalc() || DefaultModel.NeedRecalc();

			for (var i = 0; i < tArray.Length; ++i)
			{
				if (recalcC)
				{
					c = amortizationType == AmortizationType.EqualPrincipal
						? remainingPrincipal/(length - i)
						: GetLevelPayment(tArray.Skip(i).ToArray(), remainingPrincipal, rate, frequency);
				}
				interest[i] = remainingPrincipal * GetPayRate(rate, frequency, tArray[i]);
				scheduledPrincipalPay[i] = amortizationType == AmortizationType.EqualPrincipal ? c : (c - interest[i]);
				if (recalcC)
				{
					prepayment[i] = (remainingPrincipal - scheduledPrincipalPay[i])*PrepaymentModel.Smm(i + 1 + numPastPayment);

					//remaining principal will default, and scheduled principal will also default and corresponding interest will lost
					var defaultRate = DefaultModel.Mdr(i + 1 + numPastPayment);
					var paidPrincipalDefault = scheduledPrincipalPay[i] * defaultRate;
					var paidInterestDefault = interest[i] * defaultRate;
					interest[i] -= paidInterestDefault;
					scheduledPrincipalPay[i] -= paidPrincipalDefault;

					defaultPrincipal[i] = (remainingPrincipal - scheduledPrincipalPay[i])*defaultRate + paidPrincipalDefault;
				}
				else
				{
					prepayment[i] = 0.0;
					defaultPrincipal[i] = 0.0;
				}
				
				remainingPrincipal -= (scheduledPrincipalPay[i] + prepayment[i]+defaultPrincipal[i]);
				begPrincipal[i] = remainingPrincipal;
			}
		}

		public void GetPaymentDetails(double[] tArray, 
			double principal, 
			double[] cpnArray,
			Frequency frequency,
			AmortizationType amortizationType,
			out double[] begPrincipal,
			out double[] interest,
			out double[] scheduledPrincipalPay,
			out double[] prepayment,
			out double[] defaultPrincipal,
			int numPastPayment)
		{
			var length = tArray.Length;

			begPrincipal = new double[length];
			interest = new double[length];
			scheduledPrincipalPay = new double[length];
			prepayment = new double[length];
			defaultPrincipal = new double[length];

			var uniqueCpns = cpnArray.Distinct();

			var remainingPrincipal = principal;

			var c = amortizationType == AmortizationType.EqualPrincipal ? remainingPrincipal / (length) : GetLevelPayment(tArray.ToArray(), remainingPrincipal, cpnArray[0], frequency);
			var recalcC = PrepaymentModel.NeedRecalc() || DefaultModel.NeedRecalc() || uniqueCpns.Count() != 1;

			for (var i = 0; i < tArray.Length; ++i)
			{
				if (recalcC)
				{
					c = amortizationType == AmortizationType.EqualPrincipal
						? remainingPrincipal / (length - i)
						: GetLevelPayment(tArray.Skip(i).ToArray(), remainingPrincipal, cpnArray[i], frequency);
				}
				interest[i] = remainingPrincipal * GetPayRate(cpnArray[i], frequency, tArray[i]);
				scheduledPrincipalPay[i] = amortizationType == AmortizationType.EqualPrincipal ? c : (c - interest[i]);
				if (recalcC)
				{
					prepayment[i] = (remainingPrincipal - scheduledPrincipalPay[i]) * PrepaymentModel.Smm(i + 1 + numPastPayment);

					//remaining principal will default, and scheduled principal will also default and corresponding interest will lost
					var defaultRate = DefaultModel.Mdr(i + 1 + numPastPayment);
					var paidPrincipalDefault = scheduledPrincipalPay[i]*defaultRate;
					var paidInterestDefault = interest[i]*defaultRate;
					interest[i] -= paidInterestDefault;
					scheduledPrincipalPay[i] -= paidPrincipalDefault;

					defaultPrincipal[i] = (remainingPrincipal - scheduledPrincipalPay[i])*defaultRate + paidPrincipalDefault;
				}
				else
				{
					prepayment[i] = 0.0;
					defaultPrincipal[i] = 0.0;
				}

				remainingPrincipal -= (scheduledPrincipalPay[i] + prepayment[i] + defaultPrincipal[i]);
				begPrincipal[i] = remainingPrincipal;
			}
		}

		public abstract double GetPayRate(double rate, Frequency frequency, double t);
		public abstract double GetLevelPayment(double[] tArray, double principal, double rate, Frequency frequency);
	}
}
