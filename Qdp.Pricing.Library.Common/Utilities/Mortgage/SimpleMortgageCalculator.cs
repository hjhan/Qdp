using System;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities.Mortgage
{
	public class SimpleMortgageCalculator : BaseMortgageCalculator
	{
		public SimpleMortgageCalculator(IMortgagePrepayment prepaymentmodel,
			IMortgageDefault defaultModel)
			: base(prepaymentmodel, defaultModel)
		{
		}

		//tArray is acc periods in years
		public override double GetPayRate(double rate, Frequency frequency, double t)
		{
			return rate/frequency.CountPerYear();
		}

		public override double GetLevelPayment(double[] tArray, 
			double principal, 
			double rate, 
			Frequency frequency)
		{
			var payRate = rate / frequency.CountPerYear();
			var factor = Math.Pow(1 + payRate, tArray.Length);

			return principal * payRate * factor / ( factor - 1.0);
		}
	}
}
