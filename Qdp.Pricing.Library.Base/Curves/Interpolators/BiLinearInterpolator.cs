using System;
using System.Linq;
using Qdp.Pricing.Library.Base.Curves.Interfaces;
using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Library.Base.Curves.Interpolators
{
	public class BiLinearInterpolator : IInterpolator2D
	{
		public BiLinearInterpolator(double[] rowGrid, double[] colGrid, double[,] valueOnGrids)
		{
			_rowGrid = rowGrid;
			_colGrid = colGrid;
			_valueOnGrids = valueOnGrids;
			_minRow = _rowGrid.Min();
			_maxRow = _rowGrid.Max();
			_minCol = _colGrid.Min();
			_maxCol = _colGrid.Max();

		}

		public double GetValue(double x, double y)
		{
			if (x > _minRow && x < _maxRow && y > _minCol && y < _maxCol)
			{
				var x1Index = _rowGrid.GetRangeLeftIndexOf(x);
				var x2Index = _colGrid.GetRangeLeftIndexOf(y);
				
				var x1 = _rowGrid[x1Index];
				var x2 = _rowGrid[x1Index + 1];
				var y1 = _colGrid[x2Index];
				var y2 = _colGrid[x2Index + 1];

				var f11 = (double) _valueOnGrids.GetValue(x1Index, x2Index);
				var f21 = (double) _valueOnGrids.GetValue(x1Index + 1, x2Index);
				var f12 = (double) _valueOnGrids.GetValue(x1Index, x2Index+1);
				var f22 = (double) _valueOnGrids.GetValue(x1Index+1, x2Index+1);

				return (f11*(x2 - x)*(y2 - y) + f21*(x - x1)*(y2 - y) + f12*(x2 - x)*(y - y1) + f22*(x - x1)*(y - y1))/(x2 - x1)/
				       (y2 - y1);
			}
			else
			{
				if (x < _minRow && y < _minCol)
				{
					return (double) _valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_minRow), _colGrid.ToList().IndexOf(_minCol));
				}
				if (x < _minRow && y > _maxCol)
				{
					return (double) _valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_minRow), _colGrid.ToList().IndexOf(_maxCol));
				}
				if (x > _maxRow && y < _minCol)
				{
					return (double) _valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_maxRow), _colGrid.ToList().IndexOf(_minCol));
				}
				if (x > _maxRow && y > _maxCol)
				{
					return (double) _valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_maxRow), _colGrid.ToList().IndexOf(_maxCol));
				}
				if (x <= _minRow)
				{
					var boarder = _valueOnGrids.GetRow(_rowGrid.GetRangeLeftIndexOf(_minRow));
					return new LinearInterpolator(_colGrid.Select((v, i) => Tuple.Create(v, boarder[i])).ToArray()).GetValue(y);
				}
				if (x >= _maxRow)
				{
					var boarder = _valueOnGrids.GetRow(_rowGrid.GetRangeLeftIndexOf(_maxRow));
					return new LinearInterpolator(_colGrid.Select((v, i) => Tuple.Create(v, boarder[i])).ToArray()).GetValue(y);
				}
				if (y <= _minCol)
				{
					var boarder = _valueOnGrids.GetCol(_colGrid.GetRangeLeftIndexOf(_minCol));
					return new LinearInterpolator(_rowGrid.Select((v, i) => Tuple.Create(v, boarder[i])).ToArray()).GetValue(x);
				}
				if (y >= _maxCol)
				{
					var boarder = _valueOnGrids.GetCol(_colGrid.GetRangeLeftIndexOf(_maxCol));
					return new LinearInterpolator(_rowGrid.Select((v, i) => Tuple.Create(v, boarder[i])).ToArray()).GetValue(x);
				}
			}

			throw new PricingLibraryException("Bilinear interpolation failed");
		}

		private readonly double[] _rowGrid;
		private readonly double[] _colGrid;
		private readonly double[,] _valueOnGrids;
		private readonly double _minRow;
		private readonly double _maxRow;
		private readonly double _minCol;
		private readonly double _maxCol;
	}

    public class BiCubicSplineInterpolator : IInterpolator2D
    {
        public BiCubicSplineInterpolator(double[] rowGrid, double[] colGrid, double[,] valueOnGrids)
        {
            _rowGrid = rowGrid;
            _colGrid = colGrid;
            _valueOnGrids = valueOnGrids;
            _minRow = _rowGrid.Min();
            _maxRow = _rowGrid.Max();
            _minCol = _colGrid.Min();
            _maxCol = _colGrid.Max();

        }

        public double GetValue(double t, double k)
        {
            if (k > _minCol && k < _maxCol)
            {
                var kIndex = _colGrid.GetRangeLeftIndexOf(k);

                var k1 = _colGrid[kIndex];
                var k2 = _colGrid[kIndex + 1];

                var boarder1 = _valueOnGrids.GetCol(kIndex);
                var boarder2 = _valueOnGrids.GetCol(kIndex + 1);

                var vol1 = new CubicHermiteFdInterpolator(_rowGrid.Select((v, i) => Tuple.Create(v, boarder1[i])).ToArray()).GetValue(t);
                var vol2 = new CubicHermiteFdInterpolator(_rowGrid.Select((v, i) => Tuple.Create(v, boarder2[i])).ToArray()).GetValue(t);

                var vol = vol1 + (k - k1) * (vol2 - vol1) / (k2 - k1);
                return vol;
            }
            else
            {
                if (t < _minRow && k < _minCol)
                {
                    return (double)_valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_minRow), _colGrid.ToList().IndexOf(_minCol));
                }
                if (t < _minRow && k > _maxCol)
                {
                    return (double)_valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_minRow), _colGrid.ToList().IndexOf(_maxCol));
                }
                if (t > _maxRow && k < _minCol)
                {
                    return (double)_valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_maxRow), _colGrid.ToList().IndexOf(_minCol));
                }
                if (t > _maxRow && k > _maxCol)
                {
                    return (double)_valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_maxRow), _colGrid.ToList().IndexOf(_maxCol));
                }
                if (k <= _minCol)
                {
                    var boarder = _valueOnGrids.GetCol(_colGrid.GetRangeLeftIndexOf(_minCol));
                    return new CubicHermiteFdInterpolator(_rowGrid.Select((v, i) => Tuple.Create(v, boarder[i])).ToArray()).GetValue(t);
                }
                if (k >= _maxCol)
                {
                    var boarder = _valueOnGrids.GetCol(_colGrid.GetRangeLeftIndexOf(_maxCol));
                    return new CubicHermiteFdInterpolator(_rowGrid.Select((v, i) => Tuple.Create(v, boarder[i])).ToArray()).GetValue(t);
                }
            }

            throw new PricingLibraryException("Bicubicspline interpolation failed");
        }

        private readonly double[] _rowGrid;
        private readonly double[] _colGrid;
        private readonly double[,] _valueOnGrids;
        private readonly double _minRow;
        private readonly double _maxRow;
        private readonly double _minCol;
        private readonly double _maxCol;
    }
    public class VarianceBiLinearInterpolator : IInterpolator2D
    {
        public VarianceBiLinearInterpolator(double[] rowGrid, double[] colGrid, double[,] valueOnGrids)
        {
            _rowGrid = rowGrid;
            _colGrid = colGrid;
            _valueOnGrids = valueOnGrids;
            _minRow = _rowGrid.Min();
            _maxRow = _rowGrid.Max();
            _minCol = _colGrid.Min();
            _maxCol = _colGrid.Max();

        }

        public double GetValue(double x, double y)
        {
            if (x > _minRow && x < _maxRow && y > _minCol && y < _maxCol)
            {
                var x1Index = _rowGrid.GetRangeLeftIndexOf(x);
                var x2Index = _colGrid.GetRangeLeftIndexOf(y);

                var x1 = _rowGrid[x1Index];
                var x2 = _rowGrid[x1Index + 1];
                var y1 = _colGrid[x2Index];
                var y2 = _colGrid[x2Index + 1];

                var f11 = Math.Pow((double)_valueOnGrids.GetValue(x1Index, x2Index), 2);
                var f21 = Math.Pow((double)_valueOnGrids.GetValue(x1Index + 1, x2Index), 2);
                var f12 = Math.Pow((double)_valueOnGrids.GetValue(x1Index, x2Index + 1), 2);
                var f22 = Math.Pow((double)_valueOnGrids.GetValue(x1Index + 1, x2Index + 1), 2);

                return Math.Sqrt((f11 * (x2 - x) * (y2 - y) + f21 * (x - x1) * (y2 - y) + f12 * (x2 - x) * (y - y1) + f22 * (x - x1) * (y - y1)) / (x2 - x1) /
                       (y2 - y1));
            }
            else
            {
                if (x < _minRow && y < _minCol)
                {
                    return (double)_valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_minRow), _colGrid.ToList().IndexOf(_minCol));
                }
                if (x < _minRow && y > _maxCol)
                {
                    return (double)_valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_minRow), _colGrid.ToList().IndexOf(_maxCol));
                }
                if (x > _maxRow && y < _minCol)
                {
                    return (double)_valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_maxRow), _colGrid.ToList().IndexOf(_minCol));
                }
                if (x > _maxRow && y > _maxCol)
                {
                    return (double)_valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_maxRow), _colGrid.ToList().IndexOf(_maxCol));
                }
                if (x <= _minRow)
                {
                    var boarder = _valueOnGrids.GetRow(_rowGrid.GetRangeLeftIndexOf(_minRow));
                    return Math.Sqrt(new LinearInterpolator(_colGrid.Select((v, i) => Tuple.Create(v, Math.Pow(boarder[i], 2))).ToArray()).GetValue(y));
                }
                if (x >= _maxRow)
                {
                    var boarder = _valueOnGrids.GetRow(_rowGrid.GetRangeLeftIndexOf(_maxRow));
                    return Math.Sqrt(new LinearInterpolator(_colGrid.Select((v, i) => Tuple.Create(v, Math.Pow(boarder[i], 2))).ToArray()).GetValue(y));
                }
                if (y <= _minCol)
                {
                    var boarder = _valueOnGrids.GetCol(_colGrid.GetRangeLeftIndexOf(_minCol));
                    return Math.Sqrt(new LinearInterpolator(_rowGrid.Select((v, i) => Tuple.Create(v, Math.Pow(boarder[i], 2))).ToArray()).GetValue(x));
                }
                if (y >= _maxCol)
                {
                    var boarder = _valueOnGrids.GetCol(_colGrid.GetRangeLeftIndexOf(_maxCol));
                    return Math.Sqrt(new LinearInterpolator(_rowGrid.Select((v, i) => Tuple.Create(v, Math.Pow(boarder[i], 2))).ToArray()).GetValue(x));
                }
            }

            throw new PricingLibraryException("VarianceBilinear interpolation failed");
        }

        private readonly double[] _rowGrid;
        private readonly double[] _colGrid;
        private readonly double[,] _valueOnGrids;
        private readonly double _minRow;
        private readonly double _maxRow;
        private readonly double _minCol;
        private readonly double _maxCol;
    }

    public class VarianceBiCubicSplineInterpolator : IInterpolator2D
    {
        public VarianceBiCubicSplineInterpolator(double[] rowGrid, double[] colGrid, double[,] valueOnGrids)
        {
            _rowGrid = rowGrid;
            _colGrid = colGrid;
            _valueOnGrids = valueOnGrids;
            _minRow = _rowGrid.Min();
            _maxRow = _rowGrid.Max();
            _minCol = _colGrid.Min();
            _maxCol = _colGrid.Max();

        }

        public double GetValue(double t, double k)
        {
            if (k > _minCol && k < _maxCol)
            {
                var kIndex = _colGrid.GetRangeLeftIndexOf(k);

                var k1 = _colGrid[kIndex];
                var k2 = _colGrid[kIndex + 1];

                var boarder1 = _valueOnGrids.GetCol(kIndex);
                var boarder2 = _valueOnGrids.GetCol(kIndex + 1);

                var var1 = new CubicHermiteFdInterpolator(_rowGrid.Select((v, i) => Tuple.Create(v, Math.Pow(boarder1[i], 2)*v)).ToArray()).GetValue(t);
                var var2 = new CubicHermiteFdInterpolator(_rowGrid.Select((v, i) => Tuple.Create(v, Math.Pow(boarder2[i], 2)*v)).ToArray()).GetValue(t);

                var var = var1 + (k - k1) * (var2 - var1) / (k2 - k1);
                return Math.Sqrt(var/t);
            }
            else
            {
                if (t < _minRow && k < _minCol)
                {
                    return (double)_valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_minRow), _colGrid.ToList().IndexOf(_minCol));
                }
                if (t < _minRow && k > _maxCol)
                {
                    return (double)_valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_minRow), _colGrid.ToList().IndexOf(_maxCol));
                }
                if (t > _maxRow && k < _minCol)
                {
                    return (double)_valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_maxRow), _colGrid.ToList().IndexOf(_minCol));
                }
                if (t > _maxRow && k > _maxCol)
                {
                    return (double)_valueOnGrids.GetValue(_rowGrid.ToList().IndexOf(_maxRow), _colGrid.ToList().IndexOf(_maxCol));
                }
                if (k <= _minCol)
                {
                    var boarder = _valueOnGrids.GetCol(_colGrid.GetRangeLeftIndexOf(_minCol));
                    var variance = new CubicHermiteFdInterpolator(_rowGrid.Select((v, i) => Tuple.Create(v, Math.Pow(boarder[i], 2) * v)).ToArray()).GetValue(t);
                    return Math.Sqrt(variance/t);
                }
                if (k >= _maxCol)
                {
                    var boarder = _valueOnGrids.GetCol(_colGrid.GetRangeLeftIndexOf(_maxCol));
                    var variance = new CubicHermiteFdInterpolator(_rowGrid.Select((v, i) => Tuple.Create(v, Math.Pow(boarder[i], 2) * v)).ToArray()).GetValue(t);
                    return Math.Sqrt(variance / t);
                }
            }

            throw new PricingLibraryException("VarianceBiCubicSpline interpolation failed");
        }

        private readonly double[] _rowGrid;
        private readonly double[] _colGrid;
        private readonly double[,] _valueOnGrids;
        private readonly double _minRow;
        private readonly double _maxRow;
        private readonly double _minCol;
        private readonly double _maxCol;
    }
}
