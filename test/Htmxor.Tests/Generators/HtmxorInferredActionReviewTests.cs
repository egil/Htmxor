using Microsoft.CodeAnalysis;

namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorInferredActionDiagnosticsTests
{
	[Theory]
	[InlineData("ReviewUnrouted", "014:1:16:17", false)]
	[InlineData("ReviewUnrouted", "014:1:16:17", true)]
	[InlineData("ReviewUnroutedNormal", "014:1:16:17,034:1:16:17", false)]
	[InlineData("ReviewUnroutedNormal", "014:1:16:17,034:1:16:17", true)]
	[InlineData("ReviewAllowlist", "033:1:16:17", false)]
	[InlineData("ReviewAllowlist", "033:1:16:17", true)]
	public async Task Compiler_owned_handler_failure_preserves_independent_owner_feedback(string fixture, string expected, bool local)
	{
		var result = await AnalyzeAsync(fixture, compilerError: "CS1503");

		AssertDiagnostics(fixture, expected, local ? result.Local : result.Complete);
		Assert.All(result.Local, diagnostic =>
		{
			Assert.NotNull(diagnostic.Location.SourceTree);
			Assert.DoesNotContain(WellKnownDiagnosticTags.CompilationEnd, diagnostic.Descriptor.CustomTags);
		});
	}

	[Theory]
	[InlineData("Parse", "015:1:8:14,037:1:8:14", false)]
	[InlineData("Parse", "015:1:8:14,037:1:8:14", true)]
	[InlineData("Conflict", "031:1:8:14,031:2:8:14,037:1:8:14,037:2:8:14", false)]
	[InlineData("Conflict", "031:1:8:14,031:2:8:14,037:1:8:14,037:2:8:14", true)]
	public async Task Rejected_declaration_preserves_independent_owner_mismatch(string fixture, string expected, bool local)
	{
		var result = await AnalyzeAsync(fixture, ownerMismatch: true);
		if (local)
		{
			Assert.Empty(result.Local);
			return;
		}

		AssertDiagnostics(fixture, expected, result.Complete);
		Assert.Empty(result.Local);
		Assert.All(result.Complete.Where(diagnostic => diagnostic.Id == "HTMXOR037"), diagnostic =>
			Assert.Contains(WellKnownDiagnosticTags.CompilationEnd, diagnostic.Descriptor.CustomTags));
	}

	[Theory]
	[InlineData("Compiler")]
	[InlineData("ReviewIncidental")]
	public async Task Compiler_failure_without_an_independent_owner_fault_stays_silent(string fixture)
	{
		var result = await AnalyzeAsync(fixture, compilerError: "CS1503");

		Assert.Empty(result.Complete);
		Assert.Empty(result.Local);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Repeated_invalid_bindings_preserve_each_independent_cause_location(bool local)
	{
		var result = await AnalyzeAsync("ReviewRepeatedShape");

		AssertDiagnostics("ReviewRepeatedShape",
			"013:1:16:17,013:2:16:17,013:3:16:17,013:4:16:17,018:1:16:17,018:2:16:17,018:3:16:17,018:4:16:17",
			local ? result.Local : result.Complete);
	}

	[Fact]
	public async Task Repeated_valid_bindings_to_one_handler_keep_the_generated_action()
	{
		var result = await AnalyzeAsync("ReviewRepeated");

		Assert.Empty(result.Complete);
		Assert.Empty(result.Local);
		Assert.Contains("\"PUT\"", result.Generated, StringComparison.Ordinal);
	}
}
