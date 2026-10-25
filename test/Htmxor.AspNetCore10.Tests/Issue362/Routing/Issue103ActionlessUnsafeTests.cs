using System.Net;
using Htmxor.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Htmxor.AspNetCore10.Issue362.Routing;

public sealed partial class Issue362RoutingActionsTests
{
	private const string ActionlessSummaryRequestPath = "/issue-362-group/actionless-summaries/84";
	[Fact]
	public async Task Explicit_delete_without_an_action_validates_then_renders_the_request_component()
	{
		var (token, cookie, _) = await GetAntiforgeryCredentialsAsync(
			PackageConsumerAuthenticationHandler.AllAccessUserName);
		applicationProbe.Reset();

		using var request = CreateActionRequest(
			HttpMethod.Delete,
			ActionlessSummaryRequestPath,
			SummaryHost,
			PackageConsumerAuthenticationHandler.AllAccessUserName,
			cookie,
			token);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, response.StatusCode),
			() => Assert.Contains("data-issue-97-summary-component", body, StringComparison.Ordinal),
			() => Assert.Equal(1, applicationProbe.BindingCount),
			() => Assert.Equal(1, applicationProbe.InitializationCount),
			() => Assert.Equal(0, applicationProbe.CallbackCount));
	}

	[Fact]
	public async Task Explicit_delete_without_an_action_can_suppress_its_rendered_body_for_navigation()
	{
		var (token, cookie, _) = await GetAntiforgeryCredentialsAsync(
			PackageConsumerAuthenticationHandler.AllAccessUserName);
		applicationProbe.Reset();

		using var request = CreateActionRequest(
			HttpMethod.Delete,
			ActionlessSummaryRequestPath + "?suppressNavigationResponse=true",
			SummaryHost,
			PackageConsumerAuthenticationHandler.AllAccessUserName,
			cookie,
			token);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(HttpStatusCode.OK, response.StatusCode),
			() => Assert.Equal(
			"/actionless-navigation-destination",
			Assert.Single(response.Headers.GetValues(HtmxResponseHeaderNames.Location))),
			() => Assert.Equal(string.Empty, body),
			() => Assert.Equal(1, applicationProbe.BindingCount),
			() => Assert.Equal(1, applicationProbe.InitializationCount),
			() => Assert.Equal(0, applicationProbe.CallbackCount));
	}

#if !NET11_0_OR_GREATER
	[Theory]
	[InlineData(null)]
	[InlineData("invalid-token")]
	public async Task Explicit_delete_without_an_action_rejects_antiforgery_before_component_lifecycle(
		string? token)
	{
		var (_, cookie, _) = await GetAntiforgeryCredentialsAsync(
			PackageConsumerAuthenticationHandler.AllAccessUserName);
		applicationProbe.Reset();

		using var request = CreateActionRequest(
			HttpMethod.Delete,
			ActionlessSummaryRequestPath,
			SummaryHost,
			PackageConsumerAuthenticationHandler.AllAccessUserName,
			cookie,
			token);
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.True(
			response.StatusCode == HttpStatusCode.BadRequest &&
			applicationProbe.BindingCount == 0 &&
			applicationProbe.InitializationCount == 0 &&
			applicationProbe.CallbackCount == 0,
			$"Expected antiforgery rejection before component lifecycle. " +
			$"Received {(int)response.StatusCode} {response.StatusCode}; " +
			$"binding={applicationProbe.BindingCount}, " +
			$"initialization={applicationProbe.InitializationCount}, " +
			$"callback={applicationProbe.CallbackCount}. Body: {body}"),
			() => Assert.DoesNotContain("data-issue-97-summary-component", body, StringComparison.Ordinal),
			() => AssertNoApplicationCodeRan());
	}
#endif

	[Fact]
	public void Actionless_unsafe_route_preserves_declared_methods_and_security_metadata()
	{
		var endpoints = ((IEndpointRouteBuilder)app).DataSources
			.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>()
			.Where(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>() is not null);
		var endpoint = AssertGeneratedEndpoint<Issue362ActionlessSummaryComponent>(
			endpoints, "/actionless-summaries/{SummaryId:int}", SummaryPolicyName,
			[HttpMethods.Get, HttpMethods.Delete], [HttpMethods.Get, HttpMethods.Delete],
			"/orders", "section", []);
		Assert.Multiple(
			() => AssertHost(endpoint, SummaryHost),
			() => Assert.True(endpoint.Metadata.GetRequiredMetadata<IAntiforgeryMetadata>().RequiresValidation));
	}

}
