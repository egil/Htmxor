using Htmxor.Builder;
using Microsoft.CodeAnalysis;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorSharedRouteReasonTests
{
	public static TheoryData<string, string, string, string> Cases()
	{
		var cases = new TheoryData<string, string, string, string>();
		void Add(string attribute, string owner, string id, string expected) => cases.Add(attribute, owner, id, expected);
		Add("[Htmxor.HtmxRoute(\"/reason\", Methods = new[] { \"GET\" })]", "public abstract class Owner : Microsoft.AspNetCore.Components.ComponentBase", "004", "the HtmxRoute target must be a concrete, non-generic Blazor component");
		Add("[Htmxor.HtmxRoute(\"/reason\", Methods = new[] { \"GET\" })]", "public class Owner<T> : Microsoft.AspNetCore.Components.ComponentBase", "004", "the HtmxRoute target must be a concrete, non-generic Blazor component");
		Add("[Htmxor.HtmxRoute(\"/reason\", Methods = new[] { \"GET\" })]", "public class Owner", "004", "the HtmxRoute target must be a concrete, non-generic Blazor component");
		Add("[Htmxor.HtmxRoute(\" \u0020\", Methods = new[] { \"GET\" })]", "public class Owner : Microsoft.AspNetCore.Components.ComponentBase", "005", "HtmxRoute must resolve one nonblank constant route template");
		Add("[Htmxor.HtmxRoute(null, Methods = new[] { \"GET\" })]", "public class Owner : Microsoft.AspNetCore.Components.ComponentBase", "005", "HtmxRoute must resolve one nonblank constant route template");
		Add("[Htmxor.HtmxRoute(\"reason\", Methods = new[] { \"GET\" })]", "public class Owner : Microsoft.AspNetCore.Components.ComponentBase", "005", "HtmxRoute template 'reason' is invalid: The @page directive must specify a route template. The route template must be enclosed in quotes and begin with the '/' character");
		foreach (var methods in new[] { "null", "new string[0]", "new[] { \"GET\", \"get\" }", "new[] { \"HEAD\" }", "new string[] { null }" })
			Add("[Htmxor.HtmxRoute(\"/reason\", Methods = " + methods + ")]", "public class Owner : Microsoft.AspNetCore.Components.ComponentBase", "005", "explicit HtmxRoute Methods must resolve to a non-empty unique subset of GET, POST, PUT, PATCH, DELETE, and QUERY");
		Add("[Htmxor.HtmxRoute(\"/reason\", Methods = new[] { \"GET\" }, CurrentUrl = \"javascript:bad\")]", "public class Owner : Microsoft.AspNetCore.Components.ComponentBase", "005", "HtmxRoute named argument 'CurrentUrl' must resolve to a valid relative or absolute HTTP(S) URI or null");
		Add("[Htmxor.HtmxRoute(\"/reason\", Methods = new[] { \"GET\" }, Target = \"two ids\")]", "public class Owner : Microsoft.AspNetCore.Components.ComponentBase", "005", "HtmxRoute named argument 'Target' must resolve to a valid element identity or null or whitespace");
		foreach (var targets in new[] { "new[] { \"two ids\" }", "new string[] { null }" })
			Add("[Htmxor.HtmxRoute(\"/reason\", Methods = new[] { \"GET\" }, Targets = " + targets + ")]", "public class Owner : Microsoft.AspNetCore.Components.ComponentBase", "005", "HtmxRoute named argument 'Targets' must resolve to a constant array of valid element identities");
		Add("[Microsoft.AspNetCore.Components.Route(\"/stock\")] [Htmxor.HtmxRoute(\"/reason\", Methods = new[] { \"GET\" })]", "public class Owner : Microsoft.AspNetCore.Components.ComponentBase", "004", "an HTMX-only component cannot also declare a normal Blazor route");
		Add("[Htmxor.HtmxRoute(\"/reason\", Methods = new[] { \"GET\" })]", "public class Owner : Parent", "004", "an HTMX-only component cannot also declare a normal Blazor route");
		Add("[Htmxor.DisableHtmxDirectRouting] [Htmxor.HtmxRoute(\"/reason\", Methods = new[] { \"GET\" })]", "public class Owner : Microsoft.AspNetCore.Components.ComponentBase", "003", "DisableHtmxDirectRouting cannot be combined with HtmxRoute on the same type");
		return cases;
	}

	[Theory]
	[MemberData(nameof(Cases))]
	public async Task Compiler_rejection_emits_the_canonical_route_reason(string attributes, string ownerDeclaration, string id, string expected)
	{
		var source = "namespace Consumer; [Microsoft.AspNetCore.Components.Route(\"/inherited\")] public class Parent : Microsoft.AspNetCore.Components.ComponentBase {} " + attributes + " " + ownerDeclaration + " {}";
		var compilation = SharedRejectionReasonTestHost.Compile(SharedRejectionReasonTestHost.Parse(source));
		var diagnostics = await SharedRejectionReasonTestHost.Analyze(compilation, new HtmxorRouteDeclarationAnalyzer());
		Assert.Equal(id == "003" ? 2 : 1, diagnostics.Length);
		var diagnostic = Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "HTMXOR" + id && diagnostic.GetMessage().Contains(expected, StringComparison.Ordinal)));
		SharedRejectionReasonTestHost.AssertDiagnostic(diagnostic, expected);
		Assert.Equal("Owner.cs", diagnostic.Location.GetMappedLineSpan().Path);
		Assert.StartsWith(id == "003" ? "Htmxor.DisableHtmxDirectRouting" : "Htmxor.HtmxRoute", source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length), StringComparison.Ordinal);
	}

	[Theory]
	[MemberData(nameof(Cases))]
	public void Runtime_rejection_emits_the_canonical_route_reason(string attributes, string ownerDeclaration, string id, string expected)
	{
		var source = "namespace Consumer; [Microsoft.AspNetCore.Components.Route(\"/inherited\")] public class Parent : Microsoft.AspNetCore.Components.ComponentBase {} " + attributes + " " + ownerDeclaration + " {}";
		var compilation = SharedRejectionReasonTestHost.Compile(SharedRejectionReasonTestHost.Parse(source));
		var name = ownerDeclaration.Contains("Owner<T>", StringComparison.Ordinal) ? "Consumer.Owner`1" : "Consumer.Owner";
		var (assembly, owner) = SharedRejectionReasonTestHost.Emit(compilation, name);
		var exception = Assert.Throws<InvalidOperationException>(() => HtmxorAttributedRouteCatalog.Build(assembly, [owner.FullName!]));
		SharedRejectionReasonTestHost.AssertException(exception, expected);
	}
}
