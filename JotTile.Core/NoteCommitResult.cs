namespace JotTile.Core
{
    internal sealed class NoteCommitResult
    {
        private NoteCommitResult(bool succeeded, string? errorMessage)
        {
            Succeeded = succeeded;
            ErrorMessage = errorMessage;
        }

        internal bool Succeeded { get; }

        internal string? ErrorMessage { get; }

        internal static NoteCommitResult Success()
        {
            return new NoteCommitResult(true, null);
        }

        internal static NoteCommitResult Failure(string errorMessage)
        {
            return new NoteCommitResult(false, errorMessage);
        }
    }
}
