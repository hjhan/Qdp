namespace Qdp.Pricing.Library.Base.Curves.Interfaces
{
	public interface ICurve2D<TRow, TCol>
	{
		TRow[] RowGrid { get; }
		TCol[] ColGrid { get; }
		double[,] ValueOnGrids { get; }
		double GetValue(TRow x, TCol y);
        double GetValue(TRow x, TCol y, TCol spot);
        double GetValue(double x, double y);
	}
}
