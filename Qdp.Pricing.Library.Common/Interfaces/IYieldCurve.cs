using System;
using System.Security.Cryptography.X509Certificates;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Curves.Interfaces;

namespace Qdp.Pricing.Library.Common.Interfaces
{
	public interface IYieldCurve
	{
		String Name { get; }
		Date ReferenceDate { get; }
		CurrencyCode Currency { get; }
		IMarketInstrument[] MarketInstruments { get; }
		Tuple<Date, double>[] KeyPoints { get; }
		string[] KeyTenors { get; }
		BusinessDayConvention Bda { get; }
		IDayCount DayCount { get; }
		ICalendar Calendar { get; }
		Compound Compound { get; }
		Interpolation Interpolation { get; }
		ISpread Spread { get; }
		IMarketCondition BaseMarket { get; }
		YieldCurveTrait Trait { get; }
		double GetCompoundedRate(Date date);
		double GetCompoundedRate(Date startDate, Date endDate);
		double GetCompoundedRate2(Date startDate, Date endDate);
		double GetCompoundedRate(double t);
		double GetDf(Date date);
		double GetDf(Date startDate, Date endDate);
		double GetDf(double t);
		double GetSpotRate(Date date);
		double GetSpotRate(double t);
		double GetForwardRate(Date startDate, ITerm tenor, Compound compound = Compound.Simple, IDayCount dayCount = null);
		double GetForwardRate(Date startDate, Date endDate, Compound compound = Compound.Simple, IDayCount dayCount = null);
		double GetForwardRate(double t0, double dt, Compound compound = Compound.Simple, IDayCount dayCount = null);
		double GetInstantaneousForwardRate(double t, Compound compound = Compound.Simple);
		double ZeroRate(Date startDate, Date endDate, Compound compound = Compound.Continuous);
		CurrencyCode GetBaseCurrency();
		double GetFxRate(Date date);
		IYieldCurve Shift(int bp);
		IYieldCurve BumpKeyRate(int index, int bp);
		IYieldCurve BumpKeyRate(int index, double resetRate);
		IYieldCurve BumpKeyRate(int start, int middle, int end, int bp);
		IYieldCurve GetSpreadedCurve(ISpread spread);
		string[] GetKeyTenors();
		IYieldCurve UpdateReferenceDate(Date referenceDate);
		double fittingError { get; }
        double this [string tenor] { get; }
    }
}
