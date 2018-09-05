using System;
using System.Collections.Generic;
using System.Threading;

namespace Qdp.Foundation.Utilities
{
	public enum ResultStatus
	{
		InProgress,
		Finished,
	}


	public class Result<T>
	{
		enum CallbackThreadType
		{
			UpdatedThread,
			ThreadPool,
			SynchronizationContext
		}

		public ResultStatus Status { get; private set; }

		private readonly object _lock = new object();
		private readonly ManualResetEventSlim _signal = new ManualResetEventSlim(false);
		private readonly SynchronizationContext _context;
		private readonly CallbackThreadType _callbackThreadType;
		public readonly int MaxProcessValue;

		private T _value;
		private int _currentProcessValue;

		private void DoCallBack(EventHandler<ResultEventArgs<T>> callback, T value, int currentProcessValue)
		{
			switch (_callbackThreadType)
			{
				case CallbackThreadType.UpdatedThread:
					callback(this, new ResultEventArgs<T>(value, currentProcessValue));
					break;
				case CallbackThreadType.SynchronizationContext:
					_context.Post(arg => callback(this, new ResultEventArgs<T>(value, currentProcessValue)), null);
					break;
				case CallbackThreadType.ThreadPool:
					ThreadPool.QueueUserWorkItem(arg => callback(this, new ResultEventArgs<T>(value, currentProcessValue)));
					break;
			}
		}

		private readonly List<EventHandler<ResultEventArgs<T>>> _finishedCallbacks = new List<EventHandler<ResultEventArgs<T>>>();
		public event EventHandler<ResultEventArgs<T>> Finished
		{
			add
			{
				lock (_lock)
				{
					_finishedCallbacks.Add(value);
					if (Status == ResultStatus.Finished)
					{
						DoCallBack(value, _value, MaxProcessValue);
					}
				}
			}
			remove
			{
				lock (_lock)
				{
					_finishedCallbacks.Remove(value);
				}
			}
		}

		private readonly List<EventHandler<ResultEventArgs<T>>> _updatedCallbacks = new List<EventHandler<ResultEventArgs<T>>>();
		public event EventHandler<ResultEventArgs<T>> Updated
		{
			add
			{
				lock (_lock)
				{
					_updatedCallbacks.Add(value);					
				}
			}
			remove
			{
				lock (_lock)
				{
					_finishedCallbacks.Remove(value);
				}
			}
		}


		public T Value
		{
			get
			{
				WaitTillFinished();
				return _value;
			}

			private set
			{
				_value = value;
				_signal.Set();
			}
		}

		public Result(int maxProcessValue = 1)
		{
			MaxProcessValue = maxProcessValue;
			_context = null;
			_callbackThreadType = CallbackThreadType.UpdatedThread;
			Status = ResultStatus.InProgress;
		}

		public Result(SynchronizationContext callbackContext, int maxProcessValue = 1)
		{
			MaxProcessValue = maxProcessValue;
			_context = callbackContext;
			_callbackThreadType = _context == null ? CallbackThreadType.ThreadPool : CallbackThreadType.SynchronizationContext;
			Status = ResultStatus.InProgress;
		}

		public void Update(T value, bool finished = false)
		{
			lock (_lock)
			{
				if (Status == ResultStatus.InProgress)
				{
					if (finished)
					{
						_currentProcessValue = MaxProcessValue;
						Status = ResultStatus.Finished;
						_finishedCallbacks.ForEach(callback => DoCallBack(callback, value, _currentProcessValue));
						Value = value;
					}

					else
					{
						if (_currentProcessValue < MaxProcessValue)
						{
							++_currentProcessValue;
						}
						_updatedCallbacks.ForEach(callback => DoCallBack(callback, value, _currentProcessValue));
					}
				}
				else
				{
					throw new Exception("Result value has been finished already.");
				}
			}
		}

		public bool WaitTillFinished(int timeOutInMs)
		{
			return _signal.Wait(timeOutInMs);
		}

		public void WaitTillFinished()
		{
			_signal.Wait();
		}

		public static void WaitAllTillFinished(Result<T>[] results)
		{
			Array.ForEach(results, result => result.WaitTillFinished());
		}
	}
}
