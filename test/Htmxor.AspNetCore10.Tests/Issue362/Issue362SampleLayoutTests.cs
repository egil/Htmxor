using Htmxor.AspNetCore10;
using Xunit;

namespace Htmxor.AspNetCore10.Issue362;

public sealed class Issue362SampleLayoutTests
{
	[Fact]
	public async Task Minimal_sample_Home_direct_response_contains_its_inherited_layout_marker()
	{
		await using var app = await Issue283Host.StartAsync(true);
		var body = await Issue283Host.ReadAsync(app, "/issue-362-283/minimal-home", "partial");
		Assert.Multiple(
			() => Assert.Contains("Hello, world!", body, StringComparison.Ordinal),
			() => Assert.Contains("data-minimal-layout-marker", body, StringComparison.Ordinal));
	}

	[Fact]
	public async Task Minimal_sample_Home_direct_response_contains_PageTitle()
	{
		await using var app = await Issue283Host.StartAsync(true);
		var body = await Issue283Host.ReadAsync(app, "/issue-362-283/minimal-home", "partial");
		Assert.Multiple(
			() => Assert.Contains("Hello, world!", body, StringComparison.Ordinal),
			() => Assert.Contains("<title>Home</title>", body, StringComparison.Ordinal));
	}

}
