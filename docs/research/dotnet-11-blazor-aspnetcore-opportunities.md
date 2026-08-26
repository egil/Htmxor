# .NET 11 Blazor and ASP.NET Core opportunities for Htmxor

Research date: 2026-08-26

Htmxor baseline: [`d8e09e4`](https://github.com/egil/Htmxor/tree/d8e09e4da17ab4c74fbea95d8e995137785c8395)

Upstream source snapshot: ASP.NET Core [`24708c2`](https://github.com/dotnet/aspnetcore/tree/24708c2fffe78d8fa10cc7d0139af1b54d901433), plus the published [.NET 11 Preview 7 ASP.NET Core release notes](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md). .NET 11 is still a preview and is scheduled to ship in November 2026, so unshipped APIs can still change.

This is a source and API review. Anything described as a hypothesis or spike has not been proved in an executable Htmxor prototype, browser test, or benchmark.

## Executive conclusion

.NET 11 makes the desired Htmxor v1 direction more attractive, but it does not remove its hardest implementation gap.

- Static SSR gains first-class client validation, async validation, TempData, Session-backed parameters, static QuickGrid sorting and paging, safer CSRF integration, and subtree caching. Htmxor inherits these features most reliably by delegating to the stock Razor component endpoint pipeline.
- The existing public `IRazorComponentEndpointInvoker`, `RootComponentMetadata`, and `ComponentTypeMetadata` form a much more promising execution seam than `RazorComponentResult`. For an existing `@page`, a public final endpoint convention may be able to wrap the already-built stock endpoint and select a direct root only for HTMX requests. An HTMX-only component still needs a generated endpoint carrying the stock metadata. These are the first architecture candidates to run.
- `RazorComponentResult` renders a component, but its executor does not initialize routed form handling, dispatch named forms, finish Session or TempData persistence, emit initializers, or serialize persisted component state. It is not a lifecycle-equivalent substitute for a mapped Razor component endpoint.
- The stock endpoint invoker is still POST-only for form dispatch. It has no public PUT, PATCH, or DELETE component-lifecycle action hook. Source generation can remove runtime discovery and ambiguous dispatch, but it cannot manufacture a missing renderer-owned instance invocation seam.
- The proposed Native AOT component-metadata stack could eventually provide generated route, layout, render-mode, parameter, injection, and custom attribute metadata. It is open, experimental, stacked work with no milestone. Htmxor should be able to consume it later, but v1 should not wait for it or promise it.
- No merged .NET 11 work provides direct named-fragment invocation or proves a cheaper per-request fragment path. Output selection still needs a Htmxor component convention and benchmarks.

The practical roadmap consequence is: keep the v1 product contract, replace the prototype's private framework integration, and put a short set of executable .NET 10/.NET 11 feasibility gates ahead of implementation-ready endpoint, action, validation, caching, and performance issues.

## Status model

This report uses four classifications deliberately:

| Classification | Meaning |
| --- | --- |
| **Published Preview 7** | Merged and included in a published .NET 11 preview at the research date |
| **Merged main** | Present in the pinned ASP.NET Core main snapshot, but not necessarily documented as a published preview feature |
| **Existing API** | Already shipped before .NET 11; useful, but not a new .NET 11 opportunity |
| **Open proposal or inference** | Not available as a stable dependency; requires watching or executable proof |

## The most important existing execution seam

### `IRazorComponentEndpointInvoker` plus endpoint metadata

`IRazorComponentEndpointInvoker` is public. Its contract says that a Razor component endpoint supplies the root component through `RootComponentMetadata` and the page through `ComponentTypeMetadata`. Both metadata classes and their constructors are public. These are existing APIs, not .NET 11 additions: [interface](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Endpoints/src/IRazorComponentEndpointInvoker.cs), [root metadata](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Endpoints/src/Builder/RootComponentMetadata.cs), and [page metadata](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Endpoints/src/Builder/ComponentTypeMetadata.cs).

The registered implementation remains internal, but Htmxor does not need its concrete type if it can create an endpoint with the stock metadata and call the public interface. On .NET 11 main, that implementation owns substantially more than HTML rendering:

- routing-state and standard component-service initialization;
- named POST form dispatch on the renderer-created page instance;
- navigation, not-found, status-code-page, enhanced-navigation, and streaming behavior;
- antiforgery/CSRF verdict consumption;
- Session and TempData persistence after rendering;
- JavaScript initializer emission and persisted component-state serialization.

Those responsibilities are visible together in the current [stock endpoint invoker](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs). Reusing it would let Htmxor progressively enhance a stock static-SSR Blazor application without replacing its renderer, routing-state provider, navigation manager, or endpoint invoker.

This is an opportunity, not yet a conclusion. A generated endpoint must reproduce all metadata and structural conventions the invoker expects. It must also coexist with the normal `MapRazorComponents` candidate at the same route. The executable gate is specified below.

### Why `RazorComponentResult` is not equivalent

The public result is useful for rendering an arbitrary component, but the current [result executor](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Endpoints/src/Results/RazorComponentResultExecutor.cs) only initializes streaming framing, prerenders a host component, writes streaming updates, and flushes the response. It does not perform the endpoint-invoker responsibilities listed above.

This creates a concrete .NET 11 parity gap. `[SupplyParameterFromSession]` writes values back before the response is sent, and the stock invoker explicitly calls Session and TempData persistence after all rendering completes. The result executor does neither. The Preview 5 [Session documentation](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/aspnetcore.md#supplyparameterfromsession-for-blazor) and current sources therefore make `RazorComponentResult<TWrapper>` an unsafe sole v1 seam until an integration test proves otherwise or ASP.NET Core closes the gap.

### Other public-but-framework-oriented seams

`IComponentPrerenderer` is also public and existing, but its API takes a render mode and parameters, not route/form handler state. It has no public static-render-mode value, form-dispatch argument, or streaming toggle. Its public shape does not replace the mapped endpoint lifecycle. Treat it as framework plumbing rather than a v1 extension contract. See [`IComponentPrerenderer`](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Endpoints/src/DependencyInjection/IComponentPrerenderer.cs).

The request to expose the internal `EndpointHtmlRenderer` specifically for integrating frameworks was closed as not planned in [aspnetcore#51148](https://github.com/dotnet/aspnetcore/issues/51148). Htmxor should not wait for that renderer to become public.

## Endpoint discovery and convention APIs

### Existing `@page` endpoints are the richest starting point

The public `RazorComponentsEndpointConventionBuilder.Add` and `Finally` methods can inspect and modify the endpoints produced by `MapRazorComponents`. Those endpoints already carry the stock request delegate, `ComponentTypeMetadata`, `RootComponentMetadata`, page attributes, antiforgery requirements, configured render modes, and every convention applied directly to the Razor-components builder. See the current [endpoint factory](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Endpoints/src/Builder/RazorComponentEndpointFactory.cs) and [convention builder](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Endpoints/src/Builder/RazorComponentsEndpointConventionBuilder.cs).

This creates a stronger **inferred spike** for normal/dual `@page` components than copying their routes into a second data source:

1. identify the stock page endpoint through public component metadata in a final convention;
2. leave its normal request delegate and endpoint untouched for non-HTMX requests;
3. for a direct request, temporarily expose a cached cloned endpoint whose metadata selects the Htmxor direct root, then call the original stock delegate;
4. restore the original endpoint feature after invocation.

The involved route endpoint and endpoint-feature types are public, but this exact composition is not a documented Blazor extension pattern. The spike must prove metadata precedence, middleware policy timing, status-page re-execution, concurrency safety, and render-mode behavior. If it works, the normal path remains byte-for-byte stock and all builder-level conventions remain attached to the one routed endpoint.

An HTMX-only component has no stock page endpoint to wrap. That path still needs a source-generated endpoint on the exact route builder, public root/page metadata, and the stock invoker. It must address the builder-only convention gap described below.

### A useful helper, but not a component catalog

`ComponentEndpointConventionBuilderHelper` is public under an `.Infrastructure` namespace and explicitly says it is not recommended outside the Blazor framework. The helper type exists in .NET 8, while `GetEndpointRouteBuilder` appears by .NET 9 and remains in current main. The method can replace Htmxor's reflection over the builder's private `ApplicationBuilder` property, but it exposes only the route builder. It exposes neither discovered component types nor the builder's accumulated conventions. Compare the [.NET 8 helper](https://github.com/dotnet/aspnetcore/blob/v8.0.29/src/Components/Endpoints/src/Builder/ComponentEndpointConventionBuilderHelper.cs), [.NET 9 helper](https://github.com/dotnet/aspnetcore/blob/v9.0.0/src/Components/Endpoints/src/Builder/ComponentEndpointConventionBuilderHelper.cs), and [current helper](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Endpoints/src/Builder/ComponentEndpointConventionBuilderHelper.cs).

The helper can remove one private-reflection dependency immediately. It cannot solve route discovery, and its own documentation makes it a compatibility aid rather than a durable architectural center.

### Builder-only conventions still do not flow to separate endpoints

`RazorComponentsEndpointConventionBuilder.Add` stores conventions in private lists. A separately generated Htmxor endpoint on the same `IEndpointRouteBuilder` does not automatically inherit a `.RequireAuthorization()`, `.RequireRateLimiting()`, host restriction, request-size limit, or other convention applied only to the Razor components builder. The current [builder source](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Endpoints/src/Builder/RazorComponentsEndpointConventionBuilder.cs) has no public convention enumeration or forwarding surface.

The v1 design should therefore keep both endpoint families under the same route group for shared structural policy and validate security-relevant effective metadata at startup. .NET 11 Preview 7 adds `AuthorizationPolicy.CombineAsync(IAuthorizationPolicyProvider, IEnumerable<object>)`, which gives Htmxor a supported way to compare effective authorization requirements expressed as `IAuthorizeData`, `AuthorizationPolicy`, and `IAuthorizationRequirementData`. It does not copy or compare the other endpoint metadata classes. See the [Preview 7 authorization notes](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md#consistent-authorization-metadata-across-the-stack).

## Published .NET 11 changes Htmxor should inherit

### Static SSR forms and validation

.NET 11 Preview 5 adds browser-side validation for static SSR `EditForm` components containing `DataAnnotationsValidator`, including enhanced and non-enhanced forms. It also adds async form validation and localization; Preview 7 stabilizes the API and makes `EditContext.ValidateAsync` the preferred path. See [Preview 5 client validation](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/aspnetcore.md#blazor-ssr-supports-client-side-validation) and the [Preview 7 API refinements](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md#breaking-changes).

This directly supports Htmxor's progressive-enhancement contract: built-in `EditForm`, `Input*`, `ValidationMessage`, and third-party components designed for stock static SSR should continue to work when Htmxor is added. That contract is realistic only when direct Htmxor requests retain the stock form mapper and renderer lifecycle.

There is one HTMX-specific edge that the release notes do not cover. Current browser source initializes validation at initial DOM load and on Blazor's `enhancedload`. `createBlazorValidation()` returns without defining its custom element when the document contains no `<blazor-client-validation-data>` carrier. Once initialized, the carrier's `connectedCallback` and `disconnectedCallback` register and remove rules, so ordinary later DOM insertion should work. HTMX swaps do not emit Blazor `enhancedload`. Therefore, if the first validatable form arrives through HTMX after a form-free initial page, the validation service may never initialize. This is a source-based hypothesis requiring a browser test, not a confirmed defect. See [`Boot.Web.ts`](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Web.JS/src/Boot.Web.ts) and [`BlazorAdapter.ts`](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Web.JS/src/Validation/Adapters/BlazorAdapter.ts).

### TempData and Session

Preview 2 adds TempData to Blazor SSR. Preview 4 adds `[SupplyParameterFromTempData]`, and Preview 5 adds `[SupplyParameterFromSession]`. They support status messages, redirect-after-post, carts, and multi-step forms without app-specific glue. See [TempData support](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview2/aspnetcore.md#tempdata-support-for-blazor), [`SupplyParameterFromTempData`](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview4/aspnetcore.md#supplyparameterfromtempdata-for-blazor), and [`SupplyParameterFromSession`](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/aspnetcore.md#supplyparameterfromsession-for-blazor).

These features are useful beyond their direct functionality: they are lifecycle canaries. An Htmxor direct request that renders plausible markup but loses write-back or redirect behavior is not compatible with stock static SSR. The stock-invoker spike must cover both.

### A supported custom cascading-parameter supplier

.NET 11 main adds public `TryAddCascadingValueSupplier<TAttribute>` and `CascadingParameterSubscription`. Htmxor can use this supported seam for an optional attribute-shaped request value such as `[SupplyParameterFromHtmx]`, rather than replacing the renderer or wrapping every page only to supply request state. The existing `AddCascadingValue` remains sufficient for a normal typed `HtmxContext` cascade. See the current [registration API](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Components/src/CascadingValueServiceCollectionExtensions.cs) and [subscription contract](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Components/src/CascadingParameterSubscription.cs).

This is a useful public extension point, not a reason to add a new public Htmxor attribute unless real developer code benefits from it.

The supplier callback also receives the current `ComponentState`, whose public `Component` property exposes the actual renderer-created instance. In principle, an Htmxor-specific cascading attribute could use this callback to register an opted-in component instance in a request-scoped action registry. That is the most promising newly identified instance-access experiment in .NET 11.

It is not yet an action-dispatch solution. `ComponentState` is documented as an internal renderer implementation detail, the supplier callback constructs a synchronous subscription rather than an awaited pre-response action, and it exposes no supported operation that pauses the stock invoker, invokes an async custom verb, rerenders, and lets the stock invoker serialize the result. See [`ComponentState`](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Components/src/Rendering/ComponentState.cs). Include this route in the unsafe-action spike, but do not make a pubternal implementation-detail type part of the v1 public contract.

Preview 1's new `IComponentPropertyActivator` is narrower still: it customizes how `[Inject]` properties are populated. It does not expose component routing, construction, lifecycle, or event dispatch. See the [Preview 1 API description](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/aspnetcore.md#icomponentpropertyactivator).

### QuickGrid as a progressive-enhancement compatibility test

Preview 5 makes QuickGrid sorting and pagination work in static SSR by rendering URL-driven enhanced controls instead of requiring `@onclick`. See [QuickGrid without interactivity](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/aspnetcore.md#quickgrid-works-without-interactivity).

QuickGrid is a strong first-party compatibility canary for Htmxor. Its tests should cover:

- full static SSR with no Htmxor behavior;
- the same component after adding Htmxor;
- HTMX-owned updates where requested;
- ordinary query navigation and history restoration;
- no double interception between HTMX, Blazor enhanced navigation, and enhanced forms.

Passing this case would provide better evidence for third-party static-SSR compatibility than Htmxor-only sample components.

### CSRF and antiforgery integration

.NET 11 Preview 6 introduces Fetch Metadata and Origin-based CSRF protection. The final Preview 7 scope validates only endpoints whose metadata has `IAntiforgeryMetadata.RequiresValidation`; Blazor SSR form endpoints attach the required metadata automatically. The middleware records a verdict in `IAntiforgeryValidationFeature`, and the stock component invoker consumes it when it processes a form. See the [Preview 7 correction](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md#breaking-changes), the merged [consumer-verdict change](https://github.com/dotnet/aspnetcore/pull/67082), and the merged [metadata scoping change](https://github.com/dotnet/aspnetcore/pull/67460).

Htmxor should borrow the framework's ordering and verdict instead of copying cryptographic form-validation logic:

1. Every generated unsafe endpoint carries the correct `IAntiforgeryMetadata` unless explicitly and validly disabled.
2. Middleware runs before binding or application callbacks.
3. A failed verdict stops the request with a generic non-swappable response.
4. Token-based antiforgery remains supported. Fetch Metadata and Origin checks intentionally tolerate clients that do not send browser headers, so the new CSRF middleware is not a universal replacement for tokens.

The stock component invoker still only enters its form path for `POST`. A generated PUT, PATCH, or DELETE action therefore needs its own consumer that enforces the same middleware verdict before form reading, binding, or callback invocation. This is a remaining Htmxor responsibility.

The prototype does not currently meet that requirement. Its unsafe-method condition includes POST, PUT, and PATCH in the validation branch but omits DELETE, and operator precedence applies the exception-handler guard only to PATCH. See the current [custom invoker](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Endpoints/HtmxorComponentEndpointInvoker.cs#L155-L166). The unsafe-action gate must retain DELETE as a negative regression even if the prototype path is later removed.

### `CacheView` and fragment safety

Preview 7 adds `CacheView`, which caches the rendered HTML of a static-SSR subtree. A cache hit avoids instantiating and rendering the cached child components. It supports route, query, header, cookie, user, culture, and custom vary dimensions. It skips non-GET requests and streaming SSR. See the [Preview 7 feature notes](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview7/aspnetcore.md#cache-blazor-ssr-output-with-cacheview) and current [`CacheView`](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Endpoints/src/CacheView/CacheView.cs).

The related `[CacheBehavior]` and `[CacheCondition]` APIs create a safety model for request-dependent components. `Rerender` creates a live per-request hole; `Throw` rejects an unsafe placement unless declared vary dimensions make it safe. See [`CacheBehaviorAttribute`](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Components/src/CacheBehaviorAttribute.cs) and [`CacheConditionAttribute`](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Components/src/CacheConditionAttribute.cs).

`HtmxFragment` cannot be a `Rerender` live hole. A live cached component's parameters are captured once and replayed, and the CacheView writer explicitly rejects `RenderFragment` and `RenderFragment<T>` parameters because their content would be frozen to the first render. `HtmxFragment.ChildContent` is a `RenderFragment`, so `[CacheBehavior(CacheBehavior.Rerender)]` would fail. See the current [`CacheViewTextWriter`](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Endpoints/src/Rendering/CacheViewTextWriter.cs).

The fail-closed v1 annotation should instead be `[CacheBehavior(CacheBehavior.Throw)]`: an application cannot accidentally place the request-dependent selection boundary inside an outer `CacheView`. A developer may place a `CacheView` *inside* the selected `HtmxFragment` to cache intentionally stable content. That arrangement reduces work in a selected GET subtree without caching the request-dependent fragment boundary.

`CacheView` does not replace HTTP output caching. Htmxor must still emit the correct `Vary` response fields, including request headers that choose full-page versus fragment representations, and must keep unsafe or personalized responses private or uncached. `VaryByHeader="HX-Request,HX-Target"` affects the component subtree key; it does not emit HTTP `Vary` on Htmxor's behalf.

Preview 1's public `IOutputCachePolicyProvider` can support application- or tenant-specific HTTP output-cache policy selection, but it does not infer Htmxor's representation headers. Htmxor still owns safe endpoint metadata and variation. See the [Preview 1 output-cache API](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/aspnetcore.md#ioutputcachepolicyprovider).

### Child content across render-mode boundaries

Preview 5 allows non-generic `RenderFragment` child content to cross SSR-to-interactive render-mode boundaries. The framework renders and captures the fragment on the server, then projects the captured tree into the interactive component. See [the Preview 5 feature](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/aspnetcore.md#pass-child-content-across-render-mode-boundaries) and merged [aspnetcore#66528](https://github.com/dotnet/aspnetcore/pull/66528).

This helps markup-only composition in pages that also use Server, WebAssembly, or Auto render modes. It does not serialize Htmxor action callbacks, generic `RenderFragment<T>`, element references, or event ownership, and it does not turn an HTMX fragment into an interactive Blazor component.

The stable model remains two paths:

- a normal page request stays on the stock Blazor endpoint and may use Static, Interactive Server, Interactive WebAssembly, or Interactive Auto;
- a direct Htmxor request returns static SSR for an HTMX-owned region.

Htmxor can coexist with Auto mode, but neither runtime should own the same live DOM region or the same navigation. .NET 11 does not add a feature that dynamically converts an HTMX-owned fragment into a running Auto component.

There is a narrower render-mode uncertainty. If Auto is applied globally to the normal `<Routes>` root, a separate Htmxor direct root can plausibly remain static. A page component that carries its own definition-level `@rendermode InteractiveAuto` may still emit an interactive boundary when the stock renderer invokes it, and no public per-invocation "ignore this render mode" switch was identified. The stock-invoker gate must test these two cases separately.

### Navigation and browser configuration

Preview 1 adds relative navigation support and other `NavigationManager` helpers. Preview 6 adds server-side `WithBrowserOptions`, including Blazor DOM-preservation and Server/WebAssembly/Auto options. See [relative navigation](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/aspnetcore.md#relative-navigation-with-relativetocurrenturi) and [browser options](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview6/aspnetcore.md#configure-blazor-client-behavior-from-the-server).

These features reinforce the recommendation to retain stock `NavigationManager` behavior. Browser options may help an application configure the Blazor side of DOM preservation, but they do not coordinate HTMX swaps or settle navigation ownership. That remains an Htmxor browser-conformance concern.

## What .NET 11 does not solve

### PUT, PATCH, and DELETE instance actions

The current stock invoker calculates its form path with `HttpMethods.IsPost`. PUT, PATCH, and DELETE requests do not initialize the form handler or call `DispatchSubmitEventAsync`; they follow the non-POST rendering path. This is explicit in the current [invoker source](https://github.com/dotnet/aspnetcore/blob/24708c2fffe78d8fa10cc7d0139af1b54d901433/src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs).

There is no merged public API that says, in effect, "render this routed component, locate this generated component-local handler on the renderer-owned instance, invoke it, then continue the stock lifecycle." That gap is why replacing instance callbacks with static methods appeared attractive in the earlier interface sketch. Static methods avoid renderer-instance access, but they also recreate endpoint handlers inside a `.razor` file and abandon the component lifecycle. That trade is inconsistent with the revised convention-over-configuration direction.

The v1 action spike should retain lifecycle-preserving instance dispatch as the design goal, with build-time route/verb/handler validation and no runtime render-tree scanning. If the framework exposes no supported way to dispatch such actions, Htmxor should document the boundary and consider an upstream API proposal before freezing a static alternative.

### Direct named-fragment invocation

No merged .NET 11 API maps an endpoint directly to a named region inside a routed component. Htmxor can still render the owning component under a selection cascade so unselected `HtmxFragment` child content is not invoked, but routing, component construction, parameter binding, and owning lifecycle work still run. This is output pruning, not complete execution pruning.

`CacheView` can avoid repeated stable subtree work. It does not provide request-time named selection, and it is inactive on unsafe verbs and streaming renders.

### A new public endpoint-renderer extension point

No published .NET 11 feature exposes `EndpointHtmlRenderer`, its form dispatcher, or a new public route-to-component execution builder. The open async-rendering work in [aspnetcore#65206](https://github.com/dotnet/aspnetcore/pull/65206) changes framework internals and remains a blocked draft with no milestone. It is not a v1 dependency.

### A measured per-request performance improvement

.NET 11 runtime libraries use [runtime-async](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview4/runtime.md#runtime-libraries-are-now-compiled-with-runtime-async), which may reduce async state-machine overhead in framework code, and `CacheView` can eliminate repeated GET subtree rendering. Neither establishes Htmxor's per-request performance. The Blazor SSR performance investigation [aspnetcore#65193](https://github.com/dotnet/aspnetcore/issues/65193) is open under .NET 12 Planning.

Htmxor still needs benchmarks for normal pages, direct whole-component responses, one and multiple selected fragments, valid and invalid forms, unsafe actions, streaming, and errors. Measure latency, allocations, bytes, component instantiation, and lifecycle work on .NET 10 and the current .NET 11 preview before setting a v1 contract.

## Open proposals worth watching, not building v1 around

### Native AOT component metadata stack

Four closely related Blazor PRs are open and unmilestoned at the research date:

| PR | Proposed capability | Current status | Htmxor relevance |
| --- | --- | --- | --- |
| [#68295](https://github.com/dotnet/aspnetcore/pull/68295) | Metadata-first serialization and application metadata registration | Open; based on `main` | Foundation only |
| [#68299](https://github.com/dotnet/aspnetcore/pull/68299) | Generate application component descriptors | Open; stacked and blocked | High if it lands |
| [#68300](https://github.com/dotnet/aspnetcore/pull/68300) | Generate framework component descriptors | Open; stacked and blocked | Completes framework coverage |
| [#68302](https://github.com/dotnet/aspnetcore/pull/68302) | Strict Native AOT proof | Open; stacked and blocked | Proof, not an execution API |

The proposed public `ComponentDescriptor` is experimental and includes type, activation, parameters, injectables, and an open metadata list. The generator proposal says it reconstructs route, layout, render-mode, endpoint, and custom component attributes and moves discovery toward generated metadata first, reflection last. If it lands, Htmxor could consume compiler-generated `RouteAttribute` and Htmxor component attributes instead of maintaining a parallel reflection catalog.

It would not automatically discover or dispatch arbitrary inline `@onpost`, `@ondelete`, or lambda expressions. Htmxor must encode action identity as analyzable metadata and still needs a runtime instance-dispatch seam. The final proof also explicitly excludes HTTP form mapping as not Native-AOT-supported in that matrix. Generated component metadata would therefore not make Htmxor forms or unsafe actions Native AOT compatible by itself.

Keep Htmxor discovery behind an adapter:

1. v1 owns a generated catalog from stable inputs;
2. a future adapter may consume framework `ComponentDescriptor.Metadata` when it becomes a shipped contract;
3. reflection remains a compatibility fallback only where the target framework requires it.

### Source generation over Razor files

The Razor SDK already passes `.razor` files to the compiler as `AdditionalFiles`, so an Htmxor incremental generator can read their raw text. See the SDK's [`Microsoft.NET.Sdk.Razor.SourceGenerators.targets`](https://github.com/dotnet/sdk/blob/main/src/RazorSdk/Targets/Microsoft.NET.Sdk.Razor.SourceGenerators.targets). Roslyn generators run unordered and do not see files created by other generators, so a separate Htmxor generator cannot assume it can inspect Razor's generated C# semantic model in the same pass. See the [Roslyn source-generator design](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.md#high-level-design-goals).

The Razor team froze the old compiler packages while it works toward a future supported analyzer/compiler API; no new stable third-party Razor semantic extension point was identified for .NET 11. See the official [Razor compiler API announcement](https://github.com/dotnet/razor/issues/8399).

Consequently, source generation can clearly own route/component attribute discovery and deterministic endpoint catalogs. Exact semantic discovery of conditional attributes, lambdas, reused child components, and inline callback expressions remains a spike. An analyzer may treat literal `hx-get`, `hx-post`, and related attributes as evidence and diagnostics, but they should not silently grant server verbs when the handler binding cannot be proved.

### Other open work

- [#67617](https://github.com/dotnet/aspnetcore/pull/67617), a `RazorComponentResult` parameter analyzer, is open and dirty. It improves type diagnostics if merged but does not add lifecycle parity.
- [#63821](https://github.com/dotnet/aspnetcore/pull/63821), framework endpoint marker/configuration APIs, is an open dirty draft. It targets Blazor infrastructure endpoints, not component-local Htmxor routes or handlers.
- Nested routing [#11212](https://github.com/dotnet/aspnetcore/issues/11212) and custom route constraints [#28938](https://github.com/dotnet/aspnetcore/issues/28938) are open under .NET 12 Planning. Htmxor should not assume .NET 11 will add either capability.

## Direct comparison with the prototype

| Prototype technique | Better current direction | Remaining gap |
| --- | --- | --- |
| Reflect private builder/application/component collection to discover pages | Wrap existing `@page` endpoints through public final conventions; use a generated catalog and `GetEndpointRouteBuilder` for HTMX-only components | No public stock component catalog, and the conditional endpoint-metadata swap is still an inferred spike |
| Replace `IRazorComponentEndpointInvoker` | Generate endpoints that delegate to stock `IRazorComponentEndpointInvoker` | Must prove full metadata and route-candidate parity |
| Copy/fork `EndpointHtmlRenderer` behavior | Keep stock invoker and renderer | No public lifecycle action dispatch for PUT/PATCH/DELETE |
| Reflect private form and antiforgery renderer state | Consume endpoint metadata and `IAntiforgeryValidationFeature`; use stock POST form pipeline | Htmxor unsafe-verb consumer still required |
| Replace `NavigationManager` | Preserve stock navigation and redirect handling | Translate direct HTMX redirects/history without double ownership |
| Render to discover callbacks and scan the render tree | Generate deterministic handler/verb catalog and diagnostics | Binding an instance handler to the renderer-owned component is unproved |
| Cascading-value replacement hacks | Existing `AddCascadingValue`; optional .NET 11 `TryAddCascadingValueSupplier<TAttribute>` | Decide whether an attribute-shaped public API earns its complexity |
| `HtmxFragment` with request predicate | Generated selection plan plus request cascade; fail-closed `CacheBehavior.Throw`; allow `CacheView` inside the selected fragment | Cache safety and lifecycle/performance measurements |

The prototype dependencies are visible in its [service replacements](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/DependencyInjection/HtmxorApplicationBuilderExtensions.cs), [private endpoint discovery](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Builder/HtmxorComponentEndpointRouteBuilderExtensions.cs), [custom invoker](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Endpoints/HtmxorComponentEndpointInvoker.cs), and [renderer fork](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Rendering/HtmxorRenderer.cs).

## Required executable gates before v1 breakdown

### Gate 1: stock-invoker dual and HTMX-only endpoints

Create two bounded candidates that delegate to the registered `IRazorComponentEndpointInvoker`:

1. for an existing `@page`, use a public final endpoint convention and conditionally invoke the original stock delegate with the direct-root metadata while leaving normal requests untouched;
2. for an HTMX-only component, create the smallest generated endpoint carrying `RootComponentMetadata`, `ComponentTypeMetadata`, route values, HTTP method metadata, authorization, antiforgery, render-mode metadata, and the Htmxor selection cascade on the exact route group.

Prove on .NET 10 and .NET 11 Preview 7:

- full normal GET remains stock;
- direct GET renders the intended page/component representation;
- route, query, form, and cascading parameters match stock behavior;
- valid and invalid named `EditForm` POSTs run the renderer-owned component handler;
- authorization, antiforgery, request-size, rate-limit, and host policy are no weaker;
- navigation, redirects, not-found, status-code pages, streaming, and exception handling match stock behavior;
- TempData and Session read/write survive the request;
- interactive normal routes still support Server, WebAssembly, and Auto while direct responses remain static SSR;
- builder-only convention mismatches are detected rather than silently ignored.
- root-level Auto and definition-level `@rendermode InteractiveAuto` are tested as distinct cases.

Pass condition: the dual route preserves the original stock normal delegate and its effective metadata, the HTMX-only route uses no app-authored endpoint handler, and neither path uses private reflection, service replacement, or a renderer copy. Failure should identify the exact missing public seam.

### Gate 2: lifecycle-preserving unsafe actions

First prove the negative contract: PUT, PATCH, and DELETE do not dispatch through the stock named-form path. Then prototype generated component-local handler metadata and attempt to invoke the renderer-owned component instance without scanning the render tree or creating static Minimal-API-style methods.

Cover exact method matching, inherited/component-local handlers, ambiguous handlers, authorization composition, antiforgery verdicts, binding failures, validation, navigation, and declared fragment results.

Pass condition: normal Blazor lifecycle is retained and the handler/verb set is known before serving requests. If this cannot be achieved on a supported API, record the limitation and draft an upstream API proposal before freezing the v1 action syntax.

### Gate 3: HTMX-inserted static validation

Use a real browser and the shipped Preview 7 Blazor script:

1. load a page with no validatable form, then insert the first static-SSR `EditForm` through HTMX;
2. load a page with an initial form, then replace and remove form carriers through HTMX;
3. verify built-in and custom validation, cleanup, `novalidate`, invalid-submit blocking, and server fallback;
4. verify no dependence on a particular htmx version beyond the replaceable adapter hook.

Pass condition: stock validation works after each swap. If the first-form hypothesis reproduces, implement the smallest public bridge or pursue an upstream initialization hook.

### Gate 4: fragment cache isolation

Test that `[CacheBehavior(CacheBehavior.Throw)]` prevents an `HtmxFragment` from being captured by an outer `CacheView`, and test a stable `CacheView` inside a selected fragment. Vary request mode, target, route, query, identity, and culture across concurrent requests.

Pass condition: no full/fragment, target, identity, antiforgery, or culture data crosses requests; lifecycle behavior is documented; HTTP `Vary` remains correct independently of `CacheView`.

### Gate 5: compatibility and performance matrix

Run built-in `EditForm`/`Input*`/validation, QuickGrid, TempData, Session, streaming, and one representative third-party static-SSR component before and after adding Htmxor. Add Interactive Auto normal pages and an HTMX-owned static region to the browser suite.

Benchmark normal, direct whole-component, one-fragment, multi/OOB, form, unsafe action, validation-error, and unhandled-error paths. Compare stock .NET 10, stock .NET 11, the prototype, and the public-seam spike.

Pass condition: compatibility deltas and per-request costs are measured. Do not set a v1 latency or allocation promise from framework release notes alone.

## Roadmap recommendation

Do not wait for .NET 11 GA or the open Native AOT stack before beginning v1. Use .NET 10 LTS as the stable implementation baseline and keep a .NET 11 preview/RC compatibility lane so the new static-SSR features shape the design before it freezes.

Add one high-level roadmap workstream named **ASP.NET Core execution and compatibility**, with the five gates above. Only break route generation, unsafe actions, validation interop, cache integration, and performance into agent-ready implementation issues after their respective gate passes.

Maintain a separate upstream watch list for:

- the Native AOT component-metadata stack;
- any public Razor semantic analyzer API;
- a public component action/form dispatch seam;
- `RazorComponentResult` endpoint-lifecycle parity;
- Blazor SSR performance work.

The v1 product contract should remain unchanged by this research: `.razor` route ownership, normal-only/HTMX-only/both reachability, convention-over-configuration defaults, instance lifecycle, standard Blazor static-SSR compatibility, inline single/multiple fragment selection, application-owned htmx assets, and explicit security/cache invariants. The part to discard is the prototype's private framework integration, not its developer-facing reason for existing.
