using System;
using System.Runtime.Serialization;
using Qdp.Foundation.Implementations;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos
{
	[DataContract]
	[Serializable]
	public class VolSurfMktData : MarketDataDefinition
	{
		public VolSurfMktData(string id, Date[] maturities, double[] strikes, double[,] volsurfaces, string interpolation, string volSurfaceType= "StrikeVol")
			: base(id)
		{
			Maturities = maturities;
			Strikes = strikes;
			VolSurfaces = volsurfaces;
			ConstVol = double.NaN;
            Interpolation = interpolation;
            VolSurfaceType = volSurfaceType;

        }

		public VolSurfMktData(string id, double constVol)
			: base(id)
		{
			ConstVol = constVol;
			Maturities = null;
			Strikes = null;
			VolSurfaces = null;
            Interpolation = "";
            VolSurfaceType = "StrikeVol";

        }

		private VolSurfMktData()
		{
		}

		[DataMember]
		public Date[] Maturities { get; set; }

		[DataMember]
		public double[] Strikes { get; set; }

		[DataMember]
		public double[,] VolSurfaces { get; set; }

		[DataMember]
		public double ConstVol { get; set; }

        [DataMember]
        public string Interpolation { get; set; }

        [DataMember]
        public string VolSurfaceType { get; set; }
    }
}