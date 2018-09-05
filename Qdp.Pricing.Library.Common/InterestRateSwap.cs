using System;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.Pricing.Library.Common.Utilities.Coupons;

namespace Qdp.Pricing.Library.Common
{
	// same currency, same index, fixed to floating swap
	public class InterestRateSwap : ICashflowInstrument, ICalibrationSupportedInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "InterestRateSwap"; } }
        public Date StartDate
		{
			get { return FixedLeg.StartDate; }
		}

		public Date UnderlyingMaturityDate
		{
			get { return FixedLeg.UnderlyingMaturityDate; }
		}

		public double Notional
		{
			get { return Math.Abs(FixedLeg.Notional); }
            set { FixedLeg.Notional = value; FloatingLeg.Notional = value; }
		}

		public DayGap SettlmentGap { get; private set; }
		public SwapLeg FixedLeg { get; private set; }
		public SwapLeg FloatingLeg { get; private set; }
		public SwapDirection SwapDirection { get; private set; }

		public InterestRateSwap(
			SwapLeg fixedLeg,
			SwapLeg floatingLeg,
			SwapDirection swapDirection,
			string tenor = null)
		{
			FixedLeg = fixedLeg;
			FloatingLeg = floatingLeg;

			if (FixedLeg.StartDate != FloatingLeg.StartDate || FixedLeg.UnderlyingMaturityDate != FloatingLeg.UnderlyingMaturityDate
				 || !FixedLeg.Notional.AlmostEqual(-FloatingLeg.Notional) || FixedLeg.NotionalExchange != FloatingLeg.NotionalExchange)
			{
				throw new PricingBaseException("Interest rate swap fixed leg and floating leg mismatches");
			}

			SwapDirection = swapDirection;

			Tenor = tenor ?? string.Format("{0}{1}", (int)(UnderlyingMaturityDate - StartDate), "D");
		}

		public Cashflow[] GetCashflows(IMarketCondition market, bool netted = true)
		{
			var fixedCashflows = FixedLeg.GetCashflows(market);
			var floatingCashflows = FloatingLeg.GetCashflows(market);

			if (!netted)
			{
				return fixedCashflows.Union(floatingCashflows).ToArray();
			}
			else
			{
				return fixedCashflows.Union(floatingCashflows)
					.GroupBy(cf => new { cf.AccrualStartDate, cf.AccrualEndDate, cf.PaymentDate, cf.PaymentCurrency })
					.Select(item => new Cashflow(item.Key.AccrualStartDate, item.Key.AccrualEndDate, item.Key.PaymentDate, item.Sum(entry => entry.PaymentAmount), item.Key.PaymentCurrency, CashflowType.Coupon, false, market.DiscountCurve.Value.GetDf(item.Key.PaymentDate), null))
					.OrderBy(cf => cf.PaymentDate)
					.ToArray();
			}
		}

		public double GetAccruedInterest(Date calcDate, IMarketCondition market, bool isEod = true)
		{
			return FixedLeg.GetAccruedInterest(calcDate, market, isEod) + FloatingLeg.GetAccruedInterest(calcDate, market, isEod);
		}

		public string Tenor { get; private set; }
		public Date GetCalibrationDate()
		{
			return FixedLeg.PaymentBda.Adjust(FixedLeg.Calendar, UnderlyingMaturityDate);
		}

		public ICalibrationSupportedInstrument Bump(int bp)
		{
			return new InterestRateSwap(FixedLeg.Bump(bp), FloatingLeg, SwapDirection);
		}

		public ICalibrationSupportedInstrument Bump(double resetRate)
		{
			return this;
		}

		public ICalibrationSupportedInstrument Stretch(Date startDate, Date maturityDate)
		{
			return new InterestRateSwap(FixedLeg.Stretch(startDate, maturityDate), FloatingLeg.Stretch(startDate, maturityDate), SwapDirection);
		}

		public double ModelValue(IMarketCondition market, MktInstrumentCalibMethod calibMethod = MktInstrumentCalibMethod.Default)
		{
			var fixedPayRate = FixedLeg.GetPaymentRates(market);
			var fixedRate = ((FixedCoupon)FixedLeg.Coupon).FixedRate;

			var discountCurve = market.DiscountCurve.Value;
			var fixedLegAccumulatedDf = fixedPayRate
				.Where(x => x.Item1 > market.ValuationDate)
				.Sum(x => (x.Item2 / fixedRate * discountCurve.GetDf(x.Item1)));

			if (calibMethod == MktInstrumentCalibMethod.Default || calibMethod == MktInstrumentCalibMethod.IrsFloatingPvConst1)
			{
				// fair rate
				return (1.0 * discountCurve.GetDf(StartDate) - 1.0 * discountCurve.GetDf(fixedPayRate.Last().Item1)) / fixedLegAccumulatedDf; 
			}
			else if(calibMethod == MktInstrumentCalibMethod.IrsFloatingPvReal)
			{
				var floatingPayRate = FloatingLeg.GetPaymentRates(market);
				var floatingPv = floatingPayRate
					.Where(x => x.Item1 > market.ValuationDate)
					.Sum(x => x.Item2*market.DiscountCurve.Value.GetDf(x.Item1));
				return floatingPv/fixedLegAccumulatedDf;
			}

			throw new PricingLibraryException("Calibration method " + calibMethod + " is not recognized");
		}
	}
}
