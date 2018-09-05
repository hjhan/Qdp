using System;
using System.Collections.Generic;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Ecosystem.Trade.FixedIncome;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using Qdp.Pricing.Library.Equity.Options;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.Pricing.Ecosystem.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace Qdp.Pricing.Ecosystem.ExcelWrapper
{
	/// <summary>
	/// 
	/// </summary>
	public partial class XlUdf
	{

		#region calculator
		/// <summary>
		/// Calculate yield from dirtyPrice for bond
		/// </summary>
		/// <param name="bondId"></param>
		/// <param name="calcDate"></param>
		/// <param name="fullPrice"></param>
		/// <returns>if succeed then return yield（double type）, if falied then return error message</returns>
		public static object xl_YieldFromPrice(string bondId, string calcDate, double fullPrice)
		{
			var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.Dirty, fullPrice, PricingRequest.Ytm);
			if (returnObject is IPricingResult)
			{
				return ((IPricingResult) returnObject).Ytm;
			}
			else
			{
				return returnObject;
			}
		}

        /// <summary>
        /// Calculate dirtyPrice from yield for bond
        /// </summary>
        /// <param name="bondId"></param>
        /// <param name="calcDate"></param>
        /// <param name="yield"></param>
        /// <returns>if succeed then return dirtyPrice（double type）, if falied then return error message</returns>
        public static object xl_PriceFromYield(string bondId, string calcDate, double yield)
		{
			var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.Ytm, yield, PricingRequest.DirtyPrice);
			if (returnObject is IPricingResult)
			{
				return ((IPricingResult) returnObject).DirtyPrice;
			}
			else
			{
				return returnObject;
			}
		}

		/// <summary>
		/// Calculate dirtyPrice from option yield for bond
		/// </summary>
		/// <param name="bondId"></param>
		/// <param name="calcDate"></param>
		/// <param name="optionYield"></param>
		/// <returns>if succeed then return dirtyPrice（double type）, if falied then return error message</returns>
		public static object xl_PriceFromOptionYield(string bondId, string calcDate, double optionYield)
		{
			var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.YtmExecution, optionYield, PricingRequest.DirtyPrice);
			if (returnObject is IPricingResult)
			{
				return ((IPricingResult)returnObject).DirtyPrice;
			}
			else
			{
				return returnObject;
			}
		}

		/// <summary>
		/// Calculate option yield and option date from dirtyPrice for bond
		/// </summary>
		/// <param name="bondId"></param>
		/// <param name="calcDate"></param>
		/// <param name="fullPrice"></param>
		/// <returns>if succeed then return option yield and option date（Tuple<string, double> type）, if falied then return error message</returns>
		public static object xl_OptionYieldFromPrice(string bondId, string calcDate, double fullPrice)
		{
			var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.Dirty, fullPrice, PricingRequest.Ytm | PricingRequest.YtmExecution);
			if (returnObject is IPricingResult)
			{
				var result = returnObject as IPricingResult;
				var optionYield = double.IsNaN(result.YieldToCall) ? result.YieldToPut : result.YieldToCall;
				var optionDate = result.CallDate ?? result.PutDate;
				return optionDate == null ? null : Tuple.Create(optionDate.ToString(), optionYield);
			}
			else
			{
				return returnObject;
			}
		}

		/// <summary>
		/// Calculate predict rate and index date for floationg bond
		/// </summary>
		/// <param name="bondId"></param>
		/// <param name="calcDate"></param>
		/// <returns>if succeed then return predict rate and index date（Tuple<string, double> type）, if falied then return error message</returns>
		public static object xl_PredictRate(string bondId, string calcDate)
		{
			var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.Dirty, 100, PricingRequest.None);
			if (returnObject is Tuple<Date, double>)
			{
				var result = returnObject as Tuple<Date, double>;
				return Tuple.Create(result.Item1.ToString(), result.Item2);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Calculate accrued interest of a bond.
		/// </summary>
		/// <param name="bondId"></param>
		/// <param name="calcDate"></param>
		/// <returns>if succeed then return accrued interest（double type）, if falied then return error message</returns>
		public static object xl_AccruedInterest(string bondId, string calcDate)
		{
			var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.Dirty, 100, PricingRequest.Ai);
			if (returnObject is IPricingResult)
			{
				return ((IPricingResult)returnObject).Ai;
			}
			else
			{
				return returnObject;
			}
		}

		/// <summary>
		/// Calculate end of day accrued interest of a bond.
		/// </summary>
		/// <param name="bondId"></param>
		/// <param name="calcDate"></param>
		/// <returns>if succeed then return end-of-day accrued interest（double type）, if falied then return error message</returns>
		public static object xl_AccruedInterestEod(string bondId, string calcDate)
		{
			var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.Dirty, 100, PricingRequest.AiEod);
			if (returnObject is IPricingResult)
			{
				return ((IPricingResult)returnObject).Ai;
			}
			else
			{
				return returnObject;
			}
		}

		/// <summary>
		/// Calculate the accrued days of a bond.
		/// </summary>
		/// <param name="bondId"></param>
		/// <param name="calcDate"></param>
		/// <returns>if succeed then return accrued days（int type）, if falied then return error message</returns>
		public static object xl_AccruedDays(string bondId, string calcDate)
		{
			var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.Dirty, 100, PricingRequest.Ai);
			if (returnObject is IPricingResult)
			{
				return ((IPricingResult)returnObject).AiDays;
			}
			else
			{
				return returnObject;
			}
		}

		/// <summary>
		/// Calculate the end of day accrued days of a bond.
		/// </summary>
		/// <param name="bondId"></param>
		/// <param name="calcDate"></param>
		/// <returns>if succeed then return end-of-day accrued days（int type）, if falied then return error message</returns>
		public static object xl_AccruedDaysEod(string bondId, string calcDate)
		{
			var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.Dirty, 100, PricingRequest.AiEod);
			if (returnObject is IPricingResult)
			{
				return ((IPricingResult)returnObject).AiDays;
			}
			else
			{
				return returnObject;
			}
		}

        /// <summary>
        /// Calculate the modified duration of a bond.
        /// </summary>
        /// <param name="bondId"></param>
        /// <param name="calcDate"></param>
        /// <param name="fullPrice"></param>
        /// <returns>if succeed then return the modified duration（double type）, if falied then return error message</returns>
        public static object xl_ModifiedDuration(string bondId, string calcDate, double fullPrice)
		{
			var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.Dirty, fullPrice, PricingRequest.ModifiedDuration);
			if (returnObject is IPricingResult)
			{
				return ((IPricingResult)returnObject).ModifiedDuration;
			}
			else
			{
				return returnObject;
			}
		}

        //similar to xl_ModifiedDuration, but scales with notional, rather than assuming its a 100 worth bond
        public static object xl_DollarModifiedDuration(string bondId, string calcDate, double fullPrice)
        {
            var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.Dirty, fullPrice, PricingRequest.ModifiedDuration);
            if (returnObject is IPricingResult)
            {
                return ((IPricingResult)returnObject).ModifiedDuration;
            }
            else
            {
                return returnObject;
            }
        }

        /// <summary>
        /// Calculate the mac duration of a bond.
        /// </summary>
        /// <param name="bondId"></param>
        /// <param name="calcDate"></param>
        /// <param name="fullPrice"></param>
        /// <returns>if succeed then return the mac duration（double type）, if falied then return error message</returns>
        public static object xl_MacDuration(string bondId, string calcDate, double fullPrice)
		{
			var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.Dirty, fullPrice, PricingRequest.MacDuration);
			if (returnObject is IPricingResult)
			{
				return ((IPricingResult)returnObject).MacDuration;
			}
			else
			{
				return returnObject;
			}
		}

        /// <summary>
        /// Calculate the convexity of a bond.
        /// </summary>
        /// <param name="bondId"></param>
        /// <param name="calcDate"></param>
        /// <param name="fullPrice"></param>
        /// <returns>if succeed then return the convexity（double type）, if falied then return error message</returns>
        public static object xl_Convexity(string bondId, string calcDate, double fullPrice)
		{
			var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.Dirty, fullPrice, PricingRequest.Convexity);
			if (returnObject is IPricingResult)
			{
				return ((IPricingResult)returnObject).Convexity;
			}
			else
			{
				return returnObject;
			}
		}

        //similar to xl_Convexity, but scales with notional, rather than assuming its a 100 worth bond
        public static object xl_DollarConvexity(string bondId, string calcDate, double fullPrice)
        {
            var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.Dirty, fullPrice, PricingRequest.DollarConvexity);
            if (returnObject is IPricingResult)
            {
                return ((IPricingResult)returnObject).DollarConvexity;
            }
            else
            {
                return returnObject;
            }
        }

        /// <summary>
        /// Calculate the pvbp of a bond.
        /// </summary>
        /// <param name="bondId"></param>
        /// <param name="calcDate"></param>
        /// <param name="fullPrice"></param>
        /// <returns>if succeed then return the pvbp（double type）, if falied then return error message</returns>
        public static object xl_PvBp(string bondId, string calcDate, double fullPrice)
		{
			var returnObject = BondEngineCalc(bondId, calcDate, PriceQuoteType.Dirty, fullPrice, PricingRequest.Pv01);
			if (returnObject is IPricingResult)
			{
				return ((IPricingResult)returnObject).Pv01;
			}
			else
			{
				return returnObject;
			}
		}
		#endregion

		#region create trade

		/// <summary>
		/// Return QDP bond qdp object.
		/// </summary>
		/// <param name="tradeId"></param>
		/// <param name="startDate"></param>
		/// <param name="maturityDate"></param>
		/// <param name="notional"></param>
		/// <param name="calendar"></param>
		/// <param name="currency"></param>
		/// <param name="accrualDayCount"></param>
		/// <param name="accrualBusinessDayConvention"></param>
		/// <param name="paymentDayCount"></param>
		/// <param name="paymentFrequency"></param>
		/// <param name="paymentStub"></param>
		/// <param name="paymentBusinessDayConvention"></param>
		/// <param name="settlement"></param>
		/// <param name="settlementCoupon"></param>
		/// <param name="issuePrice"></param>
		/// <param name="firstPaymentDate"></param>
		/// <param name="issueRate"></param>
		/// <param name="amoritzationInIndex"></param>
		/// <param name="renormalizeAfterAmoritzation"></param>
		/// <param name="compensationRate"></param>
		/// <param name="optionToCall"></param>
		/// <param name="optionToPut"></param>
		/// <param name="optionToAssPut"></param>
		/// <param name="fixedCoupon"></param>
		/// <param name="index"></param>
		/// <param name="resetDayCount"></param>
		/// <param name="resetCompound"></param>
		/// <param name="resetStub"></param>
		/// <param name="resetBusinessDayConvention"></param>
		/// <param name="resetToFixingGap"></param>
		/// <param name="resetTerm"></param>
		/// <param name="resetAverageDays"></param>
		/// <param name="resetRateDigits"></param>
		/// <param name="spread"></param>
		/// <param name="floatingRateMultiplier"></param>
		/// <param name="stickToEom"></param>
		/// <returns></returns>
		private static object xl_Bond(
			string tradeId,
			string startDate = null,
			string maturityDate = null,
			double notional = 100,
			string calendar = "chn_ib",
			string currency = "CNY",
			string accrualDayCount = "Act365",
			string accrualBusinessDayConvention = "ModifiedFollowing",
			string paymentDayCount = "Act365",
			string paymentFrequency = "SemiAnnual",
			string paymentStub = "ShortStart",
			string paymentBusinessDayConvention = "ModifiedFollowing",
			string settlement = "+0D",
			double settlementCoupon = double.NaN,
			double issuePrice = double.NaN,
			string firstPaymentDate = null,
			double issueRate = double.NaN,
			Dictionary<int, double> amoritzationInIndex = null,
			bool renormalizeAfterAmoritzation = false,
			Dictionary<int, double> compensationRate = null,
			Dictionary<string, double> optionToCall = null,
			Dictionary<string, double> optionToPut = null,
			Dictionary<string, double> optionToAssPut = null,
			double fixedCoupon = double.NaN,
			string index = null,
			string resetDayCount = null,
			string resetCompound = null,
			string resetStub = null,
			string resetBusinessDayConvention = null,
			string resetToFixingGap = null,
			string resetTerm = null,
			int resetAverageDays = 1,
			int resetRateDigits = 12,
			double spread = double.NaN,
			double floatingRateMultiplier = double.NaN,
			bool stickToEom = false)
		{
			var tradeInfo = XlManager.GetTrade(tradeId);
			if (!(tradeInfo is BondInfoBase))
			{
				startDate = startDate ?? DateTime.Now.ToString("yyyy-MM-dd");
				maturityDate = maturityDate ?? new Term("1Y").Next(startDate.ToDate()).ToString();
				BondInfoBase bondInfo = null;
				if (string.IsNullOrWhiteSpace(index))
				{
					bondInfo = new FixedRateBondInfo(tradeId)
					{
						FixedCoupon = double.IsNaN(fixedCoupon) ? 0.03 : fixedCoupon
					};
				}
				else
				{
					bondInfo = new FloatingRateBondInfo(tradeId)
					{
						Index = index ?? "Shibor3M",
						ResetDC = resetDayCount ?? "Act365",
						ResetCompound = resetCompound ?? "Simple",
						ResetStub = resetStub ?? "ShortStart",
						ResetBD = resetBusinessDayConvention ?? "ModifiedFollowing",
						ResetToFixingGap = resetToFixingGap ?? "-1BD",
						ResetTerm = resetTerm ?? "3M",
						Spread = double.IsNaN(spread) ? 0.0 : spread,
						ResetAverageDays = resetAverageDays,
						ResetRateDigits = resetRateDigits,
						FloatingRateMultiplier = double.IsNaN(floatingRateMultiplier) ? 1.0 : floatingRateMultiplier,
						FloatingCalc = "ZzFrn",
						CapRate = 100,
						FloorRate = -100
					};
				}
				bondInfo.StartDate = startDate;
				bondInfo.MaturityDate = maturityDate;
				bondInfo.Calendar = calendar;
				bondInfo.PaymentFreq = paymentFrequency;
				bondInfo.StickToEom = stickToEom;
				bondInfo.PaymentStub = paymentStub;
				bondInfo.Notional = notional;
				bondInfo.Currency = currency;
				bondInfo.AccrualDC = accrualDayCount;
				bondInfo.DayCount = paymentDayCount;
				bondInfo.AccrualBD = accrualBusinessDayConvention;
				bondInfo.PaymentBD = paymentBusinessDayConvention;
				bondInfo.Settlement = settlement;
				bondInfo.SettlementCoupon = settlementCoupon;
				bondInfo.TradingMarket = calendar == "chn_ib"
					? TradingMarket.ChinaInterBank.ToString()
					: TradingMarket.ChinaExShe.ToString();
				bondInfo.IsZeroCouponBond = !double.IsNaN(issuePrice);
				bondInfo.IssuePrice = issuePrice;
				bondInfo.FirstPaymentDate = firstPaymentDate;
				bondInfo.AmortizationType = "None";
				bondInfo.AmoritzationInIndex = amoritzationInIndex;
				bondInfo.RenormAmortization = renormalizeAfterAmoritzation;
				bondInfo.CompensationRate = compensationRate;
				bondInfo.IssueRate = issueRate;
				bondInfo.OptionToCall = optionToCall;
				bondInfo.OptionToPut = optionToPut;
				bondInfo.OptionToAssPut = optionToAssPut;
				bondInfo.ValuationParamters = new SimpleCfValuationParameters("中债国债收益率曲线", "", "中债国债收益率曲线");

				XlManager.AddTrades(new[] {bondInfo});
				tradeInfo = bondInfo;
			}
			return tradeInfo.ToTradeInfoInLabelData(null);
		}

		/// <summary>
		/// Return QDP fixedRateBond qdp object.
		/// </summary>
		/// <param name="tradeId"></param>
		/// <param name="startDate"></param>
		/// <param name="maturityDate"></param>
		/// <param name="notional"></param>
		/// <param name="calendar"></param>
		/// <param name="currency"></param>
		/// <param name="accrualDayCount"></param>
		/// <param name="accrualBusinessDayConvention"></param>
		/// <param name="paymentDayCount"></param>
		/// <param name="paymentFrequency"></param>
		/// <param name="paymentStub"></param>
		/// <param name="paymentBusinessDayConvention"></param>
		/// <param name="settlement"></param>
		/// <param name="settlementCoupon"></param>
		/// <param name="issuePrice"></param>
		/// <param name="firstPaymentDate"></param>
		/// <param name="issueRate"></param>
		/// <param name="amoritzationInIndex"></param>
		/// <param name="renormalizeAfterAmoritzation"></param>
		/// <param name="compensationRate"></param>
		/// <param name="optionToCall"></param>
		/// <param name="optionToPut"></param>
		/// <param name="optionToAssPut"></param>
		/// <param name="fixedCoupon"></param>
		/// <param name="stickToEom"></param>
		/// <returns></returns>
		public static object xl_FixedRateBond(
			string tradeId,
			string startDate = null,
			string maturityDate = null,
			double notional = 100,
			string calendar = "chn_ib",
			string currency = "CNY",
			string accrualDayCount = "Act365",
			string accrualBusinessDayConvention = "ModifiedFollowing",
			string paymentDayCount = "Act365",
			string paymentFrequency = "SemiAnnual",
			string paymentStub = "ShortStart",
			string paymentBusinessDayConvention = "ModifiedFollowing",
			string settlement = "+0D",
			double settlementCoupon = double.NaN,
			double issuePrice = double.NaN,
			string firstPaymentDate = null,
			double issueRate = double.NaN,
			Dictionary<int, double> amoritzationInIndex = null,
			bool renormalizeAfterAmoritzation = false,
			Dictionary<int, double> compensationRate = null,
			Dictionary<string, double> optionToCall = null,
			Dictionary<string, double> optionToPut = null,
			Dictionary<string, double> optionToAssPut = null,
			double fixedCoupon = 0.03,
			bool stickToEom = false)
		{
			var tradeInfo = xl_Bond(
				tradeId,
				startDate,
				maturityDate,
				notional,
				calendar,
				currency,
				accrualDayCount,
				accrualBusinessDayConvention,
				paymentDayCount,
				paymentFrequency,
				paymentStub,
				paymentBusinessDayConvention,
				settlement,
				settlementCoupon,
				issuePrice,
				firstPaymentDate,
				issueRate,
				amoritzationInIndex,
				renormalizeAfterAmoritzation,
				compensationRate,
				optionToCall,
				optionToPut,
				optionToAssPut,
				fixedCoupon,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				1,
				12,
				double.NaN,
				double.NaN,
				stickToEom);
			return tradeInfo;
		}

		/// <summary>
		/// Return QDP floatingRateBond qdp object.
		/// </summary>
		/// <param name="tradeId"></param>
		/// <param name="startDate"></param>
		/// <param name="maturityDate"></param>
		/// <param name="notional"></param>
		/// <param name="calendar"></param>
		/// <param name="currency"></param>
		/// <param name="accrualDayCount"></param>
		/// <param name="accrualBusinessDayConvention"></param>
		/// <param name="paymentDayCount"></param>
		/// <param name="paymentFrequency"></param>
		/// <param name="paymentStub"></param>
		/// <param name="paymentBusinessDayConvention"></param>
		/// <param name="settlement"></param>
		/// <param name="settlementCoupon"></param>
		/// <param name="issuePrice"></param>
		/// <param name="firstPaymentDate"></param>
		/// <param name="issueRate"></param>
		/// <param name="amoritzationInIndex"></param>
		/// <param name="renormalizeAfterAmoritzation"></param>
		/// <param name="compensationRate"></param>
		/// <param name="optionToCall"></param>
		/// <param name="optionToPut"></param>
		/// <param name="optionToAssPut"></param>
		/// <param name="index"></param>
		/// <param name="resetDayCount"></param>
		/// <param name="resetCompound"></param>
		/// <param name="resetStub"></param>
		/// <param name="resetBusinessDayConvention"></param>
		/// <param name="resetToFixingGap"></param>
		/// <param name="resetTerm"></param>
		/// <param name="resetAverageDays"></param>
		/// <param name="resetRateDigits"></param>
		/// <param name="spread"></param>
		/// <param name="floatingRateMultiplier"></param>
		/// <param name="stickToEom"></param>
		/// <returns></returns>
		public static object xl_FloatingRateBond(
			string tradeId,
			string startDate = null,
			string maturityDate = null,
			double notional = 100,
			string calendar = "chn_ib",
			string currency = "CNY",
			string accrualDayCount = "Act365",
			string accrualBusinessDayConvention = "ModifiedFollowing",
			string paymentDayCount = "Act365",
			string paymentFrequency = "SemiAnnual",
			string paymentStub = "ShortStart",
			string paymentBusinessDayConvention = "ModifiedFollowing",
			string settlement = "+0D",
			double settlementCoupon = double.NaN,
			double issuePrice = double.NaN,
			string firstPaymentDate = null,
			double issueRate = double.NaN,
			Dictionary<int, double> amoritzationInIndex = null,
			bool renormalizeAfterAmoritzation = false,
			Dictionary<int, double> compensationRate = null,
			Dictionary<string, double> optionToCall = null,
			Dictionary<string, double> optionToPut = null,
			Dictionary<string, double> optionToAssPut = null,
			string index = "Shibor3M",
			string resetDayCount = null,
			string resetCompound = null,
			string resetStub = null,
			string resetBusinessDayConvention = null,
			string resetToFixingGap = null,
			string resetTerm = null,
			int resetAverageDays = 1,
			int resetRateDigits = 12,
			double spread = double.NaN,
			double floatingRateMultiplier = double.NaN,
			bool stickToEom = false)
		{
			return xl_Bond(
				tradeId,
				startDate,
				maturityDate,
				notional,
				calendar,
				currency,
				accrualDayCount,
				accrualBusinessDayConvention,
				paymentDayCount,
				paymentFrequency,
				paymentStub,
				paymentBusinessDayConvention,
				settlement,
				settlementCoupon,
				issuePrice,
				firstPaymentDate,
				issueRate,
				amoritzationInIndex,
				renormalizeAfterAmoritzation,
				compensationRate,
				optionToCall,
				optionToPut,
				optionToAssPut,
				double.NaN,
				index,
				resetDayCount,
				resetCompound,
				resetStub,
				resetBusinessDayConvention,
				resetToFixingGap,
				resetTerm,
				resetAverageDays,
				resetRateDigits,
				spread,
				floatingRateMultiplier,
				stickToEom);
		}

		/// <summary>
		/// Return QDP floatingRateBond qdp object.
		/// </summary>
		/// <param name="tradeId"></param>
		/// <returns></returns>
		public static object xl_FloatingRateBond(string tradeId)
		{
			return xl_Bond(
				tradeId, 
				null, 
				null, 
				100, 
				"chn_ib", 
				"CNY", 
				"Act365", 
				"ModifiedFollowing", 
				"Act365", 
				"SemiAnnual", 
				"ShortStart", 
				"ModifiedFollowing", 
				"+0D", 
				double.NaN, 
				double.NaN, 
				null, 
				double.NaN, 
				null, 
				false, 
				null, 
				null, 
				null, 
				null, 
				double.NaN, 
				"Shibor3M"
			);
		}


		#endregion

		#region private
		public static object BondEngineCalc(string bondId, string calcDate, PriceQuoteType priceQuote, double quote, PricingRequest request, FixedRateBondInfo fixedBond = null )
		{
			var bond = fixedBond ?? XlManager.GetTrade(bondId);
			if (bond == null)
			{
				return string.Format("Cannot find bond {0}.", bondId);
			}

			var vf = new BondVf((BondInfoBase)bond);
			var bondInstrument = vf.GenerateInstrument();

			var valueDate = calcDate.ToDate();
			var fixingCurve = new YieldCurve(
				"中债国债收收益率曲线",
				valueDate,
				new[]
					{
						new Tuple<Date, double>(valueDate, 0.0),
						new Tuple<Date, double>(new Term("10Y").Next(valueDate), 0.0)
					},
				BusinessDayConvention.ModifiedFollowing,
				new Act365(),
				CalendarImpl.Get("Chn_ib"),
				CurrencyCode.CNY,
				Compound.Continuous,
				Interpolation.ForwardFlat,
				YieldCurveTrait.ForwardCurve
				);
			var market = new MarketCondition(
				x => x.ValuationDate.Value = valueDate,
				x => x.FixingCurve.Value = fixingCurve,
				x =>
					x.MktQuote.Value =
						new Dictionary<string, Tuple<PriceQuoteType, double>> { { bondId, Tuple.Create(priceQuote, quote) } },
				x => x.HistoricalIndexRates.Value = HistoricalIndexRates
				);
			if (bond is FloatingRateBondInfo)
			{
				var fixingTuple = bondInstrument.Coupon.GetPrimeCoupon(HistoricalIndexRates, fixingCurve, valueDate);
				var keyTenors = new string[fixingCurve.GetKeyTenors().Length];
				fixingCurve.GetKeyTenors().CopyTo(keyTenors, 0);
				for (var i = 0; i < keyTenors.Length; ++i)
				{
					market = (MarketCondition)market.UpdateCondition(new UpdateMktConditionPack<IYieldCurve>(x => x.FixingCurve, market.FixingCurve.Value.BumpKeyRate(i, fixingTuple.Item2)));
				}
				if (request.Equals(PricingRequest.None))
				{
					return fixingTuple;
				}
			}

			var engine = vf.GenerateEngine();
			var result = engine.Calculate(bondInstrument, market, request);

			if (!result.Succeeded)
			{
				return string.Format("Failed to Calculate bond {0}:{1}", bondId, result.ErrorMessage);
			}

			return result;
		}
        #endregion

        #region private
        /// <summary>
		/// Calibrate implied bs vol from spot premium of an european option
		/// </summary>
		/// <param name="premium"></param>
		/// <param name="isCall"></param>
		/// <param name="strike"></param>
        /// <param name="spot"></param>
        /// <param name="rate"></param>
        /// <param name="expiryDateStr"></param>
        /// <param name="valuationDateStr"></param>
        /// <param name="underlyingInstrumentType"></param>
        /// <param name="calendarStr"></param>
        /// <param name="dayCountStr"></param>
        /// <param name="commodityFuturesPreciseTimeMode"></param>
        /// <param name="hasNightMarket"></param>
		/// <returns>if succeed then return implied vol, if falied then return error message</returns>
        public static double xl_ImpliedVolFromPremium(double premium, bool isCall, double strike, double spot, double rate,
            string expiryDateStr, string valuationDateStr, string underlyingInstrumentType, string calendarStr, string dayCountStr, 
            bool commodityFuturesPreciseTimeMode = false,
            bool hasNightMarket = false) {
            
            var bsEngine = new AnalyticalVanillaEuropeanOptionEngine();
            var expiryDate = expiryDateStr.ToDate();
            var valuationDate = valuationDateStr.ToDate();
            var instrumentType = underlyingInstrumentType.ToInstrumentType();
            var calendar = calendarStr.ToCalendarImpl();
            var dayCount = dayCountStr.ToDayCountImpl();

            var trade = new VanillaOption(
                valuationDate,
                expiryDate,
                OptionExercise.European,
                isCall ? OptionType.Call : OptionType.Put,
                strike,
                instrumentType,
                calendar,
                dayCount,
                CurrencyCode.CNY,
                CurrencyCode.CNY,
                new[] { expiryDate },
                new[] { expiryDate },
                notional: 1,
                hasNightMarket : hasNightMarket,
                commodityFuturesPreciseTimeMode : commodityFuturesPreciseTimeMode
                );

            var curve = new YieldCurve(
                "riskFreeRate",
                valuationDate,
                new[]
                {
                    Tuple.Create((ITerm)new Term("1D"), rate),
                    Tuple.Create((ITerm)new Term("1Y"), rate)
                },
                BusinessDayConvention.ModifiedFollowing,
                dayCount,
                calendar,
                CurrencyCode.CNY,
                Compound.Continuous,
                Interpolation.CubicHermiteMonotic,
                YieldCurveTrait.SpotCurve
                );
            var volSurf = new VolSurfMktData("VolSurf", 0.1).ToImpliedVolSurface(valuationDate);
            var market = new MarketCondition(
                    x => x.ValuationDate.Value = valuationDate,
                    x => x.DiscountCurve.Value = curve,
                    x => x.DividendCurves.Value = new Dictionary<string, IYieldCurve> { { "", curve } },//not used by futures option
                    x => x.FixingCurve.Value = curve,
                    x => x.RiskfreeCurve.Value = curve,
                    x => x.SpotPrices.Value = new Dictionary<string, double> { {"", spot }},
                    x => x.VolSurfaces.Value = new Dictionary<string, IVolSurface> { {"", volSurf } }); //not used by this calibration
            return bsEngine.ImpliedVolFromPremium(targetPremium: premium, option: trade, market: market);
        }
        #endregion
    }
}
