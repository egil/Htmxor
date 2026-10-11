using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedCompiledBindingConsumerTests
{
	[Theory]
	[InlineData("post")]
	[InlineData("put")]
	[InlineData("patch")]
	[InlineData("delete")]
	[InlineData("query")]
	[InlineData("get")]
	public async Task Compiled_binding_omitted_by_the_scanner_fails_the_package_consumer_build(string method)
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue310MissingActionScenario(method);

		var result = await workspace.BuildForDiagnosticAsync();

		AssertRejectedBinding(workspace, result, method, 4);
	}

	[Fact]
	public async Task Normal_only_component_cannot_hide_a_compiled_binding_from_the_gate()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue310NormalOnlyBindingScenario();

		var result = await workspace.BuildForDiagnosticAsync();

		AssertRejectedBinding(workspace, result, "put", 5);
	}

	[Fact]
	public async Task Action_for_another_handler_does_not_cover_a_compiled_binding_for_the_same_method()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue310ConflictingBindingScenario();

		var result = await workspace.BuildForDiagnosticAsync();

		AssertRejectedBinding(workspace, result, "put", 4);
	}

	[Fact]
	public async Task Compiled_binding_inside_script_markup_cannot_be_silently_omitted()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue310ScriptBindingScenario();

		var result = await workspace.BuildForDiagnosticAsync();

		AssertRejectedBinding(workspace, result, "put", 4);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Compiled_component_tag_binding_cannot_be_silently_omitted(bool generic)
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue310ComponentBindingScenario(generic);

		var result = await workspace.BuildForDiagnosticAsync();

		AssertRejectedBinding(workspace, result, "put", 4);
	}

	private static void AssertRejectedBinding(PackageConsumerWorkspace workspace, ProcessResult result, string method, int line)
	{
		var output = result.StandardOutput + Environment.NewLine + result.StandardError;
		Assert.True(result.ExitCode != 0, "A compiled binding without its matching action built successfully." + Environment.NewLine + output);
		Assert.Contains("HTMXOR002", output, StringComparison.Ordinal);
		Assert.Contains($"Issue310Page.razor({line},", output, StringComparison.Ordinal);
		Assert.Contains("@on" + method, output, StringComparison.Ordinal);
		Assert.False(File.Exists(workspace.ConsumerAssemblyPath));
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
	}
}

internal sealed partial class PackageConsumerWorkspace
{
	private string Issue310ComponentPath => Path.Combine(consumerDirectory, "Components", "Pages", "Issue310Page.razor");

	public void UseIssue310MissingActionScenario(string method)
	{
		UseSelectionScenario("Issue310");
		UseFramework("net10.0", "10.0.400", "10.0.11");
		Directory.CreateDirectory(Path.GetDirectoryName(Issue310ComponentPath)!);
		var source = Path.Combine(consumerDirectory, "Issue310Page.razor");
		File.WriteAllText(Issue310ComponentPath, File.ReadAllText(source).Replace("__METHOD__", method, StringComparison.Ordinal));
		File.Delete(source);
	}

	public void UseIssue310NormalOnlyBindingScenario()
	{
		UseIssue310MissingActionScenario("put");
		File.WriteAllText(Issue310ComponentPath, File.ReadAllText(Issue310ComponentPath).Replace(
			"@namespace Htmxor.PackageConsumer", "@namespace Htmxor.PackageConsumer\n@attribute [Htmxor.DisableHtmxDirectRouting]", StringComparison.Ordinal));
	}

	public void UseIssue310ConflictingBindingScenario()
	{
		UseIssue310MissingActionScenario("put");
		File.AppendAllText(Issue310ComponentPath, "\n<button @onput=\"Other\">Other</button>\n");
	}

	public void UseIssue310ScriptBindingScenario()
	{
		UseIssue310MissingActionScenario("put");
		File.WriteAllText(Issue310ComponentPath, File.ReadAllText(Issue310ComponentPath).Replace(
			"<_element @onput=\"Save\">Save</_element>",
			"<script type=\"text/html\"><button @onput=\"Save\">Save</button></script>", StringComparison.Ordinal));
	}

	public void UseIssue310ComponentBindingScenario(bool generic)
	{
		UseIssue310MissingActionScenario("put");
		var tag = generic ? "<GateChild Value=\"@(42)\" @onput=\"Save\" />" : "<GateChild @onput=\"Save\" />";
		File.WriteAllText(Issue310ComponentPath, File.ReadAllText(Issue310ComponentPath).Replace(
			"<_element @onput=\"Save\">Save</_element>", "<script type=\"text/html\">" + tag + "</script>", StringComparison.Ordinal));
		var type = generic ? "GateChild<T>" : "GateChild";
		var value = generic ? "[Parameter] public T Value { get; set; } = default!;" : string.Empty;
		File.WriteAllText(Path.Combine(consumerDirectory, "GateChild.cs"), $$"""
			using Microsoft.AspNetCore.Components;
			namespace Htmxor.PackageConsumer;
			public sealed class {{type}} : ComponentBase
			{
			    [Parameter] public EventCallback<Htmxor.HtmxEventArgs> onput { get; set; }
			    {{value}}
			}
			""");
	}
}
