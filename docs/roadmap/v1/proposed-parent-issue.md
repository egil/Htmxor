# Proposed parent issue

Status: review draft only.

This issue tracks [the agreed Htmxor v1 goal](./goal.md). The goal is the source
of truth for product and engineering scope.

## Title

`Htmxor stable v1: idiomatic Blazor routes with version-independent HTMX`

## Why

The prototype established a valuable developer model: a `.razor` component owns
its route, can participate in an ordinary Blazor page load, and can return only
the region an HTMX request needs. It also accumulated framework-private routing
and rendering dependencies, globally replaced standard Blazor services, bundled
an old HTMX runtime, exposed unsafe HTTP methods too broadly, and did not close
the performance or security evidence needed for a stable release.

Stable v1 should retain the product idea and replace the prototype architecture.

## Product contract

- Route ownership remains in `.razor` files. Applications do not define one
  controller or Minimal API handler per component.
- Convention is preferred over configuration. `@page`, standard Blazor form
  declarations, `hx-*` attributes, and statically discoverable `@on*` handlers
  provide intent; explicit metadata is required only when the safe default
  cannot be inferred or must be overridden.
- `@page` supplies the normal route and, by default, the matching direct HTMX GET
  route. An explicit declaration opts a page out of direct HTMX reachability.
  `[HtmxRoute]` on a component without `@page` supplies an HTMX-only route.
- GET is the only implicit HTTP method. Statically discoverable standard forms
  and `@onpost`, `@onput`, `@onpatch`, or `@ondelete` bindings provide server-side
  intent for unsafe methods. `hx-*` attributes describe request initiation and
  may be checked for consistency, but they do not expose server methods.
  Ambiguous or dynamic handler bindings require a diagnostic and a narrow
  explicit override.
- A component can be normal-only, HTMX-only, or available through both paths.
- A normal request uses the normal Blazor page and layout path. An HTMX request
  may receive the whole component response or one or more declared fragments.
- `HtmxFragmentElement` does not remain a second concept. Optional element,
  identifier, and unmatched attributes belong on `HtmxFragment`, which wraps its
  child content only when requested.
- Component instance lifecycle remains in play. V1 does not replace actions with
  static methods that amount to colocated Minimal API handlers.
- Stock static-SSR components remain stock components. Htmxor adds another HTTP
  representation; it does not create another form, validation, authorization,
  antiforgery, navigation, or rendering runtime.
- Full Blazor pages may contain Interactive Server, WebAssembly, or Auto islands.
  Direct HTMX responses are honest static SSR and never claim to hydrate a
  detached interactive fragment.
- The application owns the HTMX browser runtime and upgrade cadence. Htmxor
  supplies a tested compatibility profile and replaceable protocol hooks, not a
  hard runtime version dependency.
- Analyzers can reject or warn on values that are invalid for a selected known
  HTMX profile. Unknown `hx-*` attributes, newer values, and registered extension
  syntax remain valid so the analyzer does not become an accidental runtime
  version lock.

## Work sequence

### Gate 1: establish evidence

1. Capture a repeatable stock-Blazor and current-prototype baseline, including
   the known startup, lifecycle, form, static-asset, and cache cases while proving
   a direct endpoint through the stock Blazor invoker.
2. Prove whether generated PUT, PATCH, and DELETE handlers can invoke the live
   component instance through supported framework seams while remaining bound to
   the declared route and HTTP method.

Only these two issues should be made ready for agents initially.

### Gate 2: tracer-bullet implementation

After the execution decision, break the following outcomes into reviewed,
independently assignable issues:

1. generated component-owned routes and all three reachability modes;
2. progressively enhanced stock Blazor forms and static-SSR components;
3. full, selected, multiple, and out-of-band fragment rendering;
4. lifecycle-preserving unsafe-verb actions with fail-closed security;
5. caller-owned HTMX runtime and extensible request/response protocol;
6. correct caching, history, and measured per-request work;
7. coexistence with interactive render modes and enhanced navigation; and
8. package, publish, documentation, security, and consumer evidence.

### Gate 3: release validation

Publish an RC from the exact verified package. Complete a bounded consumer cycle,
including a separately owned ForTheLeague tracer bullet if that project is to
block stable release. Resolve RC findings before tagging v1.

## Acceptance criteria

- [ ] Every milestone exit gate is owned by a child issue or explicitly deferred
      with rationale and a future owner.
- [ ] The first architecture decision is backed by an executable comparison, not
      only an interface proposal.
- [ ] Every behavior-changing child issue carries a verification contract with a
      meaningful failure and success observation.
- [ ] Existing open issues receive a disposition comment that links to the exact
      owning child issue or records why they remain deferred.
- [ ] Existing issues are not closed until their promised behavior, documentation,
      or regression evidence exists.
- [ ] No child issue introduces per-component controller or Minimal API ceremony
      into application code.
- [ ] Stable-v1 evidence is produced from the exact release-candidate commit and
      package.

## Blocked by

None - this parent can be created after review.

## Research inputs

- Stable v1 gap analysis
- Blazor static SSR progressive-enhancement analysis
- .NET 11 Blazor and ASP.NET Core opportunity analysis
- HTMX backend framework comparison
- Htmxor v1 interface sketch

These should be linked to their repository paths when the issue is published.
