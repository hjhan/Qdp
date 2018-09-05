using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Equity.Interfaces;

namespace Qdp.Pricing.Library.Equity.Options
{
    public class LookbackOption : OptionBase
    {
        public override string TypeName { get { return "AsianOption"; } }
        public Dictionary<Date, double> Fixings { get; private set; }
        public StrikeStyle StrikeStyle { get; private set; }

        public LookbackOption(Date startDate,
            Date maturityDate,
            OptionExercise exercise,
            OptionType optionType,
            StrikeStyle strikeStyle,
            double strike,
            InstrumentType underlyingInstrumentType,
            ICalendar calendar,
            IDayCount dayCount,
            CurrencyCode payoffCcy,
            CurrencyCode settlementCcy,
            Date[] exerciseDates,
            Date[] observationDates,
            Dictionary<Date, double> fixings,
            double notional = 1,
            DayGap settlementGap = null,
            Date optionPremiumPaymentDate = null,
            double optionPremium = 0,
            bool isMoneynessOption = false,
            double initialSpotPrice = 0.0,
            Dictionary<Date, double> dividends = null,
            bool hasNightMarket = false,
            bool commodityFuturesPreciseTimeMode = false
            )
            : base(startDate, maturityDate, exercise, optionType, new double[] { strike }, underlyingInstrumentType, calendar, dayCount,
                  settlementCcy, payoffCcy, exerciseDates, observationDates, notional, settlementGap,
                  optionPremiumPaymentDate, optionPremium,
                  isMoneynessOption: isMoneynessOption, initialSpotPrice: initialSpotPrice, dividends: dividends, hasNightMarket: hasNightMarket,
                  commodityFuturesPreciseTimeMode: commodityFuturesPreciseTimeMode)
        {
            Fixings = fixings;
            StrikeStyle = strikeStyle;
        }

        //Todo Fixing payoff
        public override Cashflow[] GetPayoff(double[] price)
        {
            //price:{ minPrice, maxPrice, pricePath[ExerciseDates[0]] }
            //used in approximation
            var amount = 0.0;
            var effectiveStrike = IsMoneynessOption ? Strike * InitialSpotPrice : Strike;

            if (OptionType == OptionType.Call)
            {
                amount = StrikeStyle == StrikeStyle.Fixed ? Math.Max(0.0, price[1] - effectiveStrike) : Math.Max(0.0, price[2] - price[0]);
            }
            else if (OptionType == OptionType.Put)
            {
                amount = StrikeStyle == StrikeStyle.Fixed ? Math.Max(0.0, effectiveStrike - price[0]) : Math.Max(0.0, price[1] - price[2]);
            }

            var settlementDate = SettlmentGap.Get(Calendar, UnderlyingMaturityDate);

            return new[]
            {
                new Cashflow(StartDate, UnderlyingMaturityDate, settlementDate, amount*Notional, SettlementCcy, CashflowType.Net, false, double.NaN, null),
				//new Cashflow(StartDate, UnderlyingMaturityDate, OptionPremiumPaymentDate ?? settlementDate, OptionPremium, SettlementCcy, CashflowType.Net, false, double.NaN, null)
			};
        }

        public override Cashflow[] GetPayoff(Dictionary<Date, double> pricePath)
        {
            var prices = new Dictionary<Date, double>(pricePath);

            //merges fixings and monte carlo path
            foreach (var key in Fixings.Keys)
            {
                prices[key] = Fixings[key];
            }

            var maxPrice = ObservationDates.Max(x => prices[x]);
            var minPrice = ObservationDates.Min(x => prices[x]);

            return GetPayoff(new[] { minPrice, maxPrice, pricePath[ExerciseDates[0]] });
        }

        public override IOption Clone(OptionExercise exercise)
        {
            var newExerciseSchedule = (exercise == OptionExercise.European) ? new Date[] { ExerciseDates.Last() } : ExerciseDates;
            return new LookbackOption(
                startDate: this.StartDate,
                maturityDate: this.UnderlyingMaturityDate,
                exercise: exercise,
                strikeStyle: this.StrikeStyle,
                optionType: this.OptionType,
                strike: this.Strike,
                underlyingInstrumentType: this.UnderlyingProductType,
                calendar: this.Calendar,
                dayCount: this.DayCount,
                payoffCcy: this.PayoffCcy,
                settlementCcy: this.SettlementCcy,
                fixings: this.Fixings,
                exerciseDates: newExerciseSchedule,
                observationDates: this.ObservationDates,
                notional: this.Notional,
                settlementGap: this.SettlmentGap,
                optionPremiumPaymentDate: this.OptionPremiumPaymentDate,
                optionPremium: this.OptionPremium,
                isMoneynessOption: this.IsMoneynessOption,
                initialSpotPrice: this.InitialSpotPrice,
                dividends: this.Dividends,
                hasNightMarket: this.HasNightMarket,
                commodityFuturesPreciseTimeMode: this.CommodityFuturesPreciseTimeMode);
        }
    }

}
