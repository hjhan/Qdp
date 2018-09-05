using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.Random;
using Qdp.Pricing.Library.Common.Interfaces;
using Qdp.Pricing.Library.Common.Market;
using Qdp.Pricing.Library.Common.MathMethods.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Processes;
using Qdp.Pricing.Library.Equity.Interfaces;
using Qdp.Pricing.Library.Equity.Engines.Analytical;
using System.IO;
using System;

/// <summary>
/// Qdp.Pricing.Library.Equity.Engines.MonteCarlo
/// </summary>
namespace Qdp.Pricing.Library.Equity.Engines.MonteCarlo
{
    /// <summary>
    /// 通用蒙特卡洛计算引擎
    /// </summary>
    //TODO; Review changes;
	public class GenericMonteCarloEngine : BaseNumericalOptionEngine
	{
        /// <summary>
        /// 并行度
        /// </summary>
		public int ParallelDegrees { get; private set; }

        /// <summary>
        /// 模拟次数
        /// </summary>
		public int NumSimulations { get; private set; }

        /// <summary>
        /// 是否记录路径结果 
        /// </summary>
        public bool MonteCarloCollectPath { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="parallelDegrees">并行度</param>
        /// <param name="numSimulations">模拟次数</param>
        /// <param name="monteCarloCollectPath">是否记录路径结果</param>
        /// <param name="spotPriceBump">标的资产现价偏移量</param>
        public GenericMonteCarloEngine(int parallelDegrees, int numSimulations, bool monteCarloCollectPath=false, double spotPriceBump = 0.01)
			: base(spotPriceBump)
		{
			ParallelDegrees = parallelDegrees;
			NumSimulations = numSimulations;
            MonteCarloCollectPath = monteCarloCollectPath;
		}

        protected override double CalcPv(IOption option, IMarketCondition market, double timeIncrement = 0.0)
        {
            throw new System.NotImplementedException();
        }
        /// <summary>
        /// 计算期权现值
        /// </summary>
        /// <param name="option">期权</param>
        /// <param name="markets">市场</param>
        /// <returns>计算结果</returns>
        public override double[] CalcPvs(IOption option, IMarketCondition[] markets)
		{
            if(AnalyticalOptionPricerUtil.isForwardFuturesOption(option.UnderlyingProductType))
			{
                //market = market.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, IYieldCurve>>(m => m.DividendCurves, new Dictionary<string, IYieldCurve> { { "", market.DiscountCurve.Value} }));
                markets = markets.Select(x =>
                        x.UpdateCondition(new UpdateMktConditionPack<Dictionary<string, IYieldCurve>>(m => m.DividendCurves, new Dictionary<string, IYieldCurve> { { "", x.DiscountCurve.Value } }))
                        ).ToArray();
            }
			var stochasticProcesses = CreateStochasticProcess1Ds(option, markets);
			var nProcess = stochasticProcesses.Length;

			var partitioner = Partitioner.Create(0, NumSimulations, NumSimulations/ParallelDegrees + 1);
			var partitionerPvs = Enumerable.Range(0, nProcess).Select(x => new List<double>()).ToArray();
			
			var parallelOption = new ParallelOptions() {MaxDegreeOfParallelism = ParallelDegrees};

            var size = option.ObservationDates.Length * 2 + 3;
            List<double>[] result= null;
            if(MonteCarloCollectPath)
                result = new List<double>[NumSimulations + 1];

			
			Parallel.ForEach(partitioner, parallelOption,
				(range, loopState) =>
				{
					var pvs = Enumerable.Range(0, nProcess).Select(x => new List<double>()).ToArray();
					var random = new MersenneTwister();
					
					for (var n = range.Item1; n < range.Item2; ++n)
					{
						var paths= PathGenerator.GeneratePath(stochasticProcesses,
                            markets.Select(x => x.SpotPrices.Value.Values.First()).ToArray(),
                            markets.Select(x => x.ValuationDate.Value).ToArray(),
                            option.ObservationDates,
							option.Calendar,
							option.DayCount,
							random);

						for (var i = 0; i < nProcess; ++i)
						{
                            pvs[i].Add(
                                option.GetPayoff(paths[i]).Sum(cf => cf.PaymentAmount * markets[i].DiscountCurve.Value.GetDf(markets[i].ValuationDate.Value, cf.PaymentDate))
                                );
                         
                            //pvs[i].Add(option.GetPayoff(paths[i]).Sum(cf => cf.PaymentAmount * market.DiscountCurve.Value.GetDf(market.ValuationDate.Value, cf.PaymentDate)));
                        }
                        

                        //antithetic path
                        for (var i = 0; i < nProcess; ++i)
						{
							pvs[i].Add(option.GetPayoff(paths[i + nProcess]).Sum(cf => cf.PaymentAmount * markets[i].DiscountCurve.Value.GetDf(markets[i].ValuationDate.Value, cf.PaymentDate)));
                            //pvs[i].Add(option.GetPayoff(paths[i + nProcess]).Sum(cf => cf.PaymentAmount * market.DiscountCurve.Value.GetDf(market.ValuationDate.Value, cf.PaymentDate)));
                        }

                        if (MonteCarloCollectPath)
                        {
                            var originalPathPrices = from e in paths[0].ToList() orderby e.Key select e.Value;

                            //original path price evolution
                            //result[n].AddRange(prices.ToList());
                            result[n] = originalPathPrices.ToList();
                            //pv
                            result[n].Add(pvs[0].First());

                            var antiPathPrices = from e in paths[1].ToList() orderby e.Key select e.Value;

                            //antipath  price evolution
                            result[n].AddRange(antiPathPrices);
                            //pv
                            result[n].Add(pvs[0].Last());

                            //expected pv
                            result[n].Add(pvs[0].Average());
                        }
                       
                    }

					lock (partitionerPvs)
					{
						for (var i = 0; i < nProcess; ++i)
						{
							partitionerPvs[i].Add(pvs[i].Average());
						}
                    }

                    //
				});
            if (MonteCarloCollectPath)
            {
                result[NumSimulations] = Enumerable.Repeat(0.0, 2 * (option.ObservationDates.Length + 1)).ToList();
                result[NumSimulations].Add(partitionerPvs[0].Average());
                var resultStr = result.Select(digits => string.Join(",", digits)).ToArray();
                var folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var filename = folder + "/MonteCarlo.csv";
                File.WriteAllLines(filename, resultStr);
            }
                
            //return partitionerPvs[0].Average();
            return partitionerPvs.Select(x => x.Average()).ToArray();
        }

		protected IStochasticProcess1D[] CreateStochasticProcess1Ds(IOption option, IMarketCondition[] markets)
		{
			return markets.Select(mkt =>
			{
				var startDate = mkt.ValuationDate.Value;
				var times = new List<double>(){0.0};
				for (var i = 0; i < option.ObservationDates.Length; ++i)
				{
					times.Add(option.DayCount.CalcDayCountFraction(startDate, option.ObservationDates[i]));
				}
				return new BlackScholesProcess(mkt.DiscountCurve.Value,
					mkt.DividendCurves.HasValue ? mkt.DividendCurves.Value.Values.First() : null,
					mkt.VolSurfaces.Value.Values.First(),
					times.ToArray()) as IStochasticProcess1D;
			}
			).ToArray();
		}
	}
}
