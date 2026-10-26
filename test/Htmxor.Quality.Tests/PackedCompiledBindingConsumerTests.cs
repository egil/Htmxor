using Htmxor.Quality;

namespace Htmxor.Quality.Tests;

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
