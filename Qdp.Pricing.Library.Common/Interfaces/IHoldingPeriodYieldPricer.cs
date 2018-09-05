
using System.Collections.Generic;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IHoldingPeriodYieldPricer
	{
		double CalcAnnualizedYieldCleanPrice(
			string calcRequestType, 
			double inputValue1, 
			double inputValue2,
			Dictionary<string, double> result);
	}
}
