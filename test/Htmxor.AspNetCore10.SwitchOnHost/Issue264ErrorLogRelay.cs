using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10.SwitchOnHost;

// The host clears every default logging provider (see Issue264SwitchOnHostRuntime.ConfigureServices), so
// without this, an Error-level log entry -- such as GetErrorHandledTask's, on both stock's own
// EndpointHtmlRenderer and the candidate's HtmxorEndpointCandidateRenderer -- is silently discarded. Relaying
// every Error-or-above entry to this process's own stdout, on one line each, lets the parent test count them
// per request the same way it already counts "UNOBSERVED " lines.
internal sealed class Issue264ErrorLogRelay : ILoggerProvider
{
	public ILogger CreateLogger(string categoryName) => new RelayLogger(categoryName);

	public void Dispose()
	{
	}

	private sealed class RelayLogger(string categoryName) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state)
			where TState : notnull
			=> null;

		public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

		public void Log<TState>(
			LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			// One line per entry: the parent's line-based reader (Issue264SwitchOnHostProcess) would otherwise
			// miscount a multi-line message or exception as more than one entry.
			var message = formatter(state, exception).Replace('\r', ' ').Replace('\n', ' ');
			Console.WriteLine($"ERROR {categoryName}|{message}");
		}
	}
}
