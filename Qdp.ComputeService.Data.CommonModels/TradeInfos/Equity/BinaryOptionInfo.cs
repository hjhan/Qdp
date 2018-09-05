using System;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.Equity;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity
{
    [DataContract]
    [Serializable]
    public class BinaryOptionInfo : OptionInfoBase
    {
        public BinaryOptionInfo(string tradeId):
            base(
                  instrumentType: "BinaryOption",
                  tradeId: tradeId,
                  valuationParameter: null,
                  strike: 0.0,
                  underlyingInstrumentType: "",
                  underlyingTicker: "",
                  optionType: "Call",
                  notional: 0.0,
                  startDate: "",
                  underlyingMaturityDate: "",
                  exerciseDates: "",
                  observationDates: "",
                  calendar: "chn",
                  dayCount: "Act365",
                  exercise: "European",
                  payoffCurrency: "CNY",
                  settlementCurrency: "CNY",
                  settlement: "",
                  monteCarlo: false,
                  parallelDegree: null,
                  nsimulations: null,
                  isMoneynessOption: false,
                  initialSpotPrice: double.NaN,
                  hasNightMarket: false,
                  commodityFuturesPreciseTimeMode: false)
        {
        }

        public BinaryOptionInfo(
            string tradeId,
            OptionValuationParameters valuationParameter,
            string underlyingInstrumentType,
            string underlyingTicker,
            string optionType,
            double strike,
            double notional,
            string startDate,
            string exerciseDates,
            string underlyingMaturityDate= "",
            string exercise = "European",
            string dayCount = "Act365",
            string calendar = "chn",
            string settlement = "",  // T+X settlement
            string observationDates = "",
            string payoffCurrency = "CNY",
            string settlementCurrency = "CNY",
            bool monteCarlo = false,
            int? parallelDegree = null,
            int? nsimulations = null,
            bool isMoneynessOption = false,
            double initialSpotPrice = double.NaN,
            bool hasNightMarket = false,
            bool commodityFuturesPreciseTimeMode = false
            )
            : base(
                  instrumentType: "BinaryOption",
                  tradeId: tradeId,
                  valuationParameter: valuationParameter,
                  strike: strike,
                  underlyingInstrumentType: underlyingInstrumentType,
                  underlyingTicker: underlyingTicker,
                  optionType: optionType,
                  notional: notional,
                  startDate: startDate,
                  underlyingMaturityDate: underlyingMaturityDate,
                  exerciseDates: exerciseDates,
                  observationDates: observationDates,
                  calendar: calendar,
                  dayCount: dayCount,
                  exercise: exercise,
                  payoffCurrency: payoffCurrency,
                  settlementCurrency: settlementCurrency,
                  settlement: settlement,
                  monteCarlo: monteCarlo,
                  parallelDegree: parallelDegree,
                  nsimulations: nsimulations,
                  isMoneynessOption: isMoneynessOption,
                  initialSpotPrice: initialSpotPrice,
                  hasNightMarket: hasNightMarket,
                  commodityFuturesPreciseTimeMode: commodityFuturesPreciseTimeMode)
        {
		}

		[DataMember]
		public string BinaryOptionPayoffType { get; set; }

		[DataMember]
		public double CashOrNothingAmount { get; set; }

		[DataMember]
		public string BinaryOptionReplicationStrategy { get; set; }

		[DataMember]
		public double ReplicationShiftSize { get; set; }

        [DataMember]
        public string BinaryRebateType { get; set; }
    }


}
