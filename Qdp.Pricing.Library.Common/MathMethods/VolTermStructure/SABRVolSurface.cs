using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Curves;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.Interfaces;

namespace Qdp.Pricing.Library.Common.MathMethods.VolTermStructure
{

	/// <summary>
	/// The Implementation of SABR interpolation for implied vol surface.
	/// The x-Axis is maturity dates
	/// The y-Axis is strike
	/// References
	/// 1. http://www.volopta.com/ComputerCode/Matlab/SABR_Comparison_of_Original_and_Fine_Tuned_Versions.zip
	/// 2. http://www.frouah.com/finance%20notes/The%20SABR%20Model.pdf
	/// 
	/// The purpose of not burying everything into a interpolator is 
	/// 1. we need record the state if the vol surface has been interpolated.
	/// 2. other part of the library may call some functions in this class
	/// 
	/// </summary>
	public class SabrVolSurface : InterpolatedImpliedVolSurface
	{
		private readonly IDayCount _dayCount;
		
		// keep a copy of parameters for bumping
		private double _spotPrice;
		private IYieldCurve _yieldCurve;
		private bool _estimateAlpha;
		private bool _useFineTune;
		private double _initAlpha;
		private double _initBeta;
		private double _initNu;
		private double _initRho;
		
		public Date ValuationDate { get; private set; }
		
		public List<SabrCoeffOptimizerResult> OptimizerResults { get; private set; }

		/// <summary>
		/// SABR model: Pat Hagan et al (2002) "Managing Smile Risk".
		/// Compare Hagan's original model to refinements proposed in the literature
		/// and summarized in "Fine-Tune Your Smile" by Jan Obloj.
		/// There are two ways to estimate the SABR parameters (r,v,a)
		/// (1) Estimate r and v, and obtain a as the cubic root of an equation,
		///     as in Graeme West's paper "SABR in Illiquid Markets"
		/// (2) Estimate r, v, and a directly.  
		/// Refinements to the SABR volatilities are summarized by Jan Obloj.
		/// This class can be configed to produce one of 4 sets of SABR parameters.
		/// (1) Original SABR parameters, estimation method 1.
		/// (2) Original SABR parameters, estimation method 2.
		/// (3) Fine Tuned SABR parameters, estimation method 1.
		/// (4) Fine Tuned SABR parameters, estimation method 2.
		/// </summary>
		/// <param name="rowGrid">X-Axis: Maturity</param>
		/// <param name="colGrid">Y-Axis: Strike</param>
		/// <param name="valueOnGrids">Original Vol Surface</param>
		/// <param name="valuationDate">Valuation Date</param>
		public SabrVolSurface(Date[] rowGrid, double[] colGrid, double[,] valueOnGrids, Date valuationDate, 
			double spotPrice, IYieldCurve yieldCurve, bool estimateAlpha = false, bool useFineTune = true,
			double initAlpha = 0.3, double initBeta = 0.5, double initNu = 0.3, double initRho = 0.3)
			: base(valuationDate, rowGrid, colGrid, valueOnGrids, Interpolation2D.BiLinear)
		{
			if ((rowGrid as ICollection<Date>).Count != valueOnGrids.GetLength(0) || (colGrid as ICollection<double>).Count != valueOnGrids.GetLength(1) )
			{
				throw new PricingLibraryException(string.Format("Size error for the SABR volatility surface defintion"));
			}

			ValuationDate = valuationDate;
			_spotPrice = spotPrice;
			_yieldCurve = yieldCurve;
			_estimateAlpha = estimateAlpha;
			_useFineTune = useFineTune;
			_initAlpha = initAlpha;
			_initBeta = initBeta;
			_initNu = initNu;
			_initRho = initRho;

			_dayCount = new Act360();

			Calibrate(spotPrice,yieldCurve,estimateAlpha,useFineTune,initAlpha,initBeta,initNu,initRho);
		}

		public override double MaxX()
		{
			return RowGrid.Select(item => _dayCount.CalcDayCountFraction(ValuationDate, item)).ToList().Max();
		}

		public override double MinX()
		{
			return RowGrid.Select(item => _dayCount.CalcDayCountFraction(ValuationDate, item)).ToList().Min();
		}

		public override double MaxY()
		{
			return ColGrid.Max();
		}

		public override double MinY()
		{
			return ColGrid.Min();
		}

		public override IVolSurface BumpVolSurf(double volChange)
		{
			var newImpVol = base.BumpVolSurf(volChange) as ImpliedVolSurface;
			return new SabrVolSurface(RowGrid,ColGrid, newImpVol.ValueOnGrids, ValuationDate,
				_spotPrice, _yieldCurve,_estimateAlpha, _useFineTune,_initAlpha,_initBeta,_initNu,_initRho );
		}

		public override IVolSurface BumpMaturitySlice(int index, double volChange)
		{
			var newImpVol = base.BumpMaturitySlice(index, volChange) as ImpliedVolSurface;
			return new SabrVolSurface(RowGrid, ColGrid, newImpVol.ValueOnGrids, ValuationDate,
				_spotPrice, _yieldCurve, _estimateAlpha, _useFineTune, _initAlpha, _initBeta, _initNu, _initRho);
		}

		public override IVolSurface BumpMaturityStrikePoint(int indexMaturity, int indexStrike, double volChange)
		{
			var newImpVol = base.BumpMaturityStrikePoint(indexMaturity, indexStrike, volChange) as ImpliedVolSurface;
			return new SabrVolSurface(RowGrid, ColGrid, newImpVol.ValueOnGrids, ValuationDate,
				_spotPrice, _yieldCurve, _estimateAlpha, _useFineTune, _initAlpha, _initBeta, _initNu, _initRho);
		}

		private void Calibrate(double spotPrice, IYieldCurve yieldCurve, bool estimateAlpha, bool useFineTune,
			double initAlpha, double initBeta, double initNu, double initRho)
		{
			// set ATM vols
			var tempCurve2D = new Curve2D<Date, double>(RowGrid, ColGrid, ValueOnGrids, x => x.ToOADate(), x => x,
				Interpolation2D.BiLinear);
			var atmVols = RowGrid.Select(d => tempCurve2D.GetValue(d, spotPrice)).ToList();

			_spotPrice = spotPrice;
			_yieldCurve = yieldCurve;
			_useFineTune = useFineTune;
			_estimateAlpha = estimateAlpha;

			OptimizerResults = Enumerable.Range(0, RowGrid.Count())
				.Select(i =>
				{
					var t = _dayCount.CalcDayCountFraction(ValuationDate, RowGrid[i]);
					var fowardPrice = _spotPrice / _yieldCurve.GetDf(ValuationDate, RowGrid[i]);
					//var optimizer = new SABRCoeffOptimizer(t, fowardPrice, atmVols[i], AxisX2, Matrix.GetColumn(i), estimateAlpha, useFineTune, initAlpha, initBeta, initNu, initRho);
					var optimizer = new SabrCoeffOptimizer(t, fowardPrice, atmVols[i], ColGrid, ValueOnGrids.GetRow(i), estimateAlpha, useFineTune, initAlpha, initBeta, initNu, initRho);
					return optimizer.Result;
				})
				.ToList();
		}

		public override double GetForwardPrice(double t)
		{
			return _spotPrice / _yieldCurve.GetDf(t);
		}

		#region Private properties

		/// <summary>
		/// Return the weight for interpolating between two maturities
		/// Ref: OpenGamma Quantitative Research Local Volatility
		/// http://developers.opengamma.com/quantitative-research/Local-Volatility-OpenGamma.pdf
		/// </summary>
		/// <param name="y">relative distance to the left anchor</param>
		/// <returns>the weight for interpolating between two maturities</returns>
		private double Weight(double y)
		{
			return 0.5 * (Math.Sin(Math.PI * (y - 0.5)) + 1);
		}

		#endregion

		/// <summary>
		/// Get a point from the implied vol surface
		/// </summary>
		/// <param name="t">date</param>
		/// <param name="k">strike</param>
		/// <returns>implied vol</returns>
		public double GetValue(Date t, double k)
		{
			return GetValue(_dayCount.CalcDayCountFraction(ValuationDate, t), k);
		}

		/// <summary>
		/// Get a point from the implied vol surface
		/// </summary>
		/// <param name="t">date in double</param>
		/// <param name="k">strike</param>
		/// <returns></returns>
		public double GetValue(double t, double k)
		{
			//check if the point is out of range
			var axisX1InDouble = RowGrid.Select(item => _dayCount.CalcDayCountFraction(ValuationDate, item)).ToList();
			if (t < axisX1InDouble.Min() || t > axisX1InDouble.Max() || k < ColGrid.Min() || k > ColGrid.Max())
			{
				throw new PricingLibraryException(String.Format("Date - strike pair ({0},{1}) is out of range. Extrapolation is not implemented yet", t, k));
			}

			//find the two indices which on the two sides of the input date. If cannot find in the for loop, then i = 0 automatically.
			int i = 0;
			for (int j = 0; j < axisX1InDouble.Count - 1; j++)
			{
				if ((axisX1InDouble[j] < t) && (t <= axisX1InDouble[j + 1]))
				{
					i = j;
				}
			}

			// get the left and right value of the sabr interpolated vol with the same strike s
			var leftValue = SABRVolFineTune(
				OptimizerResults[i].BestAlpha,
				OptimizerResults[i].BestBeta,
				OptimizerResults[i].BestRho,
				OptimizerResults[i].BestNu,
				_spotPrice,
				k,
				OptimizerResults[i].Maturity,
				_useFineTune);
			var rightValue = SABRVolFineTune(
				OptimizerResults[i + 1].BestAlpha,
				OptimizerResults[i + 1].BestBeta,
				OptimizerResults[i + 1].BestRho,
				OptimizerResults[i + 1].BestNu,
				_spotPrice,
				k,
				OptimizerResults[i + 1].Maturity,
				_useFineTune);

			var w = Weight((OptimizerResults[i + 1].Maturity - t) / (OptimizerResults[i + 1].Maturity - OptimizerResults[i].Maturity));

			return w * leftValue + (1 - w) * rightValue;
		}

		/// <summary>
		/// Static function to calculate vol given below parameters
		/// </summary>
		/// <param name="a">alpha</param>
		/// <param name="b">beta</param>
		/// <param name="r">rho</param>
		/// <param name="v">nu</param>
		/// <param name="f">forward price</param>
		/// <param name="k">strike</param>
		/// <param name="t">maturity</param>
		/// <param name="fineTune">fine tune or not</param>
		/// <returns>calculated vol</returns>
		public static double SABRVolFineTune(double a, double b, double r, double v, double f, double k, double t, bool fineTune)
		{
			double i0H, i0B;

			var x = Math.Log(f / k);

			// Separate out into cases x=0, b=1, and b<1.
			if (Math.Abs(x) <= 0.0001)
			{
				i0H = a * Math.Pow(k, (b - 1));
				i0B = i0H;
			}
			else if (Math.Abs(v) <= 0.001) // ATM vol
			{
				i0H = x * a * (1 - b) / (Math.Pow(f, (1 - b)) - Math.Pow(k, (1 - b)));
				i0B = i0H;
			}
			else if (Math.Abs(b - 1.0) < double.Epsilon * 42)
			{
				var z = v * x / a;
				var sq = Math.Sqrt(1 - 2 * r * z + z * z);
				i0H = v * x / Math.Log((sq + z - r) / (1 - r));
				i0B = i0H;
			}
			else
			{
				var z = v * (Math.Pow(f, (1 - b)) - Math.Pow(k, (1 - b))) / a / (1 - b);
				var e = v * (f - k) / a / Math.Pow(f * k, b / 2);
				var sq = Math.Sqrt(1 - 2 * r * e + Math.Pow(e, 2));
				i0H = v * x * e / z / Math.Log((sq + e - r) / (1 - r));
				i0B = v * x / Math.Log((sq + z - r) / (1 - r));
			}

			var i1H = Math.Pow((b - 1) * a, 2) / 24 / Math.Pow((f * k), (1 - b)) + r * v * a * b / 4 / Math.Pow((f * k), (1 - b) / 2) + (2 - 3 * r * r) * v * v / 24;

			// Original Hagan SABR implied vol.
			var haganVol = i0H * (1 + i1H * t);

			// Fine tuned SABR implied vol.
			var refinedVol = i0B * (1 + i1H * t);

			return fineTune ? refinedVol : haganVol;
		}


	}
}
