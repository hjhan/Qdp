using System;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities.Mortgage
{
	//public securities association prepayment benchmark
	public class Psa : IMortgagePrepayment
	{
		public double Multiplier { get; private set; }
		private readonly double _maxRate;
		private readonly int _len;
		private int _numPaymentsPerYear;

		public Psa(double multiplier = 0.0,
			double maxRate = 0.06,
			int len = 30,
			int numPaymentsPerYear = 12
			)
		{
			Multiplier = multiplier;
			_maxRate = maxRate;
			_len = len;
			_numPaymentsPerYear = numPaymentsPerYear;
		}

		//single month mortatility
		public double Smm(int n)
		{
			return 1.0 - Math.Pow(1.0 - Cpr(n)*Multiplier, 1.0/_numPaymentsPerYear);
		}

		public bool NeedRecalc()
		{
			return !Multiplier.IsAlmostZero();
		}

		//conditional prepayment rate
		private double Cpr(int n)
		{
			if (n <= _len)
			{
				return n * _maxRate / _len;
			}
			return _maxRate;
		}
	}
}
