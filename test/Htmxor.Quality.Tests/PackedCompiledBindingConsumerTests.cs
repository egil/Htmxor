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

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Supported_component_tag_binding_reaches_the_request_owner_callback(bool generic)
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue310SupportedComponentBindingScenario(generic);

		var result = await workspace.RunAsync();
		var tests = TrxTestRun.Read(workspace.TrxPath);
		var evidence = Path.Combine(RepositoryLocator.Find(), "artifacts", "results", "issue-310-supported", generic ? "generic" : "direct");
		Directory.CreateDirectory(evidence);
		File.Copy(workspace.TrxPath, Path.Combine(evidence, "package-consumer.trx"), overwrite: true);
		File.WriteAllText(Path.Combine(evidence, "test.log"), result.StandardOutput + Environment.NewLine + result.StandardError);

		Assert.True(result.ExitCode == 0, result.StandardOutput + Environment.NewLine + result.StandardError);
		Assert.Equal(new TrxTestRun(1, 1, 1, 0, 0, 0, 0), tests);
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerPackageBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
	}

	[Fact]
	public async Task Compiled_member_template_binding_cannot_be_silently_omitted()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue310TemplateBindingScenario();

		var result = await workspace.BuildForDiagnosticAsync();

		AssertRejectedBinding(workspace, result, "put", 8);
	}

	[Fact]
	public async Task Compiled_nested_helper_template_binding_cannot_be_silently_omitted()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue310NestedTemplateBindingScenario();
		var result = await workspace.BuildForDiagnosticAsync();
		var evidence = Path.Combine(RepositoryLocator.Find(), "artifacts", "results", "issue-310-nested-template");
		Directory.CreateDirectory(evidence);
		File.WriteAllText(Path.Combine(evidence, "build.log"), result.StandardOutput + Environment.NewLine + result.StandardError);
		foreach (var path in Directory.EnumerateFiles(Path.Combine(workspace.ConsumerDirectory, "obj", "generated"), "*Issue310Page_razor.g.cs", SearchOption.AllDirectories))
		{
			File.Copy(path, Path.Combine(evidence, Path.GetFileName(path)), overwrite: true);
		}
		AssertRejectedBinding(workspace, result, "put", 12);
	}

	[Fact]
	public async Task Handwritten_member_template_callback_stays_outside_the_compiled_Razor_gate()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue310HandwrittenTemplateScenario();

		var result = await workspace.BuildForDiagnosticAsync();
		var output = result.StandardOutput + Environment.NewLine + result.StandardError;
		var evidence = Path.Combine(RepositoryLocator.Find(), "artifacts", "results", "issue-310-handwritten");
		Directory.CreateDirectory(evidence);
		File.WriteAllText(Path.Combine(evidence, "build.log"), output);
		foreach (var path in Directory.EnumerateFiles(Path.Combine(workspace.ConsumerDirectory, "obj", "generated"), "*Issue310Page_razor.g.cs", SearchOption.AllDirectories))
		{
			File.Copy(path, Path.Combine(evidence, Path.GetFileName(path)), overwrite: true);
		}

		Assert.True(result.ExitCode == 0, output);
		Assert.DoesNotContain("HTMXOR002", output, StringComparison.Ordinal);
		Assert.True(File.Exists(workspace.ConsumerAssemblyPath));
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerCompileBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
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
		PackageConsumerEvidence.AssertConsumerCompileBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
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

	public void UseIssue310SupportedComponentBindingScenario(bool generic)
	{
		UseIssue310ComponentBindingScenario(generic);
		var source = File.ReadAllText(Issue310ComponentPath)
			.Replace("<script type=\"text/html\">", string.Empty, StringComparison.Ordinal)
			.Replace("</script>", string.Empty, StringComparison.Ordinal)
			.Replace("@code {", "<p data-owner-callback=\"@saveCount\">Saved</p>\n\n@code {", StringComparison.Ordinal)
			.Replace("private void Save() { }", "private int saveCount;\n    private void Save() { saveCount++; }", StringComparison.Ordinal);
		File.WriteAllText(Issue310ComponentPath, source);
	}

	public void UseIssue310TemplateBindingScenario()
	{
		UseIssue310MissingActionScenario("put");
		var source = File.ReadAllText(Issue310ComponentPath)
			.Replace("<_element @onput=\"Save\">Save</_element>", "@Template", StringComparison.Ordinal)
			.Replace("private void Save() { }", "private void Save() { }\n    private Microsoft.AspNetCore.Components.RenderFragment Template => @<_element @onput=\"Save\">Save</_element>;", StringComparison.Ordinal);
		File.WriteAllText(Issue310ComponentPath, source);
	}

	public void UseIssue310NestedTemplateBindingScenario()
	{
		UseIssue310MissingActionScenario("put");
		var source = File.ReadAllText(Issue310ComponentPath)
			.Replace("<_element @onput=\"Save\">Save</_element>", "@Template", StringComparison.Ordinal)
			.Replace("private void Save() { }", """
				private Helper helper = new();
				    private Microsoft.AspNetCore.Components.RenderFragment Template => helper.Template;
				    private sealed class Helper
				    {
				        private void Save() { }
				        public Microsoft.AspNetCore.Components.RenderFragment Template => @<_element @onput="Save">Save</_element>;
				    }
				""", StringComparison.Ordinal);
		File.WriteAllText(Issue310ComponentPath, source);
	}

	public void UseIssue310HandwrittenTemplateScenario()
	{
		UseIssue310MissingActionScenario("put");
		var source = File.ReadAllText(Issue310ComponentPath)
			.Replace("<_element @onput=\"Save\">Save</_element>", "@Template", StringComparison.Ordinal)
			.Replace("private void Save() { }", """
				private void Save() { }
				    private Microsoft.AspNetCore.Components.RenderFragment Template => builder =>
				    {
				        builder.OpenElement(0, "button");
				        builder.AddAttribute(1, "onput", Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Htmxor.HtmxEventArgs>(this, Save));
				        builder.CloseElement();
				    };
				""", StringComparison.Ordinal);
		File.WriteAllText(Issue310ComponentPath, source);
	}
}
