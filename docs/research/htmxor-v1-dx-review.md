# Htmxor v1 developer-experience review

- Review date: 2026-08-31
- Repository baseline: `864f3d6451c240b7de9a43e84a73c673fd3c7053`
- Starting discussion: [#143, “WIP: new devex”](https://github.com/egil/Htmxor/discussions/143)
- Product contract: [Htmxor v1 goal](../roadmap/v1/goal.md)
- Feature inventory: [Htmxor v1 developer guide and htmx 4 feature map](../htmxor-v1-feature-guide.md)

## Review question and protected behavior

This review asks three questions of every public authoring path:

1. Is the path obvious from the API and normal Razor/Blazor knowledge, or must a
   developer discover a hidden convention in documentation?
2. Can Htmxor remove ceremony, ambiguity, or a dangerous default so the common
   path is the safe path?
3. Is the API surface consistent in naming, authority, argument behavior,
   extensibility, diagnostics, and ownership?

The behavior this review protects is:

> When a developer progressively enhances a Blazor static-SSR component,
> Htmxor keeps its route, callbacks, lifecycle, forms, authorization, and HTML
> in that component while making the intended HTTP representation evident from
> the declaration.

The review considers the agreed v1 plan over the prototype wherever the two
differ. It does not propose controllers, Minimal API duplicates, static action
handlers, private renderer code, reflection over private framework state, HTMX
headers as authorization, or an Htmxor-owned htmx runtime.

## Method

The review followed these surfaces from authoring to wire behavior:

- application registration and runtime ownership;
- normal-only, HTMX-only, and dual component routes;
- implicit GET, stock forms, and component-owned POST/PUT/PATCH/DELETE/QUERY
  callbacks;
- source-generator discovery and diagnostics;
- full output, one fragment, several fragments, OOB output, and
  `<hx-partial>`;
- `HtmxRequest`, `HtmxResponse`, `HtmxContext`, and callback event arguments;
- the public trigger/swap/constants helpers;
- authorization, antiforgery, validation, caching, redirects, errors, history,
  concurrency, and interactive-Blazor boundaries; and
- the complete official htmx 4.0.0 attribute, header, configuration, event,
  JavaScript, CSS, and extension inventory.

The official [htmx 4 documentation](https://four.htmx.org/docs/),
[reference](https://four.htmx.org/reference/), editor metadata, and
[extension catalog](https://four.htmx.org/extensions/) were used as the client
completeness checklist. A small .NET 10 Razor compile probe confirmed that
representative colon forms such as `hx-status:422`, `hx-on:click`,
`hx-sse:connect`, `hx-live:text`, and `<hx-partial>` can be authored as raw
markup. That compile probe proves syntax acceptance only, not runtime behavior.

## Executive verdict

The v1 component-owned model is strong and more teachable than the prototype:
developers should ordinarily add htmx attributes to an `@page`, a stock form,
or an element with an instance callback. They should not have to create an
endpoint layer or learn a second rendering model.

The API is not ready to freeze, however. Four cross-cutting inconsistencies
would make documentation compensate for avoidable design ambiguity:

1. Setup mixes `Htmx` and `Htmxor` naming and currently exposes route-group
   plumbing.
2. Route/action discovery has false affordances and insufficiently specific
   failure diagnostics.
3. Server fragment selection is entangled with DOM identity and request-target
   headers, especially for multi-target responses.
4. The public package mixes the intended server model with incomplete client
   DSLs and infrastructure/prototype types.

The correct default is raw htmx markup plus a small, consistent, typed server
surface. Mirroring every htmx attribute, trigger, swap value, extension, and
configuration option in Htmxor would create permanent version lag without
making Razor clearer.

## Surface scorecard

| Surface | Is the path obvious? | Pit-of-success opportunity | Consistency verdict |
| --- | --- | --- | --- |
| Add services | Mostly; `AddHtmx()` reads like it adds the client runtime, although it adds Htmxor services | Name the product consistently and state that the app supplies htmx | Needs decision |
| Map endpoints | No; an empty route group is implementation plumbing | Complete [#145](https://github.com/egil/Htmxor/issues/145) so the common call is parameterless | Planned fix |
| Supply htmx | Yes after documentation says the app owns it | Make missing/wrong ordering diagnosable without selecting a CDN or package manager | Consistent with v1 |
| Dual `@page` GET | Yes; convention follows Blazor | Keep GET the only implicit method | Strong |
| Normal-only page | No public marker is frozen | One explicit, component-local opt-out with an obvious name | V1 gap |
| HTMX-only route | Mostly; `HtmxRoute` is discoverable | Make Razor, code-behind, and C# forms equivalent and diagnose `_Imports` misuse | Good concept, incomplete affordances |
| Method reachability | Common static callbacks are readable | Infer only from server intent; require a narrow explicit declaration for dynamic cases | Strong authority model |
| Callback execution | Yes; instance callbacks preserve Blazor expectations | Prefer parameterless callbacks and offer context only when needed | Strong |
| Stock forms | Yes to a Blazor developer | Preserve normal submit, validation, antiforgery, and lifecycle with no parallel form API | Strong |
| Unsafe methods | Mostly; HTML itself cannot express PUT/PATCH/DELETE forms | Render/transport stock antiforgery credentials and fail closed before callbacks | Strong if diagnostics and evidence stay complete |
| Whole component response | Yes | Convention should require no fragment declaration | Strong |
| One fragment | Partly; `HtmxFragment` is understandable | Give selection a stable name independent of DOM wrapper identity | Needs redesign |
| Several fragments | No; `Match` lambdas and rendering flags hide intent | Select named fragments once, then use raw OOB/partial delivery markup | V1 gap |
| OOB and `<hx-partial>` | Yes if kept as htmx markup | Do not introduce a competing Htmxor component hierarchy | Strong composition rule |
| Request context | Mostly | Use .NET naming, explicit invalid states, and an extension-header seam | Needs consistency pass |
| Response headers | Mostly; fluent names are readable | Make validation, HTMX-only guards, body effects, URL overloads, and extension escape hatches uniform | Needs consistency pass |
| Raw htmx attributes | Yes; this is htmx's native model | Add profile-aware diagnostics without rejecting unknown syntax | Strong default |
| Trigger/swap builders | No; they look authoritative but omit htmx 4 values and carry older syntax | Remove from the stable core or make them optional profile adapters with raw escape hatches | Do not freeze as-is |
| Layout/async helpers | Requires Htmxor-specific concepts | Retain only behavior that cannot be clearer with stock components and explicit fragments | Re-evaluate before freeze |
| Client extensions | Yes as raw application-owned markup | Add bounded server protocol hooks only where an extension sends/needs server data | Strong composition rule |
| Cache/history/error behavior | Not obvious from local markup alone | Provide recipes, analyzers where possible, and executable conformance; do not hide general HTTP policy | Documentation plus evidence required |
| Interactive Blazor coexistence | Not locally obvious | Document one DOM/navigation owner per boundary and supply tested patterns | Existing [#57](https://github.com/egil/Htmxor/issues/57) remains relevant |

## Detailed findings

### 1. Registration exposes two vocabularies and one implementation detail

Current setup uses `AddHtmx()` but
`AddHtmxorComponentEndpoints(htmxorRoutes)`. The first name can reasonably be
read as installing the htmx browser runtime, which v1 explicitly does not do.
The second makes every app create an empty route group solely to pass Htmxor's
plumbing back to Htmxor.

[#145](https://github.com/egil/Htmxor/issues/145) correctly removes the route
group from the common call. Before v1 freezes, choose one product vocabulary and
one level of specificity. The clearest candidate pair is:

```csharp
builder.Services.AddRazorComponents().AddHtmxor();
app.MapRazorComponents<App>().AddHtmxorEndpoints();
```

Keeping the current names is also possible, but the documentation would always
need to explain that `AddHtmx()` does not add htmx. A rename before stable v1 is
cheaper than carrying that ambiguity indefinitely. Advanced route-group
customization, if retained, should be a separately named overload rather than a
required empty-group ritual.

Acceptance should compile both a minimal app and an app whose Razor-component
builder has authorization, rate-limit, host, cache, and arbitrary endpoint
conventions, and prove the generated representations inherit the effective
conventions.

### 2. `HtmxRoute` advertises properties the v1 generator rejects

`HtmxRouteAttribute` currently exposes `CurrentURL`, `Target`, and `Targets`.
The source-generator v1 path rejects non-`Methods` named arguments with the
generic `HTMXOR001` diagnostic. This is a high-friction false affordance:
IntelliSense tells a developer a declaration is supported, then a distant build
step says the route is unsupported without identifying a stable replacement.

The properties also encourage routes to be selected by client-supplied browser
state or DOM identity. `HX-Current-URL`, `HX-Source`, and `HX-Target` are useful
representation hints, but URLs plus HTTP methods should identify the server
capability. Extended htmx selectors such as `closest`, `next`, and `find` do not
require target IDs, further weakening target identity as a primary route key.

V1 should either implement a narrowly justified property through the full
pipeline or remove it from the public declaration. Do not leave public members
that are guaranteed build errors. Prefer component code or explicit fragment
selection for representation decisions after route authorization.

### 3. Route and callback discovery needs cause-specific diagnostics

The convention is simple: GET is implicit, stock forms and statically
discoverable instance callbacks expose additional methods, and dynamic cases
need an explicit declaration. The current source parser proves only a bounded
set of Razor text shapes and reports two broad diagnostics. A developer can
otherwise have markup that looks correct but no generated action.

The generator should diagnose at least these cases separately:

- `HtmxRoute` in `_Imports.razor` or a non-component/global location;
- a C# route with no explicit methods;
- unsupported, malformed, duplicated, or contradictory `Methods` values;
- a dynamic callback expression that cannot be resolved statically;
- multiple callbacks competing for the same method/action;
- a client method attribute whose statically known method has no server action;
- an unsafe action with no available stock antiforgery credential path; and
- a route template or component shape outside the supported compiler seam.

Each diagnostic should point to the authoring location, state what Htmxor did
not generate, and give the narrow remediation. Unknown client attributes or
extension values must not become build errors. Code-behind and pure-C#
components require equivalent declaration tests, not a Razor-only happy path.

The normal-only opt-out belongs in the same reachability model. It should be one
component-local declaration and should conflict clearly with an HTMX-only
declaration rather than relying on ordering or request headers.

### 4. Fragment selection conflates execution identity and DOM delivery

`HtmxFragment` is the right single concept, and the v1 goal is right to reject a
second `HtmxFragmentElement` abstraction. Its current selection API is not yet
the right concept boundary.

Today `Id` can both request a wrapper and act as the default direct-request
selection key. `Match` accepts an arbitrary predicate over request headers.
`RenderDuringStandardRequest` adds another branch. A wrapperless fragment with
no ID matches every direct target by default. Multi-fragment responses therefore
require readers to mentally execute predicates and flags, and DOM renaming can
silently change server work.

The amended discussion's current multi-target example is particularly easy to
misread: an `<hx-partial>` envelope describes where htmx delivers returned
markup, but it does not by itself cause the enclosing server fragment to be
selected. Documentation cannot repair that missing declaration.

V1 should separate:

- a stable server selection name;
- optional wrapper element, ID, and attributes;
- whether the fragment appears in the normal full representation; and
- client delivery through the primary swap, OOB markup, or `<hx-partial>`.

One direct request should be able to select the whole component, one name, or an
ordered set of names without a `Match` lambda. The selection must be bound to
the authorized component route and must define duplicate/unknown-name behavior.
Excluded child branches below a known boundary must not execute their own
lifecycle or data work; the owning component and necessary ancestors may.

Do not infer the multi-selection list from `HX-Target`, CSS selectors, or raw
partial markup. Those concern delivery, are untrusted, and cannot reliably name
server execution boundaries.

### 5. Raw markup is a better default than a complete client DSL

The package currently exposes `Constants`, `Trigger`, `TriggerBuilder`,
`TriggerModifierBuilder`, `SwapStyle`, `SwapStyleBuilder`, related extensions,
and supporting enums/records. These types imply a complete, version-aware
client contract. Against htmx 4.0.0 they are not complete:

- swap styles omit htmx 4 aliases, morph styles, `outerSync`, `textContent`, and
  extension styles;
- swap modifiers omit newer options and some emitted selector syntax follows an
  older grammar;
- trigger builders omit htmx 4 event modifiers and intersection options while
  retaining older-version concepts; and
- a closed enum cannot represent an application extension without falling back
  to a raw string in a different API path.

Razor already accepts the native attributes and colon syntax. The official htmx
editor metadata is a better source of client completions than a server package
release. Htmxor should therefore make literal htmx markup the documented common
path and choose one of two stable dispositions:

1. remove the client DSL from the v1 stable package; or
2. move it to an explicitly optional, profile-versioned adapter that has a raw
   escape hatch and executable parity tests generated from the selected
   official metadata.

Do not “complete” the current DSL by manually adding 4.0.0 constants. That only
moves the drift date. Server response helpers remain valuable because they
encode response-header serialization and body/status interactions that are not
visible in Razor markup.

### 6. The request/response context is useful but internally uneven

`HtmxContext` as `Request` plus `Response`, and `HtmxEventArgs` exposing the
same pair, are easy to learn. The details need a final consistency pass:

- use normal .NET acronym casing (`CurrentUrl`, `PushUrl`, `ReplaceUrl`);
- distinguish missing, malformed, repeated, and contradictory header values so
  invalid input cannot broaden behavior;
- parse booleans by their allowed value, not header presence alone;
- document every header-derived member as untrusted;
- expose all seven core htmx 4 request headers without making extension headers
  wait for a package release;
- keep all nine core response headers typed, while providing a bounded extension
  response-header escape hatch;
- make every response mutator use the same HTMX-request guard, argument
  validation, fluent return, URI/string overload policy, and body-side-effect
  rules; and
- remove or re-prove earlier-version protocol such as status-based polling
  helpers before it appears in a htmx 4 stable API.

General HTTP remains `HttpContext`. A second wrapper for status-independent
headers, cookies, ETags, content language, or ASP.NET Core output-cache policy
would make the service less consistent, not more.

### 7. The exported package surface is larger than the intended developer model

The current source exposes authoring types alongside types that appear public
for renderer, source-generator, or prototype reasons. Examples include
`IHtmxorComponentEndpointInvoker`, generated action request types,
`ConditionalComponentBase`, `IConditionalRender`, `HtmxorNavigationException`,
`AjaxContext`, client DSL internals, and layout/async helpers inherited from the
prototype.

V1 needs an explicit public allow-list and API-compatibility baseline. A useful
first classification is:

| Disposition | Candidate surface |
| --- | --- |
| Keep and stabilize | registration/mapping, `HtmxRoute`, the normal-only marker, `HtmxFragment`, `HtmxHeadOutlet`, `HtmxContext`, request/response types, callback event arguments, core header names |
| Reshape before freeze | fragment selection, route method declarations, protocol naming/parsing, response consistency, extension hooks |
| Decide from demonstrated need | direct-request layout and async-load conveniences, structured `HX-Location` model |
| Internalize or remove from the stable contract | invoker/generator bridges, renderer exceptions, conditional-render infrastructure, incomplete client DSL implementation types |

This is a review classification, not an instruction to delete types blindly.
Each candidate must be checked against existing consumers and approved v1
requirements. The stable baseline should fail CI when a public API changes
without review and should prevent source-generator cross-assembly requirements
from becoming accidental user promises.

### 8. Documentation needs a claim vocabulary, not only examples

The starting discussion is an effective compact walkthrough but currently
mixes executable current syntax, intended v1 behavior, and unapproved ideas. In
particular, registration is about to change, the fragment multi-target example
does not state server-selection semantics, QUERY is accepted intent but not yet
a complete release claim, and raw htmx extension composition can be mistaken
for Htmxor conformance.

Every example should carry or link to one of these statuses: accepted v1
contract, proved slice, client composition, DX proposal, outside v1, or not yet
exercised. This keeps a complete htmx feature guide honest without implying that
Htmxor owns or has browser-tested every optional extension.

The compact discussion should become the on-ramp and link to the repository
guide for the exhaustive matrix. The guide should be release-versioned and the
discussion should not be the only durable specification.

## Htmx feature-by-feature DX conclusions

The exhaustive feature map records every individual attribute and extension.
Across those features, five reusable conclusions avoid one-off APIs:

| Feature family | DX conclusion |
| --- | --- |
| Request verbs, forms, values, validation | Htmxor owns server reachability, callback execution, binding compatibility, and antiforgery. Raw htmx owns initiation. |
| Triggering, synchronization, confirmation, indicators | These are client behavior. Teach server idempotency, cancellation, and honest status; add no Htmxor DSL requirement. |
| Targeting, selecting, swapping, OOB, partials | Htmxor selects server execution boundaries; htmx selects DOM delivery. Never conflate the two. |
| History, boost, redirects, statuses, caching | Htmxor must preserve representation and HTTP semantics; the application owns navigation/cache policy. These need recipes and conformance tests. |
| Events, JS API, configuration, CSS, extensions | Application-owned client surface. Htmxor passes markup through and exposes bounded request/response protocol hooks only where necessary. |

This division is the simplest consistent service boundary. It lets a developer
use a new client feature without waiting for Htmxor while still receiving help
where server correctness is difficult.

## Proposed v1 issue boundaries

The deletion test for each proposed issue is: if this issue is removed, can the
v1 completion test still offer an obvious, safe, consistent public developer
model? Each proposed issue below fails that test independently and has a
separately verifiable outcome.

### A. Freeze a minimal and consistently named v1 public API

Proposed title: `refactor(api): freeze the minimal Htmxor v1 public surface`

Exact proposed body: [public API issue draft](proposed-v1-dx-issue-public-api.md).

Outcome:

- decide the service and endpoint extension names, building on rather than
  duplicating #145;
- publish the intentional authoring API allow-list;
- internalize or explicitly approve infrastructure/prototype exports;
- decide the fate of the client trigger/swap/constants DSL;
- add an API-compatibility baseline and package-level test; and
- document source and binary compatibility policy before stable v1.

Excluded: implementing the no-route-group mapping already owned by #145,
changing the .NET target owned by #148, or redesigning route/fragment behavior.

### B. Make route and action declarations diagnostically complete

Proposed title: `feat(routing): make v1 reachability and action declarations self-explanatory`

Exact proposed body: [routing issue draft](proposed-v1-dx-issue-routing.md).

Outcome:

- freeze the normal-only opt-out;
- make Razor, code-behind, and pure-C# route declarations equivalent;
- remove or implement every advertised `HtmxRoute` member;
- support the agreed static callback shapes and a narrow explicit dynamic-case
  declaration; and
- emit cause-specific, location-specific diagnostics for every unsupported or
  contradictory declaration without rejecting unknown client syntax.

Excluded: changing the instance-callback model, allowing client attributes to
grant methods, or introducing controllers/Minimal APIs.

### C. Separate named server fragment selection from DOM delivery

Proposed title: `feat(fragments): separate server selection from DOM delivery`

Exact proposed body: [fragment issue draft](proposed-v1-dx-issue-fragments.md).

Outcome:

- add a stable server fragment name independent of wrapper ID;
- define whole, single, and ordered multi-fragment selection;
- define unknown/duplicate name and standard-request behavior;
- keep raw OOB and `<hx-partial>` markup as delivery composition; and
- prove excluded child branches do not render or execute while required owners
  and ancestors retain documented lifecycle behavior.

Excluded: a second fragment component concept, selecting capabilities from
untrusted target headers, streaming responses, or detached hydration.

### D. Make the htmx 4 request/response context consistent and extensible

Proposed title: `refactor(protocol): finalize the Htmxor v1 HTTP context`

Exact proposed body: [protocol issue draft](proposed-v1-dx-issue-protocol.md).

Outcome:

- normalize .NET naming and invalid-value behavior;
- type all core htmx 4 request and response headers;
- make response mutators behaviorally uniform;
- add bounded request/response extension-header hooks;
- remove or re-prove protocol inherited from earlier htmx versions; and
- add wire-level tests for malformed/repeated headers, full/partial request
  type, boost, history, redirects, events, errors, and empty responses.

Excluded: wrapping general `HttpContext` behavior or claiming every extension
without exact evidence.

## Existing ownership and non-issues

Do not create duplicate tickets for work already owned:

- [#145](https://github.com/egil/Htmxor/issues/145) owns removal of route-group
  plumbing from registration.
- [#148](https://github.com/egil/Htmxor/issues/148) owns the .NET 10-only target
  decision and migration.
- [#57](https://github.com/egil/Htmxor/issues/57) remains the existing
  coexistence discussion and should be reconciled with the v1 DOM-ownership
  contract rather than replaced.
- [#58](https://github.com/egil/Htmxor/issues/58) concerns multi-target client
  delivery; the fragment issue must distinguish that from server selection.
- [#69](https://github.com/egil/Htmxor/issues/69) is a concrete custom-component
  `hx-vals` scenario; it should use the raw-markup and stock-binding policy
  rather than create a general client DSL.
- [#16](https://github.com/egil/Htmxor/issues/16) concerns streaming, which the
  agreed v1 goal excludes.

The review does not propose an issue for every htmx attribute or extension.
Most work without an Htmxor-specific API. Creating dozens of feature tickets
would obscure the four server-side contract gaps and imply ownership Htmxor
does not want.

## Recommended discussion amendment

Discussion #143 should remain a concise v1 on-ramp with these changes:

1. Lead with the four-rule mental model and label the page as intended v1, not
   current beta documentation.
2. Show the no-route-group registration from #145 and flag the naming decision
   until issue A resolves it.
3. Keep `@page`, `HtmxRoute`, stock form, and instance callback examples, while
   explicitly stating that client attributes never grant reachability.
4. Mark QUERY as accepted intent awaiting complete evidence.
5. Replace the current multi-target example with the explicit separation of
   named server selection and raw OOB/`<hx-partial>` delivery; if the name API is
   not approved yet, label it pseudocode rather than executable syntax.
6. Link the exhaustive guide, the v1 goal, the progress evidence, and the four
   approved issue boundaries.
7. State that the application owns htmx 4.0.0 and optional extensions, and that
   raw markup is the default client API.

## Acceptance for this documentation review

The review is complete when:

- the feature guide accounts for every official htmx 4.0.0 editor attribute,
  all seven core request headers, all nine core response headers, every global
  configuration entry, the public JavaScript/event/CSS surfaces, and all 17
  official extensions;
- every feature identifies the Htmxor server consequence or explicitly says it
  is application-owned composition;
- current behavior, agreed v1 behavior, proposed API, outside-v1 behavior, and
  unexercised claims are distinguishable;
- the review records an explicit disposition for every category of exported
  Htmxor API;
- issue boundaries do not duplicate current owners; and
- repository validation records the exact HEAD, commands, counts, and any
  browser/runtime dependency not exercised.
