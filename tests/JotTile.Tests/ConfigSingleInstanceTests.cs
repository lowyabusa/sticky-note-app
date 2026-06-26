using System.Threading;
using JotTile.Core;
using Xunit;

namespace JotTile.Tests
{
    public sealed class ConfigSingleInstanceTests
    {
        [Fact]
        public void TryRaiseSignalsExistingConfigActivationEvent()
        {
            string eventName = @"Local\JotTile.ConfigActivate.Tests." + System.Guid.NewGuid().ToString("N");
            using (EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, eventName))
            {
                Assert.True(AppSignals.TryRaise(eventName));
                Assert.True(waitHandle.WaitOne(250));
            }
        }

        [Fact]
        public void TryRaiseReturnsFalseWhenEventDoesNotExist()
        {
            string eventName = @"Local\JotTile.ConfigActivate.Tests." + System.Guid.NewGuid().ToString("N");
            Assert.False(AppSignals.TryRaise(eventName));
        }
    }
}
