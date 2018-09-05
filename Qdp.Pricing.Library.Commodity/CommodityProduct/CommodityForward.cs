using System;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Interfaces;
using System.Linq;

namespace Qdp.Pricing.Library.Commodity
{
	public class CommodityForward : Forward<IUnderlyingInstrument>, ICalibrationSupportedInstrument
	{
		public CommodityForward(Date startDate, Date maturityDate, double notional, double strike, IUnderlyingInstrument underlying, CurrencyCode currency, DayGap settlementGap = null) 
			: base(startDate, maturityDate, notional, strike, underlying, currency, settlementGap)
		{
			Tenor = Convert.ToInt32(maturityDate - startDate) + "D";

		}

		public CommodityForward(Date startDate, Term tenor, double notional, double strike, IUnderlyingInstrument underlying, CurrencyCode currency, DayGap settlementGap = null)
			: base(startDate, tenor.Next(startDate), notional, strike, underlying, currency, settlementGap)
		{
			Tenor = tenor.ToString();
		}

		public string Tenor { get; private set; }
		public Date GetCalibrationDate()
		{
			return UnderlyingMaturityDate;
		}

		public ICalibrationSupportedInstrument Bump(int bp)
		{
			return this;
		}

		public ICalibrationSupportedInstrument Bump(double resetRate)
		{
			return this;
		}

		public double ModelValue(IMarketCondition market, MktInstrumentCalibMethod calibMethod = MktInstrumentCalibMethod.Default)
		{
			var discountCurve = market.DiscountCurve.Value;
			var dividendCurve = market.DividendCurves.Value.Values.First();

			return market.SpotPrices.Value.Values.First()
			       *dividendCurve.GetDf(market.ValuationDate, UnderlyingMaturityDate)
			       /discountCurve.GetDf(market.ValuationDate, UnderlyingMaturityDate);
		}
	}
}
