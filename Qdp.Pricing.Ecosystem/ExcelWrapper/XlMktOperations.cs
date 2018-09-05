using System.Collections.Generic;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Ecosystem.ExcelWrapper
{
	public static partial class XlManager
	{
        //Note: not used by current sheets, to update corresponding test dependencies
		/// <summary>
		/// Get the marketDataDefinition for object name on a given an exising market.
		/// </summary>
		/// <param name="mktName"></param>
		/// <param name="mktObjName"></param>
		/// <returns></returns>
		public static MarketDataDefinition GetMarketObject(string mktName, string mktObjName)
		{
			var market = GetXlMarket(mktName);
			if (market == null)
			{
				return null;
			}
			return GetXlMarket(mktName).GetMktObj(mktObjName);
		}

        //Note: not used by current sheets
        /// <summary>
        /// Get the discount factor for a given date on a given curve.
        /// </summary>
        /// <param name="mktName">The name of the market.</param>
        /// <param name="curveName">The name of the yield curve.</param>
        /// <param name="date">The date to calculate the discount factor, i.e. 2017-01-03.</param>
        /// <returns></returns>
        public static double GetDf(string mktName, string curveName, string date)
		{
            //var qdpMarket = XlManager.GetXlMarket(mktName).QdpMarket;
            //var yieldcurve = qdpMarket.GetData<CurveData>(curveName).YieldCurve;\
            var yieldcurve = XlManager.GetPrebuildQdpMarket(mktName).YieldCurves[curveName];
            return yieldcurve.GetDf(date.ToDate());
		}

        //Note: not used by current sheets
        /// <summary>
        /// Get the discount factor from endDate to startDate on a given curve.
        /// </summary>
        /// <param name="mktName">The name of the market.</param>
        /// <param name="curveName">The name of the yield curve.</param>
        /// <param name="startDate">The date to calculate the discount factor, i.e. 2017-01-03.</param>
        /// <param name="endDate">The date to calculate the discount factor, i.e. 2017-01-03.</param>
        /// <returns></returns>
        public static double GetDf(string mktName, string curveName, string startDate, string endDate)
        {
            //var qdpMarket = XlManager.GetXlMarket(mktName).QdpMarket;
            //var yieldcurve = qdpMarket.GetData<CurveData>(curveName).YieldCurve;\
            var yieldcurve = XlManager.GetPrebuildQdpMarket(mktName).YieldCurves[curveName];
            return yieldcurve.GetDf(startDate.ToDate(), endDate.ToDate());
        }

        /// <summary>
        /// Get the spot rate for a given date on a given curve.
        /// </summary>
        /// <param name="mktName">The name of the market.</param>
        /// <param name="curveName">The name of the yield curve.</param>
        /// <param name="date">The date to calculate the spot rate, i.e. 2017-01-03.</param>
        /// <returns></returns>
        public static double GetSpotRate(string mktName, string curveName, string date)
        {
            //var qdpMarket = XlManager.GetXlMarket(mktName).QdpMarket;
            //var yieldcurve = qdpMarket.GetData<CurveData>(curveName).YieldCurve;
            var yieldcurve = XlManager.GetPrebuildQdpMarket(mktName).YieldCurves[curveName];
            return yieldcurve.GetSpotRate(date.ToDate());
        }

        /// <summary>
        /// Get the zero rate for a given period on a given curve.
        /// </summary>
        /// <param name="mktName">The name of the market.</param>
        /// <param name="curveName">The name of the yield curve.</param>
        /// <param name="startDate">The start date of the calculation period, i.e. 2017-01-03.</param>
        /// <param name="endDate">The end date of the calculation period, i.e. 2017-01-13.</param>
        /// <returns></returns>
        public static double GetZeroRate(string mktName, string curveName, string startDate, string endDate)
		{
            //var qdpMarket = XlManager.GetXlMarket(mktName).QdpMarket;
            //var yieldcurve = qdpMarket.GetData<CurveData>(curveName).YieldCurve;
            var yieldcurve = XlManager.GetPrebuildQdpMarket(mktName).YieldCurves[curveName];
            return yieldcurve.ZeroRate(startDate.ToDate(), endDate.ToDate());
		}

		/// <summary>
		/// Return fixing curve by historicalIndexRates
		/// </summary>
		/// <param name="settlementDate"></param>
		/// <param name="historicalIndexRates"></param>
		/// <param name="floatingBondInfo"></param>
		/// <returns></returns>
		public static InstrumentCurveDefinition GetFixingCurve(string settlementDate, Dictionary<string, Dictionary<string, double>> historicalIndexRates, FloatingRateBondInfo floatingBondInfo)
		{
			var resetCalendar = CalendarImpl.Get(floatingBondInfo.Calendar);
			var index = floatingBondInfo.Index;
			var rates = historicalIndexRates[index];
			var tradeId = floatingBondInfo.TradeId;

			var indexDate = new DayGap(floatingBondInfo.ResetToFixingGap).Get(resetCalendar, settlementDate.ToDate());
			var fixingTuple = rates.TryGetValue(indexDate.ToString(), resetCalendar);
			var indexRate = rates.GetAverageIndex(fixingTuple.Item1, resetCalendar, floatingBondInfo.ResetAverageDays,floatingBondInfo.ResetRateDigits);

			string fixingCurveName = "FixingCurve_" + tradeId;
			var fixingRateDefinition = new[]
					{
						new RateMktData("1D", indexRate, index, "None", fixingCurveName),
						new RateMktData("50Y", indexRate, index, "None", fixingCurveName),
					};
			var curveConvention = new CurveConvention("fixingCurveConvention_" + tradeId,
				 "CNY",
				 "ModifiedFollowing",
				 "Chn_ib",
				 index == null ? "Act365" : index.ToIndexType().DayCountEnum(),
				 "Continuous",
				 "ForwardFlat");

			floatingBondInfo.ValuationParamters.FixingCurveName = fixingCurveName;
			AddTrades(new []{floatingBondInfo});
			return new InstrumentCurveDefinition(fixingCurveName, curveConvention, fixingRateDefinition, "ForwardCurve");
		}

		/// <summary>
		/// Return funding curve by fund rate
		/// </summary>
		/// <param name="fundRate"></param>
		/// <returns></returns>
		public static InstrumentCurveDefinition GetFundingCurve(double fundRate)
		{
			var curveConvention = new CurveConvention("curveConvention",
					 "CNY",
					 "ModifiedFollowing",
					 "Chn_ib",
					 "Act365",
					 "Simple",
					 "Linear"
					 );

			var fundingCurveName = "FundingCurve";
			var fundingRates = new[]
			{
				new RateMktData("1D", fundRate,  "Spot", "None", fundingCurveName),
				new RateMktData("5Y", fundRate,  "Spot", "None", fundingCurveName),
			};

			return new InstrumentCurveDefinition(fundingCurveName, curveConvention, fundingRates, "SpotCurve");
		}
	}
}
