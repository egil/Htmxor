using System.Net;
using HtmxorExamples.Data;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Samples.Tests;

/// <summary>
/// Protects <c>samples/HtmxorExamples/ArchiveTogglePage.razor</c> (#306): its
/// <c>@onpatch="ToggleArchive"</c> binding sits after <c>@inherits</c> and <c>@code</c>, inside
/// <c>@foreach</c>, inside <c>HtmxFragment</c>. No existing harness hosts
/// <c>samples/HtmxorExamples</c> requests, so this hosts the sample's own real, compiled entry
/// point directly, the narrowest boundary that exercises the deployed application's actual
/// generated routing without new application infrastructure.
/// </summary>
public sealed class ArchiveTogglePageTests : IAsyncLifetime
{
	private IAlbaHost host = default!;

	public async Task InitializeAsync()
	{
		host = await AlbaHost.For<global::Program>();
	}

	[Fact]
	public async Task Patch_archive_toggle_reaches_ToggleArchive_on_the_request_created_instance()
	{
		var contact = Contacts.Data.Values.First();
		var wasArchived = contact.Archived;
		var (token, cookie) = CreateAntiforgeryCredentials();

		await host.Scenario(scenario =>
		{
			scenario.Patch.Url($"/examples/archive-toggle/{contact.Id}");
			scenario.WithRequestHeader("HX-Request", "true");
			scenario.WithRequestHeader("HX-Request-Type", "partial");
			scenario.WithRequestHeader("Cookie", cookie);
			scenario.WithRequestHeader("RequestVerificationToken", token);
			scenario.StatusCodeShouldBe(HttpStatusCode.OK);
		});

		Assert.Equal(!wasArchived, Contacts.Data[contact.Id].Archived);
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
