using System;
using System.Threading;

namespace JotTile.Core
{
    internal static class AppSignals
    {
        internal static void Raise(string eventName)
        {
            EventWaitHandle? waitHandle = null;

            try
            {
                waitHandle = EventWaitHandle.OpenExisting(eventName);
                waitHandle.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
            }
            finally
            {
                if (waitHandle != null)
                {
                    waitHandle.Dispose();
                }
            }
        }
    }
}
