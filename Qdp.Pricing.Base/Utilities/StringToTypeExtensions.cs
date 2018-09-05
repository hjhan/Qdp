using System;
using System.Globalization;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Calendar = Qdp.Pricing.Base.Enums.Calendar;

namespace Qdp.Pricing.Base.Utilities
{
	public static class StringToTypeExtensions
	{
		public static T ToEnumType<T>(this string input)
		{
			try
			{
				return (T)Enum.Parse(typeof(T), input, true);
			}
			catch (Exception ex)
			{
				throw new PricingBaseException(string.Format("{0} cannot be converted to type {1}", input, typeof(T).Name));
			}
		}

		public static BondType ToBondType(this string input)
		{
			return input.ToEnumType<BondType>();
		}

		public static FixedDateAdjustedCouponStyle ToFixedDateAdjustedCouponStyle(this string input)
		{
			return input.ToEnumType<FixedDateAdjustedCouponStyle>();
		}

		public static BinaryOptionPayoffType ToBinaryOptionPayoffType(this string input)
		{
			return input.ToEnumType<BinaryOptionPayoffType>();
		}

		public static BinaryOptionReplicationStrategy ToBinaryOptionReplicationStrategy(this string input)
		{
			return input.ToEnumType<BinaryOptionReplicationStrategy>();
		}

		public static BarrierType ToBarrierType(this string input)
		{
			return input.ToEnumType<BarrierType>();
		}

        public static BarrierStatus ToBarrierStatus(this string input)
        {
            return input.ToEnumType<BarrierStatus>();
        }

        public static Position ToPosition(this string input)
        {
            return input.ToEnumType<Position>();
        }
        public static AsianType ToAsianType(this string input)
        {
            return input.ToEnumType<AsianType>();
        }
        public static RainbowType ToRainbowType(this string input)
        {
            return input.ToEnumType<RainbowType>();
        }
        public static StrikeStyle ToStrikeStyle(this string input)
        {
            return input.ToEnumType<StrikeStyle>();
        }

        public static AmortizationType ToAmortizationType(this string input)
		{
			return input.ToEnumType<AmortizationType>();
		}

		public static IndexType ToIndexType(this string input)
		{
			return input.ToEnumType<IndexType>();
		}

		public static YieldCurveTrait ToYieldCurveTrait(this string input)
		{
			return input.ToEnumType<YieldCurveTrait>();
		}

		public static PricingRequest ToPricingRequest(this string input)
		{
			return input.ToEnumType<PricingRequest>();
		}

		public static PriceQuoteType ToPriceQuoteType(this string input)
		{
			return input.ToEnumType<PriceQuoteType>();
		}

		public static DayCount ToDayCount(this string input)
		{
			return input.ToEnumType<DayCount>();
		}

		public static IDayCount ToDayCountImpl(this string input)
		{
			return input.ToEnumType<DayCount>().Get();
		}

		public static Stub ToStub(this string input)
		{
			return input.ToEnumType<Stub>();
		}

		public static TradingMarket ToTradingMarket(this string input)
		{
			return input.ToEnumType<TradingMarket>();
		}

		public static SwapDirection ToSwapDirection(this string input)
		{
			return input.ToEnumType<SwapDirection>();
		}

		public static Calendar ToCalendar(this string input)
		{
			return input.ToEnumType<Calendar>();
		}

		public static BusinessDayConvention ToBda(this string input)
		{
			return input.ToEnumType<BusinessDayConvention>();
		}

		public static Compound ToCompound(this string input)
		{
			return input.ToEnumType<Compound>();
		}

        public static VolSurfaceType ToVolSurfaceType(this string input)
        {
            return input.ToEnumType<VolSurfaceType>();
        }

        public static CouponCompound ToCouponCompound(this string input)
		{
			return input.ToEnumType<CouponCompound>();
		}

		public static MktInstrumentCalibMethod ToCalibMethod(this string input)
		{
			return input.ToEnumType<MktInstrumentCalibMethod>();
		}

		public static ICalendar ToCalendarImpl(this string input)
		{
			return CalendarImpl.Get(input);
		}

		public static DayGap ToDayGap(this string input)
		{
			return new DayGap(input);
		}

		public static Frequency ToFrequency(this string input)
		{
			return input.ToEnumType<Frequency>();
		}

		public static FloatingCouponCalcType ToFloatingCalcType(this string input)
		{
			return input.ToEnumType<FloatingCouponCalcType>();
		}

		public static Direction ToDirection(this string input)
		{
			return input.ToEnumType<Direction>();
		}

		public static Date ToDate(this string input)
		{
			if (string.IsNullOrEmpty(input))
			{
				return null;
			}
			if (input[4] == '-')
			{
				return new Date(DateTime.ParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None));
			}
			if (input[4] == '/')
			{
				return new Date(DateTime.ParseExact(input, "yyyy/M/d", CultureInfo.InvariantCulture, DateTimeStyles.None));
			}
			int value;
			if (int.TryParse(input, out value) && value < 100000)
			{
				return new Date(DateTime.FromOADate(value));
			}

			return new Date(DateTime.ParseExact(input, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None));
		}

		public static T ToType<T>(this string input)
		{
			object dd = input;
			if (typeof (T) == typeof (int))
			{
				dd = Convert.ToInt32(input);
			}
			else if (typeof(T) == typeof(double))
			{
				dd = Convert.ToDouble(input);
			}
			return (T) dd;
		}

		public static CurrencyCode ToCurrencyCode(this string input)
		{
			return input.ToEnumType<CurrencyCode>();
		}

		public static CashflowType ToCashflowType(this string input)
		{
			return input.ToEnumType<CashflowType>();
		}

		public static OptionExercise ToOptionExercise(this string input)
		{
			return input.ToEnumType<OptionExercise>();
		}
        public static BinaryRebateType ToBinaryRebateType(this string input)
        {
            return input.ToEnumType<BinaryRebateType>();
        }
        public static OptionType ToOptionType(this string input)
		{
			return input.ToEnumType<OptionType>();
		}
        public static SpreadType ToSpreadType(this string input)
        {
            return input.ToEnumType<SpreadType>();
        }

        public static ResetStrikeType ToResetStrikeType(this string input)
        {
            return input.ToEnumType<ResetStrikeType>();
        }

        public static InstrumentType ToInstrumentType(this string input)
		{
			return input.ToEnumType<InstrumentType>();
		}

		public static Interpolation ToInterpolation(this string input)
		{
			return input.ToEnumType<Interpolation>();
		}

        public static Interpolation2D ToInterpolation2D(this string input)
        {
            return input.ToEnumType<Interpolation2D>();
        }
    }
}
