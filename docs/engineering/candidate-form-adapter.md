# Endpoint candidate form-service adapter

When an ordinary stock static SSR component POST runs through the endpoint
candidate, Htmxor honors effective antiforgery policy before binding, lifecycle,
and callbacks, and preserves stock form, validation, lifecycle, and HTTP output.
The paired TestServer suite is the compatibility boundary: it runs the installed
Razor endpoint factory, middleware, mapper, converters, options, validation, and
component callbacks. Public `AddHtmxor` registers the candidate as the global
endpoint invoker after the paired parity boundary passes.

[Issue #189](https://github.com/egil/Htmxor/issues/189) and its
[approved adapter decision](https://github.com/egil/Htmxor/issues/189#issuecomment-5554452348)
authorize this replaceable internal boundary. No form runtime is copied.

## .NET 11 configured protection

Under the [approved #207 contract](https://github.com/egil/Htmxor/issues/207),
the .NET 11 adapter consumes the effective `IAntiforgeryValidationFeature` rather
than adding direct token validation when the feature is absent. The installed
`EndpointMiddleware` checks required protection middleware before invoking the
component endpoint. Generated unsafe callbacks reject a failed verdict before
activation; an activated callback bypasses ordinary POST named-form dispatch.
Actionless POST retains its ordinary named-form behavior after successful
protection. Effective metadata opt-outs are preserved instead of overwritten
by generated endpoint defaults. The .NET 10 direct token fallback is retained.

Streaming token preparation follows the .NET 11 invoker: it generates tokens
only when `HttpContext.Items` contains
`__AntiforgeryMiddlewareWithEndpointInvoked`. This reads the framework's
request-local invocation marker through the public items collection; it adds
no reflection or private member accessor. The exact marker value is mirrored
from `MiddlewareInvokedKeys.Antiforgery` and monitored as source, alongside the
existing invoker reimplementation watch. The shared key file is .NET 11 source;
the new conditional use does not change .NET 10 token timing.

Protection/token timing was synchronized on **2026-09-13** against ASP.NET Core
**v11.0.0-rc.1.26425.128**, commit
**c3325eeb6b47bc6383c127d4f4827dc9642a2b6e**. Exact sources:

- [RazorComponentEndpointInvoker.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs): effective verdict and conditional streaming token generation.
- [MiddlewareInvokedKeys.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Shared/MiddlewareInvokedKeys.cs): mirrored antiforgery invocation key in `HtmxorEndpointCandidate.cs`.
- [EndpointMiddleware.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Http/Routing/src/EndpointMiddleware.cs): required-middleware checks.
- [CsrfProtectionMiddleware.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/DefaultBuilder/src/Internal/CsrfProtectionMiddleware.cs) and [AntiforgeryMiddleware.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Antiforgery/src/AntiforgeryMiddleware.cs): configured validation and precedence.

The Issue210 real hosted security matrix and paired stock/Htmxor streaming
controls own this parity boundary. Their exact commands, per-target counts and
limitations belong to the issue's delivery receipts. Browser credential
transport and later framework releases require their own evidence. See the
[developer protection guide](../htmxor-v1-feature-guide.md#application-owned-request-protection)
for the POST/PUT/PATCH-only token middleware warning and explicit DELETE setup.

## .NET 11 ordinary response representation

[Issue #214](https://github.com/egil/Htmxor/issues/214) aligns the non-streaming
response adapter with ASP.NET Core **v11.0.0-rc.1.26425.128**, commit
**c3325eeb6b47bc6383c127d4f4827dc9642a2b6e**, synchronized **2026-09-13**.
The .NET 10 branches retain their existing representation.

- [EndpointHtmlRenderer.Streaming.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs): emit one `Blazor-Configuration` comment before the first interactive boundary, including Server. Read public `BrowserOptions.GetBrowserOptions`, retain application configuration and its serialization attributes, fill only absent environment defaults, and encode camel-case, null-omitting JSON as base64. `HtmxorEndpointCandidateRenderer.BrowserConfiguration.cs` owns this coordination and has an explicit source watch. Enhanced exception responses retain the framing header even though error rendering waits for quiescence; status re-execution and direct HTMX responses retain their separate framing decisions.
- [EndpointHtmlRenderer.Prerendering.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Prerendering.cs): resolve nested explicit modes and infer persistence mode from the closest render-mode boundary through supported `GetComponentState` and `ParentComponentState`, so descendants retain their ancestor's marker boundary and persisted-state store routing. This coordination stays in `HtmxorEndpointCandidate.cs` under the existing renderer-prefix watch.
- [RazorComponentEndpointInvoker.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs): emit final persisted state after status-code re-execution while suppressing it for exception handling. The existing invoker watch covers this conditional change.

These changes add no private accessor or public authoring API. Framework options,
JSON converters, persistence services and render-tree generation remain real
framework dependencies. The existing package-retained MIT license applies to the
adapted coordination. The Issue214 paired TestServer contract compares decoded
configuration/state and full response ordering, retains scoped authentication and
initializer/resource metadata, and holds distinct requests inside real component
initialization to establish overlap. Exact green commands and counts belong to
the issue's delivery receipts. This boundary does not certify browser boot,
authentication refresh, detached hydration, incremental streaming, or a later
framework release.

## .NET 11 Session parameter persistence

The [approved #212 decision](https://github.com/egil/Htmxor/issues/212#issuecomment-5654425948)
extends the isolated adapter to two internal instance members on
`Microsoft.AspNetCore.Components.Endpoints.SessionCascadingValueSupplier` in
`Microsoft.AspNetCore.Components.Endpoints.dll`:

- `void SetRequestContext(Microsoft.AspNetCore.Http.HttpContext)` initializes the
  actual scoped supplier after request protection succeeds and before rendering.
- `System.Threading.Tasks.Task PersistAllValues()` captures completed component
  values after rendering and callbacks, before final response flush. Selecting a
  fragment changes emitted HTML, not the component values written back.

Completed non-streaming `NotFound()` responses also persist values from surviving
root components before the early 404 return suppresses output. That early path
does not capture values while rendering work is still pending.

`HtmxorEndpointCandidateSessionServices` validates the internal nongeneric type
and declared internal nongeneric instance methods, exact parameters and return
types during registration. It caches only type/member metadata, resolves the
existing supplier from each request scope, and preserves underlying exceptions
without retrying callbacks. Missing optional supplier registration remains a no-op
as in stock. The framework still owns Session parameter supply, serialization,
cookie establishment, middleware and storage; no Htmxor Session option is added.
The adapter and its calls are excluded from the .NET 10 target.

Synchronized **2026-09-13**, ASP.NET Core **v11.0.0-rc.1.26425.128**, commit
**c3325eeb6b47bc6383c127d4f4827dc9642a2b6e**. Exact monitored sources:

- [SessionCascadingValueSupplier.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/SessionCascadingValueSupplier.cs): the two private-access dependencies in the Session adapter, explicitly watched with `api: none`.
- [EndpointHtmlRenderer.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs): request initialization order under the existing renderer watch.
- [RazorComponentEndpointInvoker.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs): post-render persistence under the existing invoker watch.

The Issue212 hosted contract observes fresh same-cookie and distinct-cookie reads,
normal and selected responses, awaited work and actions, rejected requests with
existing data, and overlapping independent sessions. Streaming ordering belongs
to #215; this slice does not establish distributed-store deployment, same-session
conflict policy, first-cookie creation through the attribute alone, or browser
cookie transport. Exact verification belongs to delivery receipts. The existing
package-retained ASP.NET Core MIT license covers adapted coordination.

## .NET 11 TempData availability and write-back

The [approved #213 decision](https://github.com/egil/Htmxor/issues/213#issuecomment-5686489846)
extends the isolated adapter to three framework-owned members in
`Microsoft.AspNetCore.Components.Endpoints.dll`:

- internal `void TempDataCascadingValueSupplier.SetRequestContext(Microsoft.AspNetCore.Http.HttpContext)`
  initializes the existing scoped supplier after request protection succeeds and
  before rendering, so `[SupplyParameterFromTempData]` reads the actual request.
- internal static `Microsoft.AspNetCore.Components.ITempData TempDataProviderServiceCollectionExtensions.GetOrCreateTempData(Microsoft.AspNetCore.Http.HttpContext)`
  returns the framework-owned request-local dictionary for the public `ITempData`
  cascade.
- public `void TempDataService.Persist(Microsoft.AspNetCore.Http.HttpContext)`,
  resolved through existing DI, runs stock callbacks, consumption and retention
  and the provider write-back before the final response flush.

Stock reaches all three through `EndpointHtmlRenderer`, which the candidate
replaces, so none of them runs under Htmxor without this coordination. The public
`ITempData` surface offers dictionary, `Get`, `Peek` and `Keep` but no
initialization or persistence operation; the provider interface and the dictionary
`Save` operation are internal, and enumerating the framework dictionary consumes
retained keys rather than saving them. Invoking the framework members keeps that
state machine framework-owned instead of reimplementing it.

`AddTempData` cascades `ITempData` from the stock renderer's request.
`HtmxorEndpointCandidateServices` validates that exactly one such scoped supplier
is registered, removes it, and re-adds the same public cascade bound to the
candidate renderer's current request, reimplementing that upstream registration.
Unlike stock's `TryAddCascadingValue`, the replacement adds unconditionally: a
second registered `ITempData` cascade fails the exactly-one check with a named
error rather than being silently dropped or duplicated. Application customization through
`RazorComponentsServiceOptions` is untouched, and a changed upstream registration
shape fails fast instead of silently losing the cascade.

`HtmxorEndpointCandidateTempDataServices` validates each internal nongeneric type
and the declared member name, accessibility, exact parameters and return type
during registration. It caches only type and member metadata, resolves the
supplier and service from each request scope, and preserves underlying exceptions.
A missing optional supplier registration remains a no-op as in stock. The
framework retains the dictionary, supplier, provider, serializer, data protection
and request scope; no TempData implementation is copied, no private field is
written, and no Htmxor TempData option is added. The adapter and its calls are
excluded from the .NET 10 target.

Write-back runs where stock runs it: after every component, including streaming,
has finished, and alongside Session persistence. Selecting a fragment changes
emitted HTML, not the values written back. The completed non-streaming `NotFound()`
path also runs write-back, mirroring the Session behavior #212 established, and is
covered by its own hosted case: a component that writes a message and then reports
not found returns 404 with suppressed output while a later request still consumes
the message once.

A `NavigationException` raised by a completed form submit now redirects instead of
escaping the candidate invoker, matching the stock invoker, which redirects and
still runs write-back so a post-redirect-get message survives. The handler mirrors
stock `EndpointHtmlRenderer.HandleNavigationBeforeResponseStarted`, retaining its
enhanced-navigation opaque redirection for external destinations, and adds one
branch: an htmx request cannot follow a 302 for a partial response, so it receives
`HX-Redirect` instead. That destination is taken from the absolute location's path
and query, which keeps the request's path base rather than dropping it as a
base-relative path would; a destination carrying a fragment keeps its absolute form,
because a relative reference with a fragment is not a well-formed URI and Htmxor's
own destination validator rejects it. A destination whose scheme htmx cannot act on
keeps the stock representation rather than failing the request. The awaited pending
work is inside the guarded region, as upstream has it, so a navigation that only
surfaces after an `await` is handled rather than escaping.

This repair is not .NET 11 only and is covered on both target frameworks by
`Issue213SubmitRedirectTests`: stock-parity for a synchronous and an awaited submit
navigation, the enhanced-navigation opaque redirection for an external destination,
the htmx representation, a preserved fragment, a non-HTTP destination, and body parity
with the stock host, which writes the body it had already rendered before the submit
rather than replacing it.

The initial-render navigation path is deliberately unchanged. It still returns the
bare stock redirect before write-back, which also discards Session values, so
unifying it needs its own protected behavior and evidence; #230 owns that, together
with the generated-action redirect, which is not a POST and therefore takes the same
unchanged path.

Synchronized **2026-09-15**, ASP.NET Core **v11.0.0-rc.1.26425.128**, commit
**c3325eeb6b47bc6383c127d4f4827dc9642a2b6e**. Exact monitored sources:

- [TempDataCascadingValueSupplier.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/TempData/TempDataCascadingValueSupplier.cs): the request-context dependency, explicitly watched with `api: none`.
- [TempDataProviderServiceCollectionExtensions.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/TempData/TempDataProviderServiceCollectionExtensions.cs): the dictionary accessor and the cascade registration shape, explicitly watched with `api: none`.
- [TempDataService.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/DependencyInjection/TempDataService.cs): the write-back dependency, explicitly watched with `api: none`.
- [EndpointHtmlRenderer.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs): request initialization order under the existing renderer watch.
- [RazorComponentEndpointInvoker.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs): post-render persistence order and submit navigation handling under the existing invoker watch.

The Issue213 hosted contract observes cookie issue and next-request availability,
read, keep and peek controls against stock, supplied-parameter reads isolated
between users, a synchronous component redirect in both representations, and a
rejected request that neither writes nor consumes. Streaming completion belongs to
#215. This slice does not establish the session-storage provider, alternative
provider deployments, an asynchronous redirect after streaming has started, or
browser cookie transport. Exact verification belongs to delivery receipts. The
existing package-retained ASP.NET Core MIT license covers adapted coordination.

## .NET 11 CacheView coordination

The [approved #219 decision](https://github.com/egil/Htmxor/issues/219#issuecomment-5686491999)
extends the isolated adapter to `CacheView`. Stock drives it from two places the
candidate replaces — `EndpointComponentState`, which supplies the streaming flag
and the tree-position key as component state is created, and
`EndpointHtmlRenderer.WriteComponentHtml`, which performs the capture. Without
both, a cached subtree is never stored and never reused, so every request
re-renders it.

Authorized dependencies, all in `Microsoft.AspNetCore.Components.Endpoints.dll` unless noted:

- internal `CacheView.RenderState` getter, to read the per-render coordination
  state the component produced.
- internal `CacheView.IsInStreamingContext` setter, so the framework can suppress
  caching inside a streaming subtree exactly as stock does.
- internal `CacheView.TreePositionKeyFactory` setter. The tree position always
  contributes to the computed key, even when `CacheKey` is set, so without it two
  sibling boundaries under one parent would collide. It is also where Htmxor adds the
  response representation, described below.
- public `CacheViewService.ThrowIfNestedInsideCapturingCacheView(TextWriter)`,
  `CacheViewService.TryBeginWrite(...)` and `CacheViewService.EndCapture(...)` on
  the internal service type, resolved through existing DI.
- public `CacheViewRenderState.IsCacheHit` getter on the internal state type.

The extended approval adds the descendant guard's own dependencies:

- public static `CacheViewService.IsCacheableComponent(Type, CacheVaryBy)`.
- on the internal `CacheViewTextWriter`: the `IsCapturing`, `IsValidationOnly` and
  `VaryBy` getters, `PauseCapture()`, `StartCapture()`, and
  `CreateLiveCachedComponent(Type, IComponentRenderMode, RenderFragmentCapture, ILogger)`.
- the internal `Microsoft.AspNetCore.Components.RenderFragmentCapture` constructor
  taking `RenderTreeFrame[]`.

`HtmxorEndpointCandidateCacheViewServices` validates each internal nongeneric type
and every member's name, accessibility, exact parameters and return type during
registration, caching only accessor metadata. The framework retains the cache
store, key derivation, serialization, invalidation, variation and the component's
own resolution; no cache implementation is copied and no private field is written.

Two upstream sources are mirrored rather than accessed, both with provenance and a
watch. `ComponentKeyHelper.FormatSerializableKey` is a pure shared function, and
`EndpointComponentState`'s tree-position key computation is reproduced in
`HtmxorEndpointCandidateCacheViewServices.ComputeTreePositionKey`, line for line, with
the sequence lookup it needs mirrored in `HtmxorEndpointCandidateRenderer.CacheView.cs`,
and the response representation is then appended to it.

That last part is deliberate and is not what stock does. Stock serves one
representation per URL; Htmxor serves an ordinary response and one or more htmx
responses from the same URL, and the framework's key has no dimension for the
difference. Sharing one entry served an ordinary body to an htmx request, and a named
fragment that the cached markup was stored around was never reconstructed, failing the
request. One bit — whether the request is an htmx one — therefore joins the position Htmxor
already supplies, through the same approved `TreePositionKeyFactory`. Keys are
consequently not equal to stock's for the same component tree; the derivation itself
stays the framework's. Only the ordinary representation is ever stored, because an htmx
request caches nothing, so the bit exists to keep an htmx request from being served an
ordinary body rather than to hold two entries.

Selected fragment names were tried in that value and removed. Selection is permitted
from ordinary lifecycle code, so the names are not reliably populated when the
framework asks for the key, and they added nothing: a boundary holding a named
fragment is ordinary content on an ordinary request and caches normally; selection is
honoured only in `RoutingMode.Direct`, which requires an htmx request, and those cache
nothing.

Two hosts hold separate stores, so no test can
observe cross-host key equality; what is observed is that two sibling boundaries under
one parent with no explicit key are disambiguated, which is what the tree position is
for. Stock's `GetComponentKey()` override for an `SSRRenderModeBoundary` parent is not
mirrored, and that case is outside this slice. The candidate reads the
parent's frames through the supported protected `GetCurrentRenderTreeFrames` seam
and takes the component key from the frame, rather than reaching for
`ComponentState.GetComponentKey`. The candidate also tracks inherited stream
rendering per component id, because stock propagates that from the logical parent
on `EndpointComponentState` and `CacheView` needs the inherited value.

Synchronized **2026-09-16**, ASP.NET Core **v11.0.0-rc.1.26425.128**, commit
**c3325eeb6b47bc6383c127d4f4827dc9642a2b6e**. Exact monitored sources:

- [CacheView.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/CacheView/CacheView.cs): the three component dependencies, watched with `api: none`.
- [CacheViewService.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/CacheView/CacheViewService.cs): the capture coordination and the descendant guard, watched with `api: none`.
- [CacheViewRenderState.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/CacheView/CacheViewRenderState.cs): the per-render state, watched with `api: none`.
- [Rendering/CacheViewTextWriter.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/Rendering/CacheViewTextWriter.cs): the capture writer, watched with `api: none`.
- [Shared/RenderFragmentCapture.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Shared/src/RenderFragmentCapture.cs): the captured parameter frames, watched with `api: none`.
- [EndpointComponentState.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/Rendering/EndpointComponentState.cs): the reimplemented tree-position key and streaming propagation.
- [ComponentKeyHelper.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Shared/src/ComponentKeyHelper.cs): the mirrored key formatting.
- [EndpointHtmlRenderer.Streaming.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs): the write-path branch, under the existing renderer watch.
- [Rendering/SSRRenderModeBoundary.cs](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/Rendering/SSRRenderModeBoundary.cs): its `[CacheBehavior(Rerender)]` attribute, which is why stock pauses at a render-mode boundary and never validates the
  prerendered subtree beneath it. `HtmxorEndpointCandidate.cs` mirrors that answer for its own boundary, so this is a
  second, separately watched dependency on the same upstream file: `mirrors` alongside the boundary type's `reimplements`.

### The descendant guard is required, not optional

An earlier revision of this adapter omitted stock's second `CacheView` block and
justified it as a path "reached only by cached interactive content". **That was
wrong, and it was a security defect.** Stock consults
`CacheViewService.IsCacheableComponent` for every component written during an active
capture, and the components that matter are ordinary static SSR:

- `AuthorizeViewCore` is `[CacheBehavior(CacheBehavior.Throw)]` with
  `[CacheCondition(CacheVaryBy.User)]`. Stock refuses to cache it. Without the guard
  Htmxor cached it, and a second principal was served the first principal's
  authorized markup.
- `AntiforgeryToken` is `[CacheBehavior(CacheBehavior.Rerender)]`. Without the guard
  its first value was frozen into the entry and replayed.

The [extended #219 decision](https://github.com/egil/Htmxor/issues/219#issuecomment-5694552511)
therefore authorizes `IsCacheableComponent`, the `CacheViewTextWriter` capture state
with `PauseCapture`/`StartCapture`, and `CreateLiveCachedComponent` with its
`RenderFragmentCapture`. A refused component now raises the framework's own
descriptive error; an excluded one is recorded so a later hit renders it live.
A boundary the framework declined to cache — disabled, not a GET, or inside a
streaming context — has no render state. Stock still calls `TryBeginWrite`, which
then supplies a validation-only writer whose `StartValidation` sets capturing, so the
guard keeps running and the boundary still refuses what stock refuses. The adapter
mirrors that rather than returning early on a null state, and
`A_disabled_boundary_still_refuses_content_stock_refuses` asserts the message equals
stock's.

Two compositions discard the capture instead, so the boundary stores nothing and the
subtree renders normally on every request. An `HtmxAsyncLoad` writes the current request
path into its placeholder, and no path reaches a cache key unless the application
declared `VaryByRoute`, so a page at two routes would otherwise serve the first request's
placeholder to the second. It is named concretely rather than through
`IConditionalRender`, because `HtmxLayoutComponentBase` implements that interface with a
constant and refusing the interface stopped every boundary beneath the documented layout
from caching. An interactive render-mode boundary is discarded because Htmxor's own
boundary does not expose the inner component type a live cached component would need.

A named `HtmxFragment` is **not** discarded. It was, while htmx responses were cached;
selection is honoured only in `RoutingMode.Direct`, which requires an htmx request, and
those cache nothing, so on an ordinary request the fragment is ordinary content.

A consumer component that is none of these but still varies by the request — one that
injects the scoped `HtmxContext` and reads the triggering or target element, say — is
captured and replayed, because no cache key describes what it read. No `HX-*` header
enters any key at all. This is newly reachable, since before this change
nothing was stored at all. `IsCacheableComponent` is consulted *before* the discard, so
stock's `[CacheBehavior]` opt-out applies to every component and is the documented
remedy. No command exercises that composition; it is filed as #235
rather than left as narration here.

A cached boundary **beneath** either kind is discarded too. Beneath an interactive
render-mode boundary it would store prerendered interactive content keyed more weakly
than stock keys it, since stock's `SSRRenderModeBoundary` component-key override is not
mirrored. Beneath an `HtmxAsyncLoad` the reason is the one above.

The two kinds then part company on whether the capture also pauses, because stock does.
Stock's `SSRRenderModeBoundary` carries `[CacheBehavior(Rerender)]`, so stock pauses at a
render-mode boundary and never validates the prerendered subtree beneath it; Htmxor's
boundary carries no such attribute and now mirrors that answer in `HtmxorEndpointCandidate`
— a second, separately watched dependency on that upstream file. An `HtmxAsyncLoad` is
marked and falls through instead, so the framework's refusals still run over what it holds:
pausing there hid the subtree from them, and abandoning without marking left a streaming
page throwing where stock renders it. All three were measured, and the two render-mode
cases at the top of `Issue219CacheInteractiveTests` are the tripwire for the pause.

Discarding alone is not sufficient, because it governs only what is written. The tree
position therefore also records whether the boundary stands beneath a request-varying
ancestor, so the two states cannot share an entry. The reachable case is a render-mode ancestor: a component's render mode can be decided
per request, so one position stands beneath an interactive boundary on one request and
not on the next, and the two states must not share an entry.

### Executed boundary

The Issue219 hosted contract pairs most cases against a stock host, so the framework
decides the outcome and Htmxor only has to match it. The kinds paired this way are a
miss then a hit across an application data change, configured variation keeping two
values isolated, cache-observation negative controls, expiry, suppression inside a
streaming subtree, refusal of content stock refuses, response-header equality, tree
position disambiguating siblings, and render-mode ancestry. This names the kinds, not
every case: the cases themselves are in the suite, and any count of them belongs to the
reconciliation below rather than to this sentence.

The cases listed below run against the candidate alone. The list is a fact about the
suite, not about this code, so re-derive it from the suite whenever the `Issue219` tests
change rather than editing it by hand. A case belongs here when no host it constructs is
built with the htmxor flag false; the flag usually reaches `AddHtmxor` through a shared
helper rather than the test body, and some call sites pass it positionally. Check the
derivation by reconciling it: these cases plus the paired cases must account for every
test that `dotnet test … --filter "FullyQualifiedName~Issue219"` discovers. This
enumeration went stale in four separate rounds, every time because that reconciliation was
performed by reading rather than by counting, so it is now executable.
`CacheViewCaseInventoryTests` asserts that every case named below still exists under that
name, and that this list plus the paired count stated here accounts for every discovered
case. It deliberately does not classify a case as paired — that needs the host each case
builds, reached through shared helpers and sometimes positional arguments — so the count
below is still a human judgement, but one that can no longer drift unnoticed from the
suite's size.

Paired cases in the same suite: 21.

- `A_boundary_holding_an_interactive_render_mode_boundary_stores_nothing`
- `A_boundary_inside_an_async_loads_loading_content_stores_nothing`
- `A_cache_view_beneath_an_interactive_render_mode_boundary_stores_nothing`
- `A_keyed_boundary_is_not_served_another_keys_entry`
- `A_nested_boundary_beneath_an_async_load_raises_the_frameworks_refusal`
- `A_sibling_of_a_boundary_that_stores_nothing_still_caches`
- `An_async_load_placeholder_carries_the_path_of_the_request_that_asked_for_it`
- `An_htmx_request_does_not_reuse_an_entry_stored_for_the_ordinary_representation`
- `An_htmx_request_is_not_cached_and_is_not_served_an_ordinary_entry`
- `An_htmx_response_header_set_during_render_reaches_every_request`
- `Cached_subtree_runs_its_component_once_and_is_reused_afterwards`

Several of these are candidate-only by necessity rather than by choice: a stock host
cannot construct a composition containing `HtmxAsyncLoad` at all, so there is no arm to
pair against.

Every case above was confirmed to redden when its guard is disabled, except
`Cached_subtree_runs_its_component_once_and_is_reused_afterwards`, which is an
observation probe with no guard to disable, and
`An_htmx_response_header_set_during_render_reaches_every_request`, which is retained as a
tripwire for #236: it holds today because an htmx request is not cached at all, not
because anything guards the header. All inversion results are recorded in the
verification evidence attached to the pull request; receipts live under `artifacts/`,
which is ignored, so they do not ship with a clone and are cited through the pull request
rather than by path.

Three production branches have no case that reddens them, recorded rather than implied.
The **physical-versus-logical ancestor walk** is unobservable: two rounds of proposed
distinguishing compositions were built and neither divided the chains, and for a component
passed as a `RenderFragment` parameter the two parents are the same object. The **refusal
ordering** cannot change an outcome, because the cacheable decision is taken before the
kind is marked and neither kind carries `[CacheBehavior(Throw)]`. The **`captureAbandoned`
save and restore** has likewise resisted two proposed compositions. All three become
observable again if #236 restores the request-varying kinds, and #236 will have to
re-derive them.

The Htmxor-specific pause was removed rather than left unobservable. Two `PauseCapture`
call sites remain and both mirror stock: the one that mirrors stock's own second CacheView
block, and the render-mode boundary's own branch added to match `[CacheBehavior(Rerender)]`.
Both are measured; neither is an Htmxor-specific policy.

Stock and Htmxor still express the exclusion through two independent mechanisms — an
attribute on the component against a predicate in the renderer — so they are kept in step
by hand rather than by construction. That is why three separate repairs in this file each
fixed one composition and broke an adjacent one. Converging them is **#237**; it is not a
condition of this slice, whose mirror is measured by the two render-mode cases at the top
of `Issue219CacheInteractiveTests`.

`VaryByRoute`, `VaryByCookie` and `VaryByCulture` are framework-owned, unchanged, and
exercised by no command. So are `VaryBy` and `VaryByHeader`: no Htmxor code reads either
at this revision. `VaryByHeader` briefly opened htmx caching for a boundary and `VaryBy`
briefly did too, which was a defect — stock appends `VaryBy` to its key as a literal that
names no request dimension — and both were withdrawn with htmx caching itself. Distributed cache
deployment, cached form content and performance are unclaimed. Streaming boundary
markers for a component that did not itself opt into streaming already differ from
stock; that is pre-existing and owned by #192. The existing package-retained ASP.NET
Core MIT license covers adapted coordination.

## Installed form-service access

The form-service private dependencies come from `Microsoft.AspNetCore.Components.Endpoints.dll`,
resolved from `typeof(IRazorComponentEndpointInvoker).Assembly`. The baseline is
ASP.NET Core **v10.0.11**, commit **a5383385245bdacc20ec19f30e46090a8154d8da**,
synchronized **2026-09-06**. CLR assembly version is not a semantic compatibility
test; a new framework release needs upstream review and renewed paired evidence.

| Declaring type | Exact instance member |
| --- | --- |
| `Microsoft.AspNetCore.Components.Endpoints.HttpContextFormDataProvider` | Public `void SetFormData(string, IReadOnlyDictionary<string, Microsoft.Extensions.Primitives.StringValues>, Microsoft.AspNetCore.Http.IFormFileCollection)` |
| `Microsoft.AspNetCore.Components.Endpoints.Forms.EndpointAntiforgeryStateProvider` | Internal `void SetRequestContext(Microsoft.AspNetCore.Http.HttpContext)` |
| Same antiforgery type | Internal `void DisableTokenGeneration()` |
| `Microsoft.AspNetCore.Components.Endpoints.ConfiguredRenderModesMetadata` | Public property getter `Microsoft.AspNetCore.Components.IComponentRenderMode[] ConfiguredRenderModes { get; }` |

`HtmxorEndpointCandidateFormServices` validates these declared, nongeneric,
instance signatures, visibility, getter identity, and antiforgery base type during
candidate registration, before registration changes or requests. Incompatibility
throws with the installed assembly identity and expected baseline/dependency.
It caches only assembly/type/member metadata. Invocation preserves the underlying
exception, without falling back to another renderer or retrying a callback.

Every request resolves its existing scoped services from `HttpContext.RequestServices`.
No service instance is cached by the adapter, no new scope is created, and no
private field is written. The existing form provider receives the actual handler,
a read-only view of the parsed form entries, and its actual `IFormFileCollection`.
The installed mapper, application options, converters, and service customizations
remain authoritative. The antiforgery provider is initialized/disabled when it is
the installed endpoint provider (including subclasses); replacement public
`AntiforgeryStateProvider` implementations retain their own behavior.

## Ordering and supported renderer seams

1. Set ordinary response headers and use the effective endpoint metadata. For a
   real POST outside exception handling, reject a parseable unsupported content
   type, reuse an existing `IAntiforgeryValidationFeature`, or validate through the
   real `IAntiforgery` service when effective metadata requires it. Validation may
   parse HTTP form data; no component binding or execution has happened yet.
2. Read the form after validation. Multiple `_handler` values reject before
   components. Missing and empty handlers retain their different stock values
   until named-submit validation. Error detail policy follows stock: request
   validation uses Development or DetailedErrors; submit errors use Development.
3. Initialize existing navigation/authentication services, then scoped form and
   antiforgery services, then the candidate's existing endpoint route state.
   SupplyParameterFromForm, EditForm, Input components and validators perform their
   own framework-owned work during rendering.
4. Render the endpoint through `StaticHtmlRenderer.BeginRenderingComponent` and
   await quiescence. Track `RenderBatch.NamedEventChanges` through protected
   `UpdateDisplayAsync`, removing before adding so replacement locations remain
   correct. `TryCreateScopeQualifiedEventName` supplies framework scope rules.
5. Resolve the named submit against the candidate's current component tree.
   Ambiguity reports paths using protected `GetComponentState` and public
   `ComponentState.ParentComponentState`; event IDs come from protected
   `GetCurrentRenderTreeFrames`. Public `DispatchEventAsync` invokes the request's
   actual component callback and waits for full quiescence. The base static
   display behavior remains responsible for excluding OnAfterRender.
6. Before completed non-streaming HTML output, disable token generation only if
   the effective configured-render-modes metadata has an empty array. The last
   assignable metadata entry is effective; absent metadata does not mean empty.
   Initialization never eagerly generates tokens. Stock AntiforgeryToken/provider
   code retains token creation, caching, cookie and storage timing.

Interactive execution, other verbs, fragment selection, and generated actions
remain separate obligations. The configured-mode supplemental test observes late
public provider availability only; it does not claim full configured-mode
response or persistence parity.

## Exact upstream provenance and monitoring inventory

Each source below is pinned by both release tag and immutable commit. Local owners
are under `src/Htmxor/Endpoints/`. Source markers record the relationships for the
#184 upstream monitor. Private invocation is `private-accesses`; copied endpoint
coordination is `reimplements`, and the small error-detail policy is `mirrors`.
The #184 canonical inventory must contain these exact relationships before active
invoker acceptance; public activation does not waive monitoring.

| Upstream source (tag / immutable commit) | Local owner and relationship |
| --- | --- |
| [RazorComponentEndpointInvoker.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs) | `HtmxorEndpointCandidate.cs`, `HtmxorEndpointCandidateFormRequest.cs`, `HtmxorEndpointCandidateFormServices.cs`: reimplements |
| [EndpointHtmlRenderer.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs) | `HtmxorEndpointCandidate.cs`, `HtmxorEndpointCandidateFormServices.cs`: reimplements |
| [EndpointHtmlRenderer.EventDispatch.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.EventDispatch.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.EventDispatch.cs) | `HtmxorEndpointCandidateRenderer.NamedSubmit.cs`: reimplements (named-submit portions only) |
| [EndpointHtmlRenderer.Streaming.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs) | `HtmxorEndpointCandidateFormRequest.cs`: mirrors (ShouldShowDetailedErrors policy only); `HtmxorEndpointCandidateRenderer.Streaming.cs`: reimplements |
| [HttpContextFormDataProvider.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/FormMapping/HttpContextFormDataProvider.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/FormMapping/HttpContextFormDataProvider.cs) | `HtmxorEndpointCandidateFormServices.cs`: private-accesses |
| [EndpointAntiforgeryStateProvider.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Forms/EndpointAntiforgeryStateProvider.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/Forms/EndpointAntiforgeryStateProvider.cs) | `HtmxorEndpointCandidateFormServices.cs`: private-accesses |
| [ConfiguredRenderModesMetadata.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Builder/ConfiguredRenderModesMetadata.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/Builder/ConfiguredRenderModesMetadata.cs) | `HtmxorEndpointCandidateFormServices.cs`: private-accesses |
| [RazorComponentsServiceCollectionExtensions.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceCollectionExtensions.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceCollectionExtensions.cs) | `HtmxorEndpointCandidate.cs`: reimplements (existing cascading HttpContext selection) |
| [RazorComponentsServiceOptions.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceOptions.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceOptions.cs) | `HtmxorEndpointCandidateFormServices.cs`: private-accesses (`JavaScriptInitializers`) |
| [ResourceCollectionUrlMetadata.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Builder/ResourceCollectionUrlMetadata.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/Builder/ResourceCollectionUrlMetadata.cs) | `HtmxorEndpointCandidateFormServices.cs`: private-accesses |
| [ResourceCollectionProvider.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Shared/src/ResourceCollectionProvider.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Shared/src/ResourceCollectionProvider.cs) | `HtmxorEndpointCandidateFormServices.cs`: private-accesses |
| [EndpointHtmlRenderer.PrerenderingState.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.PrerenderingState.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.PrerenderingState.cs) | `HtmxorEndpointCandidate.cs`, `HtmxorEndpointCandidateStateStore.cs`: reimplements |
| [SSRRenderModeBoundary.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Rendering/SSRRenderModeBoundary.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/Rendering/SSRRenderModeBoundary.cs) | `HtmxorEndpointCandidateRenderModeBoundary.cs`: reimplements |
| [SSRRenderModeBoundary.cs (v11.0.0-rc.1.26425.128)](https://github.com/dotnet/aspnetcore/blob/v11.0.0-rc.1.26425.128/src/Components/Endpoints/src/Rendering/SSRRenderModeBoundary.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/Rendering/SSRRenderModeBoundary.cs) | `HtmxorEndpointCandidate.cs`: mirrors (`[CacheBehavior(Rerender)]`) |
| [ComponentMarker.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Shared/Components/ComponentMarker.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Shared/Components/ComponentMarker.cs) | `HtmxorEndpointCandidateRenderModeBoundary.cs`: reimplements |
| [ServerComponentSerializer.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Shared/Components/ServerComponentSerializer.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Shared/Components/ServerComponentSerializer.cs) | `HtmxorEndpointCandidateRenderModeBoundary.cs`: reimplements |
| [PrerenderComponentApplicationStore.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Shared/Components/PrerenderComponentApplicationStore.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Shared/Components/PrerenderComponentApplicationStore.cs) | `HtmxorEndpointCandidateStateStore.cs`: reimplements |
| [ProtectedPrerenderComponentApplicationStore.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Shared/Components/ProtectedPrerenderComponentApplicationStore.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Shared/Components/ProtectedPrerenderComponentApplicationStore.cs) | `HtmxorEndpointCandidateStateStore.cs`: reimplements |
| [WebAssemblyComponentSerializer.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/DependencyInjection/WebAssemblyComponentSerializer.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/DependencyInjection/WebAssemblyComponentSerializer.cs) | `HtmxorEndpointCandidateRenderModeBoundary.cs`: reimplements |
| [WebAssemblySettingsEmitter.cs (v10.0.11)](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/DependencyInjection/WebAssemblySettingsEmitter.cs) / [commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/DependencyInjection/WebAssemblySettingsEmitter.cs) | `HtmxorEndpointCandidate.cs`: reimplements |

The candidate also subclasses `StaticHtmlRenderer`, implements
`IRazorComponentEndpointInvoker`, and consumes supported `Renderer`/`ComponentState`
seams; retain those existing #188 API relationships when integrating the inventory.
A watched change requires review and fresh paired evidence before adoption;
signature compatibility alone never establishes semantic compatibility.

The upstream [MIT license at v10.0.11](https://github.com/dotnet/aspnetcore/blob/v10.0.11/LICENSE.txt)
and [immutable commit](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/LICENSE.txt)
is retained verbatim at
[ASP.NET-Core-LICENSE.txt](../../src/Htmxor/Endpoints/ASP.NET-Core-LICENSE.txt).
The project includes it at `licenses/ASP.NET-Core-LICENSE.txt` in packages.
License-file inclusion alone is not package-consumer compatibility evidence.
