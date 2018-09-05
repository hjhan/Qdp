using Qdp.Pricing.Library.Base.Utilities;

namespace Qdp.Pricing.Library.Common.MathMethods.FiniteDifference
{
	public class TridiagonalMatrix
	{
		public int Size { get; private set; }
		public double[] Diagonal { get; private set; }
		public double[] UpperDiagonal { get; private set; }
		public double[] LowerDiagonal { get; private set; }

		public TridiagonalMatrix(int size, double[] diagonal, double[] upperDiagonal, double[] lowerDiagonal)
		{
			Size = size;
			Diagonal = diagonal;
			UpperDiagonal = upperDiagonal;
			LowerDiagonal = lowerDiagonal;
		}

		public double[] SolveFor(double[] rhs)
		{
			if (rhs.Length != Size) throw new PricingLibraryException("Array size of r.h.s. must be equal to the size of the matrix");
			var temp = new double[Size];
			var result = new double[Size];
			var bet = Diagonal[0];

			if (bet == 0.0) throw new PricingLibraryException("Diagonal's first element cannot be 0.0");
			result[0] = rhs[0] / bet;

			for (int j = 1; j <= Size - 1; ++j)
			{
				temp[j] = UpperDiagonal[j - 1] / bet;
				bet = Diagonal[j] - LowerDiagonal[j - 1] * temp[j];
				if (bet == 0.0) throw new PricingLibraryException("Division by 0.0");
				result[j] = (rhs[j] - LowerDiagonal[j - 1] * result[j-1]) / bet;
			}
			// cannot be j>=0 with Size j
			for (int j = Size - 2; j > 0; --j)
			{
				result[j] -= temp[j + 1]*result[j + 1];
			}
			result[0] -= temp[1] * result[1];
			return result;
		}
	}
}
