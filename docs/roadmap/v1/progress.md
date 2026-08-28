# Htmxor v1 progress

Last updated: 2026-08-28

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
- Verified implementation and compilation-test commit for issue #93: `0f8d4d761c89afc860ec0cd5058b2b65fd737ee9`.
- Verified post-review fix commit for issue #93: `cf8cbb38bea4374636e072688e8da5927d6296f8`.
- This issue #93 progress change is documentation-only. Executable claims are tied to the tested implementation and post-review fix commits above, not to the later documentation head.
- Verified executable fix commit for issue #95: `58fa7aece281f053b1b7bffeec7ebcb8f7dfb33e`, based on exact `origin/main` commit `55e8d23ea18d4a0c8068be436afc95256a97be09`.
- This issue #95 progress change is documentation-only. Executable claims are tied to the tested commit above, not to the later documentation head.
- Verified implementation commit for issue #97: `a94cf491205ed12863ad8ed0ca623a1a7b686c6b`, based on exact fetched `origin/main` commit `e222f75e72f152718c43c534944717dc1a62c51a`.
- Verified compiler-backed follow-up commit for issue #97: `3dc8350de488ace5d02d4244bdd87ef9953d0469`, based on merged `origin/main` commit `7f88974aa94bb77c8a50cdff7ecd92f4e7993861`.
- Verified post-review constrained-route fix for issue #97: `f02a1c84dde19ed5221396339ce22ac4e936bbc6`.
- This issue #97 progress change is documentation-only. Executable claims are tied to the tested implementation commits above, not to the later documentation head.
- Framework boundary under test: ASP.NET Core 10.0.11 and Blazor static SSR on TestServer. Issues #95 and #97 use a separate external .NET 10 Razor consumer that restores a locally packed `net8.0` Htmxor package instead of referencing an Htmxor project.
- V1 slices proved on this tree: issue #78, stock `@page` routing with a direct HTMX GET; issue #81, every documented .NET 10 Blazor component-route constraint plus typed optional presence and absence; issue #83, authorization-policy and authenticated-user parity for normal and direct GETs; issue #85, one stock named `EditForm` POST with form binding, antiforgery ordering, request-component callback dispatch, and direct component output; issue #87, one shared runtime path for component-owned PUT, PATCH, and DELETE actions represented by fixed future-generator output; issue #89, composition of that assumed generated action output with an application-authored asynchronous parameter lifecycle override; issue #91, one assumed-generated constrained HTMX-only GET route for a component without `@page`, using stock Blazor invocation and static SSR; issue #93, build-time discovery and emission for that one constrained HTMX-only GET route without checked-in generated output; issue #95, analyzer packaging and one application-level registration that connects the generated route to runtime in an external package-only consumer; issue #97, deterministic aggregation of two supported package-consumer declarations through that single registration call.
- Current implementation slice: issue #97. The live v1 milestone contains issue #97 and parent issue #77; no later child is implementation-ready.

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

Protected behavior for issue #93:

> When a component without `@page` declares one statically discoverable
> HTMX-only GET route, Htmxor emits the proved descriptor and exact-group
> registration so the hosted HTTP behavior passes without checked-in generated
> code.

The [.NET 10 Razor SDK source-generator targets](https://github.com/dotnet/sdk/blob/v10.0.400/src/RazorSdk/Targets/Microsoft.NET.Sdk.Razor.SourceGenerators.targets#L15-L72)
supply `.razor` files as compiler `AdditionalFiles` to a `netstandard2.0`
incremental generator, project-referenced through documented
[`ProjectReference` metadata](https://learn.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-items?view=visualstudio#projectreference).
The generator consumes the raw files through Roslyn's public
[`AdditionalTextsProvider`](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md#additional-file-transformation)
and reads the public `RootNamespace` and `MSBuildProjectDirectory` analyzer
properties. It uses no custom target, private Razor API, generated Razor C#,
`obj` scraping, or renderer reflection.

The supported tracer recognizes exactly the current project-root component:
one literal `/reports/{ReportId:int}` route, explicit GET only, one literal
`issue-91-policy` authorization policy, and no `@page`. It emits the same
`Issue91GeneratedRoute` descriptor and registration shape that issue #91 proved,
so the existing application call registers on the exact route group and retains
its prefix and metadata. The checked-in `Issue91GeneratedRoute.g.cs` stand-in is
removed.

Compilation evidence verifies that the emitted descriptor and registration
compile against the required runtime seam. A representative explicit POST
declaration emits no source and reports one deterministic `HTMXOR001` error at
the declaration. More than one authorization declaration is also unsupported;
the generator emits no source rather than silently dropping an effective policy.
The unchanged hosted issue #91 matrix retains `200` for an authorized direct GET,
`404` for a normal GET and rejected constraint, `401` for an anonymous direct
GET, and `405` for POST, PUT, PATCH, and DELETE. This tracer does not establish a
final public generator API or packaged-consumer contract.

Protected behavior for issue #95:

> When a .NET 10 Blazor static SSR application references only a locally packed
> Htmxor package, uses one application-level Htmxor registration, and declares
> the supported HTMX-only GET route, Htmxor generates and registers that route
> so an authorized direct HTMX request receives the component without
> per-component endpoint code.

The Htmxor package now carries `Htmxor.Generators.dll` and its portable PDB at
the documented NuGet analyzer path `analyzers/dotnet/cs`. A private, build-only
project reference orders the generator build without adding the generator or
Roslyn packages to the package dependency graph. The separate consumer restores
an exact local Htmxor package version and contains no Htmxor project reference,
`InternalsVisibleTo`, direct generated-type reference, per-component endpoint
registration, or application-authored endpoint handler.

The generated source adds one internal overload for the existing
`AddHtmxorComponentEndpoints` registration call when its endpoint argument is
the exact `RouteGroupBuilder`. That overload invokes the existing application
registration and a hidden public infrastructure bridge with the generated
component type, normalized route, and authorization policy. The runtime bridge
validates that route and policy against the component type, copies its effective
public attributes with inheritance, constructs the internal GET descriptor, and
maps it on the supplied group. This keeps the generated-to-runtime connection
out of application source and uses no private reflection, copied renderer code,
controller, or Minimal API endpoint.

The package-only consumer proves an authorized direct HTMX GET returns `200`
with route value `42`, authenticated user, and application group metadata, while
omitting the stock HTML shell. A normal GET returns `404`, and an anonymous
direct HTMX GET returns `401`. The group prefix and metadata remain effective.
One host constraint declared on the component's C# partial type is also
effective: the allowed host reaches the component and a different host returns
`404`.
Package inspection verifies the runtime assembly and analyzer locations, no
generator or Roslyn dependency in the nuspec, and no generator or Roslyn
assembly in the consumer runtime dependency graph or output.

Protected behavior for issue #97:

> When a package-only .NET 10 Blazor static SSR application declares two
> supported HTMX-only GET components and calls Htmxor registration once, Htmxor
> maps both routes so each authorized direct HTMX request reaches its own
> component while normal requests cannot reach either.

The source generator now reads only Razor additional-file paths. It emits one
sorted manifest of project-root component metadata names and one application
registration extension; it contains no route, policy, component `typeof`, or
per-component endpoint code. A packaged diagnostic analyzer receives the final
compilation after Razor generation and validates the real component symbols and
their bound `AttributeData` by exact type identity. Compiler-equivalent array
creation, collection expressions, aliases, component-local constants, combined
and multiline attribute lists, and post-markup directives therefore converge on
their compiler values without Htmxor reading or parsing Razor text.

The analyzer reports deterministic nonconfigurable `HTMXOR001` errors for every
compiler-valid Htmxor declaration outside the supported envelope. The generated
extension passes its application assembly, sorted manifest, and exact caller
route group to a runtime catalog. That catalog scans exact compiled attributes,
validates and constructs the complete descriptor set before mapping any
endpoint, then performs application-level Htmxor registration once and maps both
routes in type-name order. This preserves fail-closed startup behavior even if
the analyzer is bypassed.

The package-only consumer declares exactly two project-root components with the
original `HtmxRouteAttribute`, no `@page`, distinct constrained GET routes,
distinct authorization policies, distinct output, and distinct effective
`Host` metadata from their C# partial types. Its summary route and authorization
policy use component-local constants declared in `@code`; both directives occur
after markup and that code block, and the code also contains `"@*"` as ordinary
C# string content. The route uses a collection expression and the policy uses
the attribute constructor. One `/issue-97-group` route group, one
`MapRazorComponents` call, and one application-level Htmxor registration produce
exactly two component endpoints. Each route returns `200` only for its own
authorized direct HTMX request, renders its own output without the stock shell,
and retains its own compiled route attribute, authorization policy, host, and
group metadata. Normal requests return `404`; the other component's policy
returns `403`; anonymous direct requests return `401`; and rejected route
constraints or hosts return `404`.

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
- Meaningful red for issue #93 is preserved at `c3be62f0886117667afdc0e1f2ef97511785ed10`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue91HtmxOnlyRouteTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 hosted test; 0 passed, 1 failed, 0 skipped. With the declaration present and checked-in generated stand-in absent, the authorized direct HTMX GET expected `200` but received `404`.
- Compilation proof at clean implementation commit `0f8d4d761c89afc860ec0cd5058b2b65fd737ee9`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorRouteGeneratorTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 2 tests; 2 passed, 0 failed, 0 skipped. The supported declaration emitted compiling descriptor and exact-group registration source. The unsupported explicit POST declaration emitted no source and one deterministic `HTMXOR001` error.
- Focused hosted proof at the same clean implementation commit: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue91HtmxOnlyRouteTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 hosted test; 1 passed, 0 failed, 0 skipped.
- Hosted-project proof at the same clean implementation commit without the filter discovered and executed 40 tests; 40 passed, 0 failed, 0 skipped.
- Fast-profile proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 102 quality tests, 40 .NET 10 hosted tests, and 152 non-browser library and generator tests. Total: 294 discovered, 294 executed, 294 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` passed 102 quality tests, 40 .NET 10 hosted tests, and all 154 library, generator, and browser tests. Total: 296 discovered, 296 executed, 296 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its Cobertura report was `artifacts/results/full/htmxor/26c8a3f7-9950-475f-b3b3-aa5473a791ce/coverage.cobertura.xml`, with two fresh copies recorded by the profile.
- The first independent Standards review of `c3408fc969883f4862d9c6f5c38d698d92931e36...ccfcf1c3a7c505e0481b3571d7850be93e1b80b0` found one P1: a second authorization declaration was accepted but omitted from generated endpoint metadata. The independent Spec review passed with zero actionable findings.
- Review-fix TDD red used the reviewed `ccfcf1c3a7c505e0481b3571d7850be93e1b80b0` tree plus the new test only: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Multiple_authorization_policies_report_one_deterministic_diagnostic" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 0 passed, 1 failed, 0 skipped because the old generator emitted source. After the production fix, the same command passed 1 of 1.
- Focused post-review proof at clean fix commit `cf8cbb38bea4374636e072688e8da5927d6296f8`: the generator-test command with filter `FullyQualifiedName~HtmxorRouteGeneratorTests` discovered and executed 3 tests; 3 passed, 0 failed, 0 skipped. The hosted issue #91 filter discovered and executed 1 test; 1 passed, 0 failed, 0 skipped.
- Fast-profile post-review proof at the same clean fix commit passed 102 quality tests, 40 .NET 10 hosted tests, and 153 non-browser library and generator tests. Total: 295 discovered, 295 executed, 295 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile post-review proof at the same clean fix commit passed 102 quality tests, 40 .NET 10 hosted tests, and all 155 library, generator, and browser tests. Total: 297 discovered, 297 executed, 297 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its Cobertura report was `artifacts/results/full/htmxor/abd714d5-512c-4559-a80e-bb7a21141143/coverage.cobertura.xml`, with two fresh copies recorded by the profile.
- Mutation testing was not run. Issue #93 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for issue #95 is preserved as Git tree `6c6c18d01ff427c0d6c0d9fd09523b0bdba8252a` over base `55e8d23ea18d4a0c8068be436afc95256a97be09`. The focused `PackedPackageConsumerTests` command packed the unchanged Htmxor package, restored and built the separate .NET 10 consumer, and discovered and executed one outer test and one hosted consumer test. Both failed only because the authorized direct HTMX GET expected `200` but received `404`; pack, restore, build, and test discovery all succeeded, with no generator-load error.
- Focused package proof at immutable executable tree `9952bb350bd3262e3fde6c755737430e861689d9`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 1 of 1 outer tests. Its parsed inner TRX also recorded 1 discovered, 1 executed, and 1 passed. The same test inspected the packed analyzer and runtime assets, package dependencies, authored consumer source, and runtime output.
- Generator compilation proof at the same executable tree: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorRouteGeneratorTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 3 of 3 tests.
- Existing issue #91 hosted regression proof at the same executable tree: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue91HtmxOnlyRouteTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 1 of 1 hosted tests.
- Fast-profile proof at the same executable tree: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 103 quality tests, 40 .NET 10 hosted tests, and 153 non-browser library and generator tests. Total: 296 discovered, 296 executed, 296 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same executable tree: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` passed 103 quality tests, 40 .NET 10 hosted tests, and all 155 library, generator, and browser tests. Total: 298 discovered, 298 executed, 298 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its Cobertura report was `artifacts/results/full/htmxor/111c5d18-fc35-4fa5-aa70-c7569a9b4d77/coverage.cobertura.xml`.
- The first independent Spec review of complete tree `8a44b07001a82137066304cfc0955a7aae856b5a` passed with zero actionable findings. The separate Standards review found two P1 risks: package-wide activation of the intentionally narrow tracer, and silent loss of unrepresented component security metadata.
- An initial review-fix red is preserved as Git tree `bb11835a1e167390d76fa1e4abbcc36583f690de`: one generator test failed because a third inline component attribute still produced registration source. A narrow inline-only rejection made that test pass, but the Standards rereview of tree `73073b5f236563f953447714f3985a36c7ad6606` correctly found that C# partial, inherited, and imported attributes remained invisible to the raw Razor parser. That partial fix was removed.
- Effective-metadata red is preserved as Git tree `0c75ebf5fbfd4519c3d599295fcb4079770ab4c0`. The focused packed-consumer command completed pack, restore, build, generator loading, and one hosted test; the wrong-host request expected `404` but received `200` because the bridge had dropped `[Host]` from the component's C# partial type. At executable tree `9952bb350bd3262e3fde6c755737430e861689d9`, the same outer and inner tests each passed 1 of 1 after the bridge copied public component attributes with inheritance.
- Post-metadata independent Spec rereview of complete tree `c8bd8e37612fc4c80588d8f7ee33dcb12788e54c` passed with zero actionable findings. The separate Standards rereview found no remaining implementation defect, confirmed the component-metadata risk was fixed, and retained package-wide activation as a developer-model decision gate. On 2026-08-28 the user accepted that compatibility break for this non-publishing spike because the published Htmxor package remains a beta. It is release debt, not a blocker for issue #95.
- Pull request #96 initially published exact head `fdb41ba684bace69adbaabf9c219568cf810fa2a`. GitHub Actions run `33151983806` passed package creation, the test job, dependency review, Infer#, and all CodeQL analyses. NuGet validation alone failed with rule 111, `Symbol file not found`, for `analyzers/dotnet/cs/Htmxor.Generators.dll`; the package had omitted the generator's existing portable PDB. This was a package-content failure, not runner or setup evidence.
- CI-fix red is preserved at test-first commit `2d3d85f0604d7e2d668cbb9d93d3c3fd404b857f`. The focused package-consumer command completed pack, restore, build, generator loading, and its hosted test before the outer test failed because `analyzers/dotnet/cs/Htmxor.Generators.pdb` was absent. Fix commit `58fa7aece281f053b1b7bffeec7ebcb8f7dfb33e` packs that PDB beside the analyzer DLL; the focused outer and parsed inner tests each passed 1 of 1.
- Post-CI-fix fast proof at `58fa7aece281f053b1b7bffeec7ebcb8f7dfb33e` passed 103 quality tests, 40 .NET 10 hosted tests, and 153 non-browser library and generator tests: 296 discovered, 296 executed, and 296 passed with no failures, skips, errors, or timeouts. The Release build produced 0 warnings and 0 errors. The full profile passed 103 quality, 40 hosted, and all 155 library, generator, and browser tests: 298 discovered, 298 executed, and 298 passed with no failures, skips, errors, or timeouts. Its fresh Cobertura report was `artifacts/results/full/htmxor/cc201443-d4b7-44bc-90b5-dc97fb0f99ba/coverage.cobertura.xml`.
- Meziantou NuGet validator 2.0.3, run locally with `ContinuousIntegrationBuild=true`, reported no analyzer symbol-location or deterministic-path errors after the fix. It could not validate the two analyzer source URLs because commit `58fa7aece281f053b1b7bffeec7ebcb8f7dfb33e` had not yet been pushed; final source-link and package validation therefore remain a publication-boundary CI check.
- The referenced base-main CI test job did not execute tests because `packages.microsoft.com` returned `403` while Playwright installed Ubuntu dependencies. This is external setup evidence, not a passing baseline or a product failure. Issue #95 does not change the runner.
- Mutation testing was not run. Issue #95 makes it optional diagnostic evidence for this proof of concept.
- Baseline test-only evidence for issue #97 is preserved at commit `86e53fabdbb60945b800e0af117e097de90c9ff0`, whose production tree is unchanged from exact base `e222f75e72f152718c43c534944717dc1a62c51a`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Two_supported_declarations_emit_one_deterministic_compiling_registration" --blame-hang --blame-hang-timeout 5min` discovered and executed one test; it failed with two `HTMXOR001` diagnostics because the generator rejected both supported declarations. The packed consumer at the same test-only tree likewise reached consumer compilation and failed with `HTMXOR001`. These are expected compilation failures, not meaningful behavioral red.
- Meaningful red is preserved at commit `65a636da4b7b0b8c1e9533dec2133bfc09d334d3`, whose exact tree is `37369dba82ed8fdbdb1273de2845bcee37f685e7`. The focused `PackedPackageConsumerTests` command packed the package, restored and built the consumer, loaded the generator, and discovered and executed one outer test plus two inner hosted tests. The report test passed with `200`; the summary test failed with expected `200` but actual `404` because the controlled generator emitted only the first validated registration. The temporary one-route control is completed by the following implementation commit.
- Focused generator proof at clean implementation commit `a94cf491205ed12863ad8ed0ca623a1a7b686c6b`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorRouteGeneratorTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 5 of 5 tests. The two supported declarations emitted one byte-identical generated source regardless of input order, both registrations compiled, and a supported-plus-unsupported set emitted no source with one deterministic diagnostic.
- Focused package proof at the same clean implementation commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 1 of 1 outer tests. Its parsed inner TRX recorded 2 discovered, 2 executed, and 2 passed. The outer test also checked the local package assets, consumer dependency and output boundaries, exactly two authored route and policy declarations, one route group, one component mapping, one Htmxor registration call, and no generated-type or per-component endpoint code.
- Existing single-route regression proof at the same clean implementation commit: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue91HtmxOnlyRouteTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 1 of 1 hosted tests.
- Fast-profile proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 103 quality tests, 40 .NET 10 hosted tests, and 155 non-browser library and generator tests. Total: 298 discovered, 298 executed, 298 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` passed 103 quality tests, 40 .NET 10 hosted tests, and all 157 library, generator, and browser tests. Total: 300 discovered, 300 executed, 300 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its fresh Cobertura report was `artifacts/results/full/htmxor/101680d4-a3ff-4099-831a-10326ea5027f/coverage.cobertura.xml`.
- Mutation testing was not run. Issue #97 makes it optional diagnostic evidence and does not require unrelated mutant repair.
- Compiler-backed follow-up negative control used exact Git tree `471a19734492799f1886eb6b1981db51a49738c9` over clean commit `75b1dbc4873dc1ad466ed48c445813716f94d4e3`. The only mutation changed registration rendering to `declarations.Take(1)`. The focused `PackedPackageConsumerTests` command packed the package, restored and built the .NET 10 consumer, loaded the generator, and discovered and executed one outer test plus two inner hosted tests. The report test passed; the summary test failed with expected `200` but actual `404`. Inner totals were 2 discovered, 2 executed, 1 passed, 1 failed, 0 skipped, 0 errors, and 0 timeouts. The temporary mutation was immediately reverted and the worktree returned to the clean parent tree.
- Compiler-backed fast-profile proof at implementation commit `38dc18473a5b4d84714833a6cccbe9518ec80a12`: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 103 quality tests, 40 .NET 10 hosted tests, and 162 non-browser library and generator tests. Total: 305 discovered, 305 executed, 305 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. The only worktree difference reported by the command was the then-untracked research note; production and test inputs matched the commit.
- Final-compilation focused proof at clean commit `3dc8350de488ace5d02d4244bdd87ef9953d0469`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorRouteGeneratorTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 23 of 23 tests. One generator test proves `AdditionalText.GetText()` is never called and emission is input-order independent. Sixteen analyzer tests exercise final-compilation symbol and typed-constant validation, mapped nonconfigurable diagnostics, component-local constants, aliases, array forms, unsupported filters and authorization, declarations outside the root manifest, and the two-component ceiling. Six runtime tests exercise compiled metadata, distinct paired descriptors, group metadata, declarations outside the manifest, unrelated unrouted manifest entries, and zero mappings when the second declaration or its metadata construction fails.
- Focused package proof at the same clean commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` passed 1 of 1 outer tests. Its parsed inner TRX recorded 2 discovered, 2 executed, and 2 passed. The consumer was restored, Release-built, and hosted on .NET 10 from the locally packed package. The first sandboxed run was not product evidence because Windows Event Log access denied while reporting an underlying exception; the same command outside that boundary passed.
- Fast-profile proof at the same clean commit passed 103 quality tests, 40 .NET 10 hosted tests, and 173 non-browser library, generator, analyzer, and runtime tests. Total: 316 discovered, 316 executed, 316 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same clean commit passed 103 quality tests, 40 .NET 10 hosted tests, and all 175 library, generator, analyzer, runtime, and browser tests. Total: 318 discovered, 318 executed, 318 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. The profile retained two identical coverage copies; the canonical report was `artifacts/results/full/htmxor/6894e6f5-d7f9-4003-8f21-3dbb7547490a/coverage.cobertura.xml`.
- The final-compilation, package, fast, and full proofs used .NET SDK 10.0.400 on Microsoft Windows NT 10.0.26200.0.
- Independent Standards and Spec reviews of exact clean head `ef894aa32618e6f78ac1b96b4bae6e21a4508d5a` each found the same P2 defect: the analyzer's colon heuristic accepted compiler-valid route constants such as `/{Id:}`, `/{Id=foo:bar}`, or a valid constrained parameter followed by an unclosed parameter, while the runtime route parser rejected them only during startup.
- Review-fix TDD red used production head `ef894aa32618e6f78ac1b96b4bae6e21a4508d5a` plus only the three new analyzer theory cases now retained in `f02a1c84dde19ed5221396339ce22ac4e936bbc6`. The focused unsupported-metadata command compiled and executed 12 cases; 9 passed and the 3 new cases failed because the analyzer returned no diagnostic. The fix links one narrow route-template contract into both analyzer and runtime assemblies. The same command then passed 12 of 12, and the complete generator, analyzer, and runtime selection passed 26 of 26.
- Post-review package proof at executable tree `f02a1c84dde19ed5221396339ce22ac4e936bbc6` passed 1 of 1 outer tests with 2 of 2 parsed inner hosted tests. The fast profile passed 103 quality, 40 .NET 10 hosted, and 176 non-browser library tests: 319 discovered, executed, and passed. The full profile passed 103 quality, 40 hosted, and all 178 library and browser tests: 321 discovered, executed, and passed. Both profiles had 0 failures, skips, errors, or timeouts and Release builds with 0 warnings or errors. The full profile's canonical coverage report was `artifacts/results/full/htmxor/27a133a0-a1fd-471e-941f-b5a55e95f78a/coverage.cobertura.xml`.
- Full-scope mutation was not run. It is optional diagnostic evidence for this issue and would include unrelated legacy production scope.

## Remaining limits

- Issues #95 and #97 prove one locally packed package with the current SDK and dependency set. They do not prove publishing, package signing, a release candidate, package compatibility across SDK or compiler versions, a fresh Linux restore, or a broader target-framework matrix.
- The matrix uses one representative valid and rejected value per documented constraint. It does not exhaust textual representations, undocumented custom conversion constraints, catch-all routes, or unconstrained routes.
- The direct path is proved on ASP.NET Core 10 only. The supported framework matrix remains unproved.
- The authorization proof uses one deterministic scheme and one claim policy. It does not cover scheme selection, custom challenge or forbid handlers, identity-provider integration, or authorization on other HTTP methods.
- The issue #85 proof covers one stock named `EditForm`, one valid value, and one missing-token POST. It does not cover multiple forms, validation failures, invalid-token variants, file uploads, normal POST parity, or custom method discovery. Issue #87 proves unsafe route/query instance dispatch separately, without request-body or form binding.
- The issue #87 and #89 unsafe-action stand-ins still assume future generator output. Their discovery, Razor expression and action-metadata analysis, diagnostics, and generated lifecycle integration remain unproved.
- Issue #89 covers an application-authored public `SetParametersAsync` override. An application that explicitly implements `IComponent.SetParametersAsync` would conflict with the generated explicit member and needs a future diagnostic or developer-model decision. Repeated parameter delivery, an override that intentionally omits its base call, async actions, request-body and form binding, multiple actions on one verb, multiple routes or components, navigation, exception and cancellation behavior, `ShouldRender` overrides, and streaming SSR remain unexercised.
- The issue #85, #87, and #89 hosts run on Windows TestServer with the stock ephemeral Data Protection provider. They do not exercise Kestrel, TLS, persistent key storage, server-farm key sharing, Linux, a browser, or an application-selected HTMX runtime.
- Issues #91, #93, #95, and #97 ran their hosted contract only on Windows TestServer. They did not exercise Kestrel, TLS, Linux runtime, a browser, or an application-selected HTMX runtime. The broader full profile exercised the existing Chromium tests on Windows, but did not prove fresh browser provisioning or the package-only routes in a browser.
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
- The issue #97 follow-up removes Razor-text interpretation. Its path-only
  generator does not claim the Razor grammar, while its diagnostic analyzer uses
  the final compilation and therefore sees component-generated members and
  compiler-bound attributes. Nested component directories or namespaces,
  future SDK or analyzer-pipeline changes, more than two routed components,
  multiple routes on one component, collision policy, normal-only or dual
  reachability, unsafe methods, and a final public API remain unproved.
- The compiler-bound route-declaration model is independent of the source file,
  but issue #97 proves only `HtmxRoute` attributes authored in its two
  project-root `.razor` files. The agreed v1 model also accepts the attribute on
  the matching `.razor.cs` partial or on a component authored entirely in C#;
  both discovery paths remain unproved and require real package-consumer
  tracers. The path-derived project-root manifest is an issue #97 eligibility
  filter, not the final declaration source. Every discovery path must feed the
  same compiler-bound validation and compiled-metadata runtime catalog, with the
  original compiled `HtmxRouteAttribute` authoritative. V1 does not treat
  `_Imports.razor` as an `HtmxRoute` declaration source. Effective non-route
  metadata from imports remains a separate unproved metadata-preservation case.
- Issues #95 and #97 package that exact tracer and no broader behavior. Generated
  registration overload selection requires the application to pass an endpoint
  argument whose static type is exactly `RouteGroupBuilder`. Widening it to
  `IEndpointRouteBuilder`, or passing the application instead, selects the
  existing fallback and does not register the generated route. Signature
  collisions, alternative registration shapes, and promotion of the hidden
  generated-to-runtime bridge into a final public developer API remain unproved
  and out of scope.
- The analyzer activates for every application that restores the package.
  Applications with more than two `HtmxRoute` components, or with a declaration
  outside the proved project-root route and authorization contract, receive
  build errors. The runtime catalog also scans the application assembly and
  fails closed if compiled declarations do not match the generated manifest.
  Exactly two project-root components are proved; multiple route attributes on
  one component, collision policy, nested namespaces, broader route filters,
  unsafe action discovery, IDE live-analysis parity, trimming, Native AOT, and
  startup cost remain unproved. This fail-fast behavior is accepted for the
  locally packed beta spike: unrelated unrouted manifest entries are ignored,
  while routed or security metadata is never silently omitted. It is not the
  final v1 compatibility contract and must be resolved before a stable release
  candidate.

## Recommended next slice

Under parent issue #77, select one package-only unsafe-action generation tracer
that replaces a single fixed issue #87 assumed descriptor without broadening the
Razor syntax matrix:

> When a package-only .NET 10 Blazor static SSR application declares one
> supported component-owned PUT callback and calls Htmxor registration once,
> Htmxor generates the action registration so an authorized, antiforgery-valid
> request reaches that component instance callback, while an invalid request
> cannot bind input or invoke it.

This comes next because issues #87 and #89 already prove the runtime method,
authorization, antiforgery, and lifecycle contract under fixed assumed output,
while issue #97 removes the immediate multi-component registration blocker. The
next evidence should keep both packaged GET routes green, preserve a buildable
and runnable `405` negative control before the action descriptor exists, and
then prove generated method and handler identity, callback dispatch, and
fail-closed unsupported input at the package boundary. Recheck the live tracker,
branch publication state, and `origin/main` before refining or filing that slice.
