namespace JotTile.Core
{
    internal sealed class RepositoryLoadResult<T>
    {
        internal RepositoryLoadResult(T value, string? userMessage = null)
        {
            Value = value;
            UserMessage = userMessage;
        }

        internal T Value { get; }

        internal string? UserMessage { get; }
    }
}
