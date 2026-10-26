using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Htmxor.Quality.Tests;

internal sealed record DiagnosticLocation(string File, int Line, int Column, int EndColumn);

internal sealed record PackedDiagnosticCase(
	string Name, Action<PackageConsumerWorkspace> Stage, string Source, string Ids,
	ImmutableArray<string> Text, int BindingLine = 0, string Method = "",
	ImmutableArray<string> ForbiddenIds = default, ImmutableArray<string> ForbiddenText = default)
{
	public string Namespace => "Htmxor.Diagnostics.Case" + Name.Replace("-", "", StringComparison.Ordinal);

	public ImmutableArray<DiagnosticLocation> Locations(string source)
	{
		if (BindingLine != 0)
			return BindingLocations(source);
		if (Name.StartsWith("explicit-", StringComparison.Ordinal))
			return DeclarationLocations(source);
		return [];
	}

	private ImmutableArray<DiagnosticLocation> BindingLocations(string source) => Regex.Matches(
		source.Split('\n')[BindingLine - 1], "@on" + Method + "\\s*=\\s*\"(?<handler>[^\"]+)\"")
		.Select(match => new DiagnosticLocation(Source, BindingLine, match.Groups["handler"].Index + 1,
			match.Groups["handler"].Index + match.Groups["handler"].Length)).ToImmutableArray();

	private ImmutableArray<DiagnosticLocation> DeclarationLocations(string source)
	{
		if (Name == "explicit-no-route")
			return [new(Source, 5, 17, 17)];
		var anchors = Name switch
		{
			"explicit-dynamic" or "explicit-method-call" or "explicit-computed" => new[] { "@onput" },
			"explicit-implicit-get" => ["@onget"],
			"explicit-cross-source" => ["[Htmxor.HtmxAction", "@onput"],
			"explicit-stock-form" or "explicit-htmx-form" => ["[Htmxor.HtmxAction", "<form", "@formname"],
			_ => ["[Htmxor.HtmxAction"],
		};
		return source.Split('\n').SelectMany((line, index) => Anchors(line, index + 1, anchors)).ToImmutableArray();
	}

	private IEnumerable<DiagnosticLocation> Anchors(string line, int number, string[] anchors)
	{
		foreach (var anchor in anchors)
		{
			var start = line.IndexOf(anchor, StringComparison.Ordinal);
			if (start < 0)
				continue;
			var end = anchor.StartsWith("[", StringComparison.Ordinal) ? line.IndexOf(']', start) + 1 : start + anchor.Length;
			yield return new(Source, number, start + 1, end);
		}
	}
}

internal static class PackedDiagnosticCases
{
	public static ImmutableArray<PackedDiagnosticCase> All { get; } = [.. PackageCases(), .. ExplicitCases(), .. BindingCases(), .. OtherCases()];

	private static IEnumerable<PackedDiagnosticCase> PackageCases()
	{
		yield return new("two-routes", w => w.UseSecondCompiledPageRoute(), "Issue100ReportPage.razor", "HTMXOR038", ["exactly one stock route and no HtmxRoute"]);
		yield return new("lambda", w => w.UseComputedPutHandler(), "Issue100ReportPage.razor", "HTMXOR006", ["lambda or closure"]);
		yield return new("imports", w => w.UseDisableHtmxDirectRoutingInImports(), "_Imports.razor", "HTMXOR003", ["DisableHtmxDirectRouting declarations from _Imports.razor are not supported"]);
		yield return new("methods", w => w.UseExplicitMethodsConflict(), "Issue97ReportComponent.razor", "HTMXOR033", ["explicit HtmxRoute.Methods is authoritative"]);
		yield return new("csharp-methods", w => w.UseAllCSharpRouteWithoutMethods(), "Issue97SummaryComponent.cs", "HTMXOR005", ["a C# HtmxRoute declaration must explicitly declare HtmxRoute.Methods"]);
		yield return new("partial", w => w.UseNonmatchingExplicitRoutePartial(), "Issue97ReportRoute.cs", "HTMXOR004", ["a C# HtmxRoute declaration on a Razor component must use the matching .razor.cs partial"]);
	}

	private static IEnumerable<PackedDiagnosticCase> ExplicitCases()
	{
		(string Scenario, string Method, string Text, string Id)[] cases =
		[
			("invalid-method", "GET", "method", "HTMXOR029"), ("normal-only", "PUT", "DisableHtmxDirectRouting", "HTMXOR034"),
			("base-owner", "PUT", "route", "HTMXOR030"), ("stock-form", "POST", "form", "HTMXOR032"),
			("htmx-form", "POST", "form", "HTMXOR032"), ("cross-source", "PUT", "handler", "HTMXOR031"),
			("allowlist", "PUT", "Methods", "HTMXOR033"), ("dynamic", "PUT", "HtmxAction", "HTMXOR006"),
			("method-call", "PUT", "HtmxAction", "HTMXOR009"), ("computed", "PUT", "HtmxAction", "HTMXOR010"),
			("implicit-get", "GET", "HtmxAction", "HTMXOR015"), ("no-route", "PUT", "HtmxAction", "HTMXOR014"),
		];
		foreach (var item in cases)
			yield return new("explicit-" + item.Scenario, w => w.UseIssue180RejectedDeclaration(item.Scenario, item.Method),
				item.Scenario == "base-owner" ? "Issue180RejectedBase.cs" : "Issue180Rejected.razor", item.Id, [item.Text]);
	}

	private static PackedDiagnosticCase Binding(string name, Action<PackageConsumerWorkspace> stage, int line = 4,
		string id = "HTMXOR042", ImmutableArray<string> text = default, ImmutableArray<string> forbidden = default) =>
		new(name, stage, "Components/Pages/Issue310Page.razor", id, text.IsDefault ? ["@onput"] : text, line, "put", forbidden);

	private static IEnumerable<PackedDiagnosticCase> BindingCases()
	{
		yield return Binding("derived-receiver", w => w.UseIssue310DerivedHelperReceiverScenario(), 10);
		yield return Binding("different-receiver", w => w.UseIssue310DifferentReceiverScenario(), id: "HTMXOR010", text: ["@onput", "computed expression"], forbidden: ["HTMXOR042"]);
		foreach (var method in new[] { "post", "put", "patch", "delete", "query", "get" })
		{
			var implicitGet = method == "get";
			yield return new("missing-" + method, w => w.UseIssue310MissingActionScenario(method), "Components/Pages/Issue310Page.razor",
				implicitGet ? "HTMXOR015" : "HTMXOR042",
				implicitGet ? ["@onget", "no usable action is available", "remove @onget", "GET is implicit"] : ["@on" + method, "no usable action is available", "HtmxAction"],
				4, method, ForbiddenText: implicitGet ? ["HtmxAction"] : []);
		}
		yield return Binding("normal-binding", w => w.UseIssue310NormalOnlyBindingScenario(), 5, "HTMXOR034", ["@onput", "no usable action is available", "remove the binding or marker"]);
		yield return Binding("conflicting-handler", w => w.UseIssue310ConflictingBindingScenario());
		yield return Binding("same-name", w => w.UseIssue310SameNameHandlerScenario(), 5, "HTMXOR010", ["@onput", "computed expression"], ["HTMXOR042"]);
		yield return Binding("script", w => w.UseIssue310ScriptBindingScenario());
		yield return Binding("component", w => w.UseIssue310ComponentBindingScenario(false));
		yield return Binding("generic-component", w => w.UseIssue310ComponentBindingScenario(true));
		yield return Binding("template", w => w.UseIssue310TemplateBindingScenario(), 8);
		yield return Binding("nested-helper", w => w.UseIssue310NestedTemplateBindingScenario(), 12, "HTMXOR040", ["handler 'Save' must be an instance method on the request-owned component"], ["HTMXOR042"]);
	}

	private static IEnumerable<PackedDiagnosticCase> OtherCases()
	{
		yield return new("stock-form", w => w.UseIssue179RejectedFormScenario(), "Issue179Rejected.razor", "HTMXOR033", ["Methods", "POST"]);
		yield return new("route-template", w => { w.UseIssue286StockRouteTemplateScenario(); w.UseIssue286MalformedTemplate(); }, "Issue286StaticHtmxPage.razor", "ASP0017", ["There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character"]);
		yield return new("wrappers", w => { w.UseIssue299ContextConstructionScenario("net10.0", "10.0.402", "10.0.13"); w.AddDetachedWrapperConstruction(); }, "DetachedWrappers.cs", "CS0122|CS1729", []);
		yield return new("retired-api", w => { w.UseRequestApiScenario("net10.0", "10.0.402", "10.0.13"); w.AddRetiredRequestApiReferences(); }, "RetiredRequestApi.cs", "CS0122|CS0234|CS1061", []);
	}
}
