using System.Collections.Immutable;

namespace Htmxor.Quality.Tests;

[Collection(PackedConsumerCollection.Name)]
public sealed class PackedDiagnosticConsumerTests(PackedPackageFixture package)
{
	public static IEnumerable<object[]> Cases =>
		from framework in new[] { "net10.0", "net11.0" }
		from item in PackedDiagnosticCases.All
		select new object[] { framework, item.Name };

	[Theory]
	[MemberData(nameof(Cases))]
	public async Task Packed_compilation_preserves_each_authored_diagnostic(string framework, string name)
	{
		var result = await package.DiagnosticsAsync(framework, name);
		AssertCase(PackedDiagnosticCases.All.Single(item => item.Name == name), result);
	}

	internal static void AssertCase(PackedDiagnosticCase item, PackedDiagnosticResult result)
	{
		Assert.NotEqual(0, result.ExitCode);
		Assert.False(result.AssemblyExists);
		var diagnostics = result.ForCase(item.Name);
		foreach (var forbidden in item.ForbiddenIds.IsDefault ? [] : item.ForbiddenIds)
			Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == forbidden);
		var expected = diagnostics.Where(diagnostic => item.Ids.Split('|').Contains(diagnostic.Id, StringComparer.Ordinal)).ToImmutableArray();
		Assert.NotEmpty(expected);
		Assert.All(expected, diagnostic => Assert.True(diagnostic.File.Replace('\\', '/').EndsWith("/" + item.Source, StringComparison.Ordinal), diagnostic.File));
		AssertLocations(item, result.Sources[item.Name], expected);
		AssertMessages(item, expected);
		if (item.Name == "csharp-methods")
		{
			Assert.Contains("\"" + item.Namespace + ".Issue97ReportComponent\"", result.Registration, StringComparison.Ordinal);
			Assert.DoesNotContain("\"" + item.Namespace + ".Issue97SummaryComponent\"", result.Registration, StringComparison.Ordinal);
		}
	}

	private static void AssertLocations(PackedDiagnosticCase item, string source, ImmutableArray<ConsumerDiagnostic> diagnostics)
	{
		var locations = item.Locations(source);
		if (item.BindingLine != 0 || item.Name.StartsWith("explicit-", StringComparison.Ordinal))
		{
			Assert.NotEmpty(locations);
			Assert.All(diagnostics, diagnostic => Assert.Contains(locations, location =>
				Contains(location, diagnostic)));
		}
		if (item.Name == "stock-form")
			Assert.All(diagnostics, diagnostic => Assert.Equal(3, diagnostic.Line));
		foreach (var line in RequiredLines(item.Name))
			Assert.Contains(diagnostics, diagnostic => diagnostic.Line == line);
	}

	private static bool Contains(DiagnosticLocation location, ConsumerDiagnostic diagnostic) =>
		location.Line == diagnostic.Line && location.Column <= diagnostic.Column && diagnostic.Column <= location.EndColumn;

	private static int[] RequiredLines(string name) => name switch
	{
		"wrappers" => [8, 9, 10, 11],
		"retired-api" => [5, 6, 7, 8],
		_ => [],
	};

	private static void AssertForbiddenText(PackedDiagnosticCase item, string message)
	{
		foreach (var forbidden in item.ForbiddenText.IsDefault ? [] : item.ForbiddenText)
			Assert.DoesNotContain(forbidden, message, StringComparison.OrdinalIgnoreCase);
	}

	private static void AssertMessages(PackedDiagnosticCase item, ImmutableArray<ConsumerDiagnostic> diagnostics)
	{
		var comparison = item.Name.StartsWith("explicit-", StringComparison.Ordinal) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		var messages = string.Join(Environment.NewLine, diagnostics.Select(diagnostic => diagnostic.Message));
		foreach (var text in item.Text)
			Assert.Contains(text, messages, comparison);
		Assert.All(diagnostics, diagnostic => AssertForbiddenText(item, diagnostic.Message));
		if (item.Name.StartsWith("missing-", StringComparison.Ordinal) || item.Name == "normal-binding")
			Assert.Single(diagnostics.Select(diagnostic => diagnostic.Message).Distinct());
	}
}
