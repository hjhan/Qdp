using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.Equity;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity
{
    [DataContract]
    [Serializable]
    public class ResetStrikeOptionInfo : OptionInfoBase
    {
        public ResetStrikeOptionInfo(string tradeId):
            base(
                  instrumentType: "ResetStrikeOption",
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
                  nsimulations: null)
        {
        }

        public ResetStrikeOptionInfo(string tradeId,
            OptionValuationParameters valuationParameter,
            string underlyingInstrumentType,
            string underlyingTicker,
            string optionType,
            double strike,
            double notional,
            string startDate,
            string exerciseDates,
            string underlyingMaturityDate = "",
            string exercise = "European",
            string dayCount = "Act365",
            string calendar = "chn",
            string settlement = "", // T+X settlement
            string observationDates = "",
            string payoffCurrency = "CNY",
            string settlementCurrency = "CNY",
            bool monteCarlo = false,
            int? parallelDegree = null,
            int? nsimulations = null
            )
            : base(
                  instrumentType: "ResetStrikeOption",
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
                  nsimulations: nsimulations)
        {
        }

        [DataMember]
        public string ResetStrikeType { get; set; }

        [DataMember]
        public double CashAmount { get; set; }

        [DataMember]
        public string StrikeFixingDate { get; set; }

    }


}
