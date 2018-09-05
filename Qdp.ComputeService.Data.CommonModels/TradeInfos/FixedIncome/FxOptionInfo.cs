using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome
{
	// BaseCurrency/QuoteCurrency DomesticCurrency/ForeignCurrency USD/CNY=6.5 
	[DataContract]
	[Serializable]
	public class FxOptionInfo : TradeInfoBase
	{
		public FxOptionInfo(string tradeId)
			: base(tradeId, "FxOption")
		{
		}

		private FxOptionInfo()
			: base(string.Empty, "FxOption")
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
		public double StrikeFxRate { get; set; }

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

		[DataMember]
		public string[] ExerciseDates { get; set; }

		[DataMember]
		public string[] ObservationDates { get; set; }

		[DataMember, DefaultValue(null)]
		public string OptionPremiumPaymentDate { get; set; }

		[DataMember, DefaultValue(0.0)]
		public double OptionPremium { get; set; }

		[DataMember]
		public string UnderlyingInstrumentType { get; set; }

		[DataMember]
		public string UnderlyingFxSpotSettlement { get; set; }

		[DataMember]
		public FxOptionValuationParameters ValuationParamters { get; set; }
		public override ValuationParameters GetValuationParamters()
		{
			return ValuationParamters;
		}
	}
}