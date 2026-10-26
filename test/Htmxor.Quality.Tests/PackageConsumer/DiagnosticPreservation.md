# Packed diagnostic preservation (#369)

Behavior start: `3b4a1b5e6ca8065b4d50bbabb14eab09b8647773`.

When a package consumer contains invalid component declarations or references
unavailable APIs, Htmxor preserves each case's required diagnostic ID, authored
location, diagnostic text, forbidden diagnostics and generated-registration
assertions on each applicable pinned framework while sharing compilation work.

## Configuration policy and evidence

Before: 40 ordinary failing configurations (38 logical cases), plus two
generator-disabled controls. After: four ordinary configurations, plus the same
two disabled controls. Every ordinary case now runs on both pinned frameworks;
the old analyzer cases ran on net10.0, while wrappers/retired API ran on both.
Successful consumers remain separately shared per framework from #363.

An actual all-38 build on each framework reported only the five source-generator
cases (`lambda`, `explicit-dynamic`, `explicit-method-call`, `explicit-computed`,
`explicit-implicit-get`). All 33 later compiler/analyzer cases were absent: 66
independent case assertions failed, ten passed. Separating those five cases
restored every mapped diagnostic: 76/76 case/framework assertions passed.
Therefore each framework has one five-case generator-error configuration and
one 33-case final-compilation configuration. No compiler/API-only split is
needed. This is an observed compiler pipeline boundary, not a product defect.

Disabled controls intentionally remove the actual packaged Analyzer item while
retaining Razor's compiler, and require CS1061 plus `AddHtmxorEndpoints`. Their
analyzer inventory conflicts with enabled-generator claims; they remain in
`PackedSuccessfulConsumerTests` on both frameworks.

The compiler, Razor SDK and packaged analyzer remain real. Each staged case
uses its original scenario writer, preserving authored line counts, real
`_Imports.razor` inheritance and matching/nonmatching partial filenames. Folder
and namespace identities isolate cases: `Htmxor.PackageConsumer` and
`Htmxor.Explicit.Anywhere` become `Htmxor.Diagnostics.Case<case-name-without-hyphens>`.
No synthetic `#line` input replaces imports or topology. Original helper types
and application sources remain available. The assembly-presence assertion reads
the project's actual TFM, including net11.0.

Results are immutable strings/arrays/dictionaries shared by independently
reported theory rows. Forbidden IDs are checked over **all** diagnostics for
the case before selecting expected IDs. Required ID, file, location and text
cannot be satisfied by another case. Every captured expected diagnostic must
remain inside its authored location range; a single valid match cannot cover
another captured location outside that range. API cases require every original
line, even while other cases keep the shared build red. Original package/binding
failure, missing assembly and package-only reference assertions are retained
(the package/API cases gain those common boundary checks).

## Before/after assertion map

Old names below identify the original test method or scenario. Source filenames
remain unchanged inside the named case folder. `B` means every captured binding
column is inside the handler at the stated authored line. `E` means every
captured explicit-declaration location is inside the original attribute/markup
anchor ranges. Concrete ranges and observed diagnostics are recorded per TFM
in each run's `diagnostic-mapping.json`; the source-derived anchors preserve the
old check rather than freezing incidental column values. `F` retains the old
filename assertion (no original numeric line/column constraint).

| Case | Original method/scenario | Source | Required ID | Location | Required text | Negative assertion |
| --- | --- | --- | --- | --- | --- | --- |
| two-routes | two_compiled_stock_routes | Issue100ReportPage.razor | HTMXOR038 | F | exactly one stock route and no HtmxRoute | — |
| lambda | computed_put_handler | Issue100ReportPage.razor | HTMXOR006 | F | lambda or closure | — |
| imports | marker_declared_in_Imports | _Imports.razor | HTMXOR003 | F | DisableHtmxDirectRouting declarations from _Imports.razor are not supported | — |
| methods | explicit_methods_conflict | Issue97ReportComponent.razor | HTMXOR033 | F | explicit HtmxRoute.Methods is authoritative | — |
| csharp-methods | CSharp_route_without_methods | Issue97SummaryComponent.cs | HTMXOR005 | F | a C# HtmxRoute declaration must explicitly declare HtmxRoute.Methods | Exact registration inclusion/exclusion below |
| partial | route_in_a_nonmatching_partial | Issue97ReportRoute.cs | HTMXOR004 | F | a C# HtmxRoute declaration on a Razor component must use the matching .razor.cs partial | — |
| explicit-invalid-method | invalid_explicit_declarations: invalid-method | Issue180Rejected.razor | HTMXOR029 | E | method (ignore case) | — |
| explicit-normal-only | invalid_explicit_declarations: normal-only | Issue180Rejected.razor | HTMXOR034 | E | DisableHtmxDirectRouting (ignore case) | — |
| explicit-base-owner | invalid_explicit_declarations: base-owner | Issue180RejectedBase.cs | HTMXOR030 | E | route (ignore case) | — |
| explicit-stock-form | invalid_explicit_declarations: stock-form | Issue180Rejected.razor | HTMXOR032 | E | form (ignore case) | — |
| explicit-htmx-form | invalid_explicit_declarations: htmx-form | Issue180Rejected.razor | HTMXOR032 | E | form (ignore case) | — |
| explicit-cross-source | invalid_explicit_declarations: cross-source | Issue180Rejected.razor | HTMXOR031 | E | handler (ignore case) | — |
| explicit-allowlist | invalid_explicit_declarations: allowlist | Issue180Rejected.razor | HTMXOR033 | E | Methods (ignore case) | — |
| explicit-dynamic | invalid_explicit_declarations: dynamic | Issue180Rejected.razor | HTMXOR006 | E | HtmxAction (ignore case) | — |
| explicit-method-call | invalid_explicit_declarations: method-call | Issue180Rejected.razor | HTMXOR009 | E | HtmxAction (ignore case) | — |
| explicit-computed | invalid_explicit_declarations: computed | Issue180Rejected.razor | HTMXOR010 | E | HtmxAction (ignore case) | — |
| explicit-implicit-get | invalid_explicit_declarations: implicit-get | Issue180Rejected.razor | HTMXOR015 | E | HtmxAction (ignore case) | — |
| explicit-no-route | invalid_explicit_declarations: no-route | Issue180Rejected.razor | HTMXOR014 | 5:17 exactly | HtmxAction (ignore case) | — |
| derived-receiver | inherited_helper_receiver | Components/Pages/Issue310Page.razor | HTMXOR042 | B10 | @onput | — |
| different-receiver | another_component_instance | Components/Pages/Issue310Page.razor | HTMXOR010 | B4 | @onput; computed expression | Forbid HTMXOR042 |
| missing-post | omitted_by_the_scanner: post | Components/Pages/Issue310Page.razor | HTMXOR042 | B4 | @onpost; no usable action is available; HtmxAction; one distinct message | — |
| missing-put | omitted_by_the_scanner: put | Components/Pages/Issue310Page.razor | HTMXOR042 | B4 | @onput; no usable action is available; HtmxAction; one distinct message | — |
| missing-patch | omitted_by_the_scanner: patch | Components/Pages/Issue310Page.razor | HTMXOR042 | B4 | @onpatch; no usable action is available; HtmxAction; one distinct message | — |
| missing-delete | omitted_by_the_scanner: delete | Components/Pages/Issue310Page.razor | HTMXOR042 | B4 | @ondelete; no usable action is available; HtmxAction; one distinct message | — |
| missing-query | omitted_by_the_scanner: query | Components/Pages/Issue310Page.razor | HTMXOR042 | B4 | @onquery; no usable action is available; HtmxAction; one distinct message | — |
| missing-get | omitted_by_the_scanner: get | Components/Pages/Issue310Page.razor | HTMXOR015 | B4 | @onget; no usable action is available; remove @onget; GET is implicit; one distinct message | Forbid HtmxAction (ignore case) |
| normal-binding | normal_only_component | Components/Pages/Issue310Page.razor | HTMXOR034 | B5 | @onput; no usable action is available; remove the binding or marker; one distinct message | — |
| conflicting-handler | another_handler_same_method | Components/Pages/Issue310Page.razor | HTMXOR042 | B4 | @onput | — |
| same-name | same_named_different_handler | Components/Pages/Issue310Page.razor | HTMXOR010 | B5 | @onput; computed expression | Forbid HTMXOR042 |
| script | inside_script_markup | Components/Pages/Issue310Page.razor | HTMXOR042 | B4 | @onput | — |
| component | component_tag: plain | Components/Pages/Issue310Page.razor | HTMXOR042 | B4 | @onput | — |
| generic-component | component_tag: generic | Components/Pages/Issue310Page.razor | HTMXOR042 | B4 | @onput | — |
| template | member_template | Components/Pages/Issue310Page.razor | HTMXOR042 | B8 | @onput | — |
| nested-helper | nested_helper_template | Components/Pages/Issue310Page.razor | HTMXOR040 | B12 | handler 'Save' must be an instance method on the request-owned component | Forbid HTMXOR042 |
| stock-form | own_named_form_outside_allowlist | Issue179Rejected.razor | HTMXOR033 | Line 3 | Methods; POST | — |
| route-template | malformed_HtmxRoute_template | Issue286StaticHtmxPage.razor | ASP0017 | F | There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character | — |
| wrappers | cannot_construct_detached_wrappers | DetachedWrappers.cs | CS0122 or CS1729 | Each of lines 8,9,10,11 | No original text assertion | — |
| retired-api | cannot_use_retired_request_API | RetiredRequestApi.cs | CS0122 or CS0234 or CS1061 | Each of lines 5,6,7,8 | No original text assertion | — |

For `csharp-methods`, the original good report inclusion becomes the exact
`"Htmxor.Diagnostics.Casecsharpmethods.Issue97ReportComponent"` inclusion.
The original invalid `"Htmxor.PackageConsumer.Issue97SummaryComponent"`
exclusion becomes the exact
`"Htmxor.Diagnostics.Casecsharpmethods.Issue97SummaryComponent"` exclusion.
The unrelated same-named Razor component remains in the `.Other` namespace.

## Sensitivity and limitations

Controlled output/assertion trials rejected 662 changes (331 per framework),
with the original immutable result green before and after each change: 76 each
for exit status, assembly presence, missing case ID and wrong case ID; 94 wrong
paths; 74 wrong lines; 56 wrong columns; 108 missing required text fragments;
14 extra distinct messages; six injected forbidden IDs; two injected forbidden
implicit-GET words; four registration changes. Every captured location was
perturbed separately, preserving other valid locations where present. These
trials manipulate observations; they are not real compiler mutations or Stryker.
The source and complete assertion exceptions are retained in the ignored
`artifacts/reviews/issue-369/sensitivity/` evidence.

Ten real package/source trials (20 expected assertion failures, two per trial)
independently expose each wrapper constructor,
retired routing enum/member, Method and Path, omit the valid registration and
insert the invalid registration. Every successful trial rebuilds the actual
package and reaches the owned assertion on both pinned frameworks. Temporary
source edits are restored immediately after each trial. A response-constructor
preparation attempt hit CA1062 before consumer compilation; that attempt is
retained separately and excluded from sensitivity counts. Its corrected trial
adds temporary null validation so the package compiles and the consumer's
missing diagnostic reaches the assertion.

The final handoff receipt identifies exact commands, candidate SHA, per-TFM
mapping output, successful real-trial counts and restored green evidence.
Cold fast/full timing and complete-change review belong to the Implementor's
next phase. No full-scope Stryker, hosted CI, browser behavior, fresh browser/OS
provisioning, Windows/macOS, deployment or package publication is established
by these focused compiler tests. The full browser timing floor is out of scope.
