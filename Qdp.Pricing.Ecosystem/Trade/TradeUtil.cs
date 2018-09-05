using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.ComputeService.Data.CommonModels.TradeInfos;
using Qdp.Pricing.Base.Implementations;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Ecosystem.Trade
{
    static class TradeUtil
    {
        public static void GenerateOptionDates(OptionInfoBase TradeInfo,
            out Date[] exerciseDates,
            out Date[] obsDates,
            out DayGap settlement)
        {
            settlement = string.IsNullOrEmpty(TradeInfo.Settlement) ? new DayGap("+0BD") : TradeInfo.Settlement.ToDayGap();

            var startDate = TradeInfo.StartDate.ToDate();
            var calendar = TradeInfo.Calendar.ToCalendarImpl();

            if (TradeInfo.Exercise.ToOptionExercise() == OptionExercise.American)
            {
                var inputDates = TradeInfo.ExerciseDates.Split(';');
                if (inputDates.Length > 1)
                    exerciseDates = inputDates.Select(d => d.ToDate()).ToArray();
                else
                    exerciseDates = calendar.BizDaysBetweenDatesInclEndDay(startDate, TradeInfo.ExerciseDates.ToDate()).ToArray();
            }
            else
            {
                exerciseDates = new[] { TradeInfo.ExerciseDates.ToDate() };
            }

            if (!string.IsNullOrEmpty(TradeInfo.ObservationDates))
            {
                obsDates = TradeInfo.ObservationDates.Split(QdpConsts.Semilicon).Select(x => x.ToDate()).ToArray();
            }
            else
            {
                var expiryDate = exerciseDates.Last();
                obsDates = calendar.BizDaysBetweenDatesExcluStartDay(startDate, expiryDate).Union(new[] { expiryDate }).ToArray();
            }

        }
    }
}
