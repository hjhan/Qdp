using System.Security.Cryptography.X509Certificates;
using Qdp.Pricing.Base.Enums;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IMortgageCalculator
	{
		IMortgagePrepayment PrepaymentModel { get; }
		IMortgageDefault DefaultModel { get; }
		double GetLevelPayment(double[] tArray, double principal, double rate, Frequency frequency);
		void GetPaymentDetails(double[] tArray, double principal, double rate, Frequency frequency, AmortizationType amortizationType, out double[] begPrincipal, out double[] interest, out double[] principalPay, out double[] prapeyment, out double[] defaultPrincipal, int numPastPayment);
		void GetPaymentDetails(double[] tArray, double principal, double[] cpnArray, Frequency frequency, AmortizationType amortizationType, out double[] begPrincipal, out double[] interest, out double[] principalPay, out double[] prapeyment, out double[] defaultPrincipal, int numPastPayment);
	}
}
