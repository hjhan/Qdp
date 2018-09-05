using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Base.Curves.Interpolators;
using System;
using Qdp.Pricing.Library.Base.Utilities;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using Qdp.Pricing.Base.Implementations;


namespace UnitTest.BaseTests
{
	[TestClass]
	public class VolInterpolationTest
	{
		//https://en.wikipedia.org/wiki/Bilinear_interpolation
		[TestMethod]
		public void BiLinearInterpolationTest()
		{
			var vOnGrids = new[,]
			{
				{91, 210.0},
				{162.0, 95}
			};

			var rowGrid = new[] {20.0, 21};
			var colGrid = new[] {14.0, 15};

			var interpolator = Interpolation2D.BiLinear.GetInterpolator(rowGrid, colGrid, vOnGrids);
			Assert.AreEqual(interpolator.GetValue(20, 14), 91, 1e-10);
			Assert.AreEqual(interpolator.GetValue(20, 15), 210, 1e-10);
			Assert.AreEqual(interpolator.GetValue(21, 14), 162, 1e-10);
			Assert.AreEqual(interpolator.GetValue(21, 15), 95, 1e-10);

            Assert.AreEqual(interpolator.GetValue(20.2, 14.5), 146.1, 1e-10);
            Assert.AreEqual(interpolator.GetValue(19, 13.5), 91, 1e-10);
            Assert.AreEqual(interpolator.GetValue(19, 15.5), 210, 1e-10);
            Assert.AreEqual(interpolator.GetValue(22, 13.5), 162, 1e-10);
            Assert.AreEqual(interpolator.GetValue(22, 15.5), 95, 1e-10);
            Assert.AreEqual(interpolator.GetValue(20.5, 13.5), 126.5, 1e-10);
            Assert.AreEqual(interpolator.GetValue(20.5, 15.5), 152.5, 1e-10);
            Assert.AreEqual(interpolator.GetValue(19, 14.2), 114.8, 1e-10);
            Assert.AreEqual(interpolator.GetValue(22, 14.2), 148.6, 1e-10);
        }


        [TestMethod]
        public void BiCubicSplineInterpolationTest()
        {
            var vOnGrids = new[,]
            {
                {91, 210.0},
                {162.0, 95}
            };

            var rowGrid = new[] { 20.0, 21 };
            var colGrid = new[] { 14.0, 15 };


            var boarder = vOnGrids.GetCol(0);

            var interpolator2D = new BiCubicSplineInterpolator(rowGrid, colGrid, vOnGrids);
            var interpolator = new CubicHermiteFdInterpolator(rowGrid.Select((v, i) => Tuple.Create(v, boarder[i])).ToArray());

            Assert.AreEqual(interpolator2D.GetValue(20, 14), 91, 1e-10);
            Assert.AreEqual(interpolator2D.GetValue(20, 15), 210, 1e-10);
            Assert.AreEqual(interpolator2D.GetValue(21, 14), 162, 1e-10);
            Assert.AreEqual(interpolator2D.GetValue(21, 15), 95, 1e-10);

            Assert.AreEqual(interpolator2D.GetValue(19, 13.5), 91, 1e-10);
            Assert.AreEqual(interpolator2D.GetValue(19, 15.5), 210, 1e-10);
            Assert.AreEqual(interpolator2D.GetValue(22, 13.5), 162, 1e-10);
            Assert.AreEqual(interpolator2D.GetValue(22, 15.5), 95, 1e-10);

            Assert.AreEqual(interpolator2D.GetValue(20, 14.5), 150.5, 1e-10);
            Assert.AreEqual(interpolator2D.GetValue(20.2, 14), interpolator.GetValue(20.2), 1e-10);
        }

        [TestMethod]
        public void VarianceBiCubicSplineInterpolationTest()
        {
            var vOnGrids = new[,]
            {
                {0.3, 0.3, 0.3},
                {0.3, 0.3, 0.3},
                {0.3, 0.4, 0.3}
            };

            var rowGrid = new[] { 5.0/244, 10.0/244, 23.0/244 };
            var colGrid = new[] { 0.95, 1.0, 1.05 };

            var interpolator2D = new VarianceBiCubicSplineInterpolator(rowGrid, colGrid, vOnGrids);

            Assert.AreEqual(interpolator2D.GetValue(3.0 / 244, 1.0), 0.387298335, 1e-8);
            Assert.AreEqual(interpolator2D.GetValue(4.0 / 244, 1.0), 0.335410197, 1e-8);
            Assert.AreEqual(interpolator2D.GetValue(5.0 / 244, 1.0), 0.3, 1e-8);
            Assert.AreEqual(interpolator2D.GetValue(8.0 / 244, 1.0), 0.290563114, 1e-8);
            Assert.AreEqual(interpolator2D.GetValue(9.0 / 244, 1.0), 0.292568934, 1e-8);
            Assert.AreEqual(interpolator2D.GetValue(10.0 / 244, 1.0), 0.3, 1e-8);
            Assert.AreEqual(interpolator2D.GetValue(11.0 / 244, 1.0), 0.310583512, 1e-8);

            Assert.AreEqual(interpolator2D.GetValue(14.0 / 244, 1.0), 0.341913542, 1e-8);
            Assert.AreEqual(interpolator2D.GetValue(17.0 / 244, 1.0), 0.368190174, 1e-8);
            Assert.AreEqual(interpolator2D.GetValue(19.0 / 244, 1.0), 0.381951535, 1e-8);
            Assert.AreEqual(interpolator2D.GetValue(22.0 / 244, 1.0), 0.396676932, 1e-8);
            Assert.AreEqual(interpolator2D.GetValue(23.0 / 244, 1.0), 0.4, 1e-8);
            
        }

        [TestMethod]
        public void TradeVolLinearInterpTest()
        {
            //Flat TradeVol;
            TradeVolLinearInterpCalc(expectedValue: 0.3, valuationDate: new Date(2018, 3, 30), tradeOpenVol: 0.3, tradeCloseVol: 0.3, dayCountMode: DayCountMode.TradingDay);
            TradeVolLinearInterpCalc(expectedValue: 0.3, valuationDate: new Date(2018, 3, 30), tradeOpenVol: 0.3, tradeCloseVol: 0.3, dayCountMode: DayCountMode.CalendarDay);
            //CalenderDay: startDate, middleDate and endDate
            TradeVolLinearInterpCalc(expectedValue: 0.2, valuationDate: new Date(2018, 3, 21), tradeOpenVol: 0.2, tradeCloseVol: 0.4, dayCountMode: DayCountMode.CalendarDay);
            TradeVolLinearInterpCalc(expectedValue: 0.3, valuationDate: new Date(2018, 4, 5), tradeOpenVol: 0.2, tradeCloseVol: 0.4, dayCountMode: DayCountMode.CalendarDay);
            TradeVolLinearInterpCalc(expectedValue: 0.4, valuationDate: new Date(2018, 4, 20), tradeOpenVol: 0.2, tradeCloseVol: 0.4, dayCountMode: DayCountMode.CalendarDay);
            TradeVolLinearInterpCalc(expectedValue: 0.4, valuationDate: new Date(2018, 4, 25), tradeOpenVol: 0.2, tradeCloseVol: 0.4, dayCountMode: DayCountMode.CalendarDay);
            //TradingDay: startDate and endDate
            TradeVolLinearInterpCalc(expectedValue: 0.2, valuationDate: new Date(2018, 3, 21), tradeOpenVol: 0.2, tradeCloseVol: 0.4, dayCountMode: DayCountMode.TradingDay);
            TradeVolLinearInterpCalc(expectedValue: 0.4, valuationDate: new Date(2018, 5, 8), tradeOpenVol: 0.2, tradeCloseVol: 0.4, dayCountMode: DayCountMode.TradingDay);
            //TradingDay: NonBizDay = PrevBizDay
            TradeVolLinearInterpCalc(expectedValue: 0.213333333333333, valuationDate: new Date(2018, 3, 25), tradeOpenVol: 0.2, tradeCloseVol: 0.4, dayCountMode: DayCountMode.TradingDay);
            TradeVolLinearInterpCalc(expectedValue: 0.213333333333333, valuationDate: new Date(2018, 3, 23), tradeOpenVol: 0.2, tradeCloseVol: 0.4, dayCountMode: DayCountMode.TradingDay);
            TradeVolLinearInterpSlopeCalc(0.2, 0.4);
        }

        private void TradeVolLinearInterpCalc(double expectedValue, Date valuationDate, double tradeOpenVol, double tradeCloseVol, DayCountMode dayCountMode)
        {
            var startDate = new Date(2018, 3, 21);
            var maturityDate = new Date(2018, 5, 21);
            var numOfSmoothingDays = 30;
            var calendar = CalendarImpl.Get("chn");
            double vol = AnalyticalOptionTradeVolInterp.tradeVolLinearInterp(valuationDate, tradeOpenVol, tradeCloseVol, startDate, maturityDate,
             numOfSmoothingDays, dayCountMode, calendar);

            Assert.AreEqual(expectedValue, vol , 1e-10);
        }

        private void TradeVolLinearInterpSlopeCalc(double tradeOpenVol, double tradeCloseVol, DayCountMode dayCountMode=DayCountMode.TradingDay)
        {
            var startDate = new Date(2018, 3, 21);
            var maturityDate = new Date(2018, 5, 21);
            var valuationDate = new Date[] { new Date(2018, 3, 22), new Date(2018, 3, 23), new Date(2018, 3, 26) };
            var numOfSmoothingDays = 30;
            var calendar = CalendarImpl.Get("chn");
            double vol1 = AnalyticalOptionTradeVolInterp.tradeVolLinearInterp(valuationDate[0], tradeOpenVol, tradeCloseVol, startDate, maturityDate,
             numOfSmoothingDays, dayCountMode, calendar);
            double vol2 = AnalyticalOptionTradeVolInterp.tradeVolLinearInterp(valuationDate[1], tradeOpenVol, tradeCloseVol, startDate, maturityDate,
             numOfSmoothingDays, dayCountMode, calendar);
            double vol3 = AnalyticalOptionTradeVolInterp.tradeVolLinearInterp(valuationDate[2], tradeOpenVol, tradeCloseVol, startDate, maturityDate,
             numOfSmoothingDays, dayCountMode, calendar);

            Assert.AreEqual(vol2 - vol1, vol3 - vol2, 1e-10);
        }

    }

}
