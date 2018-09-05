using System;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Implementations;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Options;

namespace Qdp.Pricing.Library.Exotic
{
	public class CallableBond : ICashflowInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "CallableBond"; } }
        public Bond Bond { get; private set; }
		public VanillaOption[] EmbededOptions { get; private set; }
		public PriceQuoteType[] StrikePriceType { get; private set; }
		public Date StartDate
		{
			get { return Bond.StartDate; }
		}
		public Date UnderlyingMaturityDate
		{
			get { return Bond.UnderlyingMaturityDate; }
		}
		public DayGap SettlmentGap
		{
			get { return Bond.SettlmentGap; }
		}
		public double Notional
		{
			get { return Bond.Notional; }
            set { Bond.Notional = value; }
		}

		public IDayCount DayCount
		{
			get { return Bond.PaymentDayCount; }
		}
		public CallableBond(Bond bond, 
			VanillaOption[] embeddedOptions,
			PriceQuoteType[] strikePriceType)
		{
			Bond = bond;
			EmbededOptions = embeddedOptions;
			StrikePriceType = strikePriceType;
		} 

		public Cashflow[] GetCashflows(IMarketCondition market, bool netted = true)
		{
			return Bond.GetCashflows(market, netted);
		}

		public double GetAccruedInterest(Date calcDate, IMarketCondition market, bool isEod = true)
		{
			return Bond.GetAccruedInterest(calcDate, market);
		}

		public Tuple<Date, OptionType, double>[] GetExerciseInfo(IMarketCondition market)
		{
			return EmbededOptions.SelectMany((x, i) => 
				x.ExerciseDates.Select(date => 
					Tuple.Create(date, 
					x.OptionType, 
					x.Strike + (StrikePriceType[i] == PriceQuoteType.Dirty ? 0.0 : GetAccruedInterest(date, market)))
					)).ToArray();
		}
	}
}
