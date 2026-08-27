# Htmxor v1 progress

Last updated: 2026-08-27

## Repository state

- Baseline commit for the first v1 slice: `66139317b9edae1fff2ff73fa5175381ee3487b1`.
- Verified implementation commit for issue #78: `8dcca3c0749cf53e310b1dff9dc22612b0d5e8f5`.
- Verified implementation commit for issue #81: `0c3fec1b8c3425ef37c2d93a5fa131f3b0c2a649`.
- Verified evidence commit for issue #83: `46f5b5324c64bff111a8e9bbb38ea812c22067ef`.
- Verified implementation commit for issue #85: `0a87dcd8b50cb5fd1be6a4ddae57601986aaea4a`.
- Verified implementation commit for issue #87: `8c2a528dbff8c528d52199c60330c99ded851b83`.
- Verified post-review test head for issue #87: `645065ef809306f744bc7cdb8adf1f799b3c0784`. Production code is unchanged from the implementation commit; the only executable delta is a test identifier correction.
- Issue #87 progress commits are documentation-only. Executable claims are tied to the tested heads above, not to the later documentation heads.
- Framework boundary under test: a real ASP.NET Core 10.0.11 and Blazor static SSR test host consuming the project-referenced `net8.0` Htmxor library.
- V1 slices proved on this tree: issue #78, stock `@page` routing with a direct HTMX GET; issue #81, every documented .NET 10 Blazor component-route constraint plus typed optional presence and absence; issue #83, authorization-policy and authenticated-user parity for normal and direct GETs; issue #85, one stock named `EditForm` POST with form binding, antiforgery ordering, request-component callback dispatch, and direct component output; issue #87, one shared runtime path for component-owned PUT, PATCH, and DELETE actions represented by fixed future-generator output.
- Current implementation slice after #87: none. Recheck issue #87, branch publication state, and `origin/main` before starting the next slice.

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

Protected behavior for issue #83:

> When a stock `@page` component requires an authorization policy, Htmxor
> enforces the same policy and supplies the same authenticated user on normal
> and direct GETs without treating HTMX request headers as authorization
> evidence.

The hosted proof uses one deterministic authentication scheme and one claim
policy through the real ASP.NET Core 10 authentication and authorization
middleware. Anonymous requests receive `401` on both paths. An authenticated
user without the required claim receives `403` on both paths. The `HX-Request`
header alone does not authorize a request.

An authorized user's name and required claim reach the component unchanged on
both paths. The normal response retains the stock application shell, while the
direct response returns the protected component without that shell. The
application still owns one component route and does not add a controller,
Minimal API handler, or duplicate endpoint. No production change was required;
the existing metadata-preserving direct path already satisfied this slice.

Protected behavior for issue #85:

> When a component-owned stock form is submitted through HTMX, Htmxor lets
> Blazor bind the form, validates antiforgery before application code, invokes
> the request component callback, and returns the component response without a
> parallel endpoint.

The hosted proof uses one component-owned `@page` route, one named stock
`EditForm`, and one `[SupplyParameterFromForm]` input. A normal GET renders the
stock application shell and supplies the form handler, antiforgery token, and
cookie. A valid direct HTMX POST binds `accepted-value`, initializes one request
component with a new request-scoped dependency, invokes its callback once with
that value, and returns the updated component without the stock shell.

A direct POST without the antiforgery token and cookie returns `400` before the
form property setter, component initialization, or callback records any
activity. The application still owns one component route. It adds no controller,
Minimal API handler, duplicate route, static endpoint-style action, custom form
binder, or antiforgery runtime.

The public endpoint convention now applies its request-local root-component
substitution to direct GET and POST requests. It preserves the stock component
endpoint's ordered metadata and invokes its captured request delegate, leaving
ASP.NET Core 10.0.11 responsible for antiforgery validation, form mapping,
component lifecycle, named callback dispatch, and rendering. Other HTTP methods
continue through the stock delegate unchanged.

Protected behavior for issue #87:

> When a Razor component declares PUT, PATCH, and DELETE actions, only the
> matching HTTP method can invoke each callback, and every callback runs on the
> request-owned component instance after authorization and antiforgery succeed.

The hosted proof uses one component-owned `@page` route with distinct `@onput`,
`@onpatch`, and `@ondelete` method groups. A hand-authored `.g.cs` stand-in
represents assumed future-generator output: component type, exact normalized
route, HTTP method, server-owned handler identity, descriptor registration, and
the component-side lifecycle hook. It does not discover or analyze Razor, emit
diagnostics, implement a source generator, or define the final generator API.

A final endpoint convention matches each fixed descriptor to the existing stock
component endpoint by component type and normalized route. It preserves the
stock request delegate and metadata, extends the effective `GET, POST` method
metadata with PUT, PATCH, and DELETE, and attaches the server-owned descriptors.
The stock method set and final-convention ordering are visible in the official
[ASP.NET Core 10.0.11 endpoint factory](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Builder/RazorComponentEndpointFactory.cs).

After routing and the retained authorization policy succeed, the shared action
wrapper calls the public `IAntiforgery.ValidateRequestAsync` before it arms a
request-scoped descriptor. This explicitly covers DELETE, which ASP.NET Core's
[antiforgery guidance](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0#http-method-limitations-and-httpmethodoverridemiddleware-interaction)
requires handlers to validate directly. The wrapper then invokes the unchanged
stock component delegate through Htmxor's request-local direct-render endpoint.

The fixed partial runs on the routed page instance. It awaits the public
[`ComponentBase.SetParametersAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.componentbase.setparametersasync?view=aspnetcore-10.0)
contract, atomically consumes the matching descriptor once, and invokes the
declared method group through `EventCallback` on `this`. This supplies route,
query, authenticated user, request-scoped dependency, and normal initialization
and parameter lifecycle state before the callback, while the stock renderer
writes the callback-updated component response. ASP.NET Core's stock
[`RazorComponentEndpointInvoker`](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs)
still reserves its form-dispatch path for POST; the proved lifecycle hook is the
narrow supported seam for these non-POST actions.

The full-fidelity DELETE case observes route value `42`, query value
`from-query`, authenticated user `issue-87-user`, a new request-scoped service,
one parameter lifecycle pass, one initialization, one callback, and the
callback-mutated direct response. Compact positive cases prove distinct PUT,
PATCH, and DELETE callbacks through the same runtime path. Cross-method cases
carry another action's client-supplied identity but invoke only the callback
selected by the actual HTTP method. An undeclared `PROPFIND` with a DELETE
identity remains `405`. Invalid antiforgery tokens for PUT, PATCH, and DELETE
return `400` with zero parameter, initialization, or callback activity.

The application maps only the stock Razor component endpoint. It adds no
controller, Minimal API handler, duplicate route, static component action,
renderer reflection, runtime render-tree discovery, renderer copy, or global
Blazor service replacement.

## Executable evidence

- Meaningful red at `66139317b9edae1fff2ff73fa5175381ee3487b1`: the new .NET 10 hosted test discovered and executed one test, then failed during real application startup with the expected `NullReferenceException` in the obsolete private-reflection component discovery path.
- Focused proof at `8dcca3c0749cf53e310b1dff9dc22612b0d5e8f5`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 1 passed, 0 failed, 0 skipped.
- Broader proof at the same clean commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 102 quality tests, 1 .NET 10 hosted test, and 150 existing non-browser tests. Total: 253 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Mutation testing was not run. Issue #78 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for the complete issue #81 matrix at `29cecb64a8bf9466c3bd7c2dfdb9874d347edcb0`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --blame-hang --blame-hang-timeout 5min` discovered and executed 21 tests; 12 passed, 9 failed, 0 skipped. Direct GETs returned `500` for all eight typed constraints and for a present optional `int`, while their normal GETs returned `200`. The `nonfile` valid case, all nine rejected-input parity cases, optional absence, and the issue #78 route test passed before the production change.
- Focused proof at clean implementation commit `0c3fec1b8c3425ef37c2d93a5fa131f3b0c2a649`: the same command discovered and executed 21 tests; 21 passed, 0 failed, 0 skipped.
- Broader proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 102 quality tests, 21 .NET 10 hosted tests, and 150 existing non-browser tests. Total: 273 discovered, 273 executed, 273 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Mutation testing was not run. Issue #81 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for issue #83 used the test tree at commit `46f5b5324c64bff111a8e9bbb38ea812c22067ef` plus a temporary negative-control mutation that removed authorization metadata from component endpoints: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue83AuthorizationTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 4 tests; 2 passed and 2 failed. The anonymous and claim-deficient cases reached their first status assertion with `200` instead of `401` and `403`. The mutation was removed and left no production diff.
- Focused proof at the same clean commit with the same filtered command: 4 discovered, 4 executed, 4 passed, 0 failed, 0 skipped.
- Hosted-project proof at the same clean commit without the filter: 25 discovered, 25 executed, 25 passed, 0 failed, 0 skipped.
- Broader proof at the same clean commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj --no-restore -- check --profile fast` passed 102 quality tests, 25 .NET 10 hosted tests, and 150 existing non-browser tests. Total: 277 discovered, 277 executed, 277 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Mutation testing was not run. Issue #83 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for issue #85 used the test tree preserved in `6d0fcf4dafe6e840423eb6e32eec41b1c8e3c7e3` with the unchanged production behavior from `4f2c0d81d25141643894d19972e1b701a9982615`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue85FormTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 2 tests; 1 passed and 1 failed. Before the shell assertion failed, the valid POST had bound `accepted-value`, initialized one request component with a new request scope, and invoked its callback once with that value. Its response still contained the stock `<html>` shell. The missing-token request passed with `400` and zero form-binding, initialization, or callback activity.
- Focused proof at clean implementation commit `0a87dcd8b50cb5fd1be6a4ddae57601986aaea4a`: the same filtered command discovered and executed 2 tests; 2 passed, 0 failed, 0 skipped.
- Hosted-project proof at the same clean implementation commit without the filter: 27 discovered, 27 executed, 27 passed, 0 failed, 0 skipped.
- Broader proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj --no-restore -- check --profile fast` passed 102 quality tests, 27 .NET 10 hosted tests, and 150 existing non-browser tests. Total: 279 discovered, 279 executed, 279 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Mutation testing was not run. Issue #85 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for issue #87 is preserved at `e48bc29bec6da718ee4e2c90cd60ed09a3f26f4b`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue87DeleteActionTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 hosted test; 0 passed, 1 failed, 0 skipped. An authorized DELETE with a valid antiforgery cookie and token expected `200` but received `405` from the real stock endpoint before the callback could run.
- Focused proof at clean implementation commit `8c2a528dbff8c528d52199c60330c99ded851b83`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue87UnsafeActionTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 11 cases; 11 passed, 0 failed, 0 skipped.
- Hosted-project proof at the same clean implementation commit without the filter: 38 discovered, 38 executed, 38 passed, 0 failed, 0 skipped.
- Broader proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj --no-restore -- check --profile fast` passed 102 quality tests, 38 .NET 10 hosted tests, and 150 existing non-browser tests. Total: 290 discovered, 290 executed, 290 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Independent Standards and Spec reviews examined `31a61637dcf44ffbd8f3e9c5bbdc38224986c549..8c2a528dbff8c528d52199c60330c99ded851b83`; both passed with zero actionable findings.
- A GitHub review later found one P3 grammar error in a test identifier. Commit `645065ef809306f744bc7cdb8adf1f799b3c0784` corrected only that identifier. At that exact clean head, the focused issue #87 command again passed 11 of 11 cases, and the fast profile again passed 102 quality, 38 hosted, and 150 library tests: 290 passed with 0 failures, skips, errors, or timeouts and a Release build with 0 warnings or errors. Separate Standards and Spec rereviews both passed with zero remaining findings.
- Mutation testing was not run. Issue #87 makes it optional diagnostic evidence for this proof of concept.

## Remaining limits

- This is a project-reference proof, not packed-package or release-candidate evidence.
- The matrix uses one representative valid and rejected value per documented constraint. It does not exhaust textual representations, undocumented custom conversion constraints, catch-all routes, or unconstrained routes.
- The direct path is proved on ASP.NET Core 10 only. The supported framework matrix and packed-package consumption remain unproved.
- The authorization proof uses one deterministic scheme and one claim policy. It does not cover scheme selection, custom challenge or forbid handlers, identity-provider integration, or authorization on other HTTP methods.
- The issue #85 proof covers one stock named `EditForm`, one valid value, and one missing-token POST. It does not cover multiple forms, validation failures, invalid-token variants, file uploads, normal POST parity, or custom method discovery. Issue #87 proves unsafe route/query instance dispatch separately, without request-body or form binding.
- The issue #87 stand-in assumes future generator output. Generator discovery, Razor expression analysis, diagnostics, final public API, and packaged-consumer registration remain unproved.
- The issue #87 lifecycle hook does not yet compose with an application-authored `SetParametersAsync` override. Async callbacks, request-body and form binding, multiple actions on one verb, multiple routes or components, navigation, exception and cancellation behavior, `ShouldRender` overrides, and streaming SSR remain unexercised.
- The issue #85 and #87 hosts run on Windows TestServer with the stock ephemeral Data Protection provider. They do not exercise Kestrel, TLS, persistent key storage, server-farm key sharing, Linux, a browser, or an application-selected HTMX runtime.
- The tests do not exercise layouts, caching, concurrency, enhanced navigation, interactive render modes, fragments, browser behavior, or performance.
- The legacy test application still uses internal private-reflection discovery and global service replacements. Later slices must replace the behavior they cover instead of extending that prototype.
- HTMX-only component routes remain unproved. PUT, PATCH, and DELETE now have an internal runtime proof driven by test-only fixed descriptors, but no public generated consumer path. Per-component POST discovery beyond the one stock named form also remains unproved.

## Recommended next slice

A separate generator slice should emit the proved descriptor and component
lifecycle contract for statically discoverable method groups. It must first
settle composition with an application-authored `SetParametersAsync` override
and must not promote this proof's internal registration shape into a public API
without that decision. Recheck the live v1 tracker and `origin/main` before
selecting or creating that issue.
