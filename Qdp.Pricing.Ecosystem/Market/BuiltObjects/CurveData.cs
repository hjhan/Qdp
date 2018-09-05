using Qdp.Foundation.Implementations;
using Qdp.Pricing.Library.Common.Market;


namespace Qdp.Pricing.Ecosystem.Market.BuiltObjects
{
	public class CurveData : GuidObject
	{
		public YieldCurve YieldCurve { get; private set; }

		public CurveData(YieldCurve yieldCurve)
		{
			YieldCurve = yieldCurve;
		}
	}
}
