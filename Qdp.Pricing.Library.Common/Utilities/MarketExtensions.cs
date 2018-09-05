using System;
using System.Collections.Generic;
using System.Data;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.Utilities
{
	public static class MarketExtensions
	{
		public static Date GetFxSpotDate(Date startDate, 
			DayGap settlement, 
			CurrencyCode fgnCcy, 
			CurrencyCode domCcy, 
			ICalendar fgnCalendar, 
			ICalendar domCalendar)
		{
			//中国外汇交易中心产品指引(外汇市场)V1.8 58页
			var foreignSpotDate = settlement.Get(fgnCalendar, startDate);
			var domesticSpotDate = settlement.Get(domCalendar, startDate);

			var usdCalendar = CalendarImpl.Get(Calendar.Usd);

			if (fgnCcy == CurrencyCode.USD)
			{
				if (!usdCalendar.IsHoliday(domesticSpotDate))
				{
					return domesticSpotDate;
				}
			}

			if (domCcy == CurrencyCode.USD)
			{
				if (!usdCalendar.IsHoliday(foreignSpotDate))
				{
					return foreignSpotDate;
				}
			}

			var guessSettlementDate = foreignSpotDate > domesticSpotDate ? foreignSpotDate : domesticSpotDate;
			if (usdCalendar.IsHoliday(guessSettlementDate) || fgnCalendar.IsHoliday(guessSettlementDate) || domCalendar.IsHoliday(guessSettlementDate))
			{
				//find next biz day for foreign currency and domestic currency
				while (fgnCalendar.IsHoliday(guessSettlementDate) || domCalendar.IsHoliday(guessSettlementDate))
				{
					guessSettlementDate = new Term("1D").Next(guessSettlementDate);
				}
				return guessSettlementDate;
			}

			return guessSettlementDate;
		}

		public static double GetDf(this IMarketCondition market, Date date)
		{
			if (market.DiscountCurve.HasValue && market.DiscountCurve.Value != null)
			{
				return market.DiscountCurve.Value.GetDf(date);
			}
			else
			{
				return 1.0;
			}
		}

		public static double GetFxRate(this IMarketCondition market, Date date, CurrencyCode fgnCcy, CurrencyCode domCcy)
		{
			if (fgnCcy == domCcy)
			{
				return 1.0;
			}

			var discountCurves = new Dictionary<CurrencyCode, IYieldCurve>();
			discountCurves[market.DiscountCurve.Value.Currency] = market.DiscountCurve.Value;
			discountCurves[market.FgnDiscountCurve.Value.Currency] = market.FgnDiscountCurve.Value;

			var baseCcy = market.DiscountCurve.Value.GetBaseCurrency();
			return Math.Round(
				(domCcy == baseCcy ? 1.0 : discountCurves[domCcy].GetFxRate(date))
				/ (fgnCcy == baseCcy ? 1.0 : discountCurves[fgnCcy].GetFxRate(date))
				, 10);
		}

		public static double ConvertCcy(this IMarketCondition market, Date date, double amount, CurrencyCode fromCcy, CurrencyCode toCcy)
		{
			if (fromCcy == toCcy)
			{
				return amount;
			}

			return amount * market.GetFxRate(date, fromCcy, toCcy); 
		}
	}
}
