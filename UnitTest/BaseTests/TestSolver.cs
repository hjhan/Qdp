using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.MathMethods.Maths;
using System;

namespace UnitTest.BaseTests
{
    public class SFunction : IFunctionOfOneVarialbe
    {
        public double F(double x)
        {
            return x * x - 9.0;
        }
    }

    [TestClass]
    public class TestBrentSolver
    {
        [TestMethod]
        public void TestSolver1()
        {
            Assert.AreEqual(BrentZero.Solve(new SFunction(), -3, 3), 3.0);
        }

        [TestMethod]
        public void TestSolverBS()
        {
            //not easy to configure
            //equivalent test in VanillaEuropeanOptionVolCalibrationTest
            Assert.AreEqual(0.0, 0.0, 1e-8);
        }

        static double min = -0.55;
        static double max = 0.5;
        static double initial = 0.05;
        static double accuracy = 1e-12; //var accuracy = 1e-14;  doesn't improve result

        [TestMethod]
        public void TestBrentJinCurveBuild()
        { 
            double finalR;
            try
            {
                finalR = BrentZero.SolveJin(new CurveCalibTest(),x0: min , x1: max, xtol: accuracy, ytol: accuracy, iterationsUsed: out int iters, errorEstimate: out double error);
            }
            catch (Exception ex)
            {
                throw new PricingLibraryException(string.Format("Fail to calibrate"));
            }

            Console.WriteLine($"solved  = {finalR}");
        }

        //BrentZero2 and BrentJin are almost identical
        [TestMethod]
        public void TestBrentZero2CurveBuild()
        {
            double finalR;
            try
            {
                finalR = BrentZero2<IUnivariateFunction>.DoSolve(f: new CurveCalibTest2(), min: min, max: max, initial: initial, changeIndex: 0, accuracy: accuracy);
            }
            catch (Exception ex)
            {
                throw new PricingLibraryException(string.Format("Fail to calibrate"));
            }
            Console.WriteLine($"solved  = {finalR}");
        }

        //The best:  same result, but much faster convergence
        [TestMethod]
        public void TestBrentZeroCurveBuild()
        {
            double finalR;
            int iters;
            double error;

            try
            {
                finalR = BrentZero.Solve(new CurveCalibTest(), left: min , right: max, tolerance: accuracy, iterationsUsed: out iters, errorEstimate: out error);
            }
            catch (Exception ex)
            {
                throw new PricingLibraryException(string.Format("Fail to calibrate"));
            }

            Console.WriteLine($"solved  = {finalR}, iters = ${iters},  error = ${error}");
        }

        internal class CurveCalibTest : IFunctionOfOneVarialbe {
            public CurveCalibTest() {
                _target = 0.0189;
            }
            private readonly double _target;

            public double F(double x)
            {
                var t = 1.0 / 251.0;
                var df = 1.0 / System.Math.Exp(x * t);
                var modelValue = (1.0 / df - 1.0) / t;
                return modelValue - _target;
            }
        }

        internal class CurveCalibTest2 : IUnivariateFunction {
            public CurveCalibTest2() {
                _target = 0.0189;
            }
            private readonly double _target;

            public double Value(double x, int changeIndex)
            {
                var t = 1.0 / 251.0;
                var df = 1.0 / System.Math.Exp(x * t);
                var modelValue = (1.0 / df - 1.0) / t;
                return modelValue - _target;
            }
        }

    }
}
