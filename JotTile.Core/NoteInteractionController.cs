using System;
using System.Drawing;

namespace JotTile.Core
{
    internal sealed class NoteInteractionController
    {
        private string _committedText;
        private string _editorText;

        internal NoteInteractionController(string committedText, bool isSaved)
        {
            _committedText = committedText ?? string.Empty;
            _editorText = _committedText;
            Mode = isSaved ? NoteInteractionMode.Saved : NoteInteractionMode.Editing;
        }

        internal NoteInteractionMode Mode { get; private set; }

        internal string CommittedText
        {
            get { return _committedText; }
        }

        internal string EditorText
        {
            get { return _editorText; }
        }

        internal bool IsDirty
        {
            get { return !string.Equals(_editorText, _committedText, StringComparison.Ordinal); }
        }

        internal bool CanCopy
        {
            get { return Mode == NoteInteractionMode.Saved; }
        }

        internal void BeginEdit()
        {
            if (Mode == NoteInteractionMode.Saved)
            {
                _editorText = _committedText;
                Mode = NoteInteractionMode.Editing;
            }
        }

        internal void UpdateEditorText(string editorText)
        {
            _editorText = editorText ?? string.Empty;
        }

        internal void DiscardUncommittedEdits()
        {
            _editorText = _committedText;
            if (Mode == NoteInteractionMode.Editing && _committedText.Length > 0)
            {
                Mode = NoteInteractionMode.Saved;
            }
        }

        internal NoteCommitResult Save(Func<NoteCommitRequest, NoteCommitResult> commit, Rectangle targetBounds)
        {
            if (Mode != NoteInteractionMode.Editing)
            {
                return NoteCommitResult.Failure("Save is only available while editing.");
            }

            NoteCommitResult result = commit(new NoteCommitRequest(_editorText, targetBounds));
            if (result.Succeeded)
            {
                _committedText = _editorText;
                Mode = NoteInteractionMode.Saved;
            }

            return result;
        }
    }
}
