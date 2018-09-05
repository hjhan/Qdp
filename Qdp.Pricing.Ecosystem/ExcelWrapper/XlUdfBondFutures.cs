using System;
using System.Linq;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Ecosystem.ExcelWrapper
{
	/// <summary>
	/// Wrapped function calls.
	/// </summary>
	public partial class XlUdf
	{
		/// <summary>
		/// Return QDP bond futures qdp object.
		/// </summary>
		/// <param name="tradeId"></param>
		/// <param name="startDate"></param>
		/// <param name="settlementDate"></param>
		/// <param name="deliverableBondIds"></param>
		/// <param name="notional"></param>
		/// <param name="calendar"></param>
		/// <param name="dayCount"></param>
		/// <param name="nominalCoupon"></param>
		/// <param name="currency"></param>
		/// <returns></returns>
		public static object xl_CustomizedBondFutures(string tradeId, string startDate = null, string settlementDate = null, string[] deliverableBondIds = null, double notional = 1000000, string calendar = "chn", string dayCount = "Act365", double nominalCoupon = 0.03, string currency = "CNY")
		{
			var trade = XlManager.GetTrade(tradeId);
			if (!(trade is BondFuturesInfo))
			{
				startDate = startDate ?? DateTime.Now.ToString("yyyy-MM-dd");
				settlementDate = settlementDate ?? new Term("3M").Next(startDate.ToDate()).ToString();
				trade = new BondFuturesInfo(tradeId)
				{
					StartDate = startDate,
					MaturityDate = settlementDate,
					Calendar = calendar,
					Currency = currency,
					DayCount = dayCount,
					DeliverableBondInfos =
						deliverableBondIds == null
							? null
							: deliverableBondIds.Select(x => (FixedRateBondInfo) XlManager.GetTrade(x)).ToArray(),
					InstrumentType = "BondFutures",
					NominalCoupon = nominalCoupon,
					Notional = notional,
					ValuationParamters = new SimpleCfValuationParameters("FundingCurve", "", "FundingCurve")
				};

				XlManager.AddTrades(new[] { trade });
			}
			return trade.ToTradeInfoInLabelData(null);
		}

		
	}
}
