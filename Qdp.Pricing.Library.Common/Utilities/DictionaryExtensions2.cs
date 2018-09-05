using System.Collections.Generic;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities
{
	public static class DictionaryExtensions2
	{
		public static Dictionary<string, IYieldCurve> UpdateKey(this Dictionary<string, IYieldCurve> dict, string key, IYieldCurve value)
		{
			return dict.UpdateKey<IYieldCurve>(key, value);
		}
	}
}
