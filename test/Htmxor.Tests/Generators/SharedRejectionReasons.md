# Compiler/runtime rejection-reason fixture inventory

Issue [#334](https://github.com/egil/Htmxor/issues/334), behavior start
`e07e579274b861b8faae253ffd3bb40a9f2fce02`. This inventory refines the
[#333 inventory](https://github.com/egil/Htmxor/issues/333#issuecomment-6350339412)
and its [second part](https://github.com/egil/Htmxor/issues/333#issuecomment-6350335598).

When a declaration violates a rule enforced by both compiler diagnostics and
runtime catalogs, Htmxor presents the same canonical reason text at both
boundaries. When a previously valid declaration is compiled, Htmxor preserves
its generated route/action output and runtime validity.

## Observation boundary

The `HtmxorShared*ReasonTests` theories run each declaration twice: once through
real Roslyn analyzer/generator diagnostics, once through actual runtime catalogs
on the same compiled fixture types. Each emitted reason is compared with an
independent literal expected reason. Tests do not read production constants or
inspect production source. Diagnostic context, the standard inferred-action
advice prefix, handler identity, and terminal sentence punctuation are envelopes;
the entire remaining reason, including narrow remediation, is compared exactly.

C# inputs compile against real framework and Htmxor references. Razor inputs are
the existing unedited SDK-generated render/declaration partials and their authored
AdditionalText. Their capture provenance is in
[InferredActionFixtures](InferredActionFixtures/README.md) and
[FormDiagnosticFixtures](FormDiagnosticFixtures/README.md). Both input and
post-generator compilations must have zero compiler errors. The test emits the
input compilation to load authentic attributes and method metadata even when
Htmxor rejects the declaration. Rejected declarations cannot produce successful
manifests, so catalog inputs are supplied at the real catalog boundary using the
fixture's actual owner, route kind, method, handler, and named-form facts. This
exercises fail-closed validation; it does not claim a rejected generator emits a
manifest, or that runtime scans callback bodies or forms.

The canonical literal is generally the existing compiler reason, with handler
identity excluded. Already equal route-leading-slash, normal-only/route conflict,
and explicit-verb reasons are green characterization. Existing runtime aggregate
handler guards may select these precise subreasons without changing their
rejection union or `InvalidOperationException` behavior.

## Genuinely shared reasons

Each row is a reason, not just an ID. Input variations within a row test
important portions of the same predicate. `Route`, `Explicit`, and `Razor` refer
to the corresponding `HtmxorShared...ReasonTests` classes.

| Key / compiler ID | Paired fixtures and exact expected reason | Runtime boundary / equivalence limit |
| --- | --- | --- |
| route-target / 004 | Route: abstract, own generic arity, non-IComponent class; `the HtmxRoute target must be a concrete, non-generic Blazor component` | Attributed `ValidateComponentType`; runtime open-containing-type extension stays distinct. |
| route-blank / 005 | Route: whitespace and null template; `HtmxRoute must resolve one nonblank constant route template` | Attributed `ReadRoute`; actual bound string/null constructor argument. |
| route-leading-slash / 005 | Route: `reason`; `HtmxRoute template 'reason' is invalid: The @page directive must specify a route template. The route template must be enclosed in quotes and begin with the '/' character` | Attributed `ReadRoute`; stock grammar is a separate owner. |
| methods / 005 | Route: null, empty, case-insensitive duplicate, HEAD, null element; `explicit HtmxRoute Methods must resolve to a non-empty unique subset of GET, POST, PUT, PATCH, DELETE, and QUERY` | Attributed `ReadExplicitMethods`; malformed CLR argument representations remain runtime-only. |
| current-url / 005 | Route: javascript URI; `HtmxRoute named argument 'CurrentUrl' must resolve to a valid relative or absolute HTTP(S) URI or null` | Attributed `ReadOptionalString`; actual string value. |
| target / 005 | Route: two element identities; `HtmxRoute named argument 'Target' must resolve to a valid element identity or null or whitespace` | Attributed `ReadOptionalString`; actual string value. |
| targets / 005 | Route: invalid identity and null element; `HtmxRoute named argument 'Targets' must resolve to a constant array of valid element identities` | Attributed `ReadTargets`; actual array contents. |
| stock-route / 004 | Route: local and inherited stock Route; `an HTMX-only component cannot also declare a normal Blazor route` | Attributed `ValidateDeclaration` walks the same hierarchy. |
| normal-only-route / 003 | Route: local marker and HtmxRoute; `DisableHtmxDirectRouting cannot be combined with HtmxRoute on the same type` | Attributed `ValidateDeclaration`; marker's other placement rules stay compiler-only. |
| inferred-methods / 033 | Razor Allowlist: `explicit HtmxRoute.Methods is authoritative and does not allow the PUT binding; add the method or remove the binding` | Attributed conflicting non-stock generated action; owner/route-kind prerequisites retained. |
| inferred-normal-only / 034 | Razor Normal: `an inferred binding on a component marked with DisableHtmxDirectRouting is not supported; remove the binding or marker` | Generated `ValidateAction`; actual own marker. |
| inferred-unrouted / 014 | Razor Unrouted: `the component owns no route, so the binding has no action to declare; add @page or HtmxRoute, or remove the binding` | Generated owner guard's exact zero-stock/zero-HTMX subrule; separate from missing-manifest defense. |
| local-page-owner / 038 | Razor LocalPage: `a component with a local @page action must compile with exactly one stock route and no HtmxRoute` | Generated stock-action owner guard; local @page maps to UsesStockRoute=true. Compiler 014 owns the missing-all-routes case before 038. |
| nonlocal-stock-owner / 039 | Razor StockRoute: `a component action without a local @page cannot use a compiled stock route` | Generated non-stock-action owner guard's stock-count-positive subrule; not its whole union. |
| explicit-verb / 029 | Explicit GET: `HtmxAction method must be POST, PUT, PATCH, DELETE or QUERY` | Explicit `AddAction`. |
| explicit-owner / 030 | Explicit no route and inherited-only route: `HtmxAction must be declared on the component type that owns its route, not an inherited or unrouted owner` | Explicit `ValidateOwner` common no-local-route subrule; abstract/open/manifest checks stay separate. |
| explicit-normal-only / 034 | Explicit own marker: `HtmxAction is not supported on a component marked with DisableHtmxDirectRouting; remove the action or marker` | Explicit `ValidateOwner`. |
| explicit-ordinary / 035 | Explicit property setter, destructor, explicit interface implementation, captured local function and lambda: `is not an ordinary callback method; declare an ordinary instance method returning void or Task` | Explicit `IsOrdinaryHandler` overlap only; see operator/event distinctions below. |
| explicit-static / 013 | Explicit static method: `is static; make it an instance method` | Explicit aggregate handler-shape guard. |
| explicit-generic / 022 | Explicit generic method: `is generic; make it non-generic` | Explicit aggregate handler-shape guard. |
| async-void / 018 | Explicit async void: `is async void; return Task instead` | Explicit aggregate handler-shape guard, actual emitted state-machine attribute. |
| return / 016,019,026 | Explicit ValueTask, Task<int>, int: `returns a value; return void or Task` | Explicit aggregate handler-shape guard; preserve separate compiler IDs. |
| arity / 025 | Explicit two required HtmxEventArgs: `has unsupported parameters; use zero parameters or one required HtmxEventArgs parameter` | Explicit parameter guard; types are valid so only arity fails. |
| byref / 023 | Explicit ref, out, in: `has unsupported parameters; use one by-value HtmxEventArgs parameter` | Explicit parameter guard; reflected byref type is distinct from optional/wrong-type subrules. |
| optional / 024 | Explicit optional HtmxEventArgs: `has an optional parameter; make it required` | Explicit parameter guard; otherwise correct type. |
| parameter-type / 020,021,025 | Explicit EventArgs, object, int: `has a parameter that is not HtmxEventArgs; its parameter must be HtmxEventArgs` | Explicit parameter guard; preserve separate IDs and independent arity reason also using 025. |
| overload / 017 | Explicit two ordinary Save methods: `is overloaded; give the handler a unique name` | Explicit handler name count; inferred overload binding is a distinct compiler-only rule. |
| explicit-methods / 033 | Explicit PUT excluded by GET: `explicit HtmxRoute.Methods does not allow the PUT HtmxAction; add the method or remove the action` | Explicit `ValidateAllowList`; do not conflate with callback/form033. |
| handlers-conflict / 031 | Explicit Save and Other PUT: `PUT declarations on one route owner must name the same handler; select one handler` | Explicit `Build` then `Merge`; compiler retains both attribute emissions. |
| form-methods / 033 | Razor NamedConflict named POST form: `explicit HtmxRoute.Methods does not allow POST for its form; add POST or remove the form` | Attributed `CreateDescriptor`, supported explicit Methods plus genuine named POST form only. |
| named-generated-POST / 032 | Razor NamedConflict: `named forms and a POST action cannot share one route owner; use named forms or a POST action` | NamedForm `Validate`, genuine named form plus generated POST callback. |
| named-explicit-POST / 032 | Razor ConsumerValidEdit: `named forms and a POST HtmxAction cannot share one route owner; choose named forms or the action` | Explicit `AddAction`; explicit compiler attribute cause, distinct form companion/envelope. |

## Predicate reconciliation and intentionally one-sided rules

- **No inferred shape fabrication.** The generated-action runtime catalog validates
  route/manifest metadata, not reflected callback shape. The explicit catalog
  validates actual HtmxAction methods. Shared handler-shape positives here use
  real explicit attributes. Existing inferred tests preserve authentic bindable
  static, generic, async-void, Task<T>, optional, EventArgs/object/dynamic cases.
  ValueTask, int-return, byref and excess-arity callback conversions remain
  compiler-owned where Razor cannot bind them. They are not inferred positives.
- **Owner shape.** Compiler route-target arity is the type's own arity; runtime
  ContainsGenericParameters also rejects a non-generic nested type inside an open
  generic owner. Compiler explicit-owner checks route presence and IComponent;
  runtime additionally checks abstract/open types and manifest membership.
  New preservation tests require zero explicit compiler diagnostics for abstract
  and generic routed owners while runtime still rejects them. Compiler origin,
  top-level/import/file provenance, C# explicit Methods and Razor owner matching
  do not become runtime rules.
- **Generated owner union.** Zero routes is an actual shared subrule, exercised by
  the authentic Unrouted capture; it must not be conflated with the missing
  generated manifest guard. Local-page shape and nonlocal stock prohibition are
  separate rows. Multiple HtmxRoute attributes are compiler-owned CS0579 and
  runtime-only forged metadata, not a fabricated paired declaration. Runtime
  exact-one HtmxRoute, nulls, duplicate generated actions, assembly identity,
  manifest order, route processor shape and metadata construction remain defenses.
- **Forms.** Compiler form033 requires a valid POST form and a supported Methods
  set, even if the form has no name. The attributed catalog sees a named-form
  manifest after method validation. Only the named POST intersection is paired;
  EditUnnamed/PlainUnnamed remain compiler-only name and form033 causes. A bad
  Methods set is route005, not inferred form033. Invalid names and non-POST forms
  cannot be inferred to share this predicate. NamedFormCatalog checks manifest
  membership against generated POST; explicit POST conflict is a different
  catalog and compiler cause. These rows retain the narrower remediation.
- **Nonordinary methods.** Roslyn MethodKind and reflection are not equivalent.
  Operators are compiler035 but runtime rejects their static shape; the new
  preservation control requires that distinction. Field-event method attributes
  copy to two accessors, while compiler feedback is once per authored attribute;
  event accessor metadata also has independently invalid parameter shape. Those
  are not globally equivalent ordinary-method rules. An ordinary method marked
  SpecialName, CompilerGenerated, or named Finalize remains valid; controls
  protect against broadening the reflection guard.
- **Other one-sided ownership.** Lambda/factory/computed callback expressions,
  member ownership, inferred overload resolution, implicit GET, misplaced
  binding, form naming, completeness and mismatched source provenance are
  compiler rules. Stock ASP0017 grammar and runtime constraint resolution retain
  their owners. Malformed CLR attribute payloads and forged/constructed metadata
  do not justify new compiler diagnostics.

## Preservation and evidence scope

The focused pre-edit baseline selects all `Htmxor.Generators.Tests`, attributed
catalog and explicit catalog tests. It covers existing raw diagnostic
ID/location/multiplicity checks and generated-output snapshots, including valid
inferred actions and forms. The same selection runs after adding this contract.
New preservation controls exercise all four valid explicit signatures, ordinary
SpecialName/CompilerGenerated/Finalize methods, case-insensitive supported route
methods, null/empty/relative/absolute representation values, and runtime-only
abstract/open explicit owner rejection. Existing catalog tests retain manifest,
processor, duplicate, metadata construction, and stock routing defenses.

This is in-process Linux/net10.0 Roslyn/catalog evidence. Captured Razor output
is recompiled, not regenerated by an SDK invocation in this test. No editor UI,
net11.0 runtime, browser, package consumer, external host, performance or security
claim follows. Fast/full profiles, exact-head independent reviews and temporary
production-literal reversal belong to later implementation/green finalization;
no full-scope Stryker run is requested in this red-contract phase.

## Assertion-sensitivity plan (execution deferred)

This plan addresses `LR-d5e76d1-S001`. **No fault below has been injected or run
in the test-contract phase.** Execute only after implementation is green, in the
same Tester's separately assigned green-finalization phase. Keep the approved
expected reasons and fixture captures unchanged while perturbing production
observations. The Implementor controls temporary production edits; serialize
those edits with Tester work. Resolve the named emission/guard against that
implementation's exact HEAD rather than assuming its new shared-source filename.

Use one fault at a time and the smallest selected test method containing the
named representative. Cases sharing an assertion mechanism are grouped below;
a representative proves that mechanism, not mutation coverage of every guard.
A fault must compile and reach the specified behavioral assertion. Earlier
compilation, fixture-setup or zero-discovery failure is not successful sensitivity
evidence. If an earlier assertion masks the named check, narrow the perturbation
until that check is reached, or report the missing evidence without claiming it.

| Assertion family / selected test | Deliberate fault at the observed boundary | Expected failure before restoration |
| --- | --- | --- |
| Compiler canonical reason: `Compiler_rejection_emits_the_canonical_route_reason`, `Compiler_rejection_emits_the_canonical_explicit_reason`, `Compiler_rejected_inferred_owner_emits_the_canonical_reason`, and the three `Compiler_named_*` methods | In separate runs append ` [sensitivity]` to the emitted reason for route-blank, explicit-static, inferred-methods, form-methods, named-generated-POST and named-explicit-POST. Preserve ID, severity, location, count and contextual envelope. If a shared literal also changes runtime output, retain that as collateral evidence; run the named compiler selection. | Exact reason comparison fails at `AssertDiagnostic`; route/form selectors may still select by the unchanged expected substring. These representatives cover plain route text, stripped handler identity, stripped inferred advice, and both distinct form032 reasons. |
| Compiler severity: shared `AssertDiagnostic` | Change the explicit-static diagnostic's severity from Error to Warning without suppressing its emission or changing its ID/reason/location. | Explicit compiler test reaches `Assert.Equal(DiagnosticSeverity.Error, ...)` and fails. |
| Compiler stable IDs: route, explicit and Razor selection paths | In separate runs map route-blank005 to004, explicit-static013 to022, inferred-methods033 to034, and the explicit named-form032 cause to031. Preserve count, reason and locations. | Route/Razor `Assert.Single` finds no matching original cause; explicit `Assert.Equal("HTMXOR" + id, ...)` fails; explicit-form selection loses its032 cause. No new ID is allocated. |
| C# diagnostic locations: route and explicit compiler tests | Separately report route-blank at the class identifier instead of its HtmxRoute attribute, and move explicit-static's start one character into the authored action attribute while keeping its reason/ID/count. Separately change the reported C# path to `Wrong.cs` while retaining its span. | Route attribute-text prefix check fails; explicit expected-start sequence fails; `Owner.cs` path equality fails for either C# family. These are location faults, not edits to the compiled fixture source. |
| Razor primary paths: inferred-owner and named-form compiler tests | Separately report inferred-methods and form-methods with the same mapped span/reason/ID but path `Wrong.razor`. | The respective mapped `.razor` path equality fails after the cause is selected. Exact line/column policies remain covered by the unchanged existing baseline tests; these new assertions promise path preservation only. |
| Compiler count/multiplicity: route, explicit and Razor compiler tests | Separately duplicate route-blank005, duplicate explicit-static013, suppress just one of the two explicit conflicting-handler031 emissions, and duplicate the selected inferred-methods033 or named-generated-POST032 emission. | C# diagnostic-count assertions fail (including expected two031 emissions); Razor selected-cause `Assert.Single` fails. Do not deduplicate either actual result or TRX evidence. The explicit031 start-sequence check also requires both distinct authored attributes. |
| Runtime colon-delimited reasons: route and explicit runtime tests, Razor Allowlist, form-methods and named-explicit-POST | In separate runs restore the pre-change runtime wording for route-blank, explicit-static, inferred-methods, form-methods and named-explicit-POST, changing only the catalog's reason argument. If implementation uses one shared value, temporarily override only that runtime call site's value. | The respective runtime test reaches exact `AssertException` reason equality and fails; actual catalog rejection still has type `InvalidOperationException`. This is the required original-literal reversal, not a comparison between constants. |
| Runtime prefix-delimited reasons: generated-owner and named-form catalogs | Separately append ` [sensitivity]` inside the emitted reason for inferred-normal-only and named-generated-POST, retaining each contextual prefix. Separately change only each prefix's component/action identity to `wrong-owner`. | Reason faults fail exact equality in `AssertReasonAfter`; prefix faults fail its `Assert.StartsWith`. These exercise both generated-action and named-form envelope shapes, not the colon extractor. |
| Already-equal runtime rows | In three separate runs append ` [sensitivity]` to only the runtime reason for route-leading-slash, normal-only-route003, and explicit-verb029. Leave the compiler reason/fixtures unchanged even if the final implementation normally shares the value. | Each previously green runtime row fails exact reason equality. A reversal of route-blank alone does not discharge these three observations. |
| Runtime fail-closed outcome / exception type across catalogs | For one isolated rejection in each attributed, explicit, generated-owner and named-form catalog, temporarily throw `ArgumentException` instead of `InvalidOperationException`, preserving the reason. Do not bypass validation into an unrelated later guard. | The corresponding runtime test fails `Assert.Throws<InvalidOperationException>` before reason comparison; log this as exception-contract sensitivity, separately from wording sensitivity. |
| Valid declarations remain compiler-valid: both `Supported_*` preservation methods | Separately inject an otherwise well-formed Htmxor diagnostic for the valid `/valid` HtmxRoute and for the valid ordinary instance `Save()` HtmxAction. Retain successful C# compilation. | The respective preservation test's analyzer-result `Assert.Empty` fails. This detects new compiler rejection of valid declarations, rather than relying on a reason test for an already-invalid declaration. |
| Valid runtime route/handler acceptance: both `Supported_*` methods | Separately make route validation reject lowercase `get`, reject an otherwise accepted null representation, and make explicit validation reject the valid zero-parameter callback. Separately broaden `IsOrdinaryHandler` to reject `IsSpecialName`, the CompilerGenerated attribute, or every method named Finalize. | The corresponding valid-declaration case fails on the real catalog call instead of producing its single descriptor; the three ordinary-method controls catch the overly broad guards. Keep the emitted fixture types unchanged. |
| Successful descriptor cardinality and values: both `Supported_*` methods | Separately return no descriptor and two descriptors for the valid owner. In separate runs return a single descriptor with ComponentType changed to `typeof(ComponentBase)`, NormalizedRoute changed to `/wrong`, and (explicit action only) HttpMethod changed to GET. Keep fixture compilation and validation successful. | `Assert.Single` detects cardinality changes; owner equality, `/valid` equality and explicit PUT equality each detect their corresponding single-field fault. These are distinct checks: a cardinality fault cannot prove value checks were reached. |
| Runtime-only abstract/open explicit owners | Separately remove just the abstract-owner rejection and just the open-generic-owner rejection from explicit `ValidateOwner`, keeping manifest/route checks intact. Separately add either restriction to the compiler's explicit-owner predicate. | `Runtime_only_abstract_and_open_explicit_owners_still_fail_closed` fails its runtime `Assert.Throws` when an owner is accepted; the compiler-broadening fault instead fails its independent `Assert.Empty`. This protects both halves of the deliberately unequal predicate. |
| Operator's one-sided classification | Separately emit operator013 instead of its existing035 while retaining one compiler diagnostic; then restore. Separately classify the operator as nonordinary in runtime `IsOrdinaryHandler`, so its rejection contains the ordinary-method reason rather than the static-shape reason. | `Ordinary_operator_rule_remains_a_distinct_compiler_only_rejection` fails its exact035 equality for the first fault and `Assert.DoesNotContain("ordinary", ...)` for the second. A generic runtime throw alone does not prove this classification distinction. |
| Harness preconditions (not behavioral evidence) | In temporary test-only edits, invert the zero-compiler-error, Emit.Success, no-CS8785, and post-generator-zero-error expectations individually against one otherwise unchanged valid compilation/captured fixture. | Each corresponding harness assertion must fail; restore it immediately. Record these as guard/expectation inversions only. They do not count as meaningful red or production-regression coverage and do not replace any row above. |

For each row's distinct fault, record exact HEAD, changed symbol/diff, selected
case, command, discovered/executed/passed/failed/skipped counts and the actual
assertion failure. Use the repository's focused Release runner, for example:

```bash
dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj -c Release --filter 'FullyQualifiedName~HtmxorSharedExplicitReasonTests.Compiler_rejection_emits_the_canonical_explicit_reason' --blame-hang --blame-hang-timeout 5min
```

Substitute the named class/method for each row; avoid filters on truncated theory
display names. Save each fault's log/TRX under the green-finalization receipt.
Restore the exact pre-fault bytes and rebuild/rerun that same selection after
every fault: all its discovered tests must pass, with zero skipped and no
remaining fault diff. Do not stack faults or accept a restored result from a
prior fault's run. After all faults are removed, rerun the complete
`FullyQualifiedName~HtmxorShared` selection (currently118 cases) and the affected
936-case baseline selection together, then perform the separately required
fast/full gates. Record actual final counts if approved tests changed; do not
reuse these planned counts as execution evidence. Retain original red evidence
at `d5e76d161f5ab4db1995b8cb9a703974bee07270`. No full-scope mutation run is
added by this plan.
