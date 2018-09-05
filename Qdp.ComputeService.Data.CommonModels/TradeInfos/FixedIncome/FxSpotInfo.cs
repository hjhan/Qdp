using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	// BaseCurrency/QuoteCurrency DomesticCurrency/ForeignCurrency USD/CNY=6.5 
	[DataContract]
	[Serializable]
	public class FxSpotInfo : TradeInfoBase
	{
		public FxSpotInfo(string tradeId)
			: base(tradeId, "FxSpot")
		{
		}

		private FxSpotInfo()
			: base(string.Empty, "FxSpot")
		{
		}

		[DataMember]
		public string StartDate { get; set; }

		[DataMember]
		public string MaturityDate { get; set; }

		[DataMember]
		public string Settlment { get; set; }

		[DataMember]
		public string OptionType { get; set; }

		[DataMember]
		public string Exercise { get; set; }

		[DataMember]
		public double FxSpotRate { get; set; }

		[DataMember]
		public double NotionalInQuoteCcy { get; set; }

		[DataMember]
		public string DayCount { get; set; }

		[DataMember]
		public string BaseCurrency { get; set; }

		[DataMember]
		public string BaseCcyCalendar { get; set; }

		[DataMember]
		public string QuoteCurrency { get; set; }

		[DataMember]
		public string QuoteCcyCalendar { get; set; }

		[DataMember]
		public string PayoffCurrency { get; set; }

		[DataMember]
		public string SettlementCurrency { get; set; }
	}
}