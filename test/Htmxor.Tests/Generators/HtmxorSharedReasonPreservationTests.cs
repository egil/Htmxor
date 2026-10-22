using Htmxor.Builder;
using Microsoft.CodeAnalysis;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorSharedReasonPreservationTests
{
	[Theory]
	[InlineData("null", "null", "null")]
	[InlineData("\"\"", "\" \"", "new string[0]")]
	[InlineData("\"/relative\"", "\"result\"", "new[] { \"result\", \"other\" }")]
	[InlineData("\"https://example.test/result\"", "\"result\"", "new[] { \"result\" }")]
	public async Task Supported_route_representation_and_case_insensitive_methods_remain_valid(string currentUrl, string target, string targets)
	{
		var source = "namespace Consumer; [Htmxor.HtmxRoute(\"/valid\", Methods = new[] { \"get\", \"pUt\", \"QUERY\" }, CurrentUrl = " + currentUrl + ", Target = " + target + ", Targets = " + targets + ")] public class Owner : Microsoft.AspNetCore.Components.ComponentBase {}";
		var compilation = SharedRejectionReasonTestHost.Compile(SharedRejectionReasonTestHost.Parse(source));

		Assert.Empty(await SharedRejectionReasonTestHost.Analyze(compilation, new HtmxorRouteDeclarationAnalyzer()));
		var (assembly, owner) = SharedRejectionReasonTestHost.Emit(compilation);
		var route = Assert.Single(HtmxorAttributedRouteCatalog.Build(assembly, [owner.FullName!]));
		Assert.Equal(owner, route.ComponentType);
		Assert.Equal("/valid", route.NormalizedRoute);
	}

	[Theory]
	[InlineData("private void Save() {}")]
	[InlineData("private void Save(Htmxor.HtmxEventArgs args) {}")]
	[InlineData("private System.Threading.Tasks.Task Save() => System.Threading.Tasks.Task.CompletedTask;")]
	[InlineData("private System.Threading.Tasks.Task Save(Htmxor.HtmxEventArgs args) => System.Threading.Tasks.Task.CompletedTask;")]
	[InlineData("[System.Runtime.CompilerServices.SpecialName] private void Save() {}")]
	[InlineData("[System.Runtime.CompilerServices.CompilerGenerated] private void Save() {}")]
	[InlineData("private void Finalize() {}")]
	public async Task Supported_explicit_handlers_keep_their_route_and_method(string member)
	{
		var source = "namespace Consumer; [Microsoft.AspNetCore.Components.Route(\"/valid\")] public class Owner : Microsoft.AspNetCore.Components.ComponentBase { [Htmxor.HtmxAction(\"pUt\")] " + member + " }";
		var compilation = SharedRejectionReasonTestHost.Compile(SharedRejectionReasonTestHost.Parse(source));

		Assert.Empty(await SharedRejectionReasonTestHost.Analyze(compilation, new HtmxorActionDeclarationAnalyzer()));
		var (assembly, owner) = SharedRejectionReasonTestHost.Emit(compilation);
		var action = Assert.Single(HtmxorExplicitComponentActionCatalog.Build(assembly, [owner.FullName!], []));
		Assert.Equal(owner, action.ComponentType);
		Assert.Equal("PUT", action.HttpMethod);
		Assert.Equal("/valid", action.NormalizedRoute);
	}

	[Theory]
	[InlineData("public abstract class Owner", "Consumer.Owner")]
	[InlineData("public class Owner<T>", "Consumer.Owner`1")]
	public async Task Runtime_only_abstract_and_open_explicit_owners_still_fail_closed(string declaration, string typeName)
	{
		var source = "namespace Consumer; [Microsoft.AspNetCore.Components.Route(\"/valid\")] " + declaration + " : Microsoft.AspNetCore.Components.ComponentBase { [Htmxor.HtmxAction(\"PUT\")] private void Save() {} }";
		var compilation = SharedRejectionReasonTestHost.Compile(SharedRejectionReasonTestHost.Parse(source));

		Assert.Empty(await SharedRejectionReasonTestHost.Analyze(compilation, new HtmxorActionDeclarationAnalyzer()));
		var (assembly, owner) = SharedRejectionReasonTestHost.Emit(compilation, typeName);
		Assert.Throws<InvalidOperationException>(() => HtmxorExplicitComponentActionCatalog.Build(assembly, [owner.FullName!], []));
	}

	[Fact]
	public async Task Ordinary_operator_rule_remains_a_distinct_compiler_only_rejection()
	{
		var source = "namespace Consumer; [Microsoft.AspNetCore.Components.Route(\"/valid\")] public class Owner : Microsoft.AspNetCore.Components.ComponentBase { [Htmxor.HtmxAction(\"PUT\")] public static Owner operator +(Owner a, Owner b) => a; }";
		var compilation = SharedRejectionReasonTestHost.Compile(SharedRejectionReasonTestHost.Parse(source));
		var diagnostic = Assert.Single(await SharedRejectionReasonTestHost.Analyze(compilation, new HtmxorActionDeclarationAnalyzer()));
		Assert.Equal("HTMXOR035", diagnostic.Id);
		var (assembly, owner) = SharedRejectionReasonTestHost.Emit(compilation);

		var exception = Assert.Throws<InvalidOperationException>(() => HtmxorExplicitComponentActionCatalog.Build(assembly, [owner.FullName!], []));
		Assert.DoesNotContain("ordinary", exception.Message, StringComparison.Ordinal);
	}
}
