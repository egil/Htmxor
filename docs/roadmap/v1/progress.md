# Htmxor v1 progress

Last updated: 2026-08-27

## Repository state

- Baseline commit for the first v1 slice: `66139317b9edae1fff2ff73fa5175381ee3487b1`.
- Verified implementation commit for issue #78: `8dcca3c0749cf53e310b1dff9dc22612b0d5e8f5`.
- Framework boundary under test: a real ASP.NET Core 10 and Blazor static SSR test host consuming the project-referenced `net8.0` Htmxor library.
- V1 implementation slices proved on this tree: issue #78, stock `@page` routing with a direct HTMX GET.
- Current implementation slice after #78: none. Recheck PR #80 and `origin/main` before starting the next slice.

## Proven v1 behavior

Protected behavior:

> When a .NET 10 Blazor static SSR application adds Htmxor and maps one `@page`
> component, Htmxor preserves the normal stock full-page GET and returns the
> endpoint-selected component for a direct HTMX GET without a parallel
> application endpoint.

The hosted test proves that one `.razor` file owns the route, the application
starts, and exactly one component endpoint represents that page. A normal GET
uses the stock Blazor shell. A direct HTMX GET to the same route returns the
page component without that shell. Both responses retain application-supplied
endpoint metadata.

The public integration captures the stock Razor Components request delegate in
a final endpoint convention. Normal requests call it unchanged. For a direct
HTMX GET, Htmxor gives that delegate a request-local copy of the selected route
endpoint, preserving its route pattern, order, display name, and ordered
metadata while replacing only its root component metadata. The internal direct
host renders the `RouteData` already selected by the stock invoker, so it does
not perform a second routing pass.

The new public path does not use private reflection, copy Blazor renderer code,
add a controller or Minimal API handler, declare a duplicate application route,
or replace stock routing, rendering, navigation, or endpoint-invoker services.
The old prototype remains internal to the legacy test application for behavior
that later slices have not replaced.

## Executable evidence

- Meaningful red at `66139317b9edae1fff2ff73fa5175381ee3487b1`: the new .NET 10 hosted test discovered and executed one test, then failed during real application startup with the expected `NullReferenceException` in the obsolete private-reflection component discovery path.
- Focused proof at `8dcca3c0749cf53e310b1dff9dc22612b0d5e8f5`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 1 passed, 0 failed, 0 skipped.
- Broader proof at the same clean commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 102 quality tests, 1 .NET 10 hosted test, and 150 existing non-browser tests. Total: 253 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Mutation testing was not run. Issue #78 makes it optional diagnostic evidence for this proof of concept.

## Remaining limits

- This is a project-reference proof, not packed-package or release-candidate evidence.
- The test does not exercise route or query parameters, layouts, forms, unsafe methods, authorization, antiforgery enforcement, caching, concurrency, enhanced navigation, interactive render modes, browser behavior, application-selected HTMX runtimes, or performance.
- The legacy test application still uses internal private-reflection discovery and global service replacements. Later slices must replace the behavior they cover instead of extending that prototype.
- HTMX-only component routes and component-owned actions have not moved to the new public path.

## Recommended next slice

Protected behavior:

> When a direct HTMX GET selects a stock `@page` component with route and query
> values and an injected service, Htmxor supplies those values to the selected
> component and runs its normal lifecycle once without a second routing pass or
> a parallel application endpoint.

This comes next because issue #78 established a small host that renders the
stock invoker's `RouteData` directly. Route values, query values, dependency
injection, and lifecycle execution are the nearest unproved parts of that seam.
A .NET 10 hosted integration test should observe the rendered values and a
request-scoped lifecycle sentinel through normal and direct GETs. Authorization,
forms, unsafe methods, fragments, browser conformance, packaging, and performance
remain later slices.

No new implementation issue has been published for this candidate.
