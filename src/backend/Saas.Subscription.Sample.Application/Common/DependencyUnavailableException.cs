namespace Saas.Subscription.Sample.Application.Common;

/// <summary>
/// A required dependency (database, cache, provider) is temporarily unavailable. The API maps it to a
/// recoverable 503 instead of a 500; the message and inner exception are logged but never returned.
/// </summary>
public sealed class DependencyUnavailableException : Exception
{
    public DependencyUnavailableException(string message)
        : base(message)
    {
    }

    public DependencyUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
