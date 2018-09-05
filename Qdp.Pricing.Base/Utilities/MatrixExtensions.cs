using System;
using System.Collections.Generic;
using System.Linq;
using Qdp.Foundation.TableWithHeader;

namespace Qdp.Pricing.Base.Utilities
{
	public static class MatrixUtilities
	{
		// transpose a 2D arrary
		// http://www.codeproject.com/Articles/793684/Transposing-the-rows-and-columns-of-array-CSharp
		public static T[,] TransposeRowsAndColumns<T>(this T[,] arr)
		{
			var rowCount = arr.GetLength(0);
			var columnCount = arr.GetLength(1);
			var transposed = new T[columnCount, rowCount];
			if (rowCount == columnCount)
			{
				transposed = (T[,]) arr.Clone();
				for (var i = 1; i < rowCount; i++)
				{
					for (var j = 0; j < i; j++)
					{
						var temp = transposed[i, j];
						transposed[i, j] = transposed[j, i];
						transposed[j, i] = temp;
					}
				}
			}
			else
			{
				for (var column = 0; column < columnCount; column++)
				{
					for (var row = 0; row < rowCount; row++)
					{
						transposed[column, row] = arr[row, column];
					}
				}
			}
			return transposed;
		}

		public static T[] ToArrayObj<T>(this object[,] array) where T : class, new()
		{
			var rows = array.GetLength(0);
			var cols = array.GetLength(1);
			if (rows < 2 || cols <= 0)
			{
				throw new PricingBaseException("TableWithHeader must have at least 2 rows and 1 cols");
			}
			for (var j = 0; j < cols; ++j)
			{
				var name = array[0, j] as string;
				if (name == null)
				{
					throw new PricingBaseException("Header field must be string");
				}
			}
			var reader = new TableWithHeaderReader<object>(array);
			return reader.Rows.Select(x => x.As<T>()).ToArray();
		}

		public static Dictionary<T, K> ToDicObj<T, K>(this string dicStr)
		{

			if (!string.IsNullOrWhiteSpace(dicStr))
			{
				var dic = new Dictionary<T, K>();
				var array = dicStr.Split(';');
				foreach (var item in array)
				{
					var splits = item.Split(':');
					dic[splits[0].ToType<T>()] = splits[1].ToType<K>();
				}
                return dic;
            }
			return null;
		}
	}
}
