# Htmxor v1 progress

Last updated: 2026-08-27

## Repository state

- Baseline commit for the first v1 slice: `66139317b9edae1fff2ff73fa5175381ee3487b1`.
- Verified implementation commit for issue #78: `8dcca3c0749cf53e310b1dff9dc22612b0d5e8f5`.
- Verified implementation commit for issue #81: `0c3fec1b8c3425ef37c2d93a5fa131f3b0c2a649`.
- Framework boundary under test: a real ASP.NET Core 10 and Blazor static SSR test host consuming the project-referenced `net8.0` Htmxor library.
- V1 implementation slices proved on this tree: issue #78, stock `@page` routing with a direct HTMX GET; issue #81, every documented .NET 10 Blazor component-route constraint plus typed optional presence and absence.
- Current implementation slice after #81: none. Recheck issue #81, branch publication state, and `origin/main` before starting the next slice.

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

Protected behavior for issue #81:

> When a direct HTMX GET selects a stock `@page` component whose route uses any
> route constraint supported by Blazor on .NET 10, Htmxor supplies the same typed
> route values, query value, and request-scoped dependency as stock Blazor and
> initializes one component instance without another route or application
> endpoint.

The proved constraint and parameter-type set is:

- `bool` to `System.Boolean`;
- `datetime` to `System.DateTime`;
- `decimal` to `System.Decimal`;
- `double` to `System.Double`;
- `float` to `System.Single`;
- `guid` to `System.Guid`;
- `int` to `System.Int32`;
- `long` to `System.Int64`;
- `nonfile` to `System.String`.

The hosted matrix proves representative valid typed output and rejected-input
parity for every constraint, including rejection of the file-like
`document.txt` by `nonfile`. A constrained optional `int` has matching present
and absent behavior. Every successful normal and direct request retains the
query value, request-scoped service value, and initialization count `1`. Each
component-owned route template has one endpoint. Normal responses retain the
stock application shell and direct responses omit it.

The direct host passes the endpoint-selected `RouteData` through the stock
public `Router`. Its endpoint-supplied route-data path performs Blazor's
constrained-value processing and returns the selected component without another
route match. The existing endpoint routing, query supplier, dependency
injection, lifecycle, and static SSR renderer remain in charge. The supported
path neither copies nor extends the legacy hand-written conversion switch.

## Executable evidence

- Meaningful red at `66139317b9edae1fff2ff73fa5175381ee3487b1`: the new .NET 10 hosted test discovered and executed one test, then failed during real application startup with the expected `NullReferenceException` in the obsolete private-reflection component discovery path.
- Focused proof at `8dcca3c0749cf53e310b1dff9dc22612b0d5e8f5`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 1 passed, 0 failed, 0 skipped.
- Broader proof at the same clean commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 102 quality tests, 1 .NET 10 hosted test, and 150 existing non-browser tests. Total: 253 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Mutation testing was not run. Issue #78 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for the complete issue #81 matrix at `29cecb64a8bf9466c3bd7c2dfdb9874d347edcb0`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --blame-hang --blame-hang-timeout 5min` discovered and executed 21 tests; 12 passed, 9 failed, 0 skipped. Direct GETs returned `500` for all eight typed constraints and for a present optional `int`, while their normal GETs returned `200`. The `nonfile` valid case, all nine rejected-input parity cases, optional absence, and the issue #78 route test passed before the production change.
- Focused proof at clean implementation commit `0c3fec1b8c3425ef37c2d93a5fa131f3b0c2a649`: the same command discovered and executed 21 tests; 21 passed, 0 failed, 0 skipped.
- Broader proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 102 quality tests, 21 .NET 10 hosted tests, and 150 existing non-browser tests. Total: 273 discovered, 273 executed, 273 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Mutation testing was not run. Issue #81 makes it optional diagnostic evidence for this proof of concept.

## Remaining limits

- This is a project-reference proof, not packed-package or release-candidate evidence.
- The matrix uses one representative valid and rejected value per documented constraint. It does not exhaust textual representations, undocumented custom conversion constraints, catch-all routes, or unconstrained routes.
- The direct path is proved on ASP.NET Core 10 only. The supported framework matrix and packed-package consumption remain unproved.
- The test does not exercise layouts, forms, unsafe methods, authorization, antiforgery enforcement, caching, concurrency, enhanced navigation, interactive render modes, browser behavior, application-selected HTMX runtimes, or performance.
- The legacy test application still uses internal private-reflection discovery and global service replacements. Later slices must replace the behavior they cover instead of extending that prototype.
- HTMX-only component routes and component-owned actions have not moved to the new public path.

## Recommended next slice

Protected behavior for the recommended next slice:

> When a stock `@page` component requires an authorization policy, Htmxor
> enforces the same policy and supplies the same authenticated user on normal
> and direct GETs without treating HTMX request headers as authorization
> evidence.

This comes next because the request-local endpoint copy retains authorization
metadata structurally, but no test proves policy enforcement or authentication
state on the direct path. A .NET 10 hosted integration test should use real
authentication and authorization middleware, one component policy, and normal
and direct HTTP requests. Forms, unsafe methods, antiforgery, fragments, browser
conformance, packaging, and performance remain later slices.

No new implementation issue has been published for this candidate.
