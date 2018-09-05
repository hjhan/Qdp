using System.Collections.Generic;
using System.Linq;

namespace Qdp.Foundation.TableWithHeader
{
	public class TableWithHeaderReader<T>
	{
		public Dictionary<string, int> TableHeaderDict { get; private set; }
		public int ColumnCount { get; private set; }
		public T[,] Table { get; private set; }
		public TableRow<T>[] Rows { get; private set; }
		public TableWithHeaderReader(T[,] table)
		{
			Table = table;
			ColumnCount = Table.GetLength(1);
			TableHeaderDict = Enumerable.Range(0, ColumnCount)
				.Select(i => Table[0, i] as string)
				.Select((x, i) => new {x, i})
				.ToDictionary(t => t.x, t => t.i);
			Rows = Enumerable.Range(1, Table.GetLength(0)-1)
				.Select(x => new TableRow<T>(this, x))
				.ToArray();
		}
	}
}
