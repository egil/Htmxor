#if NET11_0_OR_GREATER
using System.Net;

namespace Htmxor.AspNetCore10.Issue362.Routing;

public sealed partial class Issue362RoutingActionsTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("invalid-token")]
	public async Task Headerless_actionless_DELETE_preserves_native_allowance_despite_missing_or_invalid_token(string? token)
	{
		var (_, cookie, _) = await GetAntiforgeryCredentialsAsync(PackageConsumerAuthenticationHandler.AllAccessUserName);
		applicationProbe.Reset();
		using var request = CreateActionRequest(HttpMethod.Delete, ActionlessSummaryRequestPath, SummaryHost,
			PackageConsumerAuthenticationHandler.AllAccessUserName, cookie, token);

		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, response.StatusCode),
			() => Assert.Contains("data-issue-97-summary-component", body, StringComparison.Ordinal),
			() => Assert.Equal(1, applicationProbe.BindingCount),
			() => Assert.Equal(1, applicationProbe.InitializationCount),
			() => Assert.Equal(0, applicationProbe.CallbackCount));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("invalid-token")]
	[InlineData("valid-token")]
	public async Task Cross_site_actionless_DELETE_preserves_native_rejection_before_lifecycle(string? tokenKind)
	{
		var (validToken, cookie, _) = await GetAntiforgeryCredentialsAsync(PackageConsumerAuthenticationHandler.AllAccessUserName);
		applicationProbe.Reset();
		using var request = CreateActionRequest(HttpMethod.Delete, ActionlessSummaryRequestPath, SummaryHost,
			PackageConsumerAuthenticationHandler.AllAccessUserName, cookie, tokenKind == "valid-token" ? validToken : tokenKind);
		request.Headers.Add("Sec-Fetch-Site", "cross-site");
		request.Headers.Add("Origin", "https://untrusted.example");

		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode),
			() => Assert.DoesNotContain("data-issue-97-summary-component", body, StringComparison.Ordinal),
			() => AssertNoApplicationCodeRan());
	}
}
#endif
