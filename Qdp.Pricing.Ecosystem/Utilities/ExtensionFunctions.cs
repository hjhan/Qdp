using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.Foundation.Implementations;
using Qdp.Pricing.Base.Implementations;
using Qdp.Pricing.Library.Common.MathMethods.VolTermStructure;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Base.Enums;

namespace Qdp.Pricing.Ecosystem.Utilities
{
	public static class ExtensionFunctions
	{
		public static ImpliedVolSurface ToImpliedVolSurface(this VolSurfMktData volSurfMktData, Date valuationDate, string dc = "Bus244" )
		{
            
            if (!double.IsNaN(volSurfMktData.ConstVol))
            {
                double constData  = 0.0;
                if (volSurfMktData is CorrSurfMktData)
                {
                    constData = (volSurfMktData as CorrSurfMktData).ConstCorr;
                }
                else if(volSurfMktData is VolSurfMktData)
                {
                    constData = (volSurfMktData as VolSurfMktData).ConstVol;
                }
                var maturities = new[] { new Term("1Y").Prev(valuationDate), new Term("30Y").Next(valuationDate) };
                var strikes = new[] { 0.0, 1.0e6 };
                var cols = new[,] { { constData, constData }, { constData, constData } };
                return new ImpliedVolSurface(valuationDate, maturities, strikes, cols, Interpolation2D.VarianceBiLinear, dcConvention: dc, volSurfaceType:  volSurfMktData.VolSurfaceType.ToVolSurfaceType());
            }
			else
			{
                if (volSurfMktData is CorrSurfMktData)
                {
                    var corr = (volSurfMktData as CorrSurfMktData).CorrSurface;
                    return new ImpliedVolSurface(valuationDate, volSurfMktData.Maturities, volSurfMktData.Strikes, corr, 
                        volSurfMktData.Interpolation.ToInterpolation2D(), dcConvention: dc, volSurfaceType: volSurfMktData.VolSurfaceType.ToVolSurfaceType());
                }
                else if (volSurfMktData is VolSurfMktData)
                {
                    return new ImpliedVolSurface(valuationDate, volSurfMktData.Maturities, volSurfMktData.Strikes, volSurfMktData.VolSurfaces, 
                        volSurfMktData.Interpolation.ToInterpolation2D(), dcConvention: dc, volSurfaceType: volSurfMktData.VolSurfaceType.ToVolSurfaceType());
                }
                else {
                    throw new PricingBaseException("unexpected vol type!  not supported yet");
                }
                    
            }
		}

        public static VolSurfMktData ToVolSurfMktData(this ImpliedVolSurface volSurface, string id)
        {
            return new VolSurfMktData(id, volSurface.RowGrid, volSurface.ColGrid, volSurface.ValueOnGrids, volSurface.getInterpolationMethod(), volSurface.getVolSurfaceType());
        }
	}
}
