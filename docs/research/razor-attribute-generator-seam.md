# Compiler-backed Razor attribute discovery

Research date: 2026-08-28 UTC

This note answers one narrow question: can Htmxor discover `HtmxRouteAttribute`
and related component attributes from `.razor` files by consuming the C# that
Razor generates, so equivalent legal C# forms such as these have identical
meaning?

```razor
@attribute [Htmxor.HtmxRoute("/summaries/{SummaryId:int}", Methods = new[] { "GET" })]
@attribute [Htmxor.HtmxRoute("/summaries/{SummaryId:int}", Methods = ["GET"])]
```

C# collection expressions have an implicit conversion to a single-dimensional
array target such as the `string[]` `Methods` property. See the
[C# 12 collection-expression specification](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-12.0/collection-expressions#conversions).

The upstream snapshots used here are Razor
[`58ec969`](https://github.com/dotnet/razor/tree/58ec96978ef4e5823b54e960b9fd64cff45d7e68),
Roslyn
[`7e23e9d`](https://github.com/dotnet/roslyn/tree/7e23e9d7d1a90e50319cbe2be17ecda58e3a5c83),
and the .NET SDK
[`fa35477`](https://github.com/dotnet/sdk/tree/fa3547709570e0805fbce80c294bedb69d720c1b).
They were the current upstream heads on the research date. The Roslyn
pre-compilation API discussed below is experimental and is not an established
.NET 10 Htmxor dependency.

## Conclusion

Htmxor should not interpret the spelling of C# attribute arguments. The right
model is to consume the attributed component symbol and Roslyn `AttributeData`,
then validate the bound constructor and named argument values. That naturally
normalizes `new[] { "GET" }`, `["GET"]`, qualification, aliases, argument order,
whitespace, comments, and other C# forms that bind to the same attribute values.

There is no supported one-pass seam for doing that from a second source
generator in the exact SDK 10.0.400 pipeline exercised here. The pinned Razor
source creates component declarations in a private copied compilation, then
emits the completed component C# as an implementation output. An executable
probe confirms that a sibling generator's `CompilationProvider` does not see
that implementation output.

The same packaged-DLL probe establishes a supported compiler-backed seam for
validation: a C# `DiagnosticAnalyzer` receives the completed compilation after
Razor and ordinary source-generator outputs have been added. It sees the real
generated component symbol, exact attribute symbols, fully resolved
`AttributeData`, and Razor-mapped source locations. Component-local constants,
including constants declared later in `@code`, are therefore available without
reading or parsing Razor text.

An analyzer cannot emit registration source or communicate values back to a
sibling generator. Issue #97 consequently separates three responsibilities:

1. a path-only source generator emits one application-local registration
   wrapper and a sorted manifest of project-root component metadata names;
2. a diagnostic analyzer validates every actual `HtmxRouteAttribute`
   declaration in the final compilation by exact symbol and bound value;
3. the generated wrapper passes its assembly and manifest to a runtime catalog,
   which validates all compiled declarations before mapping any endpoint and
   preserves their effective metadata.

The source generator and analyzer use `AdditionalText.Path` only. Neither calls
`AdditionalText.GetText`, recognizes Razor directives, parses C# snippets, or
depends on directive placement. Unsupported but compiler-valid declarations
produce deterministic nonconfigurable `HTMXOR001` build errors. The runtime
catalog independently fails closed if analyzer execution is bypassed or the
compiled metadata differs from the supported contract.

A user-authored `.razor.cs` partial class remains a fully compiler-native option,
but requiring it would change the public developer model. The executable probe
also confirms that disabling the Razor source generator materializes generated
C# early enough for a sibling generator to see it, but that is a project-wide
two-stage build choice. Copying or loading the SDK-private Razor compiler remains
outside the v1 design.

## What the Razor SDK and generator do

The Razor SDK installs its compiler assemblies as analyzers and passes `.razor`
and `.cshtml` inputs to the C# compiler as `AdditionalFiles`. It also exposes
target path, CSS scope, root namespace, Razor language version, and project
directory through analyzer configuration. See the SDK target that
[registers the analyzers and visible metadata](https://github.com/dotnet/sdk/blob/fa3547709570e0805fbce80c294bedb69d720c1b/src/RazorSdk/Targets/Microsoft.NET.Sdk.Razor.SourceGenerators.targets#L24-L47)
and then
[adds the Razor inputs as `AdditionalFiles`](https://github.com/dotnet/sdk/blob/fa3547709570e0805fbce80c294bedb69d720c1b/src/RazorSdk/Targets/Microsoft.NET.Sdk.Razor.SourceGenerators.targets#L50-L72).

Razor consumes those same files through `AdditionalTextsProvider`. For
components, its current pipeline:

1. filters the additional texts, resolves `_Imports.razor`, and selects component
   files;
2. generates declaration-only C# for every component;
3. parses those declarations with `CSharpSyntaxTree.ParseText`;
4. adds them to a private copy of the input compilation for Razor's own tag
   helper discovery.

Those steps are visible in
[`RazorSourceGenerator.Initialize`](https://github.com/dotnet/razor/blob/58ec96978ef4e5823b54e960b9fd64cff45d7e68/src/Compiler/Microsoft.CodeAnalysis.Razor.Compiler/src/SourceGenerators/RazorSourceGenerator.cs#L36-L115).
After the remaining Razor phases run, the completed generated C# is registered
with
[`RegisterImplementationSourceOutput`](https://github.com/dotnet/razor/blob/58ec96978ef4e5823b54e960b9fd64cff45d7e68/src/Compiler/Microsoft.CodeAnalysis.Razor.Compiler/src/SourceGenerators/RazorSourceGenerator.cs#L247-L339).
It is part of the final application compilation, but it is not fed back into the
compilation observed by sibling generators.

This is not an ordering bug that Htmxor can solve by arranging analyzers in a
package. Roslyn's generator design says generators run unordered and ordinarily
have no access to files produced by other generators. The current
[incremental-generator cookbook](https://github.com/dotnet/roslyn/blob/7e23e9d7d1a90e50319cbe2be17ecda58e3a5c83/docs/features/incremental-generators.cookbook.md#L38-L49)
retains that rule.

## What `@attribute` means to Razor

Razor, not Htmxor, owns the directive grammar and placement rules.

The current source registers `@attribute` with the descriptor labels
`SingleLine` and `FileScopedMultipleOccurring` for ordinary Razor files,
components, and component imports such as `_Imports.razor`. See
[`AttributeDirective`](https://github.com/dotnet/razor/blob/58ec96978ef4e5823b54e960b9fd64cff45d7e68/src/Compiler/Microsoft.CodeAnalysis.Razor.Compiler/src/Language/Extensions/AttributeDirective.cs#L10-L30).
The `DirectiveUsage` enum XML comment describes file-scoped multiple directives
as existing "prior to any HTML or code." See its
[`documented contract`](https://github.com/dotnet/razor/blob/58ec96978ef4e5823b54e960b9fd64cff45d7e68/src/Compiler/Microsoft.CodeAnalysis.Razor.Compiler/src/Language/DirectiveUsage.cs#L8-L27).
That documentation does not describe the observed component behavior in SDK
10.0.400: Razor accepted multiline `@attribute` payloads after markup and before
a later component-local `@using`, then emitted all of them as effective class
syntax. The descriptor names therefore must not be treated as an executable
placement or line-count restriction.

For every recognized directive reference, Razor takes the directive's C# token
and inserts it ahead of the generated class. It preserves the source span. The
implementation is
[`AttributeDirectivePass`](https://github.com/dotnet/razor/blob/58ec96978ef4e5823b54e960b9fd64cff45d7e68/src/Compiler/Microsoft.CodeAnalysis.Razor.Compiler/src/Language/Extensions/AttributeDirectivePass.cs#L10-L38).
The official integration fixture demonstrates that Razor forwards qualified
names, multiple attributes, named arguments, and assembly targets into generated
C# rather than interpreting their argument spelling itself:
[`AttributeDirective.cshtml`](https://github.com/dotnet/razor/blob/58ec96978ef4e5823b54e960b9fd64cff45d7e68/src/Compiler/Microsoft.AspNetCore.Razor.Language/test/TestFiles/IntegrationTests/CodeGenerationIntegrationTest/AttributeDirective.cshtml)
and its
[`generated C# expectation`](https://github.com/dotnet/razor/blob/58ec96978ef4e5823b54e960b9fd64cff45d7e68/src/Compiler/Microsoft.AspNetCore.Razor.Language/test/TestFiles/IntegrationTests/CodeGenerationIntegrationTest/AttributeDirective_Runtime.codegen.cs#L16-L52).

Registering the directive for component imports is significant. Effective
component metadata can originate in `_Imports.razor`, not only in the component
file that Htmxor happens to scan. The bounded v1 fallback therefore includes the
root `_Imports.razor`. It rejects nested imports instead of pretending to
reproduce Razor's full import hierarchy.

## Executable SDK 10.0.400 probe

The temporary repository probe under `artifacts/pipeline-probe` exercises the
actual compiler behavior rather than inferring it from descriptor names. Its
ignored input `Consumer/Probe.razor` is:

```razor
<h1>Probe</h1>

@attribute [
    RouteAlias(
        ReportRoute,
        Methods = new[]
        {
            GetMethod
        })
]
@attribute [
    AuthAlias(
        Policy = ReportPolicy)
]

@using AuthAlias = Microsoft.AspNetCore.Authorization.AuthorizeAttribute
```

It contains, in order:

1. ordinary `<h1>` markup;
2. a multiline route `@attribute` whose type alias and constant arguments come
   from `_Imports.razor` and C# source;
3. a multiline authorization `@attribute`;
4. a component-local attribute-type alias declared by `@using` after both
   attributes.

SDK 10.0.400 compiled that component successfully. The ignored generated output
at
`generated/Microsoft.CodeAnalysis.Razor.Compiler/Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator/Probe_razor.g.cs`
contains the imported and component-local aliases, both multiline attribute
lists ahead of the generated `Probe` class, and the original markup in
`BuildRenderTree`. Its relevant shape is:

```csharp
using RouteAlias = Htmxor.HtmxRouteAttribute;
using static PipelineProbe.Consumer.RouteConstants;
using AuthAlias = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

[RouteAlias(ReportRoute, Methods = new[] { GetMethod })]
[AuthAlias(Policy = ReportPolicy)]
public partial class Probe : ComponentBase
{
    // BuildRenderTree emits <h1>Probe</h1>.
}
```

The attributes are effective class attributes despite their source placement
after markup and before the later `@using`.

A second SDK 10.0.400 probe put two adjacent C# attribute lists in one directive,
`@attribute [RouteAlias(...)][AuthAlias(...)]`. Razor rejected the second `[` with
`RZ1017`, "Unexpected literal following the 'attribute' directive. Expected a
new line." Retaining exactly one complete `AttributeListSyntax` per
`@attribute` matches that observed Razor boundary. It is not a claim about
untested directive forms or later SDKs.

The independent `ObserverGenerator/ObserverGenerator.cs` records the inputs
visible to its `CompilationProvider`. Under the default Razor source-generator
pipeline, `generated/ObserverGenerator/PipelineProbe.ObserverGenerator/ObservedInputs.g.cs`
lists `Probe.razor` and `_Imports.razor` only as additional texts. Its visible
syntax-tree list contains the ordinary C# and SDK-generated assembly files, but
not `Probe_razor.g.cs` or a `Probe` component symbol. The observer can bind a
synthetic C# class it adds to a copied compilation, which proves that Roslyn can
normalize the aliases and constants once valid class syntax is available; it
cannot obtain the actual Razor-generated component this way.

For comparison,
`generated-legacy/ObserverGenerator/PipelineProbe.ObserverGenerator/ObservedInputs.g.cs`
lists `Probe.razor.g.cs` and `_Imports.razor.g.cs` as ordinary visible syntax
trees when Razor source generation is disabled and Razor output is materialized
before C# compilation. It has no raw Razor additional text. That is evidence
that a two-stage build can expose the right semantic input, not evidence that
the default one-pass pipeline does.

### Final-compilation analyzer probe

A second ignored SDK 10.0.400 probe under
`artifacts/analyzer-pipeline-probe` packages one diagnostic analyzer and two
observer generators in the same analyzer DLL. Its component places multiline
route and authorization directives after markup and takes the route method and
policy from constants declared later in `@code`.

The generator's `CompilationProvider` reported the Razor component and an
ordinary sibling generator's output as missing; it saw only post-initialization
output. The diagnostic analyzer reported the Razor component, sibling output,
and post-initialization output as present. It read the exact bound values
`/probe/{Id:int}`, `[GET]`, and `policy.from.component.code` from
`AttributeData`. The route attribute's physical syntax reference pointed into
`ProbeComponent_razor.g.cs`, while `GetMappedLineSpan()` identified the original
multiline span in `ProbeComponent.razor`.

The consumer built successfully with SDK 10.0.400 and Roslyn 5.9.0. Packing the
observer confirmed that the analyzer and generators used the same ordinary
`analyzers/dotnet/cs` NuGet asset. This rules out analyzer ordering or a second
package as the explanation for the different visibility: diagnostic analyzers
receive the final compilation, whereas sibling generators do not.

## Why a sibling generator cannot bind the generated attribute yet

Standard source output is added only to the final compilation. It is not an
input to other standard generator phases. Roslyn's phased-generation design
states that `RegisterSourceOutput` and `RegisterImplementationSourceOutput` can
read the compilation, while their output is not fed back into it. See the
[pipeline order](https://github.com/dotnet/roslyn/blob/7e23e9d7d1a90e50319cbe2be17ecda58e3a5c83/docs/features/pre-compilation-source-outputs.md#L139-L166).

Roslyn's new `RegisterPreCompilationSourceOutput` fills this gap. Its output is
added to the initial compilation before compilation-dependent phases, and it is
visible across generators. The design document identifies Razor's private
declaration compilation as its primary motivation and proposes that Razor emit
its partial declarations as pre-compilation source. See
[the Razor motivation and proposed flow](https://github.com/dotnet/roslyn/blob/7e23e9d7d1a90e50319cbe2be17ecda58e3a5c83/docs/features/pre-compilation-source-outputs.md#L18-L48).

The API remains experimental and requires suppressing `RSEXPERIMENTAL007`, as
the
[Roslyn cookbook records](https://github.com/dotnet/roslyn/blob/7e23e9d7d1a90e50319cbe2be17ecda58e3a5c83/docs/features/incremental-generators.cookbook.md#L257-L302).
More importantly for Htmxor, the pinned Razor main source still uses the private
declaration compilation and implementation output described above, and the SDK
10.0.400 probe observes the same boundary. The Roslyn capability alone does not
expose Razor-generated component symbols in that toolchain.

## Chosen compiler-backed v1 seam

[`HtmxorRouteGenerator`](../../src/Htmxor.Generators/HtmxorRouteGenerator.cs)
does not inspect Razor content. It uses the additional-file paths and standard
Razor build properties to select project-root component filenames, sorts their
metadata names ordinally, and emits one application-local overload. The
overload passes that manifest, its own application assembly, and the caller's
exact `RouteGroupBuilder` to Htmxor runtime infrastructure. It contains no
route, policy, component `typeof`, or per-component endpoint registration.

[`HtmxorRouteDeclarationAnalyzer`](../../src/Htmxor.Generators/HtmxorRouteDeclarationAnalyzer.cs)
runs against the final C# compilation with generated-code analysis enabled. It
enumerates exact `Htmxor.HtmxRouteAttribute` symbols in the application assembly
and reads only `AttributeData.ConstructorArguments` and `NamedArguments`.
Aliases, local or external constants, explicit arrays, collection expressions,
raw strings, qualification, formatting, comments, and directive position are
therefore C# and Razor compiler concerns rather than Htmxor grammar.

The analyzer requires the supported issue #97 envelope: at most two concrete
project-root `IComponent` types, no normal component route, exactly one
constrained HTMX route with explicit GET-only methods and no other filters, and
one standard effective authorization policy without roles, authentication
schemes, or anonymous access. It reports one useful deterministic
`HTMXOR001` error at the generated attribute's Razor-mapped location for each
unsupported declaration. The diagnostic is nonconfigurable because omitting a
route or security constraint would silently change reachable application
behavior.

[`HtmxorAttributedRouteCatalog`](../../src/Htmxor/Builder/HtmxorAttributedRouteCatalog.cs)
uses public compiled metadata as the runtime data plane. It independently
checks exact attribute types and the generated project-root manifest, validates
and constructs the complete descriptor set before any endpoint is mapped, then
preserves each component's effective metadata on the endpoint created through
the supplied route group. This runtime check is not a parser and does not make
request headers authorization evidence.

The manifest and analyzer deliberately reject routed nested components and
namespace shapes outside this tracer. Malformed C# or Razor remains a compiler
error. Compiler-valid but unsupported Htmxor declarations remain `HTMXOR001`.
Neither case is silently dropped.

## Possible future platform simplification

If Razor later emits declaration C# as pre-compilation source, Htmxor could move
registration-value generation back into one semantic source-generator phase:

```csharp
context.SyntaxProvider.ForAttributeWithMetadataName(
    "Htmxor.HtmxRouteAttribute",
    static (node, _) => node is TypeDeclarationSyntax,
    static (attributeContext, cancellationToken) =>
        ReadRoute(attributeContext, cancellationToken));
```

Roslyn recommends `ForAttributeWithMetadataName` for attribute-driven
generators, including for its correctness and incremental performance. See the
[official cookbook guidance](https://github.com/dotnet/roslyn/blob/7e23e9d7d1a90e50319cbe2be17ecda58e3a5c83/docs/features/incremental-generators.cookbook.md#L108-L118)
and the
[`ForAttributeWithMetadataName` API contract](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxvalueprovider.forattributewithmetadataname?view=roslyn-dotnet-5.0.0).

The transform should select the matching `AttributeData` by symbol identity and
read:

- the route from `AttributeData.ConstructorArguments`;
- `Methods` from `AttributeData.NamedArguments`;
- each method from the array `TypedConstant.Values`;
- the target component from `GeneratorAttributeSyntaxContext.TargetSymbol`;
- all other effective metadata from symbols and bound attributes, not text.

Roslyn exposes constructor arguments and named property arguments as typed
constants through
[`AttributeData.ConstructorArguments`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.attributedata.constructorarguments?view=roslyn-dotnet-5.0.0),
[`AttributeData.NamedArguments`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.attributedata.namedarguments?view=roslyn-dotnet-5.0.0),
and
[`TypedConstant.Values`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.typedconstant.values?view=roslyn-dotnet-5.0.0).
At that boundary, both example `Methods` spellings have the same semantic array
value. Unsupported declarations can be rejected deterministically based on
their values and symbols instead of silently falling through a textual grammar.

Executable regression coverage for the current analyzer, and later for a move
to that platform seam, should include at least:

1. `new[] { "GET" }` and `["GET"]` producing byte-identical route models;
2. qualified and imported attribute names resolving to the same attribute
   symbol;
3. reordered named arguments and harmless whitespace/comments;
4. multiline `@attribute` directives after markup and component-local constants
   declared in `@code`, matching the SDK 10.0.400 probe;
5. effective aliases, constants, and attributes supplied by normal C# and Razor
   compilation context;
6. invalid or ambiguous C# failing from Roslyn syntax or binding evidence rather
   than a value inferred from text;
7. multiple or unsupported effective declarations failing closed without
   partial generated registration.

The package-consumer fixture keeps its second directive after markup and
`@code`, takes route, method, and policy from component-local constants, and
includes the text `@*` inside a C# string. Together with the SDK probe, it proves
the result is based on compiler semantics rather than a Razor text scanner.

## Current choices for Htmxor v1

| Choice | Compiler-backed | Keeps `@attribute` in `.razor` | Current supported seam | Cost |
| --- | --- | --- | --- | --- |
| Path-only generator, final-compilation analyzer, and compiled-metadata runtime catalog | Yes | Yes | Chosen and executable for the v1 envelope | Three narrow phases and a startup assembly scan |
| Wait for Razor pre-compilation declarations, then use semantic attribute discovery | Yes | Yes | No in the inspected source and SDK 10.0.400 default pipeline | Requires a Razor implementation and SDK-version gate |
| Put attributes on a user-authored `.razor.cs` partial class | Yes | No | Yes | Changes the public developer model |
| Load or redistribute the SDK's current Razor compiler | Potentially | Yes | No | Couples Htmxor to SDK-private, rapidly changing compiler internals |
| Locate or parse directives from raw `.razor` input | Partial | Yes | Rejected after review | Cannot cover component-local semantics or the Razor grammar reliably |
| Interpret attribute names or values directly from raw `.razor` text | No | Yes | Rejected | Cannot preserve legal C# equivalence or exact symbol identity |
| Disable Razor source generation so Razor output is materialized before C# compilation | Yes | Yes | Executable in the probe | Changes the project-wide build pipeline and still needs IDE/build/package parity evidence |

The Razor team explicitly froze the old compiler packages and stated that the
replacement compiler package would remain unpublished while its internals
changed, pending a vetted public API. See the official
[Razor compiler API announcement](https://github.com/dotnet/razor/issues/8399).
That rules out treating an SDK compiler assembly or the frozen
`Microsoft.AspNetCore.Razor.Language` package as a durable v1 integration
contract.

## Recommendation

Use the path-only generator, final-compilation analyzer, and runtime catalog for
the issue #97 contract. Keep collection expressions, explicit array creation,
aliases, component-local constants, lookalike attribute types, and multiline
post-markup directives as executable regressions. The generator must retain a
throwing `AdditionalText.GetText()` test so future changes cannot quietly
reintroduce a text parser.

Re-run the Razor/analyzer probe before claiming a new SDK or compiler. Keep
runtime reflection isolated in the catalog and do not claim publish trimming,
Native AOT, IDE live-analysis parity, or startup performance until those
boundaries are measured.

When Razor exposes its partial declarations through pre-compilation output,
replace the fallback with direct semantic discovery through
`ForAttributeWithMetadataName`. That could simplify registration generation,
but its absence in SDK 10.0.400 is no longer a blocker for this deliberately
bounded v1 design.
