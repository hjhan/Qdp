using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common
{
	public class Forward<TUnderlying> : IForward<TUnderlying>
		where TUnderlying :  IUnderlyingInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "Forward"; } }
        public Date StartDate { get; private set; }
		public Date UnderlyingMaturityDate { get; private set; }
		public DayGap SettlmentGap { get; private set; }
		public double Notional { get; set; }
		public double Strike { get; private set; }
		public TUnderlying Underlying { get; private set; }
		public CurrencyCode Currency { get; private set; }

		public Forward(Date startDate,
			Date maturityDate,
			double notional,
			double strike,
			TUnderlying underlying,
			CurrencyCode currency,
			DayGap settlementGap = null)
		{
			StartDate = startDate;
			UnderlyingMaturityDate = maturityDate;
			Notional = notional;
			Strike = strike;
			Underlying = underlying;
			Currency = currency;
			SettlmentGap = settlementGap ?? new DayGap("+0BD");
		} 

		public Cashflow[] GetReplicatingCashflows(IMarketCondition market)
		{
			if (UnderlyingMaturityDate <= market.ValuationDate)
			{
				return new Cashflow[0];
			}

			var cfs = new List<Cashflow>
			{
				new Cashflow(null, null, market.ValuationDate, Underlying == null ? market.SpotPrices.Value.Values.First(): Underlying.GetSpotPrice(market), Currency, CashflowType.Net, true, market.DiscountCurve.Value.GetDf(market.ValuationDate), null)
			};

			if(Underlying != null && Underlying is ICashflowInstrument){
				var underlyingCf = (Underlying as ICashflowInstrument).GetCashflows(market, true)
					.Select(x => new Cashflow(x.AccrualStartDate, x.AccrualEndDate, x.PaymentDate, -x.PaymentAmount, x.PaymentCurrency, x.CashflowType, x.IsFixed, market.DiscountCurve.Value.GetDf(market.ValuationDate), x.CalculationDetails));
				cfs.AddRange(underlyingCf.Where(cf => cf.PaymentDate > market.ValuationDate && cf.PaymentDate < UnderlyingMaturityDate));
			}
			cfs.Add(new Cashflow(UnderlyingMaturityDate, UnderlyingMaturityDate, UnderlyingMaturityDate, -Strike, Currency, CashflowType.Net, true, market.DiscountCurve.Value.GetDf(UnderlyingMaturityDate), null));
			return cfs.Select(x => new Cashflow(x.AccrualStartDate, x.AccrualEndDate, x.PaymentDate, x.PaymentAmount * Notional / (Underlying == null ? 1.0 : Underlying.Notional), x.PaymentCurrency, x.CashflowType, x.IsFixed, market.DiscountCurve.Value.GetDf(x.PaymentDate), x.CalculationDetails)).ToArray();
		}
	}
}
