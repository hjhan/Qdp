using System;

namespace Qdp.Pricing.Library.Base.Utilities
{
	public class PricingLibraryException : Exception
	{
		public PricingLibraryException(string msg)
			: base(msg)
		{
			
		}

		public PricingLibraryException(string msg, Exception ex)
			: base(msg, ex)
		{
			
		}
	}

    public class CalibrationException : Exception
    {
        public CalibrationException(string msg)
            : base(msg)
        {

        }

        public CalibrationException(string msg, Exception ex)
            : base(msg, ex)
        {

        }
    }
}
