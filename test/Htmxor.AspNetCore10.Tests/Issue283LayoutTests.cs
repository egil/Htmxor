using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.AspNetCore10;

public sealed class Issue283LayoutTests
{
	[Theory]
	[InlineData("dual", "razor")]
	[InlineData("dual", "codebehind")]
	[InlineData("dual", "csharp")]
	[InlineData("htmx", "razor")]
	[InlineData("htmx", "codebehind")]
	[InlineData("htmx", "csharp")]
	public async Task Whole_direct_output_wraps_each_authoring_shape_in_effective_layout(string route, string shape)
	{
		await using var app = await Issue283Host.StartAsync(true);
		var body = await Issue283Host.ReadAsync(app, $"/issue-283/{route}/{shape}", "partial");
		var expected = shape == "codebehind"
			? $"<section data-override-layout><p data-page>{shape}</p></section>"
			: $"<aside data-default-layout><p data-page>{shape}</p></aside>";
		Assert.Equal(expected, body);
	}

	[Theory]
	[InlineData("priority", "priority")]
	[InlineData("inherited", "csharp")]
	[InlineData("base-priority", "csharp")]
	public async Task Higher_priority_overrides_imported_or_inherited_default(string path, string content)
	{
		await using var app = await Issue283Host.StartAsync(true);
		var body = await Issue283Host.ReadAsync(app, "/issue-283/" + path, "partial");
		Assert.Equal($"<section data-override-layout><p data-page>{content}</p></section>", body);
	}

	[Fact]
	public async Task Any_IComponent_accepting_Body_is_a_valid_layout()
	{
		await using var app = await Issue283Host.StartAsync(true);
		var body = await Issue283Host.ReadAsync(app, "/issue-283/plain", "partial");
		Assert.Equal("<article data-plain-layout><p data-page>csharp</p></article>", body);
	}

	[Fact]
	public async Task Convenience_layout_emits_PageTitle_from_its_HeadOutlet()
	{
		await using var app = await Issue283Host.StartAsync(true);
		var body = await Issue283Host.ReadAsync(app, "/issue-283/head", "partial");
		Assert.Contains("<title>Issue 283 PageTitle</title>", body, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("Toasts", "<aside>toast</aside>")]
	[InlineData("Toasts,Form", "<aside>toast</aside><div><input name=\"value\"></div>")]
	[InlineData("Form,Toasts", "<div><input name=\"value\"></div><aside>toast</aside>")]
	[InlineData("Form", "<div><input name=\"value\"></div>")]
	[InlineData("Envelope", "<section><p data-page-chrome>page chrome</p><div><input name=\"value\"></div></section>")]
	public async Task Selection_emits_only_requested_subtrees_in_caller_order(string selection, string expected)
	{
		await using var app = await Issue283Host.StartAsync(true);
		var body = await Issue283Host.ReadAsync(app, "/issue-283/selection?Selection=" + selection, "partial");
		Assert.Equal(expected, body);
	}

	[Theory]
	[InlineData("Envelope,Form")]
	[InlineData("Form,Envelope")]
	public async Task Selecting_layout_ancestor_and_page_descendant_fails_before_output(string selection)
	{
		await using var app = await Issue283Host.StartAsync(true);
		var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Issue283Host.ReadAsync(app, "/issue-283/selection?Selection=" + selection, "partial"));
		Assert.Contains("ancestor", error.Message, StringComparison.OrdinalIgnoreCase);
		Assert.False(app.Services.GetRequiredService<Issue283FailureProbe>().ResponseStarted);
	}

	[Fact]
	public async Task Duplicate_names_across_layout_and_page_fail_for_whole_output()
	{
		await using var app = await Issue283Host.StartAsync(true);
		var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Issue283Host.ReadAsync(app, "/issue-283/duplicate", "partial"));
		Assert.Contains("Toasts", error.Message, StringComparison.Ordinal);
		Assert.Contains("More than one fragment", error.Message, StringComparison.Ordinal);
		Assert.False(app.Services.GetRequiredService<Issue283FailureProbe>().ResponseStarted);
	}

	[Theory]
	[InlineData("razor", null)]
	[InlineData("razor", "boosted")]
	[InlineData("razor", "full")]
	[InlineData("codebehind", null)]
	[InlineData("codebehind", "boosted")]
	[InlineData("codebehind", "full")]
	[InlineData("csharp", null)]
	[InlineData("csharp", "boosted")]
	[InlineData("csharp", "full")]
	public async Task Ordinary_boosted_and_full_bytes_keep_stock_layout(string shape, string? representation)
	{
		await using var stock = await Issue283Host.StartAsync(false);
		await using var candidate = await Issue283Host.StartAsync(true);
		var path = "/issue-283/dual/" + shape;
		var expected = await Issue283Host.ReadBytesAsync(stock, path, representation);
		var actual = await Issue283Host.ReadBytesAsync(candidate, path, representation);
		// Captured with unchanged production at cd667c7ca887a66d0409ec006462b5f19dc69e0f; compare bytes without normalization.
		var baseline = System.Text.Encoding.UTF8.GetBytes($"<!DOCTYPE html><html><body><main data-stock-layout><p data-page>{shape}</p></main></body></html>");
		Assert.Equal(baseline, expected);
		Assert.Equal(baseline, actual);
	}

	[Fact]
	public async Task Disabled_direct_component_keeps_its_stock_only_route()
	{
		await using var app = await Issue283Host.StartAsync(true);
		using var client = app.GetTestClient();
		using var request = Issue283Host.Request("/issue-283/disabled", "partial");
		using var response = await client.SendAsync(request);
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		Assert.Contains("<main data-stock-layout>", await Issue283Host.ReadAsync(app, "/issue-283/disabled", null), StringComparison.Ordinal);
	}
}
