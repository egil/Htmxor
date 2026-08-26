# Htmxor v1 progress

Last updated: 2026-08-26

## Repository state

- Baseline commit: `d8e09e4da17ab4c74fbea95d8e995137785c8395`
- V1 implementation slices completed: none
- Current implementation slice: none
- Agreed target: [Htmxor v1 goal](./goal.md)

## Proven current behavior

The existing .NET 8 test suite passes 152 tests when its Playwright Chromium
runtime is installed. It contains 150 unit, component, and hosted HTTP cases and
two browser cases. The suite proves several prototype behaviors, including a
normal and HTMX representation on one route, route-value binding, one selected
fragment response, response-header construction, and two browser interactions.

These are prototype characterization results. They do not prove the v1 goal.

## Known blockers and defects

- Every project and CI job targets .NET 8. The current private component
  discovery path fails when used by a .NET 10 application.
- A test sends PATCH with the DELETE callback identifier and expects the DELETE
  callback to run. Route, method, and action identity are not safely bound.
- Antiforgery validation covers POST, PUT, and PATCH in the custom invoker but
  omits DELETE.
- Existing browser tests use the embedded HTMX 1.9.12 script. They do not prove
  application-owned HTMX compatibility.
- The test application contains stock Blazor form and validation examples that
  no test exercises.
- No test covers Interactive Auto coexistence, cache variation, a packed-package
  Production publish, or a request-cost budget.

## Next candidate slice

Protected behavior:

> When a .NET 10 Blazor application adds Htmxor and maps one `@page` component,
> the application starts, a normal GET follows the stock Blazor path, and a
> direct HTMX GET returns the component representation without a controller,
> Minimal API handler, or per-component endpoint declaration in application
> code.

This is a candidate, not an active commitment. Before starting it, the
orchestrator must verify the current repository and framework APIs and confirm
that no newer evidence makes another slice more urgent.

## Decision expected after the slice

The evidence should identify the supported endpoint execution path and whether
.NET 10 can be the v1 baseline. If supported public APIs cannot preserve the
component-owned route and stock normal request, stop and ask whether to require
a newer .NET version or pursue an upstream ASP.NET Core change.

## Deferred work

Forms, unsafe methods, source generation, fragment optimization, caller-owned
HTMX conformance, cache behavior, interactive islands, package verification,
and performance budgets remain part of the v1 goal. Do not turn them into
implementation-ready work until the current execution evidence makes their
boundaries clear.
