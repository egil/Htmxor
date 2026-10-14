using System.Text.RegularExpressions;
using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

[Collection(PackageConsumerCollection.Name)]
public sealed class PackedCompiledBindingConsumerTests
{
	[Fact]
	public async Task Action_for_the_request_component_does_not_cover_an_inherited_helper_receiver()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue310DerivedHelperReceiverScenario();
		var result = await workspace.BuildForDiagnosticAsync();
		var evidence = Path.Combine(RepositoryLocator.Find(), "artifacts", "results", "issue-310-receiver-context", workspace.PackageVersion);
		Directory.CreateDirectory(evidence);
		File.WriteAllText(Path.Combine(evidence, "build.log"), result.StandardOutput + Environment.NewLine + result.StandardError);
		File.WriteAllText(Path.Combine(evidence, "consumer-outcome.txt"), $"exit={result.ExitCode} assembly={File.Exists(workspace.ConsumerAssemblyPath)}");
		File.Copy(Path.Combine(workspace.ConsumerDirectory, "Components", "Pages", "Issue310Page.razor"), Path.Combine(evidence, "Issue310Page.razor"));
		File.Copy(Path.Combine(workspace.ConsumerDirectory, "Htmxor.PackageConsumer.csproj"), Path.Combine(evidence, "Htmxor.PackageConsumer.csproj"));
		if (File.Exists(workspace.ConsumerAssemblyPath))
		{
			File.Copy(workspace.ConsumerAssemblyPath, Path.Combine(evidence, "Htmxor.PackageConsumer.dll"));
		}
		foreach (var path in Directory.EnumerateFiles(Path.Combine(workspace.ConsumerDirectory, "obj", "generated"), "*.cs", SearchOption.AllDirectories))
		{
			File.Copy(path, Path.Combine(evidence, Path.GetFileName(path)), overwrite: true);
		}
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerCompileBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
		AssertRejectedBinding(workspace, result, "put", 10);
	}

	[Fact]
	public async Task Action_for_another_component_instance_does_not_cover_a_compiled_binding()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue310DifferentReceiverScenario();
		var result = await workspace.BuildForDiagnosticAsync();
		var evidence = Path.Combine(RepositoryLocator.Find(), "artifacts", "results", "issue-310-receiver", workspace.PackageVersion);
		Directory.CreateDirectory(evidence);
		File.WriteAllText(Path.Combine(evidence, "build.log"), result.StandardOutput + Environment.NewLine + result.StandardError);
		File.WriteAllText(Path.Combine(evidence, "consumer-outcome.txt"), $"exit={result.ExitCode} assembly={File.Exists(workspace.ConsumerAssemblyPath)}");
		File.Copy(Path.Combine(workspace.ConsumerDirectory, "Components", "Pages", "Issue310Page.razor"), Path.Combine(evidence, "Issue310Page.razor"));
		File.Copy(Path.Combine(workspace.ConsumerDirectory, "Htmxor.PackageConsumer.csproj"), Path.Combine(evidence, "Htmxor.PackageConsumer.csproj"));
		if (File.Exists(workspace.ConsumerAssemblyPath))
		{
			File.Copy(workspace.ConsumerAssemblyPath, Path.Combine(evidence, "Htmxor.PackageConsumer.dll"));
		}
		foreach (var path in Directory.EnumerateFiles(Path.Combine(workspace.ConsumerDirectory, "obj", "generated"), "*.cs", SearchOption.AllDirectories))
		{
			File.Copy(path, Path.Combine(evidence, Path.GetFileName(path)), overwrite: true);
		}
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerCompileBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
		AssertRejectedBinding(workspace, result, "put", 4);
	}

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

		AssertRejectedBinding(workspace, result, "put", 5, "HTMXOR034");
		var output = result.StandardOutput + Environment.NewLine + result.StandardError;
		var message = Assert.Single(Regex.Matches(output,
			@"Issue310Page\.razor\(5,\d+\): error HTMXOR034: (?<message>[^\r\n]+)")
			.Select(static match => match.Groups["message"].Value).Distinct());
		Assert.Contains("no usable action is available", message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("remove the binding or marker", message, StringComparison.Ordinal);
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
	public async Task Action_for_a_same_named_different_handler_does_not_cover_a_compiled_binding()
	{
		using var workspace = new PackageConsumerWorkspace(RepositoryLocator.Find());
		workspace.UseIssue310SameNameHandlerScenario();

		var result = await workspace.BuildForDiagnosticAsync();

		AssertRejectedBinding(workspace, result, "put", 5);
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
		Assert.DoesNotContain("HTMXOR", output, StringComparison.Ordinal);
		Assert.True(File.Exists(workspace.ConsumerAssemblyPath));
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerCompileBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
	}

	private static void AssertRejectedBinding(PackageConsumerWorkspace workspace, ProcessResult result, string method, int line, string expectedId = "HTMXOR042")
	{
		var output = result.StandardOutput + Environment.NewLine + result.StandardError;
		Assert.True(result.ExitCode != 0, "A compiled binding without its matching action built successfully." + Environment.NewLine + output);
		Assert.True(output.Contains(expectedId, StringComparison.Ordinal), output);
		Assert.Contains($"Issue310Page.razor({line},", output, StringComparison.Ordinal);
		AssertAuthoredBindingLocation(workspace, output, method, line, expectedId);
		Assert.Contains("@on" + method, output, StringComparison.Ordinal);
		Assert.False(File.Exists(workspace.ConsumerAssemblyPath));
		PackageConsumerEvidence.AssertPackage(workspace.PackagePath);
		PackageConsumerEvidence.AssertConsumerCompileBoundary(workspace.ConsumerDirectory, workspace.PackageVersion);
	}

	private static void AssertAuthoredBindingLocation(PackageConsumerWorkspace workspace, string output, string method, int line, string expectedId)
	{
		var source = File.ReadAllLines(Path.Combine(workspace.ConsumerDirectory, "Components", "Pages", "Issue310Page.razor"))[line - 1];
		var handlers = Regex.Matches(source, "@on" + method + "\\s*=\\s*\"(?<handler>[^\"]+)\"")
			.Select(static match => match.Groups["handler"]).ToArray();
		Assert.NotEmpty(handlers);
		var pattern = @"Issue310Page\.razor\(" + line + @",(?<column>\d+)\): error " + Regex.Escape(expectedId) + ":";
		var columns = Regex.Matches(output, pattern).Select(static match =>
			int.Parse(match.Groups["column"].Value, System.Globalization.CultureInfo.InvariantCulture)).Distinct().ToArray();
		Assert.NotEmpty(columns);
		Assert.All(columns, column => Assert.Contains(handlers, handler =>
			handler.Index + 1 <= column && column <= handler.Index + handler.Length));
	}
}

internal sealed partial class PackageConsumerWorkspace
{
	private string Issue310ComponentPath => Path.Combine(consumerDirectory, "Components", "Pages", "Issue310Page.razor");

	public void UseIssue310MissingActionScenario(string method)
	{
		UseSelectionScenario("Issue310");
		UseFramework("net10.0", "10.0.402", "10.0.13");
		Directory.CreateDirectory(Path.GetDirectoryName(Issue310ComponentPath)!);
		var source = Path.Combine(consumerDirectory, "Issue310Page.razor");
		File.WriteAllText(Issue310ComponentPath, File.ReadAllText(source).Replace("__METHOD__", method, StringComparison.Ordinal));
		File.Delete(source);
	}

	public void UseIssue310DerivedHelperReceiverScenario()
	{
		UseIssue310MissingActionScenario("put");
		var source = File.ReadAllText(Issue310ComponentPath)
			.Replace("<_element @onput=\"Save\">Save</_element>", "@((new Helper()).Template)", StringComparison.Ordinal)
			.Replace("private void Save() { }", """
				protected void Save() { }
				    private sealed class Helper : Issue310Page
				    {
				        public Microsoft.AspNetCore.Components.RenderFragment Template => @<_element @onput="Save">Save</_element>;
				    }
				""", StringComparison.Ordinal);
		File.WriteAllText(Issue310ComponentPath, source + "\n<button @onput=\"Save\">Save</button>\n");
	}

	public void UseIssue310DifferentReceiverScenario()
	{
		UseIssue310MissingActionScenario("put");
		var source = File.ReadAllText(Issue310ComponentPath)
			.Replace("<_element @onput=\"Save\">Save</_element>", "<_element @onput=\"other.Save\">Save</_element>", StringComparison.Ordinal)
			.Replace("private void Save() { }", "private static readonly Issue310Page other = new();\n    private void Save() { }", StringComparison.Ordinal);
		File.WriteAllText(Issue310ComponentPath, source + "\n<button @onput=\"Save\">Save</button>\n");
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

	public void UseIssue310SameNameHandlerScenario()
	{
		UseIssue310MissingActionScenario("put");
		var source = File.ReadAllText(Issue310ComponentPath)
			.Replace("@namespace Htmxor.PackageConsumer", "@namespace Htmxor.PackageConsumer\n@inherits GateBase", StringComparison.Ordinal)
			.Replace("<_element @onput=\"Save\">Save</_element>", "<_element @onput=\"base.Save\">Save</_element>", StringComparison.Ordinal)
			.Replace("private void Save() { }", "private new void Save() { }", StringComparison.Ordinal);
		File.WriteAllText(Issue310ComponentPath, source + "\n<button @onput=\"Save\">Save</button>\n");
		File.WriteAllText(Path.Combine(consumerDirectory, "GateBase.cs"), """
			using Microsoft.AspNetCore.Components;

			namespace Htmxor.PackageConsumer;
			public class GateBase : ComponentBase
			{
			    protected void Save() { }
			}
			""");
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
