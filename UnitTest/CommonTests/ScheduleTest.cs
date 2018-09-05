using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Implementations;

namespace UnitTest.CommonTests
{
	[TestClass]
	public class ScheduleTest
	{
		[TestMethod]
		public void TestSchedule1()
		{
			var startDate = new Date(2016,5,31);
			var maturityDate = new Date(2026,5,31);
			var frequency = Frequency.SemiAnnual;
			var stub = Stub.ShortStart;
			var calendar = "chn_ib".ToCalendarImpl();
			var convention = BusinessDayConvention.None;

			var schedule = new Schedule(startDate, maturityDate, frequency.GetTerm(), stub, calendar, convention);
			var dates = schedule.ToList();
			Assert.AreEqual(true, schedule.IsRegular[0]);
			Assert.AreEqual(new Date(2016, 5, 31), dates[0]);
		}
		[TestMethod]
		public void TestSchedule2()
		{
			var startDate = new Date(2016, 2, 29);
			var maturityDate = new Date(2021, 2, 28);
			var frequency = Frequency.Annual;
			var stub = Stub.ShortStart;
			var calendar = "chn_ib".ToCalendarImpl();
			var convention = BusinessDayConvention.None;

			var schedule = new Schedule(startDate, maturityDate, frequency.GetTerm(), stub, calendar, convention);
			var dates = schedule.ToList();
			Assert.AreEqual(false, schedule.IsRegular[0]);
			Assert.AreEqual(new Date(2016, 2, 29), dates[0]);
		}
	}
}
