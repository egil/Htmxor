# Inferred-action Razor compiler fixtures

Paired authored Razor and unedited render/declaration partials captured with SDK
`11.0.100-rc.2.26504.105` at `/tmp/issue338-razor-capture`, using
`Microsoft.NET.Sdk.Web`, `net10.0`, `OutputType=Library`, `RootNamespace=Consumer`,
`EmitCompilerGeneratedFiles=true`, and a project reference to Htmxor at
`5f321bc58ac473902a31ad48b05adeabe293fbef`. Imports: Microsoft.AspNetCore.Components,
Htmxor, Htmxor.Components. Command: `dotnet build Fixtures.csproj -c Release`.
Capture reports the intentional Compiler.razor CS1503; every other fixture is
individually required to have zero compiler errors by the test harness.

Tests compile both real partial trees and observe raw generator + declaration +
compiled-binding diagnostics without deduplication. A stale AdditionalText input
simulates missing scanner declarations while preserving authentic compiled Razor.
Newly local semantic callback causes use the genuine mapped handler-expression
span in both full and local results, as approved by the repository owner:
https://github.com/egil/Htmxor/issues/338#issuecomment-6321855920.
Direct generator parse/conflict diagnostics retain their authored attribute spans;
genuine aggregate 042 diagnostics retain their existing compiled handler spans.
The independent expected ID/span multisets pin both Conflict031 binding locations,
and Cross042 specifically to the independent POST Other binding, excluding local042.
The earlier ignored Roslyn capability probe documents why an external attribute
primary location cannot provide source-tree-local semantic feedback; the owner
approval resolves that prior contract conflict without changing Razor output.

Review fixtures were captured unedited at `/tmp/issue338-review-capture` with the
same SDK and settings, using a binary reference to Htmxor at `5bda5fa`.
`dotnet build Capture.csproj -c Release` reports exactly four intentional CS1503
errors (ValueTask callbacks and an incidental invalid Factory.Create invocation).
Each compiler-invalid test requires its single CS1503 before diagnostic checks.
ReviewRepeated contains sixteen genuine bindings to one valid instance handler.
The owner-mismatch tests retain the original Parse/Conflict compiler output and
supply a different RootNamespace to both generator and analyzer: the scanned owner
then truly differs from the compiled component. Direct 015/031 and aggregate 037
retain original attribute spans; owner-dependent handler guesses stay suppressed.
ReviewRepeatedShape repeats a compiler-valid static async-void callback four times;
full and local raw multisets preserve both independent causes at all four lines.
