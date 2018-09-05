using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Equity.Options;

namespace Qdp.Pricing.Library.Exotic
{
	public class ConvertibleBond : ICashflowInstrument
	{
        public string Id { get; private set; }
        public string TypeName { get { return "ConvertibleBond"; } }
        public Bond Bond { get; private set; }
		public VanillaOption ConversionOption { get; private set; }
		public double ConversionRatio { get; private set; }
		public VanillaOption[] EmbeddedOptions { get; private set; }
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

		public ConvertibleBond(Bond bond, 
			VanillaOption conversionOptions,
			VanillaOption[] embeddedOptions,
			PriceQuoteType[] strikePriceType
			)
		{
			Bond = bond;
			ConversionOption = conversionOptions;
		    if (ConversionOption == null)
		    {
		        ConversionRatio = double.NaN;
		    }
		    else
		    {
		        ConversionRatio = Bond.Notional / ConversionOption.Strike;
		    }
		    EmbeddedOptions = embeddedOptions;
			StrikePriceType = strikePriceType;
		} 

		public Cashflow[] GetCashflows(IMarketCondition market, bool netted = true)
		{
			throw new System.NotImplementedException();
		}

		public double GetAccruedInterest(Date calcDate, IMarketCondition market, bool isEod = true)
		{
			return Bond.GetAccruedInterest(calcDate, market, isEod);
		}
	}
}
