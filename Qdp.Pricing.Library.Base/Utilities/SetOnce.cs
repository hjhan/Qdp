using System;
using Qdp.Pricing.Library.Base.Interfaces;

namespace Qdp.Pricing.Library.Base.Utilities
{
	public class SetOnce<T> : ISetOnce
	{
		private readonly string _propertyName;
		private T _value;
		public bool HasValue { get; private set; }
		public void SetValue(object value)
		{
			Value = (T) value;
		}

		public object GetValue()
		{
			return Value;
		}

		public SetOnce(string propertyName)
		{
			_propertyName = propertyName;
		}

		public T Value
		{
			get
			{
				if (!HasValue)
				{
					throw new PricingLibraryException(string.Format("Value {0} has not been set", _propertyName));
				}
				return _value;
			}
			set
			{
				if (HasValue)
				{
					throw new PricingLibraryException(string.Format("Value {0} has already been set", _propertyName));
				}
				_value = value;
				HasValue = true;
			}
		}

		public static implicit operator T(SetOnce<T> value)
		{
			if (value == null)
			{
				throw new ArgumentException("Setonce<Value>");
			}
			return value.Value;
		}

		public override string ToString()
		{
			return HasValue ? Convert.ToString(_value) : "";
		}
	}
}
