using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Base.Implementations;

namespace Qdp.Pricing.Library.Common.Engines
{
	public class IrsEngine : CashflowProductEngine<InterestRateSwap>
	{
		public override double GetFairQuote(InterestRateSwap trade, IMarketCondition market)
		{
			//return trade.MaturityDate > market.DiscountCurve.Value.KeyPoints.Last().Item1 ? double.NaN : trade.ModelValue(market);
			return  trade.ModelValue(market);
		}

		public override double CalcPv01(InterestRateSwap trade, IMarketCondition market, double pv = double.NaN)
		{
			var bumpedIrs = trade.Bump(-1);
			var bumpedPv = CalcPv((InterestRateSwap)bumpedIrs, market);

			return bumpedPv - (!double.IsNaN(pv)? pv : CalcPv(trade, market));
		}

        public override double CalcCarry(InterestRateSwap trade, IMarketCondition market) {
            var today = market.ValuationDate;
            var nextDay = trade.FixedLeg.Calendar.NextBizDay(today);
            
            var fixedLegCarry = trade.FixedLeg.GetAccruedInterest(nextDay, market, isEod: true) - 
                trade.FixedLeg.GetAccruedInterest(today, market, isEod: true);

            var fltLegCarry = trade.FloatingLeg.GetAccruedInterest(nextDay, market, isEod: true) -
                trade.FloatingLeg.GetAccruedInterest(today, market, isEod: true);

            //equivalent to above
            //var accDates = trade.FloatingLeg.Accruals.PeriodInclDate(today);
            //var fltRate = 0.0;
            //if (accDates != null)
            //    fltRate = trade.FloatingLeg.Coupon.GetCoupon(accDates[0], accDates[1], market.FixingCurve.Value, market.HistoricalIndexRates, out CfCalculationDetail[] temp) *
            //                trade.FloatingLeg.DayCount.CalcDayCountFraction(today, nextDay);
            //var fltLegCarry2 = fltRate * trade.FloatingLeg.Notional;

            return fixedLegCarry + fltLegCarry;
        }
	}
}
