using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Qdp.Foundation.Implementations;
using Qdp.Foundation.Interfaces;
using Qdp.Foundation.Utilities;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Ecosystem.DependencyTree
{
	public class DependentObject
	{
		private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private IGuidObject _value;
		private bool _tagForUpdate;

		public Type ValueType { get; private set; }
		public List<DependentObject> DependsOnItems { get; private set; }
		public List<DependentObject> DependsByItems { get; private set; }

		public DependentObjectManager Manager { get; internal set; }
		public Func<DependentObject, DependentObject[], GuidObject> BuildFromDependents { get; set; }

		public DependentObject()
		{
			DependsByItems = new List<DependentObject>();
			DependsOnItems = new List<DependentObject>();

			BuildFromDependents = (fatherNode, childrenNodes) =>
			{
				throw new PricingBaseException("Build method undefined!");
			};
		}

		public bool TagForUpdate
		{
			get
			{
				return _tagForUpdate;
			}
		}

		public IGuidObject Value
		{
			get { return _value; }
			private set
			{
				_value = value;
				if (_value != null)
				{
					ValueType = _value.GetType();
				}
			}
		}

		public void SetTagForUpdate()
		{
			_tagForUpdate = true;
			foreach (var dependentObject in DependsByItems)
			{
				dependentObject._tagForUpdate = true;
			}
		}

		public void ClearTagForUpdate()
		{
			_tagForUpdate = false;
		}

		public bool UpdateValue(IGuidObject value)
		{
			if (!value.Equals(Value))
			{
				Value = value;
				foreach (var dependentObject in DependsByItems)
				{
					dependentObject.SetTagForUpdate();
				}
				return true;
			}
			else
			{
				return false;
			}
		}


		public void DependsOn(params string[] keys)
		{
			var objects = Manager.GetDependsOnObjects(keys);
			DependsOnItems.AddRange(objects);
			Array.ForEach(objects, x => x.DependsByItems.Add(this));
		}

		public void RemoveDepends(params string[] keys)
		{
			var objects = Manager.GetDependsByObjects(keys);
			foreach (var objectSingle in objects)
			{
				objectSingle.DependsOnItems.Remove(this);
			}
		}

		public void Rebuild()
		{
			if (TagForUpdate)
			{
				try
				{
					foreach (var dependentObject in DependsOnItems)
					{
						dependentObject.Rebuild();
					}

					Value = BuildFromDependents(this, DependsOnItems.ToArray());
					ClearTagForUpdate();
				}
				catch (Exception ex)
				{
					Logger.Error(ex.GetDetail());
				}
			}
		}
	}
}
