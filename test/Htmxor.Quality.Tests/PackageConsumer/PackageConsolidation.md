# Slice A package claim ledger (#363)

The assertion/claim map is in [PackageClaims.json](PackageClaims.json). Exact-head
commands, raw failures, restored-green results and cold-profile measurements are
retained in the ignored complete-change verification receipt.

Comparison base: `8b6baa6b6ed1db1b93dc51c85b9e05eea60a6ab6`.
The historical before measurements precede the upstream license fix in
[#370](https://github.com/egil/Htmxor/issues/370), merged by
[PR #371](https://github.com/egil/Htmxor/pull/371). The before/after timing
comparison therefore includes that upstream packaging change.

`PackedPackageArchiveTests.Package_ships_the_exact_license_input` replaces
`PackedLicenseTests.Package_contains_the_unchanged_ASPNET_Core_license` from #370.
It retains entry presence and exact source bytes for the ASP.NET Core license and
also checks the repository LICENSE, using the existing shared package. Removing
the duplicate avoids an extra pack and reconciles its old workspace constructor.

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

## Assertion fidelity and package-break trials

The map records each independent archive, surface, signature, metadata, discovery
and consumer-boundary claim. `A*` variants remove/add actual archive entries or
forbidden dependencies; `L*` remove/corrupt each license independently; `S*`,
`M*` and `G*` alter packaged metadata or generator output; `T*` change selected
compile assets; `R*` add valid runtime assemblies. Each trial uses an isolated
package copy/cache. Source inversions are restored immediately after packaging;
no source change belongs to this branch. Both frameworks retain actual nested
failure evidence for constructor, factory, signature/delegate, alias and discovery
claims. Setup/compiler failures while preparing a trial are excluded from red
assertion evidence.

Historical #359 P assertion A257 requires raw response-header constant values to
be strings. The allowlist alone checks declared field types, so the retained
`Package_exposes_current_url_naming_and_core_request_surface` test also performs
the original `GetRawConstantValue`/`Assert.IsType<string>` guard against the real
package. `M-header-null` independently fails that assertion on both TFMs.

`C-total` omits a nested carrier and fails the total-count guard. `C-map` replaces
it with another passing case, keeps 21 total, and fails the exact class/count map.
These are test-orchestration controls, separate from package defects. `B-notice`
keeps all nested cases green but fails the unexpected HTMXOR build-output guard.
Exact SDK/runtime pins and the authored source/reference topology assertions are
dependency/fixture controls, not claims that an archive can modify the platform
or test-authored files. The real restore/build/run and unchanged controls retain
those boundaries.

The source comparison against the historical behavior start records identical
assertion expressions in 22 diagnostic methods/helpers, apart from the explicitly
migrated supported-build controls. Required/forbidden IDs, text, locations and
generated-source checks stay intact. #369 owns diagnostic grouping.

The output-only content-file variants keep dependency-file checks green and fail
only the output-file exclusion. Runtime-asset isolation temporarily omits the
preceding compile-asset check and observes a missing runtime property after a
successful real restore; the original helper is restored exactly. A preliminary
extra-DLL experiment did not change NuGet's selected assets and is excluded from
sensitivity success, as are setup failures. Raw evidence distinguishes these
observations from assertion failures and from the restored base-package controls.
