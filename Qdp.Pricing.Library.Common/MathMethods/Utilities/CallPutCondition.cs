using System;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.MathMethods.Interfaces;

namespace Qdp.Pricing.Library.Common.MathMethods.Utilities
{
	public class CallPutCondition : NumericCondition
	{
		private readonly double _strike;
		private readonly OptionType _optionType;

		public CallPutCondition(OptionType optionType, double strike)
		{
			_strike = strike;
			_optionType = optionType;
		}

		public override double Apply(double price)
		{
			switch (_optionType)
			{
				case OptionType.Call:
					return Math.Min(price, _strike);
				case OptionType.Put:
					return Math.Max(price, _strike);
				default:
					throw new PricingLibraryException("CallPutCondition: Option type must be either call or put");
			}
		}
	}
}
