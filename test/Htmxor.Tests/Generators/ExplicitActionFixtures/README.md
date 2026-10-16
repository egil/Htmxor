# Explicit-action compiler fixtures

`*.generated.fixture` and `*.declaration.fixture` are unedited Razor output from SDK
`11.0.100-rc.2.26504.105`, compiling the paired `*.razor.fixture` for `net10.0`
with `Microsoft.NET.Sdk.Web`, `OutputType=Library`, `RootNamespace=Consumer`,
the SDK-provided ASP.NET Core framework reference, and a reference to Htmxor.
The imports were `Microsoft.AspNetCore.Components`, `Htmxor`, and
`Htmxor.Components`. Capture used `EmitCompilerGeneratedFiles=true` and
`dotnet build -c Release` at `/tmp/issue332-razor-capture`.

Tests preserve generated operations and source mappings. `Shape` exercises
independent explicit faults in authored `@code`; `Conflict` retains the real
named-form operation and Htmxor event callback binding that both conflict
with the explicit POST declaration.

The SDK emits component members into `_razor.decl.g.cs` and rendering into
`_razor.g.cs`; the analyzer fixture compiles both authentic partial trees.
