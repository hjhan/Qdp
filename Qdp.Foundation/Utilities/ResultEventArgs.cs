using System;

namespace Qdp.Foundation.Utilities
{
	public class ResultEventArgs<T> : EventArgs
	{
		public T ResultValue { get; private set; }
		public int CurrentProcessValue { get; private set; }

		public ResultEventArgs(T resultValue, int currentProcessValue)
		{
			ResultValue = resultValue;
			CurrentProcessValue = currentProcessValue;
		}
	}
}
