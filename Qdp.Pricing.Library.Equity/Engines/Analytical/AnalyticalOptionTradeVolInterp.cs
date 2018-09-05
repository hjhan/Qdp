using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;


namespace Qdp.Pricing.Library.Equity.Engines.Analytical
{
    public class AnalyticalOptionTradeVolInterp
    {
        public static double tradeVolLinearInterp(Date valuationDate, double tradeOpenVol, double tradeCloseVol, Date startDate, Date maturityDate,
            int numOfSmoothingDays, DayCountMode dayCountMode, ICalendar calendar)
        {
            Date smoothingEndDate;
            double T, t;

            switch (dayCountMode)
            {
                case DayCountMode.CalendarDay:
                    smoothingEndDate = startDate.AddDays(numOfSmoothingDays);
                    if (smoothingEndDate > maturityDate) smoothingEndDate = maturityDate;
                    T = smoothingEndDate - startDate;
                    t = valuationDate - startDate;

                    break;                    
                default:
                    smoothingEndDate = calendar.AddBizDays(startDate, numOfSmoothingDays);
                    if (smoothingEndDate > maturityDate) smoothingEndDate = maturityDate;
                    Date valuationTradingDate = calendar.IsBizDay(valuationDate) ? valuationDate : calendar.PrevBizDay(valuationDate);
                    T = calendar.BizDaysBetweenDatesExcluStartDay(startDate, smoothingEndDate).Count;
                    t = calendar.BizDaysBetweenDatesExcluStartDay(startDate, valuationTradingDate).Count;
                    break;
            }

            if (valuationDate >= smoothingEndDate)
            {
                return tradeCloseVol;
            }
            if (valuationDate < smoothingEndDate & valuationDate >= startDate)
            {
                double slope = (tradeCloseVol - tradeOpenVol) / T;
                return tradeOpenVol + slope * t;
            }
            else
            {
                throw new PricingBaseException("AnalyticalOptionTradeVolInterp error: valuationDate < startDate");
            }
        }
    }
}
