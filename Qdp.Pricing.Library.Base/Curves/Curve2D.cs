using System;
using System.Linq;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Base.Curves.Interpolators;
using System.Runtime.Serialization;

namespace Qdp.Pricing.Library.Base.Curves
{
    [DataContract]
    [Serializable]
    public class Curve2D<TRow, TCol> : ICurve2D<TRow, TCol>
	{
		public TRow[] RowGrid { get; private set; }
		public TCol[] ColGrid { get; private set; }
		public double[,] ValueOnGrids { get; private set; }

		private readonly IInterpolator2D _interpolator;
		private readonly Func<TRow, double> _x2DoubleFunc;
		private readonly Func<TCol, double> _y2DoubleFunc;

		public Curve2D(TRow[] x, TCol[] y, double[,] valueOnGrids, Func<TRow, double> x2DoubleFunc, Func<TCol, double> y2DoubleFunc, Interpolation2D interpolation2D)
		{
			RowGrid = x;
			ColGrid = y;
			ValueOnGrids = valueOnGrids;
			
			_x2DoubleFunc = x2DoubleFunc;
			_y2DoubleFunc = y2DoubleFunc;
			_interpolator = interpolation2D.GetInterpolator(RowGrid.Select(_x2DoubleFunc).ToArray(), ColGrid.Select(_y2DoubleFunc).ToArray(), ValueOnGrids);

		}
        virtual public double GetValue(TRow x, TCol y)
		{
			return GetValue(_x2DoubleFunc(x), _y2DoubleFunc(y));
		}

        virtual public double GetValue(TRow x, TCol y, TCol spot )
        {
            throw new NotImplementedException();
        }

        public double GetValue(double x, double y)
		{
			return _interpolator.GetValue(x, y);
		}

		private void Validate()
		{
			if (ColGrid.Length != ValueOnGrids.GetLength(1))
			{
				throw new PricingBaseException("ColGrid.Length != ValueOnGrids.NumColumns");
			}

			if (RowGrid.Length != ValueOnGrids.GetLength(0))
			{
				throw new PricingBaseException("RowGrid.Length != ValueOnGrids.NumRows");
			}
		}
	}
}
