These are unedited generated C# and original Razor inputs captured with SDK
10.0.402, `Microsoft.NET.Sdk.Razor`, targeting net10.0. The capture project used
RootNamespace `Htmxor.Consumer`, a `Microsoft.AspNetCore.App` framework reference,
a reference to the baseline Htmxor assembly, `EmitCompilerGeneratedFiles=true`,
and `CompilerGeneratedFilesOutputPath=generated`.

Capture command: `dotnet build /tmp/issue331-razor-capture10/Capture.csproj -c Release`
from that directory with a global.json selecting SDK 10.0.402 and rollForward
disabled. Result: 0 warnings, 0 errors. Baseline: 1ac6c981edf6921db627a5a2429fe53e89f0f6f5.

Tests relocate only the captured source paths. Where a fault is under test,
they replace the route attribute or supply a separate authored C# partial;
render-tree generation stays the SDK's output.

`NestedOwner` is an additional unedited capture for accepted #331 / PR #336
Copilot finding discussion_r4281335760. It uses the same capture project, command
and pinned SDK10.0.402, with runtime assembly baseline
6676dbe78a04990ceadea3320a5db1da52d9e71e. Build result: 0 warnings, 0 errors.
Its Razor source has a valid outer route plus a concrete nested component with
valid explicit route arguments inside `@code`. Tests remove only the nested
attribute from both captured C# and authored Razor for the valid outer control;
the generated render-tree body remains the SDK's output.
