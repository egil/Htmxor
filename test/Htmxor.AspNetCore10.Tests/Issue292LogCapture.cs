using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

internal sealed class Issue292LogCapture : ILoggerProvider
{
	internal const string Category = "Htmxor.Builder.HtmxorDirectEndpointMatcherPolicy";
	private readonly ConcurrentQueue<Entry> entries = new();

	public Entry[] Entries => entries.ToArray();

	public ILogger CreateLogger(string categoryName) => new CaptureLogger(categoryName, entries);

	public void Dispose() { }

	internal sealed record Entry(
		LogLevel Level,
		EventId EventId,
		IReadOnlyDictionary<string, object?> Fields,
		string Message,
		Exception? Exception);

	private sealed class CaptureLogger(string category, ConcurrentQueue<Entry> entries) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => category == Category;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
			Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			var fields = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(state);
			entries.Enqueue(new(logLevel, eventId, fields.ToDictionary(), formatter(state, exception), exception));
		}
	}
}
