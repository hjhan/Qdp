using System;

namespace Qdp.Pricing.Library.Base.Curves.Interfaces
{
	public interface ICurve<TX> 
	{
		TX Start { get; }
		Tuple<TX, double>[] KeyPoints { get; }
		double GetValue(TX x);
		double GetIntegral(TX x);
	}
}
