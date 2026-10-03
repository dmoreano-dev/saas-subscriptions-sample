using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

/// <summary>Records everything the host logs (message, structured state, exception, scopes) as plain text.</summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _entries = new();

    public IReadOnlyCollection<string> Entries => _entries;

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, ConcurrentQueue<string> entries) : ILogger
    {
        private readonly AsyncLocal<List<string>> _scopes = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            (_scopes.Value ??= []).Add(Describe(state));
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue(string.Join(
                '\n', category, formatter(state, exception), Describe(state), exception?.ToString() ?? string.Empty,
                string.Join('\n', _scopes.Value ?? [])));
        }

        private static string Describe<TState>(TState state) =>
            state is IEnumerable<KeyValuePair<string, object?>> pairs
                ? string.Join('\n', pairs.Select(pair => $"{pair.Key}={pair.Value}"))
                : state?.ToString() ?? string.Empty;
    }
}
