using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Processes;
using Qdp.Pricing.Library.Common.MathMethods.Processes.Trees;
using Qdp.Pricing.Library.Equity.Interfaces;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Common.Market;

namespace Qdp.Pricing.Library.Equity.Engines.Numerical
{
    /// <summary>
    /// 美式期权的二叉树计算引擎。
    /// </summary>
    //Value american option on Cox Ross Rubinstein binomial tree
    // John Hull, 9th edition (21.8) - (21.10) p. 455-456 have faster approximation
    public class BinomialTreeAmericanEngine : BaseNumericalOptionEngine
	{
		private readonly BinomialTreeType _binomialTreeType;
		private readonly int _steps;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="binomialTreeType">二叉树类型</param>
        /// <param name="steps">二叉树深度</param>
		public BinomialTreeAmericanEngine(BinomialTreeType binomialTreeType=BinomialTreeType.CoxRossRubinstein, int steps=30)
		{
			_binomialTreeType = binomialTreeType;
			_steps = steps;
		}

        protected override double CalcPv(IOption option, IMarketCondition market, double timeIncrement = 0.0) {
            return DoCalcPv(option, market, timeIncrement : timeIncrement);
        }

        protected override double CalcExpiryDelta(IOption option, IMarketCondition[] markets, double T)
        {
            var pvBase = DoCalcPv(option, markets[0], T);
            var pvUp = DoCalcPv(option, markets[1], T);
            return (pvUp - pvBase) / SpotPriceBump;
        }

        private double DoCalcPv(IOption option, IMarketCondition market, double expiryDayRemainingLife = double.NaN, double timeIncrement = 0.0)
		{
            var valueDate = market.ValuationDate;
            var dayCount = market.DiscountCurve.Value.DayCount;
            var exerciseDates = option.ExerciseDates.Where(x => x >= market.ValuationDate).ToArray();
            var tExStart = dayCount.CalcDayCountFraction(valueDate, exerciseDates[0]) + timeIncrement;
            var tExEnd = dayCount.CalcDayCountFraction(valueDate, exerciseDates.Last()) + timeIncrement;
            if (!double.IsNaN(expiryDayRemainingLife))
            {
                tExEnd = expiryDayRemainingLife;
            }
            
            var spot = market.SpotPrices.Value.Values.First();

            Tuple<Date, double>[] dividends = (option.Dividends != null)? option.Dividends.Select(x => new Tuple<Date, double>(x.Key, x.Value)).ToArray(): null;
            if (dividends!= null)
                Array.Sort(dividends, (o1, o2) => o1.Item1.CompareTo(o2.Item1));
            var sigma = AnalyticalOptionPricerUtil.pricingVol(volSurf: market.VolSurfaces.Value.Values.First(),
                exerciseDate: option.ExerciseDates.Last(), option: option, spot: spot);
            //Notional is been considered during simulation.
            var fa = BinomialDiscreteDividends(option, market,
                market.ValuationDate,
                dayCount: market.DiscountCurve.Value.DayCount,
                spotPrice: market.SpotPrices.Value.Values.First(),
                r: market.DiscountCurve.Value.ZeroRate(market.ValuationDate, option.ExerciseDates.Last()),
                v: sigma,
                dividends: dividends,
                steps: _steps,
                tStart: tExStart,
                tEnd: tExEnd);

            var option2 = option.Clone(OptionExercise.European);
            var bsmEngine = new AnalyticalVanillaEuropeanOptionEngine();

            //if cash dividends, then annualize it to equivalent dividend yield
            //var diviFraction = dayCount.CalcDayCountFraction(option.StartDate, option.ExerciseDates.Last());
            //var eqvDividendYield = (dividends ==null)? 0.0: dividends.Select(x => x.Item2).Sum() / spot / diviFraction;
            //var eqvDividendCurve = new YieldCurve(
            //    "dividend",
            //    valueDate,
            //    new[]
            //    {
            //        Tuple.Create((ITerm)new Term("1D"), eqvDividendYield),
            //        Tuple.Create((ITerm)new Term("1Y"), eqvDividendYield)
            //    },
            //    BusinessDayConvention.ModifiedFollowing,
            //    dayCount,
            //    market.DividendCurves.Value.First().Value.Calendar,
            //    CurrencyCode.CNY,
            //    Compound.Continuous,
            //    Interpolation.CubicHermiteMonotic,
            //    YieldCurveTrait.SpotCurve
            //    );

            //var eqvMarket = market.UpdateDividendCurve(eqvDividendCurve, market.DividendCurves.Value.First().Key);
            
            var fbsm = bsmEngine.Calculate(option2, market, PricingRequest.Pv).Pv;

            var fe = BinomialDiscreteDividends(option2, market,
                market.ValuationDate,
                dayCount: market.DiscountCurve.Value.DayCount,
                spotPrice: spot,
                r: market.DiscountCurve.Value.ZeroRate(market.ValuationDate, option.ExerciseDates.Last()),
                v: sigma,
                dividends: dividends,
                steps: _steps,
                tStart: tExStart,
                tEnd: tExEnd);

            //control variate
            //fa + ( fbsm - fe)
            return fa + (fbsm - fe);
        }

        //private double originalPv(IOption option, IMarketCondition market) {
        //    var valueDate = market.ValuationDate;
        //    var dayCount = market.DiscountCurve.Value.DayCount;
        //    var tExStart = dayCount.CalcDayCountFraction(valueDate, option.ExerciseDates[0]);
        //    var tExEnd = dayCount.CalcDayCountFraction(valueDate, option.ExerciseDates.Last());

        //    var binomialTree = BuildTree(market, tExEnd, option.Strike, market.SpotPrices.Value.Values.First(), _steps);
        //    var treeEngine = new BinomialTreeWithNoDividend(binomialTree, option, _steps, tExStart, tExEnd);
        //    return treeEngine.ReverseInduction();
        //}

        private double BinomialDiscreteDividends(
            IOption option, IMarketCondition market,
            Date valueDate,
            IDayCount dayCount,
            double spotPrice,
            double r,
            double v,
            Tuple<Date,double>[] dividends,
            int steps, double tStart, double tEnd)
        {    
            var dt = (tEnd - tStart) / steps;
            var df = Math.Exp(-r*dt);
            var u = Math.Exp(v * Math.Sqrt(dt));
            var d = 1 / u;
            var a = Math.Exp(r * dt);
            var p = (a - d) / (u - d);
            var uu = u * u;

            if (dividends == null || dividends.Length == 0) {
                var binomialTree = BuildTree(market, tEnd, option.Strike, spotPrice, steps, vol: v, r: r);
                var treeEngine = new BinomialTreeWithNoDividend(binomialTree, option, steps, tStart, tEnd);
                return treeEngine.ReverseInduction();
            }

            var cashDividendDate = dividends.First().Item1;
            var cashDividendAmount = dividends.First().Item2;

            var tCashDividend = dayCount.CalcDayCountFraction(valueDate, cashDividendDate);
            var stepsBeforeDividend = Convert.ToInt16(Math.Floor(tCashDividend / (tEnd - tStart) * steps));

            var stockPriceNodes = new List<double>();
            var optionValueNodes = new List<double>();

            var p0 = spotPrice * Math.Pow(d, stepsBeforeDividend);
            stockPriceNodes.Add( p0 );

            foreach (int i in Enumerable.Range(1, stepsBeforeDividend)) {
                stockPriceNodes.Add(stockPriceNodes[i - 1] * uu);
            }

            //option value for the node time step just before dividend
            foreach (int i in Enumerable.Range(0, stepsBeforeDividend)) {
                var valueNotExercising = BinomialDiscreteDividends(option, market, valueDate, dayCount, 
                    spotPrice: stockPriceNodes[i] - cashDividendAmount,
                    r:r, v:v, dividends:dividends.Skip(1).ToArray(), steps: steps - stepsBeforeDividend,
                    tStart: tCashDividend,
                    tEnd: tEnd);

                var exerciseValue = option.GetPayoff(new[] { stockPriceNodes[i] }).Sum(x => x.PaymentAmount);
                if (option.Exercise == OptionExercise.American)
                    optionValueNodes.Add(Math.Max(valueNotExercising, exerciseValue));
                else
                    optionValueNodes.Add(valueNotExercising);
            }

            //option value before dividend , reverse induction, normal binomial
            foreach (int j in Enumerable.Range(0, stepsBeforeDividend-1).Reverse() ) {
                foreach (int i in Enumerable.Range(0, j)) {
                    stockPriceNodes[i] = d * stockPriceNodes[i+1];

                    var valueNotExercising = (p * optionValueNodes[i+1] + (1 - p) * optionValueNodes[i]) * df;
                    if (option.Exercise == OptionExercise.American)
                    {
                        var exerciseValue = option.GetPayoff(new[] { stockPriceNodes[i] }).Sum(x => x.PaymentAmount);
                        optionValueNodes[i] = Math.Max(valueNotExercising, exerciseValue);
                    }
                    else {
                        optionValueNodes[i] = valueNotExercising;
                    }
                }
            }

            return optionValueNodes[0];
        }

        private BinomialTree BuildTree(IMarketCondition market, double tEnd, double strike, double spotPrice, int steps, double vol, double r)
        {
            //most suitable arrangement to deal with both strike vol and moneyness vol
            var process = new BlackScholesProcess(
                r : r,
                q: 0.0,
                sigma: vol
                );

            return _binomialTreeType == BinomialTreeType.CoxRossRubinstein
				? (BinomialTree) new CoxRossRubinsteinBinomialTree(process, spotPrice, tEnd, steps)
				: new LeisenReimerBinomialTree(process, spotPrice, strike, tEnd, steps);
		}

	}

    internal class BinomialTreeWithNoDividend
    {
        private BinomialTree _binomialTree;
        private int _steps;
        private double _tExStart, _tExEnd;
        private IOption _option;

        public BinomialTreeWithNoDividend(BinomialTree binomialTree, IOption option,  
            int steps, double tExStart, double tExEnd) {
            _binomialTree = binomialTree;
            _option = option;
            _steps = steps;
            _tExStart = tExStart;
            _tExEnd = tExEnd;   
        }

        public double ReverseInduction() {

            var optionPrices = new double[_steps + 1][];
            optionPrices[_steps] = new double[_steps + 1];
            for (var j = 0; j < _steps + 1; ++j)
            {
                optionPrices[_steps][j] = _option.GetPayoff(new[] { _binomialTree.StateValue(_steps, j) }).Sum(x => x.PaymentAmount);
            }

            for (var i = _steps - 1; i >= 0; --i)
            {
                optionPrices[i] = new double[i + 1];
                for (var j = 0; j <= i; ++j)
                {
                    var pu = _binomialTree.Probability(i, j, BranchDirection.Up);
                    var pd = _binomialTree.Probability(i, j, BranchDirection.Down);

                    var dt = _binomialTree.Dt;
                    var df = Math.Exp(- _binomialTree.Process.GetDiscountRate(0.0) * dt);

                    var optionValue = df * (pu * optionPrices[i + 1][j + 1] + pd * optionPrices[i + 1][j]);

                    if (_option.Exercise == OptionExercise.European)
                    {
                        optionPrices[i][j] = optionValue;
                    }
                    else
                    {
                        optionPrices[i][j] = ((i * dt) >= _tExStart && (i * dt) <= _tExEnd)
                        ? Math.Max(_option.GetPayoff(new[] { _binomialTree.StateValue(i, j) }).Sum(x => x.PaymentAmount), optionValue)
                        : optionValue;
                    }                  
                }
            }

            return optionPrices[0][0];
        }
    }

}
