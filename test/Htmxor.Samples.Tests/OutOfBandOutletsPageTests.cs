using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Samples.Tests;

/// <summary>
/// Protects <c>samples/HtmxorExamples/Components/Pages/Examples/OutOfBandOutlets/Index.razor</c>.
/// A #310 decision moved this criterion from #177 to #285: the page's stock <c>@page</c> declares
/// <c>@onput="IncrementCount"</c>, and a PUT to the sample's own unmodified entry point must reach
/// <c>IncrementCount</c> rather than answer 405.
/// </summary>
public sealed class OutOfBandOutletsPageTests : IAsyncLifetime
{
	private IAlbaHost host = default!;

	public async Task InitializeAsync()
	{
		host = await AlbaHost.For<global::Program>();
	}

	[Fact]
	public async Task Put_out_of_band_outlets_increments_CurrentCount_and_answers_200()
	{
		var (token, cookie) = CreateAntiforgeryCredentials();

		var result = await host.Scenario(scenario =>
		{
			scenario.Put.Url("/examples/out-of-band-outlets");
			scenario.WithRequestHeader("HX-Request", "true");
			scenario.WithRequestHeader("HX-Request-Type", "partial");
			scenario.WithRequestHeader("Cookie", cookie);
			scenario.WithRequestHeader("RequestVerificationToken", token);
			scenario.ConfigureHttpContext(context =>
			{
				using var form = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					["CurrentCount"] = "0",
				});
				form.CopyTo(context.Request.Body, null, CancellationToken.None);
				context.Request.Headers.ContentType = form.Headers.ContentType!.ToString();
				context.Request.Headers.ContentLength = form.Headers.ContentLength;
			});
			scenario.StatusCodeShouldBe(HttpStatusCode.OK);
		});

		var body = result.ReadAsText();
		Assert.Contains("Current count: 1", body, StringComparison.Ordinal);
		Assert.Contains("The count is odd!", body, StringComparison.Ordinal);
	}

	private (string Token, string Cookie) CreateAntiforgeryCredentials()
	{
		var context = new DefaultHttpContext();
		var tokenSet = host.Services.GetRequiredService<IAntiforgery>().GetAndStoreTokens(context);
		var setCookie = context.Response.Headers["Set-Cookie"]
			.Single(value => value!.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal))!;

		return (tokenSet.RequestToken!, setCookie.Split(';')[0]);
	}

	public async Task DisposeAsync()
	{
		await host.DisposeAsync();
	}
}
