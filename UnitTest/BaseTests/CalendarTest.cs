using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;

namespace UnitTest.BaseTests
{
	[TestClass]
	public class CalendarTest
	{
		private readonly ICalendar _cal;

		public CalendarTest()
		{
			_cal = CalendarImpl.Get("chn");
		}

		[TestMethod]
		public void TestCalendarAdjust()
		{
			var d1 = new Date(2014, 01, 30);
			var d2 = new Date(2014, 01, 31);
			var d3 = new Date(2014, 02, 06);
			var d4 = new Date(2014, 02, 12);

			Assert.AreEqual(new Date(2014, 02, 07), _cal.Adjust(d2, BusinessDayConvention.Following));
			Assert.AreEqual(new Date(2014, 01, 30), _cal.Adjust(d2, BusinessDayConvention.ModifiedFollowing));
			Assert.AreEqual(new Date(2014, 01, 30), _cal.Adjust(d3, BusinessDayConvention.Previous));
			Assert.AreEqual(new Date(2014, 02, 07), _cal.Adjust(d3, BusinessDayConvention.ModifiedPrevious));

			Assert.AreEqual(new Date(2014, 01, 31), _cal.AddDays(d1, 1));
			Assert.AreEqual(new Date(2014, 02, 07), _cal.AddBizDays(d1, 1));
			Assert.AreEqual(new Date(2014, 02, 07), _cal.AddDays(d1, 1, BusinessDayConvention.Following));
			Assert.AreEqual(new Date(2014, 01, 30), _cal.AddDays(d1, 1, BusinessDayConvention.ModifiedFollowing));
			Assert.AreEqual(new Date(2014, 02, 07), _cal.AddBizDays(d1, 1, BusinessDayConvention.Previous));
			Assert.AreEqual(new Date(2014, 02, 07), _cal.AddBizDays(d1, 1, BusinessDayConvention.ModifiedFollowing));

			Assert.AreEqual(new Date(2014, 02, 01), _cal.AddDays(d2, 1));
			Assert.AreEqual(new Date(2014, 02, 07), _cal.AddBizDays(d2, 1));
			Assert.AreEqual(new Date(2014, 02, 07), _cal.AddDays(d2, 1, BusinessDayConvention.Following));
			Assert.AreEqual(new Date(2014, 02, 07), _cal.AddDays(d2, 1, BusinessDayConvention.ModifiedFollowing));
			Assert.AreEqual(new Date(2014, 02, 10), _cal.AddBizDays(d2, 1, BusinessDayConvention.Following));
			Assert.AreEqual(new Date(2014, 02, 07), _cal.AddBizDays(d2, 1, BusinessDayConvention.Previous));

			Assert.AreEqual(false, _cal.BizDaysBetweenDates(d2, d3).Any());
			Assert.AreEqual(4, _cal.BizDaysBetweenDates(d1, d4).Count);
		}
	}


	[TestClass]
	public class GenerateHolidayLists
	{
		private readonly Date _begDate = new Date(2000, 01, 01);
		private readonly Date _endDate = new Date(2060, 12, 31);
		public readonly int January = 1;
		public readonly int February = 2;
		public readonly int March = 3;
		public readonly int April = 4;
		public readonly int May = 5;
		public readonly int June = 6;
		public readonly int July = 7;
		public readonly int August = 8;
		public readonly int September = 9;
		public readonly int October = 10;
		public readonly int November = 11;
		public readonly int December = 12;
		public readonly DayOfWeek Monday = DayOfWeek.Monday;
		public readonly DayOfWeek Tuesday = DayOfWeek.Tuesday;
		public readonly DayOfWeek Wednesday = DayOfWeek.Wednesday;
		public readonly DayOfWeek Thursday = DayOfWeek.Thursday;
		public readonly DayOfWeek Friday = DayOfWeek.Friday;
		public readonly DayOfWeek Saturday = DayOfWeek.Saturday;
		public readonly DayOfWeek Sunday = DayOfWeek.Sunday;

		
		public void UsdCalendar()
		{
			Console.WriteLine("{");
			Console.WriteLine(@"""HolidayList"":[");
			var incTerm = new Term("1D");


			for (var date = _begDate; date <= _endDate;)
			{
				var w = date.WeekDay;
				var d = date.Day;
				var m = date.Month;
				if (date.IsWeekend
				    || (m == 1 && (d == 1 || (d == 2 && w == DayOfWeek.Monday))) // new year's day, move to Monday if on Sunday
				    || (m == 12 && (d == 31 && w == DayOfWeek.Friday)) // or move to Friday if on Saturday
					// Martin Luther King's birthday (third Monday in 1)
				    || ((d >= 15 && d <= 21) && w == DayOfWeek.Monday && m == 1)
					// Washington's birthday (third Monday in February)
				    || ((d >= 15 && d <= 21) && w == DayOfWeek.Monday && m == 2)
					// Memorial Day (last Monday in May)
				    || (d >= 25 && w == DayOfWeek.Monday && m == 5)
					// Independence Day (Monday if Sunday or Friday if Saturday)
				    || ((d == 4 || (d == 5 && w == DayOfWeek.Monday) || (d == 3 && w == DayOfWeek.Friday)) && m == 7)
					// Labor Day (first Monday in September)
				    || (d <= 7 && w == DayOfWeek.Monday && m == 9)
					// Columbus Day (second Monday in October)
				    || ((d >= 8 && d <= 14) && w == DayOfWeek.Monday && m == 10)
					// Veteran's Day (Monday if Sunday or Friday if Saturday)
				    || ((d == 11 || (d == 12 && w == DayOfWeek.Monday) || (d == 10 && w == DayOfWeek.Friday)) && m == 11)
					// Thanksgiving Day (fourth Thursday in November)
				    || ((d >= 22 && d <= 28) && w == DayOfWeek.Thursday && m == 11)
					// Christmas (Monday if Sunday or Friday if Saturday)
				    || ((d == 25 || (d == 26 && w == DayOfWeek.Monday) || (d == 24 && w == DayOfWeek.Friday)) && m == 12)
					)

				{
					Console.WriteLine(@"""" + date.Year + "," + date.Month.ToString("0#") + "," + date.Day.ToString("0#") + @"""" + ",");
				}
				date = incTerm.Next(date);
			}


			Console.WriteLine("]");
			Console.WriteLine("}");
		}

		
		public void HkdCalendar()
		{
			Console.WriteLine("{");
			Console.WriteLine(@"""HolidayList"":[");
			var incTerm = new Term("1D");


			for (var date = _begDate; date <= _endDate; )
			{
				var w = date.WeekDay;
				var d = date.Day;
				var m = date.Month;
				var y = date.Year;
				var em = EasterMonday(y);

				if (date.IsWeekend
					|| ((d == 1 || ((d == 2 || d == 3) && w == DayOfWeek.Monday)) && m == 1) // New Year's Day
					|| (d == em - 3) // Good Friday
					|| (d == em) // Easter Monday
					|| ((d == 1 || ((d == 2 || d == 3) && w == DayOfWeek.Monday)) && m == 5) // Labor Day
					|| ((d == 1 || ((d == 2 || d == 3) && w == DayOfWeek.Monday)) && m == 7) // SAR Establishment Day
					|| ((d == 1 || ((d == 2 || d == 3) && w == DayOfWeek.Monday)) && m == 10) // National Day
					|| (d == 25 && m == 12) // Christmas Day
					|| (d == 26 && m == 12) // Boxing Day
					|| (y == 2004 && (m ==1 && (d == 22 || d == 23 || d ==24))) // lunar new year
					|| (y == 2004 && (m ==4 && (d == 5))) // Ching Ming Festival
					|| (y == 2004 && (m == 5 && (d == 26))) // Buddha's birthday
					|| (y == 2004 && (m == 6 && (d == 22))) // Tuen NG festival
					|| (y == 2004 && (m == 9 && (d == 29))) // Chung Yeung
				)
				{
					Console.WriteLine(@"""" + date.Year + "," + date.Month.ToString("0#") + "," + date.Day.ToString("0#") + @"""" + ",");
				}
				date = incTerm.Next(date);
			}


			Console.WriteLine("]");
			Console.WriteLine("}");
		}

		//[TestMethod]
		public void EurCalendar()
		{
			Console.WriteLine("{");
			Console.WriteLine(@"""HolidayList"":[");
			var incTerm = new Term("1D");


			for (var date = _begDate; date <= _endDate; )
			{
				var w = date.WeekDay;
				var d = date.Day;
				var m = date.Month;
				if (date.IsWeekend)
				{
					Console.WriteLine(@"""" + date.Year + "," + date.Month.ToString("0#") + "," + date.Day.ToString("0#") + @"""" + ",");
				}
				date = incTerm.Next(date);
			}


			Console.WriteLine("]");
			Console.WriteLine("}");
		}

		public void JpyCalendar()
		{
			Console.WriteLine("{");
			Console.WriteLine(@"""HolidayList"":[");
			var incTerm = new Term("1D");


			for (var date = _begDate; date <= _endDate;)
			{
				var w = date.WeekDay;
				var d = date.Day;
				var m = date.Month;
				var y = date.Year;

				var exactVernalEquinoxTime = 20.69115;
				var exactAutumnalEquinoxTime = 23.09;
				var diffPerYear = 0.242194;
				var movingAmount = (y - 2000)*diffPerYear;
				var numberOfLeapYears = (y - 2000)/4 + (y - 2000)/100 - (y - 2000)/400;
				var ve = Convert.ToInt32(exactVernalEquinoxTime + movingAmount - numberOfLeapYears); // vernal equinox day
				var ae = Convert.ToInt32(exactAutumnalEquinoxTime + movingAmount - numberOfLeapYears); // autumnal equinox day


				if (date.IsWeekend
					// New Year's Day
				    || (d == 1 && m == January)
					// Bank Holiday
				    || (d == 2 && m == January)
					// Bank Holiday
				    || (d == 3 && m == January)
					// Coming of Age Day (2nd Monday in January),
					// was January 15th until 2000
				    || (w == Monday && (d >= 8 && d <= 14) && m == January
				        && y >= 2000)
				    || ((d == 15 || (d == 16 && w == Monday)) && m == January
				        && y < 2000)
					// National Foundation Day
				    || ((d == 11 || (d == 12 && w == Monday)) && m == February)
					// Vernal Equinox
				    || ((d == ve || (d == ve + 1 && w == Monday)) && m == March)
					// Greenery Day
				    || ((d == 29 || (d == 30 && w == Monday)) && m == April)
					// Constitution Memorial Day
				    || (d == 3 && m == May)
					// Holiday for a Nation
				    || (d == 4 && m == May)
					// Children's Day
				    || (d == 5 && m == May)
					// any of the three above observed later if on Saturday or Sunday
				    || (d == 6 && m == May
				        && (w == Monday || w == Tuesday || w == Wednesday))
					// Marine Day (3rd Monday in July),
					// was July 20th until 2003, not a holiday before 1996
				    || (w == Monday && (d >= 15 && d <= 21) && m == July
				        && y >= 2003)
				    || ((d == 20 || (d == 21 && w == Monday)) && m == July
				        && y >= 1996 && y < 2003)
					// Respect for the Aged Day (3rd Monday in September),
					// was September 15th until 2003
				    || (w == Monday && (d >= 15 && d <= 21) && m == September
				        && y >= 2003)
				    || ((d == 15 || (d == 16 && w == Monday)) && m == September
				        && y < 2003)
					// If a single day falls between Respect for the Aged Day
					// and the Autumnal Equinox, it is holiday
				    || (w == Tuesday && d + 1 == ae && d >= 16 && d <= 22
				        && m == September && y >= 2003)
					// Autumnal Equinox
				    || ((d == ae || (d == ae + 1 && w == Monday)) && m == September)
					// Health and Sports Day (2nd Monday in October),
					// was October 10th until 2000
				    || (w == Monday && (d >= 8 && d <= 14) && m == October
				        && y >= 2000)
				    || ((d == 10 || (d == 11 && w == Monday)) && m == October
				        && y < 2000)
					// National Culture Day
				    || ((d == 3 || (d == 4 && w == Monday)) && m == November)
					// Labor Thanksgiving Day
				    || ((d == 23 || (d == 24 && w == Monday)) && m == November)
					// Emperor's Birthday
				    || ((d == 23 || (d == 24 && w == Monday)) && m == December
				        && y >= 1989)
					// Bank Holiday
				    || (d == 31 && m == December)
					// one-shot holidays
					// Marriage of Prince Akihito
				    || (d == 10 && m == April && y == 1959)
					// Rites of Imperial Funeral
				    || (d == 24 && m == February && y == 1989)
					// Enthronement Ceremony
				    || (d == 12 && m == November && y == 1990)
					// Marriage of Prince Naruhito
				    || (d == 9 && m == June && y == 1993))
				{
					Console.WriteLine(@"""" + date.Year + "," + date.Month.ToString("0#") + "," + date.Day.ToString("0#") + @"""" + ",");
				}
				date = incTerm.Next(date);
			}


			Console.WriteLine("]");
			Console.WriteLine("}");
		}

		public void GbpCalendar()
		{
			Console.WriteLine("{");
			Console.WriteLine(@"""HolidayList"":[");
			var incTerm = new Term("1D");

			for (var date = _begDate; date <= _endDate;)
			{
				var w = date.WeekDay;
				var d = date.Day;
				var dd = date.DayOfYear();
				var m = date.Month;
				var y = date.Year;
				var em = EasterMonday(y);

				if (date.IsWeekend
					// New Year's Day (possibly moved to Monday)
				    || ((d == 1 || ((d == 2 || d == 3) && w == Monday)) &&
				        m == January)
					// Good Friday
				    || (dd == em - 3)
					// Easter Monday
				    || (dd == em)
					// first Monday of May (Early May Bank Holiday)
				    || (d <= 7 && w == Monday && m == May)
					// last Monday of May (Spring Bank Holiday)
				    || (d >= 25 && w == Monday && m == May && y != 2002)
					// last Monday of August (Summer Bank Holiday)
				    || (d >= 25 && w == Monday && m == August)
					// Christmas (possibly moved to Monday or Tuesday)
				    || ((d == 25 || (d == 27 && (w == Monday || w == Tuesday)))
				        && m == December)
					// Boxing Day (possibly moved to Monday or Tuesday)
				    || ((d == 26 || (d == 28 && (w == Monday || w == Tuesday)))
				        && m == December)
					// June 3rd, 2002 only (Golden Jubilee Bank Holiday)
					// June 4rd, 2002 only (special Spring Bank Holiday)
				    || ((d == 3 || d == 4) && m == June && y == 2002)
					// December 31st, 1999 only
				    || (d == 31 && m == December && y == 1999)
					)
				{
					Console.WriteLine(@"""" + date.Year + "," + date.Month.ToString("0#") + "," + date.Day.ToString("0#") + @"""" + ",");
				}
				date = incTerm.Next(date);
			}


			Console.WriteLine("]");
			Console.WriteLine("}");
		}

		public void AudCalendar()
		{
			Console.WriteLine("{");
			Console.WriteLine(@"""HolidayList"":[");
			var incTerm = new Term("1D");

			for (var date = _begDate; date <= _endDate;)
			{
				var w = date.WeekDay;
				var d = date.Day;
				var dd = date.DayOfYear();
				var m = date.Month;
				var y = date.Year;
				var em = EasterMonday(y);

				if (date.IsWeekend
					// New Year's Day (possibly moved to Monday)
				    || (d == 1 && m == January)
					// Australia Day, January 26th (possibly moved to Monday)
				    || ((d == 26 || ((d == 27 || d == 28) && w == Monday)) &&
				        m == January)
					// Good Friday
				    || (dd == em - 3)
					// Easter Monday
				    || (dd == em)
					// ANZAC Day, April 25th (possibly moved to Monday)
				    || ((d == 25 || (d == 26 && w == Monday)) && m == April)
					// Queen's Birthday, second Monday in June
				    || ((d > 7 && d <= 14) && w == Monday && m == June)
					// Bank Holiday, first Monday in August
				    || (d <= 7 && w == Monday && m == August)
					// Labour Day, first Monday in October
				    || (d <= 7 && w == Monday && m == October)
					// Christmas, December 25th (possibly Monday or Tuesday)
				    || ((d == 25 || (d == 27 && (w == Monday || w == Tuesday)))
				        && m == December)
					// Boxing Day, December 26th (possibly Monday or Tuesday)
				    || ((d == 26 || (d == 28 && (w == Monday || w == Tuesday)))
				        && m == December)
					)
				{
					Console.WriteLine(@"""" + date.Year + "," + date.Month.ToString("0#") + "," + date.Day.ToString("0#") + @"""" + ",");
				}
				date = incTerm.Next(date);
			}


			Console.WriteLine("]");
			Console.WriteLine("}");
		}

		//[TestMethod]
		public void SgdCalendar()
		{
			Console.WriteLine("{");
			Console.WriteLine(@"""HolidayList"":[");
			var incTerm = new Term("1D");

			for (var date = _begDate; date <= _endDate;)
			{
				var w = date.WeekDay;
				var d = date.Day;
				var dd = date.DayOfYear();
				var m = date.Month;
				var y = date.Year;
				var em = EasterMonday(y);

				if (date.IsWeekend
					// New Year's Day
				    || ((d == 1 || (d == 2 && w == Monday)) && m == January)
					// Good Friday
				    || (dd == em - 3)
					// Labor Day
				    || (d == 1 && m == May)
					// National Day
				    || ((d == 9 || (d == 10 && w == Monday)) && m == August)
					// Christmas Day
				    || (d == 25 && m == December)

					// Chinese New Year
				    || ((d == 22 || d == 23) && m == January && y == 2004)
				    || ((d == 9 || d == 10) && m == February && y == 2005)
				    || ((d == 30 || d == 31) && m == January && y == 2006)
				    || ((d == 19 || d == 20) && m == February && y == 2007)
				    || ((d == 7 || d == 8) && m == February && y == 2008)
				    || ((d == 26 || d == 27) && m == January && y == 2009)
				    || ((d == 15 || d == 16) && m == January && y == 2010)
				    || ((d == 23 || d == 24) && m == January && y == 2012)
				    || ((d == 11 || d == 12) && m == February && y == 2013)

					// Hari Raya Haji
				    || ((d == 1 || d == 2) && m == February && y == 2004)
				    || (d == 21 && m == January && y == 2005)
				    || (d == 10 && m == January && y == 2006)
				    || (d == 2 && m == January && y == 2007)
				    || (d == 20 && m == December && y == 2007)
				    || (d == 8 && m == December && y == 2008)
				    || (d == 27 && m == November && y == 2009)
				    || (d == 17 && m == November && y == 2010)
				    || (d == 26 && m == October && y == 2012)
				    || (d == 15 && m == October && y == 2013)

					// Vesak Poya Day
				    || (d == 2 && m == June && y == 2004)
				    || (d == 22 && m == May && y == 2005)
				    || (d == 12 && m == May && y == 2006)
				    || (d == 31 && m == May && y == 2007)
				    || (d == 18 && m == May && y == 2008)
				    || (d == 9 && m == May && y == 2009)
				    || (d == 28 && m == May && y == 2010)
				    || (d == 5 && m == May && y == 2012)
				    || (d == 24 && m == May && y == 2013)

					// Deepavali
				    || (d == 11 && m == November && y == 2004)
				    || (d == 8 && m == November && y == 2007)
				    || (d == 28 && m == October && y == 2008)
				    || (d == 16 && m == November && y == 2009)
				    || (d == 5 && m == November && y == 2010)
				    || (d == 13 && m == November && y == 2012)
				    || (d == 2 && m == November && y == 2013)

					// Diwali
				    || (d == 1 && m == November && y == 2005)

					// Hari Raya Puasa
				    || ((d == 14 || d == 15) && m == November && y == 2004)
				    || (d == 3 && m == November && y == 2005)
				    || (d == 24 && m == October && y == 2006)
				    || (d == 13 && m == October && y == 2007)
				    || (d == 1 && m == October && y == 2008)
				    || (d == 21 && m == September && y == 2009)
				    || (d == 10 && m == September && y == 2010)
				    || (d == 20 && m == August && y == 2012)
				    || (d == 8 && m == August && y == 2013)
					)
				{
					Console.WriteLine(@"""" + date.Year + "," + date.Month.ToString("0#") + "," + date.Day.ToString("0#") + @"""" + ",");
				}
				date = incTerm.Next(date);
			}


			Console.WriteLine("]");
			Console.WriteLine("}");
		}

		//[TestMethod]
		public void CadCalendar()
		{
			Console.WriteLine("{");
			Console.WriteLine(@"""HolidayList"":[");
			var incTerm = new Term("1D");

			for (var date = _begDate; date <= _endDate;)
			{
				var w = date.WeekDay;
				var d = date.Day;
				var dd = date.DayOfYear();
				var m = date.Month;
				var y = date.Year;
				var em = EasterMonday(y);

				if (date.IsWeekend
					// New Year's Day (possibly moved to Monday)
				    || ((d == 1 || (d == 2 && w == Monday)) && m == January)
					// Family Day (third Monday in February, since 2008)
				    || ((d >= 15 && d <= 21) && w == Monday && m == February
				        && y >= 2008)
					// Good Friday
				    || (dd == em - 3)
					// The Monday on or preceding 24 May (Victoria Day)
				    || (d > 17 && d <= 24 && w == Monday && m == May)
					// July 1st, possibly moved to Monday (Canada Day)
				    || ((d == 1 || ((d == 2 || d == 3) && w == Monday)) && m == July)
					// first Monday of August (Provincial Holiday)
				    || (d <= 7 && w == Monday && m == August)
					// first Monday of September (Labor Day)
				    || (d <= 7 && w == Monday && m == September)
					// second Monday of October (Thanksgiving Day)
				    || (d > 7 && d <= 14 && w == Monday && m == October)
					// November 11th (possibly moved to Monday)
				    || ((d == 11 || ((d == 12 || d == 13) && w == Monday))
				        && m == November)
					// Christmas (possibly moved to Monday or Tuesday)
				    || ((d == 25 || (d == 27 && (w == Monday || w == Tuesday)))
				        && m == December)
					// Boxing Day (possibly moved to Monday or Tuesday)
				    || ((d == 26 || (d == 28 && (w == Monday || w == Tuesday)))
				        && m == December)
					)
				{
					Console.WriteLine(@"""" + date.Year + "," + date.Month.ToString("0#") + "," + date.Day.ToString("0#") + @"""" + ",");
				}
				date = incTerm.Next(date);
			}


			Console.WriteLine("]");
			Console.WriteLine("}");
		}

		//[TestMethod]
		public void NzdCalendar()
		{
			Console.WriteLine("{");
			Console.WriteLine(@"""HolidayList"":[");
			var incTerm = new Term("1D");

			for (var date = _begDate; date <= _endDate;)
			{
				var w = date.WeekDay;
				var d = date.Day;
				var dd = date.DayOfYear();
				var m = date.Month;
				var y = date.Year;
				var em = EasterMonday(y);

				if (date.IsWeekend
					// New Year's Day (possibly moved to Monday or Tuesday)
				    || ((d == 1 || (d == 3 && (w == Monday || w == Tuesday))) &&
				        m == January)
					// Day after New Year's Day (possibly moved to Mon or Tuesday)
				    || ((d == 2 || (d == 4 && (w == Monday || w == Tuesday))) &&
				        m == January)
					// Anniversary Day, Monday nearest January 22nd
				    || ((d >= 19 && d <= 25) && w == Monday && m == January)
					// Waitangi Day. February 6th
				    || (d == 6 && m == February)
					// Good Friday
				    || (dd == em - 3)
					// Easter Monday
				    || (dd == em)
					// ANZAC Day. April 25th
				    || (d == 25 && m == April)
					// Queen's Birthday, first Monday in June
				    || (d <= 7 && w == Monday && m == June)
					// Labour Day, fourth Monday in October
				    || ((d >= 22 && d <= 28) && w == Monday && m == October)
					// Christmas, December 25th (possibly Monday or Tuesday)
				    || ((d == 25 || (d == 27 && (w == Monday || w == Tuesday)))
				        && m == December)
					// Boxing Day, December 26th (possibly Monday or Tuesday)
				    || ((d == 26 || (d == 28 && (w == Monday || w == Tuesday)))
				        && m == December)
					)
				{
					Console.WriteLine(@"""" + date.Year + "," + date.Month.ToString("0#") + "," + date.Day.ToString("0#") + @"""" + ",");
				}
				date = incTerm.Next(date);
			}


			Console.WriteLine("]");
			Console.WriteLine("}");
		}

		//[TestMethod]
		public void ChfCalendar()
		{
			Console.WriteLine("{");
			Console.WriteLine(@"""HolidayList"":[");
			var incTerm = new Term("1D");

			for (var date = _begDate; date <= _endDate;)
			{
				var w = date.WeekDay;
				var d = date.Day;
				var dd = date.DayOfYear();
				var m = date.Month;
				var y = date.Year;
				var em = EasterMonday(y);

				if (date.IsWeekend
					// New Year's Day
				    || (d == 1 && m == January)
					// Berchtoldstag
				    || (d == 2 && m == January)
					// Good Friday
				    || (dd == em - 3)
					// Easter Monday
				    || (dd == em)
					// Ascension Day
				    || (dd == em + 38)
					// Whit Monday
				    || (dd == em + 49)
					// Labour Day
				    || (d == 1 && m == May)
					// National Day
				    || (d == 1 && m == August)
					// Christmas
				    || (d == 25 && m == December)
					// St. Stephen's Day
				    || (d == 26 && m == December)
					)
				{
					Console.WriteLine(@"""" + date.Year + "," + date.Month.ToString("0#") + "," + date.Day.ToString("0#") + @"""" + ",");
				}
				date = incTerm.Next(date);
			}


			Console.WriteLine("]");
			Console.WriteLine("}");
		}

		//myr is not implemented in quantlib, fix manually year by year

		//[TestMethod]
		public void RubCalendar()
		{
			Console.WriteLine("{");
			Console.WriteLine(@"""HolidayList"":[");
			var incTerm = new Term("1D");

			for (var date = _begDate; date <= _endDate;)
			{
				var w = date.WeekDay;
				var d = date.Day;
				var dd = date.DayOfYear();
				var m = date.Month;
				var y = date.Year;
				var em = EasterMonday(y);

				if (date.IsWeekend
					// New Year's holidays
				    || (d >= 1 && d <= 8 && m == January)
					// Defender of the Fatherland Day (possibly moved to Monday)
				    || ((d == 23 || ((d == 24 || d == 25) && w == Monday)) &&
				        m == February)
					// International Women's Day (possibly moved to Monday)
				    || ((d == 8 || ((d == 9 || d == 10) && w == Monday)) &&
				        m == March)
					// Labour Day (possibly moved to Monday)
				    || ((d == 1 || ((d == 2 || d == 3) && w == Monday)) &&
				        m == May)
					// Victory Day (possibly moved to Monday)
				    || ((d == 9 || ((d == 10 || d == 11) && w == Monday)) &&
				        m == May)
					// Russia Day (possibly moved to Monday)
				    || ((d == 12 || ((d == 13 || d == 14) && w == Monday)) &&
				        m == June)
					// Unity Day (possibly moved to Monday)
				    || ((d == 4 || ((d == 5 || d == 6) && w == Monday)) &&
				        m == November)
					)
				{
					Console.WriteLine(@"""" + date.Year + "," + date.Month.ToString("0#") + "," + date.Day.ToString("0#") + @"""" + ",");
				}
				date = incTerm.Next(date);
			}


			Console.WriteLine("]");
			Console.WriteLine("}");
		}

		//thb is not implemented in quantlib, fix manually year by year
		
		private int EasterMonday(int y) {
         var easterMonday = new[]{
                  98,  90, 103,  95, 114, 106,  91, 111, 102,   // 1901-1909
             87, 107,  99,  83, 103,  95, 115,  99,  91, 111,   // 1910-1919
             96,  87, 107,  92, 112, 103,  95, 108, 100,  91,   // 1920-1929
            111,  96,  88, 107,  92, 112, 104,  88, 108, 100,   // 1930-1939
             85, 104,  96, 116, 101,  92, 112,  97,  89, 108,   // 1940-1949
            100,  85, 105,  96, 109, 101,  93, 112,  97,  89,   // 1950-1959
            109,  93, 113, 105,  90, 109, 101,  86, 106,  97,   // 1960-1969
             89, 102,  94, 113, 105,  90, 110, 101,  86, 106,   // 1970-1979
             98, 110, 102,  94, 114,  98,  90, 110,  95,  86,   // 1980-1989
            106,  91, 111, 102,  94, 107,  99,  90, 103,  95,   // 1990-1999
            115, 106,  91, 111, 103,  87, 107,  99,  84, 103,   // 2000-2009
             95, 115, 100,  91, 111,  96,  88, 107,  92, 112,   // 2010-2019
            104,  95, 108, 100,  92, 111,  96,  88, 108,  92,   // 2020-2029
            112, 104,  89, 108, 100,  85, 105,  96, 116, 101,   // 2030-2039
             93, 112,  97,  89, 109, 100,  85, 105,  97, 109,   // 2040-2049
            101,  93, 113,  97,  89, 109,  94, 113, 105,  90,   // 2050-2059
            110, 101,  86, 106,  98,  89, 102,  94, 114, 105,   // 2060-2069
             90, 110, 102,  86, 106,  98, 111, 102,  94, 114,   // 2070-2079
             99,  90, 110,  95,  87, 106,  91, 111, 103,  94,   // 2080-2089
            107,  99,  91, 103,  95, 115, 107,  91, 111, 103,   // 2090-2099
             88, 108, 100,  85, 105,  96, 109, 101,  93, 112,   // 2100-2109
             97,  89, 109,  93, 113, 105,  90, 109, 101,  86,   // 2110-2119
            106,  97,  89, 102,  94, 113, 105,  90, 110, 101,   // 2120-2129
             86, 106,  98, 110, 102,  94, 114,  98,  90, 110,   // 2130-2139
             95,  86, 106,  91, 111, 102,  94, 107,  99,  90,   // 2140-2149
            103,  95, 115, 106,  91, 111, 103,  87, 107,  99,   // 2150-2159
             84, 103,  95, 115, 100,  91, 111,  96,  88, 107,   // 2160-2169
             92, 112, 104,  95, 108, 100,  92, 111,  96,  88,   // 2170-2179
            108,  92, 112, 104,  89, 108, 100,  85, 105,  96,   // 2180-2189
            116, 101,  93, 112,  97,  89, 109, 100,  85, 105    // 2190-2199
            };
            return easterMonday[y-1901];
        }

        [TestMethod]
        public void TestNumberOfBizDates() {
            var cny = CalendarImpl.Get("chn");
            var start = new Date(2018, 4, 26); // 2 days
            var end = new Date(2018, 5, 4); //3 days
            var numDays = cny.NumberBizDaysBetweenDate(start, end, true);
            Assert.AreEqual( 4, numDays);
        }

        [TestMethod]
        public void TestWithCachedCalendarDataFromExternalDataSource()
        {
            var calendars = new Dictionary<string, CalendarHoliday>();
            calendars["test"] = new CalendarHoliday()
            {
                HolidayList = new List<string>()
                {
                    "2018-04-22",
                    "2018-04-29",
                    "2018-04-30",
                    "2018-05-01",
                    "2018-05-05",
                }
            };

            CalendarImpl.InitializeCalendarData(calendars);
            var cny = CalendarImpl.Get("test");
            var start = new Date(2018, 4, 26); // 2 days
            var end = new Date(2018, 5, 4); //3 days
            var numDays = cny.NumberBizDaysBetweenDate(start, end, true);
            Assert.AreEqual(5, numDays);
        }

		//Validate against china money announcement
		//http://www.chinamoney.com.cn/fe/Channel/10325

		//[TestMethod]
		public void ValidateCalendarAgainstChinaMoney()
		{
			var cmHolidays = GetChinaMoneyHoliday();
			var Calendars = Enum.GetValues(typeof (Calendar));

			var calToFix = new List<Calendar>();
			foreach (Calendar Calendar in Calendars)
			{
				var calendar = CalendarImpl.Get(Calendar);
				if (Calendar == Calendar.Chn)
				{
					continue;
				}
			
				var holToValidate = cmHolidays[Calendar];
				foreach (var d in holToValidate)
				{
					if (!calendar.IsHoliday(d))
					{
						Console.WriteLine("date {0} should be holiday but is not in calendar {1}", d, Calendar);
						calToFix.Add(Calendar);
					}
				}
				var years = holToValidate.Select(x => x.Year).Distinct().OrderBy(x => x);
				var date = new Date(years.First(), 01, 01);
				var incTerm = new Term("1D");
				
				while (date < new Date(years.Last(), 12, 31))
				{
					if (calendar.IsHoliday(date) && !date.IsWeekend && !holToValidate.Contains(date))
					{
						Console.WriteLine("date {0} is holiday in calendar {1} but should not be", date, Calendar);
						calToFix.Add(Calendar);
					}
					date = incTerm.Next(date);
				}
				
			
				Console.WriteLine();
				Console.WriteLine();
			}

			calToFix = calToFix.Distinct().ToList();
			var outputFolder = @"D:\fixedCalendar\";
			foreach (var Calendar in calToFix)
			{
				var outputFile = outputFolder + Calendar.ToString().ToLower() + ".txt";
				var file = new StreamWriter(File.Open(outputFile, FileMode.Create));
				file.WriteLine("{");
				file.WriteLine(@"""HolidayList"":[");
				var incTerm = new Term("1D");

				var calendar = CalendarImpl.Get(Calendar);
				var holToValidate = cmHolidays[Calendar];
				var years = holToValidate.Select(x => x.Year).Distinct().ToDictionary(x => x, x => x);
				
				for (var date = _begDate; date <= _endDate;)
				{
					if (calendar.IsHoliday(date))
					{
						if (years.ContainsKey(date.Year) && !date.IsWeekend && !holToValidate.Contains(date))
						{
							
						}
						else
						{
							file.WriteLine(@"""" + date.Year + "," + date.Month.ToString("0#") + "," + date.Day.ToString("0#") + @"""" + ",");
						}
					}
					else
					{
						if (holToValidate.Contains(date))
						{
							file.WriteLine(@"""" + date.Year + "," + date.Month.ToString("0#") + "," + date.Day.ToString("0#") + @"""" + ",");
						}
					}
					
					date = incTerm.Next(date);
				}


				file.WriteLine("]");
				file.WriteLine("}");
				file.Close();
			}
		}


		private Dictionary<Calendar, List<Date>> GetChinaMoneyHoliday()
		{
			var directoryInfo = new DirectoryInfo(@".\Data\ChinaMoneyHolidays\");
			var files = directoryInfo.GetFiles().Select(x => x.Name);
			var dict = new Dictionary<Calendar, List<Date>>();
			foreach (var file in files)
			{
				var year = Convert.ToInt32(file);
				var lines = File.ReadAllLines(@".\Data\ChinaMoneyHolidays\" + file);
				foreach (var line in lines)
				{
					var splits = line.Split(':');
					var code = (Calendar)Enum.Parse(typeof(Calendar), splits[0] == "CNY" ? "chn_ib" : splits[0], true);
					var dates = splits[1].Split(';');
					
					var holidays = dates.Select(x =>
					{
						var dm = x.Split('-');
						var m = Convert.ToInt32(dm[0]);
						var d = Convert.ToInt32(dm[1]);
						return new Date(year, m, d);
					}).ToList();
					if (!dict.ContainsKey(code))
					{
						dict[code] = holidays;
					}
					else
					{
						dict[code].AddRange(holidays);
					}
				}
			}
			return dict;
		}
	}
}
