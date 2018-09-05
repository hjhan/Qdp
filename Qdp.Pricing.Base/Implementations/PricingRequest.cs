using System;
using System.Collections.Generic;
using System.Linq;

namespace Qdp.Pricing.Base.Implementations
{
    /// <summary>
    /// 计算请求类型枚举
    /// </summary>
	public enum PricingRequest : ulong
	{
		None = 0x0,
		Pv = 0x1,
		Dv01 = 0x2,
		Ai = 0x4,
		Ytm = 0x8,
		ModifiedDuration = 0x10,
		Convexity = 0x20,
		Cashflow = 0x40,
		Delta = 0x80,
		Gamma = 0x100,
		Vega = 0x200,
		Rho = 0x400,
		Theta = 0x800,
		Dv01Underlying = 0x1000,
		KeyRateDv01 = 0x2000,
		MacDuration = 0x4000,
        //Sp01 = 0x8000,
        Carry = 0x8000,
        ZeroSpread = 0x10000,
		RhoForeign = 0x20000,
		ProductSpecific = 0x40000,
		Pv01 = 0x80000,
		FairQuote = 0x100000,
		Irr = 0x200000,
		UnderlyingFairQuote = 0x400000,
		AiEod = 0x800000,
		DirtyPrice = 0x1000000,
		CleanPrice = 0x2000000,
		MktQuote = 0x4000000,
		YtmExecution = 0x8000000,
        NetAnnualizedYield = 0x10000000,
        ConvertFactors =    0x20000000,
        ZeroSpreadDelta =   0x40000000,
        StoppingTime =      0x80000000,
        DDeltaDt = 0x100000000,
        DVegaDt = 0x200000000,
        DDeltaDvol = 0x400000000,
        DVegaDvol = 0x800000000,
        UnderlyingPv = 0x1000000000,
        Basis = 0x2000000000,  // CTD bond of bond futures
        CheapestToDeliver = 0x4000000000,         // CTD cash bond -  BondFut * ConvFact
        DollarDuration = 0x800000000,
        DollarConvexity = 0x10000000000,  //10 
        All =             0xFFFFFFFFFFF
	}

	public static class PricingRqeustExtension
	{
		public static PricingRequest[] Split(this PricingRequest request)
		{
			var result = new List<PricingRequest>();

			const ulong min = 0x1UL;
			var max = Enum.GetValues(typeof (PricingRequest)).Cast<ulong>().Max();

			for (var e = min; e <= max; e = e << 1)
			{
				if ((request & (PricingRequest) e) == (PricingRequest) e)
				{
					result.Add((PricingRequest)e);
				}
			}

			return result.ToArray();
		}
	}
}
