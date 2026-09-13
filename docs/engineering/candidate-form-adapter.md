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

## Installed-service access

All private dependencies come from `Microsoft.AspNetCore.Components.Endpoints.dll`,
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
