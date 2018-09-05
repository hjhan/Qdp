using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	[DataContract]
	[Serializable]
	public class AbsWithRepurchaseInfo : TradeInfoBase
	{
		public AbsWithRepurchaseInfo(string tradeId)
			: base(tradeId, "AbsWithRepurchase")
		{
		}

		public AbsWithRepurchaseInfo()
			: base(Guid.NewGuid().ToString(), "AbsWithRepurchase")
		{
		}

		[DataMember]
		public LoanInfo LoanInfo { get; set; }

		[DataMember]
		public BondInfoBase[] Tranches { get; set; }

		[DataMember]
		public double RepurchaseRatio { get; set; }
		[DataMember]
		public double MaintenanceFeeRate { get; set; }
		[DataMember]
		public double ProtectionFeeRate { get; set; }

		public override TradeInfoBase[] GetDependenciesTrades()
		{
			return new TradeInfoBase[]{LoanInfo}.Union(Tranches).ToArray();
		}
	}
}