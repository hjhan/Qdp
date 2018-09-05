namespace Qdp.Pricing.Library.Base.Utilities
{
	public static class MatrixExtension
	{
		public static double[,] ToMatrix(this double[][] input)
		{
			if (input == null)
			{
				return null;
			}

			var rows = input.GetLength(0);
			var cols = input.GetLength(1);
			for (var i = 0; i < rows; ++i)
			{
				if(input[i].GetLength(0) != cols)
				{
					throw new PricingLibraryException("Dimension mismatch when converting double[][] to double[,]");
				}
			}

			var ret = new double[rows, cols];
			for (var i = 0; i < rows; ++i)
			{
				for (var j = 0; j < cols; ++j)
				{
					ret[i, j] = input[i][j];
				}
			}

			return ret;
		}

		public static T[] GetRow<T>(this T[,] matrix, int row)
		{
			var cols = matrix.GetLength(1);
			var ret = new T[cols];
			for (var i = 0; i < cols; ++i)
			{
				ret[i] = matrix[row, i];
			}
			return ret;
		}

		public static T[] GetCol<T>(this T[,] matrix, int row)
		{
			var rows = matrix.GetLength(0);

			var ret = new T[rows];
			for (var i = 0; i < rows; ++i)
			{
				ret[i] = matrix[i, row];
			}

			return ret;
		}
	}
}
