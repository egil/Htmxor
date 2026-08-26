# Proposed issue 01

Status: review draft only.

## Title

`spike: execute Htmxor requests through Blazor's stock endpoint invoker`

## Triage

- Type: HITL for the final target-framework and execution decision; evidence
  gathering and prototype work are AFK.
- Proposed state after publication: needs spike
- Parent: proposed stable-v1 parent issue

## What to build

Build a disposable vertical request path for one routed Razor component. The same
component must handle a normal GET, a direct HTMX GET, one selected fragment, and
a stock named `EditForm` POST. For an existing `@page`, compare wrapping the stock
endpoint through public endpoint conventions with creating a separate generated
endpoint. The candidate should use public
`IRazorComponentEndpointInvoker`, `RootComponentMetadata`, and
`ComponentTypeMetadata` so Blazor continues to own routing state, lifecycle, form
mapping, validation, antiforgery, navigation, streaming, persistence completion,
and framework assets.

Compare that candidate with the current Htmxor pipeline and with a wrapper
rendered through `RazorComponentResult`. The comparison is not an invitation to
expose either framework API in application Razor code. Its purpose is to select a
replaceable internal Htmxor executor and the minimum supported target framework.

Use the .NET 11 public attribute-driven cascading-value registration seam to
prototype Htmxor request context without editing a renderer's internal supplier
collection. Assess the infrastructure-oriented component endpoint builder helper
for an HTMX-only route and for the narrow route-builder access it actually
provides; do not treat it as component discovery or convention propagation.

## Acceptance criteria

- [ ] The spike maps a component-owned route without an application-authored
      controller or per-component Minimal API handler.
- [ ] A direct GET runs through the stock component endpoint invoker without
      replacing Blazor services or reflecting over private members.
- [ ] For a dual `@page` route, a public final endpoint convention identifies the
      stock page through public component metadata. A normal request runs the
      original delegate unchanged; a direct request changes only the bounded
      metadata/root representation needed by the stock delegate.
- [ ] For an HTMX-only component, a generated endpoint is mapped on the exact
      route builder obtained from the Razor component builder. Route-group prefix
      and conventions are preserved, while conventions attached only to the
      Razor-components builder are recorded as a distinct gap.
- [ ] Route and query binding, authorization metadata, endpoint-group conventions,
      navigation, redirects, status codes, and the normal page route match a stock
      Razor component endpoint.
- [ ] A named `EditForm` POST proves valid, invalid, malformed, missing, and
      repeated values, `[SupplyParameterFromForm]`, antiforgery, exact callback
      selection, and rerendered validation output.
- [ ] The selected-fragment case records which component lifecycle and data work
      still runs and proves that excluded child branches below the selection
      boundary do not run when the candidate claims that optimization.
- [ ] The `RazorComponentResult` comparison records behavior for named form
      initialization, Session/TempData completion, persistent component state,
      browser initializers, streaming, redirects, and static assets. Missing
      behavior is recorded rather than copied from a private renderer.
- [ ] Htmxor request context reaches the component through the .NET 11 public
      cascading-value registration seam without global service replacement.
- [ ] The public component endpoint builder helper is tested for route-builder
      access, and the result explicitly records that it does not expose discovered
      components or automatically propagate later conventions unless evidence
      proves otherwise.
- [ ] A normal route with Auto applied at the root is compared with a direct static
      response. A component carrying its own definition-level
      `@rendermode InteractiveAuto` is tested separately because the stock renderer
      has no public per-invocation switch to ignore that boundary.
- [ ] The current .NET 10 startup/discovery failure, duplicate lifecycle work,
      Production asset behavior, and fingerprinted framework assets are captured
      as versioned comparison fixtures.
- [ ] .NET 10 and the inspected .NET 11 preview are compared. APIs and behaviors
      are marked shipped, preview, deliberately infrastructure oriented,
      internal, open/draft, or inferred.
- [ ] The result recommends a v1 target-framework baseline, one internal request
      execution path, and a fallback or upstream request for every blocking gap.
- [ ] No production library behavior is introduced by the spike.
- [ ] A human accepts the execution and target-framework decision before the
      convention/public-API issue becomes ready for agents.

## Verification contract

- **Protected behavior:** When Htmxor handles another representation of a routed
  component request, stock Blazor still owns the component lifecycle and its
  normal GET/POST framework behavior.
- **Risk and evidence:** Framework drift and false form/rendering parity; hosted
  integration tests execute real ASP.NET Core routing, dependency injection,
  middleware, component endpoint invocation, form mapping, rendering, and static
  web assets on the compared frameworks.
- **Observation seam:** HTTP requests and responses from the mapped endpoints,
  plus a deterministic component lifecycle/data probe when avoided work is the
  protected behavior.
- **Boundary fidelity:** Use the real .NET 10 and installed .NET 11 preview Blazor
  services and endpoint implementations. Replace only application data with a
  deterministic probe; do not replace routing, rendering, form, antiforgery,
  persistence, or asset services.
- **Meaningful red:** The current private endpoint-discovery path reproduces its
  .NET 10 failure, and the result-only candidate fails any stock endpoint parity
  checks for behavior it genuinely omits. A green candidate must execute the real
  protected framework path.
- **Success evidence:** One supported candidate passes the bounded GET/POST and
  fragment comparison, its remaining gaps have explicit owners, and the chosen
  executor can change internally without changing application Razor call sites.
- **Residual risk:** This spike does not solve non-POST callback dispatch, the
  first-validatable-form HTMX boot edge, all `CacheView` isolation cases, the full
  source generator, browser-runtime conformance, or release performance budgets.

## Blocked by

None - can start immediately.
