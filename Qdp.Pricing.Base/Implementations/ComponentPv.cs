using System;
using System.Runtime.Serialization;
using Qdp.Pricing.Base.Enums;

namespace Qdp.Pricing.Base.Implementations
{
	[DataContract]
	[Serializable]
	public class ComponentPv
	{
		[DataMember]
		public double ComponentAmount { get; set; }
		[DataMember]
		public CurrencyCode ComponentCcy { get; set; }
		[DataMember]
		public double SettlementAmount { get; set; }
		[DataMember]
		public CurrencyCode SettlementCcy { get; set; }

		public ComponentPv(CurrencyCode componentCcy,
			double componentAmount,
			CurrencyCode settlementCcy,
			double settlementAmount)
		{
			ComponentCcy = componentCcy;
			ComponentAmount = componentAmount;
			SettlementCcy = settlementCcy;
			SettlementAmount = settlementAmount;
		}

		public ComponentPv()
		{

		}
	}
}
