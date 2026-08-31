# Htmxor v1 developer experience review

- Review date: 2026-08-31
- Original review baseline: `4415863e225636d489cca2b375fb83fad583b4f5`
- Registration-naming evidence: red
  `6664dacb95ca79306fe4c5ef572d8226d2dbb477`; verified implementation
  `acf778f3abc19e26ee7524122adb8734181b9781`
- Starting discussion: [#143, "WIP: new devex"](https://github.com/egil/Htmxor/discussions/143)
- Product contract: [Htmxor v1 goal](../roadmap/v1/goal.md)
- Feature inventory: [Htmxor v1 guide and htmx 4 map](../htmxor-v1-feature-guide.md)

## Verdict

The component model is good. A developer should be able to add htmx attributes
to an `@page`, stock form, or element callback without creating another endpoint
or learning another renderer.

The bounded registration-naming slice of
[#151](https://github.com/egil/Htmxor/issues/151) resolves the setup vocabulary:
`AddHtmxor()` registers services and `AddHtmxorEndpoints()` applies endpoint
conventions without implying that Htmxor supplies the client runtime.

The API is still not ready to freeze. Three problems would force the
documentation to explain around the package:

1. Route and action discovery advertises options that the generator rejects,
   then reports failures too broadly.
2. Fragment selection mixes server work with DOM identity and request headers.
3. The package exports incomplete client helpers and implementation types beside
   the small server API developers need.

The best default is native htmx markup plus a small typed server API. Copying
every htmx attribute, trigger, swap value, configuration option, and extension
into C# would make Htmxor lag behind htmx without making Razor easier to read.

## Questions used in the review

For each developer task, the review asks:

1. Can a Blazor developer find the path from the API and ordinary Razor
   knowledge?
2. Can Htmxor remove a step, an ambiguity, or an unsafe default?
3. Do names, authority rules, argument checks, diagnostics, and extension points
   behave the same way across the API?

The behavior to protect is:

> When a developer enhances a Blazor static-SSR component with htmx, Htmxor
> keeps the route, callbacks, lifecycle, forms, authorization, and HTML in that
> component and makes the HTTP representation clear from the declaration.

The agreed v1 plan takes precedence over prototype code. This review does not
add controllers, duplicate Minimal API endpoints, static action handlers,
private renderer code, private reflection, header-based authorization, or an
Htmxor-owned htmx runtime.

## What was checked

The review followed each task from Razor declaration to HTTP behavior:

- service registration, endpoint mapping, and script ownership;
- normal-only, HTMX-only, and dual component routes;
- GET, stock forms, and component-owned POST, PUT, PATCH, DELETE, and QUERY
  callbacks;
- generator discovery and build diagnostics;
- full components, one fragment, several fragments, OOB content, and
  `<hx-partial>`;
- `HtmxRequest`, `HtmxResponse`, `HtmxContext`, and callback event arguments;
- trigger, swap, constant, layout, and async helper types;
- authorization, antiforgery, validation, caching, redirects, errors, history,
  concurrency, and interactive Blazor; and
- the official htmx 4.0.0 attributes, headers, configuration, events,
  JavaScript API, CSS classes, and extensions.

The official [htmx 4 documentation](https://four.htmx.org/docs/),
[reference](https://four.htmx.org/reference/), editor metadata, and
[extension catalog](https://four.htmx.org/extensions/) supplied the client
inventory. A .NET 10 Razor probe compiled representative colon attributes such
as `hx-status:422`, `hx-on:click`, `hx-sse:connect`, and `hx-live:text`, plus
`<hx-partial>`. That proves Razor accepts the syntax. It does not prove browser
behavior.

## API scorecard

| Area | What a developer sees | Recommended change | Verdict |
| --- | --- | --- | --- |
| Services | `AddHtmxor()` names the server integration without claiming the client runtime | State that the app supplies htmx | Current; bounded #151 proof |
| Endpoint mapping | `AddHtmxorEndpoints()` applies Htmxor conventions and returns `RazorComponentsEndpointConventionBuilder` | Keep the no-argument, convention-chain API and extend proof only when a new grouping case matters | #145 behavior; #151 name |
| Script setup | The app loads htmx, then `HtmxHeadOutlet` | Diagnose a missing or misplaced adapter without choosing the app's package source | Good model |
| Dual `@page` GET | Blazor routing also answers direct htmx GET | Keep GET as the only implicit method | Keep |
| Normal-only page | No final opt-out exists | Add one component-local marker | Missing |
| HTMX-only route | `HtmxRoute` is easy to find | Make Razor, code-behind, and C# declarations equivalent | Keep, finish diagnostics |
| Actions | Instance callbacks sit beside their markup | Infer only statically discoverable server declarations and diagnose the rest | Keep |
| Forms | Stock Blazor forms remain stock | Preserve fallback submit, binding, validation, antiforgery, and lifecycle | Keep |
| Whole component | No fragment declaration is needed | Keep the convention free of extra ceremony | Keep |
| One fragment | `HtmxFragment` is clear, but `Id` has two jobs | Give server selection its own stable name | Redesign |
| Several fragments | Lambdas and render flags hide the result | Select an ordered set of names once per response | Missing |
| OOB and partial delivery | Native htmx markup already expresses it | Do not add another Htmxor component hierarchy | Keep |
| Request data | The context shape is easy to learn | Fix .NET naming and represent invalid headers safely | Finish |
| Response operations | Fluent methods fit the protocol | Use the same guards, validation, return type, and body rules | Finish |
| Client attributes | Native markup is direct and current | Add optional profile-aware diagnostics without rejecting new syntax | Keep |
| Trigger and swap helpers | Their typed names imply full coverage, but they omit htmx 4 behavior | Remove them from core v1 or move them to a versioned optional package | Do not freeze |
| Layout and async helpers | They add Htmxor concepts | Keep only helpers that beat stock components and explicit fragments | Reassess |
| Client extensions | Raw markup works without package changes | Add a server hook only when an extension sends server data | Keep |
| Interactive Blazor | DOM ownership is not obvious from local markup | Document and test one owner for each DOM and navigation region | Track in #57 |

## Findings

### 1. Setup now uses one product vocabulary

Current setup uses `AddHtmxor()` for services and `AddHtmxorEndpoints()` for
endpoint conventions. The first method registers Htmxor's server integration;
it does not install, choose, or configure the application-supplied htmx runtime.
The endpoint method returns `RazorComponentsEndpointConventionBuilder` so the
application can continue applying endpoint conventions or metadata through the
mapping API.

[#145](https://github.com/egil/Htmxor/issues/145) removed the old destination
argument and proved the no-argument call at the application root and through one
standard ASP.NET Core route group. The bounded registration-naming slice of
[#151](https://github.com/egil/Htmxor/issues/151) keeps that shape and settles
the product-named pair:

```csharp
builder.Services.AddRazorComponents().AddHtmxor();
app.MapRazorComponents<App>().AddHtmxorEndpoints();
```

The beta does not keep the former names as compatibility aliases: doing so would
preserve the client-runtime ambiguity that the rename removes. Do not restore
the destination argument that #145 removed.

The #145 proof covers component policy and metadata, hosts, route constraints,
antiforgery, generated methods, and shared group metadata. It does not cover
nested groups, multiple Razor component applications, group endpoint filters or
rate limits, interactive render modes, or the grouped path on Kestrel or in a
browser.

This is only the naming decision inside #151. Reviewing the public allow-list,
adding a PublicAPI compatibility baseline, and deciding the incomplete client
helpers remain open.

### 2. `HtmxRoute` offers members the generator refuses

`HtmxRouteAttribute` exports `CurrentURL`, `Target`, and `Targets`. The v1
generator rejects those named arguments with the generic `HTMXOR001` diagnostic.
IntelliSense therefore recommends declarations that cannot build.

Those members also point developers toward the wrong authority rule.
`HX-Current-URL`, `HX-Source`, and `HX-Target` can help choose a representation,
but they come from the client. A URL and HTTP method must identify the server
capability. Htmx selectors such as `closest`, `next`, and `find` do not even
require a target ID.

V1 should implement a member through the full request path or remove it. It
should not publish members that always fail at build time. Representation
choices can happen in component code or through explicit fragment selection
after routing and authorization.

### 3. Build failures need specific diagnostics

The rule is simple: GET is implicit; stock forms and statically discoverable instance callbacks
add methods; dynamic cases need an explicit declaration. The compiler should
make every exception to that rule easy to fix.

It needs distinct diagnostics for:

- `HtmxRoute` placed in `_Imports.razor` or another global location;
- a C# route without an explicit method list;
- malformed, duplicated, unsupported, or contradictory `Methods` values;
- a callback expression the generator cannot resolve;
- two callbacks competing for one method and action;
- a statically known client method with no matching server action;
- an unsafe action with no stock antiforgery credential path; and
- a route template or component form outside the compiler's supported model.

Each diagnostic should point to the declaration, say what Htmxor did not
generate, and give the shortest fix. Unknown client attributes and extension
values must remain valid markup. Code-behind and C# components need the same
coverage as Razor components.

The normal-only opt-out belongs here too. It should be one local declaration
with a clear conflict if the component is also marked HTMX-only.

### 4. Fragment names and DOM IDs do different jobs

`HtmxFragment` is the right single component. Its current selection rules are
hard to predict.

Today `Id` can request a wrapper and select server output. `Match` can run an
arbitrary predicate over request headers. `RenderDuringStandardRequest` adds
another branch. A wrapperless fragment without an ID matches every direct
target. In a multi-fragment response, a reader has to evaluate all of those
rules before knowing which child components run. Renaming a DOM node can also
change server work.

V1 should separate:

- the stable server selection name;
- the optional wrapper element, ID, and attributes;
- inclusion in a normal full response; and
- browser delivery by the main swap, OOB markup, or `<hx-partial>`.

One request should select the whole component, one name, or an ordered set of
names without a `Match` lambda. The contract must define duplicate and unknown
names. A child excluded below a known fragment boundary must not render or run
its own lifecycle work. The component and required ancestors may run.

Do not derive selected names from `HX-Target`, CSS selectors, or partial markup.
Those values describe browser delivery and cannot grant server access.

### 5. Native markup beats an incomplete C# client API

The package exports `Constants`, `Trigger`, trigger builders, `SwapStyle`, swap
builders, and supporting enums and records. Their names imply a complete,
version-aware htmx model, but the htmx 4 inventory shows gaps:

- swap helpers omit aliases, morph styles, `outerSync`, `textContent`, and
  extension styles;
- modifier helpers omit current options and emit some older selector syntax;
- trigger helpers omit current event and intersection options while retaining
  concepts from older htmx versions; and
- closed enums cannot represent extension values on the same code path.

Razor accepts native htmx markup and colon attributes. Official editor metadata
already supplies client completions. Htmxor should either remove these helpers
from core v1 or move them to an optional, htmx-versioned package with a raw
string fallback and metadata-driven parity tests.

Adding the missing 4.0.0 constants by hand would only postpone the next drift.
Typed response helpers are different: they serialize response headers and
coordinate status and body behavior that Razor markup cannot express.

### 6. The HTTP context needs one set of rules

`HtmxContext` with `Request` and `Response` is easy to learn. Its details need a
final pass:

- use .NET acronym casing such as `CurrentUrl`, `PushUrl`, and `ReplaceUrl`;
- distinguish missing, malformed, repeated, and contradictory header values;
- parse booleans by allowed values rather than header presence;
- mark every header-derived value as untrusted;
- type all seven core request headers and all nine core response headers;
- provide validated APIs for extension request and response headers;
- give every response method the same request guard, argument checks, fluent
  return, URL overload policy, and documented body effect; and
- remove or prove protocol behavior carried forward from older htmx versions.

General HTTP belongs on `HttpContext`. Htmxor does not need wrappers for cookies,
ETags, content language, or ASP.NET Core output-cache policy.

### 7. The package exports more than the developer model

The current package mixes authoring types with renderer, generator, and
prototype types. Examples include `IHtmxorComponentEndpointInvoker`, generated
action request types, `ConditionalComponentBase`, `IConditionalRender`,
`HtmxorNavigationException`, `AjaxContext`, the client helper internals, and
prototype layout and async components.

V1 needs a reviewed public allow-list:

| Decision | Candidate APIs |
| --- | --- |
| Keep | registration and mapping, `HtmxRoute`, normal-only marker, `HtmxFragment`, `HtmxHeadOutlet`, context and request/response types, callback event arguments, core header names |
| Reshape | fragment selection, method declarations, header parsing and naming, response rules, extension headers |
| Require a demonstrated use case | direct-request layout, async-load helpers, structured `HX-Location` model |
| Make internal or remove | renderer and generator bridges, renderer exceptions, conditional-render machinery, incomplete client helper implementation types |

This table is a review result, not permission to delete types without checking
consumers. An API compatibility baseline should catch unreviewed changes and
prevent generator assembly needs from becoming accidental user promises.

### 8. Examples need honest status labels

Discussion #143 currently mixes working beta syntax, planned v1 behavior, and
ideas that still need API decisions. No-argument registration behavior, its
product-named API pair, and QUERY now have bounded proof through #145, the first
#151 slice, and #111 respectively. The rest of #151 remains open: the public
allow-list, PublicAPI compatibility baseline, and client-helper decision.
Multi-fragment selection and optional extension use still need explicit status
labels.

The discussion should stay short and link to the repository guide for the full
inventory. Examples should identify whether they show an accepted v1 contract,
a proved slice, application-owned client behavior, an API proposal, behavior
outside v1, or behavior not yet exercised. Passing markup through the renderer
does not prove browser or extension compatibility.

## Conclusions by htmx feature family

| Feature family | Htmxor rule |
| --- | --- |
| Request verbs, forms, values, validation | Htmxor owns server access, callback execution, binding compatibility, and antiforgery. Native htmx starts the request. |
| Triggers, synchronization, confirmation, indicators | These stay in client markup. Server code must tolerate duplicates, cancellation, and overlap. |
| Targets, selectors, swaps, OOB, partials | Htmxor selects server output. Htmx chooses DOM delivery. |
| History, boost, redirects, statuses, caching | Preserve HTTP and representation rules. The application chooses navigation and cache policy. |
| Events, JavaScript, configuration, CSS, extensions | The application owns the client code. Htmxor only needs protocol APIs for data the server must read or write. |

This split lets an application adopt a new htmx client feature without waiting
for Htmxor while keeping the difficult server rules typed and testable.

## Published v1 issues

| Issue | Decision or result |
| --- | --- |
| [#151: freeze the v1 public API](https://github.com/egil/Htmxor/issues/151) | Registration names are settled as `AddHtmxor()` / `AddHtmxorEndpoints()`; the public allow-list, PublicAPI baseline, and client-helper decision remain open |
| [#152: finish route and action declarations](https://github.com/egil/Htmxor/issues/152) | Add the normal-only marker, equivalent component forms, supported callback declarations, and specific diagnostics |
| [#153: separate fragment selection from DOM delivery](https://github.com/egil/Htmxor/issues/153) | Add stable names, whole/single/ordered selection, defined error behavior, and lifecycle proof |
| [#154: finish the htmx 4 HTTP context](https://github.com/egil/Htmxor/issues/154) | Normalize names and validation, cover core headers, add extension headers, and test exact HTTP input and output |

Existing issues keep their current scope:

- [#145](https://github.com/egil/Htmxor/issues/145) is closed after proving the
  no-argument registration at the root and through one standard route group.
- [#148](https://github.com/egil/Htmxor/issues/148) owns the .NET 10 target.
- [#57](https://github.com/egil/Htmxor/issues/57) covers Blazor coexistence.
- [#58](https://github.com/egil/Htmxor/issues/58) covers multi-target browser
  delivery, which is separate from server fragment selection.
- [#69](https://github.com/egil/Htmxor/issues/69) records a concrete `hx-vals`
  use case.
- [#16](https://github.com/egil/Htmxor/issues/16) covers streaming, which is
  outside the v1 goal.

Most htmx attributes and extensions do not need Htmxor issues. They already work
as native markup. Splitting them into dozens of tickets would imply that Htmxor
owns client features it intends to pass through.

## Documentation result

The guide accounts for all 49 official htmx 4.0.0 editor attributes, seven core
request headers, nine core response headers, 22 global configuration entries,
62 documented event entries, 12 JavaScript methods, six CSS classes, and 17 official
extensions. Each entry says what Htmxor or the application must do on the
server.

Repository verification must record the exact commit, commands, test counts,
and any browser, operating system, runtime, service, extension, or configuration
that was not exercised.
