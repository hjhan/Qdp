using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Base.Implementations;

namespace UnitTest.CommonTests
{
	[TestClass]
	public class CalendarTest
	{
		[TestMethod]
		public void TestCalendarLoad()
		{
			var chnCalendar = CalendarImpl.Get("chn");
		}
	}
}
