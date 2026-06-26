using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using JotTile.Core;
using Xunit;

namespace JotTile.Tests
{
    public sealed class StickyNoteHardeningTests
    {
        [Fact]
        public void NormalizeLoadedNotePreservesWhitespaceOnlyText()
        {
            NoteRecord note = CreateNote(" \t\r\n ", true);

            NoteWindowPlacement.NormalizeLoadedNote(
                note,
                new Rectangle(0, 0, 800, 600),
                new[] { new Rectangle(0, 0, 800, 600) });

            Assert.Equal(" \t\r\n ", note.Text);
        }

        [Fact]
        public void NormalizeLoadedNoteBringsOffscreenNoteBackToVisibleArea()
        {
            NoteRecord note = CreateNote("saved", true);
            note.X = 3000;
            note.Y = 2000;

            NoteWindowPlacement.NormalizeLoadedNote(
                note,
                new Rectangle(0, 0, 800, 600),
                new[] { new Rectangle(0, 0, 800, 600) });

            Assert.InRange(note.X, 16, 800 - 16 - note.Width);
            Assert.InRange(note.Y, 16, 600 - 16 - note.Height);
        }

        [Fact]
        public void CreateNewNoteBoundsClampLargeOffsetsBackOntoScreen()
        {
            Rectangle bounds = NoteWindowPlacement.CreateNewNoteBounds(
                new Rectangle(0, 0, 800, 600),
                80,
                new Size(260, 160));

            Assert.InRange(bounds.X, 16, 800 - 16 - bounds.Width);
            Assert.InRange(bounds.Y, 16, 600 - 16 - bounds.Height);
        }

        [Fact]
        public void TryQueueUiCallbackLogsWhenDispatchFails()
        {
            using (TestWorkspace workspace = new TestWorkspace())
            {
                AppLogger logger = workspace.CreateLogger();
                using (Control control = new Control())
                {
                    control.Dispose();
                    bool queued = StickyNotesAppContext.TryQueueUiCallback(
                        control,
                        (System.Windows.Forms.MethodInvoker)delegate { },
                        logger,
                        "watch-test",
                        false);

                    Assert.False(queued);
                }

                string log = workspace.ReadLog();
                Assert.Contains("watch-test", log);
                Assert.Contains("Dispatching work to the UI thread failed.", log);
            }
        }

        [Fact]
        public void PersistBoundsDoesNotMutateNoteBeforeCallback()
        {
            RunInSta(delegate
            {
                NoteRecord note = CreateNote("saved", true);
                Rectangle callbackBounds = Rectangle.Empty;
                using (StickyNoteForm form = new StickyNoteForm(
                    note,
                    AppSettings.CreateDefault(),
                    new NoteLayoutCalculator(),
                    new AppLogger(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "JotTile.Tests", Guid.NewGuid().ToString("N"))),
                    delegate(NoteRecord _, NoteCommitRequest __) { return NoteCommitResult.Success(); },
                    delegate(NoteRecord _, Rectangle bounds)
                    {
                        callbackBounds = bounds;
                        return false;
                    },
                    delegate(StickyNoteForm _) { }))
                {
                    form.Bounds = new Rectangle(220, 180, 280, 200);
                    System.Reflection.MethodInfo? method = typeof(StickyNoteForm).GetMethod("PersistBounds", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    Assert.NotNull(method);

                    method!.Invoke(form, null);

                    Assert.Equal(10, note.X);
                    Assert.Equal(10, note.Y);
                    Assert.Equal(160, note.Width);
                    Assert.Equal(90, note.Height);
                    Assert.Equal(new Rectangle(10, 10, 160, 90), form.Bounds);
                    Assert.Equal(new Rectangle(220, 180, 280, 200), callbackBounds);
                }
            });
        }

        [Fact]
        public void SavedTextViewUsesReadOnlyRichTextBox()
        {
            RunInSta(delegate
            {
                using (StickyNoteForm form = new StickyNoteForm(
                    CreateNote(new string('a', 4096), true),
                    AppSettings.CreateDefault(),
                    new NoteLayoutCalculator(),
                    new AppLogger(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "JotTile.Tests", Guid.NewGuid().ToString("N"))),
                    delegate(NoteRecord _, NoteCommitRequest __) { return NoteCommitResult.Success(); },
                    delegate(NoteRecord _, Rectangle __) { return true; },
                    delegate(StickyNoteForm _) { }))
                {
                    Assert.True(form.SavedTextView.ReadOnly);
                    Assert.True(form.SavedTextView.Multiline);
                    Assert.Equal(RichTextBoxScrollBars.Vertical, form.SavedTextView.ScrollBars);
                }
            });
        }

        [Fact]
        public void SavedTextViewHidesScrollbarWhenContentFits()
        {
            RunInSta(delegate
            {
                using (StickyNoteForm form = new StickyNoteForm(
                    CreateNote("Short saved text", true),
                    AppSettings.CreateDefault(),
                    new NoteLayoutCalculator(),
                    new AppLogger(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "JotTile.Tests", Guid.NewGuid().ToString("N"))),
                    delegate(NoteRecord _, NoteCommitRequest __) { return NoteCommitResult.Success(); },
                    delegate(NoteRecord _, Rectangle __) { return true; },
                    delegate(StickyNoteForm _) { }))
                {
                    Assert.Equal(RichTextBoxScrollBars.None, form.SavedTextView.ScrollBars);
                }
            });
        }

        private static NoteRecord CreateNote(string text, bool isSaved)
        {
            return new NoteRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Text = text,
                X = 10,
                Y = 10,
                Width = 160,
                Height = 90,
                CreatedAt = "2026-01-01T00:00:00.0000000Z",
                UpdatedAt = "2026-01-01T00:00:00.0000000Z",
                IsSaved = isSaved
            };
        }

        private static void RunInSta(ThreadStart action)
        {
            Exception? failure = null;
            using (ManualResetEventSlim completed = new ManualResetEventSlim(false))
            {
                Thread thread = new Thread((ThreadStart)delegate
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        failure = ex;
                    }
                    finally
                    {
                        completed.Set();
                    }
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                completed.Wait();
            }

            if (failure != null)
            {
                throw failure;
            }
        }
    }
}
