using System.Globalization;
using System.Text.RegularExpressions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit.Abstractions;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorDiagnosticHelpTests(ITestOutputHelper output)
{
	private const string DocumentationUrl = "https://github.com/egil/Htmxor/blob/main/docs/diagnostics.md";

	[Fact]
	public void Every_active_descriptor_exposes_its_stable_documentation_link()
	{
		var analyzers = DiscoverAnalyzers();
		var descriptors = analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics).ToArray();
		var ids = descriptors.Select(descriptor => descriptor.Id).Distinct(StringComparer.Ordinal).ToArray();
		var releaseInventory = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Htmxor.Generators", "AnalyzerReleases.Unshipped.md"));
		var releaseIds = Regex.Matches(releaseInventory, @"(?m)^(HTMXOR[0-9]{3})[ \t]*\|")
			.Select(match => match.Groups[1].Value).ToArray();
		output.WriteLine($"Discovered {analyzers.Length} analyzers, {descriptors.Length} descriptor entries, {ids.Length} unique IDs, {releaseIds.Length} release rows.");

		using var scope = new AssertionScope();
		analyzers.Should().NotBeEmpty();
		ids.Should().BeEquivalentTo(ActiveIds());
		releaseIds.Should().OnlyHaveUniqueItems().And.BeEquivalentTo(ids);
		foreach (var analyzer in analyzers)
		{
			analyzer.SupportedDiagnostics.Should().NotBeEmpty("{0} must expose its supported diagnostics", analyzer.GetType().Name);
			foreach (var descriptor in analyzer.SupportedDiagnostics)
			{
				descriptor.HelpLinkUri.Should().Be(DocumentationUrl + "#" + descriptor.Id.ToLowerInvariant(),
					"every descriptor entry must link to its own help, including {0} on {1}", descriptor.Id, analyzer.GetType().Name);
			}
		}
	}

	[Fact]
	public void Diagnostic_help_page_has_every_active_anchor_and_is_linked_from_the_documentation_entry()
	{
		var root = RepositoryRoot();
		var pagePath = Path.Combine(root, "docs", "diagnostics.md");
		File.Exists(pagePath).Should().BeTrue("diagnostic help must resolve to a committed repository Markdown page");
		var page = File.ReadAllText(pagePath);
		var anchors = Regex.Matches(page, @"(?m)^#{1,6}[ \t]+(HTMXOR[0-9]{3})[ \t]*#*[ \t]*\r?$")
			.Select(match => match.Groups[1].Value.ToLowerInvariant()).ToArray();
		var ids = DiscoverAnalyzers().SelectMany(analyzer => analyzer.SupportedDiagnostics)
			.Select(descriptor => descriptor.Id).Distinct(StringComparer.Ordinal).ToArray();
		var entry = File.ReadAllText(Path.Combine(root, "docs", "index.md"));
		var links = Regex.Matches(entry, @"\[[^\]\r\n]+\]\(([^)\s]+)\)")
			.Select(match => match.Groups[1].Value).ToArray();

		using var scope = new AssertionScope();
		anchors.Should().OnlyHaveUniqueItems().And.BeEquivalentTo(ids.Select(id => id.ToLowerInvariant()),
			"each active ID needs its own stable Markdown heading anchor, and retired IDs are not active");
		links.Should().Contain(link => link == "diagnostics.md" || link == "./diagnostics.md" || link == DocumentationUrl,
			"the canonical diagnostic help page must be reachable from docs/index.md");
	}

	private static DiagnosticAnalyzer[] DiscoverAnalyzers()
		=> typeof(HtmxorRouteDeclarationAnalyzer).Assembly.GetTypes()
			.Where(type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
			.OrderBy(type => type.FullName, StringComparer.Ordinal)
			.Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
			.ToArray();

	private static string[] ActiveIds()
		=> Enumerable.Range(3, 41).Select(number => "HTMXOR" + number.ToString("000", CultureInfo.InvariantCulture)).ToArray();

	private static string RepositoryRoot()
	{
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null && !File.Exists(Path.Combine(current.FullName, "Htmxor.sln")))
		{
			current = current.Parent;
		}
		return current?.FullName
			?? throw new InvalidOperationException("Could not locate Htmxor.sln from the test runtime directory.");
	}
}
