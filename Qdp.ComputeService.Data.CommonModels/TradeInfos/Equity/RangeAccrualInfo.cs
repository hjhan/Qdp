using System.ComponentModel;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.Equity;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity
{
	public class RangeAccrualInfo: OptionInfoBase
    {
        public RangeAccrualInfo(string tradeId):
            base(
                  instrumentType: "RangeAccrual",
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

        public RangeAccrualInfo(
            string tradeId,
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
            string settlement = "",
            string observationDates = "",
            string payoffCurrency = "CNY",
            string settlementCurrency = "CNY",
            bool monteCarlo = false,
            int? parallelDegree = null,
            int? nsimulations = null
            )
            : base(
                  instrumentType: "RangeAccrual",
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

		[DataMember, DefaultValue("1.00,1.05,0.03;1.05,1.10,0.04")]
		public string Ranges { get; set; }

		[DataMember, DefaultValue("2016-07-14,0.998;2016-07-15,0.999")]
		public string Fixings { get; set; }

	}


}
