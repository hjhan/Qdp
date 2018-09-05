using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.Equity;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.ComputeService.Data.CommonModels.ValuationParams;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;
using Qdp.Pricing.Base.Utilities;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Implementations;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.Equity;

namespace Qdp.ComputeService.Data.CommonModels.TradeInfos
{
    [DataContract]
    [Serializable]
    public abstract class OptionInfoBase : TradeInfoBase
    {
        [DataMember]
        public string UnderlyingTicker { get; set; }
        
        [DataMember]
        public OptionValuationParameters ValuationParamter { get; set; }

        [DataMember]
        public string UnderlyingInstrumentType { get; set; }

        [DataMember]
        public double Strike { get; set; }

        [DataMember]
        public string Exercise { get; set; }    //european or american

        [DataMember]
        public string OptionType { get; set; }  //call or put

        [DataMember]
        public double Notional { get; set; }

        [DataMember]
        public string StartDate { get; set; }

        [DataMember]
        public string ExerciseDates { get; set; }

        [DataMember]
        public string ObservationDates { get; set; }

        [DataMember]
        public string UnderlyingMaturityDate { get; set; }

        [DataMember]
        public string Calendar { get; set; }

        [DataMember]
        public string DayCount { get; set; }

        [DataMember]
        public string PayoffCurrency { get; set; }

        [DataMember]
        public string SettlementCurrency { get; set; }

        [DataMember]
        public string OptionPremiumPaymentDate { get; set; }  //default to trade date

        [DataMember]
        public string Settlement { get; set; }

        [DataMember]
        public double? OptionPremium { get; set; }
        [DataMember]
        public bool MonteCarlo { get; set; }
        [DataMember]
        public bool MonteCarloCollectPath { get; set; }

        [DataMember]
        public int? ParallelDegree { get; set; }

        [DataMember]
        public int? NSimulations { get; set; }

        [DataMember]
        public bool IsMoneynessOption { get; set; }

        [DataMember]
        public double InitialSpotPrice { get; set; }

        [DataMember]
        public string CashDividends { get; set; }

        [DataMember]
        public bool HasNightMarket { get; set; }

        [DataMember]
        public bool CommodityFuturesPreciseTimeMode { get; set; }

        public OptionInfoBase(string tradeId,
            OptionValuationParameters valuationParameter,
            double strike,
            string instrumentType,
            string underlyingInstrumentType,
            string underlyingTicker,
            string optionType,
            double notional,
            string startDate,
            string underlyingMaturityDate,
            string exerciseDates,
            string calendar,
            string dayCount,
            string observationDates = "",
            string exercise = "European",
            string payoffCurrency = "CNY",
            string settlementCurrency = "CNY",
            string settlement = "Cash",
            bool monteCarlo = false,
            bool monteCarloCollectPath = false,
            int? parallelDegree = null,
            int? nsimulations = null,
            double tradeprice = double.NaN,
            string counterparty = null,
            string tradeDate = null,
            string tradeTime = null,
            bool isMoneynessOption = false,
            double initialSpotPrice = 0.0,
            string cashDividends = "",
            bool hasNightMarket = false,
            bool commodityFuturesPreciseTimeMode = false
            ) : base(tradeId, instrumentType, tradeprice, counterparty, tradeDate, tradeTime)
        {
            ValuationParamter = valuationParameter;

            Strike = strike;
            UnderlyingInstrumentType = underlyingInstrumentType;
            UnderlyingTicker = underlyingTicker;
            OptionType = optionType;
            Exercise = exercise;
            Notional = notional;
            StartDate = startDate;
            UnderlyingMaturityDate = underlyingMaturityDate;
            ExerciseDates = exerciseDates;
            ObservationDates = observationDates;  //optional?
            Calendar = calendar;
            DayCount = dayCount;

            PayoffCurrency = payoffCurrency;
            SettlementCurrency = settlementCurrency;
            Settlement = settlement;

            MonteCarlo = monteCarlo;
            MonteCarloCollectPath = monteCarloCollectPath;
            ParallelDegree = parallelDegree;
            NSimulations = nsimulations;

            IsMoneynessOption = isMoneynessOption;
            InitialSpotPrice = initialSpotPrice;
            CashDividends = cashDividends;

            if (tradeprice == double.NaN)
                OptionPremium = null;
            else
                OptionPremium = tradeprice;

            OptionPremiumPaymentDate = tradeDate;
            HasNightMarket = hasNightMarket;
            CommodityFuturesPreciseTimeMode = commodityFuturesPreciseTimeMode;           

        }
    }

    [DataContract]
	[Serializable]
	[KnownType(typeof(FloatingRateBondInfo))]
	[KnownType(typeof(FixedRateBondInfo))]
	[KnownType(typeof(FixedDateCouonAdjustedBondInfo))]
	[KnownType(typeof(VanillaOptionInfo))]
	[KnownType(typeof(InterestRateSwapInfo))]
	[KnownType(typeof(FxOptionInfo))]
	[KnownType(typeof(FixedLegInfo))]
	[KnownType(typeof(FloatingLegInfo))]
	[KnownType(typeof(BondFuturesInfo))]
	[KnownType(typeof(LoanInfo))]
	[KnownType(typeof(HoldingPeriodInfo))]
	public abstract class TradeInfoBase
	{

		[DataMember]
		public string InstrumentType { get; set; }

		[DataMember]
		public string TradeId { get; set; }

		[DataMember]
		public string Counterparty { get; set; }

		[DataMember]
		public double TradePrice { get; set; }

		[DataMember]
		public string TradeDate { get; set; }

        [DataMember]
        public string TradeType { get; set; }

        [DataMember]
		public string TradeTime { get; set; }
		public TradeInfoBase(string tradeId, 
			string instrumentType,
			double tradeprice = double.NaN,
			string counterparty = null,
			string tradeDate = null,
			string tradeTime = null
			)
		{
			TradeId = tradeId;
			InstrumentType = instrumentType;
			Counterparty = counterparty;
			TradePrice = tradeprice;
			TradeDate = tradeDate;
			TradeTime = tradeTime;
		}

		public TradeInfoBase()
		{
		}

		public virtual TradeInfoBase[] GetDependenciesTrades()
		{
			return null;
		}

		public string ValuationParamtersToLabelData()
		{
			var valParam = GetValuationParamters();
			if (valParam == null)
			{
				return "";
			}
			return valParam.ToLabelData();
		}

		public ValuationParameters ValuationParamtersConverter(object value)
		{
			string valueStr = (string) value;
			if (this is FixedRateBondInfo || this is FloatingRateBondInfo || this is InterestRateSwapInfo)
			{
				var array = new object[3, 2];
				var rowArray = valueStr.Split(';');
				int i = 0;
				foreach (var colStr in rowArray)
				{
					var colArray = colStr.Split(':');
					array[i, 0] = colArray[0];
					array[i++, 1] = colArray[1];

				}
				return array.TransposeRowsAndColumns().ToArrayObj<SimpleCfValuationParameters>().Single();
			}

			return null;
		}

		protected Dictionary<string, double> ConverterDictionaryStringDouble(string value)
		{

			return value.ToDicObj<string, double>();
		}

		protected Dictionary<int, double> ConverterDictionaryIntDouble(string value)
		{
			return value.ToDicObj<int, double>();
		}

		public virtual ValuationParameters GetValuationParamters()
		{
			return null;
		}


		protected string DictionaryToLableData(Dictionary<string, double> valueDic)
		{
			string convertReturn = null;
			if (valueDic != null)
			{
				convertReturn = valueDic.Select(d => string.Format("{0}:{1}", d.Key, d.Value)).Aggregate("", (current, v) => current + ";" + v).Substring(1);
			}
			return convertReturn;
		}

		protected string DictionaryToLableData(Dictionary<int, double> valueDic)
		{
			string convertReturn = null;
			if (valueDic != null)
			{
				convertReturn = valueDic.Select(d => string.Format("{0}: {1}", d.Key, d.Value)).Aggregate("", (current, v) => current + ";" + v).Substring(1);
			}
			return convertReturn;
		}

        #region upgrade
        IDayCount _dayCount = new Act365(); 
        protected virtual IDayCount Day_Count { get { return _dayCount; } set { _dayCount = value; } }
        protected virtual string Maturity_Date {  get;set;  }

        //todo 要返回交易盈亏 哪个类比较好？
        //PnLResultBase CalculatePositionTradesPnL

        /// <summary>
        /// 计算剩余期限,返回单位年
        /// </summary>
        /// <param name=""></param>
        public virtual double GetDayCountFraction(Date startDate)
        {
            Date endDate = Maturity_Date.ToDate();
            return Math.Round(Day_Count.CalcDayCountFraction(startDate,endDate, startDate,endDate),3);
        }

        //public virtual IPricingResult ValueTrade(QdpMarket market, PricingRequest request)
        //{

        //}

        #endregion
    }
}