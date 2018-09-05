using System;
using System.Runtime.Serialization;

namespace Qdp.ComputeService.Data.CommonModels.MarketInfos.Utilities
{
	[DataContract]
	[Serializable]
	public class MktCacheConfig
	{
		public MktCacheConfig()
		{
			TimeOutInMinutes = 30;
			DoNotOverWrite = false;
		}

		public MktCacheConfig(int timeOutInMinutes, bool doNotoverwrite)
		{
			TimeOutInMinutes = timeOutInMinutes;
			DoNotOverWrite = doNotoverwrite;
		}

		[DataMember]
		public int TimeOutInMinutes { get; set; }

		[DataMember]
		public bool DoNotOverWrite { get; set; }

		public DateTime ExpirationTime
		{
			get { return DateTime.Now.Add(new TimeSpan(TimeOutInMinutes)); }
		}
	}
}