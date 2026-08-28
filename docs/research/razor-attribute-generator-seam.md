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

Roslyn now has the exact platform seam that would solve this:
`RegisterPreCompilationSourceOutput`. It was designed primarily to let Razor
place its partial component declarations in the initial compilation. Such
declarations would be visible to Htmxor's normal incremental-generator phase.
However, the pinned Razor main snapshot still keeps those declarations private,
and the exact SDK probe has the same externally visible boundary. Depending on
the seam now would therefore require a Razor implementation change and a
compiler/SDK support decision. This is a statement about the inspected source
and toolchain, not a prediction about later Razor adoption.

Issue #97 uses a bounded fallback while that platform seam is unavailable. It
does not interpret C# attribute expressions. Htmxor locates only the Razor
directive boundaries exercised by the v1 contract, retains each complete C#
attribute list and `using`, and asks Roslyn to parse and bind them on a synthetic
partial component added to a copy of the application's compilation. It then
reads `AttributeData` and `TypedConstant` values only for exact attribute type
symbols. Anything the locator, parser, or semantic model cannot prove fails
closed without generating a partial registration.

This fallback is intentionally narrower than Razor. It covers project-root
components, the root `_Imports.razor`, and the observed `@attribute`, `@using`,
`@namespace`, and `@page` directive boundaries. It does not claim the full Razor
grammar, nested import behavior, or compatibility with an untested future SDK.
The SDK 10.0.400 post-markup and multiline probe below defines part of its
executable compatibility envelope.

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

## Chosen bounded v1 fallback

Htmxor now uses the smallest fallback that keeps C# interpretation in Roslyn.
It has two distinct stages.

First,
[`RazorDirectiveLocator`](../../src/Htmxor.Generators/RazorDirectiveDocument.cs)
scans `SourceText` for the supported directive keywords at the start of a line
after whitespace and outside Razor comments. It recognizes only `@attribute`,
`@using`, `@namespace`, and `@page`. It does not use a regular expression or
attempt to parse markup, code blocks, or the rest of Razor. For each
`@attribute`, it gives the remaining source to Roslyn and retains one complete,
diagnostic-free `AttributeListSyntax`. It also parses complete `@using`
directives with Roslyn. The locator's job ends at finding these observed Razor
boundaries.

Second,
[`ComponentAttributeBinding`](../../src/Htmxor.Generators/ComponentAttributeBinding.cs)
builds a synthetic partial class from the root `_Imports.razor` and component
usings and attribute lists. It uses the application's `CSharpParseOptions`, root
namespace, references, and existing symbols. It adds that syntax tree to a copy
of the application `Compilation`, gets the declared class symbol, and consumes
the resulting `AttributeData`. It requires every syntactic attribute to bind,
rejects syntax or typed-constant errors, and maps an attribute location back to
the original Razor file.

[`RouteDeclaration`](../../src/Htmxor.Generators/RouteDeclaration.cs) resolves
`Htmxor.HtmxRouteAttribute` and
`Microsoft.AspNetCore.Authorization.AuthorizeAttribute` by metadata name and
compares symbols with `SymbolEqualityComparer.Default`. It reads the route,
methods, and policy from constructor and named `TypedConstant` values. Attribute
aliases, constant aliases, `new[] { "GET" }`, `["GET"]`, multiline formatting,
comments, and combined attributes in one list therefore converge on their bound
values when they are legal in that context. Text checks for `HtmxRoute` exist
only to detect an unresolved route candidate and fail closed. They never extract
a route, method, or policy.

The supported Razor envelope stays narrow. Components and the optional
`_Imports.razor` must sit at the project root. Nested imports, `@page`, an
incompatible `@namespace`, malformed directives, unresolved attributes,
binding-count mismatches, and declarations outside the supported route and
authorization contract produce the deterministic unsupported-declaration
diagnostic. Htmxor does not silently omit a declaration it can identify but
cannot prove.

This design accepts compiler-equivalent C# forms inside each captured complete
attribute list. It does not claim that Htmxor implements Razor's grammar or that
the locator will match future SDK behavior without another executable probe.

## Preferred future platform seam

Once Razor emits declaration C# as pre-compilation source, Htmxor should stop
reading `.razor` text. Its route input should instead begin with the semantic
attribute provider:

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

Executable regression coverage for the bounded fallback, and later for a move to
the platform seam, should include at least:

1. `new[] { "GET" }` and `["GET"]` producing byte-identical route models;
2. qualified and imported attribute names resolving to the same attribute
   symbol;
3. reordered named arguments and harmless whitespace/comments;
4. multiline `@attribute` directives after markup and before a later local
   `@using`, matching the SDK 10.0.400 probe;
5. effective aliases, constants, and attributes supplied through
   `_Imports.razor`;
6. invalid or ambiguous C# failing from Roslyn syntax or binding evidence rather
   than a value inferred from text;
7. multiple or unsupported effective declarations failing closed without
   partial generated registration.

The current
[semantic-equivalence fixture](../../test/Htmxor.Tests/Generators/HtmxorRouteGeneratorTests.cs)
starts with markup, uses one multiline attribute list containing both supported
attributes, and places its local `@using` directives afterward. Together with
the SDK 10.0.400 probe, it encodes the observed post-markup and multiline
behavior rather than the descriptor's more restrictive wording.

## Current choices for Htmxor v1

| Choice | Compiler-backed | Keeps `@attribute` in `.razor` | Current supported seam | Cost |
| --- | --- | --- | --- | --- |
| Locate bounded directives, then bind complete C# lists in a copied `Compilation` | Yes for captured C# | Yes | Chosen and executable for the v1 envelope | Htmxor owns a small observed-boundary locator and must reject anything it cannot prove |
| Wait for Razor pre-compilation declarations, then use semantic attribute discovery | Yes | Yes | No in the inspected source and SDK 10.0.400 default pipeline | Requires a Razor implementation and SDK-version gate |
| Put attributes on a user-authored `.razor.cs` partial class | Yes | No | Yes | Changes the public developer model |
| Load or redistribute the SDK's current Razor compiler | Potentially | Yes | No | Couples Htmxor to SDK-private, rapidly changing compiler internals |
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

Use the bounded fallback for the issue #97 v1 contract. Keep its responsibilities
separate: locate only the proved Razor directive boundaries, then hand complete
C# attribute lists and usings to Roslyn. Generate a route only from exact
attribute symbols and bound `AttributeData`. Preserve the deterministic
unsupported-declaration diagnostic whenever syntax, binding, import scope, or
metadata falls outside the proved envelope. Never drop a declaration because a
text pattern did not fit.

Keep collection expressions, equivalent array creation, aliases, constants,
lookalike attribute types, root imports, and the multiline post-markup form as
executable regression cases. Re-run the Razor probe before claiming support for
a new SDK behavior. Do not broaden the locator into a Razor parser based on an
assumed grammar.

When Razor exposes its partial declarations through pre-compilation output,
replace the fallback with direct semantic discovery through
`ForAttributeWithMetadataName`. That remains the cleaner platform integration,
but its absence in SDK 10.0.400 is no longer a blocker for this deliberately
bounded v1 design.
