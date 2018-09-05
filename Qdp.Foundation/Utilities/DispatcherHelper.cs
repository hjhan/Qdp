using System.Threading;
#if !NETCOREAPP2_1
using System.Windows.Threading;
#endif

namespace Qdp.Foundation.Utilities
{
	public static class DispatcherHelper
	{
#if !NETCOREAPP2_1
        public static Dispatcher CreateNewThreadDispatcher(string name = null, ThreadPriority priority = ThreadPriority.Normal, bool isBackGround = false)
		{
            //Dispatcher result = null;

            //using (var signal = new ManualResetEventSlim(false))
            //{
            //	var thread = new Thread(() =>
            //	{
            //		result = Dispatcher.CurrentDispatcher;
            //		signal.Set();
            //		Dispatcher.Run();
            //	})
            //	{
            //		Priority = priority,
            //		IsBackground = isBackGround
            //	};
            //	if (name != null)
            //	{
            //		thread.Name = name;
            //	}
            //	thread.Start();
            //	signal.Wait();
            //}

            //return result;

            return null;
		}
#endif
	}
}
