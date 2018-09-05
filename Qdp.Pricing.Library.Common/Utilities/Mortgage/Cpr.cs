using System;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities.Mortgage
{
	//conditional prepayment rate
	public class Cpr : IMortgagePrepayment
	{
		private double _annualConditionalPrepaymentRate;
		private int _numPaymentsPerYear;
		public Cpr(double annualConditionalPrepaymentRate,
			int numPaymentsPerYear = 12
			)
		{
			_annualConditionalPrepaymentRate = annualConditionalPrepaymentRate;
			_numPaymentsPerYear = numPaymentsPerYear;
		}

		public double Smm(int n)
		{
			return 1.0 - Math.Pow(1.0 - _annualConditionalPrepaymentRate, 1.0 / _numPaymentsPerYear);
		}

		public bool NeedRecalc()
		{
			return !_annualConditionalPrepaymentRate.IsAlmostZero();
		}
	}
}
