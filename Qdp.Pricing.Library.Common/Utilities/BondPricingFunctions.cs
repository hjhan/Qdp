using System;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Foundation.Utilities;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market.Spread;
using Qdp.Pricing.Library.Common.MathMethods.Maths;

namespace Qdp.Pricing.Library.Common.Utilities
{
	public class BondPricingFunctions
	{
		public static double ZeroSpread(Bond bond, IMarketCondition market)
		{
			return ZeroSpread(bond.GetCashflows(market, true), market.DiscountCurve.Value, market.ValuationDate, market.MktQuote.Value[bond.Id].Item2);
		}

		public static double ZeroSpread(Cashflow[] cashflows,
			IYieldCurve discountCurve,
			Date valueDate,
			double fullPrice)
		{
			var remainingCfs = cashflows.Where(x => x.PaymentDate > valueDate).ToList();

			if (!remainingCfs.Any()) return 0.0;

			try
			{
				var fcn = new SolveZeroSpread(cashflows, discountCurve, valueDate, fullPrice);
                return BrentZero.Solve(fcn, -0.25, 10, 1e-12);
                //return BrentZero.Solve(fcn, -20, 10, 1e-12);
            }
			catch (Exception ex)
			{
				throw new PricingBaseException("Bond yield does not converge " + ex.GetDetail());
			}
		}


        public static double PriceFromZeroSpread(Cashflow[] cashflows,
            IYieldCurve discountCurve,
            Date valueDate,
            double zspread
            )
        {
            var newCurve = discountCurve.GetSpreadedCurve(new ZeroSpread(zspread));
            var newPrice = cashflows.Where(cf => cf.PaymentDate > valueDate)
                .Sum(cf => cf.PaymentAmount * newCurve.GetDf(valueDate, cf.PaymentDate));
            return newPrice;
        }

        //1bp zspread risk
        public static double ZeroSpreadRisk(Cashflow[] cashflows,
            IYieldCurve discountCurve,
            Date valueDate,
            double zspread
            )
        {
            var curve = discountCurve.GetSpreadedCurve(new ZeroSpread(zspread));
            var price = cashflows.Where(cf => cf.PaymentDate > valueDate)
                .Sum(cf => cf.PaymentAmount * curve.GetDf(valueDate, cf.PaymentDate));

            var shiftedCurve = discountCurve.GetSpreadedCurve(new ZeroSpread(zspread+1e-4));
            var shiftedPrice = cashflows.Where(cf => cf.PaymentDate > valueDate)
                .Sum(cf => cf.PaymentAmount * shiftedCurve.GetDf(valueDate, cf.PaymentDate));

            return (shiftedPrice - price); //TODO: scaling or not?
        }


        public static double GetModifiedDuration(Cashflow[] cashflows,
			IDayCount dayCount,
			Frequency frequency,
			Date startDate,
			Date valueDate,
			double yield,
			TradingMarket tradeingMarket,
			bool irregularPayment = false,
			IBondYieldPricer bondYieldPricer = null)
		{
			const double dy = 1e-4;
			if(bondYieldPricer == null) bondYieldPricer = new BondYieldPricer();
			var priceUp = bondYieldPricer.FullPriceFromYield(cashflows, dayCount, frequency, startDate, valueDate, yield - dy, tradeingMarket, irregularPayment);
			var priceDown = bondYieldPricer.FullPriceFromYield(cashflows, dayCount, frequency, startDate, valueDate, yield + dy, tradeingMarket, irregularPayment);
			return (priceUp - priceDown)/dy/(priceDown + priceUp);
		}

		public static double GetConvexity(Cashflow[] cashflows,
			IDayCount dayCount,
			Frequency frequency,
			Date startDate,
			Date valueDate,
			double yield,
			TradingMarket tradeingMarket,
			bool irregularPayment = false,
			IBondYieldPricer bondYieldPricer = null)
		{
			const double dy = 1e-4;
			if (bondYieldPricer == null) bondYieldPricer = new BondYieldPricer();
			var priceUp = bondYieldPricer.FullPriceFromYield(cashflows, dayCount, frequency, startDate, valueDate, yield - dy, tradeingMarket, irregularPayment);
			var priceDown = bondYieldPricer.FullPriceFromYield(cashflows, dayCount, frequency, startDate, valueDate, yield + dy, tradeingMarket, irregularPayment);
			var price = bondYieldPricer.FullPriceFromYield(cashflows, dayCount, frequency, startDate, valueDate, yield, tradeingMarket, irregularPayment);
			return (priceUp + priceDown - 2*price)/price/dy/dy;
		}
	}

	internal class SolveYtm : IFunctionOfOneVarialbe
	{
		private readonly Cashflow[] _cashflows;
		private readonly IDayCount _dayCount;
		private readonly Frequency _frequency;
		private readonly Date _startDate;
		private readonly Date _valueDate;
		private readonly double _fullPrice;
		private readonly TradingMarket _tradeingMarket;
		private readonly bool _irregularPayment;
		private readonly IBondYieldPricer _bondYieldPricer;

		public SolveYtm(Cashflow[] cashflows,
			IDayCount dayCount,
			Frequency frequency,
			Date startDate,
			Date valueDate,
			double fullPrice,
			TradingMarket tradeingMarket,
			bool irregularPayment,
			IBondYieldPricer bondYieldPricer = null)
		{
			_cashflows = cashflows;
			_dayCount = dayCount;
			_frequency = frequency;
			_startDate = startDate;
			_valueDate = valueDate;
			_fullPrice = fullPrice;
			_tradeingMarket = tradeingMarket;
			_irregularPayment = irregularPayment;
			_bondYieldPricer = bondYieldPricer ?? new BondYieldPricer();
		}
		public double F(double x)
		{
			return _bondYieldPricer.FullPriceFromYield(_cashflows, _dayCount, _frequency, _startDate, _valueDate, x, _tradeingMarket, _irregularPayment) - _fullPrice;
		}
	}

	internal class SolveZeroSpread : IFunctionOfOneVarialbe
	{
		private readonly Cashflow[] _cashflows;
		private readonly IYieldCurve _discountCurve;
		private readonly Date _valueDate;
		private readonly double _fullPrice;

		public SolveZeroSpread(Cashflow[] cashflows,
			IYieldCurve discountCurve,
			Date valueDate,
			double fullPrice)
		{
			_discountCurve = discountCurve;
			_cashflows = cashflows;
			_valueDate = valueDate;
			_fullPrice = fullPrice;
		}
		public double F(double x)
		{
			var newCurve = _discountCurve.GetSpreadedCurve(new ZeroSpread(x));
			return _cashflows.Where(cf => cf.PaymentDate > _valueDate)
				.Sum(cf => cf.PaymentAmount*newCurve.GetDf(_valueDate, cf.PaymentDate))
			       - _fullPrice;
		}
	}
}
