using System;
using System.Runtime.Serialization;
using Qdp.Foundation.Interfaces;

namespace Qdp.Foundation.Implementations
{
	[DataContract]
    [Serializable]
	public class GuidObject : IGuidObject
	{
		[DataMember]
		public Guid Guid { get; protected set; }

		public GuidObject()
		{
			Guid = Guid.NewGuid();
		}

		protected bool Equals(GuidObject other)
		{
			return Guid.Equals(other.Guid);
		}

		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return Guid.GetHashCode();
		}
	}
}
