using System;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;


namespace Qdp.Pricing.Base.Implementations
{
    /// <summary>
    /// 日期规则Act360。
    /// 两个日期之间以实际天数计算，一年按360天计算
    /// </summary>
    [Serializable]
	public class Act360 : IDayCount
	{
        /// <summary>
        /// 两个日期之间的天数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <returns>天数</returns>
        public double DaysInPeriod(Date startDate, Date endDate) => endDate - startDate;

        /// <summary>
        /// 两个日期的差，以年为单位
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>日期差</returns>
        public double CalcDayCountFraction(Date startDate, Date endDate, Date referenceStartDate, Date referenceEndDate) => DaysInPeriod(startDate, endDate) / 360;

        /// <summary>
        /// 通过基准日期和日期差，计算目标日期
        /// </summary>
        /// <param name="startDate">基准日期</param>
        /// <param name="dayCountFraction">日期差</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>目标日期</returns>
        public Date CalcEndDateFromDayCountFraction(Date startDate, double dayCountFraction, Date referenceStartDate, Date referenceEndDate) => startDate.AddDays(Convert.ToInt16(dayCountFraction * 360.0));
    }

    /// <summary>
    /// 日期规则Act365。
    /// 两个日期之间以实际天数计算，一年按365天计算
    /// </summary>
    [Serializable]
    public class Act365 : IDayCount
	{
        /// <summary>
        /// 两个日期之间的天数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <returns>天数</returns>
        public double DaysInPeriod(Date startDate, Date endDate) => endDate - startDate;

        /// <summary>
        /// 两个日期的差，以年为单位
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>日期差</returns>
        public double CalcDayCountFraction(Date startDate, Date endDate, Date referenceStartDate, Date referenceEndDate) => DaysInPeriod(startDate, endDate) / 365;

        /// <summary>
        /// 通过基准日期和日期差，计算目标日期
        /// </summary>
        /// <param name="startDate">基准日期</param>
        /// <param name="dayCountFraction">日期差</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>目标日期</returns>
        public Date CalcEndDateFromDayCountFraction(Date startDate, double dayCountFraction, Date referenceStartDate, Date referenceEndDate) => startDate.AddDays(Convert.ToInt16(dayCountFraction * 365.0));
    }

    /// <summary>
    /// 日期规则Act365。
    /// 两个日期之间按实际天数计算，但不考虑闰年日，即2-29都按2-28计算，一年按365天计算。
    /// </summary>
    [Serializable]
    public class Act365NoLeap : IDayCount
	{
        private static int[] monthOffset = new int[]
        {
            0, 31, 59, 90, 120, 151, // Jan - Jun
			181, 212, 243, 273, 304, 334 // Jun - Dec
		};

        /// <summary>
        /// 两个日期之间的天数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <returns>天数</returns>
        public double DaysInPeriod(Date startDate, Date endDate)
		{
			var s1 = startDate.Day + monthOffset[startDate.Month - 1] + (startDate.Year*365);
			var s2 = endDate.Day + monthOffset[endDate.Month - 1] + (endDate.Year*365);

			if (startDate.Month == 2 && startDate.Day == 29)
			{
				--s1;
			}

			if (endDate.Month == 2 && endDate.Day == 29)
			{
				--s2;
			}

			return s2 - s1;
		}

        /// <summary>
        /// 两个日期的差，以年为单位
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>日期差</returns>
		public double CalcDayCountFraction(Date startDate, Date endDate, Date referenceStartDate, Date referenceEndDate)
		{
			return DaysInPeriod(startDate, endDate) / 365;
		}

        /// <summary>
        /// 通过基准日期和日期差，计算目标日期
        /// </summary>
        /// <param name="startDate">基准日期</param>
        /// <param name="dayCountFraction">日期差</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>目标日期</returns>
        public Date CalcEndDateFromDayCountFraction(Date startDate, double dayCountFraction, Date referenceStartDate, Date referenceEndDate)
        {
            var actualDaysInPeriod = dayCountFraction * 365;

            var guessEndDate = startDate.AddDays(Convert.ToInt16(dayCountFraction * 365.0));
            var s1 = startDate.Day + monthOffset[startDate.Month - 1] + (startDate.Year * 365);
            var s2 = guessEndDate.Day + monthOffset[guessEndDate.Month - 1] + (guessEndDate.Year * 365);

            var dayGap = actualDaysInPeriod - (s2 - s1);
            return guessEndDate.AddDays(Convert.ToInt16(dayGap));
        }

    }

    //TODO:  to remove it, useless
    //http://139.196.190.223:8888/browse/QDP-274
    //To match WIND bond pricing
    /// <summary>
    /// 日期规则Act365Wind。
    /// 目前和Act365相同
    /// </summary>
    [Serializable]
    public class Act365Wind : IDayCount
    {
        /// <summary>
        /// 两个日期之间的天数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <returns>天数</returns>
        public double DaysInPeriod(Date startDate, Date endDate)
        {
            return endDate - startDate;
        }

        /// <summary>
        /// 两个日期的差，以年为单位
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>日期差</returns>
        public double CalcDayCountFraction(Date startDate, Date endDate, Date referenceStartDate, Date referenceEndDate)
        {
            return DaysInPeriod(startDate, endDate) / 365;
        }

        /// <summary>
        /// 通过基准日期和日期差，计算目标日期
        /// </summary>
        /// <param name="startDate">基准日期</param>
        /// <param name="dayCountFraction">日期差</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>目标日期</returns>
        public Date CalcEndDateFromDayCountFraction(Date startDate, double dayCountFraction, Date referenceStartDate, Date referenceEndDate)
        {
            return startDate.AddDays(Convert.ToInt16(dayCountFraction * 365.0));
        }
    }

    /// <summary>
    /// 日期规则ActActAfb
    /// </summary>
    [Serializable]
    public class ActActAfb : IDayCount
	{
        /// <summary>
        /// 两个日期之间的天数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <returns>天数</returns>
		public double DaysInPeriod(Date startDate, Date endDate)
		{
			return endDate - startDate;
		}

        /// <summary>
        /// 两个日期的差，以年为单位
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>日期差</returns>
		public double CalcDayCountFraction(Date startDate, Date endDate, Date referenceStartDate, Date referenceEndDate)
		{
			var i = 0;

			while (new Term(i, Period.Year).Next(startDate) < endDate)
			{
				++i;
			}

			var wholeYears = i - 1;
			var fracStart = new Term(wholeYears, Period.Year).Next(startDate);
			var fracEnd = new Term(1, Period.Year).Next(fracStart);
			var s = DaysInPeriod(fracStart, endDate) / DaysInPeriod(fracStart, fracEnd);

			return wholeYears + s;
		}

        /// <summary>
        /// 通过基准日期和日期差，计算目标日期
        /// </summary>
        /// <param name="startDate">基准日期</param>
        /// <param name="dayCountFraction">日期差</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>目标日期</returns>
        public Date CalcEndDateFromDayCountFraction(Date startDate, double dayCountFraction, Date referenceStartDate, Date referenceEndDate)
        {
            var guessEndDate = startDate.AddDays(Convert.ToInt16(dayCountFraction * 365.0));

            var i = 0;
            while (new Term(i, Period.Year).Next(startDate) < guessEndDate)
            {
                ++i;
            }
            var wholeYears = i - 1;
            var fracStart = new Term(wholeYears, Period.Year).Next(startDate);
            var fracEnd = new Term(1, Period.Year).Next(fracStart);

            var days = (dayCountFraction - wholeYears) * DaysInPeriod(fracStart, fracEnd); ;
            return fracStart.AddDays(Convert.ToInt16(days));
        }
    }

    /// <summary>
    /// 日期规则InterBankBond。
    /// 中国银行间市场的日期规则，应使用ActActAfb代替
    /// </summary>
    [Obsolete("Use ActActAfb instead")]
    [Serializable]
    public class InterBankBond : IDayCount
	{
        /// <summary>
        /// 两个日期之间的天数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <returns>天数</returns>
		public double DaysInPeriod(Date startDate, Date endDate)
		{
			return endDate - startDate;
		}

        /// <summary>
        /// 两个日期的差，以年为单位
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>日期差</returns>
		public double CalcDayCountFraction(Date startDate, Date endDate, Date referenceStartDate, Date referenceEndDate)
		{
			var i = 0;

			while (new Term(i, Period.Year).Next(startDate) < endDate)
			{
				++i;
			}

			var wholeYears = i - 1;
			var fracStart = new Term(wholeYears, Period.Year).Next(startDate);
			var fracEnd = new Term(1, Period.Year).Next(fracStart);
			var s = DaysInPeriod(fracStart, endDate)/DaysInPeriod(fracStart, fracEnd);

			return wholeYears + s;
		}

        /// <summary>
        /// 通过基准日期和日期差，计算目标日期
        /// </summary>
        /// <param name="startDate">基准日期</param>
        /// <param name="dayCountFraction">日期差</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>目标日期</returns>
        public Date CalcEndDateFromDayCountFraction(Date startDate, double dayCountFraction, Date referenceStartDate, Date referenceEndDate)
        {
            var guessEndDate = startDate.AddDays(Convert.ToInt16(dayCountFraction * 365.0));

            var i = 0;
            while (new Term(i, Period.Year).Next(startDate) < guessEndDate)
            {
                ++i;
            }
            var wholeYears = i - 1;
            var fracStart = new Term(wholeYears, Period.Year).Next(startDate);
            var fracEnd = new Term(1, Period.Year).Next(fracStart);

            var days = (dayCountFraction - wholeYears) * DaysInPeriod(fracStart, fracEnd); ;
            return fracStart.AddDays(Convert.ToInt16(days));
        }
    }

    /// <summary>
    /// 日期规则Act365M。
    /// 两个日期之间按实际天数计算，但不包含闰年日（2-29），一年按365天计算。
    /// </summary>
    [Serializable]
    public class Act365M : IDayCount
	{
        /// <summary>
        /// 两个日期之间的天数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <returns>天数</returns>
		public double DaysInPeriod(Date startDate, Date endDate)
		{
			var startYear = startDate.Year;
			var endYear = endDate.Year;

			var excludedDayCount = 0;
			for (var year = startYear; year <= endYear; ++year)
			{
				if (Date.IsLeapYear(year))
				{
					var leapDate = new Date(year, 2, 29);
					if (leapDate.Between(startDate, endDate))
					{
						++excludedDayCount;
					}
				}
			}

			return endDate - startDate - excludedDayCount;
		}

        /// <summary>
        /// 两个日期的差，以年为单位
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>日期差</returns>
		public double CalcDayCountFraction(Date startDate, Date endDate, Date referenceStartDate, Date referenceEndDate)
		{
			return DaysInPeriod(startDate, endDate)/365;
		}

        /// <summary>
        /// 通过基准日期和日期差，计算目标日期
        /// </summary>
        /// <param name="startDate">基准日期</param>
        /// <param name="dayCountFraction">日期差</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>目标日期</returns>
        public Date CalcEndDateFromDayCountFraction(Date startDate, double dayCountFraction, Date referenceStartDate, Date referenceEndDate)
        {
            var yrs = Convert.ToInt16(dayCountFraction);
            var startYear = startDate.Year;
            var endYear = startYear + yrs;
            var guessEndDate = startDate.AddDays(Convert.ToInt16(dayCountFraction * 365.0));

            var includedDayCount = 0;
            for (var year = startYear; year <= endYear; ++year) {
                if (Date.IsLeapYear(year))
                {
                    var leapDate = new Date(year, 2, 29);
                    if (leapDate.Between(startDate, guessEndDate))
                    {
                        ++includedDayCount;
                    }
                }
            }

            //Note: potential issue:  end day could be  (2,28) or  (2,29),  we return (2,28)
            return startDate.AddDays(Convert.ToInt16(dayCountFraction * 365.0) + includedDayCount);
        }
    }

    /// <summary>
    /// 日期规则ActActIsma
    /// </summary>
    [Serializable]
    public class ActActIsma : IDayCount
	{
        /// <summary>
        /// 两个日期之间的天数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <returns>天数</returns>
		public double DaysInPeriod(Date startDate, Date endDate)
		{
			return endDate - startDate;
		}

        /// <summary>
        /// 两个日期的差，以年为单位
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>日期差</returns>
		public double CalcDayCountFraction(Date startDate, Date endDate, Date referenceStartDate, Date referenceEndDate)
		{
			if (startDate == endDate)
			{
				return 0.0;
			}

			var refPeriodStart = referenceStartDate ?? startDate;
			var refPeriodEnd = referenceEndDate ?? endDate;

			var months = (int) (0.5 + 12*(refPeriodEnd - refPeriodStart)/365);

			if (months == 0)
			{
				refPeriodStart = startDate;
				refPeriodEnd = new Term("1Y").Next(startDate);
				months = 12;
			}

			var period = months/12.0;

			if (endDate <= refPeriodEnd)
			{
				if (startDate >= refPeriodStart)
				{
					return period*DaysInPeriod(startDate, endDate)/DaysInPeriod(refPeriodStart, refPeriodEnd);
				}
				else
				{
					var prevRef = new Term(months, Period.Month).Prev(refPeriodStart);
					if (endDate > refPeriodStart)
					{
						return CalcDayCountFraction(startDate, refPeriodStart, prevRef, refPeriodStart) +
						       CalcDayCountFraction(refPeriodStart, endDate, refPeriodStart, refPeriodEnd);
					}
					else
					{
						return CalcDayCountFraction(startDate, endDate, prevRef, refPeriodStart);
					}
				}
			}
			else
			{
				if (refPeriodStart > startDate)
				{
					throw new PricingBaseException("ActActIsma cannot accept sequency: startDate<refPeriodStart refPeriodEnd<endDate!");
				}

				var sum = CalcDayCountFraction(startDate, refPeriodEnd, refPeriodStart, refPeriodEnd);
				var i = 0;
				Date newRefStart, newRefEnd;
				while (true)
				{
					newRefStart = new Term(months * i, Period.Month).Next(refPeriodEnd);
					newRefEnd = new Term(months * i, Period.Month).Next(refPeriodEnd);
					if (endDate < newRefEnd)
					{
						break;
					}
					else
					{
						sum += period;
						i++;
					}
				}

				return sum + CalcDayCountFraction(newRefStart, endDate, newRefStart, newRefEnd);
			}
		}

        /// <summary>
        /// 通过基准日期和日期差，计算目标日期
        /// </summary>
        /// <param name="startDate">基准日期</param>
        /// <param name="dayCountFraction">日期差</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>目标日期</returns>
        public Date CalcEndDateFromDayCountFraction(Date startDate, double dayCountFraction, Date referenceStartDate, Date referenceEndDate)
        {
            //TODO:  do it properly
            return startDate.AddDays(Convert.ToInt16(dayCountFraction * 365.0));
        }
    }

    /// <summary>
    /// 日期规则ModifiedAfb
    /// </summary>
    [Serializable]
    public class ModifiedAfb : IDayCount
	{
        /// <summary>
        /// 两个日期之间的天数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <returns>天数</returns>
		public double DaysInPeriod(Date startDate, Date endDate)
		{
			return endDate - startDate;
		}

        /// <summary>
        /// 两个日期的差，以年为单位
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>日期差</returns>
		public double CalcDayCountFraction(Date startDate, Date endDate, Date referenceStartDate, Date referenceEndDate)
		{
            var refStartDate = referenceEndDate ?? startDate;
			var den = DaysInPeriod(refStartDate, new Term("1Y").Next(refStartDate));

			var s = DaysInPeriod(startDate, endDate) / den;

			return s;
		}

        /// <summary>
        /// 通过基准日期和日期差，计算目标日期
        /// </summary>
        /// <param name="startDate">基准日期</param>
        /// <param name="dayCountFraction">日期差</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>目标日期</returns>
        public Date CalcEndDateFromDayCountFraction(Date startDate, double dayCountFraction, Date referenceStartDate, Date referenceEndDate)
        {
            var refStartDate = referenceEndDate ?? startDate;
            var den = DaysInPeriod(refStartDate, new Term("1Y").Next(refStartDate));

            var actualDaysInPeriod = dayCountFraction* den;
            return startDate.AddDays(Convert.ToInt16(actualDaysInPeriod));
        }
    }

    /// <summary>
    /// 日期规则ActAct。
    /// 两个日期之间以实际天数计算，一年按实际天数计算
    /// </summary>
    [Serializable]
    public class ActAct : IDayCount
	{
        /// <summary>
        /// 两个日期之间的天数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <returns>天数</returns>
		public double DaysInPeriod(Date startDate, Date endDate)
		{
			return endDate - startDate;
		}

        /// <summary>
        /// 两个日期的差，以年为单位
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>日期差</returns>
		public double CalcDayCountFraction(Date startDate, Date endDate, Date referenceStartDate, Date referenceEndDate)
		{
			var startYear = startDate.Year;
			var endYear = endDate.Year;

			if (startYear == endYear)
			{
				return (endDate - startDate)/Date.DaysInYear(startYear);
			}

			var s = 1.0 - 1.0*(startDate.DayOfYear() - 1)/Date.DaysInYear(startYear);
			var e = 1.0*(endDate.DayOfYear() - 1)/Date.DaysInYear(endYear);

			return s + e + (endYear - startYear - 1);
		}

        /// <summary>
        /// 通过基准日期和日期差，计算目标日期
        /// </summary>
        /// <param name="startDate">基准日期</param>
        /// <param name="dayCountFraction">日期差</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>目标日期</returns>
        public Date CalcEndDateFromDayCountFraction(Date startDate, double dayCountFraction, Date referenceStartDate, Date referenceEndDate)
        {
            if (dayCountFraction <= 1.0)
            {
                var days = Convert.ToInt16(Date.DaysInYear(startDate.Year) * dayCountFraction);
                return startDate.AddDays(days);
            }
            else {

                var guessEndDate = startDate.AddDays(Convert.ToInt16(dayCountFraction * 365.0));
                var startYear = startDate.Year;
                var endYear = guessEndDate.Year;

                var s = 1.0 - 1.0 * (startDate.DayOfYear() - 1) / Date.DaysInYear(startYear);

                var actualEndDcf =  dayCountFraction - s - (endYear - startYear - 1);
                var actualEndDayOfYear = actualEndDcf * Date.DaysInYear(endYear) + 1.0;
                var endDayDiff = actualEndDayOfYear - guessEndDate.DayOfYear();
                return guessEndDate.AddDays(Convert.ToInt16(endDayDiff));
            }
        }
    }

    /// <summary>
    /// 日期规则B30360.
    /// 每个月按30天计算，每年按360天计算。
    /// </summary>
    [Serializable]
    public class B30360 : IDayCount
	{
        /// <summary>
        /// 两个日期之间的天数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <returns>天数</returns>
		public double DaysInPeriod(Date startDate, Date endDate)
		{
            return CalcDaysInPeriod(startDate, endDate - startDate);
        }
        
        internal double CalcDaysInPeriod(Date startDate, double endDateJulienInterval) {
            var endDate = startDate.AddDays(Convert.ToInt16(endDateJulienInterval));
            var year = 360.0 * (endDate.Year - startDate.Year);
            var month = 30.0 * (endDate.Month - startDate.Month);

            var endDay = endDate.Day;
            var startDay = startDate.Day;

            if (DateTime.DaysInMonth(endDate.Year, endDate.Month) == endDate.Day)
            {
                endDay = 30;
            }
            if (DateTime.DaysInMonth(startDate.Year, startDate.Month) == startDate.Day)
            {
                startDay = 30;
            }
            var day = endDay - startDay;

            return year + month + day;
        }

        /// <summary>
        /// 两个日期的差，以年为单位
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>日期差</returns>
		public double CalcDayCountFraction(Date startDate, Date endDate, Date referenceStartDate, Date referenceEndDate)
		{
			return DaysInPeriod(startDate, endDate) / 360;
		}

        /// <summary>
        /// 通过基准日期和日期差，计算目标日期
        /// </summary>
        /// <param name="startDate">基准日期</param>
        /// <param name="dayCountFraction">日期差</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>目标日期</returns>
        public Date CalcEndDateFromDayCountFraction(Date startDate, double dayCountFraction, Date referenceStartDate, Date referenceEndDate)
        {
            var days = Convert.ToInt16(dayCountFraction * 360.0);
            var yr = days / 360;
            var month = (days - yr * 360) / 30;
            var day = days - yr * 360 - month * 30;
            return new Date(yr, month, day);
        }
    }

    /// <summary>
    /// 日期规则Act365。
    /// 两个日期之间以交易日数量计算，一年按244个交易日计算
    /// </summary>
    [Serializable]
    public class Bus244 : IDayCount
    {
        /// <summary>
        /// 两个日期之间的天数
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <returns>天数</returns>
        public double DaysInPeriod(Date startDate, Date endDate)
        {
            return CalendarImpl.Get("chn").NumberBizDaysBetweenDate(startDate, endDate, true);
        }

        /// <summary>
        /// 两个日期的差，以年为单位
        /// </summary>
        /// <param name="startDate">开始日</param>
        /// <param name="endDate">结束日</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>日期差</returns>
        public double CalcDayCountFraction(Date startDate, Date endDate, Date referenceStartDate, Date referenceEndDate) => DaysInPeriod(startDate, endDate) / 244.0;

        /// <summary>
        /// 通过基准日期和日期差，计算目标日期
        /// </summary>
        /// <param name="startDate">基准日期</param>
        /// <param name="dayCountFraction">日期差</param>
        /// <param name="referenceStartDate">参考开始日</param>
        /// <param name="referenceEndDate">参考结束日</param>
        /// <returns>目标日期</returns>
        public Date CalcEndDateFromDayCountFraction(Date startDate, double dayCountFraction, Date referenceStartDate, Date referenceEndDate) => startDate.AddDays(Convert.ToInt16(dayCountFraction * 244.0));
    }
}
