using System.Net;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;

namespace Htmxor.AspNetCore10;

public sealed class Issue210AuthorizationTests
{
	[Theory]
	[InlineData("actions", false, null, HttpStatusCode.Unauthorized)]
	[InlineData("form", false, null, HttpStatusCode.Unauthorized)]
	[InlineData("actions", true, Issue83AuthenticationHandler.ForbiddenUser, HttpStatusCode.Forbidden)]
	[InlineData("form", true, Issue83AuthenticationHandler.ForbiddenUser, HttpStatusCode.Forbidden)]
	public async Task Fallback_and_group_custom_requirement_reject_before_application_effects(
		string page, bool group, string? user, HttpStatusCode expected)
	{
		await using var host = await Issue210SecurityHost.StartAsync(groupAuthorization: group);
		SetUser(host.Client, user);
		using var request = Issue210SecurityHost.Request("GET", page, direct: page == "actions");
		request.Headers.Add("Sec-Fetch-Site", "same-origin");

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(expected, response.StatusCode);
		host.Probe.AssertUntouched();
	}

	[Theory]
	[InlineData("actions", false, Issue83AuthenticationHandler.ForbiddenUser)]
	[InlineData("form", false, Issue83AuthenticationHandler.ForbiddenUser)]
	[InlineData("actions", true, Issue83AuthenticationHandler.AuthorizedUser)]
	[InlineData("form", true, Issue83AuthenticationHandler.AuthorizedUser)]
	public async Task Authorized_user_reaches_ordinary_and_generated_routes_under_effective_policy(
		string page, bool group, string user)
	{
		await using var host = await Issue210SecurityHost.StartAsync(groupAuthorization: group);
		SetUser(host.Client, user);
		using var request = Issue210SecurityHost.Request("GET", page, direct: page == "actions");

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(1, host.Probe.Initializations);
	}

	[Theory]
	[InlineData("actions", "PUT")]
	[InlineData("form", "POST")]
	public async Task Valid_protection_does_not_bypass_group_custom_authorization(string page, string method)
	{
		await using var host = await Issue210SecurityHost.StartAsync(groupAuthorization: true);
		using var request = Issue210SecurityHost.Request(method, page, form: true, direct: page == "actions");
		await host.AddCredentialsAsync(request);
		SetUser(host.Client, Issue83AuthenticationHandler.ForbiddenUser);
		request.Headers.Add("Sec-Fetch-Site", "same-origin");

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		host.Probe.AssertUntouched();
	}

	[Theory]
	[InlineData("actions", "PUT")]
	[InlineData("form", "POST")]
	public async Task Authorized_user_with_valid_protection_runs_the_application_callback(string page, string method)
	{
		await using var host = await Issue210SecurityHost.StartAsync(groupAuthorization: true);
		using var request = Issue210SecurityHost.Request(method, page, form: true, direct: page == "actions");
		await host.AddCredentialsAsync(request);
		request.Headers.Add("Sec-Fetch-Site", "same-origin");

		using var response = await host.Client.SendAsync(request);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal(1, host.Probe.Callbacks);
	}

	[Theory]
	[InlineData(typeof(Issue210ActionPage))]
	[InlineData(typeof(Issue210FormPage))]
	[InlineData(typeof(Issue210ActionlessPage))]
	public async Task Group_host_rate_limit_and_cors_metadata_survive_on_component_routes(Type componentType)
	{
		await using var host = await Issue210SecurityHost.StartAsync(cors: "trusted", groupAuthorization: true);
		var endpoints = ((IEndpointRouteBuilder)host.App).DataSources.SelectMany(source => source.Endpoints)
			.Where(endpoint => endpoint.Metadata.GetMetadata<ComponentTypeMetadata>()?.Type == componentType);

		var endpoint = Assert.Single(endpoints);
		Assert.Equal("localhost", Assert.Single(endpoint.Metadata.GetRequiredMetadata<IHostMetadata>().Hosts));
		Assert.Equal("bounded", endpoint.Metadata.GetRequiredMetadata<EnableRateLimitingAttribute>().PolicyName);
		Assert.Equal("trusted", endpoint.Metadata.GetRequiredMetadata<IEnableCorsAttribute>().PolicyName);
	}

	private static void SetUser(HttpClient client, string? user)
	{
		client.DefaultRequestHeaders.Remove(Issue83AuthenticationHandler.UserHeaderName);
		if (user is not null)
		{
			client.DefaultRequestHeaders.Add(Issue83AuthenticationHandler.UserHeaderName, user);
		}
	}
}
