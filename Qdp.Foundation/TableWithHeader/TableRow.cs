using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Qdp.Foundation.TableWithHeader
{
	public class TableRow<T>
	{
		private readonly TableWithHeaderReader<T> _table;
		private readonly int _row;

		public TableRow(TableWithHeaderReader<T> tableWithHeaderReader, int row)
		{
			_table = tableWithHeaderReader;
			_row = row;
		}

		public TObj As<TObj>() where TObj : class, new()
		{
			try
			{
				var type = typeof (TObj);
				var obj = new TObj();
				var methodInfos = type.GetMethods();
				foreach (var item in _table.TableHeaderDict)
				{
					var property = type.GetProperty(item.Key);
					if (property != null && property.CanWrite)
					{
						var value = this[item.Value];
						SetObj(obj, property, value, methodInfos);
					}
				}
				return obj;
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static void SetObj(object obj, PropertyInfo propertyInfo, object value, IEnumerable<MethodInfo> methodInfos)
		{
			var converterName = string.Format("{0}Converter", propertyInfo.Name);
			var converter = methodInfos.FirstOrDefault(x => x.Name.Equals(converterName) && x.GetParameters().Length == 1);
			if (converter != null)
			{
				var result = converter.Invoke(obj, new[] {value});
				propertyInfo.SetValue(obj, result, null);
			}
			else
			{
				var strValue = value!=null ? value.ToString() : null;
				if (string.IsNullOrEmpty(strValue))
				{
					return;
				}
				try
				{
					if (propertyInfo.PropertyType.FullName.Contains("System.String"))
					{
						propertyInfo.SetValue(obj, strValue, null);
					}
					else if (propertyInfo.PropertyType.FullName.Contains("System.Int16"))
					{
						propertyInfo.SetValue(obj, Int16.Parse(strValue), null);
					}
					else if (propertyInfo.PropertyType.FullName.Contains("System.Int32"))
					{
						propertyInfo.SetValue(obj, Int32.Parse(strValue), null);
					}
					else if (propertyInfo.PropertyType.FullName.Contains("System.Int64"))
					{
						propertyInfo.SetValue(obj, Int64.Parse(strValue), null);
					}
					else if (propertyInfo.PropertyType.FullName.Contains("System.Double"))
					{
						propertyInfo.SetValue(obj, Double.Parse(strValue), null);
					}
					else if (propertyInfo.PropertyType.FullName.Contains("System.Single"))
					{
						propertyInfo.SetValue(obj, Single.Parse(strValue), null);
					}
					else if (propertyInfo.PropertyType.FullName.Contains("System.Boolean"))
					{
						propertyInfo.SetValue(obj, Boolean.Parse(strValue), null);
					}
					else if (propertyInfo.PropertyType.FullName.Contains("System.DateTime"))
					{
						propertyInfo.SetValue(obj, DateTime.Parse(strValue), null);
					}
					else
					{
						propertyInfo.SetValue(obj, null, null);
					}
				}
				catch (ArgumentOutOfRangeException)
				{
					throw;
				}
				catch (Exception)
				{
					; // for all these value type, the default value has been set already
				}
			}
		}


		public object this[int column]
		{
			get { return _table.Table[_row, column]; }
		}

		public object this[string header]
		{
			get
			{
				if (_table.TableHeaderDict == null || !_table.TableHeaderDict.ContainsKey(header))
				{
					throw new Exception(string.Format("Header {0} was not found", header));
				}
				return this[_table.TableHeaderDict[header]];
			}
		}
}
}
