using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.Equity;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity
{
    [DataContract]
    [Serializable]
    public class SpreadOptionInfo : OptionInfoBase
    {
        public SpreadOptionInfo(string tradeId):
            base(
                  instrumentType: "SpreadOption",
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
                  hasNightMarket: false,
                  commodityFuturesPreciseTimeMode: false)
        {
        }

        public SpreadOptionInfo(string tradeId,
            OptionValuationParameters valuationParameter,
            string underlyingInstrumentType,
            string underlyingTicker,
            double strike,
            string optionType,
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
            int? nsimulations = null,
            bool hasNightMarket = false,
            bool commodityFuturesPreciseTimeMode = false
            )
            : base(
                  instrumentType: "SpreadOption",
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
                  hasNightMarket: hasNightMarket,
                  commodityFuturesPreciseTimeMode: commodityFuturesPreciseTimeMode)
        {
        }

        [DataMember]
        public string SpreadType { get; set; }

        [DataMember]
        public string[] UnderlyingTickers { get; set; }

        [DataMember]
        public double[] Weights { get; set; }

        [DataMember]
        public string PricingStrategy { get; set; }


    }


}
