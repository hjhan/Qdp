using System;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities.Mortgage
{
	//conditional default rate
	public class Cdr : IMortgageDefault
	{
		private double _annualConditionalDefaultRate;
		private int _numPaymentsPerYear;
		public double RecoveryRate { get; private set; }

		public Cdr(double annualConditionalDefaultRate,
			int numPaymentsPerYear = 12,
			double recoveryRate = 0.0)
		{
			_annualConditionalDefaultRate = annualConditionalDefaultRate;
			_numPaymentsPerYear = numPaymentsPerYear;
			RecoveryRate = recoveryRate;
		}

		public double Mdr(int n)
		{
			return (1.0 - Math.Pow(1.0 - _annualConditionalDefaultRate, 1.0 / _numPaymentsPerYear)) * (1.0 - RecoveryRate);
		}

		public bool NeedRecalc()
		{
			return !_annualConditionalDefaultRate.IsAlmostZero();
		}
	}
}
