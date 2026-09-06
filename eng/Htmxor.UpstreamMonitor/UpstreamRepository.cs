using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Htmxor.UpstreamMonitor;

internal sealed partial class UpstreamRepository(GitHubApi api, string repository)
{
	public async Task<UpstreamRevision> ResolveAsync(MonitorRequest request, CancellationToken cancellationToken)
	{
		var tag = request.RequestedTag ?? await LatestTagAsync(request.SupportedMajorVersion, cancellationToken);
		if (StableVersion(tag)?.Major != request.SupportedMajorVersion)
		{
			throw new MonitorFailure("The requested tag is not a stable supported ASP.NET Core release.");
		}
		var reference = await api.GetAsync($"/repos/{repository}/git/ref/tags/{Uri.EscapeDataString(tag)}", cancellationToken);
		var target = reference.GetProperty("object");
		var visited = new HashSet<string>(StringComparer.Ordinal);
		while (target.GetProperty("type").GetString() == "tag")
		{
			var sha = target.GetProperty("sha").GetString()!;
			if (!visited.Add(sha))
			{
				throw new MonitorFailure("GitHub annotated tag resolution repeated an object.");
			}
			var annotated = await api.GetAsync($"/repos/{repository}/git/tags/{Uri.EscapeDataString(sha)}", cancellationToken);
			target = annotated.GetProperty("object");
		}
		if (target.GetProperty("type").GetString() != "commit")
		{
			throw new MonitorFailure("GitHub tag did not resolve to a commit.");
		}
		return new(tag, target.GetProperty("sha").GetString()!);
	}

	public async Task<IReadOnlyList<ChangedFile>> CompareAsync(string baseline, string target, CancellationToken cancellationToken)
	{
		var comparison = await api.GetAsync($"/repos/{repository}/compare/{Uri.EscapeDataString(baseline)}...{Uri.EscapeDataString(target)}", cancellationToken);
		var files = comparison.GetProperty("files");
		if (files.GetArrayLength() >= 300)
		{
			throw new MonitorFailure("GitHub compare file inventory reached the 300-file limit; completeness is unknown.");
		}
		return files.EnumerateArray().SelectMany(Changes).ToArray();
	}

	public async Task<string> SourceAsync(string path, string commit, CancellationToken cancellationToken)
	{
		var encodedPath = string.Join('/', path.Split('/').Select(Uri.EscapeDataString));
		var content = await api.GetAsync($"/repos/{repository}/contents/{encodedPath}?ref={Uri.EscapeDataString(commit)}", cancellationToken);
		if (content.GetProperty("encoding").GetString() != "base64")
		{
			throw new MonitorFailure("GitHub source content used an unsupported encoding.");
		}
		return Encoding.UTF8.GetString(Convert.FromBase64String(content.GetProperty("content").GetString()!));
	}

	private async Task<string> LatestTagAsync(int major, CancellationToken cancellationToken)
	{
		var releases = await api.GetPagesAsync($"/repos/{repository}/releases?per_page=100", cancellationToken);
		return releases.Where(IsStable).Select(release => release.GetProperty("tag_name").GetString()!)
			.Where(tag => StableVersion(tag)?.Major == major).OrderByDescending(StableVersion).FirstOrDefault()
			?? throw new MonitorFailure("No stable supported ASP.NET Core release was found.");
	}

	private static bool IsStable(JsonElement release) =>
		!release.GetProperty("draft").GetBoolean() && !release.GetProperty("prerelease").GetBoolean();

	private static Version? StableVersion(string tag) =>
		StableTag().IsMatch(tag) && Version.TryParse(tag[1..], out var version) ? version : null;

	private static IEnumerable<ChangedFile> Changes(JsonElement file)
	{
		var path = file.GetProperty("filename").GetString()!;
		var status = file.GetProperty("status").GetString();
		if (status == "renamed")
		{
			return [new(file.GetProperty("previous_filename").GetString()!, ChangeKind.Removed), new(path, ChangeKind.Added)];
		}
		return [new(path, status switch
		{
			"added" => ChangeKind.Added,
			"removed" => ChangeKind.Removed,
			"modified" or "changed" => ChangeKind.Changed,
			_ => throw new MonitorFailure("GitHub compare returned an unsupported file status."),
		})];
	}

	[GeneratedRegex(@"^v\d+\.\d+\.\d+$")]
	private static partial Regex StableTag();
}

internal sealed record ChangedFile(string Path, ChangeKind Kind);
