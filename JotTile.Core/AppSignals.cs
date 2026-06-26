using System;
using System.Threading;

namespace JotTile.Core
{
    internal static class AppSignals
    {
        internal static void Raise(string eventName)
        {
            TryRaise(eventName);
        }

        internal static bool TryRaise(string eventName)
        {
            EventWaitHandle? waitHandle = null;

            try
            {
                waitHandle = EventWaitHandle.OpenExisting(eventName);
                waitHandle.Set();
                return true;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return false;
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
