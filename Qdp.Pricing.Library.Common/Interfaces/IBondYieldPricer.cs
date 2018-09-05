using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IBondYieldPricer
	{
		double YieldFromFullPrice(Cashflow[] cashflows,
			IDayCount dayCount,
			Frequency frequency,
			Date startDate,
			Date valueDate,
			double fullPrice,
			TradingMarket tradeingMarket,
			bool irregularPayment = false);

		double FullPriceFromYield(Cashflow[] cashflows,
			IDayCount dayCount,
			Frequency frequency,
			Date startDate,
			Date valueDate,
			double yield,
			TradingMarket tradeingMarket,
			bool irregularPayment = false);

		double GetMacDuration(Cashflow[] cashflows,
			IDayCount dayCount,
			Frequency frequency,
			Date startDate,
			Date valueDate,
			double yield,
			TradingMarket tradingMarket);
	}
}
