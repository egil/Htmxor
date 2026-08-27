namespace Htmxor.Quality;

internal sealed record ProcessCommand(
	string FileName,
	string WorkingDirectory,
	IReadOnlyList<string> Arguments,
	bool EnsureSuccess = true)
{
	public string Display =>
		$"{FileName} {string.Join(' ', Arguments.Select(QuoteForDisplay))}";

	private static string QuoteForDisplay(string value) =>
		value.Any(char.IsWhiteSpace) ? $"\"{value}\"" : value;
}

internal sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

internal interface IProcessRunner
{
	Task<ProcessResult> RunAsync(
		ProcessCommand command,
		CancellationToken cancellationToken = default);
}
