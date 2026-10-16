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
Semantic causes use the source-tree handler expression in both complete and local
results; generator-owned parse/conflict causes retain the authored attribute span.
This narrow semantic location change from external attribute location requires
explicit test-contract review against the brief's location-preservation constraint.
