using Htmxor.Builder;
using Microsoft.CodeAnalysis;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorSharedExplicitReasonTests
{
	public static TheoryData<string, string, string, string> Cases()
	{
		var result = new TheoryData<string, string, string, string>();
		void Add(string member, string id, string expected, string route = "[Microsoft.AspNetCore.Components.Route(\"/explicit\")]") => result.Add(member, route, id, expected);
		Add("[Htmxor.HtmxAction(\"GET\")] private void Save() {}", "029", "HtmxAction method must be POST, PUT, PATCH, DELETE or QUERY");
		Add("[Htmxor.HtmxAction(\"PUT\")] private void Save() {}", "030", "HtmxAction must be declared on the component type that owns its route, not an inherited or unrouted owner", "");
		Add("[Htmxor.HtmxAction(\"PUT\")] private void Save() {}", "034", "HtmxAction is not supported on a component marked with DisableHtmxDirectRouting; remove the action or marker", "[Microsoft.AspNetCore.Components.Route(\"/explicit\")] [Htmxor.DisableHtmxDirectRouting]");
		Add("[Htmxor.HtmxAction(\"PUT\")] private void Save() {}", "030", "HtmxAction must be declared on the component type that owns its route, not an inherited or unrouted owner", "inherited");
		Add("[Htmxor.HtmxAction(\"PUT\")] private static void Save() {}", "013", "is static; make it an instance method");
		Add("[Htmxor.HtmxAction(\"PUT\")] private void Save<T>() {}", "022", "is generic; make it non-generic");
		Add("[Htmxor.HtmxAction(\"PUT\")] private async void Save() { await System.Threading.Tasks.Task.Yield(); }", "018", "is async void; return Task instead");
		Add("[Htmxor.HtmxAction(\"PUT\")] private int Save() => 1;", "026", "returns a value; return void or Task");
		Add("[Htmxor.HtmxAction(\"PUT\")] private System.Threading.Tasks.Task<int> Save() => System.Threading.Tasks.Task.FromResult(1);", "019", "returns a value; return void or Task");
		Add("[Htmxor.HtmxAction(\"PUT\")] private System.Threading.Tasks.ValueTask Save() => default;", "016", "returns a value; return void or Task");
		Add("[Htmxor.HtmxAction(\"PUT\")] private void Save(Htmxor.HtmxEventArgs a, Htmxor.HtmxEventArgs b) {}", "025", "has unsupported parameters; use zero parameters or one required HtmxEventArgs parameter");
		foreach (var modifier in new[] { "ref", "out", "in" })
			Add("[Htmxor.HtmxAction(\"PUT\")] private void Save(" + modifier + " Htmxor.HtmxEventArgs a) { throw new System.NotImplementedException(); }", "023", "has unsupported parameters; use one by-value HtmxEventArgs parameter");
		Add("[Htmxor.HtmxAction(\"PUT\")] private void Save(Htmxor.HtmxEventArgs a = null) {}", "024", "has an optional parameter; make it required");
		foreach (var (type, id) in new[] { ("System.EventArgs", "020"), ("object", "021"), ("int", "025") })
			Add("[Htmxor.HtmxAction(\"PUT\")] private void Save(" + type + " a) {}", id, "has a parameter that is not HtmxEventArgs; its parameter must be HtmxEventArgs");
		Add("[Htmxor.HtmxAction(\"PUT\")] private void Save() {} private void Save(Htmxor.HtmxEventArgs a) {}", "017", "is overloaded; give the handler a unique name");
		Add("public int Value { get => 1; [Htmxor.HtmxAction(\"PUT\")] set {} }", "035", "is not an ordinary callback method; declare an ordinary instance method returning void or Task");
		Add("[Htmxor.HtmxAction(\"PUT\")] ~Owner() {}", "035", "is not an ordinary callback method; declare an ordinary instance method returning void or Task");
		Add("private void Initialize() { [Htmxor.HtmxAction(\"PUT\")] void Save() { _ = this.GetHashCode(); } Save(); }", "035", "is not an ordinary callback method; declare an ordinary instance method returning void or Task");
		Add("private System.Action Factory() => [Htmxor.HtmxAction(\"PUT\")] () => { _ = this.GetHashCode(); };", "035", "is not an ordinary callback method; declare an ordinary instance method returning void or Task");
		Add("[Htmxor.HtmxAction(\"PUT\")] void IHandler.Save() {}", "035", "is not an ordinary callback method; declare an ordinary instance method returning void or Task");
		Add("[Htmxor.HtmxAction(\"PUT\")] private void Save() {}", "033", "explicit HtmxRoute.Methods does not allow the PUT HtmxAction; add the method or remove the action", "[Htmxor.HtmxRoute(\"/explicit\", Methods = new[] { \"GET\" })]");
		Add("[Htmxor.HtmxAction(\"PUT\")] private void Save() {} [Htmxor.HtmxAction(\"PUT\")] private void Other() {}", "031", "PUT declarations on one route owner must name the same handler; select one handler");
		return result;
	}

	[Theory]
	[MemberData(nameof(Cases))]
	public async Task Compiler_rejection_emits_the_canonical_explicit_reason(string member, string route, string id, string expected)
	{
		var implementation = member.Contains("IHandler.Save", StringComparison.Ordinal) ? ", IHandler" : "";
		var source = "namespace Consumer; public interface IHandler { void Save(); } [Microsoft.AspNetCore.Components.Route(\"/inherited\")] public class Parent : Microsoft.AspNetCore.Components.ComponentBase {} " + (route == "inherited" ? "" : route) + " public class Owner : " + (route == "inherited" ? "Parent" : "Microsoft.AspNetCore.Components.ComponentBase") + implementation + " { " + member + " }";
		var compilation = SharedRejectionReasonTestHost.Compile(SharedRejectionReasonTestHost.Parse(source));
		var diagnostics = await SharedRejectionReasonTestHost.Analyze(compilation, new HtmxorActionDeclarationAnalyzer());
		Assert.Equal(id == "031" ? 2 : 1, diagnostics.Length);
		var firstAttribute = source.IndexOf("Htmxor.HtmxAction", StringComparison.Ordinal);
		int[] expectedStarts = id == "031" ? [firstAttribute, source.LastIndexOf("Htmxor.HtmxAction", StringComparison.Ordinal)] : [firstAttribute];
		Assert.Equal(expectedStarts, diagnostics.Select(diagnostic => diagnostic.Location.SourceSpan.Start).Order());
		Assert.All(diagnostics, diagnostic =>
		{
			Assert.Equal("HTMXOR" + id, diagnostic.Id);
			SharedRejectionReasonTestHost.AssertDiagnostic(diagnostic, expected);
			Assert.Equal("Owner.cs", diagnostic.Location.GetMappedLineSpan().Path);
			Assert.Equal(id == "029" ? "Htmxor.HtmxAction(\"GET\")" : "Htmxor.HtmxAction(\"PUT\")", source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
		});
	}

	[Theory]
	[MemberData(nameof(Cases))]
	public void Runtime_rejection_emits_the_canonical_explicit_reason(string member, string route, string id, string expected)
	{
		var implementation = member.Contains("IHandler.Save", StringComparison.Ordinal) ? ", IHandler" : "";
		var source = "namespace Consumer; public interface IHandler { void Save(); } [Microsoft.AspNetCore.Components.Route(\"/inherited\")] public class Parent : Microsoft.AspNetCore.Components.ComponentBase {} " + (route == "inherited" ? "" : route) + " public class Owner : " + (route == "inherited" ? "Parent" : "Microsoft.AspNetCore.Components.ComponentBase") + implementation + " { " + member + " }";
		var compilation = SharedRejectionReasonTestHost.Compile(SharedRejectionReasonTestHost.Parse(source));
		var (assembly, owner) = SharedRejectionReasonTestHost.Emit(compilation);
		var exception = Assert.Throws<InvalidOperationException>(() =>
		{
			var actions = HtmxorExplicitComponentActionCatalog.Build(assembly, [owner.FullName!], []);
			_ = HtmxorExplicitComponentActionCatalog.Merge(actions);
		});
		SharedRejectionReasonTestHost.AssertException(exception, expected);
	}
}
