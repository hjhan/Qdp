using System;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities.Mortgage
{
	//standard default assumption
	public class Sda : IMortgageDefault
	{
		public double Multiplier { get; private set; }
		public double RecoveryRate { get; private set; }
		private int _numPaymentsPerYear;
		public Sda(double multiplier = 0.0, 
			double recoveryRate = 0.0,
			int numPaymentsPerYear = 12)
		{
			Multiplier = multiplier;
			RecoveryRate = recoveryRate;
			_numPaymentsPerYear = numPaymentsPerYear;
			if (recoveryRate < 0.0 || recoveryRate > 1.0)
			{
				throw new PricingLibraryException("Recovery rate must be between 0.0 and 1.0!");
			}
		}

		public bool NeedRecalc()
		{
			return !Multiplier.IsAlmostZero();
		}

		public double Mdr(int n)
		{
			return (1.0 - Math.Pow(1.0 - Cdr(n) * Multiplier, 1.0 / _numPaymentsPerYear)) * (1.0 - RecoveryRate);
		}

		//conditional default rate 
		// Fabozzi Mortgage back securities products page 67
		private double Cdr(int n)
		{
			if (n <= 30)
			{
				return 0.0002*n;
			}
			if (n <= 60)
			{
				return 0.006;
			}
			if (n <= 120)
			{
				return 0.006 - 0.000095*(n - 60);
			}

			return 0.0003;
		}
	}
}
