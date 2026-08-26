# Proposed milestone: Htmxor v1

Status: review draft only.

The milestone implements [the agreed Htmxor v1 goal](./goal.md). If this tracker
draft conflicts with the goal, the goal wins.

## Title

`Htmxor v1`

## Description

Deliver a stable, secure, convention-over-configuration integration between
Blazor static server-side rendering and an application-owned HTMX runtime.

The v1 differentiator is preserved: a developer owns routes and request behavior
in `.razor` components. They do not create a parallel controller or Minimal API
endpoint for every component. A component can be reachable as a normal Blazor
page, only by an HTMX request, or by both. The same component can return a full
page or selected server-declared fragments while retaining normal Blazor
lifecycle, forms, validation, authorization, and antiforgery behavior.

## Exit gates

- Ordinary Blazor static-SSR pages and forms behave the same after Htmxor is
  registered when they have not opted into HTMX behavior.
- Htmxor uses supported ASP.NET Core and Blazor extension points on its supported
  target frameworks. Any deliberately used infrastructure API is isolated,
  documented, version-tested, and replaceable.
- Component routes and statically discoverable actions are generated or mapped
  without application-authored endpoint boilerplate and without render-time
  callback discovery.
- Normal-only, HTMX-only, and dual reachability have safe conventions and
  explicit overrides.
- Full responses, one selected fragment, multiple fragments, and out-of-band
  updates have executable HTTP-boundary coverage.
- Stock `EditForm`, built-in input and validation components, and representative
  third-party static-SSR components work without a separate Htmxor form runtime.
- Unsafe methods are allow-listed from component intent and fail closed for
  authorization and CSRF. A request cannot replay an action under another route
  or HTTP method.
- Applications own the HTMX script and may upgrade it independently. The server
  has version-neutral request/response hooks, while analyzers validate against a
  selectable known-feature profile without rejecting future extension markup.
- Full and fragment representations have correct cache and history variation.
  Public anonymous GETs do not acquire an unnecessary per-user token cookie.
- Repeatable benchmarks cover cold and warm requests, concurrent requests,
  response bytes, allocations, and excluded fragment work against stock Blazor
  and full-document baselines.
- Static HTMX regions coexist with Interactive Server, WebAssembly, and Auto
  islands under a documented single-owner rule for navigation and DOM updates.
- The exact release-candidate package passes package validation, Production
  publish smoke tests, security review, and a clean consumer integration.

## Explicitly outside the initial v1 commitment

- Htmxor-owned or pinned distribution of the HTMX browser runtime.
- Treating HTMX-only reachability as an authorization boundary.
- Transparent action discovery that cannot be proved statically.
- Replacement of Blazor's renderer, endpoint invoker, navigation manager, or
  form runtime with copied private framework code.
- Hydrating a detached HTMX fragment as an Interactive Auto component.
- Streaming HTMX fragment updates, Native AOT, browser-log transport, and full
  document-head merging unless a bounded spike proves them without delaying the
  core release.

## Schedule

Do not assign a due date until the public execution-seam spike selects the
supported target framework and identifies any required upstream ASP.NET Core
work.
