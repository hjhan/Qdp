using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Curves;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market.Spread;
using Qdp.Pricing.Library.Common.Utilities;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;

namespace Qdp.Pricing.Library.Common.Market
{
	sealed public class YieldCurve : IYieldCurve
	{
		public String Name { get; private set; }
		public Date ReferenceDate { get; private set; }
		public CurrencyCode Currency { get; private set; }
		public IMarketInstrument[] MarketInstruments { get; private set; }
        public Dictionary<string, IMarketInstrument> MarketInstrumentsByTenor { get; private set; }

        public Tuple<Date, double>[] KeyPoints { get; private set; }
		public string[] KeyTenors { get; private set; }
		public BusinessDayConvention Bda { get; private set; }
		public IDayCount DayCount { get; private set; }
		public ICalendar Calendar { get; private set; }
		public Interpolation Interpolation { get; private set; }
		public ISpread Spread { get; private set; }
		public IMarketCondition BaseMarket { get; private set; }
		public YieldCurveTrait Trait { get; private set; }

		public Compound Compound { get; private set; }
		public Expression<Func<IMarketCondition, object>>[] CalibrateMktUpdateCondition;
        public InstrumentCurveDefinition RawDefinition { get; private set; }
        private readonly Dictionary<string, double> InputRateByTenor;

		//private readonly Curve<Date> _curve;
		private readonly Curve<double> _curveXInYears;

        public double this[string tenor]
        {
            get {
                return InputRateByTenor[tenor];
            }
                
            set { return;  }
        }

        //swap curve building for both pricing and pnl,   bond curve building only,  not for pnl
		public YieldCurve(
			string name,
			Date referenceDate,
			MarketInstrument[] marketInstruments,
			BusinessDayConvention bda,
			IDayCount dayCount,
			ICalendar calendar,
			CurrencyCode currency,
			Compound compound,
			Interpolation interpolation,
			YieldCurveTrait trait,
			IMarketCondition baseMarket = null,
			Expression<Func<IMarketCondition, object>>[] calibrateMktUpdateCondition = null,
			ISpread spread = null,
			Date[] knotPoints = null,
			string[] keyTenors = null,
            InstrumentCurveDefinition rawDefinition = null
			)
		{
			Name = name;
			ReferenceDate = referenceDate;
			Currency = currency;
			Bda = bda;
			DayCount = dayCount;
			Compound = compound;
			Calendar = calendar;
			Interpolation = interpolation;
			Trait = trait;
            RawDefinition = rawDefinition;
			var tempMarketInstruments = marketInstruments.OrderBy(x => x.Instrument.UnderlyingMaturityDate).ToArray();
			var uniqueTenorMktInstruments = new List<MarketInstrument> { tempMarketInstruments[0] };
			for (var i = 1; i < tempMarketInstruments.Length; ++i)
			{
				if (tempMarketInstruments[i].Instrument.GetCalibrationDate() != tempMarketInstruments[i - 1].Instrument.GetCalibrationDate())
				{
					uniqueTenorMktInstruments.Add(tempMarketInstruments[i]);
				}
			}
			MarketInstruments = uniqueTenorMktInstruments.ToArray();
            InputRateByTenor = MarketInstruments.ToDictionary(p => p.Instrument.Tenor, p => p.TargetValue);

            BaseMarket = baseMarket;
			Spread = spread ?? new ZeroSpread(0.0);
			CalibrateMktUpdateCondition = calibrateMktUpdateCondition;

			if (MarketInstruments.Any(x => x.Instrument is Bond))
			{
				var err = double.NaN;
				KeyPoints = BondCurveCalibrator.Calibrate(Name, ReferenceDate, MarketInstruments.ToArray(), Bda, DayCount, Calendar, Compound, Interpolation, Trait, Currency, knotPoints, out err, baseMarket, CalibrateMktUpdateCondition);
				fittingError = err;
			}
			else
			{
				KeyPoints = YieldCurveCalibrator.Calibrate(name, ReferenceDate, MarketInstruments.ToArray(), Bda, DayCount, Calendar, Compound, Interpolation, Trait, Currency, baseMarket, CalibrateMktUpdateCondition);
			}

			
			//if (KeyPoints.Select(x => x.Item1).Any(z => z is Date))
			//{
			//	_curve = new Curve<Date>(ReferenceDate, KeyPoints, x => x.ToOADate(), interpolation);
			//}
			_curveXInYears = new Curve<double>(0.0,
				KeyPoints.Select(x => Tuple.Create(DayCount.CalcDayCountFraction(ReferenceDate, x.Item1), x.Item2)).ToArray(),
				x => x,
				Interpolation);
		}

        //internal use + keyPoint manipulation, i.e. solving zspread 
        //Note: this api will bypass curve calibration
        public YieldCurve(
            string name,
            Date referenceDate,
            Tuple<Date, double>[] keyPoints,
            BusinessDayConvention bda,
            IDayCount dayCount,
            ICalendar calendar,
            CurrencyCode currency,
            Compound compound,
            Interpolation interpolation,
            YieldCurveTrait trait,
            IMarketCondition baseMarket = null,
            Expression<Func<IMarketCondition, object>>[] calibrateMktUpdateCondition = null,
            ISpread spread = null,
            string[] keyTenors = null,
            InstrumentCurveDefinition rawDefinition = null
            )
        {
            Name = name;
            ReferenceDate = referenceDate;
            Currency = currency;
            Bda = bda;
            DayCount = dayCount;
            Compound = compound;
            Calendar = calendar;
            Interpolation = interpolation;
            Trait = trait;
            BaseMarket = baseMarket;
            Spread = spread ?? new ZeroSpread(0.0);
            CalibrateMktUpdateCondition = calibrateMktUpdateCondition;
            MarketInstruments = null;
            KeyPoints = keyPoints.OrderBy(x => x.Item1).ToArray();
            KeyTenors = keyTenors ?? KeyPoints.Select(x => new Term(x.Item1 - ReferenceDate, Period.Day)).Select(x => x.ToString()).ToArray();
            RawDefinition = rawDefinition;

            InputRateByTenor = KeyPoints.Select(x => Tuple.Create<string, double> (new Term(x.Item1 - ReferenceDate, Period.Day).ToString(), x.Item2)).
                ToDictionary(v => v.Item1, v=>v.Item2 );

            //_curve = new Curve<Date>(ReferenceDate, KeyPoints, x => x.ToOADate(), interpolation);
            _curveXInYears = new Curve<double>(0.0,
                KeyPoints.Select(x => Tuple.Create(DayCount.CalcDayCountFraction(ReferenceDate, x.Item1), x.Item2)).ToArray(),
                x => x,
                Interpolation);
        }


        //for bond pnl calculation,  directly setting key points, no curve calibration
        //Note: this api will bypass curve calibration 
        public YieldCurve(
			string name,
			Date referenceDate,
			Tuple<ITerm, double>[] keyPoints,
			BusinessDayConvention bda,
			IDayCount dayCount,
			ICalendar calendar,
			CurrencyCode currency,
			Compound compound,
			Interpolation interpolation,
			YieldCurveTrait trait,
			IMarketCondition baseMarket = null,
			Expression<Func<IMarketCondition, object>>[] calibrateMktUpdateCondition = null,
			ISpread spread = null,
			string[] keyTenors = null,
            InstrumentCurveDefinition rawDefinition = null
			)
		{
			Name = name;
			ReferenceDate = referenceDate;
			Currency = currency;
			Bda = bda;
			DayCount = dayCount;
			Compound = compound;
			Calendar = calendar;
			Interpolation = interpolation;
			Trait = trait;
			BaseMarket = baseMarket;
			Spread = spread ?? new ZeroSpread(0.0);
			CalibrateMktUpdateCondition = calibrateMktUpdateCondition;
            MarketInstruments = null;
            RawDefinition = rawDefinition;

            KeyPoints = keyPoints.Select(x => Tuple.Create(x.Item1.Next(ReferenceDate), x.Item2)).ToArray();
            InputRateByTenor = keyPoints.Select(x => Tuple.Create<string, double>(x.Item1.ToString(), x.Item2)).
                ToDictionary(v => v.Item1, v => v.Item2);
            KeyTenors = keyTenors ?? keyPoints.Select(x => x.Item1.ToString()).ToArray();
            //_curve = new Curve<Date>(ReferenceDate, KeyPoints, x => x.ToOADate(), interpolation);
            _curveXInYears = new Curve<double>(0.0,
				KeyPoints.Select(x => Tuple.Create(DayCount.CalcDayCountFraction(ReferenceDate, x.Item1), x.Item2)).ToArray(),
				x => x,
				Interpolation);
		}

        //TODO: consider deprecating this, because no one is using it
		public double GetCompoundedRate(Date date)
		{
			var t = DayCount.CalcDayCountFraction(ReferenceDate, date);
			return GetCompoundedRate(t);
		}

        //TODO: consider deprecating this, because no one is using it
        public double GetCompoundedRate(Date startDate, Date endDate)
		{
			var t0 = DayCount.CalcDayCountFraction(ReferenceDate, startDate);
			var t1 = DayCount.CalcDayCountFraction(ReferenceDate, endDate);
			return GetCompoundedRate(t1) / GetCompoundedRate(t0);
		}

		public double GetCompoundedRate2(Date startDate, Date endDate)
		{
			var t = DayCount.CalcDayCountFraction(startDate, endDate);
			return GetCompoundedRate(t);
		}

		public double GetCompoundedRate(double t)
		{
			if (t.IsAlmostZero() || t < 0.0)
			{
				return 1.0;
			}
			if (Trait == YieldCurveTrait.ForwardCurve)
			{
				return Math.Exp(_curveXInYears.GetIntegral(t));
			}
			if (Trait == YieldCurveTrait.DiscountCurve)
			{
				return 1.0/_curveXInYears.GetValue(t);
			}
			return Compound.CalcCompoundRate(t, GetSpotRate(t));
		}

		public double GetDf(Date date)
		{
			var t = DayCount.CalcDayCountFraction(ReferenceDate, date);
			return GetDf(t);
		}

		public double GetDf(Date startDate, Date endDate)
		{
			var t0 = DayCount.CalcDayCountFraction(ReferenceDate, startDate);
			var t1 = DayCount.CalcDayCountFraction(ReferenceDate, endDate);
			return GetDf(t1) / GetDf(t0);
		}

		public double GetDf(double t)
		{
			if (t.IsAlmostZero() || t < 0.0)
			{
				return 1.0;
			}
			if (Trait == YieldCurveTrait.DiscountCurve)
			{
				return _curveXInYears.GetValue(t);
			}
			return 1.0/GetCompoundedRate(t);
		}

		public double GetSpotRate(Date date)
		{
			var t = DayCount.CalcDayCountFraction(ReferenceDate, date);
			return GetSpotRate(t);

		}

		public double GetSpotRate(double t)
		{
			if (Trait == YieldCurveTrait.SpotCurve)
			{
				return _curveXInYears.GetValue(t) + Spread.GetValue(t);
			}

			var df = GetDf(t);
			return Compound.CalcRateFromDf(df, t);
		}

		public double GetForwardRate(Date startDate, ITerm tenor, Compound compound = Compound.Simple, IDayCount dayCount = null)
		{
			var fixingEndDate = Bda.Adjust(Calendar, tenor.Next(startDate));
			return GetForwardRate(startDate, fixingEndDate, compound, dayCount);
		}

		public double GetForwardRate(Date startDate, Date endDate, Compound compound = Compound.Simple, IDayCount dayCount = null)
		{
			var t0 = DayCount.CalcDayCountFraction(ReferenceDate, startDate);
			var t1 = DayCount.CalcDayCountFraction(ReferenceDate, endDate);
			return GetForwardRate(t0, t1-t0, compound, dayCount);
		}

		public double GetForwardRate(double t0, double dt, Compound compound = Compound.Simple, IDayCount dayCount = null)
		{
			if (dt <= 0.0)
			{
				return _curveXInYears.GetValue(t0) + Spread.GetValue(t0);
			}

			if (t0.IsAlmostZero() && dt.IsAlmostZero())
			{
				return GetForwardRate(0, 0.0001, compound, dayCount);
			}
			var df = GetDf(t0 + dt) / GetDf(t0);

			return compound.CalcRateFromDf(df, dt);
		}

		public double GetInstantaneousForwardRate(double t, Compound compound = Compound.Simple)
		{
			return GetForwardRate(t, 0.0001, compound);
		}

		public double ZeroRate(Date startDate, Date endDate, Compound compound = Compound.Continuous)
		{
			var df = GetDf(startDate, endDate);

			return compound.CalcRateFromDf(df, DayCount.CalcDayCountFraction(startDate, endDate));
		}

		public string[] GetKeyTenors()
		{
			return MarketInstruments != null
				? MarketInstruments.Select(x => x.Instrument.Tenor).Select(x => x.ToString()).ToArray()
				: KeyTenors; 
			//KeyPoints.Select(x => new Term(x.Item1 - ReferenceDate, Period.Day)).Select(x => x.ToString()).ToArray();
		}

		public CurrencyCode GetBaseCurrency()
		{
			if (BaseMarket != null && BaseMarket.FgnDiscountCurve.HasValue)
			{
				return BaseMarket.FgnDiscountCurve.Value.Currency;
			}

			return Currency;
		}

		public double GetFxRate(Date date)
		{
			if (BaseMarket == null || !BaseMarket.FgnDiscountCurve.HasValue)
			{
				return 1.0;
			}

			var fxSpot = BaseMarket.FxSpots.Value[0];
			if (BaseMarket.FgnDiscountCurve.Value.Currency != fxSpot.FgnCcy)
			{
				throw new PricingBaseException("Fx rate foreign currency must be the same as base curve's currency");
			}


			return fxSpot.FxRate * GetDf(date, fxSpot.UnderlyingMaturityDate) / BaseMarket.FgnDiscountCurve.Value.GetDf(date, fxSpot.UnderlyingMaturityDate);
		}

		public IYieldCurve Shift(int bp)
		{
			if (MarketInstruments == null)
            {
                return new YieldCurve(
                    Name,
                    ReferenceDate,
                    KeyPoints.Select(x => Tuple.Create(x.Item1, x.Item2 + bp * 0.0001)).ToArray(),
                    Bda,
                    DayCount,
                    Calendar,
                    Currency,
                    Compound,
                    Interpolation,
                    Trait,
                    BaseMarket,
                    CalibrateMktUpdateCondition,
                    Spread
                    );
            }
            else
            {
                return new YieldCurve(
					Name,
					ReferenceDate,
					MarketInstruments.Select(x => new MarketInstrument(x.Instrument.Bump(bp), x.TargetValue + bp * 0.0001, x.CalibMethod)).ToArray(),
					Bda,
					DayCount,
					Calendar,
					Currency,
					Compound,
					Interpolation,
					Trait,
					BaseMarket,
					CalibrateMktUpdateCondition,
					null
					)
				{
					Spread = Spread
				};
			}
		}

		public IYieldCurve BumpKeyRate(int index, int bp)
		{
			if (index < 0 || index > KeyPoints.Count() - 1)
			{
				throw new IndexOutOfRangeException("YieldCurve");
			}

			
			if (MarketInstruments == null)
			{
				//var offset = MarketInstruments == null ? 0 : KeyPoints.Length - MarketInstruments.Length;
				//var additionalBumpIndex = (index == 0 && offset > 0) ? 0 : Int32.MaxValue;
				return new YieldCurve(
					Name,
					ReferenceDate,
					KeyPoints.Select((x, i) => i == index ? Tuple.Create(x.Item1, x.Item2 + bp * 0.0001) : x).ToArray(),
					Bda,
					DayCount,
					Calendar,
					Currency,
					Compound,
					Interpolation,
					Trait,
					BaseMarket,
					CalibrateMktUpdateCondition,
					Spread
					);
			}
			else
			{
				return new YieldCurve(
					Name,
					ReferenceDate,
					MarketInstruments.Select((x, i) => i == index ? new MarketInstrument(x.Instrument.Bump(bp), x.TargetValue + bp * 0.0001, x.CalibMethod) : x as MarketInstrument).ToArray(),
					Bda,
					DayCount,
					Calendar,
					Currency,
					Compound,
					Interpolation,
					Trait,
					BaseMarket,
					CalibrateMktUpdateCondition,
					null
					)
				{
					Spread = Spread
				};
			}
		}

		public IYieldCurve BumpKeyRate(int index, double resetRate)
		{
			if (index < 0 || index > KeyPoints.Count() - 1)
			{
				throw new IndexOutOfRangeException("YieldCurve");
			}


			if (MarketInstruments == null)
			{
				//var offset = MarketInstruments == null ? 0 : KeyPoints.Length - MarketInstruments.Length;
				//var additionalBumpIndex = (index == 0 && offset > 0) ? 0 : Int32.MaxValue;
				return new YieldCurve(
					Name,
					ReferenceDate,
					KeyPoints.Select((x, i) => i == index ? Tuple.Create(x.Item1, resetRate) : x).ToArray(),
					Bda,
					DayCount,
					Calendar,
					Currency,
					Compound,
					Interpolation,
					Trait,
					BaseMarket,
					CalibrateMktUpdateCondition,
					Spread
					);
			}
			else
			{
				return new YieldCurve(
					Name,
					ReferenceDate,
					MarketInstruments.Select((x, i) => i == index ? new MarketInstrument(x.Instrument.Bump(resetRate), resetRate, x.CalibMethod) : x as MarketInstrument).ToArray(),
					Bda,
					DayCount,
					Calendar,
					Currency,
					Compound,
					Interpolation,
					Trait,
					BaseMarket,
					CalibrateMktUpdateCondition,
					null
					)
				{
					Spread = Spread
				};
			}
		}

		public IYieldCurve BumpKeyRate(int start, int middle, int end, int bp)
		{
			var newKeyPoints = new List<Tuple<Date, double>>();
			var newMarketInstrumetns = new List<MarketInstrument>();
			for (var i = 0; i < KeyPoints.Length; ++i)
			{
				double rateBump = 0.0;
				if (i > start && i <= middle)
				{
					rateBump = bp * 0.0001 / (KeyPoints[middle].Item1 - KeyPoints[start].Item1) *(KeyPoints[i].Item1 - KeyPoints[start].Item1);
				}

				if (i > middle && i <= end)
				{
					rateBump = bp * 0.0001 - bp * 0.0001 / (KeyPoints[end].Item1 - KeyPoints[middle].Item1) * (KeyPoints[i].Item1 - KeyPoints[middle].Item1);
					newKeyPoints.Add(Tuple.Create(KeyPoints[i].Item1, KeyPoints[i].Item2 + rateBump));
				}
				if (MarketInstruments != null)
				{
					newMarketInstrumetns.Add(new MarketInstrument(MarketInstruments[i].Instrument, MarketInstruments[i].TargetValue + rateBump, MarketInstruments[i].CalibMethod));
				}
			}

			if (MarketInstruments != null)
			{
				return new YieldCurve(
					Name,
					ReferenceDate,
					newKeyPoints.ToArray(),
					Bda,
					DayCount,
					Calendar,
					Currency,
					Compound,
					Interpolation,
					Trait,
					BaseMarket,
					CalibrateMktUpdateCondition,
					Spread
					);
			}
			else
			{
				return new YieldCurve(
					Name,
					ReferenceDate,
					newMarketInstrumetns.ToArray(),
					Bda,
					DayCount,
					Calendar,
					Currency,
					Compound,
					Interpolation,
					Trait,
					BaseMarket,
					CalibrateMktUpdateCondition,
					null
					)
				{
					Spread = Spread
				};

			}
		}

		public IYieldCurve GetSpreadedCurve(ISpread spread)
		{
			return new YieldCurve(
				Name,
				ReferenceDate,
				KeyPoints,
				Bda,
				DayCount,
				Calendar,
				Currency,
				Compound,
				Interpolation,
				Trait,
				BaseMarket,
				CalibrateMktUpdateCondition,
				spread,
				KeyTenors
				)
			{
				MarketInstruments = MarketInstruments
			};
		}

		public static IYieldCurve GetConstRateCurve(IYieldCurve yieldCurve, double constRate)
		{
			return new YieldCurve(
				yieldCurve.Name,
				yieldCurve.ReferenceDate,
				yieldCurve.KeyPoints.Select((x, i) => Tuple.Create(x.Item1, constRate)).ToArray(),
				yieldCurve.Bda,
				yieldCurve.DayCount,
				yieldCurve.Calendar,
				yieldCurve.Currency,
				yieldCurve.Compound,
				yieldCurve.Interpolation,
				yieldCurve.Trait,
				yieldCurve.BaseMarket,
				((YieldCurve)yieldCurve).CalibrateMktUpdateCondition,
				yieldCurve.Spread
				);
		}

		public IYieldCurve UpdateReferenceDate(Date referenceDate)
		{
			return new YieldCurve(
				Name,
				referenceDate,
				KeyPoints,
				Bda,
				DayCount,
				Calendar,
				Currency,
				Compound,
				Interpolation,
				Trait,
				BaseMarket,
				CalibrateMktUpdateCondition,
				Spread
				);
		}

		public double fittingError { get; private set; }
	}
}
