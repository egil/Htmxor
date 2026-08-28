# Htmxor v1 goal

Status: agreed product and engineering target.

Htmxor v1 lets a developer add HTMX behavior to static server-rendered Blazor
components without creating a parallel controller or Minimal API layer. The
component type owns its route, request handling, lifecycle, and output, whether
authored in `.razor`, split between `.razor` and a matching `.razor.cs` partial,
or authored entirely in C#.

Adding Htmxor to an existing Blazor static SSR application must not change pages
that have not opted into HTMX behavior. Components built for stock static SSR,
including third-party components, should keep working. A developer can then add
HTMX one component or one interaction at a time.

## Developer model

A component can be available through normal Blazor routing, only to HTMX
requests, or through both paths.

- `@page` owns the normal Blazor route. By convention, it also makes the same
  component available to a direct HTMX GET. A component can opt out and remain
  normal-only.
- A component without `@page` can declare an HTMX-only route by applying
  `HtmxRoute` to that component: through `@attribute` in `.razor`, on its
  matching `.razor.cs` partial, or on a component authored entirely in C#.
  `HtmxRoute` is component-specific and is not supplied through
  `_Imports.razor`. Application code does not add a matching endpoint elsewhere.
- GET is the only implicit HTTP method. Htmxor infers POST from stock Blazor form
  declarations and infers POST, PUT, PATCH, or DELETE from statically
  discoverable `@onpost`, `@onput`, `@onpatch`, or `@ondelete` bindings. `hx-*`
  attributes describe how requests are initiated and may be checked for
  consistency, but they do not expose server methods. Ambiguous or dynamic
  handler bindings require a narrow explicit declaration and should produce a
  useful build diagnostic.
- Request handling runs on the component instance created for that request.
  Route and query values, dependency injection, lifecycle methods, form state,
  authentication state, and rendering all remain available. Static handler
  methods that behave like Minimal API endpoints are not the v1 model.

The common case should need no Htmxor-specific route or method configuration.
Explicit configuration exists for exceptions, not as routine ceremony.

## Full pages and fragments

A normal request follows the stock Blazor page and layout path. A direct HTMX
request can return the component output, one named fragment, several fragments,
or out-of-band content declared by the component.

`HtmxFragment` is the one fragment concept. It can render its child content
without a wrapper. If the developer supplies element, identifier, or HTML
attributes, the fragment wraps its child content and emits those values. There
is no separate `HtmxFragmentElement` concept in v1.

Fragment selection must have clear execution semantics. The component and any
required ancestors may run their normal lifecycle, but Htmxor should not render
excluded child branches below a known selection boundary. Tests and benchmarks
must verify every claim that fragment selection avoids work.

## Blazor remains in charge

Htmxor adds another HTTP representation of a Razor component. It does not add a
second component, form, validation, authorization, antiforgery, navigation, or
rendering runtime.

Stock `EditForm`, `Input*`, `DataAnnotationsValidator`, validation messages,
`[SupplyParameterFromForm]`, named forms, and their lifecycle callbacks must work
on progressively enhanced requests. Authorization metadata and antiforgery
requirements must behave the same on normal and HTMX paths.

Normal pages may contain Interactive Server, WebAssembly, or Auto components.
A direct HTMX response is static SSR. Htmxor does not claim that a detached
fragment can hydrate into an interactive component. The documentation must make
DOM and navigation ownership clear when HTMX and interactive Blazor share a
page.

Htmxor should use supported ASP.NET Core and Blazor extension points. It must not
depend on copied renderer code, private reflection, or global replacement of the
stock renderer, endpoint invoker, routing state, navigation manager, or form
runtime. If a public API is intended mainly for framework infrastructure, Htmxor
must isolate it behind a replaceable internal boundary and test it on every
supported .NET version.

## The application owns HTMX

Htmxor v1 does not require or distribute one specific HTMX runtime. The
application chooses the script, version, extensions, content security policy,
and upgrade schedule.

The server integration must understand the stable HTTP protocol it needs and
provide bounded hooks for new request headers, response headers, and extension
behavior. An analyzer may validate values against a developer-selected HTMX
profile. It must not reject unknown attributes, extension syntax, or newer
values merely because Htmxor does not know them yet.

## Security and HTTP behavior

HTMX request headers are untrusted request data. HTMX-only routing is a
reachability choice, not an authorization boundary.

Every generated or adapted endpoint must retain the effective authorization,
antiforgery, host, rate-limit, and other security metadata of the component
route. Unsafe methods fail closed. The normalized route, HTTP method, and action
identity are bound together, so information captured for one action cannot
invoke another action or another method.

Antiforgery validation runs before body binding or component callbacks for every
unsafe method. Full-page and fragment responses must set correct cache variation,
including HTMX and authentication-dependent inputs. Redirects, errors, history
restoration, boosted requests, and response headers must retain their HTTP and
browser meaning.

## Performance and release standard

Htmxor must publish repeatable cold, warm, and concurrent request measurements
against stock Blazor static SSR. The measurements must include elapsed time,
allocations, response bytes, and work skipped by fragment selection. V1 needs an
explicit request-cost budget based on those results.

The exact release-candidate package must pass the supported .NET compatibility
matrix, security tests, browser conformance tests using application-supplied
HTMX scripts, package validation, and a clean Production publish and consumer
test. Project-reference tests alone are not release evidence.

## Outside v1

V1 does not include an Htmxor-owned HTMX browser bundle, hydration of detached
HTMX fragments, streaming fragment updates, Native AOT support, or transparent
runtime action discovery. These can be considered later without changing the
component-owned route and lifecycle model.

## Completion test

V1 is complete when a developer can add Htmxor to a stock Blazor static SSR
application, leave existing pages and forms unchanged, and progressively add
component-owned HTMX routes, actions, and fragments without writing endpoint
boilerplate or giving up Blazor behavior. The result must be secure by default,
independent of the application's HTMX version, supported on the declared .NET
versions, and within the published request-cost budget.
