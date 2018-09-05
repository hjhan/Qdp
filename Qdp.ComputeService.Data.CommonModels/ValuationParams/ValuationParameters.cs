using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.ValuationParams
{
	[DataContract]
	[Serializable]
	public abstract class ValuationParameters
	{
		public abstract string ToLabelData();
	}
}