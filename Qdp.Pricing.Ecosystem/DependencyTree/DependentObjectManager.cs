using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.ComputeService.Data.CommonModels.MarketInfos;
using Qdp.ComputeService.Data.CommonModels.MarketInfos.CurveDefinitions;
using Qdp.Foundation.Interfaces;
using Qdp.Pricing.Base.Utilities;

namespace Qdp.Pricing.Ecosystem.DependencyTree
{
	public class DependentObjectManager : IDisposable
	{
		public readonly DependentObject TopObject;
		private readonly Dictionary<string, DependentObject> _allObjects = new Dictionary<string, DependentObject>();

		public DependentObjectManager()
		{
			TopObject = new DependentObject { Manager = this, BuildFromDependents = (fatherNode, childrenNodes) => null };
		}

		public bool GetObject<T>(string key, out T value) where T : DependentObject, new()
		{
            if (!_allObjects.TryGetValue(key, out DependentObject val))
            {
                value = new T { Manager = this };
                _allObjects.Add(key, value);
                TopObject.DependsOn(key);
                return true;
            }
            else
            {
                value = val as T;
                if (value == null)
                {
                    throw new PricingBaseException("Type cannot be null when getting object");
                }
                return false;
            }
        }

		public bool GetDependByObject(Guid guid, out string[] value) 
		{
			List<string> val = (from objects in _allObjects from objectDependsOn in objects.Value.DependsOnItems where objectDependsOn.Value.Guid.Equals(guid) select objects.Key).ToList();

			value = new string[] { };
			if (val.Count > 0)
			{
				value = val.ToArray();
				return true;
			}
			return false;
		}

		public Dictionary<string, object> GetAllObject()
		{
			return _allObjects.Where(x => x.Value.Value is RateMktData || x.Value.Value is CurveConvention).ToDictionary(x=>x.Key, y=>(object)y.Value.Value);
		}

		public bool RemoveObject<T>(string key, out T value) where T : DependentObject, new()
		{
            value = new T { Manager = this };
            if (_allObjects.TryGetValue(key, out DependentObject val))
			{
				_allObjects.Remove(key);
			}
			return true;
		}

		public void Update(string key, IGuidObject value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
            if (GetObject(key, out DependentObject dobj) || !value.Equals(dobj.Value))
            {
                dobj.UpdateValue(value);
            }
        }

		public DependentObject[] GetDependsOnObjects(IEnumerable<string> keys)
		{
			return keys.Select(key =>
			{
                if (!GetObject(key, out DependentObject value))
                {
                    TopObject.DependsOnItems.Remove(value);
                }
                return value;
			}).ToArray();
		}

		public DependentObject[] GetDependsByObjects(IEnumerable<string> keys)
		{
			return keys.Select(key =>
			{
                if (!GetObject(key, out DependentObject value))
                {
                    TopObject.DependsByItems.Remove(value);
                }
                return value;
			}).ToArray();
		}

		public void ClearDependencyTree()
		{
			TopObject.Rebuild();
		}

		public void Dispose()
		{
			_allObjects.Clear();
		}
	}
}
