using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Htmxor.Generators.Tests;

public sealed class HtmxorFormDiagnosticTests
{
	private const string Directory = "/tmp/issue339-form-capture";
	private const string Prefix = "no usable action is available: ";
	private static readonly ImmutableArray<MetadataReference> References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
		.Concat([typeof(ComponentBase).Assembly.Location, typeof(HtmxRouteAttribute).Assembly.Location])
		.Distinct(StringComparer.OrdinalIgnoreCase).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToImmutableArray();

	public static TheoryData<string, bool, bool, string> IndependentCases
	{
		get
		{
			var cases = new TheoryData<string, bool, bool, string>();
			foreach (var unrelatedError in new[] { false, true })
			{
				cases.Add("EditUnnamed", false, unrelatedError, "027:1:10:15,form033:1:10:15");
				cases.Add("EditUnnamed", true, unrelatedError, "027:1:10:15,form033:1:10:15");
				cases.Add("PlainUnnamed", false, unrelatedError, "028:1:0:5,form033:1:0:5");
				cases.Add("PlainUnnamed", true, unrelatedError, "");
				cases.Add("Repeated", false, unrelatedError, "028:1:0:5,form033:1:0:5,028:2:0:5,form033:2:0:5");
				cases.Add("Repeated", true, unrelatedError, "");
				cases.Add("NamedConflict", false, unrelatedError, "032:1:24:32,form033:1:24:32,binding033:2:17:21");
				cases.Add("NamedConflict", true, unrelatedError, "032:1:24:32,form033:1:24:32,binding033:2:17:21");
				cases.Add("PlainConflict", false, unrelatedError, "032:1:33:37,form033:1:33:37,binding033:2:17:21");
				cases.Add("PlainConflict", true, unrelatedError, "032:1:33:37,form033:1:33:37,binding033:2:17:21");
			}
			return cases;
		}
	}

	[Theory]
	[MemberData(nameof(IndependentCases))]
	public async Task Independent_form_causes_keep_raw_multiplicity_locations_and_approved_locality(string fixture, bool local, bool unrelatedError, string expected)
	{
		var result = await AnalyzeAsync(fixture, unrelatedError: unrelatedError);

		AssertDiagnostics(fixture, expected, local ? result.Local : result.Complete);
		if (local) Assert.All(result.Local, diagnostic =>
		{
			Assert.NotNull(diagnostic.Location.SourceTree);
			Assert.DoesNotContain(WellKnownDiagnosticTags.CompilationEnd, diagnostic.Descriptor.CustomTags);
		});
	}

	[Theory]
	[InlineData("InvalidName", false, "form033:1:24:32")]
	[InlineData("InvalidName", true, "form033:1:24:32")]
	[InlineData("InvalidOwner", false, "")]
	[InlineData("InvalidOwner", true, "")]
	[InlineData("MixedInvalid", false, "form033:1:24:32,028:2:0:5,form033:2:0:5")]
	[InlineData("MixedInvalid", true, "form033:1:24:32")]
	public async Task Compiler_owned_invalid_facts_do_not_hide_an_independent_valid_form(string fixture, bool local, string expected)
	{
		var result = await AnalyzeAsync(fixture, compilerError: "CS0103");

		AssertDiagnostics(fixture, expected, local ? result.Local : result.Complete);
	}

	[Theory]
	[InlineData("ReviewPlainInvalid", false, false, "form033:1:33:40,binding033:2:17:21")]
	[InlineData("ReviewPlainInvalid", true, false, "form033:1:33:40,binding033:2:17:21")]
	[InlineData("ReviewPlainInvalid", false, true, "form033:1:33:40,binding033:2:17:21")]
	[InlineData("ReviewPlainInvalid", true, true, "form033:1:33:40,binding033:2:17:21")]
	[InlineData("ReviewDescendant", false, false, "form033:2:11:19")]
	[InlineData("ReviewDescendant", true, false, "form033:2:11:19")]
	[InlineData("ReviewDescendant", false, true, "form033:2:11:19")]
	[InlineData("ReviewDescendant", true, true, "form033:2:11:19")]
	public async Task Invalid_name_or_descendant_preserves_only_independently_decidable_causes(string fixture, bool local, bool unrelatedError, string expected)
	{
		var result = await AnalyzeAsync(fixture, unrelatedError: unrelatedError,
			compilerError: fixture == "ReviewDescendant" ? "CS0103,CS1662" : "CS0103");

		AssertDiagnostics(fixture, expected, local ? result.Local : result.Complete);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Invalid_plain_name_cannot_drive_aggregate_inference(bool unrelatedError)
	{
		var result = await AnalyzeAsync("ReviewInvalidInference", unrelatedError: unrelatedError, compilerError: "CS0103", omitForm: true);

		AssertDiagnostics("ReviewInvalidInference", "", result.Complete);
		Assert.Empty(result.Local);
	}

	[Theory]
	[InlineData("ConsumerInvalidEdit", false, "")]
	[InlineData("ConsumerInvalidEdit", true, "")]
	[InlineData("ConsumerInvalidPlain", false, "")]
	[InlineData("ConsumerInvalidPlain", true, "")]
	[InlineData("ConsumerValidEdit", false, "action032:4:5:23")]
	[InlineData("ConsumerValidEdit", true, "action032:4:5:23")]
	[InlineData("ConsumerInvalidExcluded", false, "form033:1:24:32,action033:4:5:23")]
	[InlineData("ConsumerInvalidExcluded", true, "form033:1:24:32,action033:4:5:23")]
	public async Task Explicit_post_conflicts_require_valid_form_names_without_hiding_independent_exclusions(string fixture, bool unrelatedError, string expected)
	{
		var result = await AnalyzeAsync(fixture, unrelatedError: unrelatedError,
			compilerError: fixture == "ConsumerValidEdit" ? null : "CS0103");

		AssertDiagnostics(fixture, expected, result.Complete);
	}

	[Fact]
	public async Task Missing_generated_form_inference_is_complete_only_at_its_original_mapped_name()
	{
		var result = await AnalyzeAsync("Mismatch", omitForm: true);

		AssertDiagnostics("Mismatch", "043:1:33:37", result.Complete);
		Assert.Empty(result.Local);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Named_form_and_member_template_post_conflict_retains_its_form_location(bool crossTree)
	{
		var result = await AnalyzeAsync("Cross", crossTree: crossTree);

		AssertDiagnostics("Cross", "032:1:24:32,036:5:41:48", result.Complete);
		if (crossTree) Assert.Empty(result.Local);
	}

	[Theory]
	[InlineData("Valid")]
	[InlineData("Stock")]
	public async Task Valid_forms_keep_their_generated_output_without_diagnostics(string fixture)
	{
		var result = await AnalyzeAsync(fixture);

		Assert.Empty(result.Complete);
		Assert.Empty(result.Local);
		Assert.Contains("\"Consumer." + fixture + "\"", result.Generated, StringComparison.Ordinal);
		if (fixture == "Valid") Assert.Contains("NamedFormComponentTypeNames =\n\t[\n\t\t\"Consumer.Valid\",", result.Generated, StringComparison.Ordinal);
	}

	private static void AssertDiagnostics(string fixture, string specification, ImmutableArray<Diagnostic> diagnostics)
	{
		var expected = specification.Length == 0 ? [] : specification.Split(',').Select(item => Expected(fixture, item)).ToArray();
		var actual = diagnostics.Select(diagnostic =>
		{
			var span = diagnostic.Location.GetMappedLineSpan();
			Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
			if (fixture is "PlainUnnamed" or "Repeated" || fixture == "MixedInvalid" && span.StartLinePosition.Line == 2)
			{
				Assert.Null(diagnostic.Location.SourceTree);
				Assert.Equal(LocationKind.ExternalFile, diagnostic.Location.Kind);
			}
			Assert.Contains(WellKnownDiagnosticTags.NotConfigurable, diagnostic.Descriptor.CustomTags);
			return $"{diagnostic.Id}|{diagnostic.GetMessage()}|{span.Path}|{span.StartLinePosition.Line}:{span.StartLinePosition.Character}-{span.EndLinePosition.Line}:{span.EndLinePosition.Character}";
		});
		var observed = actual.Order(StringComparer.Ordinal).ToArray();
		Assert.True(expected.Order(StringComparer.Ordinal).SequenceEqual(observed), "Expected:\n" + string.Join("\n", expected) + "\nActual:\n" + string.Join("\n", observed));
	}

	private static string Expected(string fixture, string item)
	{
		var values = item.Split(':');
		var reason = values[0] switch
		{
			"027" => "an EditForm on an HtmxRoute owner requires FormName",
			"028" => "a POST form on an HtmxRoute owner requires @formname",
			"032" => "named forms and a POST action cannot share one route owner; use named forms or a POST action",
			"action032" => "named forms and a POST HtmxAction cannot share one route owner; choose named forms or the action",
			"action033" => "explicit HtmxRoute.Methods does not allow the POST HtmxAction; add the method or remove the action",
			"form033" => "explicit HtmxRoute.Methods does not allow POST for its form; add POST or remove the form",
			"binding033" => "Use [HtmxAction] to explicitly declare a supported instance action instead of this binding; explicit HtmxRoute.Methods is authoritative and does not allow the POST binding; add the method or remove the binding",
			"036" => "Use [HtmxAction] to explicitly declare a supported instance action instead of this binding; @onpost in a Razor template or @code markup is not supported; put the binding in the component's own markup or declare [HtmxAction]",
			"043" => "the compiled POST form has no matching generated POST inference for its component; move the form into the route owner's own Razor markup, or replace the form with a supported explicit POST action",
			_ => throw new ArgumentException("Unknown expected reason", nameof(item)),
		};
		var id = values[0][^3..];
		return $"HTMXOR{id}|{Prefix}{reason}|{FixtureDirectory(fixture)}/{fixture}.razor|{values[1]}:{values[2]}-{values[1]}:{values[3]}";
	}

	private static async Task<Result> AnalyzeAsync(string fixture, bool unrelatedError = false, string? compilerError = null, bool omitForm = false, bool crossTree = false)
	{
		var parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
		var generatedPath = "Microsoft.CodeAnalysis.Razor.Compiler/Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator/";
		var render = CSharpSyntaxTree.ParseText(Read(fixture, "generated"), parse, generatedPath + fixture + "_razor.g.cs");
		var declaration = CSharpSyntaxTree.ParseText(Read(fixture, "declaration"), parse, generatedPath + fixture + "_razor.decl.g.cs");
		var trees = new List<SyntaxTree> { render, declaration };
		if (crossTree)
		{
			// Partition authentic lowered members without inventing builder calls or source mappings.
			var root = render.GetRoot();
			var owner = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
			var template = owner.Members.OfType<PropertyDeclarationSyntax>().Single();
			var templateOwner = owner.WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(template))
				.WithAttributeLists(default).WithBaseList(null);
			trees[0] = render.WithRootAndOptions(root.ReplaceNode(owner, owner.RemoveNode(template, SyntaxRemoveOptions.KeepUnbalancedDirectives)!), parse);
			trees.Add(CSharpSyntaxTree.Create((CSharpSyntaxNode)root.ReplaceNode(owner, templateOwner), parse, generatedPath + "CrossTemplate_razor.g.cs"));
			render = trees[0];
		}
		if (unrelatedError) trees.Add(CSharpSyntaxTree.ParseText("class Unrelated { Missing value; }", parse, "Unrelated.cs"));
		var compilation = CSharpCompilation.Create("FormFeedback", trees, References, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		var expectedErrors = (compilerError?.Split(',') ?? []).Concat(unrelatedError ? ["CS0246"] : []).Order(StringComparer.Ordinal).ToArray();
		Assert.Equal(expectedErrors, compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal));
		var content = Read(fixture, "razor");
		var directory = FixtureDirectory(fixture);
		var razor = new RazorFile(directory + "/" + fixture + ".razor", omitForm ? content.Split('\n')[0] + "\n" : content);
		var imports = new RazorFile(directory + "/_Imports.razor", Read("_Imports", "razor"));
		var options = new Options(directory);
		GeneratorDriver generator = CSharpGeneratorDriver.Create([new HtmxorRouteGenerator().AsSourceGenerator(), new HtmxorActionGenerator().AsSourceGenerator()], [razor, imports], parse, options);
		generator = generator.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var driverDiagnostics);
		Assert.DoesNotContain(driverDiagnostics, diagnostic => diagnostic.Id == "CS8785");
		Assert.Equal(expectedErrors, output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal));
		var analyzers = output.WithAnalyzers([new HtmxorRouteDeclarationAnalyzer(), new HtmxorActionDeclarationAnalyzer(), new HtmxorCompiledBindingAnalyzer()], new AnalyzerOptions([razor, imports], options));
		var complete = generator.GetRunResult().Diagnostics.AddRange(await analyzers.GetAnalyzerDiagnosticsAsync());
		var local = await analyzers.GetAnalyzerSemanticDiagnosticsAsync(output.GetSemanticModel(render), null, CancellationToken.None);
		return new Result(complete, local, string.Join("\n", generator.GetRunResult().GeneratedTrees.Select(tree => tree.GetText().ToString())));
	}

	private static string FixtureDirectory(string fixture) => fixture.StartsWith("Consumer", StringComparison.Ordinal) ? "/tmp/issue339-consumer-capture"
		: fixture.StartsWith("Review", StringComparison.Ordinal) ? "/tmp/issue339-review-capture" : Directory;

	private static string Read(string fixture, string part)
	{
		using var stream = typeof(HtmxorFormDiagnosticTests).Assembly.GetManifestResourceStream("Htmxor.Generators.FormDiagnosticFixtures." + fixture + "." + part + ".fixture")!;
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	private sealed record Result(ImmutableArray<Diagnostic> Complete, ImmutableArray<Diagnostic> Local, string Generated);
	private sealed class RazorFile(string path, string content) : AdditionalText
	{
		public override string Path => path;
		public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content);
	}
	private sealed class Options(string directory) : AnalyzerConfigOptionsProvider
	{
		public override AnalyzerConfigOptions GlobalOptions { get; } = new Values(directory);
		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
	}
	private sealed class Values(string directory) : AnalyzerConfigOptions
	{
		public override bool TryGetValue(string key, out string value)
		{
			value = key == "build_property.RootNamespace" ? "Consumer" : directory;
			return key is "build_property.RootNamespace" or "build_property.MSBuildProjectDirectory";
		}
	}
}
