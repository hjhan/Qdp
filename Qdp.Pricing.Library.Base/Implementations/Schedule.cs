using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Library.Base.Implementations
{
	public class Schedule : IEnumerable<Date>
	{
		private readonly List<Date> _dates;
		public List<bool> IsRegular { get; private set; }
		
		public Schedule(Date startDate, Date endDate, ITerm stepTerm, Stub stub, ICalendar calendar = null, BusinessDayConvention bda = BusinessDayConvention.None, bool stickEom=false)
		{
			var unadjustedDates = new List<Date>();
			IsRegular = new List<bool>();

			if (Convert.ToInt32(stepTerm.Length) == Convert.ToInt32(Term.Infinity.Length))
			{
				if (startDate != endDate)
				{
					unadjustedDates.Add(startDate);
					IsRegular.Add(true);
				}
				unadjustedDates.Add(endDate);
				IsRegular.Add(true);
			}
			else
			{
				switch (stub)
				{
					case Stub.LongEnd:
					case Stub.ShortEnd:
					{
						var numPeriods = 1;
						Date date;
						for (date = startDate; date < endDate; date = stepTerm.Next(startDate, numPeriods++, stickEom))
						{
							unadjustedDates.Add(date);
							IsRegular.Add(true);
						}
						if (date != endDate && stub == Stub.LongEnd)
						{
							if (unadjustedDates.Count == 1)
							{
								unadjustedDates.Add(endDate);
								IsRegular.Add(date == startDate);
							}
							else
							{
								unadjustedDates[unadjustedDates.Count - 1] = endDate;
								IsRegular[IsRegular.Count - 1] = date == startDate;
							}
						}
						else
						{
							unadjustedDates.Add(endDate);
							IsRegular.Add(date == startDate);
						}
						break;
					}
					case Stub.LongStart:
					case Stub.ShortStart:
					{
						var numPeriods = 1;
						Date date;
						for (date = endDate; date > startDate; date = stepTerm.Prev(endDate, numPeriods++, stickEom))
						{
							unadjustedDates.Add(date);
							IsRegular.Add(true);
						}
						if (date != startDate && stub == Stub.LongStart)
						{
							if (unadjustedDates.Count == 1)
							{
								unadjustedDates.Add(startDate);
								IsRegular.Add(date == startDate);
							}
							else
							{
								unadjustedDates[unadjustedDates.Count - 1] = startDate;
								IsRegular[IsRegular.Count - 1] = date == startDate;
							}
						}
						else
						{
							unadjustedDates.Add(startDate);
							IsRegular.Add(date == startDate);
						}
						unadjustedDates.Reverse();
						IsRegular.Reverse();
						break;
					}
				}
			}

			if (bda != BusinessDayConvention.None && calendar != null)
			{
				_dates = new List<Date>();
				foreach (var date in unadjustedDates)
				{
					var adjustedDate = bda.Adjust(calendar, date);
					var finalDate = _dates.LastOrDefault();
					if (finalDate == null || finalDate != adjustedDate)
					{
						_dates.Add(adjustedDate);
					}
				}
			}
			else
			{
				_dates = unadjustedDates;
			}
		}

        public Date[] PeriodInclDate(Date date) {
            var first = _dates.FindIndex(x => x >= date);
            if (first == -1)
                return null;
            else
                return new Date[] { _dates[first - 1], _dates[first] };
        }

		public Schedule(IEnumerable<Date> dates)
		{
			_dates = dates.ToList();
		}

		public IEnumerator<Date> GetEnumerator()
		{
			return _dates.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
