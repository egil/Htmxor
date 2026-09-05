namespace Htmxor.Quality;

internal sealed record RepositoryEvidence(string Head, bool Dirty)
{
	public static async Task<RepositoryEvidence> CaptureAsync(
		string repositoryRoot,
		IProcessRunner runner,
		CancellationToken cancellationToken)
	{
		var headResult = await runner.RunAsync(
			Git(repositoryRoot, "rev-parse", "HEAD"),
			cancellationToken);
		var statusResult = await runner.RunAsync(
			Git(repositoryRoot, "status", "--porcelain=v1", "--untracked-files=all"),
			cancellationToken);
		var head = headResult.StandardOutput.Trim();
		if (head.Length == 0)
		{
			throw new InvalidOperationException("Git returned an empty HEAD.");
		}

		return new(head, !string.IsNullOrWhiteSpace(statusResult.StandardOutput));
	}

	private static ProcessCommand Git(string repositoryRoot, params string[] arguments) =>
		new("git", repositoryRoot, arguments, NetworkAccess: NetworkAccess.Disabled);
}
