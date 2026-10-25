# Slice A package claim ledger (#363)

**Status: implementation in progress; blocked by the omitted ASP.NET Core license.**

The proposed mappings below await a passing focused run, package-break sensitivity,
restored green, final cold profiles, and independent review.

Behavior start: `5e6a239425b440f574328c8ace076fce2e6e35b6`.

When the suite runs, Htmxor preserves every remaining real-package claim on both
supported TFMs with one session package and one successful consumer per TFM,
while migrated behavior runs at its in-process boundary.

`PackedPackageFixture` owns the package until the Packed collection is disposed.
Every workspace references that immutable local feed and uses its own package
cache. The Htmx4 browser/publish collection retains its existing lifecycle.

## Success consumers

`PackedSuccessfulConsumerTests` performs one successful restore/build/test with
SDK/runtime 10.0.402/10.0.13 and one with
11.0.100-rc.2.26504.105/11.0.0-rc.2.26504.105; roll-forward is disabled.
Each nested run discovers the same 21 independently reported package tests.
The existing failing configurations remain independent; #369 owns their grouping.

| Former boundary | Retained inputs and assertions |
| --- | --- |
| Default package runtime | All Razor, all-C#, and matching code-behind inputs; later page-like comment; five surface/registration assertions in `PackageConsumerTests`, plus four independent component discovery rows |
| #168 / #169 | Original source and one discovery probe per component |
| #287 | Both original guard/form sources and form-component discovery |
| #291 / #293 | Original Kestrel host/page, Kestrel discovery, integer signature and delegate binding; #291 page compiles the integer delegate helper |
| #299 | Original component, four wrapper constructor rows, factory presence/visibility/return metadata |
| #209 | Exact core/framework identity and genuinely typed generated GET; its route is prefixed `/issue-209` to coexist with the default report route |
| #175 / #179 / #180 | Original normal-only, form and explicit-action compiler inputs; explicit-action `Areas/Unusual-Folder` layout |
| #283 | Original fixture folder/import/code-behind inputs and actual MinimalHtmxorApp imports/layout/Home |
| #284 / #285 | Original authorization shapes and complete folder/import/namespace precedence topology |
| #286 / #306 / #309 / #316 | Original route grammar, owner markup, agreeing bindings and convention-authoring inputs |
| #310 plain/generic | Both original supported shapes coexist with distinct type/route names; handwritten RenderFragment callback also compiles with no HTMXOR diagnostics |
| #298 / #299 supported controls | `SupportedRequestApi.cs` and the wrapper component compile in the success consumer before the independent retired-API/inaccessible-wrapper probes |

Compiler-only scenario tests remain compiler inputs but are excluded from the
nested runtime filter, matching their former build-only boundaries. All original
runtime package assertions execute. The generator-absent control still removes
the actual analyzer item, proves Razor's compiler remains, and checks CS1061 plus
AddHtmxorEndpoints independently for both TFMs.

## Archive, assets and metadata

The same nupkg supplies both DLL allowlist checks, retired conditional/request
surface checks, generator DLL/PDB, both runtime DLLs, no net8 DLL, static asset,
no bundled htmx/legacy extension, no runtime generator/Roslyn assemblies, and
nuspec dependency exclusions. Each success consumer retains exact package
references, no ProjectReference/friend access, selected compile/runtime assets,
analyzer inventory, dependency-file and output-assembly exclusions. Both license
entries are checked against the exact bytes of their actual pack inputs.

The original default-source topology guards use only the default compiler input
files so unrelated coexisting scenario applications cannot change their counts.
Both root/grouped registration forms remain compiler inputs in
`PackageRegistration.cs.template`.

## Removed shared-template runtime replay

These removals rely on #362's 281-entry assertion ledger (224 owned, 36 controls,
21 additional coverage) and #359's 282-entry ledger (18 package entries).
The four shared routing carriers account for 124 ledger entries. Historical
sensitivity evidence retains its historical SHA; current cold-profile evidence
is recorded in the ignored exact-head receipt.

| Removed runtime methods/input | Specific replacement in `Htmxor.AspNetCore10.Tests/Issue362/Routing` |
| --- | --- |
| Report/Summary/Third metadata and route-selection methods | `Issue362RoutingActionsTests`: three component metadata methods; `Component_route_preserves_representation_policy_host_and_constraint_rejection`, matching/mismatched representation theory rows |
| Grouped stock/generated paths | `Issue362RoutingActionsTests.Group_owns_stock_and_generated_paths_without_unprefixed_duplicates` |
| Manual render-tree allowlist, PUT/DELETE/PATCH instance behavior, antiforgery, authorization, wrong method and wrong route | Same-named methods in `Issue362RoutingActionsTests`; loop inputs expanded in that carrier |
| Root registration HTTP/metadata replay | `Issue145RootRegistrationTests.Root_registration_preserves_stock_and_generated_component_endpoints` |
| Separate actionless-unsafe consumer and scenario | `Issue103ActionlessUnsafeTests` for render/navigation and net10 token rejection; `Issue103NativeProtectionTests` for net11 headerless allowance and cross-site rejection |

The dual-TFM project explicitly compiles `Issue362/**/*.cs` and includes its Razor
inputs on net11; net10 includes the same inputs through its normal Razor glob.
The net10/net11 actionless distinction remains explicit in those carriers.
No Htmx4 fixtures or diagnostic expectations are rewritten by this slice.
