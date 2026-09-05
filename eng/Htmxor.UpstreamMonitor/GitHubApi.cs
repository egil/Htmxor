using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Htmxor.UpstreamMonitor;

internal sealed partial class GitHubApi(HttpClient client)
{
	public async Task<JsonElement> GetAsync(string path, CancellationToken cancellationToken)
	{
		using var response = await client.GetAsync(ValidatePath(path), cancellationToken);
		return await ReadAsync(response, cancellationToken);
	}

	public async Task<IReadOnlyList<JsonElement>> GetPagesAsync(string path, CancellationToken cancellationToken)
	{
		var items = new List<JsonElement>();
		var visited = new HashSet<string>(StringComparer.Ordinal);
		string? next = path;
		while (next is not null)
		{
			if (!visited.Add(next))
			{
				throw new MonitorFailure("GitHub pagination repeated a page.");
			}
			using var response = await client.GetAsync(ValidatePath(next), cancellationToken);
			var page = await ReadAsync(response, cancellationToken);
			items.AddRange(page.EnumerateArray());
			next = NextPage(response);
		}
		return items;
	}

	public async Task<JsonElement> WriteAsync(HttpMethod method, string path, object body, CancellationToken cancellationToken)
	{
		using var request = new HttpRequestMessage(method, ValidatePath(path)) { Content = JsonContent.Create(body) };
		using var response = await client.SendAsync(request, cancellationToken);
		return await ReadAsync(response, cancellationToken);
	}

	private Uri ValidatePath(string path)
	{
		var address = new Uri(client.BaseAddress!, path);
		if (address.GetLeftPart(UriPartial.Authority) != client.BaseAddress!.GetLeftPart(UriPartial.Authority))
		{
			throw new MonitorFailure("GitHub pagination referenced an unexpected origin.");
		}
		return address;
	}

	private static async Task<JsonElement> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (!response.IsSuccessStatusCode)
		{
			throw new MonitorFailure($"GitHub API returned {(int)response.StatusCode} {response.ReasonPhrase}.");
		}
		using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
		return document.RootElement.Clone();
	}

	private static string? NextPage(HttpResponseMessage response) =>
		response.Headers.TryGetValues("Link", out var links)
			? links.SelectMany(value => NextLink().Matches(value).Select(match => match.Groups[1].Value)).FirstOrDefault()
			: null;

	[GeneratedRegex("<([^>]+)>;\\s*rel=\"next\"")]
	private static partial Regex NextLink();
}

internal sealed class MonitorFailure(string message) : Exception(message);

internal static class MonitorErrors
{
	public static string SafeMessage(Exception exception) => exception is MonitorFailure
		? exception.Message
		: "Upstream monitor infrastructure failed.";
}
