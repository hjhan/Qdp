using System;
using System.Runtime.Serialization;
using Qdp.Foundation.Implementations;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos
{
    [DataContract]
    [Serializable]
    public class CorrSurfMktData : VolSurfMktData
    {
        public CorrSurfMktData(string id, Date[] maturities, double[] strikes, double[,] corrSurface, string interpolation)
            : base(id, maturities, strikes, corrSurface, interpolation)
        {
            CorrSurface = corrSurface;
        }

        public CorrSurfMktData(string id, double constCorr)
            : base(id, 0)
        {
            ConstCorr = constCorr;
            CorrSurface = null;

        }

        [DataMember]
        public double[,] CorrSurface { get; set; }

        [DataMember]
        public double ConstCorr { get; set; }

    }
}