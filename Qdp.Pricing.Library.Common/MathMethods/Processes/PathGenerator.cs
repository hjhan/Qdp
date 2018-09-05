using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Common.MathMethods.Interfaces;
using Random = MathNet.Numerics.Random.RandomSource;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes
{
	public static class PathGenerator
	{
		public static List<Dictionary<Date, double>> GeneratePath(
			IStochasticProcess1D[] processes,
			double[] x0,
			Date[] startDates,
			Date[] observationDates,
			ICalendar calendar,
			IDayCount basis,
			Random random)
		{
			var currentX = x0.ToList();
			var processArray = processes.ToArray();


			var count = processArray.Count();
			var results = new List<Dictionary<Date, double>>[count * 2]
				.Select(x => new Dictionary<Date, double>())
				.ToList();

			var normalRandomSamples = GetRandomSamples(random, observationDates.Length);

			//original path
			for (var i = 0; i < processArray.Length; ++i)
			{
				var start = startDates[i];
				var obsDates = observationDates.Where(x => x > start).ToArray();
				results[i][start] = currentX[i];
				var currentT = 0.0;

				for (int j = 0; j < obsDates.Count(); j++)
				{
					var dt = basis.CalcDayCountFraction(start, obsDates[j]);
					currentX[i] = processArray[i].Evolve(currentT, currentX[i], dt, normalRandomSamples[j]);
					currentT += dt;
					results[i][obsDates[j]] = currentX[i];
					start = obsDates[j];
				}
			}

			//antithetic path
			currentX = x0.ToList();
			for (var i = 0; i < processArray.Length; ++i)
			{
				var start = startDates[i];
				var obsDates = observationDates.Where(x => x > start).ToArray();
				results[i + count][start] = currentX[i];
				var currentT = 0.0;

				for (int j = 0; j < obsDates.Count(); j++)
				{
					var step = basis.CalcDayCountFraction(start, obsDates[j]);
					currentX[i] = processArray[i].Evolve(currentT, currentX[i], step, -normalRandomSamples[j]);
					currentT += step;
					results[i + count][obsDates[j]] = currentX[i];
					start = obsDates[j];
				}
			}

			return results;
		}

		public static List<Dictionary<Date, List<double>>> GeneratePathND(
			IStochasticProcessNd process,
			double[] x0,
			Date startDate,
			Date[] obsDates,
			ICalendar calendar,
			IDayCount basis,
			Random random)
		{
			var currentX = x0.ToList();

			var results = new List<Dictionary<Date, List<double>>>[2]
				.Select(x => new Dictionary<Date, List<double>>())
				.ToList();

			var normalRandomSamples = new List<List<double>>();
			for (var i = 0; i < obsDates.Length; ++i) // i for each market
			{
				normalRandomSamples.Add(GetRandomSamples(random, process.Size));
			}

			//original path
			obsDates = obsDates.Where(x => x > startDate).ToArray();
			results[0][startDate] = currentX;
			var currentT = 0.0;

			for (var i = 0; i < obsDates.Count(); i++)
			{
				var dt = basis.CalcDayCountFraction(startDate, obsDates[i]);
				currentX = process.Evolve(currentT, currentX, dt, normalRandomSamples[i]);
				currentT += dt;
				results[0][obsDates[i]] = currentX;
				startDate = obsDates[i];
			}

			//antithetic path
			obsDates = obsDates.Where(x => x > startDate).ToArray();
			results[1][startDate] = currentX;
			currentT = 0.0;

			for (var i = 0; i < obsDates.Count(); i++)
			{
				var dt = basis.CalcDayCountFraction(startDate, obsDates[i]);
				currentX = process.Evolve(currentT, currentX, dt, normalRandomSamples[i].Select(x => -x).ToList());
				currentT += dt;
				results[1][obsDates[i]] = currentX;
				startDate = obsDates[i];
			}

			return results;
		}


		private static List<double> GetRandomSamples(Random baseGen, int steps)
		{
			lock (baseGen)
			{
				var randomSamples = new List<double>();
				for (int i = 0; i < steps; i++)
				{
					var temp = Normal.Sample(baseGen, 0, 1);
					randomSamples.Add(temp);
				}
				return randomSamples;
			}

		}
	}
}