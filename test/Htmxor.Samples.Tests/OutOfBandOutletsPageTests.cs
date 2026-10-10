using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Samples.Tests;

/// <summary>
/// Protects <c>samples/HtmxorExamples/Components/Pages/Examples/OutOfBandOutlets/Index.razor</c>:
/// a PUT to its stock <c>@page</c>'s <c>@onput="IncrementCount"</c> binding answers 200 and
/// increments the bound <c>CurrentCount</c>, instead of answering 405.
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
			scenario.Put.FormData(new()
				{
					{ "CurrentCount", "1" },
				})
				.ToUrl("/examples/out-of-band-outlets");
			scenario.WithRequestHeader("HX-Request", "true");
			scenario.WithRequestHeader("HX-Request-Type", "partial");
			scenario.WithRequestHeader("HX-Target", "div#counter");
			scenario.WithRequestHeader("Cookie", cookie);
			scenario.WithRequestHeader("RequestVerificationToken", token);
			scenario.StatusCodeShouldBe(HttpStatusCode.OK);
		});

		var body = result.ReadAsText();
		Assert.Contains("Current count: 2", body, StringComparison.Ordinal);
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
