using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Base.Curves.Interpolators;

namespace UnitTest.BaseTests
{
	[TestClass]
	public class LogLinearInterpolatorTest
	{
		[TestMethod]
		public void TestConvexMonotoneInterpolator()
		{
			var n = 3;
			var xmin = -1.7;
			var xmax = 1.9;
			var dx = (xmax - xmin)/(n - 1);
			var x = new double[n];
			for (var i = 0; i < n - 1; ++i)
			{
				x[i] = xmin + i*dx;
			}
			x[n - 1] = xmax;
			var points = x.Select(i => Tuple.Create(i, -i*i)).ToArray();
			var interpolator = new ConvexMonoticInterpolator(points);
			var xval = interpolator.GetValue(0.5);
			var xval2 = interpolator.GetValue(1.66);
			var xint = interpolator.GetIntegral(0.5);
			var xint2 = interpolator.GetIntegral(1.66);
			Assert.AreEqual(xval, -1.67805115729379, 1e-10);
			Assert.AreEqual(xval2, -1.3236363726756, 1e-10);
			Assert.AreEqual(xint, -0.715443789612429, 1e-10);
			Assert.AreEqual(xint2, -2.45236320680455, 1e-10);
			Console.WriteLine("{0},{1},{2},{3}", interpolator.GetValue(0.5), interpolator.GetValue(1.66), interpolator.GetIntegral(0.5), interpolator.GetIntegral(1.66));
		}

		[TestMethod]
		public void TestConvexMonotoneInterpolator2()
		{
			var n = 10;
			var xmin = -5.0;
			var xmax = 5.0;
			var dx = (xmax - xmin) / (n - 1);
			var x = new double[n];
			for (var i = 0; i < n - 1; ++i)
			{
				x[i] = xmin + i * dx;
			}
			x[n - 1] = xmax;
			var points = x.Select(i => Tuple.Create(i, Math.Sin(i))).ToArray();
			var interpolator = new ConvexMonoticInterpolator(points, 0.3, 0.7, true, false, true);

			Assert.AreEqual(interpolator.GetValue(0.5), 0.76253416320106471, 1e-10);
			Assert.AreEqual(interpolator.GetValue(2.86), -0.32126930529034564, 1e-10);
			Assert.AreEqual(interpolator.GetValue(6.0), -0.41908206609122495, 1e-10);
			Assert.AreEqual(interpolator.GetIntegral(0.5), -0.78855126693586397, 1e-10);
			Assert.AreEqual(interpolator.GetIntegral(2.86), 0.69184961567178349, 1e-10);
			Assert.AreEqual(interpolator.GetIntegral(6.0), -1.4845534823836011, 1e-10);
		}


		[TestMethod]
		public void TestLogLinearInterpolator()
		{
			var points = new[]
			{
				Tuple.Create(0.0, 1.0),
				Tuple.Create(1.0, 0.9),
				Tuple.Create(2.0, 0.8),
				Tuple.Create(3.0, 0.7),
			};
			var interpolator = new LogLinearInterpolator(points, Extrapolation.Natural);

			var target = new[]
			{
				1.0,
				0.94868329805,
				0.9,
				0.84852813742,
				0.8,
				0.74833147735,
				0.7,
				0.65479004268,
				0.6125,
				0.57294128734,
			};

			var x = 0.0;
			for (var i = 0; i < 10; ++i)
			{
				Assert.AreEqual(interpolator.GetValue(x), target[i], 1e-10);
				x += 0.5;
			}
		}
	}
}
