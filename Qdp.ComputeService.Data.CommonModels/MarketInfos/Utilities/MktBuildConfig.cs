using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos.Utilities
{
	[DataContract]
	[Serializable]
	public class MktBuildConfig
	{
		[DataMember]
		public bool SwallowError { get; set; }
	}
}