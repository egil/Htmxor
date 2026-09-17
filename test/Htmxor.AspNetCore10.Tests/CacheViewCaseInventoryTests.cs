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

	private static IReadOnlyCollection<string> DiscoverIssue219Cases()
	{
		var methods = typeof(CacheViewCaseInventoryTests).Assembly.GetTypes()
			.Where(type => type.Name.StartsWith("Issue219", StringComparison.Ordinal))
			.SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
			.Where(method => method.GetCustomAttributes().Any(attribute =>
				attribute.GetType().Name is "FactAttribute" or "TheoryAttribute")).ToArray();

		// One method is one discovered case only while every case is a [Fact]. A [Theory] discovers one case
		// per data row, so counting methods would understate the total in exactly the direction this check
		// exists to catch: a case could be added and absorbed by the stated paired count without reddening.
		// Fail loudly on the first one rather than silently miscount.
		var theories = methods.Where(method => method.GetCustomAttributes()
			.Any(attribute => attribute.GetType().Name == "TheoryAttribute")).Select(method => method.Name).ToArray();

		Assert.True(theories.Length == 0,
			$"This reconciliation counts one discovered case per method, which a [Theory] breaks: it discovers one "
			+ $"per data row. Convert it, or teach this check to count rows, before relying on the totals in "
			+ $"docs/engineering/candidate-form-adapter.md. Found: {string.Join(", ", theories)}");

		return methods.Select(method => method.Name).ToHashSet(StringComparer.Ordinal);
	}

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
