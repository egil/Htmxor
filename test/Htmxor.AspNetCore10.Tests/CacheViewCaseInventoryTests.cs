#if NET11_0_OR_GREATER
using System.Reflection;

namespace Htmxor.AspNetCore10;

// The #219 case inventory in docs/engineering/candidate-form-adapter.md went stale in four separate rounds,
// every time because the reconciliation the document asks for was performed by reading rather than by
// counting. This pins that reconciliation so it cannot be skipped: the document names the cases that run
// against the candidate alone and states how many of the rest are paired, and those two facts must still
// account for every Issue219 case the suite discovers.
//
// Deliberately not named Issue219* so that it does not itself answer the
// --filter "FullyQualifiedName~Issue219" the document tells a reader to run.
//
// It does not decide which cases are paired. That classification needs the host each case builds, reached
// through shared helpers and sometimes positional arguments, and a wrong answer would be worse than none.
// The reconciliation catches what actually recurred: a listed case that no longer exists under that name,
// and a total that moved while the document did not.
public sealed class CacheViewCaseInventoryTests
{
	private const string InventoryHeading = "The cases listed below run against the candidate alone.";

	[Fact]
	public void Every_case_the_adapter_document_lists_still_exists_under_that_name()
	{
		var listed = ReadListedCandidateOnlyCases();
		var discovered = DiscoverIssue219Cases();

		Assert.NotEmpty(listed);
		Assert.Empty(listed.Except(discovered, StringComparer.Ordinal));
	}

	[Fact]
	public void The_listed_and_paired_counts_account_for_every_discovered_case()
	{
		var listed = ReadListedCandidateOnlyCases();
		var discovered = DiscoverIssue219Cases();

		Assert.Equal(discovered.Count, listed.Count + ReadStatedPairedCount());
	}

	private static IReadOnlyCollection<string> DiscoverIssue219Cases() =>
		typeof(CacheViewCaseInventoryTests).Assembly.GetTypes()
			.Where(type => type.Name.StartsWith("Issue219", StringComparison.Ordinal))
			.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
			.Where(method => method.GetCustomAttributes().Any(attribute =>
				attribute.GetType().Name is "FactAttribute" or "TheoryAttribute"))
			.Select(method => method.Name).ToHashSet(StringComparer.Ordinal);

	private static IReadOnlyList<string> ReadListedCandidateOnlyCases()
	{
		var lines = ReadAdapterDocument();
		var start = Array.FindIndex(lines, line => line.Contains(InventoryHeading, StringComparison.Ordinal));
		Assert.True(start >= 0, $"The adapter document must still introduce the inventory with: {InventoryHeading}");

		return lines.Skip(start).SkipWhile(line => !line.StartsWith("- `", StringComparison.Ordinal))
			.TakeWhile(line => line.StartsWith("- `", StringComparison.Ordinal))
			.Select(line => line.Trim('-', ' ', '`')).ToArray();
	}

	private static int ReadStatedPairedCount()
	{
		const string marker = "Paired cases in the same suite: ";
		var line = Array.Find(ReadAdapterDocument(), candidate => candidate.StartsWith(marker, StringComparison.Ordinal));
		Assert.True(line is not null, $"The adapter document must state the paired count on a line beginning: {marker}");

		return int.Parse(line![marker.Length..].TrimEnd('.'), System.Globalization.CultureInfo.InvariantCulture);
	}

	private static string[] ReadAdapterDocument()
	{
		var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
		var path = Path.Combine(root, "docs", "engineering", "candidate-form-adapter.md");

		Assert.True(File.Exists(path), "The candidate adapter document must be committed.");
		return File.ReadAllLines(path);
	}
}
#endif
