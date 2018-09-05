using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities.Mortgage
{
	public class MortgageCalculator : BaseMortgageCalculator
	{
		public MortgageCalculator(IMortgagePrepayment prepaymentmodel,
			IMortgageDefault defaultModel)
			: base(prepaymentmodel, defaultModel)
		{
		}

		public override double GetPayRate(double rate, Frequency frequency, double t)
		{
			return rate*t;
		}

		public override double GetLevelPayment(double[] tArray,
			double principal,
			double rate,
			Frequency frequency)
		{
			var aggregate = 1.0;
			var sum = 0.0;
			for (var i = tArray.Length - 1; i > 0; --i)
			{
				aggregate *= 1 + rate * tArray[i];
				sum += aggregate;
			}

			sum += 1;
			aggregate *= 1 + rate * tArray[0];
			return principal * aggregate / sum;
		}
	}
}
