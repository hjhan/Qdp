using System;

namespace Qdp.Foundation.TableWithHeader
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class ColumnAttribute : Attribute
	{
		public int Column { get; set; }

		public ColumnAttribute(int column)
		{
			Column = column;
		}
	}
}
