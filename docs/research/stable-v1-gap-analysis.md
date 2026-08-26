# Htmxor stable v1 gap analysis

Research date: 2026-08-26
Repository baseline: `d8e09e4da17ab4c74fbea95d8e995137785c8395`

This report compares the prototype with the current stable .NET, Blazor, and htmx contracts. It is a source review, not a penetration test or a runtime benchmark. Claims about current versions and framework behavior use first-party release notes, documentation, or source. Recommendations are separated from confirmed facts.

The review covered the repository's source, tests, samples, documentation, build and release workflows, commit and release history, all 41 GitHub issues, all nine discussions, and the pull requests that established or still affect the architecture. The code audit followed the public API through endpoint discovery, route selection, rendering, form and event dispatch, antiforgery, browser assets, fragments, and response handling. The runtime checks were deliberately narrow: restore and existing tests, a compile probe against .NET 10, and a minimal .NET 10 host startup probe.

## Conclusion

The product idea remains sound: a Razor component can own both its normal full-page representation and an htmx fragment representation. The current implementation should not be promoted in place, however. Its central rendering path replaces Blazor services, copies framework rendering code, and reflects over private framework members. One of those reflection assumptions is already incompatible with .NET 10. The implementation also bundles htmx 1.9.12, while the current stable htmx release is 2.0.10.

The shortest credible route to v1 is to keep standard Blazor page endpoints entirely under the framework, then map explicit htmx fragment endpoints through public ASP.NET Core APIs. `RazorComponentResult` is the current public result type for rendering a Razor component from an endpoint and exposes parameters, status, content type, and a streaming control. It is a promising foundation, but its documented API does not promise Blazor named-form dispatch. That part needs a spike before the v1 endpoint API is fixed. [RazorComponentResult API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpresults.razorcomponentresult?view=aspnetcore-10.0)

The four release-blocking themes are:

1. Remove dependencies on private Blazor implementation details.
2. Remove the embedded htmx dependency, verify the htmx 2.0.10 reference profile, and publish extension/adaptation seams plus a caller-runnable conformance suite.
3. Close the confirmed antiforgery and event-dispatch gaps and define a fail-closed security contract.
4. Establish per-request performance and published-application evidence before setting release budgets.

## Current version baseline

| Area | Current upstream stable | Prototype | v1 position |
| --- | --- | --- | --- |
| .NET and ASP.NET Core | .NET 10 is the current LTS release. The latest servicing release on the research date is 10.0.11, released 2026-08-11. [.NET 10 lifecycle](https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md) and [August 2026 servicing announcement](https://github.com/dotnet/announcements/issues/436) | Targets `net8.0` and directly references `Microsoft.AspNetCore.Components.Web` 8.0.1. [Project file](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Htmxor.csproj) | Target .NET 10 for v1. Only multi-target .NET 8 if ForTheLeague has a concrete migration need and is willing to carry two framework-specific test matrices. |
| htmx | 2.0.10 is the stable reference on the research date; htmx 4 remains a prerelease. [htmx installation](https://htmx.org/docs/#installing), [v2.0.10 release](https://github.com/bigskysoftware/htmx/releases/tag/v2.0.10), and [v4.0.0-beta6 prerelease](https://github.com/bigskysoftware/htmx/releases/tag/v4.0.0-beta6) | Embeds 1.9.12. [Bundled script](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/wwwroot/htmx/htmx.min.js) | Bundle and require no htmx version. Verify a separately replaceable adapter against 2.0.10, allow application-owned newer/custom runtimes, and treat htmx 4 evidence as preview until upstream stabilizes. |

The repository has only alpha and beta version tags and its last commit is from September 2024. There is no stable 1.0 baseline to preserve.

## Repository and release baseline

- `main` is at `d8e09e4` and is 36 commits beyond the `v1.0.0-beta.1` tag. The latest published NuGet prerelease is `1.0.0-beta.1.24`; there is no stable package. [Release comparison](https://github.com/egil/Htmxor/compare/v1.0.0-beta.1...main) and [NuGet package](https://www.nuget.org/packages/Htmxor)
- GitHub contains 41 issues: 16 open and 25 closed. There are also two stale open pull requests, draft [#41](https://github.com/egil/Htmxor/pull/41) for a component result and [#74](https://github.com/egil/Htmxor/pull/74) for the ignored embedded-script switch. There are no milestones or assignees on the open work.
- The project has no benchmark project, retained per-request performance evidence, `SECURITY.md`, stable API compatibility baseline, or completed changelog. The CI and release workflows target .NET 8 and use several mutable action or tool versions.
- GitHub security features are enabled, but three high-severity CodeQL findings remain open in the BlazingPizza authentication sample. They may be sample-specific or duplicates, but they require triage before a high-security release. [CodeQL alerts](https://github.com/egil/Htmxor/security/code-scanning)
- A local run completed 150 non-browser tests. The browser suite could not run because Playwright Chromium is not installed in the environment, so this review does not claim that end-to-end tests are green.

The issue-by-issue disposition is recorded in the appendix. The open issue set is consistent with the code findings: htmx 2, static assets, Blazor coexistence, antiforgery, caching, redirects, duplicate lifecycle work, and streaming remain unresolved.

## What is worth keeping

The prototype already proves several useful parts of the model:

- A component's ordinary `@page` route is also made available to direct htmx requests, while `[HtmxRoute]` can add htmx-only routes. [Component discovery model](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Builder/ComponentInfo.cs)
- An endpoint selector distinguishes standard and direct responses, and route metadata can constrain method, current URL, target, or trigger. [Selector policy](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Builder/ComponentEndpointMatcherPolicy.cs) and [route contract](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/HtmxRouteAttribute.cs)
- The public request and response header constants cover the current official htmx header set. [htmx header reference](https://htmx.org/reference/#request_headers)
- Direct responses support route values, an optional fragment layout, `PageTitle`, response status, redirects, retargeting, reselection, and client events. [Request host](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Endpoints/HtmxorComponentRequestHost.cs), [fragment layout](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Components/HtmxLayoutComponentBase.cs), and [response API](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Http/HtmxResponse.cs)

These behaviors should become acceptance tests around a new implementation. The copied renderer should not become the compatibility contract.

## Gap matrix

| Priority | Confirmed gap | Consequence | v1 gate |
| --- | --- | --- | --- |
| P0 | Endpoint discovery reflects a private `ApplicationBuilder` property. The .NET 10.0.11 builder source has no such member. | The current null-forgiving reflection path fails before endpoints can be mapped on .NET 10. | Run standard pages without replacing Blazor's invoker or renderer; map fragments through public APIs. |
| P0 | Direct endpoints are built in a separate endpoint data source and do not inherit conventions applied to `MapRazorComponents`, including `RequireAuthorization`, rate limiting, CORS, host restrictions, output caching, or arbitrary metadata. | Full and fragment forms of the same page can have different security and operational policies. Component attributes and fallback policies cover only part of the contract. | Generate or map fragment endpoints through an API that preserves the same endpoint conventions, then prove full/direct authorization parity at the HTTP boundary. |
| P0 | Direct `DELETE` endpoints advertise antiforgery metadata and the client sends a token, but neither ASP.NET Core 10 antiforgery middleware nor Htmxor's fallback treats `DELETE` as a form-validation method. | A `DELETE` can reach the endpoint without antiforgery validation. Cross-site exploitability then depends on the browser, CORS, and client surface, but the server contract is not fail-closed. | Explicitly validate every unsafe htmx verb, including `DELETE`, and add negative integration tests for missing, invalid, and cross-user tokens. |
| P0 | Component event dispatch accepts a client-supplied 32-bit FNV hash and does not verify that its encoded htmx method and URL match the incoming request. An existing test sends PATCH with the DELETE handler hash and invokes `OnDelete`. | A client can invoke a different rendered handler than the request method indicates. The short deterministic hash is an identifier, not an authorization mechanism. | Remove arbitrary event replay from v1 or bind an opaque, protected action capability to method, normalized route, component/action identity, user context, and expiry. |
| P0 | htmx history restore is parsed but does not affect routing. htmx defaults `historyRestoreAsHxRequest` to `true`. | A history cache miss can receive a fragment when htmx requires a full document, and caches can mix full and partial representations. | Emit `historyRestoreAsHxRequest=false`, route `HX-History-Restore-Request` to a full page, and apply `Vary: HX-Request` where one URL has two representations. |
| P1 | The embedded htmx and configuration DTO describe 1.9 behavior. | DELETE parameter placement, same-origin behavior, scroll behavior, validation, extensions, and new configuration cannot be represented reliably. Mirroring future htmx configuration in C# would repeat this coupling. | Remove the embedded artifact and frozen configuration DTO; make configuration application-owned and verify Htmxor through replaceable adapters and conformance tests. |
| P1 | `HtmxHeadOutlet` implements `IComponent.SetParametersAsync` without applying the `ParameterView`. | `UseEmbeddedHtmx=false` is ignored, so the package still emits bundled htmx 1.9.12 even when the consumer asks to supply a current script. | Narrow the outlet to Htmxor-owned metadata, emit no htmx asset or htmx configuration, and test the contract in a published Production application. |
| P1 | Routing and rendering depend on copied and reflected internals. | Servicing changes can silently change authentication state, navigation, form mapping, streaming, or resource handling. Trimming is also fragile. | No private reflection or copied internal renderer in the v1 request path. |
| P1 | Conditional fragments suppress markup while walking an already-rendered component tree. | Excluded components have already run lifecycle and data-loading work, so markup reduction does not imply proportional server-work reduction. | Compose and render the selected fragment directly; prove excluded branches do not resolve or call their data dependencies. |
| P1 | `HX-Target` matching assumes a target id. htmx also supports relative and extended CSS targets, but only sends the target's id when one exists. | A valid deep interaction can miss a target-constrained endpoint. | Make URL and method the stable identity. Treat htmx headers as optional representation hints, not endpoint authority. |
| P1 | Boosted requests with a target are classified as direct, but all Htmxor endpoints reject boosted requests. Boolean htmx headers are interpreted by presence, including a literal `false`. | Valid navigation can end with no route candidate, and malformed or false-valued headers can change routing. | Define one truth table for normal, boosted, targeted, and history-restore requests and cover it with endpoint-level tests. |
| P1 | Route equality is asymmetric, its hash is case-sensitive while equality is not, and every ordinary `@page` is expanded to one shared mutable five-verb default. | Discovery can retain duplicates or drop routes, and pages receive broader unsafe-method exposure than they requested. | Use immutable explicit method metadata with a correct equality contract, deterministic precedence, and GET-only defaults. |
| P1 | `HtmxAsyncLoad` renders every loader during any direct request and drops `PathBase` and query strings from generated URLs. Optional and custom-constrained route parameters are only partially copied from framework behavior. | A targeted request can execute unrelated loaders or lose route state, while deep routes diverge from ordinary Blazor binding. | Address loaders explicitly and delegate route/query/form binding to public framework contracts rather than maintaining a partial parser. |
| P1 | There is no per-request benchmark suite or published-app matrix. | Performance claims cannot be evaluated and regressions cannot be caught. | Record baseline latency, throughput, allocations, and response size against stock static SSR and a public-result implementation. |
| P2 | Core htmx updates `<title>`, not arbitrary head metadata. | Pages that expect canonical links, styles, or other head elements to merge will behave differently during htmx navigation. | Promise title updates only, or version and test the official head-support extension as an explicit opt-in. |

## Blazor compatibility and endpoint architecture

Blazor static server-side rendering produces HTML in the response and does not retain an interactive component instance after the request. Routing, authorization, and authentication for static SSR run through normal ASP.NET Core request processing. [Blazor render modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0)

Htmxor currently alters that request processing globally. `AddHtmx` removes or replaces `IRoutingStateProvider`, `IRazorComponentEndpointInvoker`, the internal `EndpointHtmlRenderer`, the cascading `HttpContext` supplier, and `NavigationManager`. [Service replacement code](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/DependencyInjection/HtmxorApplicationBuilderExtensions.cs) The replacement renderer then discovers the internal `HttpContextFormDataProvider` by name and invokes non-public methods. [Renderer reflection](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Rendering/HtmxorRenderer.cs)

Direct endpoints are also built in a new endpoint data source from discovered component types rather than from the conventions accumulated on `RazorComponentsEndpointConventionBuilder`. [Endpoint registration](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Builder/HtmxorComponentEndpointRouteBuilderExtensions.cs) and [direct endpoint construction](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Builder/ComponentEndpointDataSource.cs) Component attributes are copied, but builder-level calls such as `RequireAuthorization`, rate limiting, CORS, host restrictions, output caching, and arbitrary metadata do not automatically reach the direct endpoint. A v1 implementation must make policy parity structural rather than relying on application authors to duplicate it.

This is no longer only a maintenance risk. Endpoint discovery calls `GetProperty("ApplicationBuilder", NonPublic)!` on `RazorComponentsEndpointConventionBuilder`. [Current reflection path](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Builder/HtmxorComponentEndpointRouteBuilderExtensions.cs) The tagged .NET 10.0.11 implementation has internal endpoint-builder state but no `ApplicationBuilder` member, so the lookup returns null and the next call dereferences it. [ASP.NET Core 10.0.11 builder source](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Builder/RazorComponentsEndpointConventionBuilder.cs) A clean local .NET 10 host referencing the built package compiled successfully and then reproduced this exact `NullReferenceException` at startup in `GetDiscoveredComponents`. This confirms that the first blocker is runtime compatibility, not a theoretical source risk.

The copied renderer also freezes old framework behavior. The .NET 10 endpoint renderer now owns resource collection, authentication-state listeners, state restoration, navigation and not-found handling, and streaming bookkeeping. [ASP.NET Core 10.0.11 endpoint renderer](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs) Its form-data provider remains internal. [ASP.NET Core 10.0.11 form-data provider](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/FormMapping/HttpContextFormDataProvider.cs) Copying those behaviors into Htmxor creates an undocumented framework fork.

The recommended v1 architecture is:

1. Leave `MapRazorComponents<App>()` and its services unchanged for every standard page request.
2. Map htmx fragment endpoints explicitly. A source generator can turn `[HtmxRoute]` declarations into public endpoint registrations without runtime discovery.
3. Render fragment components with `RazorComponentResult` or another public result contract. Keep authorization and other endpoint metadata on the generated endpoint.
4. Select full page versus fragment using a stable URL/method contract. If the same URL serves both, use an explicit public endpoint-selector policy and cache variation. Never consider `HX-*` headers proof of identity or permission.
5. Prefer explicit HTTP actions and form handlers over replaying arbitrary component event-handler ids from `HXOR-Event-Handler-Id`. The current event replay is tightly coupled to renderer internals and has a confirmed method-confusion defect. [Client event header](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/wwwroot/htmxor.js)

There is a compatibility-first fallback: return the normal full document and use htmx `hx-select` to choose the fragment. htmx explicitly supports full-document responses with `hx-select`. [Requests and responses](https://htmx.org/docs/#requests) This avoids a private renderer but still pays for full application and layout rendering on each request, so it should be a baseline rather than the intended fast path.

### Uncertain architecture points

- `RazorComponentResult` clearly supports direct rendering. It is not documented as an entry point for Blazor's named static-SSR form dispatch. A spike must prove form binding, validation, antiforgery, authorization, status codes, navigation, and streaming before adopting it as the only renderer.
- A public `IEndpointSelectorPolicy` can distinguish duplicate route candidates, but the long-term stability of using duplicate page and fragment endpoints on the same route should be validated against .NET 10 routing tests. Separate fragment URLs are simpler and remain the safe fallback.
- The best registration shape, explicit fluent mapping versus generated `[HtmxRoute]` mappings, is an API design decision. Either can avoid private discovery.

## Client-runtime decoupling and the htmx 2 reference profile

htmx 2 removes extensions from the core distribution and changes several defaults. The official migration guide calls out separately distributed extensions, a required SSE migration, URL parameters for `DELETE`, same-origin-only requests, instant scroll behavior, and the `hx-on:*` event syntax. [htmx 1 to 2 migration guide](https://htmx.org/migration-guide-htmx-1/)

The prototype's `HtmxConfig` mirrors the 1.9 configuration. Its comments say `methodsThatUseUrlParams` defaults to GET, `scrollBehavior` defaults to smooth, and `selfRequestsOnly` defaults to false, while its own C# default forces same-origin requests. [Prototype configuration](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/HtmxConfig.cs) In htmx 2.0.10 the corresponding defaults are `['get', 'delete']`, `instant`, and `true`. The current configuration also lacks `inlineStyleNonce`, `disableInheritance`, `responseHandling`, `allowNestedOobSwaps`, and `historyRestoreAsHxRequest`. htmx's validation guidance additionally recommends enabling `reportValidityOfForms`. [Current htmx configuration](https://htmx.org/docs/#config)

Required migration work:

- Remove the embedded artifact from the server package. The application owns the htmx script, version, extensions, configuration, CSP, and upgrade schedule.
- Repair and narrow `HtmxHeadOutlet`. Its manual `SetParametersAsync` currently ignores the supplied `ParameterView`, so `UseEmbeddedHtmx=false` never changes the property and the old embedded script is still emitted. The v1 outlet should emit only Htmxor-owned metadata such as the antiforgery token and handled-fragment marker. [Head outlet implementation](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Components/HtmxHeadOutlet.cs)
- Retire the serialized catch-all C# configuration DTO. Publish a documented htmx 2 high-security profile as application-owned `<meta name="htmx-config">` markup so a new upstream option does not require a Htmxor release.
- Split the browser bridge into version-neutral Htmxor behavior and a small replaceable htmx lifecycle adapter. Verify the maintained adapter against 2.0.10, but do not include or version-gate the runtime.
- Add a declarative typed server protocol registry for future request and response headers. It must own parsing, size limits, protected-header conflicts, URL validation, automatic `Vary`, and cache safety without exposing route or security mutation hooks.
- Preserve unknown `hx-*` attributes and native `hx-ext` usage. An analyzer may diagnose known attributes but must not reject future ones.
- Cover DELETE query-parameter behavior in the verified htmx 2 adapter suite.
- Set `historyRestoreAsHxRequest=false` whenever `HX-Request` selects fragments. htmx says this should always be disabled for partial/full response selection and expects a complete page after a history cache miss. [htmx history contract](https://htmx.org/docs/#history)
- Add `Vary: HX-Request` to cacheable dual-representation responses. htmx documents this requirement for servers that return a full page without the header and a fragment with it. [htmx caching guidance](https://htmx.org/docs/#caching)
- Stop loading `event-header.js` unconditionally. Native htmx extensions remain application-owned. The current extension adds a `Triggering-Event` request header that is not consumed by this repository, so removal is preferable unless a supported use case is found. [Current head outlet](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Components/HtmxHeadOutlet.cs)
- Test every official request and response header. The names are currently complete, but the `HX-Request` comment saying it is always true is no longer a safe server assumption during correctly configured history restoration. [htmx headers](https://htmx.org/reference/#request_headers)

Compatibility claims should say “verified with htmx 2.0.10 and this adapter,” not “requires htmx 2.0.10.” Unknown and newer versions remain allowed. A public conformance runner should execute the same unsafe-request, history, redirect, handled-error, OOB, extension, and Blazor DOM-ownership tests against an application-supplied script and adapter.

## Deep routes and full-page composition

The v1 contract should define three cases independently:

| Request | Required result |
| --- | --- |
| Browser GET to a public route | A complete, linkable, refreshable, authorized HTML document. |
| htmx GET to the same public route | The intended component fragment, or a full document from which `hx-select` can reliably select it. |
| htmx request to a fragment-only route | A component fragment with the same authorization, antiforgery, validation, and error semantics as the owning page. A normal browser request should either return a deliberate full-page host or a deliberate 404, not an accidental renderer failure. |

Current route selection can constrain a route by `HX-Target`, but htmx only sends the target element's id if one exists. At the same time, `hx-target` supports `this`, `closest`, `next`, `previous`, and `find`, which are useful precisely because they do not require ids. [htmx targeting](https://htmx.org/docs/#targets) A route keyed primarily by target headers is therefore incomplete. URLs and HTTP methods should identify the server capability; target and trigger headers can refine representation after authorization.

For components nested below a page, v1 should require an explicit fragment route or generated mapping. Reusing every `@page` route as a direct component route is convenient and should remain available, but arbitrary child-component discovery should not be inferred from renderer internals.

Three current correctness defects reinforce the need for one explicit routing contract:

- A boosted request with an explicit target is classified as direct, after which the standard candidate and every Htmxor candidate reject it. Boolean htmx headers are interpreted by presence rather than the value `true`. [Request classification](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Http/HtmxRequest.cs), [endpoint metadata](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Builder/EndpointMetadata.cs), and [selector](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Builder/ComponentEndpointMatcherPolicy.cs)
- `HtmxRouteAttribute.Equals` treats nulls as one-sided wildcards while its hash is case-sensitive, despite case-insensitive equality. Its public mutable default also grants GET, POST, PUT, PATCH, and DELETE to every converted `@page`. [Route attribute](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/HtmxRouteAttribute.cs) A stable v1 should default ordinary pages to GET and require explicit unsafe methods.
- During any direct request, every `HtmxAsyncLoad` renders its child, and its generated URL drops `PathBase` and the query string. The request host also maintains a partial copy of route-constraint conversion. [Async loader](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Components/HtmxAsyncLoad.cs) and [route parameter host](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Endpoints/HtmxorComponentRequestHost.cs) The public framework should own binding, while each loader gets an explicit address.

## Security assessment

### Confirmed antiforgery defect

`HtmxRouteAttribute` permits GET, POST, PUT, PATCH, and DELETE by default. Every generated endpoint receives `RequireAntiforgeryTokenAttribute`, and the browser helper attaches a request token to all methods except GET, HEAD, OPTIONS, and TRACE. [Route methods](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/HtmxRouteAttribute.cs), [endpoint metadata](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Builder/ComponentEndpointDataSource.cs), and [browser token helper](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/wwwroot/htmxor.js)

The server-side checks do not match that contract. ASP.NET Core 10's antiforgery middleware only treats POST, PUT, and PATCH as valid form methods. [ASP.NET Core form-method helper](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Shared/HttpExtensions.cs) Htmxor's fallback uses the same three methods. [Htmxor request validation](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Endpoints/HtmxorComponentEndpointInvoker.cs) As a result, DELETE requests are not validated even though the client sends a token and the endpoint advertises antiforgery metadata. This is a release blocker.

The fallback expression also applies its exception-handler exclusion only to PATCH because `&&` binds more tightly than `||`. POST and PUT can still enter form dispatch during exception handling. This is a correctness gap that should receive a regression test.

The v1 security contract should state that all state-changing verbs require antiforgery validation, authentication and authorization are re-evaluated server-side on every request, and GET is side-effect free. Microsoft warns that GET requests must not change state because antiforgery protection does not cover them. [ASP.NET Core antiforgery guidance](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)

### Confirmed event-dispatch defect

For `@onget`, `@onpost`, and related handlers, the renderer computes a 32-bit FNV-1a hash of the rendered `hx-{method}={url}` attribute and emits that value as `hxor-eventid`. [Hash and emitted attribute](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Rendering/HtmxorRenderer.HtmlWriting.cs) The browser copies it into `HXOR-Event-Handler-Id`. On the next request, the server looks up the supplied hash and dispatches the associated Blazor handler, but it does not compare the hash's source method or URL with the actual request method or normalized path. [Dispatch implementation](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Rendering/HtmxorRenderer.HtmxorEventDispatch.cs)

The existing `Delete_method_htmxor_event_handler` test sends a PATCH request with the DELETE button's hash and asserts that `OnDelete` runs. [Method-confusion test](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/test/Htmxor.Tests/EventHandlerTest.cs) Its passing expectation proves the server does not bind handler identity to the incoming method. A deterministic 32-bit hash also cannot serve as authenticity or authorization evidence.

The safest v1 decision is to remove this callback-replay feature and use explicit HTTP action endpoints. If it remains, the action token must be opaque and tamper-protected, short-lived, and bound at least to HTTP method, normalized endpoint or route, component/action identity, and the relevant user or authorization context. The server must still perform antiforgery and authorization checks independently.

### Token transport and fail-closed behavior

Blazor static SSR forms use named forms, `[SupplyParameterFromForm]`, and antiforgery integration. Form names must be unique, mapping limits are configurable, and separate input models reduce overposting. [Blazor forms](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/?view=aspnetcore-10.0) and [form binding](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/binding?view=aspnetcore-10.0)

Htmxor currently emits a JavaScript-readable antiforgery request-token cookie on every response. The cookie uses `SameSite=Strict` but does not explicitly set `Secure` or `Path`, and the JavaScript dereferences the cookie lookup without handling a missing token. [Token-cookie middleware](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Antiforgery/HtmxorAntiforgeryMiddleware.cs) htmx recommends ordinary hidden framework form tokens when available, with `hx-headers` as another supported transport. [htmx CSRF guidance](https://htmx.org/docs/#csrf-prevention)

Prefer Blazor's hidden form token for forms. If a header token remains necessary for non-form verbs, issue it deliberately, set cookie scope and transport properties explicitly, avoid rotating it on every unrelated response, and fail closed when it is missing. Test authenticated and anonymous requests, token rotation, multiple tabs, reverse-proxy HTTPS, and every unsafe method.

### Browser-side hardening

htmx supplies layered controls for raw or sensitive HTML: `hx-disable`, `hx-history=false`, `historyCacheSize=0`, `selfRequestsOnly`, `allowScriptTags=false`, `allowEval=false`, URL validation, and CSP. Disabling eval also disables trigger filters, `hx-on:`, and JavaScript-valued `hx-vals` and `hx-headers`, so this must be an explicit compatibility choice. [htmx security tools](https://htmx.org/docs/#security)

Provide a documented high-security profile rather than silently changing every htmx default. A sensible profile starts with same-origin requests, no evaluated expressions, no scripts in swapped content, no sensitive local-history cache, CSP-compatible indicator styles or a style nonce, and nested out-of-band swaps disabled unless required. These values are a proposed policy, not an upstream mandate.

Other v1 security gates:

- Treat every `HX-*` and `HXOR-*` header as attacker-controlled. Header-based routing may select a representation only after the endpoint's authorization policy is established.
- Preserve all component authorization metadata on generated fragment endpoints and test equivalent outcomes between the full page and fragment.
- Bound form collection size, recursion depth, collection size, and error count. Use input DTOs rather than binding domain entities. [Blazor static SSR security guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/static-server-side-rendering?view=aspnetcore-10.0)
- Encode untrusted output and isolate any raw HTML with a sanitizer and `hx-disable`. htmx's security guidance warns that untrusted raw attributes can inject htmx behavior. [htmx web security basics](https://htmx.org/essays/web-security-basics-with-htmx/)
- Add local-only redirect helpers for `HX-Location`, `HX-Redirect`, `HX-Push-Url`, and `HX-Replace-Url`, or name unrestricted variants explicitly. The current API accepts arbitrary strings.
- Test cache-control and `Vary` behavior so authenticated fragments cannot be served across users or confused with full pages.

## Head, history, and streaming

Core htmx extracts a response `<title>` and updates `document.title`. It does not merge the rest of `<head>`. The official `head-support` extension exists for merging head elements and is currently distributed separately. [Official head-support extension](https://htmx.org/extensions/head-support/)

The prototype's fragment layout includes Blazor `HeadOutlet`, which is enough to put `PageTitle` output in the response. This supports a narrow v1 promise: title changes work during fragment requests. Full metadata merging should either be a non-goal or an application-owned extension exercised at an exact version in tests for stylesheet deduplication, canonical links, scripts, and boosted navigation.

The current renderer writes an initial buffered response and contains comments about streaming updates, but it does not call the framework's streaming update loop. Its branch also appears to await request-event quiescence before writing. This suggests that direct rendering is effectively buffered, but that conclusion is uncertain until a component with `[StreamRendering]` is exercised over a real response. Blazor's current streaming contract sends placeholder content and later updates in the same response. [Blazor streaming rendering](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/rendering?view=aspnetcore-10.0#streaming-rendering)

For v1, choose one explicit contract:

- Buffer fragments and set `PreventStreamingRendering=true`. This is simpler and makes response timing predictable.
- Support framework streaming end to end through a public result API and test proxy buffering, status codes, cancellation, and htmx swap behavior.

Do not advertise streaming until the second path is proven.

## Trimming and publishing

The package does not declare itself trimmable, while endpoint discovery and rendering use runtime reflection over private types and members. Blazor trimming guidance warns that reflection can produce publish-time warnings and runtime failures and recommends testing published output regularly. [Blazor trimming guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/configure-trimmer?view=aspnetcore-10.0)

Stable v1 must make one honest choice:

- Support trimming by replacing private and unbounded reflection with explicit or generated registrations, annotating the remaining public reflection, and running a `PublishTrimmed` smoke app in CI.
- Declare trimming unsupported and test a normal framework-dependent and self-contained publish.

The first choice aligns better with source-generated endpoint mapping. Native AOT should not be promised as part of this work without a separate supported-hosting investigation.

## Per-request performance plan

No benchmark project or load-test harness exists in the repository. The current direct path creates a scoped renderer, renders a fresh component tree, builds event-handler state, and writes through a 16 KiB pooled stream writer. Some reflection occurs at startup or type initialization, but copied rendering and event dispatch affect the request path. Source inspection cannot quantify the net cost.

The current conditional-fragment mechanism is an output filter, not an execution filter. `ShouldGenerateMarkup` is consulted while the renderer walks an existing render tree and toggles a conditional writer. [Conditional component state](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Rendering/HtmxorComponentState.cs) and [HTML writer](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Rendering/HtmxorRenderer.HtmlWriting.cs) Component construction, parameters, lifecycle methods, and data calls have already happened. Therefore a smaller response is not evidence of lower server work. The new architecture should render the selected fragment as the root or explicit child composition, and an integration test with counting data dependencies should prove that excluded page branches never execute.

Build a benchmark sample with production settings and compare:

1. Stock Blazor static SSR full-page GET.
2. Stock full-page GET plus client-side `hx-select` compatibility path.
3. Htmxor direct fragment GET through the proposed public endpoint.
4. Current prototype direct GET, while it still runs on its supported framework, as historical evidence.
5. Valid and invalid POST forms.
6. Valid and invalid DELETE requests after the antiforgery fix.
7. A full page with several data-loading branches versus one directly composed fragment, verifying both response cost and which dependencies executed.

Measure cold start separately from steady state. For each steady-state scenario capture throughput, mean, p50, p95 and p99 latency, allocations per operation, response bytes, and error rate at increasing concurrency. Include a small fragment, a realistic ForTheLeague match/card fragment, an authorized route, route and query parameters, and a slow streaming candidate. Run the packaged Release build behind the same server and logging settings.

Do not invent an absolute latency target before this baseline exists. The v1 gate should first be no material regression against the public `RazorComponentResult` fragment baseline, with a project-owned budget agreed from measured ForTheLeague traffic and payloads.

## Proposed v1 scope

### Supported

- .NET 10 LTS; no required htmx version. The maintained reference adapter is verified against htmx 2.0.10, and applications may supply newer or custom runtimes.
- Static SSR full pages owned by Blazor.
- Explicit or generated component fragment endpoints using public ASP.NET Core contracts.
- Direct fragment composition that does not execute unrelated page branches.
- GET, POST, PUT, PATCH, and DELETE with fail-closed validation for unsafe verbs.
- Route and query parameters, authorization metadata, antiforgery, validation errors, status codes, cancellation, htmx request context, and the current official request and response headers.
- Same-route full and fragment representations when cache and history behavior is correct.
- `PageTitle` updates.
- A documented high-security configuration profile.
- Published-app smoke tests and repeatable per-request benchmarks.

### Deferred unless a spike proves them cheaply

- Arbitrary Blazor event-callback replay from an htmx header.
- Private Blazor renderer replacement.
- Transparent discovery of arbitrary nested components without an explicit route declaration.
- Full `<head>` merging in core.
- Streaming fragment updates.
- Native AOT.
- WebSocket and SSE integration beyond documenting compatible official htmx extensions.

## ForTheLeague adoption recommendation

Do not add the current Htmxor package to ForTheLeague production code. The .NET 10 startup failure, policy-propagation gap, and request-dispatch defects make that a no-go even for a limited rollout.

The concept is still worth a tracer-bullet spike after roadmap stage 1 produces a public-API endpoint path. Start with one public, cacheable read experience that has both a normal deep link and a small refreshable leaf component, such as a match or ranking card. Compare stock Blazor static SSR, the full-document `hx-select` fallback, and the explicit fragment endpoint. Require the fragment path to avoid unrelated page data work, emit no per-user cookie on the public GET, and produce an unambiguous cache/history contract.

Treat authenticated mutation as a separate adoption gate. A representative form or action must prove authorization parity, invalid-token rejection for every supported unsafe verb, redirect and validation behavior, and no event-handler method confusion. This sequence tests the architectural value without making ForTheLeague the security laboratory for an unfinished package.

## Roadmap to stable v1

### 0. Reproduction and decisions

- Add a minimal .NET 10 sample that records the current endpoint-discovery failure.
- Freeze the endpoint shape: separate fragment routes by default, with same-route negotiation as a tested option.
- Spike `RazorComponentResult` for GET, named POST form binding, authorization, navigation, and streaming.
- Decide whether v1 supports trimming and whether .NET 8 compatibility is needed for a ForTheLeague migration window.

Exit gate: an architecture decision backed by runnable spikes, with no reliance on undocumented members.

### 1. Public endpoint foundation

- Upgrade the library and tests to .NET 10.
- Stop replacing Blazor's standard renderer, invoker, routing-state provider, and navigation manager.
- Implement explicit endpoint mappings, then add source generation if attribute ergonomics are required.
- Port existing route, layout, request-context, and response-header behaviors as acceptance tests.

Exit gate: normal Blazor pages behave identically with and without Htmxor registered, and fragment endpoints use public framework contracts only.

### 2. Browser-runtime independence and htmx 2 evidence

- Remove embedded htmx and make its script, configuration, extensions, and upgrade schedule application-owned.
- Build the version-neutral browser core, replaceable htmx 2 adapter, and declarative typed server protocol registry.
- Implement the full-page history-restore path and cache variation outside extension control.
- Publish a conformance runner for an application-supplied script and adapter.
- Test deep links, refresh, back/forward cache miss, boosted navigation, `hx-select`, out-of-band swaps, target elements without ids, custom extensions, and all headers against the htmx 2.0.10 reference row.

Exit gate: the reference row passes in supported browsers, a caller-owned runtime passes through the public runner without rebuilding `Htmxor.Server`, unknown `hx-*` attributes survive unchanged, and no 1.x compatibility extension is needed for documented v1 behavior.

### 3. Security closure

- Validate antiforgery on every unsafe verb, including DELETE.
- Prove authorization parity between full and fragment endpoints.
- Replace or harden the token-cookie transport and add missing-token behavior.
- Add local redirect validation, form-mapping limits, cache tests, malicious-header tests, CSP tests, and raw-HTML guidance.
- Remove component event replay or prove action tokens are bound to method, route, identity, and expiry. Add the current PATCH-to-DELETE case as a negative HTTP-boundary test.
- Run dependency, static-analysis, and application security review against the packaged sample.

Exit gate: the security matrix fails closed and the DELETE regression test is present at the HTTP boundary.

### 4. Performance and publish evidence

- Add the benchmark and concurrency harness described above.
- Measure current and proposed paths, then set project-owned budgets.
- Test cancellation, large forms, exceptions, and slow components.
- Prove direct fragments do not execute excluded component lifecycle or data work.
- Add normal publish and, if supported, trimmed publish smoke tests.

Exit gate: repeatable results meet the agreed ForTheLeague budget without correctness loss, and the published sample passes on the exact release artifacts.

### 5. Release hardening

- Finish the incomplete usage documentation for routing, layouts, forms, conditional rendering, security, caching, history, and errors.
- Define public API compatibility and package-validation baselines.
- Test the exact NuGet package in a clean consumer application.
- Publish an RC, gather a real ForTheLeague integration cycle, resolve RC findings, then tag v1.

Exit gate: no P0 or P1 gaps remain, all evidence was produced from the release candidate's exact commit and package, and the documented full-page and fragment scenarios are covered end to end.

## Issue inventory and disposition

This appendix prevents unresolved tracker work from disappearing into a new roadmap. It records all 41 issues as of the research date. “Close” means close only after the stated documentation or regression evidence exists.

### Open issues

| Issue | Current finding | v1 disposition |
| --- | --- | --- |
| [#11: htmx v2](https://github.com/egil/Htmxor/issues/11) | Still fully applicable; the 1 to 2 migration affects more than the script file. | Block v1 on the verified 2.0.10 reference row and caller-owned-runtime conformance suite, not on a package dependency. |
| [#15: browser logs](https://github.com/egil/Htmxor/issues/15) | Event volume and serialization remain unresolved. | Defer; use server HTTP tracing and correlation for v1. |
| [#16: streaming](https://github.com/egil/Htmxor/issues/16) | The current pipeline waits for quiescence and htmx does not natively consume Blazor streaming updates. | Explicitly defer unless the public-result spike proves a bounded contract. |
| [#18: response headers](https://github.com/egil/Htmxor/issues/18) | A declarative extension point is useful but unfinished. | Freeze the typed request/response feature registry and protected-header rules before API freeze. |
| [#30: event callback association](https://github.com/egil/Htmxor/issues/30) | The event id is ambiguous and is now confirmed not to be bound to method or route. | P0 if callback replay remains; otherwise remove it from v1. |
| [#40: empty action URLs](https://github.com/egil/Htmxor/issues/40) | Explicit empty strings already mean current URL. | Document and close. |
| [#48: community standards](https://github.com/egil/Htmxor/issues/48) | Security and contribution policies are missing. | Complete before stable release, especially `SECURITY.md`. |
| [#50: output caching](https://github.com/egil/Htmxor/issues/50) | Full and fragment responses need correct variance; response-wide token cookies conflict with public caching. | Security and ForTheLeague adoption gate. |
| [#56: CSRF token placement](https://github.com/egil/Htmxor/issues/56) | The readable-cookie approach remains unresolved and DELETE validation is broken. | P0 threat model and negative HTTP tests. |
| [#57: standard Blazor coexistence](https://github.com/egil/Htmxor/issues/57) | Global service replacement prevents safe gradual adoption. | Architecture gate: standard Blazor must remain unchanged. |
| [#58: htmx extensions](https://github.com/egil/Htmxor/issues/58) | htmx 2 distributes extensions separately; the htmx 4 preview already changes the extension/event model. | Keep native extensions application-owned; define the browser-adapter contract, unknown-attribute behavior, and conformance runner. |
| [#64: redirects](https://github.com/egil/Htmxor/issues/64) | Identity-flow navigation diverges under the custom navigation manager. | Cover local/external redirects, status, history, and nested paths. |
| [#67: duplicate initialization](https://github.com/egil/Htmxor/issues/67) | Duplicate `OnInitializedAsync` remains unexplained in an AWS Lambda scenario. | Reproduce before performance claims; prevent duplicate data work. |
| [#69: `hx-vals`](https://github.com/egil/Htmxor/issues/69) | Attribute capture already supports the basic custom-component case. | Document; add a JSON helper only if it makes encoding safer. |
| [#72: static files in Production](https://github.com/egil/Htmxor/issues/72) | The suggested publish-output explanation has no maintainer verification. | Published Production integration test. |
| [#75: static asset fingerprinting](https://github.com/egil/Htmxor/issues/75) | The copied renderer misses current framework resource behavior. | Release blocker and further evidence for removing the renderer fork. |

### Closed issues and decisions to carry forward

| Issue | Historical result | v1 treatment |
| --- | --- | --- |
| [#1](https://github.com/egil/Htmxor/issues/1) | Full versus fragment conditional output became `HtmxFragment`. | Preserve the behavior contract, not the renderer implementation. |
| [#2](https://github.com/egil/Htmxor/issues/2) | Added request context and typed htmx headers. | Revalidate every header against htmx 2 and CORS/cache behavior. |
| [#3](https://github.com/egil/Htmxor/issues/3) | Added the first antiforgery-cookie approach amid acknowledged uncertainty. | Treat as superseded by #56, not completed security evidence. |
| [#4](https://github.com/egil/Htmxor/issues/4) | Chose a custom router, endpoint, invoker, and renderer. | Reverse the private-framework ownership while retaining its acceptance scenarios. |
| [#5](https://github.com/egil/Htmxor/issues/5) | Used a fragment layout and `HeadOutlet` for titles. | Promise title updates only unless head merging is explicitly versioned and tested. |
| [#6](https://github.com/egil/Htmxor/issues/6) | Introduced the route attribute that became `HtmxRoute`. | Review the API after the public endpoint spike. |
| [#7](https://github.com/egil/Htmxor/issues/7) | Added custom route discovery. | Replace private runtime discovery with explicit or generated public mapping. |
| [#9](https://github.com/egil/Htmxor/issues/9) | Added the custom endpoint invoker. | Replace it or sharply isolate it; compare behavior with stock Blazor. |
| [#13](https://github.com/egil/Htmxor/issues/13) | Preferred sections/layouts over a dedicated OOB service. | Validate the pattern against htmx 2 before documenting it. |
| [#14](https://github.com/egil/Htmxor/issues/14) | Recorded the beta checklist. | Do not reuse it as v1 evidence; it treated unresolved antiforgery as complete. |
| [#17](https://github.com/egil/Htmxor/issues/17) | Considered basic JSON/JavaScript quoting sufficient. | Revisit under CSP, disabled eval, and malicious-input tests. |
| [#19](https://github.com/egil/Htmxor/issues/19) | Mapped `NavigationManager` actions to htmx headers. | Retain as redirect/history regression scenarios, not a reason to replace navigation globally. |
| [#22](https://github.com/egil/Htmxor/issues/22) | Fixed duplicate sidebar output in the sample. | Keep the scenario as a rendering regression. |
| [#25](https://github.com/egil/Htmxor/issues/25) | Established multi-fragment output filtering and acknowledged hidden lifecycle work. | Preserve UX, replace output-only pruning with direct composition. |
| [#26](https://github.com/egil/Htmxor/issues/26) | Defined boosted routing behavior. | Re-specify it with the full htmx 2 routing truth table. |
| [#27](https://github.com/egil/Htmxor/issues/27) | Folded multiple-fragment ambiguity into #25. | Keep a focused multiple-fragment endpoint test. |
| [#33](https://github.com/egil/Htmxor/issues/33) | Established `Htmxor`, `Htmx`, and `hx` naming. | Carry into the public API review. |
| [#45](https://github.com/egil/Htmxor/issues/45) | Automatic antiforgery markup was closed as not planned. | Superseded by #56; the security requirement remains open. |
| [#46](https://github.com/egil/Htmxor/issues/46) | Moved event ids into `hxor-eventid`. | The method-confusion finding reopens the underlying trust problem. |
| [#51](https://github.com/egil/Htmxor/issues/51) | Fixed swap-enum JSON casing. | Retain protocol serialization tests. |
| [#53](https://github.com/egil/Htmxor/issues/53) | Used lowercase enum member names to survive Razor attribute erasure. | Review all values against htmx 2 before API freeze. |
| [#60](https://github.com/egil/Htmxor/issues/60) | Defined consumer-owned htmx via `UseEmbeddedHtmx=false`. | Replace the broken switch with unconditional application ownership; Htmxor should have no embedded runtime to disable. |
| [#61](https://github.com/egil/Htmxor/issues/61) | Deferred package splitting. | Revisit only if static-only boundaries or client references require it. |
| [#65](https://github.com/egil/Htmxor/issues/65) | Demonstrated `[SupplyParameterFromForm]` for active search. | Convert into supported form-binding documentation and tests. |
| [#70](https://github.com/egil/Htmxor/issues/70) | Worked around Production 404s by disabling embedded htmx. | Not root-cause evidence; #72, #74, and #75 remain active. |

The discussions reinforce the same direction. [#39](https://github.com/egil/Htmxor/discussions/39) treats independently addressable fragments as routable components, [#42](https://github.com/egil/Htmxor/discussions/42) asks for gradual coexistence with other Blazor render modes, [#47](https://github.com/egil/Htmxor/discussions/47) reports confusing route boundaries and post-swap behavior, and [#73](https://github.com/egil/Htmxor/discussions/73) records the maintenance stop. The remaining discussions cover release announcements, page JavaScript, fragment lifecycle cost, AWS antiforgery/Data Protection behavior, and rejected static fragment methods; none changes the P0/P1 ordering above.

## Open questions

1. Does ForTheLeague need same-URL full and fragment endpoints, or are explicit `/fragments/...` URLs acceptable? The latter reduces routing and cache complexity.
2. Are PUT, PATCH, and DELETE important public API requirements, or can v1 use POST actions with explicit intent? Keeping them is reasonable, but every verb needs a defined binding and antiforgery contract.
3. Is full head merging required, or is title-only behavior enough?
4. Is streaming useful for a real ForTheLeague fragment, or should v1 buffer fragments deliberately?
5. Is trimmed publish a release requirement?
6. Which representative ForTheLeague pages and concurrency levels define the performance budget?
