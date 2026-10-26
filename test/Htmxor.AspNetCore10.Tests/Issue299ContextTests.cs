using System.Reflection;
using Htmxor.Builder;
using Htmxor.Http;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Htmxor.AspNetCore10;

public sealed class Issue299ContextTests(Issue299Host host) : IClassFixture<Issue299Host>
{
	[Fact]
	public async Task Generated_post_dispatches_with_the_supported_per_request_instances()
	{
		var (token, cookie) = CreateAntiforgeryCredentials();

		using var request = new HttpRequestMessage(HttpMethod.Post, "/issue-299/context");
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		request.Headers.Add("Cookie", cookie);
		request.Headers.Add("RequestVerificationToken", token);
		using var response = await host.Client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.True(
			response.StatusCode == HttpStatusCode.OK,
			$"Expected the supported component-tag @onpost action to answer 200 OK, received " +
			$"{(int)response.StatusCode} {response.StatusCode}. Body: {body}"),
			() => Assert.Contains("data-callback=\"same-request\"", body, StringComparison.Ordinal),
			() => Assert.Contains("issue299-saved", response.Headers.GetValues("HX-Trigger").Single(), StringComparison.Ordinal)
		);
	}

	[Fact]
	public void Generated_action_factory_preserves_request_and_response_identity()
	{
		var factory = RequireEventArgsFactory();
		var context = new DefaultHttpContext().GetHtmxContext();

		var args = Assert.IsType<HtmxEventArgs>(factory.Invoke(null, new object[] { context }));

		Assert.Multiple(
			() => Assert.Same(context.Request, args.Request),
			() => Assert.Same(context.Response, args.Response)
		);
	}

	[Fact]
	public void Generated_action_factory_rejects_a_missing_context()
	{
		var factory = RequireEventArgsFactory();

		var exception = Assert.Throws<TargetInvocationException>(() => factory.Invoke(null, new object?[] { null }));

		Assert.Equal("context", Assert.IsType<ArgumentNullException>(exception.InnerException).ParamName);
	}

	private static MethodInfo RequireEventArgsFactory()
	{
		var method = typeof(HtmxorGeneratedComponentAction).GetMethod("CreateEventArgs",
			BindingFlags.Public | BindingFlags.Static, new[] { typeof(HtmxContext) });
		Assert.NotNull(method);
		return method;
	}

	private (string Token, string Cookie) CreateAntiforgeryCredentials()
	{
		var antiforgery = host.App.Services.GetRequiredService<IAntiforgery>();
		var context = new DefaultHttpContext { RequestServices = host.App.Services };
		var tokenSet = antiforgery.GetAndStoreTokens(context);
		var setCookie = context.Response.Headers["Set-Cookie"]
			.Single(value => value!.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal))!;

		return (tokenSet.RequestToken!, setCookie.Split(';')[0]);
	}

}

public sealed class Issue299Host : IAsyncLifetime
{
	public WebApplication App { get; private set; } = default!;
	public HttpClient Client { get; private set; } = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();
		builder.Services.AddRazorComponents().AddHtmxor();
		App = builder.Build();
		App.UseAntiforgery();
		App.MapRazorComponents<Issue299App>().AddHtmxorEndpoints();
		await App.StartAsync();
		Client = App.GetTestClient();
	}

	public async Task DisposeAsync()
	{
		Client.Dispose();
		await App.DisposeAsync();
	}
}
