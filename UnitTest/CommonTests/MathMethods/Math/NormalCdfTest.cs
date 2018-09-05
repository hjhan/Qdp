using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Library.Common.MathMethods.Maths;

namespace UnitTest.CommonTests.MathMethods.convertible
{
    [TestClass]
    public class NormalCdfTest
    {
        public void DoTestNormalCdfGenz(double x,double y,double rho,double actual)
        { 
            var A = NormalCdf.NormalCdfGenz(x, y, rho);
            Assert.AreEqual(A, actual, 1e-8);
        }
        [TestMethod]
        public void TestNormalCdfGenz()
        {
            DoTestNormalCdfGenz(0.5, 0.5, -0.5, 0.4192231090);

        }
        public void DoTestNormalCdfWest(double x)
        {
            var A = NormalCdf.NormalCdfHart(x);
            var B = NormalCdf.NormalCdfWest(x);
            Assert.AreEqual(A, B, 1e-8);
        }

        [TestMethod]
        public void TestNormalCdfWest()
        {
            DoTestNormalCdfWest(0.75);

        }

        public void DoTestBiNormalCdfWest(double x, double y, double rho)
        {
            var A = NormalCdf.NormalCdfGenz(x, y, rho);
            var B = NormalCdf.NormalBiCdfWest(x, y, rho);

            Assert.AreEqual(A, B, 1e-8);
        }
        [TestMethod]
        public void TestBiNormalCdfWest()
        {
            DoTestBiNormalCdfWest(-0.78, 10, 0);
            DoTestBiNormalCdfWest(-0.78, 10, 0.25);
            DoTestBiNormalCdfWest(-0.78, 10, 0.5);
            DoTestBiNormalCdfWest(-0.78, 10, 0.725);
            DoTestBiNormalCdfWest(-0.78, 10, 0.8);
            DoTestBiNormalCdfWest(-0.78, 10, 0.99);
            DoTestBiNormalCdfWest(-0.78, 10, -0.25);
            DoTestBiNormalCdfWest(-0.78, 10, -0.5);
            DoTestBiNormalCdfWest(-0.78, 10, -0.725);
            DoTestBiNormalCdfWest(-0.78, 10, -0.8);
            DoTestBiNormalCdfWest(-0.78, 10, -0.99);

        }

    }

    [TestClass]
    public class InverseNormalCdfTest
    {
        public void DoTestInverseNormalCdf(double U,double actual)
        {
            var A = NormalCdf.NormSInv(U);
            Assert.AreEqual(A, actual, 1e-6);
        }
        [TestMethod]
        public void TestInverseNormalCdf()
        {

            DoTestInverseNormalCdf(0.025, -1.959964);

        }
    }

    [TestClass]

    public class MaxTest
    {
        [TestMethod]
        public void MathMaxTest()
        {
            var max= System.Math.Max(250.01 - 250, 0);
            var m = System.Math.Round(max, 4);
        }
    }

}
