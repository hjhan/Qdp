using System;
using System.Runtime.Serialization;

namespace Qdp.Pricing.Base.Utilities
{
	public class PricingBaseException : Exception
	{
		public PricingBaseException() 
			: base()
		{
		}

		public PricingBaseException(string msg)
			: base(msg)
		{
		}

		public PricingBaseException(string msg, Exception innerException)
			: base(msg, innerException)
		{
		}

		public PricingBaseException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
