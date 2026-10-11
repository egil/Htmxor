using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Htmxor.AspNetCore10;

/// <summary>
/// Issue #286, deferred onto this project's supported framework matrix: a static path, an
/// unconstrained (<c>string</c>) parameter, an optional parameter present and absent, a
/// catch-all, an inline default value present and absent, a multi-constraint segment, the
/// non-<c>int</c> type-converting constraints, string-valued constraints, a complex segment, a
/// <c>{**Rest}</c> catch-all, and a non-matching constrained value each answer the same value or
/// status on an <c>HtmxRoute</c> component as on a paired stock <c>@page</c> twin, pinned on
/// net10.0 and net11.0.
/// </summary>
public sealed class Issue286StockRouteTemplateParityTests : IAsyncLifetime
{
	private WebApplication app = default!;
	private HttpClient client = default!;

	public async Task InitializeAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			ApplicationName = typeof(Issue286StockRouteTemplateParityTests).Assembly.GetName().Name,
			EnvironmentName = Environments.Development,
		});
		builder.WebHost.UseTestServer();
		builder.Logging.ClearProviders();
		builder.Services.AddRazorComponents().AddHtmxor();

		app = builder.Build();
		app.UseAntiforgery();
		app.MapRazorComponents<Issue78App>().AddHtmxorEndpoints();

		await app.StartAsync();
		client = app.GetTestClient();
	}

	[Fact]
	public async Task Static_path_binds_the_same_as_its_stock_twin()
	{
		await AssertHtmxBodyContainsAsync("/issue-286-10/static", "static-ok");
		await AssertStockBodyContainsAsync("/issue-286-10/static-stock", "static-ok");
	}

	[Fact]
	public async Task Unconstrained_string_parameter_binds_the_same_as_its_stock_twin()
	{
		await AssertHtmxBodyContainsAsync("/issue-286-10/string/hello-world", "value=hello-world");
		await AssertStockBodyContainsAsync("/issue-286-10/string-stock/hello-world", "value=hello-world");
	}

	[Fact]
	public async Task Optional_parameter_present_binds_the_same_as_its_stock_twin()
	{
		await AssertHtmxBodyContainsAsync("/issue-286-10/optional/7", "page=7");
		await AssertStockBodyContainsAsync("/issue-286-10/optional-stock/7", "page=7");
	}

	[Fact]
	public async Task Optional_parameter_absent_binds_the_same_as_its_stock_twin()
	{
		await AssertHtmxBodyContainsAsync("/issue-286-10/optional", "page=none");
		await AssertStockBodyContainsAsync("/issue-286-10/optional-stock", "page=none");
	}

	[Fact]
	public async Task Catchall_parameter_binds_the_same_as_its_stock_twin()
	{
		await AssertHtmxBodyContainsAsync("/issue-286-10/catchall/a/b/c", "rest=a/b/c");
		await AssertStockBodyContainsAsync("/issue-286-10/catchall-stock/a/b/c", "rest=a/b/c");
	}

	[Fact]
	public async Task Default_value_present_binds_the_same_as_its_stock_twin()
	{
		await AssertHtmxBodyContainsAsync("/issue-286-10/default/7", "id=7");
		await AssertStockBodyContainsAsync("/issue-286-10/default-stock/7", "id=7");
	}

	[Fact]
	public async Task Default_value_absent_binds_the_declared_default_the_same_as_its_stock_twin()
	{
		await AssertHtmxBodyContainsAsync("/issue-286-10/default", "id=5");
		await AssertStockBodyContainsAsync("/issue-286-10/default-stock", "id=5");
	}

	[Fact]
	public async Task Multi_constraint_segment_binds_the_same_as_its_stock_twin()
	{
		await AssertHtmxBodyContainsAsync("/issue-286-10/multi/7", "id=7");
		await AssertStockBodyContainsAsync("/issue-286-10/multi-stock/7", "id=7");
	}

	[Fact]
	public async Task Non_int_type_converting_constraints_bind_the_same_as_their_stock_twin()
	{
		const string expected = "True|2024-01-02|1.5|2.5|3.5|11111111-1111-1111-1111-111111111111|9876543210";
		await AssertHtmxBodyContainsAsync(
			"/issue-286-10/converting/true/2024-01-02/1.5/2.5/3.5/11111111-1111-1111-1111-111111111111/9876543210",
			expected);
		await AssertStockBodyContainsAsync(
			"/issue-286-10/converting-stock/true/2024-01-02/1.5/2.5/3.5/11111111-1111-1111-1111-111111111111/9876543210",
			expected);
	}

	[Fact]
	public async Task String_valued_constraints_bind_the_same_as_their_stock_twin()
	{
		const string expected = "abcd|alpha|regexval";
		await AssertHtmxBodyContainsAsync("/issue-286-10/stringconstraint/abcd/alpha/regexval", expected);
		await AssertStockBodyContainsAsync(
			"/issue-286-10/stringconstraint-stock/abcd/alpha/regexval",
			expected);
	}

	[Fact]
	public async Task Complex_segment_binds_the_same_as_its_stock_twin()
	{
		await AssertHtmxBodyContainsAsync("/issue-286-10/complex/left-right", "id=left|b=right");
		await AssertStockBodyContainsAsync("/issue-286-10/complex-stock/left-right", "id=left|b=right");
	}

	[Fact]
	public async Task Double_star_catchall_binds_the_same_as_its_stock_twin()
	{
		await AssertHtmxBodyContainsAsync("/issue-286-10/doublestar/a/b/c", "rest=a/b/c");
		await AssertStockBodyContainsAsync("/issue-286-10/doublestar-stock/a/b/c", "rest=a/b/c");
	}

	[Fact]
	public async Task Nonmatching_constrained_value_answers_the_same_status_as_its_stock_twin()
	{
		await AssertHtmxStatusAsync("/issue-286-10/multi/not-a-number", HttpStatusCode.NotFound);
		await AssertStockStatusAsync("/issue-286-10/multi-stock/not-a-number", HttpStatusCode.NotFound);
	}

	private async Task AssertHtmxStatusAsync(string path, HttpStatusCode expected)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		using var response = await client.SendAsync(request);

		Assert.Equal(expected, response.StatusCode);
	}

	private async Task AssertStockStatusAsync(string path, HttpStatusCode expected)
	{
		using var response = await client.GetAsync(path);

		Assert.Equal(expected, response.StatusCode);
	}

	private async Task AssertHtmxBodyContainsAsync(string path, string expected)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, path);
		request.Headers.Add("HX-Request", "true");
		request.Headers.Add("HX-Request-Type", "partial");
		using var response = await client.SendAsync(request);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(
			response.StatusCode == HttpStatusCode.OK,
			$"Expected GET {path} to answer 200 OK, received {(int)response.StatusCode} " +
			$"{response.StatusCode}. Body: {body}");
		Assert.Contains(expected, body, StringComparison.Ordinal);
	}

	private async Task AssertStockBodyContainsAsync(string path, string expected)
	{
		using var response = await client.GetAsync(path);
		var body = await response.Content.ReadAsStringAsync();

		Assert.True(
			response.StatusCode == HttpStatusCode.OK,
			$"Expected GET {path} to answer 200 OK, received {(int)response.StatusCode} " +
			$"{response.StatusCode}. Body: {body}");
		Assert.Contains(expected, body, StringComparison.Ordinal);
	}

	public async Task DisposeAsync()
	{
		client?.Dispose();
		if (app is not null)
		{
			await app.DisposeAsync();
		}
	}
}
