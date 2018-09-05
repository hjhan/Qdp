using System;
using Qdp.ComputeService.Data.CommonModels.TradeInfos.FixedIncome;
using Qdp.ComputeService.Data.CommonModels.ValuationParams.FixedIncome;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Ecosystem.ExcelWrapper
{
	public partial class XlUdf
	{
		/// <summary>
		/// Return a interest rate swap.
		/// </summary>
		/// <param name="tradeId"></param>
		/// <param name="startDate"></param>
		/// <param name="maturityDate"></param>
		/// <param name="tenor"></param>
		/// <param name="notional"></param>
		/// <param name="currency"></param>
		/// <param name="swapDirection"></param>
		/// <param name="calendar"></param>
		/// <param name="fixedLegDayCount"></param>
		/// <param name="fixedLegFrequency"></param>
		/// <param name="fixedLegBusinessDayConvention"></param>
		/// <param name="fixedLegStub"></param>
		/// <param name="fixedLegCoupon"></param>
		/// <param name="floatingLegDayCount"></param>
		/// <param name="floatingLegFrequency"></param>
		/// <param name="floatingLegBusinessDayConvention"></param>
		/// <param name="floatingLegStub"></param>
		/// <param name="index"></param>
		/// <param name="resetTerm"></param>
		/// <param name="resetStub"></param>
		/// <param name="resetBusinessDayConvention"></param>
		/// <param name="resetToFixingGap"></param>
		/// <param name="resetCompound"></param>
		/// <returns></returns>
		public static object xl_InterestRateSwap(string tradeId
			, string startDate = null
			, string maturityDate = null
			, string tenor = "1Y"
			, double notional = 100.0
			, string currency = "CNY"
			, string swapDirection = "Payer"
			, string calendar = "chn_ib"
			, string fixedLegDayCount = "Act365"
			, string fixedLegFrequency = "Quarterly"
			, string fixedLegBusinessDayConvention = "ModifiedFollowing"
			, string fixedLegStub = "ShortEnd"
			, double fixedLegCoupon = 0.03
			, string floatingLegDayCount = "Act365"
			, string floatingLegFrequency = "Quarterly"
			, string floatingLegBusinessDayConvention = "ModifiedFollowing"
			, string floatingLegStub = "ShortEnd"
			, string index = "Fr007"
			, string resetTerm = "1W"
			, string resetStub = "ShortEnd"
			, string resetBusinessDayConvention = "None"
			, string resetToFixingGap = "+1BD"
			, string resetCompound = "Compounded"
			)
		{
			var interestRateSwapInfo = XlManager.GetTrade(tradeId);
			if (!(interestRateSwapInfo is InterestRateSwapInfo))
			{
				startDate = startDate ?? DateTime.Now.ToString("yyyy-MM-dd");
				if (maturityDate == null && tenor != null)
				{
					maturityDate = new Term(tenor).Next(startDate.ToDate()).ToString();
				}
				else
				{
					maturityDate = maturityDate ?? new Term("1Y").Next(startDate.ToDate()).ToString();
				}
				string curveName = "";
				switch (index)
				{
					case "Fr007":
						curveName = "Fr007SwapCurve";
						break;
					case "Shibor3M":
						curveName = "Shibor3MSwapCurve";
						break;
					case "Shibor1D":
						curveName = "ShiborONSwapCurve";
						break;
					case "Depo1Y":
						curveName = "Depo1YSwapCurve";
						break;
				}

				interestRateSwapInfo = new InterestRateSwapInfo(tradeId)
				{
					StartDate = startDate,
					MaturityDate = maturityDate,
					Tenor = tenor,
					Notional = notional,
					Currency = currency,
					SwapDirection = swapDirection,
					Calendar = calendar,
					FixedLegDC = fixedLegDayCount,
					FixedLegFreq = fixedLegFrequency,
					FixedLegBD = fixedLegBusinessDayConvention,
					FixedLegStub = fixedLegStub,
					FixedLegCoupon = fixedLegCoupon,
					FloatingLegDC = floatingLegDayCount,
					FloatingLegFreq = floatingLegFrequency,
					FloatingLegBD = floatingLegBusinessDayConvention,
					FloatingLegStub = floatingLegStub,
					Index = index,
					ResetTerm = resetTerm,
					ResetStub = resetStub,
					ResetBD = resetBusinessDayConvention,
					ResetToFixingGap = resetToFixingGap,
					ResetCompound = resetCompound,
					ValuationParamters = new SimpleCfValuationParameters(curveName, curveName, null)
				};

				XlManager.AddTrades(new[] {interestRateSwapInfo});
			}
			return interestRateSwapInfo.ToTradeInfoInLabelData(null);
		}
	}
}
