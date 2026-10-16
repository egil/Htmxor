using Microsoft.CodeAnalysis;

namespace Htmxor.Generators.Tests;

public sealed partial class HtmxorExplicitActionAnalyzerTests
{
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Method_targeted_event_fields_report_once_per_authored_attribute(bool multipleAttributes, bool local)
	{
		var attributes = multipleAttributes
			? "[method: Htmxor.HtmxAction(\"PUT\"), Htmxor.HtmxAction(\"PATCH\")]"
			: "[method: Htmxor.HtmxAction(\"PUT\")]";
		var source = Owner(attributes + " public event System.Action Changed; public void Raise() => Changed?.Invoke();");
		var expectedCount = multipleAttributes ? 2 : 1;
		var compilation = CreateCompilation(source);
		Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
		var changed = Assert.IsAssignableFrom<IEventSymbol>(Assert.Single(compilation.GetTypeByMetadataName("Consumer.Owner")!.GetMembers("Changed")));
		// One authored method-targeted attribute is copied to both compiler-created accessors.
		Assert.Equal(expectedCount, changed.AddMethod!.GetAttributes().Count(attribute => attribute.AttributeClass!.ToDisplayString() == "Htmxor.HtmxActionAttribute"));
		Assert.Equal(expectedCount, changed.RemoveMethod!.GetAttributes().Count(attribute => attribute.AttributeClass!.ToDisplayString() == "Htmxor.HtmxActionAttribute"));

		var diagnostics = await AnalyzeFeedbackAsync(source, local);

		Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "HTMXOR031");
		AssertFeedback(diagnostics, source, Enumerable.Repeat(("HTMXOR035", "ordinary callback method"), expectedCount).ToArray());
		Assert.Equal(expectedCount, diagnostics.Select(diagnostic => diagnostic.Location.SourceSpan).Distinct().Count());
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Compiler_owned_generic_constraints_suppress_only_the_malformed_declaration(bool localFunction, bool local)
	{
		var malformed = localFunction
			? "private void Initialize() { [Htmxor.HtmxAction(\"PATCH\")] static void Broken<T>() where T : Missing { } }"
			: "[Htmxor.HtmxAction(\"PATCH\")] private static void Broken<T>() where T : Missing { }";
		var source = Owner(malformed + " [Htmxor.HtmxAction(\"PUT\")] private static void Save() { }");

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS0246");

		AssertFeedback(diagnostics, source, ("HTMXOR013", "is static"));
		Assert.Equal(source.LastIndexOf("Htmxor.HtmxAction", StringComparison.Ordinal), Assert.Single(diagnostics).Location.SourceSpan.Start);
	}

	[Theory]
	[InlineData(false, false, false)]
	[InlineData(false, false, true)]
	[InlineData(false, true, false)]
	[InlineData(false, true, true)]
	[InlineData(true, false, false)]
	[InlineData(true, false, true)]
	[InlineData(true, true, false)]
	[InlineData(true, true, true)]
	public async Task Compiler_errors_in_callback_bodies_do_not_hide_decidable_header_feedback(bool localFunction, bool expressionBody, bool local)
	{
		var body = expressionBody ? "=> Missing();" : "{ Missing(); }";
		var member = localFunction
			? "private void Initialize() { [Htmxor.HtmxAction(\"PUT\")] static void Broken<T>() " + body + " }"
			: "[Htmxor.HtmxAction(\"PUT\")] private static void Broken<T>() " + body;
		var source = Owner(member);

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS0103");

		if (localFunction)
		{
			AssertFeedback(diagnostics, source, ("HTMXOR035", "ordinary callback method"));
		}
		else
		{
			AssertFeedback(diagnostics, source, ("HTMXOR013", "is static"), ("HTMXOR022", "is generic"));
		}
	}

	[Theory]
	[InlineData("new string[] { Missing }", false)]
	[InlineData("new string[] { Missing }", true)]
	[InlineData("new string[] { \"GET\", Missing }", false)]
	[InlineData("new string[] { \"GET\", Missing }", true)]
	public async Task Nested_route_method_binding_errors_do_not_invent_an_allowlist_conflict(string methods, bool local)
	{
		var source = Owner("[Htmxor.HtmxAction(\"PUT\")] private static void Save() { }",
			route: "[Htmxor.HtmxRoute(\"/explicit\", Methods = " + methods + ")]");
		var route = Assert.Single(CreateCompilation(source).GetTypeByMetadataName("Consumer.Owner")!.GetAttributes()
			.Where(attribute => attribute.AttributeClass!.ToDisplayString() == "Htmxor.HtmxRouteAttribute"));
		var constant = Assert.Single(route.NamedArguments, argument => argument.Key == "Methods").Value;
		Assert.Equal(TypedConstantKind.Array, constant.Kind);
		Assert.Contains(constant.Values, value => value.Kind == TypedConstantKind.Error);

		var diagnostics = await AnalyzeFeedbackAsync(source, local, compilerError: "CS0103");

		Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "HTMXOR033");
		AssertFeedback(diagnostics, source, ("HTMXOR013", "is static"));
	}
}
