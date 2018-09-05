using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams;
using Qdp.Foundation.TableWithHeader;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	[DataContract]
	[Serializable]
	public class FloatingRateBondInfo : BondInfoBase
	{
		public FloatingRateBondInfo(string tradeId,
			string bondId = null,
			double tradeprice = double.NaN,
			string counterparty = null,
			string tradeDate = null,
			string tradeTime = null)
			: base(tradeId, "FloatingRateBond", bondId, tradeprice, counterparty, tradeDate, tradeTime)
		{
		}

		public FloatingRateBondInfo()
			: base(Guid.NewGuid().ToString(), "FloatingRateBond")
		{
		}

		[DataMember]
		[Column(3)]
		public string FloatingCalc { get; set; }

		[DataMember]
		[Column(4)]
		public string Index { get; set; }

		[DataMember]
		[Column(5)]
		public string ResetDC { get; set; }

		[DataMember]
		[Column(6)]
		public string ResetCompound { get; set; }

		[DataMember]
		[Column(7)]
		public string ResetStub { get; set; }

		[DataMember]
		[Column(8)]
		public string ResetBD { get; set; }

		[DataMember]
		[Column(9)]
		public string ResetToFixingGap { get; set; }

		[DataMember, DefaultValue(null)]
		[Column(10)]
		public string ResetTerm { get; set; }

		[DataMember]
		[Column(11)]
		public double Spread { get; set; }

		[DataMember]
		[Column(12)]
		public int ResetAverageDays { get; set; }

		[DataMember]
		[Column(13)]
		public int ResetRateDigits { get; set; }

		[DataMember]
		public double CapRate { get; set; }

		[DataMember]
		public double FloorRate { get; set; }

		[DataMember]
		public double FloatingRateMultiplier { get; set; }
		public override ValuationParameters GetValuationParamters()
		{
			return ValuationParamters;
		}

	}
}