using Qdp.Pricing.Base.Enums;

namespace Qdp.Pricing.Library.Base.Utilities
{
	public static class SwapDirectionExtension
	{
		public static double Sign(this SwapDirection swapDirection)
		{
			return swapDirection == SwapDirection.Payer ? -1 : 1;
		}
	}
}
