using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Library.Base.Curves;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;
using System.Linq;
using Qdp.Pricing.Base.Interfaces;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interpolators;

namespace Qdp.Pricing.Library.Common.MathMethods.VolTermStructure
{
	public abstract class InterpolatedImpliedVolSurface : ImpliedVolSurface
	{
		protected InterpolatedImpliedVolSurface(Date valuationDate, Date[] dates, double[] strikes, double[,] values, Interpolation2D interp)
			: base(valuationDate, dates, strikes, values, interp)
		{
		}

		public abstract double MaxX();

		public abstract double MinX();

		public abstract double MaxY();

		public abstract double MinY();

		public abstract double GetForwardPrice(double date);
	}

    [DataContract]
    [Serializable]
    public class ImpliedVolSurface : Curve2D<Date, double>, IVolSurface
	{
        private Interpolation2D _interp;
        private VolSurfaceType _surfType;
        private readonly IInterpolator2D _interpolator;
        private string _dcConvention;
        private IDayCount _dc;
        private Date _valuationDate;

        public string getVolSurfaceType() {
            return _surfType.ToString();
        }

        public string getInterpolationMethod() {
            return _interp.ToString();
        }

        public ImpliedVolSurface(Date valuationDate, Date[] dates, double[] strikes, double[,] values, Interpolation2D interp, String dcConvention = "Bus244" , VolSurfaceType volSurfaceType = VolSurfaceType.StrikeVol)
			: base(dates, strikes, values, x => x.ToOADate(), x => x, interp)
		{
            _valuationDate = valuationDate;
            _interp = interp;
            _surfType = volSurfaceType;
            _dcConvention = dcConvention;
            _dc = dcConvention.ToDayCountImpl();

            if ((dates as ICollection<Date>).Count != values.GetLength(0) || (strikes as ICollection<double>).Count != values.GetLength(1) )
			{
				throw new PricingLibraryException(string.Format("Size error for the implied volatility surface defintion"));
			}
            
            double[] maturityFractions = dates.Select((v, i) => _dc.CalcDayCountFraction(valuationDate, v)).ToArray();
            _interpolator = _interp.GetInterpolator(maturityFractions, strikes.Select((v,i) =>v).ToArray(), ValueOnGrids);
        }

        override public double GetValue(Date expiry, double strike, double spot)
        {
            switch (_surfType) {
                case VolSurfaceType.StrikeVol:
                    return GetValue(expiry, strike);
                case VolSurfaceType.MoneynessVol:
                    return GetValue(expiry, strike/spot);
                default :
                    throw new NotSupportedException($"Do not support vol type: {_surfType}");
            }
        }

        override public double GetValue(Date x, double y)
        {
            double timeFraction = _dc.CalcDayCountFraction(_valuationDate, x);
            return _interpolator.GetValue(timeFraction, y);
        }


        public virtual IVolSurface BumpVolSurf(double volChange)
		{
			var newMatrix = ValueOnGrids.Clone() as double[,];
			for (var i = 0; i < RowGrid.Length; ++i)
			{
				for (var j = 0; j < ColGrid.Length; ++j)
				{
					newMatrix[i, j] += volChange;
				}
			}
			return new ImpliedVolSurface(_valuationDate, RowGrid, ColGrid, newMatrix, _interp, dcConvention: _dcConvention, volSurfaceType: _surfType);
		}

		public virtual IVolSurface BumpMaturitySlice(int index, double volChange)
		{
			var newMatrix = ValueOnGrids.Clone() as double[,];
			for (var i = 0; i < ColGrid.Length; i++)
				//newMatrix[i, index] += volChange;		// note axis1 and 2 are corresponding to matrix dimension 2 and 1 respectively.
				newMatrix[index, i] += volChange;		
			return new ImpliedVolSurface(_valuationDate, RowGrid, ColGrid, newMatrix, _interp, dcConvention: _dcConvention, volSurfaceType: _surfType);
		}

		public virtual IVolSurface BumpMaturityStrikePoint(int indexMaturity, int indexStrike, double volChange)
		{
			var newMatrix = ValueOnGrids.Clone() as double[,];
			//newMatrix[indexStrike, indexMaturity] += volChange; // note axis1 and 2 are corresponding to matrix dimension 2 and 1 respectively.
			newMatrix[indexMaturity, indexStrike] += volChange; 

			return new ImpliedVolSurface(_valuationDate, RowGrid, ColGrid, newMatrix, _interp, dcConvention: _dcConvention, volSurfaceType: _surfType);
		}
	}
}