# Stock-form diagnostic compiler fixtures

Captured with SDK 11.0.100-rc.2.26504.105 at /tmp/issue339-form-capture,
Microsoft.NET.Sdk.Web, net10.0, OutputType=Library, RootNamespace=Consumer,
EmitCompilerGeneratedFiles=true, CompilerGeneratedFilesOutputPath=generated.
The project references the Release Htmxor binary built from
12d0d93bb20005bb874e16b70d4b52e0ac104a27. Imports are retained as a fixture.
Command: `dotnet build /tmp/issue339-form-capture/Capture.csproj -c Release`.
This capture reports intentional InvalidOwner CS0103 and Repeated RZ10012
(uppercase FORM still lowers to a real element). The tests separately compile
both unedited generated partials for each component and require exactly its
specified compiler errors, before and after generation. InvalidName and
MixedInvalid each require CS0103 at their unresolved FormName expression.

All generated/declaration fixture bytes are authentic unedited Razor output.
The harness uses real Roslyn references and raw generator plus analyzer results.
Unrelated.cs independently introduces CS0246. Mismatch supplies scanner-only
stale AdditionalText while preserving the actual compiled form. Cross contains
an authentic Razor member template, preserving callback036 alongside form032;
it is a same-tree member-template control, not cross-source evidence.

Owner decision https://github.com/egil/Htmxor/issues/339#issuecomment-6336676592
preserves external authored locations. Static PlainUnnamed/Repeated causes are
complete-only; mapped EditForm/name-expression causes are local and complete.
Expected ID/reason/span multisets are explicit test data, with zero-based spans.
No Distinct/deduplication is applied to diagnostics.

The crossTree case partitions Cross's authentic lowered template property into
another C# partial syntax tree using Roslyn syntax APIs, retaining all lowered
operations and existing explicit Razor mappings. It removes that property from
the original render tree and retains its other members. Only the second class's
wrapper attributes/base list are omitted to avoid duplicate owner declarations.
This tests a genuine distinct-tree compiler relation; it does not claim the SDK
ordinarily emits that partition. No authored-primary mapping is synthesized.
The unpartitioned case preserves the original capture. A two-Razor-file attempt
was rejected because the SDK emits duplicate BuildRenderTree overrides; its
CS0111 is setup evidence only, retained in ignored cross-capture.log.

## Focused verification

Run from the repository root. The explicit-action filter uses the declared
`HtmxorExplicitActionAnalyzerTests` class, including the methods in
`HtmxorExplicitActionCompilerBoundaryTests.cs`; the filename is not a class name.
The original 307-case checkpoint selected zero explicit-action cases. Its 279
passing preservation cases covered route, inferred callback and stock-form tests
only; that historical run does not establish #332 preservation.

```bash
dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj -c Release --filter 'FullyQualifiedName~HtmxorFormDiagnosticTests|FullyQualifiedName~HtmxorStockFormCompilerTests|FullyQualifiedName~HtmxorStockFormRegressionTests|FullyQualifiedName~HtmxorInferredAction|FullyQualifiedName~HtmxorExplicitActionAnalyzerTests|FullyQualifiedName~HtmxorRoute' --blame-hang --blame-hang-timeout 5min --logger 'trx;LogFileName=issue339-focused.trx' --results-directory artifacts/reviews/issue-339/focused-results
```

Independently parse TRX case names and outcomes to require nonzero discovery for
`HtmxorExplicitActionAnalyzerTests`, and count its five compiler-boundary theory
methods separately. A matched test assembly does not prove this discovery.
At the original red-contract checkpoint the 17 known form failures were intentional;
passing preservation/control cases are green characterization, not new red.

The [assertion-sensitivity plan](assertion-sensitivity.md) names the bounded
inversions and restoration checks to execute after production implementation.

## Complete-change review regression capture

The ReviewPlainInvalid, ReviewInvalidInference and ReviewDescendant paired
fixtures were freshly captured, byte-for-byte, at /tmp/issue339-review-capture
using the same pinned SDK and Web/net10.0 capture settings above. Command:
`dotnet build /tmp/issue339-review-capture/Capture.csproj -c Release`.
This project references the Release net10.0 Htmxor.dll from the exact curated
1de69786636e8e216533106b509c6bb3959690c4 cold archive. It excludes generated/**
from Compile. Project, imports, command log and SHA256 inventory are retained
under artifacts/reviews/issue-339/review-regressions (ignored evidence).

Both plain fixtures require exactly CS0103 in the separate __formName
initializer. ReviewPlainInvalid retains its real same-owner POST callback:
form033 and callback033 survive; guessed032/028/043 do not. The inference
control keeps authentic compiled output but omits the scanner form, using the
existing stale-AdditionalText seam: invalid names cannot establish aggregate043.
ReviewDescendant requires exactly CS0103 plus CS1662 in ChildContent; the
compiler errors are prerequisites, not red evidence. Its expected033 uses the
existing primary selector's first mapped ChildContent descendant, the authored
lowered expression statement (zero-based 2:11-2:19), rather than inventing a FormName
primary. Original InvalidName/MixedInvalid retain mapped FormName 1:24-1:32.
All these mapped causes are required locally and completely, including the
new fixtures with an unrelated CS0246. InvalidOwner remains compiler-owned.

Compiler-error IDs are compared as raw sorted multisets before and after
Htmxor generation, including both body errors and any unrelated-source error.
New review regressions supersede the former invalid-name empty expectation;
previous green and red receipts remain historical evidence.

The ReviewDescendant end column is the selected ExpressionStatement span, not
only its IdentifierName child. In the retained lowering the first mapped node
under ChildContent is `__builder2.AddContent(5, missing ... );`, syntax span
[2580..2677), first token __builder2, last token semicolon. Roslyn maps that
whole statement to 2:11-2:19; its `missing` child [2605..2612) maps to 2:11-2:18.
The authored line is `    <span>@missing</span>`: the mapped statement interval
includes the following `<`, while the identifier interval does not. The SDK's
enhanced #line mapping supplies this result; fixtures are not rewritten.
FindMappedLocation, FindNamedEventLocation and GetLocation are identical in
1de6978, e2820b8 and aa07bf0. The original end18 expectation used the child's
span in error; end19 preserves the existing primary-location selection.

## Explicit-action consumer regression (Copilot4351679432)

ConsumerInvalidEdit/ConsumerValidEdit pair an authentic EditForm name with a
real valid private parameterless `[HtmxAction("POST")]` on the same owner.
ConsumerInvalidPlain covers the shared reader's separately lowered name
initializer. ConsumerInvalidExcluded has known GET-only Methods, preserving
both the independent form033 and action033 reasons despite its bad name.
Each has a paired actual SDK render/declaration capture, unedited, from
/tmp/issue339-consumer-capture with the same pinned Web/net10.0 settings.
Command: `dotnet build /tmp/issue339-consumer-capture/Capture.csproj -c Release`.
Reference: Release net10.0 Htmxor.dll built in the exact f547710 cold archive.
Project/imports/global.json, raw capture log and hashes are retained in ignored
artifacts/reviews/issue-339/copilot-regressions.

The capture intentionally reports exactly three CS0103 errors, one for each
invalid component; the valid pair has none. Each harness run separately checks
its exact compiler-ID multiset before and after real generators, adding only
CS0246 when the unrelated source is requested. It then compares raw complete
results from generators plus route/action-declaration/compiled-binding analyzers.
HtmxorActionDeclarationAnalyzer's compilation-end callback actually invokes
explicit-action conflict validation; the valid pair requires genuine032 once,
so omitting that analyzer cannot create a false green. Its primary is the
original HtmxAction AttributeSyntax, authored zero-based4:5-4:23, excluding the
square brackets. Invalid names must not create032 there. GET exclusion keeps
form033 at original FormName1:24-1:32 and action033 at4:5-4:23, once per reason.
The original authoring/mapping policy and all existing579focused cases remain.
These eight new full-analysis cases cover each fixture with/without unrelated
error. They do not introduce another local scheduling contract for aggregate032.
