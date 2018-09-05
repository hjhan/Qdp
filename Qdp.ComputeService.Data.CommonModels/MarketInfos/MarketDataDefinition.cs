using System;
using System.Runtime.Serialization;
using Qdp.Foundation.Implementations;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos
{
	[DataContract]
	[Serializable]
	public abstract class MarketDataDefinition : GuidObject
	{
		protected MarketDataDefinition(string name)
		{
			Name = name;
		}

		protected MarketDataDefinition()
		{
		}

		[DataMember]
		public string Name { get; protected set; }

		public virtual MarketDataDefinition[] GetDependencies()
		{
			return new MarketDataDefinition[0];
		}

		public virtual void MergeDependencies(MarketDataDefinition mergeData)
		{
			
		}

		public virtual void RemoveDependencies(MarketDataDefinition mergeData)
		{

		}
	}
}