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
Existing scanned-binding diagnostics retain their authored attribute mapped spans
in the executable contract. Genuine aggregate 042 diagnostics retain the compiled
handler span; Cross pins that mismatch specifically to the independent POST Other
binding. Recoverable compiled-only bindings use independently specified expression
spans. Roslyn local semantic delivery with the preserved scanned attribute primary
span remains unresolved (review finding LR-4c1324f-P001); no location exception has
been approved. The ignored capability probe and exact-snapshot receipt document
this dependency constraint for the Implementor.
