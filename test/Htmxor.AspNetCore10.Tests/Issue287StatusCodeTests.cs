using System.Net;
using System.Text.RegularExpressions;
using Htmxor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Htmxor.AspNetCore10;

public sealed class Issue287StatusCodeTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Invalid_named_EditForm_submission_returns_422_validation_markup_and_calls_back_once(bool htmx)
	{
		await using var app = await StartAsync();
		using var client = app.GetTestClient();
		using var get = await client.GetAsync("/issue-287/form");
		var form = await get.Content.ReadAsStringAsync();
		Assert.Equal(HttpStatusCode.OK, get.StatusCode);
		var token = Regex.Match(form, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"");
		Assert.True(token.Success, form);
		using var request = new HttpRequestMessage(HttpMethod.Post, "/issue-287/form");
		if (htmx)
			request.Headers.Add("HX-Request", "true");
		request.Headers.Add("Cookie", Assert.Single(get.Headers.GetValues("Set-Cookie"),
			value => value.StartsWith(".AspNetCore.Antiforgery.", StringComparison.Ordinal)).Split(';')[0]);
		request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
		{
			["_handler"] = "save",
			["Model.Name"] = "",
			["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value),
		});

		using var response = await client.SendAsync(request);
		var html = await response.Content.ReadAsStringAsync();

		Assert.Multiple(
			() => Assert.Equal(1, app.Services.GetRequiredService<Issue287CallbackProbe>().Count),
			() => Assert.True(response.StatusCode == HttpStatusCode.UnprocessableEntity,
			$"Expected 422 validation response, received {(int)response.StatusCode} {response.StatusCode}. Body: {html}"),
			() => Assert.Contains(">The Name field is required.</div>", html, StringComparison.Ordinal),
			() => Assert.Contains("data-callback-count=\"1\" data-result=\"invalid\"", html, StringComparison.Ordinal)
		);
	}

	internal static async Task<WebApplication> StartAsync()
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();
		builder.Services.AddSingleton<Issue287CallbackProbe>();
		builder.Services.AddRazorComponents().AddHtmxor();
		var app = builder.Build();
		app.UseDeveloperExceptionPage();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue287App>().AddHtmxorEndpoints();
		await app.StartAsync();
		return app;
	}
}

public sealed class Issue287CallbackProbe
{
	public int Count { get; set; }
}
