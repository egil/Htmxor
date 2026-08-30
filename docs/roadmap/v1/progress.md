# Htmxor v1 progress

Last updated: 2026-08-30

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
- Verified executable proof commit for issue #100: `e03501dab0df0cf7efedc65cfab73419601d7ca8`, based on exact fetched `origin/main` commit `d6e440f0fcb029174571979062705681b7a94d46`.
- Verified post-review compiled-route fix for issue #100: `42082a1bacb71364f5ccf513c8b5e791528d83cf`.
- This issue #100 progress change is documentation-only. Executable claims are tied to the tested commits above, not to the later documentation head.
- Preserved meaningful-red commit for issue #103 after rebase: `371c1125a4442b6df688a686abbe8b49269721a6`.
- Verified implementation commit for issue #103: `fb7e31f3d8378d7b7ab8f521f862429da14dfb50`, based on exact fetched `origin/main` commit `b313e8dc6913ae7cbe424e86192aad7440761ac1`.
- Preserved post-review explicit-allow-list red for issue #103: `b78ff30f3c43acbef1ab99b69e51fad7b539879d`.
- Verified post-review explicit-allow-list fix for issue #103: `732a957c36d080ddef39ca24db744b7d0c803fa4`.
- Preserved issue #103 audit reds: effective antiforgery metadata ordering at `834e1058991c191c846ad6252180d7194397308d`, actionless unsafe-route validation at `9ed1938ca40e13cb2ac0d0066ba7b64c5220eab2`, static handler ownership at `adfb00626985a8c5d960af37b92e7dfbd412f342`, mutable omitted-method defaults at `c52ac7c9388d4b40c52205c556d7b03d9a1b4ba7`, and later supported markup at `bc2715c0d650bc434d6656414503764c274bf0ec`.
- Verified issue #103 audit fix commit: `217dd95642400509759d77c3bd8bc4ca53e178a6`, based on exact fetched `origin/main` commit `b313e8dc6913ae7cbe424e86192aad7440761ac1`.
- Preserved issue #103 audit-review static-delegate red: `cdc476767f947187864ae72d8dd45fc905a9999e`.
- Verified issue #103 audit-review fix: `6299b537ceb8954f191997310d9ddfc8c5dc0bee`.
- Preserved issue #103 second audit-review reds: multiline script raw-text binding at `0250a15f740a9d4c79e3d730784b2c9848287df4` and mismatched packaged partial filename at `75668806318873f0415e58c010eaec6e435039b7`.
- Verified issue #103 second audit-review fixes: `7c2d3365569751d3e63e7c5b19658452e2fced48` and `cfc995d9f14b89441224539d76c0279062ea52a4`.
- Preserved issue #103 third audit-review residual raw-text red: `2301078da8447b8a4c6e8d733962eeef7a18a80f`.
- Verified issue #103 third audit-review parser fix: `ce388a1f10fadb121a48ab6f259f62536a5b693b`.
- Preserved issue #103 fourth audit-review self-closing raw-text red: `67776f803b91a60582246d6c6d022ac8e79db872`.
- Verified issue #103 fourth audit-review parser fix: `f1a7884364ad241a32050e59115ea62cbbf1dae5`; the compiler-backed fail-closed component-markup boundary is recorded at `36d92a73f1151f14850632a1d45108e2a948bcca`.
- Preserved issue #103 final root-audit plaintext red: `a129585dfed8bf01c23b54b4131acc3f95f88fba`.
- Verified issue #103 final root-audit plaintext fix: `561bcc2da118ee09e515c037663eebdaf4cb27f6`.
- Preserved issue #103 imported-static ownership red: `7ef7af1ee1ca686c3417370282792041325d82c9`.
- Verified issue #103 imported-static ownership fix: `5177466f0afb09fd087b21d2bd44c04344c5b72b`.
- Preserved issue #103 inaccessible-base ownership red: `fcb89cebded23186d8e0df0833c244a0b5b4d6fb`.
- Verified issue #103 accessibility fix: `88631ad5cf3da3c7d44f111fefd4355c8bf3fc13`; the accessible inherited-handler control is recorded at `696d4539f68ea33a56aa6210412bec87895a2efa`.
- This issue #103 progress change is documentation-only. Executable claims are tied to the tested implementation, post-review, audit-fix, and audit-review-fix commits above, not to the later documentation head.
- Preserved meaningful-red commit for issue #106: `6285dae3646ff8357bdc413315dc1138c69b4de9`.
- Verified executable proof commit for issue #106: `b51b1644e394b2f8a8c9ca6072a7170fff6e5221`, based on exact fetched `origin/main` commit `a489f30f7a20ec801fe52b5ab4f894382d1d9c90`.
- Preserved post-review synthetic Razor-generator visibility control for issue #106: `b56547a2fd5c8922200632048445826b6f1a70da`.
- Verified post-review Razor-manifest compatibility hardening for issue #106: `8cc1badea33f950b43b51ed3d82f6d50e0373480`.
- Preserved post-review C# route-ownership red for issue #106: `21614bb1366482325296263dff7f2da3834f7951`.
- Verified post-review matching-code-behind fix for issue #106: `9e4b3565e95177154b8fdf9e79f3a9ae1b92d30b`.
- This issue #106 progress change is documentation-only. Executable claims are tied to the tested executable proof and post-review fix commits above, not to the later documentation head.
- Preserved meaningful-red commit for issue #108: `ad519a1cee4829f21f4a7678caf568fdac6fb755`.
- Verified executable proof commit for issue #108: `e1b9106553d1838a08916e76edc1ce1181ebd61b`, based on exact freshly fetched `origin/main` commit `5bcd9b89b5a8b885467e3c9f13da629f9cc1d32d`.
- Verified post-proof client-configuration cleanup for issue #108: `8302006d4bc2c6a0627f99c94376f6e0941f0e19`. This removes the obsolete public client-configuration surface instead of leaving it as a silent no-op after Htmxor stopped emitting client configuration.
- Verified Linux pre-publication evidence head for issue #108: `d2d3885c36a78572e93d72e0f9e038240bc9dc90`. This head has the same executable tree as the client-configuration cleanup plus the recorded issue #108 documentation.
- Preserved issue #108 pre-publication sample-ownership red: `bc60bff7829162c72c5bcf776d2895b5b5cf7298`. All three unsafe samples failed the new ownership guard because they advertised htmx 4 without retaining the legacy antiforgery configuration required by the current adapter.
- Verified issue #108 pre-publication review fix: `e2ac91524ec9ce911cb7f66a0fff7bbcee1ff4c2`. The unsafe samples now explicitly own their temporary legacy runtime and configuration, while the documentation gives an exact acquisition and hash-verification path for a fresh application's htmx 4 asset.
- This issue #108 progress change is documentation-only. Executable claims are tied to the tested Linux pre-publication and review-fix heads above, not to the later documentation head.
- Framework boundary under test: ASP.NET Core 10.0.11 and Blazor static SSR. Issues #95, #97, #100, #103, and #106 use a separate external .NET 10 Razor consumer on TestServer that restores a locally packed `net8.0` Htmxor package instead of referencing an Htmxor project. Issue #108 uses a separate package-only .NET 10 application on real Kestrel with Chromium.
- Product target correction authorized on 2026-08-28: v1 documentation,
  examples, browser conformance, and release evidence target an
  application-supplied htmx 4.0.0 script running with htmx 4 defaults. Htmxor
  does not embed or silently select that runtime. Issue #108 is the first narrow
  executed htmx 4 browser slice; the remaining conformance matrix is unproved.
- V1 slices proved on this tree: issue #78, stock `@page` routing with a direct HTMX GET; issue #81, every documented .NET 10 Blazor component-route constraint plus typed optional presence and absence; issue #83, authorization-policy and authenticated-user parity for normal and direct GETs; issue #85, one stock named `EditForm` POST with form binding, antiforgery ordering, request-component callback dispatch, and direct component output; issue #87, one shared runtime path for component-owned PUT, PATCH, and DELETE actions represented by fixed future-generator output; issue #89, composition of that assumed generated action output with an application-authored asynchronous parameter lifecycle override; issue #91, one assumed-generated constrained HTMX-only GET route for a component without `@page`, using stock Blazor invocation and static SSR; issue #93, build-time discovery and emission for that one constrained HTMX-only GET route without checked-in generated output; issue #95, analyzer packaging and one application-level registration that connects the generated route to runtime in an external package-only consumer; issue #97, deterministic aggregation of two supported package-consumer declarations through that single registration call; issue #100, one package-generated stock-page PUT callback bound to the compiled component endpoint while two explicit HTMX-only controls remain GET-only; issue #103, shared POST, PUT, PATCH, and DELETE inference for stock `@page` and omitted-`Methods` HTMX-only routes with explicit-method conflicts rejected before mapping; issue #106, explicit authoritative C# method discovery for matching `.razor.cs` partials and all-C# components, deterministic rejection and registration suppression when a C# declaration omits `Methods`, and no method widening from manual render-tree code; issue #108, removal of Htmxor-owned htmx distribution and one package-only application-owned htmx 4.0.0 stock-page and component-GET browser path.
- Current implementation slice: issue #108, application-owned htmx 4.0.0 GET.
  Issue #106 is closed. Positive omitted-`Methods` inference from companion Razor
  markup remains deferred for C# declarations under the parent v1 work.

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

Protected behavior for issue #100:

> When a package-only .NET 10 Blazor static SSR application declares a stock
> `@page "/reports/{id:int}"` component with one supported
> `@onput="PutReport"` callback and calls Htmxor registration once, Htmxor
> attaches the generated PUT action to that compiled stock endpoint. An
> authorized, antiforgery-valid request reaches the request-owned component
> callback, while an invalid request cannot bind input or invoke it. The stock
> GET and existing HTMX-only GET routes remain available and GET-only.

The narrow action generator recognizes one project-root stock page whose
supported one-line directive preamble contains one simple `@page` directive
and whose first markup start tag contains one double-quoted `@onput` simple
method-group binding. It uses `@page` only as an eligibility signal and never
copies or normalizes its route text. It does not inspect `hx-put`. The generated
token contains only the component type, PUT method, and server-owned handler
identity. The generated partial component hook first awaits normal
`SetParametersAsync` processing, consumes only that exact request token, and
invokes the method group through `EventCallback` on the request-owned instance.

Registration validates that the one generated PUT token belongs to the
project-root component manifest. The stock endpoint final convention matches
it through public `ComponentTypeMetadata` and creates the internal action
descriptor from the compiled endpoint's final route pattern. Authorization
therefore runs in the normal pipeline, and antiforgery completes before the
scoped action token is armed. Generated HTMX-only routes receive no action
descriptor; the two controls with explicit GET methods stay GET-only and have
no action or antiforgery metadata.

The locally packed consumer retains both issue #97 HTMX-only GET components
and adds one stock report page. Its normal GET returns `200` with the stock
shell. An authorized PUT with a valid token returns `200`, observes route value
`42`, query value `from-query`, the authenticated report user, a fresh
request-scoped dependency, and completed binding and initialization, invokes
the callback once, and renders the resulting state without the stock shell.
Missing or invalid antiforgery evidence returns `400`; the wrong user returns
`403`; and a different method or the `hx-put`-only summary route returns `405`.
All rejected requests record zero binding, initialization, and callback
activity. A forged client handler header cannot select a callback.

A computed `@onput` lambda fails the separate package-only Release build with
nonconfigurable `HTMXOR002` and produces no consumer assembly. Razor and HTML
comments, quoted attribute values including explicit raw-string expressions,
Razor code strings, raw attribute metadata, and script text do not declare an
action. The existing issue #87 stock `@page` PUT, PATCH, and DELETE stand-ins
remain separate and green.

Protected behavior for issue #103:

> When a package-only .NET 10 Blazor static SSR component owns a route through
> `@page` or `HtmxRoute` without `Methods`, Htmxor keeps GET implicit, adds only
> the POST, PUT, PATCH, and DELETE methods expressed by supported component
> bindings, and invokes only the matching request-owned callback after
> authorization and antiforgery succeed. Explicit methods remain authoritative,
> and client declarations never grant a server method.

The shared action generator recognizes simple double-quoted method-group
bindings for `@onpost`, `@onput`, `@onpatch`, and `@ondelete` on HTML elements or
Razor component tags, including a supported tag after a complete single-line
ordinary markup line. It can emit distinct actions for different unsafe methods
on one tag. A supported handler name must resolve only to instance methods;
static methods and delegate-valued fields or properties fail with
nonconfigurable `HTMXOR002`, so callbacks remain owned by the request component
instance. Prior ordinary markup is limited to self-closing syntax on an actual
HTML void element or one matching pair containing supported plain text;
incomplete, nested, non-void self-closing, `plaintext`, and raw-text shapes fail
closed. Stock components use their compiled `@page` endpoint as route owner; an
omitted-`Methods` `HtmxRoute` produces one HTMX-only endpoint with immutable
implicit GET plus only its declared unsafe methods. The runtime validates the
complete action and route set before adding endpoint conventions or mappings.

An explicit `HtmxRoute.Methods` set is authoritative. A supported binding whose
method belongs to that set is generated and mapped; a binding outside that set
produces deterministic nonconfigurable `HTMXOR002`, and runtime validation also
fails before mapping if analyzer diagnostics are bypassed. `HTMXOR002` has one
internal descriptor shared by analyzer and generator. Route declarations
originating from `_Imports.razor` are rejected for both stock and HTMX-only
owners. Client-only `hx-post`, `hx-put`, `hx-patch`, `hx-delete`, htmx 4
`hx-action` plus `hx-method`, and `hx-query` declarations emit no action and do
not alter the GET, POST, PUT, PATCH, and DELETE server allow-list.

Unsafe endpoint metadata is fail-closed by effective ordering: Htmxor appends
required antiforgery metadata when an earlier effective entry disables
validation. An explicit unsafe `HtmxRoute` validates the selected request before
rendering even when no generated action exists. Public default-method arrays are
fresh values and cannot mutate the catalog's internal omitted-route GET
invariant.

The locally packed consumer retains its two existing HTMX-only routes: the
summary route explicitly allows GET plus an actionless DELETE, while the report
route omits `Methods` and infers PATCH from a Razor component-tag binding. It
also adds a stock report-page DELETE. The PATCH handler lives in the matching
`.razor.cs` partial. An authorized, antiforgery-valid summary DELETE renders the
request component without a callback; missing and invalid tokens are rejected
before parameter binding or initialization. Other authorized,
antiforgery-valid requests reach only
their route- and method-selected request component, complete route/query
parameter delivery and initialization, invoke the selected callback once, and
render its state through static SSR. Representative wrong-method, cross-route,
cross-component, unauthorized, and antiforgery-invalid requests cannot select or
reach another callback. The application continues to use the single packaged
registration and authors no controller, Minimal API component endpoint, static
handler, renderer copy, private reflection, or global Blazor service replacement.

Protected behavior for issue #108:

> When a .NET 10 Blazor static SSR application supplies htmx 4.0.0, Htmxor
> emits and packages no htmx runtime or legacy htmx extension, stock full-page
> GET remains available, and a real Chromium interaction can use `hx-get`
> against a component-owned route and swap returned static SSR HTML.

The external .NET 10 application restores the locally packed Htmxor package,
has no Htmxor project reference or internals access, and owns the exact
`htmx.org@4.0.0` asset with SHA-256
`E484D9171A9DB30A39C8F16E3D709D4137F3211C659F8E6125816635033D593F`.
Package inspection finds Htmxor's narrow `htmxor.js` adapter but no htmx
runtime, type declarations, or event-header extension. `HtmxHeadOutlet` emits
only that adapter and no runtime or Htmxor-owned configuration payload.

Real Chromium first navigates normally to the stock `@page` route and observes
the complete Blazor document. It then confirms `window.htmx.version` is exactly
`4.0.0`, all browser requests are loopback, the runtime came from the
application path, and no Htmxor runtime, legacy extension, or compatibility
extension was requested. Activating the accessible `hx-get` control sends
`HX-Request: true` to a second component-owned `@page` route, receives shell-free
static SSR, and visibly swaps that markup into the intended target. The retained
1.9.12 browser fixture now owns and explicitly labels its legacy asset and
configuration; it remains regression coverage, not htmx 4 evidence.

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
- Revised meaningful red for issue #100 is preserved at clean test-only commit `e21c195b82b1f754bfb66b55719347b245616d12`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Package_only_application_registers_two_generated_get_routes_and_one_stock_page_put_action" --blame-hang --blame-hang-timeout 10min` packed Htmxor, restored and Release-built the separate .NET 10 consumer, and started its TestServer. The outer test discovered, executed, and failed 1 of 1. Its inner TRX discovered and executed 8 hosted HTTP tests: 4 passed and 4 failed. Both explicit GET-only HTMX-route controls and the stock GET passed. The authorized stock PUT returned `405 MethodNotAllowed` with binding, initialization, and callback counts all zero; the missing-token, invalid-token, and unauthorized cases also stopped at `405` because PUT was not yet on the stock route.
- Complementary generator red at the same clean test-only commit used `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Simple_stock_page_method_group_emits_one_shared_action_and_compiles_with_route_manifest|FullyQualifiedName~Page_directive_like_text_inside_later_code_comment_does_not_suppress_supported_action|FullyQualifiedName~Stock_page_onput_emits_an_action_without_copying_route_text" --blame-hang --blame-hang-timeout 10min`; 3 tests were discovered and executed, and all 3 failed only because the generated PUT source was absent.
- Post-review route-identity red is preserved at clean test-only commit `58bd3050990f38554fca050cc4a91d473df393c3`. The focused generator and registration selection discovered and executed 26 tests; 25 passed and the new compiled-route cardinality test failed because registration accepted two direct stock routes. The dedicated package command `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Package_only_application_rejects_a_put_component_with_two_compiled_stock_routes" --blame-hang --blame-hang-timeout 10min` discovered, executed, and failed 1 of 1 outer tests after packing Htmxor and restoring, Release-building, and starting the separate .NET 10 consumer. Its parsed inner TRX discovered, executed, and passed 8 of 8 hosted tests after the fixture added `[Route("/alternate-reports/{ReportId:int}")]` in the `.razor.cs` partial and targeted that compiled route. The passing inner checks included the authorized antiforgery-valid alternate-route PUT, request binding, lifecycle, and callback, proving the unintended action widening. A prior sandboxed package selection failed with `NU1301` because network access was denied; it was setup evidence, not product evidence.
- Post-review focused proof at exact clean implementation commit `42082a1bacb71364f5ccf513c8b5e791528d83cf`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorPutActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 26 of 26 tests. The selection covers supported stock-page generation without route text, `hx-put` exclusion, explicit GET-only HTMX-route non-widening, lookalike and dynamic fail-closed cases, manifest validation, compiled endpoint binding, and deterministic rejection when an action owner has two compiled stock routes.
- Post-review package proof at the same exact clean commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 3 of 3 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 8 of 8 hosted HTTP tests. The second-route consumer restored and Release-built but proved registration fails before serving. The computed-callback consumer proved nonconfigurable `HTMXOR002` and no consumer assembly.
- Fast-profile proof at the same exact clean commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `42082a1bacb71364f5ccf513c8b5e791528d83cf`, passed 105 quality tests, 40 .NET 10 hosted tests, and 196 non-browser library, generator, analyzer, and runtime tests. Total: 341 discovered, 341 executed, 341 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Issue #100's revised exact-head proofs used .NET SDK 10.0.400 on Microsoft Windows 10.0.26200. The local full profile and mutation testing were not run: ordinary pull-request CI owns the configured full profile, and mutation is optional for this proof of concept.
- Meaningful red for issue #103 was executed at the original clean test-only commit `16e465ac9f8b89d7bcade0511026d6fdeb1b1e31`, based on exact then-current `origin/main` `bb37e6fe6c07e135b7c1815b62ca271636cd8728`; the same test change is preserved after rebase at `371c1125a4442b6df688a686abbe8b49269721a6`. The focused package-consumer command discovered and executed one outer test and 11 inner hosted tests. The inner run passed 9 and failed 2: an authorized stock DELETE and an omitted-`Methods` HTMX-only PATCH each expected `200` but received `405`, with binding, initialization, and callback counts all zero. The compiler matrix command discovered and executed 8 tests; the existing stock PUT case passed and the other 7 failed only because their expected generated action was absent. Restore, pack, build, generator loading, host startup, and test discovery succeeded.
- Focused compiler, analyzer, and runtime-catalog proof at exact clean implementation commit `fb7e31f3d8378d7b7ab8f521f862429da14dfb50`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 68 of 68 tests. The selection covers all four unsafe bindings under both route owners, HTML and Razor component tags, multiple distinct methods on one tag, omitted and explicit methods, `_Imports.razor` rejection, deterministic nonconfigurable conflicts, and the `hx-post`/`hx-put`/`hx-patch`/`hx-delete`, `hx-action` plus `hx-method`, and `hx-query` negative controls.
- Focused package proof at the same exact clean implementation commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 11 of 11 hosted HTTP tests. The other package builds retained the multiple-stock-route and computed-handler failures and proved the new explicit-method conflict produces nonconfigurable `HTMXOR002` without a consumer assembly.
- Fast-profile proof at the same exact clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `fb7e31f3d8378d7b7ab8f521f862429da14dfb50`, passed 106 quality tests, 40 .NET 10 hosted tests, and 219 non-browser library, generator, analyzer, and runtime tests. Total: 365 discovered, 365 executed, 365 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same exact clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `fb7e31f3d8378d7b7ab8f521f862429da14dfb50`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 221 library, generator, analyzer, runtime, and legacy-browser tests. Total: 367 discovered, 367 executed, 367 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/77d75cf8-6d20-498c-80c3-2a4027532b45/coverage.cobertura.xml`.
- Independent review at documentation head `a7bc1e09c305b1964cadb9a807e4d442f863f93f` found that explicit `HtmxRoute.Methods` was treated as a blanket GET-only restriction rather than an authoritative membership allow-list, and that analyzer and generator duplicated the `HTMXOR002` descriptor. Those reviews and the preceding evidence were invalidated by the fixes below.
- Post-review meaningful red is preserved at clean test-only commit `b78ff30f3c43acbef1ab99b69e51fad7b539879d`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Binding_inside_explicit_htmx_route_methods_is_supported|FullyQualifiedName~Bridge_binds_an_action_allowed_by_explicit_htmx_route_methods" --blame-hang --blame-hang-timeout 5min` discovered and executed 2 tests; both failed. The compiler test received `HTMXOR001` because explicit GET plus PATCH was rejected, and the runtime catalog test threw before binding the explicitly allowed PATCH. Setup, build, and discovery succeeded.
- Post-review focused compiler, analyzer, and runtime-catalog proof at exact clean fix commit `732a957c36d080ddef39ca24db744b7d0c803fa4`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 70 of 70 tests. Matching explicit GET plus PATCH declarations now compile and bind while inferred methods outside the explicit set still fail closed with nonconfigurable `HTMXOR002`; unsupported `QUERY` remains rejected, omitted-`Methods` inference is unchanged, and the client-only negative controls remain covered.
- Post-review package proof at the same exact clean fix commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 11 of 11 hosted HTTP tests. The rejected consumers retained their multiple-stock-route, computed-handler, and explicit-method-conflict failures.
- Post-review fast-profile proof at the same exact clean fix commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `732a957c36d080ddef39ca24db744b7d0c803fa4`, passed 106 quality tests, 40 .NET 10 hosted tests, and 221 non-browser library, generator, analyzer, and runtime tests. Total: 367 discovered, 367 executed, 367 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Post-review full-profile proof at the same exact clean fix commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `732a957c36d080ddef39ca24db744b7d0c803fa4`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 223 library, generator, analyzer, runtime, and legacy-browser tests. Total: 369 discovered, 369 executed, 369 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/1052b5d6-42d0-4e23-bf98-5966e6a5441a/coverage.cobertura.xml`.
- Audit antiforgery-ordering red is preserved at clean test-only commit `834e1058991c191c846ad6252180d7194397308d`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Unsafe_generated_route_requires_effective_antiforgery_after_prior_disabling_metadata" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 0 passed and 1 failed because ordered required-then-disabled metadata left effective validation false. The same command passed 1 of 1 after `6b2fc684afb780a71032b8ec800526a9c830dc0a` appended Htmxor's required metadata when the effective last entry was not true.
- Audit actionless-route red is preserved at clean test-only commit `9ed1938ca40e13cb2ac0d0066ba7b64c5220eab2`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Package_only_application_infers_stock_and_htmx_only_unsafe_actions" --blame-hang --blame-hang-timeout 10min` discovered and executed 1 outer test, which failed because its parsed inner run passed 12 and failed 2 of 14 hosted tests. Missing and invalid antiforgery tokens on an explicit GET plus DELETE route with no generated action both returned `200`, bound parameters once, and initialized the component once. The authorized valid-token DELETE already rendered successfully. After `47302b39e2ad02c8bdd2c10eb15bb5da38ebde40`, the same outer selection passed 1 of 1 and its inner run passed 14 of 14; the rejected requests return `400` before binding or initialization while the authorized request still renders without a callback.
- Audit static-handler red is preserved at clean test-only commit `adfb00626985a8c5d960af37b92e7dfbd412f342`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Static_handler_is_rejected_as_a_nonconfigurable_action_declaration" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 0 passed and 1 failed because the analyzer returned no diagnostic for a static `@ondelete` method group. After `1e641355974493aab0655a9e763fe17e367d3303`, the same command passed 1 of 1 with deterministic nonconfigurable `HTMXOR002` resolved through public Roslyn symbols.
- Audit mutable-default red is preserved at clean test-only commit `c52ac7c9388d4b40c52205c556d7b03d9a1b4ba7`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Build_keeps_omitted_methods_get_only_when_public_defaults_are_mutated" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 0 passed and 1 failed because mutating the public shared default widened a subsequently constructed omitted route to `POST`. After `18345461d328c70256f8181308cc51b248d16370`, the same command passed 1 of 1 across sequential POST and TRACE mutation controls; new attribute instances and catalog descriptors remained GET-only.
- Audit later-markup red is preserved at clean test-only commit `bc2715c0d650bc434d6656414503764c274bf0ec`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~binding_after_prior_markup_emits_a_compiling_action" --blame-hang --blame-hang-timeout 5min` discovered and executed 2 tests; both failed because no action source was generated. After `217dd95642400509759d77c3bd8bc4ca53e178a6`, the same command passed 2 of 2 for a later HTML binding under `@page` and a later Razor component-tag binding under omitted-`Methods` `HtmxRoute`. The complete `HtmxorActionGeneratorTests` selection passed 36 of 36, retaining fail-closed comment, code, raw-string, interpolation, and nonbinding controls.
- Audit focused proof at exact clean implementation commit `217dd95642400509759d77c3bd8bc4ca53e178a6`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 75 of 75 tests.
- Audit packed-package proof at the same exact clean implementation commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests. The rejected consumers retained their multiple-stock-route, computed-handler, and explicit-method-conflict failures.
- Audit fast-profile proof at the same exact clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `217dd95642400509759d77c3bd8bc4ca53e178a6`, passed 106 quality tests, 40 .NET 10 hosted tests, and 226 non-browser library, generator, analyzer, and runtime tests. Total: 372 discovered, 372 executed, 372 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Audit full-profile proof at the same exact clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `217dd95642400509759d77c3bd8bc4ca53e178a6`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 228 library, generator, analyzer, runtime, and legacy-browser tests. Total: 374 discovered, 374 executed, 374 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/c95a2b41-c438-43ae-a4d5-e98b7b93288e/coverage.cobertura.xml`.
- Independent Standards and Spec reviews at exact documentation head `4c63e0ea1caf675cf5e651666c12977f23f86bc8` both found the same P1: a simple handler identifier could resolve to a static delegate-valued field or property because semantic validation inspected only method symbols. Those reviews and all preceding exact-head evidence were invalidated by the fix below.
- Audit-review meaningful red is preserved at clean test-only commit `cdc476767f947187864ae72d8dd45fc905a9999e`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Static_delegate_handler_member_is_rejected_as_a_nonconfigurable_action_declaration" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 0 passed and 1 failed because a static `Func<HtmxEventArgs, Task>` field bound through `@ondelete` received no diagnostic. After `6299b537ceb8954f191997310d9ddfc8c5dc0bee`, the static method, static delegate field, and supported instance method selection discovered, executed, and passed 3 of 3 with deterministic nonconfigurable `HTMXOR002` for both unsupported handler shapes.
- Audit-review focused proof at exact clean implementation fix `6299b537ceb8954f191997310d9ddfc8c5dc0bee`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 76 of 76 tests.
- Audit-review packed-package proof at the same exact clean implementation fix: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests; the three rejected consumers retained their expected compiler or startup failures.
- Audit-review fast-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `6299b537ceb8954f191997310d9ddfc8c5dc0bee`, passed 106 quality tests, 40 .NET 10 hosted tests, and 227 non-browser library, generator, analyzer, and runtime tests. Total: 373 discovered, 373 executed, 373 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Audit-review full-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `6299b537ceb8954f191997310d9ddfc8c5dc0bee`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 229 library, generator, analyzer, runtime, and legacy-browser tests. Total: 375 discovered, 375 executed, 375 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/22e4f97e-4196-40af-91ac-133b51de9727/coverage.cobertura.xml`.
- Fresh independent rereviews at exact documentation head `c5cd75ab3df9509c0455b28c75f516f37a1d7798` invalidated those reviews and exact-head checks with two separate P1 findings. Standards proved that an `@ondelete`-like token inside multiline `<script>` raw text emitted a DELETE action after the prior-markup change. Spec found that the successful package handler was staged as `Issue97ReportComponentCodeBehind.cs`, not the required matching `Issue97ReportComponent.razor.cs` partial.
- The multiline-script meaningful red is preserved at clean test-only commit `0250a15f740a9d4c79e3d730784b2c9848287df4`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Nonbinding_ondelete_inside_multiline_script_text_does_not_emit_an_action" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 0 passed and 1 failed because `HtmxorGeneratedActions.g.cs` was emitted. After `7c2d3365569751d3e63e7c5b19658452e2fced48`, the new raw-text negative plus both approved later-markup positives passed 3 of 3, and the complete generator selection passed 37 of 37. Prior markup must now be a complete matching single-line element or self-closing element.
- The matching-partial meaningful red is preserved at clean test-only commit `75668806318873f0415e58c010eaec6e435039b7`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Package_only_application_infers_stock_and_htmx_only_unsafe_actions" --blame-hang --blame-hang-timeout 10min` packed Htmxor, restored and Release-built the separate consumer, and passed all 14 inner hosted tests before the outer source-boundary assertion failed because `Issue97ReportComponent.razor.cs` did not exist. Commit `cfc995d9f14b89441224539d76c0279062ea52a4` renames only that template; the identical selection then passed 1 of 1 outer and 14 of 14 inner tests with `PatchReport` in the matching partial.
- Second audit-review focused proof at exact clean implementation fix `cfc995d9f14b89441224539d76c0279062ea52a4`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 77 of 77 tests.
- Second audit-review packed-package proof at the same exact clean implementation fix: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests; the three rejected consumers retained their expected compiler or startup failures.
- Second audit-review fast-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `cfc995d9f14b89441224539d76c0279062ea52a4`, passed 106 quality tests, 40 .NET 10 hosted tests, and 228 non-browser library, generator, analyzer, and runtime tests. Total: 374 discovered, 374 executed, 374 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Second audit-review full-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `cfc995d9f14b89441224539d76c0279062ea52a4`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 230 library, generator, analyzer, runtime, and legacy-browser tests. Total: 376 discovered, 376 executed, 376 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/e6fce091-e760-43b4-8081-56e8b0491bed/coverage.cobertura.xml`.
- A fresh Spec rereview passed exact documentation head `9ef8f8fc4bede1eef52f0e83a078e3b5d39a8848`, but Standards invalidated both rereviews and all preceding exact-head evidence with one residual P1. The self-closing check trusted the line's final slash rather than the opening tag's first closing delimiter, and a matching outer suffix could hide a nested raw-text opener. Both shapes still emitted DELETE actions.
- The residual raw-text meaningful red is preserved at clean test-only commit `2301078da8447b8a4c6e8d733962eeef7a18a80f`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Nonbinding_ondelete_after_misleading_script_slash_does_not_emit_an_action|FullyQualifiedName~Nonbinding_ondelete_after_nested_raw_text_suffix_does_not_emit_an_action" --blame-hang --blame-hang-timeout 5min` discovered and executed 2 tests; 0 passed and both failed because action source was emitted. Commit `494b232d02680045a52e6a1b037cc566de465e7e` adds a passing genuine `<hr />` control. After `ce388a1f10fadb121a48ab6f259f62536a5b693b`, the two residual negatives, original multiline-script negative, and both approved later-markup cases passed 5 of 5; the self-closing control passed 1 of 1; and the complete generator selection passed 40 of 40.
- Third audit-review focused proof at exact clean implementation fix `ce388a1f10fadb121a48ab6f259f62536a5b693b`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 80 of 80 tests.
- Third audit-review packed-package proof at the same exact clean implementation fix: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests; the three rejected consumers retained their expected compiler or startup failures.
- Third audit-review fast-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `ce388a1f10fadb121a48ab6f259f62536a5b693b`, passed 106 quality tests, 40 .NET 10 hosted tests, and 231 non-browser library, generator, analyzer, and runtime tests. Total: 377 discovered, 377 executed, 377 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Third audit-review full-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `ce388a1f10fadb121a48ab6f259f62536a5b693b`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 233 library, generator, analyzer, runtime, and legacy-browser tests. Total: 379 discovered, 379 executed, 379 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/b60caa65-c69b-46a3-ac72-8049f3b43c0f/coverage.cobertura.xml`.
- A fresh Standards review at exact documentation head `5e208d5ab65bbd32a15fbe1a55c76cc4cf13ad11` invalidated both publication reviews and all preceding exact-head evidence with one P1. The parser accepted `<script />` as complete prior markup even though HTML keeps the raw-text element open, so a later `@ondelete` token emitted a DELETE action. No additional Standards findings were identified; the concurrent Spec review found no defect before the head changed but correctly issued no final verdict.
- The self-closing raw-text meaningful red is preserved at clean test-only commit `67776f803b91a60582246d6c6d022ac8e79db872`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Nonbinding_ondelete_after_self_closing_script_syntax_does_not_emit_an_action" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 0 passed and 1 failed because `HtmxorGeneratedActions.g.cs` was emitted. After `f1a7884364ad241a32050e59115ea62cbbf1dae5`, that negative and the genuine `<hr />` positive passed 2 of 2. Commit `36d92a73f1151f14850632a1d45108e2a948bcca` adds a passing compiler boundary control proving that a prior self-closing Razor component line fails closed while bindings on Razor component tags after supported ordinary markup remain green.
- Fourth audit-review focused proof at exact clean evidence commit `36d92a73f1151f14850632a1d45108e2a948bcca`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 82 of 82 tests.
- Fourth audit-review packed-package proof at the same exact clean evidence commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests; the three rejected consumers retained their expected compiler or startup failures.
- Fourth audit-review fast-profile proof at the same exact clean evidence commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `36d92a73f1151f14850632a1d45108e2a948bcca`, passed 106 quality tests, 40 .NET 10 hosted tests, and 233 non-browser library, generator, analyzer, and runtime tests. Total: 379 discovered, 379 executed, 379 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Fourth audit-review full-profile proof at the same exact clean evidence commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `36d92a73f1151f14850632a1d45108e2a948bcca`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 235 library, generator, analyzer, runtime, and legacy-browser tests. Total: 381 discovered, 381 executed, 381 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/ccb7429c-e349-4b6f-a953-610bbb18155d/coverage.cobertura.xml`.
- The final root Spec challenge passed exact documentation head `fc2b12c0e94631db076db468a38ad39848fd103b` with zero findings, but Standards found one remaining P1. The parser accepted `<plaintext></plaintext>` as a complete paired prior element even though HTML's `plaintext` tokenizer state ignores the apparent closing tag through EOF, so a later `@ondelete` token emitted DELETE. The audit confirmed all earlier security, ownership, default, package, and raw-text findings fixed.
- The plaintext meaningful red is preserved at exact clean test-only commit `a129585dfed8bf01c23b54b4131acc3f95f88fba`: after a successful locked restore, `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~apparent_plaintext_pair" --blame-hang --blame-hang-timeout 5min` discovered and executed 2 lowercase and uppercase controls; 0 passed and both failed because `HtmxorGeneratedActions.g.cs` was emitted. The detached evidence worktree was removed afterward.
- After `561bcc2da118ee09e515c037663eebdaf4cb27f6`, the lowercase and uppercase negatives, ordinary paired-markup positive, and existing multiline-script negative passed 4 of 4. The fix excludes only `plaintext` case-insensitively from paired prior markup. A source review of the HTML tokenizer states found no second element context whose apparent end tag cannot exit before EOF; RAWTEXT, RCDATA, script data, and scripting-enabled `noscript` recognize their appropriate end tags. This was a scope check, not browser evidence.
- Final root-audit focused proof at exact clean implementation fix `561bcc2da118ee09e515c037663eebdaf4cb27f6`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 84 of 84 tests.
- Final root-audit packed-package proof at the same exact clean implementation fix: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests; the three rejected consumers retained their expected compiler or startup failures.
- Final root-audit fast-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `561bcc2da118ee09e515c037663eebdaf4cb27f6`, passed 106 quality tests, 40 .NET 10 hosted tests, and 235 non-browser library, generator, analyzer, and runtime tests. Total: 381 discovered, 381 executed, 381 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Final root-audit full-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `561bcc2da118ee09e515c037663eebdaf4cb27f6`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 237 library, generator, analyzer, runtime, and legacy-browser tests. Total: 383 discovered, 383 executed, 383 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/9fffe262-c7e4-42f9-b9ad-0e8dda67896c/coverage.cobertura.xml`.
- At exact documentation head `2f794aa008bb0e53dd3910a5325d6efde1f6a51e`, a fresh Spec review found no issue #103 defect, but the independent Standards review reproduced one request-ownership P1. With a global `using static`, an external static method could satisfy the generated bare handler identifier because the analyzer treated an absent component member as supported. The Standards review found no additional defects, and the head and all preceding checks and reviews were invalidated by the fix below.
- The imported-static meaningful red is preserved at exact clean test-only commit `7ef7af1ee1ca686c3417370282792041325d82c9`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Imported_static_handler_outside_component" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 compiler-backed analyzer test; 0 passed and 1 failed because the diagnostic collection was empty. Compilation, build, and test discovery succeeded. After `5177466f0afb09fd087b21d2bd44c04344c5b72b`, the complete analyzer selection passed 25 of 25; an unsafe action handler must now resolve to an instance method on the request-owned component hierarchy, so an absent component match fails closed with nonconfigurable `HTMXOR002`.
- Imported-static review-fix focused proof at exact clean implementation commit `5177466f0afb09fd087b21d2bd44c04344c5b72b`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 85 of 85 tests.
- Imported-static review-fix packed-package proof at the same exact clean implementation commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests; the three rejected consumers retained their expected compiler or startup failures.
- Imported-static review-fix fast-profile proof at the same exact clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `5177466f0afb09fd087b21d2bd44c04344c5b72b`, passed 106 quality tests, 40 .NET 10 hosted tests, and 236 non-browser library, generator, analyzer, and runtime tests. Total: 382 discovered, 382 executed, 382 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Imported-static review-fix full-profile proof at the same exact clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `5177466f0afb09fd087b21d2bd44c04344c5b72b`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 238 library, generator, analyzer, runtime, and legacy-browser tests. Total: 384 discovered, 384 executed, 384 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/103d5638-dcee-4117-bf67-17e57265d843/coverage.cobertura.xml`.
- A fresh Standards review at exact documentation head `f518426fed3d7247006112b996807ef4e0640cf7` invalidated that fix, its evidence, and the concurrent Spec review with one narrower P1. The hierarchy scan counted a private base instance method even though it was inaccessible from the generated component partial; C# could therefore ignore that member and resolve the bare handler identifier to a globally imported external static method. Standards reproduced zero analyzer diagnostics, one generated DELETE action, and zero driver or compilation errors, with no additional findings.
- The inaccessible-base meaningful red is preserved at exact clean test-only commit `fcb89cebded23186d8e0df0833c244a0b5b4d6fb`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Imported_static_handler_with_inaccessible_base_collision" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 compiler-backed analyzer test; 0 passed and 1 failed because the diagnostic collection was empty. Compilation, build, and test discovery succeeded.
- Commit `88631ad5cf3da3c7d44f111fefd4355c8bf3fc13` uses Roslyn's public symbol-accessibility contract so inaccessible hierarchy members cannot grant a server action or mask an imported static handler. Commit `696d4539f68ea33a56aa6210412bec87895a2efa` adds the complementary compiler-backed green control for a protected inherited instance handler; the inaccessible collision and accessible inheritance selection passed 2 of 2.
- Accessibility review-fix focused proof at exact clean evidence commit `696d4539f68ea33a56aa6210412bec87895a2efa`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 87 of 87 tests.
- Accessibility review-fix packed-package proof at the same exact clean evidence commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests; the three rejected consumers retained their expected compiler or startup failures.
- Accessibility review-fix fast-profile proof at the same exact clean evidence commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `696d4539f68ea33a56aa6210412bec87895a2efa`, passed 106 quality tests, 40 .NET 10 hosted tests, and 238 non-browser library, generator, analyzer, and runtime tests. Total: 384 discovered, 384 executed, 384 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Accessibility review-fix full-profile proof at the same exact clean evidence commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `696d4539f68ea33a56aa6210412bec87895a2efa`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 240 library, generator, analyzer, runtime, and legacy-browser tests. Total: 386 discovered, 386 executed, 386 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/c41cdb38-afe1-4364-8975-624711fb5c55/coverage.cobertura.xml`.
- The first audit packed-package rerun in the sandbox discovered and executed 4 outer tests, but all 4 failed during fresh temporary-consumer restore with `NU1301` because network access was denied. It was setup evidence, not product evidence; the identical command with network access produced the passing packed-package result above.
- The first sandboxed post-rebase locked restore failed with `NU1301` because NuGet network access was denied; it was setup evidence and its chained no-restore test had no valid restored input. The same `dotnet restore --locked-mode` outside that boundary succeeded before all reported post-rebase proofs.
- Issue #103's exact-head proofs used .NET SDK 10.0.400 on Microsoft Windows NT 10.0.26200.0. The full profile's existing Chromium fixture still used embedded htmx 1.9.12 and did not exercise issue #103's package routes or application-supplied htmx 4.0.0. Mutation testing was not run; it is optional for this proof of concept.
- Issue #106 started from freshly fetched exact `origin/main` `a489f30f7a20ec801fe52b5ab4f894382d1d9c90`. Live issue #106 and parent #77 were open when that work began, neither open pull request owned an overlapping file, and the isolated branch `egil/issue-106-explicit-csharp-routes` was clean before work began. The approved mergeable slice discovers project-root C# route declarations only when `HtmxRoute.Methods` is explicit. This includes a matching `.razor.cs` partial and a component authored entirely in C#. Any C#-origin declaration that omits `Methods` fails with deterministic nonconfigurable `HTMXOR001` and contributes no generated registration. Issue #106 is now closed; positive omitted-`Methods` `.razor.cs` inference remains deferred under the parent v1 work.
- Meaningful red is preserved at clean test-only commit `6285dae3646ff8357bdc413315dc1138c69b4de9`, whose production tree is exact base `a489f30f7a20ec801fe52b5ab4f894382d1d9c90`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~All_CSharp_component_with_explicit_methods_is_in_generated_registration" --blame-hang --blame-hang-timeout 5min` compiled the all-C# component and discovered and executed 1 test; 0 passed and 1 failed because `HtmxorGeneratedRouteRegistration.g.cs` omitted `Htmxor.Consumer.AllCSharpComponent`. The assertion failure, not an analyzer compilation error or setup failure, is the behavioral red.
- A post-publication review raised a future-compatibility risk if a peer Razor generator's output ever becomes visible to Htmxor's syntax provider. SDK 10.0.400 does not expose peer-generator output that way, so this is not the issue #106 product red above. The test-only synthetic control at `b56547a2fd5c8922200632048445826b6f1a70da` deliberately supplied a Razor-generated-path declaration in the input compilation. `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Razor_generated_omitted_methods_candidate_does_not_suppress_Razor_manifest" --blame-hang --blame-hang-timeout 5min` compiled successfully, discovered and executed 1 test, and failed because the generated registration was empty. The hardening at `8cc1badea33f950b43b51ed3d82f6d50e0373480` excludes compiler Razor declarations from C# omission suppression while retaining suppression for every authored C# omission.
- A fresh Spec review found that a Razor-backed type could place an explicit route on an arbitrary project-root C# partial. At test-only commit `21614bb1366482325296263dff7f2da3834f7951`, `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Explicit_route_in_nonmatching_CSharp_partial_reports_nonconfigurable_error" --blame-hang --blame-hang-timeout 5min` compiled both cases and discovered and executed 2 tests; 0 passed and 2 failed because neither `Other.cs` nor `Other.razor.cs` produced the required diagnostic. `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Package_only_application_rejects_an_explicit_route_in_a_nonmatching_partial" --blame-hang --blame-hang-timeout 10min` packed and restored the external consumer, discovered and executed 1 outer test, and failed because the invalid consumer incorrectly built with exit code 0. The fix at `9e4b3565e95177154b8fdf9e79f3a9ae1b92d30b` uses the final compiled Razor declaration to require a project-root matching `.razor.cs`; all-C# components remain valid in arbitrary project-root C# filenames, including when an unrelated same-basename Razor type compiles into another namespace.
- Focused compiler proof at exact clean post-review executable head `9e4b3565e95177154b8fdf9e79f3a9ae1b92d30b`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorRouteGeneratorTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 61 of 61 generator, analyzer, and runtime-catalog tests. The selection covers explicit `.cs` and matching `.razor.cs` discovery, nonmatching-partial rejection, arbitrary-filename all-C# ownership, omitted-Methods diagnostics and registration suppression, C# `#line` provenance, same-name Razor action ownership, compiler Razor-manifest preservation, explicit method membership, manual render-tree isolation, and incremental candidate reuse.
- Focused packed-package proof at the same exact clean executable head: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 7 of 7 outer tests. The local pack restored and Release-built an isolated .NET 10 consumer. Its default TestServer run discovered, executed, and passed 12 of 12 tests; the staged explicit actionless-unsafe regression discovered, executed, and passed 14 of 14. The package boundary proves an explicit `GET, PATCH` declaration in the matching `.razor.cs`, an explicit all-C# `GET`, direct rendering with authorization, route binding and lifecycle, normal and unauthorized unavailability, `405` method isolation despite manual `BuildRenderTree` HTMX attributes and callback construction, antiforgery on an explicit unsafe route without an action, failed compilation plus no generated registration when the all-C# declaration omits `Methods`, and failed compilation with no consumer assembly when a Razor-backed route moves to a nonmatching C# partial.
- Fast-profile proof at the same exact clean executable head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 109 quality tests, 40 .NET 10 hosted tests, and 255 non-browser library, generator, analyzer, and runtime tests. Total: 404 discovered, 404 executed, 404 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same exact clean executable head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` passed 109 quality tests, 40 .NET 10 hosted tests, and all 257 library, generator, analyzer, runtime, and legacy-browser tests. Total: 406 discovered, 406 executed, 406 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. It retained two byte-identical fresh Cobertura copies with SHA-256 `2B6E58D0342DD21DD374691799768B046143F8EE9B8028A503C312F4540939E2`; the canonical report was `artifacts/results/full/htmxor/ed335957-43f8-4558-b8ea-e0aa11b8f48f/coverage.cobertura.xml`.
- The first sandboxed locked restore and packed-consumer attempts failed only with `NU1301` because network access was denied. A sandboxed broad library run also failed when Windows Event Log access was denied while ASP.NET Data Protection reported an underlying exception. Those are setup-boundary observations, not meaningful red or product results; the same restore and test boundaries outside the sandbox passed before the evidence above.
- Issue #106's exact-head proofs used .NET SDK 10.0.400 on Microsoft Windows NT 10.0.26200.0. The full profile exercised the existing cached Chromium fixture, but did not prove fresh browser provisioning, Linux, Kestrel, TLS, a published or signed package, a release candidate, other SDK/compiler versions, or the package-only routes in a browser. It did not exercise htmx 4, QUERY, fragments, interactive render modes, performance, or external services. Full-scope mutation was not run; it is optional for this proof of concept and would include unrelated legacy production scope.
- Issue #108 started from freshly fetched exact `origin/main` `5bcd9b89b5a8b885467e3c9f13da629f9cc1d32d` on isolated branch `egil/issue-108-htmx4-browser-get`. Live issue #108 was open and unblocked, issue #106 was closed, superseded PR #74 was closed, and the only open PR, #41, owned renderer paths outside this slice. The starting worktree was clean, and its HEAD equaled current `origin/main` before the branch was created.
- Meaningful red is preserved at clean test-only commit `ad519a1cee4829f21f4a7678caf568fdac6fb755`, whose production tree is the exact starting base: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Head_outlet_emits_no_Htmxor_owned_htmx_runtime_or_configuration" --blame-hang --blame-hang-timeout 5min` compiled successfully, discovered and executed 1 test, and failed 1 of 1 because the rendered public `HtmxHeadOutlet` contained Htmxor's `htmx-config` payload. This was a public behavioral observation, not a setup, build, or discovery failure.
- Focused head-outlet and configuration proof at clean commit `ed1daabae9e1630d479ee257a7dabda7ba16c5a4`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxHeadOutletTest|FullyQualifiedName~HtmxConfigTest" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 8 of 8 tests. The public outlet emitted only `_content/Htmxor/htmxor.js`; `UseEmbeddedHtmx` was absent, while the remaining server configuration contract retained its independent serialization coverage.
- Focused real-package proof at exact clean executable head `e1b9106553d1838a08916e76edc1ce1181ebd61b`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-build --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests.Package_only_application_discovers_explicit_CSharp_routes_and_supported_actions" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 1 of 1 outer tests; its parsed package-consumer TRX passed 12 of 12 inner tests. The produced nupkg retained `staticwebassets/htmxor.js` and contained no htmx runtime, type declaration, event-header extension, or build-only runtime dependency. The external consumer used no Htmxor project reference or internals access.
- Focused htmx 4 browser proof at the same exact clean executable head: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-build --no-restore --filter "FullyQualifiedName~Htmx4PackageBrowserTests.Package_only_net10_application_uses_application_owned_htmx4_for_component_get" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 1 of 1 outer tests; its parsed external-consumer TRX passed 1 of 1 inner tests. The test packed Htmxor locally, restored and Release-built the isolated `net10.0` application, started real Kestrel, and drove real Chromium. It verified the exact application asset hash, stock full-page navigation, executed htmx 4.0.0, loopback-only script ownership, `HX-Request: true`, a shell-free component response, and the visible target swap with no page or console error.
- Fast-profile proof at the same exact clean executable head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 109 quality tests, 40 .NET 10 hosted tests, and 257 non-browser library, generator, analyzer, and runtime tests. Total: 406 discovered, 406 executed, 406 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. The explicit `Category!=Browser` quality filter excluded the new Chromium consumer, while the legacy project retained its existing fully qualified name filter.
- Full-profile proof at the same exact clean executable head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` passed 110 quality tests, 40 .NET 10 hosted tests, and all 259 library, generator, analyzer, runtime, and legacy-browser tests. Total: 409 discovered, 409 executed, 409 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its two fresh Cobertura copies had identical SHA-256 `A6E632461387BC0D1281F397541C860FB3FB22B10906F5DBC675A319496572CA`; the canonical report was `artifacts/results/full/htmxor/f14d646a-5860-414f-a93f-1c97513b81f0/coverage.cobertura.xml`.
- Final WSL package proof at exact clean pre-publication head `d2d3885c36a78572e93d72e0f9e038240bc9dc90`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests.Package_only_application_discovers_explicit_CSharp_routes_and_supported_actions" --logger "console;verbosity=detailed" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 1 of 1 outer tests; its parsed package-consumer TRX passed 12 of 12 inner tests.
- Final WSL htmx 4 browser proof at the same exact clean head: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Htmx4PackageBrowserTests.Package_only_net10_application_uses_application_owned_htmx4_for_component_get" --logger "console;verbosity=normal" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 1 of 1 outer tests; its parsed external-consumer TRX passed 1 of 1 inner tests through real Kestrel and Chromium.
- Final WSL fast-profile proof at the same exact clean head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 109 quality tests, 40 .NET 10 hosted tests, and 252 non-browser library, generator, analyzer, and runtime tests. Total: 401 discovered, 401 executed, 401 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Final WSL full-profile proof at the same exact clean head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` passed 110 quality tests, 40 .NET 10 hosted tests, and all 254 library, generator, analyzer, runtime, and legacy-browser tests. Total: 404 discovered, 404 executed, 404 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained a nonempty fresh Cobertura report.
- An independent Standards review of documentation head `7cb3b025e1069fda2c31b5c12e0dd1959c56de49` found that the fresh-application instructions named an htmx 4 asset without explaining how to acquire it. An independent Spec review found that changing the unsafe samples to htmx 4 claimed compatibility that this slice had not executed: the retained adapter still uses the legacy unsafe-request event seam, while the htmx 4 unsafe adapter migration is explicitly deferred.
- The sample-ownership test-only commit `bc60bff7829162c72c5bcf776d2895b5b5cf7298` preserved the resulting behavioral red. `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~SampleRuntimeOwnershipTests" --blame-hang --blame-hang-timeout 10min` compiled successfully, discovered and executed 3 tests, and failed 3 of 3 because each unsafe sample lacked the required app-owned legacy runtime/configuration declaration.
- At clean review-fix executable head `e2ac91524ec9ce911cb7f66a0fff7bbcee1ff4c2`, the three sample ownership controls passed. A combined focused run covering those controls, the package-only route consumer, and the real htmx 4 browser GET discovered, executed, and passed 5 of 5 outer tests. The package fixture's parsed inner run passed 12 of 12, and the browser fixture's parsed inner run passed 1 of 1.
- Fast-profile proof at the same clean review-fix executable head passed 112 quality tests, 40 .NET 10 hosted tests, and 252 non-browser library, generator, analyzer, and runtime tests. Total: 404 discovered, 404 executed, 404 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same clean review-fix executable head passed 113 quality tests, 40 .NET 10 hosted tests, and all 254 library, generator, analyzer, runtime, and legacy-browser tests. Total: 407 discovered, 407 executed, 407 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained a nonempty fresh Cobertura report.
- The first sandboxed focused-browser attempt failed only during isolated NuGet restore with `NU1301` because socket access was denied. Subsequent test-fixture source-mapping and analyzer failures occurred before browser execution and were corrected as test setup. None is counted as meaningful red. The identical final focused command with network and process access passed as recorded above.
- Issue #108's exact-head proofs used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, cached Chromium revision 1234 / 151.0.7922.34, and Microsoft Windows NT 10.0.26200.0. Kestrel used loopback HTTP. The evidence did not provision a fresh browser, use TLS, run on Linux, publish or sign a package, or call a CDN or other live service during browser execution. Full-scope mutation was not run; issue #108 makes it optional, and it would include unrelated legacy production scope.

- The final pre-publication rerun used the same SDK, runtime, Playwright, and Chromium versions on Ubuntu 26.04 under WSL. Playwright's exact Chromium revision and Linux dependencies were provisioned before the successful focused and full runs. Kestrel again used loopback HTTP. TLS, a published or signed package, a release candidate, other SDK/compiler versions, and external services during browser execution remain unproved. Full-scope mutation was not run; issue #108 makes it optional, and it would include unrelated legacy production scope.

## Remaining limits

- Issues #95, #97, #100, #103, #106, and #108 prove one locally packed package with the current SDK and dependency set. They do not prove publishing, package signing, a release candidate, package compatibility across SDK or compiler versions, a fresh Linux restore, or a broader target-framework matrix.
- The matrix uses one representative valid and rejected value per documented constraint. It does not exhaust textual representations, undocumented custom conversion constraints, catch-all routes, or unconstrained routes.
- The direct path is proved on ASP.NET Core 10 only. The supported framework matrix remains unproved.
- The authorization proof uses one deterministic scheme and one claim policy. It does not cover scheme selection, custom challenge or forbid handlers, identity-provider integration, or authorization on other HTTP methods.
- The issue #85 proof covers one stock named `EditForm`, one valid value, and one missing-token POST. It does not cover multiple forms, validation failures, invalid-token variants, file uploads, normal POST parity, or custom method discovery. Issue #87 proves unsafe route/query instance dispatch separately, without request-body or form binding.
- Issue #103 replaces the verb-specific package proof with generated stock DELETE and HTMX-only PATCH paths. The issue #87 and #89 fixed stand-ins remain as earlier hosted regression fixtures, not as the only unsafe-method evidence.
- Issue #89 covers an application-authored public `SetParametersAsync` override. An application that explicitly implements `IComponent.SetParametersAsync` would conflict with the generated explicit member and needs a future diagnostic or developer-model decision. Repeated parameter delivery, an override that intentionally omits its base call, async actions, request-body and form binding, multiple actions on one verb, multiple-route action mapping, multiple action-owning components, navigation, exception and cancellation behavior, `ShouldRender` overrides, and streaming SSR remain unexercised.
- The issue #85, #87, and #89 hosts run on Windows TestServer with the stock ephemeral Data Protection provider. They do not exercise Kestrel, TLS, persistent key storage, server-farm key sharing, Linux, a browser, or an application-selected HTMX runtime.
- Issues #91, #93, #95, #97, #100, and #103 ran their hosted contract only on Windows TestServer. They did not exercise Kestrel, TLS, Linux runtime, a browser, or an application-selected HTMX runtime. Issue #108 adds one package-only Kestrel and Chromium GET path on Windows and Linux, but it does not prove those earlier package-only routes and actions in a browser.
- Beyond issue #108's single GET, the tests do not exercise layouts, caching, concurrency, enhanced navigation, interactive render modes, fragments, browser behavior, or performance.
- Htmxor no longer packages or emits htmx 1.9.12, htmx type declarations, the
  event-header extension, or an Htmxor-owned `htmx-config` payload. The legacy
  test application and the unsafe samples own their retained 1.9.12 assets and
  configuration and remain legacy-only regression evidence. They are not htmx
  4 compatibility proof. Htmxor still owns the narrow `htmxor.js`
  adapter. Issue #108 does not prove that adapter's unsafe-request,
  antiforgery, or htmx 4 event-context behavior.
- Issue #108 covers only one application-owned htmx 4.0.0 GET using htmx 4
  defaults. Unsafe methods, the htmx 4 antiforgery adapter, `HX-Request-Type`,
  `HX-Source`, the changed `HX-Target` format, `hx-action`, `hx-method`,
  `hx-query`, response-header consolidation, explicit inheritance,
  error-response swapping, DELETE body behavior, standardized events and
  request context, fragments, out-of-band ordering, history, extensions, cache
  policy, repeatable CI browser provisioning, package publication, and the
  supported framework matrix remain separate evidence. Issue #103 proves only
  at the compiler boundary that client declarations do not grant server methods.
  For the agreed raw `<hx-partial>` composition, target and `id` behavior,
  partial-only responses, main-before-partial ordering, mixed main/OOB/partial
  content, swap selection, and browser execution remain unproved.
- The future QUERY server declaration is accepted as `@onquery`, but it has no
  implementation or executable evidence. Client declarations, including
  `hx-query`, `hx-action`, and `hx-method`, never grant QUERY reachability.
- The legacy test application still uses internal private-reflection discovery and global service replacements. Later slices must replace the behavior they cover instead of extending that prototype.
- Issue #91 proves one assumed-generated HTMX-only GET route with an `int`
  constraint, one authorization policy, and one application route-group metadata
  marker. It does not prove typed route-value conversion through this new seam,
  other constraints, multiple generated routes or components, collisions,
  normal-only or dual generated reachability, HEAD or OPTIONS behavior, or the
  full range of application group and security conventions. Issue #103 adds a
  package-generated stock DELETE and an HTMX-only component-tag PATCH while the
  earlier PUT, PATCH, and DELETE stand-ins remain regression fixtures.
- The issue #97 follow-up removes Razor-text interpretation. Its path-only
  generator does not claim the Razor grammar, while its diagnostic analyzer uses
  the final compilation and therefore sees component-generated members and
  compiler-bound attributes. Nested component directories or namespaces,
  future SDK or analyzer-pipeline changes, more than two routed components,
  multiple routes on one component, collision policy, normal-only or dual
  reachability, and a final public API remain unproved.
- The compiler-bound route-declaration model distinguishes Razor-backed types
  from all-C# types in the final compilation. Issue #106 proves explicit
  `HtmxRoute.Methods` on both a matching project-root `.razor.cs` partial and an
  all-C# project-root component through the real packed-consumer boundary. A
  Razor-backed route in a nonmatching C# partial fails with nonconfigurable
  `HTMXOR001` and produces no consumer assembly. The generator may still list
  that explicit type in the failed compilation because its pre-compilation seam
  cannot distinguish an unrelated same-basename Razor file in another namespace;
  generated-registration suppression is required only for omitted C# methods.
  The original compiled `HtmxRouteAttribute` remains authoritative. A C#
  declaration without `Methods`, including one in
  `.razor.cs`, now fails closed with `HTMXOR001` and no generated registration.
  Inferring omitted methods from companion Razor markup remains deferred until
  a supported pre-compilation ownership seam exists. V1 still does not treat
  `_Imports.razor` as an `HtmxRoute` declaration source. Effective non-route
  metadata from imports remains a separate unproved metadata-preservation case.
- Issues #95, #97, and #100 package those exact tracers and no broader behavior. Generated
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
  broader unsafe action discovery, IDE live-analysis parity, trimming, Native
  AOT, and startup cost remain unproved. This fail-fast behavior is accepted
  for the locally packed beta spike: unrelated unrouted manifest entries are
  ignored,
  while routed or security metadata is never silently omitted. It is not the
  final v1 compatibility contract and must be resolved before a stable release
  candidate.
- Issue #103 recognizes simple double-quoted method groups for `@onpost`,
  `@onput`, `@onpatch`, and `@ondelete` on supported HTML or Razor component
  start tags, including after a complete single-line ordinary markup line. It
  supports multiple different unsafe methods on one tag and one packaged handler
  implemented in the matching `.razor.cs` partial, but it does not claim the
  Razor or HTML grammar. Apparent paired `plaintext` markup is explicitly
  unsupported and fails closed. Arbitrary or multiline preceding markup,
  complex or dynamic expressions, markup after code or control transitions,
  conditional markup,
  local `@namespace`, nested components, all-C# action declarations, action
  declarations authored in `.razor.cs`, overloads, or multiple callbacks for
  one HTTP method remain unproved. Repeated parameter delivery, exceptions,
  cancellation, body and form binding expansion, and QUERY semantics also remain
  unproved. A prior self-closing Razor component line is also outside this POC
  parser: the generator receives raw `.razor` `AdditionalText` without Razor
  component-tag resolution, and capitalization cannot safely distinguish a
  component from case-insensitive HTML raw-text elements. Discovery therefore
  fails closed at that line. This does not limit supported bindings on Razor
  component tags after the proved plain-markup prefix. A generated stock action
  owner must still have exactly one direct compiled `RouteAttribute`; zero or
  multiple routes fail before endpoint conventions or HTMX-only mappings are
  added.

## Current implementation slice

Issue #108 establishes the first application-owned htmx 4 browser path:

> When a .NET 10 Blazor static SSR application supplies htmx 4.0.0, Htmxor
> emits and packages no htmx runtime or legacy htmx extension, stock full-page
> GET remains available, and a real Chromium interaction can use `hx-get`
> against a component-owned route and swap returned static SSR HTML.

This slice removes the obsolete embedded-runtime option and package assets,
narrows `HtmxHeadOutlet` to Htmxor's adapter, and proves only stock navigation
plus one real Chromium component GET and DOM swap. It does not migrate unsafe
requests or the antiforgery adapter, implement QUERY or `@onquery`, add
fragments, or claim the remaining htmx 4 matrix. Issue #106 is closed; positive
omitted-`Methods` inference from companion Razor markup remains deferred under
the parent v1 work. Publication still requires a current tracker,
competing-ownership, branch, and `origin/main` check. Stop for the user if the
result requires changing the v1 goal, supported framework, or security posture.
