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
- Verified executable proof commit for issue #89: `d5153938a2142b49a6b9c5168c14fda4944e315e`.
- This issue #89 progress change is documentation-only. Executable claims are tied to the tested commit above, not to the later documentation head.
- Verified implementation commit for issue #91: `47da4a36eb4909f8d120ab032bb12435196a23b9`.
- This issue #91 progress change is documentation-only. Executable claims are tied to the tested implementation commit above, not to the later documentation head.
- Framework boundary under test: a real ASP.NET Core 10.0.11 and Blazor static SSR test host consuming the project-referenced `net8.0` Htmxor library.
- V1 slices proved on this tree: issue #78, stock `@page` routing with a direct HTMX GET; issue #81, every documented .NET 10 Blazor component-route constraint plus typed optional presence and absence; issue #83, authorization-policy and authenticated-user parity for normal and direct GETs; issue #85, one stock named `EditForm` POST with form binding, antiforgery ordering, request-component callback dispatch, and direct component output; issue #87, one shared runtime path for component-owned PUT, PATCH, and DELETE actions represented by fixed future-generator output; issue #89, composition of that assumed generated action output with an application-authored asynchronous parameter lifecycle override; issue #91, one assumed-generated constrained HTMX-only GET route for a component without `@page`, using stock Blazor invocation and static SSR.
- Current implementation slice after #91: none. Recheck issue #91, branch publication state, and `origin/main` before starting the next slice.

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

Protected behavior for issue #89:

> When a Razor component already overrides `SetParametersAsync`, assumed
> generated action code preserves that application lifecycle method and invokes
> the matching unsafe action exactly once after parameter processing completes.

The hosted proof uses one component-owned `@page` route, one asynchronous
application override, and one DELETE action. The override awaits stock parameter
processing, yields before recording its own completion, and requests the render
that exposes that application state. A hand-authored `.g.cs` stand-in adds
`IComponent` to another partial declaration and explicitly reimplements
`IComponent.SetParametersAsync`. It awaits the component's public virtual
`SetParametersAsync` method, which preserves the application override, before it
atomically consumes and invokes the armed action.

This composition follows public contracts. ASP.NET Core 10.0.11 stores and
invokes rendered components through [`IComponent`](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Components/src/IComponent.cs#L6-L27),
while [`ComponentBase.SetParametersAsync`](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Components/src/ComponentBase.cs#L210-L250)
remains public and virtual. C# merges interfaces across
[partial declarations](https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/classes#1527-partial-type-declarations)
and permits a derived type to
[reimplement an inherited interface](https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/interfaces#1967-interface-re-implementation).
The generated explicit implementation changes the `IComponent` dispatch target,
while its ordinary virtual call reaches the application override. The inherited
`ComponentBase` implementation still supplies `IComponent.Attach`.

An ordinary authorized GET renders the stock application shell. It runs the
application override, initialization, and parameter callback once, completes the
override's asynchronous work, and does not invoke the unsafe callback. An
authorized, antiforgery-valid DELETE creates a new request component and observes
the ordered sequence `override-start`, `initialized`, `parameters-set`,
`override-complete`, then `callback`. Route value `42`, query value `from-query`,
authenticated user `issue-89-user`, the request-scoped dependency, and the
application's completed state all reach the callback. The callback runs once and
its state appears in the direct response.

No production runtime change was needed. Exact-once action dispatch comes from
the request-scoped descriptor's atomic `TryConsume`, not from an assumption that
Blazor supplies parameters only once. The stand-in proves neither source-generator
behavior nor a final emitted API.

Protected behavior for issue #91:

> When a component without `@page` declares an HTMX-only GET route, assumed
> generated registration maps that route through the stock Blazor invoker so an
> authorized direct HTMX request receives the request-owned component, while a
> normal GET cannot reach it.

The ASP.NET Core 10.0.11 hosted proof uses one component without `@page`, one
`/reports/{ReportId:int}` GET route under an application route group, one query
value, one claim policy, and one request-scoped dependency. A hand-authored
`.g.cs` stand-in supplies only the component type, normalized constrained route,
GET and authorization metadata, and registration on the exact route group. It
does not prove source-generator behavior or a final generated API.

The shared internal registration maps the normalized route with the public
[`MapGet`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.endpointroutebuilderextensions.mapget?view=aspnetcore-10.0)
overload that accepts an explicit `RequestDelegate`. That delegate resolves the
public `IRazorComponentEndpointInvoker` and calls `Render`, matching the stock
[Razor component endpoint factory's invocation path](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Builder/RazorComponentEndpointFactory.cs#L18-L68).
The endpoint carries component, direct-root, link-suppression, route, and
authorization metadata. Mapping it on the exact route group preserves the
group's prefix and application metadata marker.

A direct-only matcher removes this endpoint from normal requests; it grants no
authority. ASP.NET Core authorization still enforces the endpoint policy. The
stock invoker initializes route state, and the stock static SSR renderer creates
the request-owned component. A narrow direct root renders that route data without
a `Router` route-table lookup, which the component cannot satisfy because it has
no `@page`. This path neither copies renderer code nor replaces Blazor services.

An authorized direct GET returns `200` without the stock shell and renders one
initialized component containing route value `42`, query value `from-query`,
identity `issue-91-user`, the scoped dependency, and the group marker. A normal
GET returns `404`, an anonymous direct GET returns `401`, rejected constrained
input returns `404`, and POST, PUT, PATCH, and DELETE return `405`. Exactly one
component endpoint owns the prefixed route. The host application authors no
matching handler; the checked-in `.g.cs` stand-in supplies the assumed-generated
registration.

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
- Meaningful red for issue #89 is preserved at `4561dc26d1d80f6c776ca46a3131e66982aed164`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue89LifecycleCompositionTests" --blame-hang --blame-hang-timeout 5min` discovered and executed one hosted test; 0 passed, 1 failed, 0 skipped. The normal request first proved one completed application lifecycle pass with no action. The authorized, antiforgery-valid DELETE then returned `200`, completed the application override once, and rendered route, query, user, request scope, and application state, but the response retained callback count `0`; the assertion required `1`.
- Focused proof at clean executable commit `d5153938a2142b49a6b9c5168c14fda4944e315e`: the same command discovered and executed one hosted test; 1 passed, 0 failed, 0 skipped.
- Issue #87 regression proof at the same clean commit used `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue89LifecycleCompositionTests|FullyQualifiedName~Issue87UnsafeActionTests" --blame-hang --blame-hang-timeout 5min`; 12 cases were discovered, executed, and passed with 0 failures or skips. This retained issue #87 method identity, authorization metadata, and antiforgery coverage alongside the composition proof.
- Broader proof at the same clean commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 102 quality tests, 39 .NET 10 hosted tests, and 150 existing non-browser tests. Total: 291 discovered, 291 executed, 291 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Mutation testing was not run. Issue #89 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for issue #91 is preserved at `319a23680d2b89b7eed39504c9e974e0e3772cae`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue91HtmxOnlyRouteTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 hosted test; 0 passed, 1 failed, 0 skipped. The authorized direct HTMX GET expected `200` but received `404` because the component had neither a stock `@page` endpoint nor an assumed-generated HTMX-only endpoint.
- Focused proof at clean implementation commit `47da4a36eb4909f8d120ab032bb12435196a23b9`: the same command discovered and executed 1 hosted test; 1 passed, 0 failed, 0 skipped.
- Broader proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj --no-restore -- check --profile fast` passed 102 quality tests, 40 .NET 10 hosted tests, and 150 existing non-browser tests. Total: 292 discovered, 292 executed, 292 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- The first Standards review found one P3 root-component naming ambiguity. The implementation commit was amended, affected evidence was rerun, and separate Standards and Spec rereviews of `9c89b7f5629b53a1dfed8fd1186dd44d374524c6...47da4a36eb4909f8d120ab032bb12435196a23b9` passed with zero actionable findings.
- Mutation testing was not run. Issue #91 makes it optional diagnostic evidence for this proof of concept.

## Remaining limits

- This is a project-reference proof, not packed-package or release-candidate evidence.
- The matrix uses one representative valid and rejected value per documented constraint. It does not exhaust textual representations, undocumented custom conversion constraints, catch-all routes, or unconstrained routes.
- The direct path is proved on ASP.NET Core 10 only. The supported framework matrix and packed-package consumption remain unproved.
- The authorization proof uses one deterministic scheme and one claim policy. It does not cover scheme selection, custom challenge or forbid handlers, identity-provider integration, or authorization on other HTTP methods.
- The issue #85 proof covers one stock named `EditForm`, one valid value, and one missing-token POST. It does not cover multiple forms, validation failures, invalid-token variants, file uploads, normal POST parity, or custom method discovery. Issue #87 proves unsafe route/query instance dispatch separately, without request-body or form binding.
- The issue #87, #89, and #91 stand-ins assume future generator output. Generator discovery, Razor expression and route-metadata analysis, route normalization, generated registration, diagnostics, final public API, and packaged-consumer registration remain unproved.
- Issue #89 covers an application-authored public `SetParametersAsync` override. An application that explicitly implements `IComponent.SetParametersAsync` would conflict with the generated explicit member and needs a future diagnostic or developer-model decision. Repeated parameter delivery, an override that intentionally omits its base call, async actions, request-body and form binding, multiple actions on one verb, multiple routes or components, navigation, exception and cancellation behavior, `ShouldRender` overrides, and streaming SSR remain unexercised.
- The issue #85, #87, and #89 hosts run on Windows TestServer with the stock ephemeral Data Protection provider. They do not exercise Kestrel, TLS, persistent key storage, server-farm key sharing, Linux, a browser, or an application-selected HTMX runtime.
- Issue #91 also ran only on Windows TestServer. It did not exercise Kestrel, TLS, Linux, a browser, an application-selected HTMX runtime, or packed-package consumption.
- The tests do not exercise layouts, caching, concurrency, enhanced navigation, interactive render modes, fragments, browser behavior, or performance.
- The legacy test application still uses internal private-reflection discovery and global service replacements. Later slices must replace the behavior they cover instead of extending that prototype.
- Issue #91 proves one assumed-generated HTMX-only GET route with an `int`
  constraint, one authorization policy, and one application route-group metadata
  marker. It does not prove typed route-value conversion through this new seam,
  other constraints, multiple generated routes or components, collisions,
  normal-only or dual generated reachability, HEAD or OPTIONS behavior, or the
  full range of application group and security conventions. PUT, PATCH, and
  DELETE still have only test-stand-in generated descriptors, and per-component
  POST discovery beyond the stock named-form proof remains unproved.

## Recommended next slice

A narrow source-generator tracer should replace `Issue91GeneratedRoute.g.cs` for
one component without `@page` and one statically discoverable HTMX-only GET
route. Its protected behavior should be: when that component declares the route,
Htmxor emits the proved descriptor and exact-group registration so the hosted
HTTP behavior passes without checked-in generated output. The slice should add
compilation evidence and useful diagnostics for its one supported shape, without
widening into normal-only or dual reachability, unsafe actions, packaging, or a
final public generated API. Recheck the live v1 tracker and `origin/main` before
selecting or creating the issue.
