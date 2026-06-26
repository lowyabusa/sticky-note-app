using Xunit;

namespace JotTile.Tests
{
    public sealed class StickyNoteFormWindowStyleTests
    {
        [Fact]
        public void ShadowNoteStyleAddsToolWindowAndRemovesAppWindow()
        {
            int originalStyle = NativeMethods.WsExAppWindow | 0x20;
            int styled = StickyNoteForm.BuildShadowNoteExtendedStyle(originalStyle);

            Assert.NotEqual(0, styled & NativeMethods.WsExToolWindow);
            Assert.Equal(0, styled & NativeMethods.WsExAppWindow);
            Assert.NotEqual(0, styled & 0x20);
        }
    }
}
