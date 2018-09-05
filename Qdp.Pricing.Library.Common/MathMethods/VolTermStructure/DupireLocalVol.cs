using System;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.MathMethods.Interfaces;

namespace Qdp.Pricing.Library.Common.MathMethods.VolTermStructure
{
	public class DupireLocalVol : ILocalVolSurface
	{
		private readonly double _minLocalVol;
		private readonly double _maxLocalVol;
		private readonly IDayCount _dayCount;

		public DupireLocalVol(Date valuationDate, InterpolatedImpliedVolSurface impliedVol)
		{
			ValuationDate = valuationDate;
			ImpliedVol = impliedVol;
			TimeBump = new BumpingStrategy(2.74e-3, 2.74e-3, false);
			PriceBump = new BumpingStrategy(1e-4, 1e-4, true);

			_dayCount = new Act360();
			_minLocalVol = 0.05;
			_maxLocalVol = 2.0;
		}

		public Date ValuationDate { get; private set; }

		public BumpingStrategy TimeBump { get; private set; }
		public BumpingStrategy PriceBump { get; private set; }

		public InterpolatedImpliedVolSurface ImpliedVol { get; private set; }

		private double MinX()
		{
			var solidMinX = ImpliedVol.MinX();
			return TimeBump.BumpBackwardInverse(solidMinX);
		}

		private double MaxX()
		{
			var solidMaxX = ImpliedVol.MaxX();
			return TimeBump.BumpForwardInverse(solidMaxX);
		}

		private double MinY()
		{
			var solidMinY = ImpliedVol.MinY();
			return PriceBump.BumpBackwardInverse(solidMinY);
		}

		private double MaxY()
		{
			var solidMaxY = ImpliedVol.MaxY();
			return PriceBump.BumpForwardInverse(solidMaxY);
		}

		public IVolSurface BumpVolSurf(double volChange)
		{
			var newImpliedVol = ImpliedVol.BumpVolSurf(volChange) as InterpolatedImpliedVolSurface;
			return new DupireLocalVol(ValuationDate, newImpliedVol);
		}

		public IVolSurface BumpMaturitySlice(int index, double volChange)
		{
			var newImpliedVol = ImpliedVol.BumpMaturitySlice(index, volChange) as InterpolatedImpliedVolSurface;
			return new DupireLocalVol(ValuationDate, newImpliedVol);
		}

		public IVolSurface BumpMaturityStrikePoint(int indexMaturity, int indexStrike, double volChange)
		{
			var newImpliedVol = ImpliedVol.BumpMaturityStrikePoint(indexMaturity,indexStrike,volChange) as InterpolatedImpliedVolSurface;
			return new DupireLocalVol(ValuationDate, newImpliedVol);
		}

		public Date[] RowGrid { get; private set; }
		public double[] ColGrid { get; private set; }
		public double[,] ValueOnGrids { get; private set; }

		public double GetValue(Date t, double k)
		{
			return GetValue(_dayCount.CalcDayCountFraction(ValuationDate, t), k);
		}

		/// <summary>
		/// Function to return the local vol value from SABR interpolated implied vol surface
		/// The local vol suface should be extrapolated from the implied vol surf region 
		/// by using the value from the nearest border.
		/// </summary>
		/// <param name="t">time</param>
		/// <param name="k">strike or moneyness</param>
		/// <returns>local vol</returns>
		public double GetValue(double t, double k)
		{
			if (t > MaxX())
				throw new PricingLibraryException("time is out of surface range.");

			// else move point out of region to well defined region
			var newT = Math.Max(t, MinX());
			var newK = Math.Max(Math.Min(k, MaxY()), MinY());

			return GetLocalVolFromImpliedVol(newT, newK);
		}

        //just to satisfy interface requirement
        public double GetValue(Date x, double y, double spot)
        {
            throw new NotImplementedException();
        }

        #region Private Functions

        // below three functions assume time t and strike k is in good region
        private double GetFirstDerivativeToTime(double t, double k)
		{
			double forwardTime = TimeBump.BumpForward(t);
			double backwardTime = TimeBump.BumpBackward(t);

			return (ImpliedVol.GetValue(forwardTime, k) - ImpliedVol.GetValue(backwardTime, k))/(forwardTime - backwardTime);
		}

		private double GetFirstDerivativeToStrike(double t, double k)
		{
			double forwardPrice = PriceBump.BumpForward(k);
			double backwardPrice = PriceBump.BumpBackward(k);

			return (ImpliedVol.GetValue(t, forwardPrice) - ImpliedVol.GetValue(t, backwardPrice))/(forwardPrice - backwardPrice);
		}

		private double GetSecondDerivativeToStrike(double t, double k)
		{
			// there is possibility that the bump sizes are different between forward and backward. 
			// we use the minimum of them to calculate the second order derivative.
			double bumpsize = PriceBump.MinBump(k); 

			return (ImpliedVol.GetValue(t, k + bumpsize) + ImpliedVol.GetValue(t, k - bumpsize) - 2*ImpliedVol.GetValue(t, k))/
			       (bumpsize*bumpsize);
		}

		private double GetLocalVolFromImpliedVol(double t, double k)
		{
			var forwardPrice = ImpliedVol.GetForwardPrice(t);
			var sigmaImpl = ImpliedVol.GetValue(t, k);
			var firstOrderToT = GetFirstDerivativeToTime(t, k);
			var firstOrderToK = GetFirstDerivativeToStrike(t, k);
			var secondOrderToK = GetSecondDerivativeToStrike(t, k);
			var d1 = (Math.Log(forwardPrice/k) + 0.5*sigmaImpl*sigmaImpl*t)/(sigmaImpl*Math.Sqrt(t));
			var sigmaLSquare = (Math.Pow(sigmaImpl, 2) + 2*sigmaImpl*t*firstOrderToT)/
			                   (Math.Pow((1 + d1*Math.Sqrt(t)*k*firstOrderToK), 2) +
			                    sigmaImpl*t*k*k*(secondOrderToK - d1*Math.Sqrt(t)*Math.Pow(firstOrderToK, 2)));

			return sigmaLSquare >= 0
				? Math.Sqrt(sigmaLSquare)
				: 0.0;
		}

		#endregion


	}
}
