using System.Drawing;
using JotTile.Core;
using Xunit;

namespace JotTile.Tests
{
    public sealed class NoteInteractionControllerTests
    {
        [Fact]
        public void NewDraftStartsInEditingMode()
        {
            NoteInteractionController controller = new NoteInteractionController(string.Empty, false);

            Assert.Equal(NoteInteractionMode.Editing, controller.Mode);
            Assert.False(controller.CanCopy);
        }

        [Fact]
        public void SaveSuccessCommitsTextAndTransitionsToSaved()
        {
            NoteInteractionController controller = new NoteInteractionController(string.Empty, false);
            controller.UpdateEditorText("Hello");

            NoteCommitResult result = controller.Save(
                delegate(NoteCommitRequest request)
                {
                    Assert.Equal("Hello", request.Text);
                    return NoteCommitResult.Success();
                },
                new Rectangle(10, 10, 200, 120));

            Assert.True(result.Succeeded);
            Assert.Equal(NoteInteractionMode.Saved, controller.Mode);
            Assert.Equal("Hello", controller.CommittedText);
            Assert.True(controller.CanCopy);
        }

        [Fact]
        public void SaveFailureKeepsEditingStateAndCommittedText()
        {
            NoteInteractionController controller = new NoteInteractionController("Saved", true);
            controller.BeginEdit();
            controller.UpdateEditorText("Unsaved");

            NoteCommitResult result = controller.Save(
                delegate
                {
                    return NoteCommitResult.Failure("nope");
                },
                new Rectangle(10, 10, 200, 120));

            Assert.False(result.Succeeded);
            Assert.Equal(NoteInteractionMode.Editing, controller.Mode);
            Assert.Equal("Saved", controller.CommittedText);
            Assert.Equal("Unsaved", controller.EditorText);
        }

        [Fact]
        public void BeginEditRestoresCommittedTextIntoEditor()
        {
            NoteInteractionController controller = new NoteInteractionController("Saved text", true);
            controller.BeginEdit();

            Assert.Equal(NoteInteractionMode.Editing, controller.Mode);
            Assert.Equal("Saved text", controller.EditorText);
        }
    }
}
