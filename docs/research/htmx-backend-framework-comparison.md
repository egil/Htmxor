# HTMX backend framework comparison

Research date: 2026-08-26 UTC

This report compares the developer-facing interfaces of representative backend frameworks and libraries that explicitly support HTMX. It uses project documentation, source repositories, release pages, and package registries maintained by the projects or their publishers. Version numbers are a dated snapshot, not a promise about future compatibility.

The comparison focuses on the question that matters for Htmxor: can a developer keep routing, request handling, full-page rendering, and fragment rendering local to a Razor component without writing a parallel controller, Minimal API endpoint, or handler?

## Executive finding

### Observed facts

Most surveyed alternatives leave the HTTP route in application-authored controller, handler, URL configuration, or endpoint code:

- Htmx.Net and axum-htmx add typed request and response protocol helpers to routes the application already owns.
- django-htmx and htmx-spring-boot make full-versus-fragment selection convenient, but the choice remains in a view or controller.
- FastHX decorates an existing FastAPI route.
- RazorX and Rizzy render Razor components through explicit Minimal API or MVC routes.
- templ can keep named fragments in one component file, but a Go handler owns the route and chooses the fragment.

holm is the important exception. It discovers page and layout modules from the Python package tree, creates routes without manual FastAPI registration, and can colocate generated POST submit handlers and decorated HTML actions with a page. Actions return unwrapped components suited to HTMX fragment requests. A page can also inspect `HX-Request` and return `without_layout(content)` to serve a full page and an HTMX fragment from the same URL. [holm file-system routing](https://volfpeter.github.io/holm/file-system-based-routing/), [application components](https://volfpeter.github.io/holm/application-components/), and [`without_layout`](https://volfpeter.github.io/holm/utilities/).

Htmxor's current route discovery still has a distinct shape. A Razor `@page` route is also registered as a direct Htmxor route, while `[HtmxRoute]` can give a component a direct route without making it a normal Blazor page. Inline `HtmxFragment` components select output within the same routable component. holm's same-URL alternative is an application-authored header branch over a locally constructed `content` value; it does not provide declarative normal-only, HTMX-only, and dual modes or Htmxor-style named/predicate inline selection and multiple-fragment composition. Htmxor's behaviors are visible in the [component route catalog](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Builder/ComponentInfo.cs), [route attribute](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/HtmxRouteAttribute.cs), and [fragment component](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Components/HtmxFragment.cs).

No surveyed alternative provides the proposed combination below. Htmxor today provides most of it, but v1 would still need to add normal-only reachability and harden action binding:

1. a UI component or page owns or conventionally derives its route without separate endpoint registration;
2. normal-only, HTMX-only, and dual reachability are explicit declarative choices;
3. the same routable component can return its full output, one inline fragment, or several inline and out-of-band fragments;
4. HTTP actions remain local to that component and are bound to explicit verbs.

### Assessment

The proposed v1 should preserve and complete that interface. It should not move Htmxor toward controller-owned or Minimal API-owned feature endpoints. The part to replace is the implementation beneath the interface: private Blazor service replacement, reflection over internal types, and copied renderer behavior. The comparison supports a public-API and generated-registration architecture, not a different authoring model.

The most useful outside ideas are complementary:

- Htmx.Net's small typed protocol layer and explicit cache support;
- Rizzy's use of the public Razor component result path;
- Django 6 and Thymeleaf's named fragments colocated with a full template;
- FastHX's explicit reachability modes and selectable error renderers;
- holm's automatic UI route discovery, local submit handlers, and verb-specific HTML actions;
- Spring's CSRF and authentication-flow integration;
- axum-htmx's automatic `Vary` handling and warning that HTMX headers are not authorization;
- templ's compile-time component model and explicit fragment execution semantics.

## Version snapshot

The stable browser-library reference for this review is htmx 2.0.10, published in the [official npm package](https://www.npmjs.com/package/htmx.org/v/2.0.10). This is a dated research and test baseline, not a proposed Htmxor dependency. htmx 4 remained prerelease software at the research date, so a backend library's verified behavior with an htmx 4 beta is recorded separately from stable htmx 2 evidence.

| Project | Reviewed version or revision | Evidence |
| --- | --- | --- |
| Htmxor | commit `d8e09e4da17ab4c74fbea95d8e995137785c8395`, 2024-09-09; targets .NET 8 and embeds htmx 1.9.12 | [repository revision](https://github.com/egil/Htmxor/tree/d8e09e4da17ab4c74fbea95d8e995137785c8395), [project file](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Htmxor.csproj), [embedded asset](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/wwwroot/htmx/htmx.min.js) |
| Htmx.Net / Htmx.TagHelpers | 1.12.0, 2026-03-02; documents htmx 1.x and 2.x | [NuGet](https://www.nuget.org/packages/Htmx/1.12.0), [repository](https://github.com/khalidabuhakmeh/Htmx.Net) |
| Rizzy | 5.4.0, 2026-06-09; .NET 10 | [NuGet](https://www.nuget.org/packages/Rizzy/5.4.0), [repository](https://github.com/JalexSocial/Rizzy) |
| RazorX | repository template marked `1.0.0-beta`; requires .NET 9 or later | [repository and README](https://github.com/ranzlee/razorx) |
| Giraffe.ViewEngine.Htmx | 2.0.10, 2026-07-03; htmx 2.0.10 and .NET 8 through 10 | [NuGet](https://www.nuget.org/packages/Giraffe.ViewEngine.Htmx/2.0.10), [repository](https://git.bitbadger.solutions/bit-badger/Giraffe.Htmx) |
| django-htmx | 1.29.0, 2026-08-05; Django 5.2 through 6.1 | [PyPI](https://pypi.org/project/django-htmx/1.29.0/) |
| Django template partials | Django 6.0 core feature | [Django 6.0 release notes](https://docs.djangoproject.com/en/6.0/releases/6.0/), [template language reference](https://docs.djangoproject.com/en/6.0/ref/templates/language/#template-partials) |
| FastHX | 3.2.2, 2026-07-23 | [PyPI](https://pypi.org/project/fasthx/3.2.2/), [repository](https://github.com/volfpeter/fasthx) |
| holm | 0.10.0, 2026-07-23 | [PyPI](https://pypi.org/project/holm/0.10.0/), [documentation](https://volfpeter.github.io/holm/) |
| htmx-spring-boot | 5.1.0; compatibility table names Spring Boot 4.0.3 and Java 17 | [Maven Central](https://central.sonatype.com/artifact/io.github.wimdeblauwe/htmx-spring-boot/5.1.0), [compatibility matrix](https://github.com/wimdeblauwe/htmx-spring-boot#compatibility) |
| axum-htmx | 0.8.1, 2025-06-05 | [docs.rs release page](https://docs.rs/crate/axum-htmx/0.8.1) |
| templ | 0.3.1020, 2026-05-10 | [GitHub release](https://github.com/a-h/templ/releases/tag/v0.3.1020) |

Rizzy and Giraffe.ViewEngine.Htmx are included because they expose two particularly relevant .NET patterns: public Razor component rendering and server-side fragment selection from a colocated template. Biff and axum-routing-htmx appear later as brief secondary examples.

## Comparison criteria

The report uses these terms consistently:

- **Normal-only** means a route is intentionally unavailable as an HTMX representation.
- **HTMX-only** means a non-HTMX request does not receive the endpoint's ordinary representation. This is a representation constraint, not an authorization boundary.
- **Both** means one logical URL can serve a normal full page and an HTMX-specific component or fragment response.
- **Inline fragment** means a selectable region is declared in the same source template or component as its full representation.
- **Endpoint boilerplate** means application code must repeat route, verb, handler, or component selection outside the UI component.
- **Maintenance coupling** describes how much host framework machinery the library replaces or owns. It is not a feature score.

Ease-of-use and maintainability comments are qualitative assessments based on the documented authoring surface and implementation seams. They are not benchmark results.

## Same feature at the call site

Assume a feature needs `/users` as a full page and a user-list fragment for an HTMX refresh. The core authoring difference is visible before comparing the larger capability sets:

| System | What the feature author writes | Separate routing/selection boundary |
| --- | --- | --- |
| **Htmxor today** | One `.razor` file with `@page "/users"`, `<HtmxFragment>...</HtmxFragment>`, HTMX attributes, and optional `@onpost` or other local handlers | No per-feature endpoint. Htmxor discovers the page route and selects normal or direct rendering |
| **holm** | `users/page.py` with `def page(request)`, optional `handle_submit`, and colocated `@action.get/post/...` functions | No FastAPI registration. A fragment can use a separate action or a manual `HX-Request` plus `without_layout` branch in `page`; there is no declarative inline match/selection model |
| **RazorX or Rizzy** | A `.razor` component plus a C# handler/controller that calls `MapGet(... RenderComponent<Users>())`, `View<Users>()`, or `PartialView<UserList>()` | The C# endpoint owns route, verb, security policy, and component selection |
| **Django 6** | A template with `{% partialdef user-list %}` plus URLconf and a Python view that selects `users.html` or `users.html#user-list` | The view owns the HTMX branch and template-name selection |
| **htmx-spring-boot** | A Thymeleaf template fragment plus `@GetMapping("/users")`, often with `@HxRequest`, returning `"users :: list"` | The controller owns route conditions and fragment selection |
| **templ** | A `.templ` file with `@templ.Fragment("user-list")` plus a Go handler using `templ.WithFragments("user-list")` | The handler owns route and fragment selection; the containing template still executes |

This is why a thin library can be easy to maintain while still imposing more work on each consumer. holm removes explicit route registration, but Htmxor can go further by retaining the full page, local action, and inline selected output in one Razor component contract.

## Developer-facing routing and rendering matrix

The following table records documented behavior. An application's ability to hand-code a branch is not treated as a first-class library feature.

| System | Where the developer declares the route | Normal-only, HTMX-only, and both | Full versus fragment response | Inline and multiple fragments |
| --- | --- | --- | --- | --- |
| **Htmxor today** | The `.razor` component through `@page` and/or `[HtmxRoute]`; one application-level registration maps discovered components | `@page` currently means both; `[HtmxRoute]` without `@page` supplies HTMX-only reachability; no first-class normal-only opt-out | Routing mode automatically chooses the normal Blazor root path or direct component path | `HtmxFragment` and `HtmxFragmentElement` select inline output; layouts and ordinary HTMX OOB markup can return additional swaps |
| **Htmx.Net / TagHelpers** | Razor Page handler, MVC action, or named route remains application code | All modes are possible through application routing and `Request.IsHtmx()` branches; the library does not declare a mode | Developer returns `Page()` or `Partial(...)` | Razor partials remain separate views unless the application supplies another convention; content and OOB composition are app-owned |
| **Rizzy** | MVC controller or Minimal API route names the Razor component | `[HtmxRequest]` can constrain an action to HTMX; ordinary routes remain normal; dual behavior uses Rizzy result/layout conventions | `View<TComponent>()`, `PartialView<TComponent>()`, `RenderFragment`, and `HtmxLayout` | Separate Razor components or render fragments; a service supports multiple OOB swaps |
| **RazorX** | `IRequestHandler.MapRoutes` explicitly maps each Minimal API route and handler | The handler and endpoint policy decide the mode | Handler returns `RenderComponent<T>()`; mutations map another route and render a leaf component | Component decomposition supplies fragments; route-to-component choice is in C# handler code |
| **django-htmx + Django 6** | Django URL configuration and Python view | Normal is an ordinary view; both requires a `request.htmx` branch; HTMX-only requires an app guard | View chooses `page.html` or `page.html#partial` | Django 6 `{% partialdef %}` keeps named partials inline; the view selects one named partial. Multiple/OOB output is template/application markup |
| **FastHX** | Existing FastAPI route plus `@jinja.hx`, `@htmy.hx`, or `page` decorator | `page` always renders HTML; `hx(..., no_data=True)` is HTMX-only; ordinary `hx` renders HTML for HTMX and leaves the non-HTMX route data unchanged, often JSON | Rendering decorator and optional component selector choose the renderer | Selector chooses a template/component, including an error component. Inline sub-selection depends on the renderer rather than FastHX itself |
| **holm** | File-system conventions discover `page.py`; `handle_submit` adds POST on the same URL; `@action.get/post/...` creates relative HTML routes without FastAPI registration | No first-class normal-only or HTMX-only guard. A page can manually branch on `HX-Request`; generated actions remain ordinary HTTP endpoints | Pages receive automatic layouts; a page can return `without_layout(content)` for HTMX; actions are unwrapped by default and can opt into layouts | Returned htmy components can be fragments, but there is no named inline selector or first-class multiple/OOB result model |
| **htmx-spring-boot** | Spring MVC controller method with `@GetMapping` and optionally `@HxRequest` | Ordinary mapping is normal; `@HxRequest` is HTMX-conditioned; separate conditioned and ordinary handlers or a branch can serve both | Controller returns a view, `view :: fragment`, a redirect/refresh view, or a multi-fragment result | Thymeleaf markup selectors keep fragments in a template. Spring `FragmentsRendering` returns several fragments, including OOB responses |
| **axum-htmx** | Explicit Axum router and Rust handler | Ordinary routes are normal; `HxRequestGuardLayer` can require HTMX; both requires app branching | No rendering abstraction; handler selects its HTML/template result | No fragment model; application or template engine owns all content selection |
| **templ** | Explicit Go `http.Handler` or framework route | Application handler decides all modes | Handler renders a full component, a leaf component, or named fragments; the official HTMX example also demonstrates returning a full page and letting client `hx-select` extract one element | `@templ.Fragment("name")` and `templ.WithFragments(...)` select one or more named inline fragments, but the containing template still executes |
| **Giraffe.ViewEngine.Htmx** | Explicit Giraffe route and F# handler | Application handler decides all modes | Handler can render the full view or find a fragment by element `id` | One view can contain the named element, avoiding a separate partial file; route and selection remain in the handler |

Sources for the table: [Htmxor routing and fragments](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/docs/index.md#routing), [Htmx.Net README](https://github.com/khalidabuhakmeh/Htmx.Net/blob/main/Readme.md), [Rizzy endpoint mapping](https://jalexsocial.github.io/rizzy.docs/docs/framework/minimalapi/mapping-endpoints/), [Rizzy OOB swapping](https://jalexsocial.github.io/rizzy.docs/docs/htmx/out-of-band-swapping/), [RazorX request cycle](https://github.com/ranzlee/razorx#requestresponse-cycle), [django-htmx partial rendering](https://django-htmx.readthedocs.io/en/latest/tips.html#partial-rendering), [FastHX Jinja API](https://volfpeter.github.io/fasthx/api/jinja/), [FastHX component selectors](https://volfpeter.github.io/fasthx/api/component_selectors/), [holm application components](https://volfpeter.github.io/holm/application-components/), [holm HTMX actions](https://volfpeter.github.io/holm/guides/actions-with-htmx/), [holm `without_layout`](https://volfpeter.github.io/holm/utilities/), [htmx-spring-boot README](https://github.com/wimdeblauwe/htmx-spring-boot), [axum-htmx API](https://docs.rs/axum-htmx/latest/axum_htmx/), [templ fragments](https://templ.guide/syntax-and-usage/fragments/), and [Giraffe fragment rendering](https://bitbadger.solutions/blog/2022/fragment-rendering-in-giraffe-view-engine.html).

## Protocol, security, caching, and assets matrix

| System | Request and response helpers | Forms, CSRF, and authentication | Cache variation | Browser asset and streaming support |
| --- | --- | --- | --- | --- |
| **Htmxor today** | `HtmxContext`, typed request values, and fluent `HtmxResponse` headers; component-local `@onget`, `@onpost`, and related events | Uses ASP.NET antiforgery metadata and `UseHtmxAntiforgery`; current endpoint/action binding needs hardening because route defaults and rendered-event dispatch are broader than a stable v1 should allow | No first-class automatic `Vary` policy in the reviewed source | Embeds htmx 1.9.12 and emits it from `HtmxHeadOutlet`; current design is coupled to a fixed, outdated asset |
| **Htmx.Net / TagHelpers** | `Request.IsHtmx()`, typed header access, and fluent response-header builders | Opt-in antiforgery meta tag plus mapped script adds the token to non-GET requests; ASP.NET auth and page/model binding remain authoritative | `WithVary()` explicitly adds `Vary: HX-Request` | Caller owns the htmx browser asset; no rendering or streaming layer |
| **Rizzy** | HTMX request constraints, response abstractions, layouts, OOB swaps, and component results | MVC actions can use standard authorization and antiforgery attributes. Minimal API routes still need endpoint-owned policies; rendering a component does not add them automatically | `HtmxLayout` adds `Vary: HX-Request` | Client package currently references an htmx 4 beta; a streaming extension exists |
| **RazorX** | Component response extensions and trigger builders | Public endpoint filters apply antiforgery, authorization, and validation policies | Application/endpoint policy | Application owns browser integration; rendering uses public ASP.NET component APIs |
| **django-htmx** | `request.htmx`; redirect, location, refresh, stop-polling responses; push, replace, reswap, retarget, reselect, and trigger modifiers | Django forms, auth, and CSRF remain authoritative. Official guidance places `x-csrftoken` in ancestor `hx-headers` | Official tips require `Vary: HX-Request` when the response branches | `{% htmx_script %}` can serve stable htmx 2.0.10 or opt-in htmx 4 beta 6 with matched extensions and a CSP nonce |
| **FastHX** | Rendering decorators and request-aware component/error selectors; fewer typed HX response helpers than Spring or Htmx.Net | FastAPI dependencies, forms, auth, and app CSRF remain authoritative; `no_data=True` is not a security boundary | Application policy | Renderer-owned; HTMY integration supports async streaming when the renderer supports it |
| **holm** | Page, submit-handler, and action conventions render through FastHX/htmy; standard FastAPI dependencies remain available | htmy escapes output by default. holm recommends secure cookie settings and avoiding unsafe GETs, but leaves CSRF-token implementation to the application | No automatic HTMX cache variation documented in the reviewed guides | Application includes htmx; holm itself has no JavaScript dependency. Rendering inherits FastHX/htmy behavior |
| **htmx-spring-boot** | Injectable `HtmxRequest`/`HtmxResponse`; response annotations; redirect, location, refresh, reselect, reswap, retarget, history, and trigger APIs | Thymeleaf dialect adds Spring Security CSRF to unsafe `hx:*` requests, including non-form elements. Dedicated auth success, failure, logout, entry-point, and access-denied adapters return suitable HX flows | Application/Spring cache policy; request conditions are explicit | Application owns htmx inclusion; library supplies server modules and Thymeleaf dialect rather than a pinned client |
| **axum-htmx** | Typed extractors for request headers, typed response parts for standard HX headers, JSON event support, and HTMX route guard | Axum/Tower application owns forms, CSRF, and auth. Documentation explicitly warns the request guard is not authorization because headers are forgeable | Optional `AutoVaryLayer` adds the headers implied by extractors | No asset, template, or streaming ownership |
| **templ** | HTMX protocol remains ordinary Go headers; `templ.Handler` controls rendering behavior | App owns authentication and CSRF. Project docs point to Go's cross-origin protection; generated templates provide contextual escaping, typed script attributes, and URL sanitization | Application policy | User downloads/serves htmx. Handler supports normal buffered and streaming rendering; fragment filtering does not skip execution of the containing template |
| **Giraffe.ViewEngine.Htmx** | Typed F# attributes and request/response helpers | Existing Giraffe/ASP.NET middleware owns security | Application policy | Local and CDN htmx nodes are available and release version tracks htmx 2.0.10 |

Sources for the table: [Htmxor response API](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Http/HtmxResponse.cs), [Htmxor antiforgery endpoint metadata](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Builder/ComponentEndpointDataSource.cs), [Htmx.Net antiforgery and response helpers](https://github.com/khalidabuhakmeh/Htmx.Net/blob/main/Readme.md), [Rizzy repository](https://github.com/JalexSocial/Rizzy), [django-htmx installation and CSRF](https://django-htmx.readthedocs.io/en/stable/installation.html), [django-htmx response helpers](https://django-htmx.readthedocs.io/en/latest/http.html), [django-htmx template assets](https://django-htmx.readthedocs.io/en/latest/template_tags.html), [FastHX HTMY API](https://volfpeter.github.io/fasthx/api/htmy/), [holm security](https://volfpeter.github.io/holm/security/), [htmx-spring-boot security integration](https://github.com/wimdeblauwe/htmx-spring-boot#spring-security), [axum-htmx API](https://docs.rs/axum-htmx/latest/axum_htmx/), [templ HTMX guide](https://templ.guide/server-side-rendering/htmx/), [templ security guide](https://templ.guide/security/injection-attacks/), and [Giraffe package](https://www.nuget.org/packages/Giraffe.ViewEngine.Htmx/2.0.10).

## Detailed interface comparison

### Htmxor baseline

**Observed interface.** A user performs one application-level registration with `AddHtmx`, `UseHtmxAntiforgery`, and `AddHtmxorComponentEndpoints`, then works in `.razor` components. A component's normal `@page` route is copied into the Htmxor route set. An explicit `[HtmxRoute]` can add route, HTTP method, current URL, target, and trigger constraints. The default `HtmxRouteAttribute.Methods` currently contains GET, POST, PUT, PATCH, and DELETE. [Startup example](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/samples/MinimalHtmxorApp/Program.cs), [component route discovery](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Builder/ComponentInfo.cs), and [route constraints](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/HtmxRouteAttribute.cs). Component-local `@onget`, `@onpost`, and related handlers currently receive a generated hash in rendered markup and are selected from the supplied event-handler id on the later request. [Rendered event registration](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Rendering/HtmxorRenderer.HtmlWriting.cs) and [request dispatch](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Rendering/HtmxorRenderer.HtmxorEventDispatch.cs).

Normal requests travel through the root component and Blazor router. Direct requests render the matching component, with an optional `HtmxLayout`. `HtmxFragment` evaluates a request predicate and conditionally emits its child content. `HtmxFragmentElement` wraps selected content in a targetable element. Page title, response headers, layouts, and OOB layouts are supported. [Routing modes](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/docs/index.md#routing), [fragment source](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Components/HtmxFragment.cs), and [OOB layout example](https://github.com/egil/Htmxor/tree/d8e09e4da17ab4c74fbea95d8e995137785c8395/samples/HtmxorExamples/Components/Pages/Examples/OutOfBandOutlets).

The implementation replaces or removes Blazor services, implements a custom endpoint invoker, copies renderer behavior, and reflects over internal renderer/form types. [Service replacement](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/DependencyInjection/HtmxorApplicationBuilderExtensions.cs), [custom invoker](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Endpoints/HtmxorComponentEndpointInvoker.cs), and [renderer reflection](https://github.com/egil/Htmxor/blob/d8e09e4da17ab4c74fbea95d8e995137785c8395/src/Htmxor/Rendering/HtmxorRenderer.cs).

**Assessment.** The authoring interface has unusually strong locality. Its gaps are a missing normal-only declaration, unsafe verb defaults, action identity that needs exact route-and-verb binding, an old embedded client, incomplete cache variation, and high private-framework coupling. These are reasons to rebuild the plumbing, not reasons to require a controller beside every component.

### Htmx.Net and Htmx.TagHelpers

**Observed interface.** The canonical Razor Pages example keeps selection in the page handler:

```csharp
return Request.IsHtmx()
    ? Partial("_Form", this)
    : Page();
```

Tag Helpers generate URLs from `hx-page`, `hx-page-handler`, controller/action, named route, and route values. They do not create the endpoint. Request headers and response headers have typed/fluent APIs, including `WithVary()`. Antiforgery integration maps a small script that sends the framework token on non-GET requests. The application chooses and serves the htmx browser version. [Project README and examples](https://github.com/khalidabuhakmeh/Htmx.Net/blob/main/Readme.md).

**Assessment.** This has the lowest coupling among the surveyed .NET integrations because it extends public `HttpRequest`, `HttpResponse`, MVC, and Razor Pages seams. The cost is repeated branching and coordination among route handler, full page, and partial view. Htmxor should borrow its protocol types, explicit `Vary` support, and caller-controlled asset strategy without adopting its per-handler authoring burden.

### Rizzy

**Observed interface.** Rizzy renders Razor components from MVC or Minimal API endpoints. A handler returns a full `View<TComponent>()`, `PartialView<TComponent>()`, or render fragment. `HtmxLayout` removes the root/page layout for HTMX requests and adds `Vary: HX-Request`. `[HtmxRequest]` constrains an MVC action to HTMX, and an OOB swap service composes several updates. Its full view path uses the public ASP.NET `RazorComponentResult` seam. MVC actions can use ordinary authorization and antiforgery attributes; Minimal API routes still require the application to attach the corresponding endpoint policies. [Minimal API mapping](https://jalexsocial.github.io/rizzy.docs/docs/framework/minimalapi/mapping-endpoints/), [OOB swapping](https://jalexsocial.github.io/rizzy.docs/docs/htmx/out-of-band-swapping/), and [repository source](https://github.com/JalexSocial/Rizzy).

**Assessment.** Rizzy is the closest current .NET evidence that rich Razor component responses can sit on supported ASP.NET APIs. It still splits route/action ownership from the `.razor` file, so copying its public rendering seam is useful while copying its endpoint authoring model would remove Htmxor's differentiator. Its current htmx 4 beta dependency also makes it a poor model for Htmxor's stable-client policy.

### RazorX

**Observed interface.** RazorX describes itself as an ASP.NET and HTMX meta-framework that uses Minimal APIs for routing and Razor components as templates rather than Blazor routing or interactivity. A feature typically contains a page component and a C# request handler. The handler's `MapRoutes` method calls `MapGet`, `MapPost`, and related APIs, then returns `RenderComponent<T>()`. Endpoint filters cover antiforgery, authorization, and validation. [Request/response cycle and sample](https://github.com/ranzlee/razorx#requestresponse-cycle).

**Assessment.** The explicit split makes the runtime easy to understand and keeps it on public APIs. It also creates the separate endpoint file and per-component mapping Htmxor is intended to avoid: the `.razor` component is not reachable until a C# handler maps it. RazorX is a useful implementation reference, not the right Htmxor user interface.

### Giraffe.ViewEngine.Htmx

**Observed interface.** This F# package tracks htmx versions and supplies typed attributes plus request and response helpers. Its fragment-rendering facility can locate an element by `id` within one view and render that subtree, which avoids maintaining a second partial template. The application still declares a Giraffe route and chooses full or fragment output in the handler. [Package metadata](https://www.nuget.org/packages/Giraffe.ViewEngine.Htmx/2.0.10), [source repository](https://git.bitbadger.solutions/bit-badger/Giraffe.Htmx), and [fragment rendering design](https://bitbadger.solutions/blog/2022/fragment-rendering-in-giraffe-view-engine.html).

**Assessment.** Its element-id selection validates the value of fragment locality in a .NET stack. Htmxor can offer a stronger version because the fragment and the route both stay with the Razor component.

### django-htmx with Django 6 template partials

**Observed interface.** Middleware attaches a typed `request.htmx` object. A view commonly chooses a template as follows:

```python
template_name = "countries.html"
if request.htmx:
    template_name += "#country-table"
return render(request, template_name, context)
```

Django 6 defines `{% partialdef country-table inline %}` in the same template and allows direct rendering with `countries.html#country-table`. django-htmx supplies request data, response helpers, cache guidance, CSRF guidance, and a script tag that can serve htmx 2.0.10 or an opt-in htmx 4 beta with matching extensions and CSP nonce support. `request.htmx` recognizes the exact `HX-Request: true` value, and its absolute-current-URL helper rejects a different scheme or host. [Partial rendering](https://django-htmx.readthedocs.io/en/latest/tips.html#partial-rendering), [middleware](https://django-htmx.readthedocs.io/en/latest/middleware.html), [Django partial syntax](https://docs.djangoproject.com/en/6.0/ref/templates/language/#template-partials), [response helpers](https://django-htmx.readthedocs.io/en/latest/http.html), and [asset tag](https://django-htmx.readthedocs.io/en/latest/template_tags.html).

**Assessment.** Django 6 provides the closest template-level analogy to `HtmxFragment`: named partials are local to the full template. The route and request branch remain elsewhere. Its shallow middleware/template-loader implementation is maintainable, but every feature owns URLconf/view/template coordination. Its stable-versus-prerelease asset choice, CSP support, exact header parsing, and documented cache variation are good v1 patterns.

### FastHX

**Observed interface.** FastHX wraps an ordinary FastAPI route with a renderer decorator:

```python
@app.get("/users")
@jinja.hx("user-list.html")
async def users() -> list[User]:
    return await load_users()
```

`hx()` renders HTML for an HTMX request and otherwise leaves the route's data result unchanged. `no_data=True` rejects the non-HTMX data representation and therefore supplies an HTMX-only mode. `page()` always renders HTML. A component selector can choose a template/component from a request header, and an error selector can choose presentation for failures. Renderer protocols can support async streaming. [Jinja examples](https://volfpeter.github.io/fasthx/examples/jinja-templating/), [Jinja API](https://volfpeter.github.io/fasthx/api/jinja/), [selectors](https://volfpeter.github.io/fasthx/api/component_selectors/), and [HTMY streaming](https://volfpeter.github.io/fasthx/api/htmy/).

**Assessment.** FastHX has the clearest explicit vocabulary for always-HTML, HTMX-conditioned HTML, and HTMX-only rendering. Its ordinary `hx` mode is often HTML-versus-JSON rather than full-page-versus-fragment HTML, so it is not the whole Htmxor model. The two decorators are concise and based on public FastAPI behavior, but the route still lives outside the template. Htmxor should put similarly explicit reachability choices on the component.

### holm

**Observed interface.** holm is the strongest counterexample to the claim that backend HTMX integrations always require manual endpoint registration. Its file-system router discovers a `page.py` module and creates the page's GET route. A `handle_submit` function in the same module creates POST at the same URL. Verb-specific `@action.get()`, `@action.post()`, and related functions can live in `page.py` or `actions.py`; holm derives their relative paths and renders returned htmy components without layouts by default, which is intended for HTMX fragments.

```python
# users/page.py becomes GET /users
def page(request: Request) -> Component:
    content = UsersPage()
    if request.headers.get("HX-Request") == "true":
        return without_layout(content)
    return content

# The same file also creates POST /users.
def handle_submit() -> Component:
    return UpdatedUsersPage()

# Creates GET /users/count and returns an unwrapped fragment.
@action.get()
def count() -> Component:
    return UserCount()
```

Pages and actions use normal FastAPI dependency injection, and layouts are composed from the package tree. Actions can opt back into their owner layouts. The example above preserves one route and keeps the branch in `page.py`, but the developer reads the header and chooses the response explicitly. holm has no named inline-region matcher or first-class multiple-fragment result. Generated page and action routes remain ordinary HTTP routes unless the application adds a FastAPI dependency or another guard. [Application components](https://volfpeter.github.io/holm/application-components/), [file-system routing](https://volfpeter.github.io/holm/file-system-based-routing/), [actions with HTMX](https://volfpeter.github.io/holm/guides/actions-with-htmx/), and [`without_layout`](https://volfpeter.github.io/holm/utilities/).

holm uses htmy's default escaping and standard FastAPI dependencies. Its security guide recommends secure cookie settings, SameSite, and unsafe-verb discipline, but says applications that need CSRF tokens must implement them according to their architecture. It does not document Spring-style automatic tokens, Htmxor-style endpoint antiforgery metadata, or automatic `Vary` behavior for the documented full/partial branch. [holm security](https://volfpeter.github.io/holm/security/).

`App()` locates the caller package, scans and imports known files at startup, builds nested public FastAPI `APIRouter` instances, and wraps generated operations with FastHX `HTMY.page`. This is a deep module with high caller leverage and locality, built on public adapters rather than private host-framework replacement. The tradeoff is runtime discovery: route mistakes lack compile-time diagnostics, imports execute during startup, and the tagged implementation logs and skips an import that raises. Version 0.10.0 also replaced the earlier `layout.html` convention with Jinja layouts, evidence that this pre-1.0 interface is still moving. [Tagged application implementation](https://github.com/volfpeter/holm/blob/v0.10.0/holm/app.py), [tagged application model](https://github.com/volfpeter/holm/blob/v0.10.0/holm/_model.py), and [0.10 upgrade guide](https://github.com/volfpeter/holm/blob/v0.10.0/docs/upgrade-guides/0.9.0-to-0.10.0.md).

**Assessment.** holm proves that automatic UI route discovery, local HTTP actions, and same-URL full/partial branching can be implemented as a maintainable framework convention. It is closer to Htmxor's desired developer experience than FastHX alone, and Htmxor should borrow its explicit method decorators and deterministic route naming. Htmxor remains distinct in retaining standard Blazor `@page` ownership while adding declarative reachability and inline named/predicate selection with multiple-fragment composition, without requiring a manual header branch. It should exceed holm's security posture by carrying ASP.NET authorization and mandatory antiforgery metadata onto every generated unsafe endpoint.

### htmx-spring-boot

**Observed interface.** Spring controller methods retain route ownership:

```java
@HxRequest
@GetMapping("/users")
public String users() {
    return "users :: list";
}
```

`@HxRequest` can constrain trigger id, trigger name, target, and boosted state, and it can be composed into a custom mapping annotation. A project can use an HTMX-conditioned handler beside an ordinary handler for the same path, or branch from an injectable `HtmxRequest`. `HtmxResponse`, annotations, special view names, and dedicated views cover the standard response headers. A Thymeleaf `template :: fragment` selects one inline fragment; Spring `FragmentsRendering` composes several. [Project README](https://github.com/wimdeblauwe/htmx-spring-boot) and [5.1.0 API](https://javadoc.io/doc/io.github.wimdeblauwe/htmx-spring-boot/5.1.0/).

The Thymeleaf dialect automatically puts the Spring Security CSRF token in `hx-headers` for POST, PUT, PATCH, and DELETE attributes, including elements that are not forms. Security success, failure, logout, entry-point, and access-denied integrations translate redirects or refreshes into HTMX-compatible responses. [Spring Security integration](https://github.com/wimdeblauwe/htmx-spring-boot#spring-security).

**Assessment.** This is the richest protocol and security integration in the survey. It uses supported Spring extension points and publishes an explicit compatibility matrix, which is a better maintenance posture than depending on private renderer types. Its user code remains split between controller and template. Htmxor should borrow its security completeness, endpoint conditions, and multi-fragment result semantics while keeping component-owned routes and handlers.

### axum-htmx

**Observed interface.** A Rust handler opts into typed request extractors and returns typed response parts. `HxRequestGuardLayer` can reject or redirect non-HTMX traffic for a router. `AutoVaryLayer` observes the request extractors in use and adds the corresponding `Vary` fields. The crate does not select templates, bind forms, provide CSRF, authorize callers, or ship browser assets. Its documentation explicitly warns that the HTMX guard cannot enforce authorization because a client can forge `HX-Request`. [axum-htmx API and security warning](https://docs.rs/axum-htmx/latest/axum_htmx/).

**Assessment.** This is the thinnest integration reviewed. It has a small maintenance surface and predictable framework behavior, while pushing almost all application behavior into handlers. Its typed header parts, automatic cache variation, and explicit security warning should be adopted in Htmxor's protocol layer.

### templ

**Observed interface.** templ compiles `.templ` components to typed Go code, but routes remain explicit `http.Handler` or framework registrations. The official HTMX counter example renders the complete page on each request and uses client-side `hx-select` to extract `#countsForm`. This keeps one renderer path but executes and transmits the full document. [HTMX example](https://templ.guide/server-side-rendering/htmx/) and [server-rendered application example](https://templ.guide/server-side-rendering/example-counter-application/).

For server-side selection, a template can define `@templ.Fragment("name")`, and a handler can use `templ.WithFragments("name")`. Multiple and nested matches are supported. The documentation warns that fragment filtering discards nonmatching output after executing the containing template, so component code outside the selected fragment still runs. [Fragment documentation](https://templ.guide/syntax-and-usage/fragments/).

**Assessment.** templ is the closest comparison for inline, named, server-selected component fragments. It also documents the performance boundary Htmxor must state clearly: output selection does not automatically become lifecycle or data-loading selection. Htmxor has the opportunity to retain the same locality while removing the explicit route and handler selection code.

## Brief secondary examples

### Biff

Biff is a Clojure web framework that uses HTMX in its standard application model. Routes are data structures that point to handlers, and handlers return Hiccup trees. Forms use Ring antiforgery, `biff/form` inserts the token, and the starter setup places the token in HTMX headers. This gives forms an explicit CSRF path, while route and component output remain separate program elements. [Biff message tutorial](https://biffweb.com/docs/tutorial/messages/) and [security reference](https://biffweb.com/docs/reference/security/).

### axum-routing-htmx

axum-routing-htmx adds Rust macros such as `#[hx_get("/title")]` and builds an `HtmxRouter`. It makes the server route more declarative, but the Rust handler function still owns the route and response. It does not move route ownership into a UI template/component. [Crate documentation](https://docs.rs/axum-routing-htmx/latest/axum_routing_htmx/).

### Htmx.Components

Htmx.Components 2.0.4 is a larger .NET 10 MVC/ViewComponent framework. Controllers can return models and let result filters choose a full view or multi-swap response. It includes tables, CRUD, page state, navigation, authorization flows, and a browser runtime. That breadth demonstrates how much application boilerplate result filters can remove, but it also couples consumers to a domain-specific MVC/filter architecture. It is not a component-owned routing model. [NuGet release](https://www.nuget.org/packages/Htmx.Components/2.0.4), [developer architecture](https://ucdavis.github.io/Htmx.Components/articles/developer-guide/architecture.html), and [source repository](https://github.com/ucdavis/Htmx.Components).

## Investigated adjacent projects excluded from the main matrices

- **Hydro** has attractive component-local server actions and generated component endpoints, but its current documentation describes a custom AJAX protocol with Alpine.js and contrasts that model with HTMX. It is useful evidence for action locality, not evidence of HTMX compatibility. [Hydro overview](https://usehydro.dev/introduction/overview.html) and [framework comparison](https://usehydro.dev/introduction/comparisons.html).
- **RazorSlices** is a public-API, low-allocation Razor rendering library for Minimal APIs. Its current README lists HTMX extensions as an area of interest rather than a supported integration, so it is not included in the capability matrices. [RazorSlices repository](https://github.com/DamianEdwards/RazorSlices).
- **htmxRazor 2.1.2** explicitly supports HTMX through Razor Tag Helpers, more than 90 server-rendered UI components, packaged assets, response helpers, and interaction patterns. Application routing still comes from Razor Pages or MVC, so it is a useful UI-layer comparison rather than a peer for Htmxor's route and component-response model. [htmxRazor package](https://www.nuget.org/packages/htmxRazor/).

## Maintainability comparison

### Observed implementation spectrum

1. **Thin protocol adapters:** Htmx.Net, django-htmx, and axum-htmx use public request, response, middleware, template, or layer extension points. They own little rendering behavior. Application code owns the branch and the route.
2. **Rendering adapters:** FastHX, htmx-spring-boot, Rizzy, and Giraffe.ViewEngine.Htmx connect public framework routing to a renderer or fragment selector. They reduce response boilerplate while retaining a handler/controller boundary.
3. **Convention-routed SSR framework:** holm discovers pages, layouts, submit handlers, and actions from public Python modules, then composes FastAPI, FastHX, and htmy. It owns route conventions without owning private host-framework machinery.
4. **Component compiler:** templ owns compilation and rendering but leaves routes to Go handlers.
5. **Component-routed integration:** Htmxor discovers Blazor component routes and owns the direct rendering path as well as HTMX integration.

The documented and source seams reviewed for the maintained integrations use public host-framework extension points rather than replacing a private renderer or endpoint invoker. Spring's larger API surface is implemented through MVC argument resolvers, request conditions, views, auto-configuration, and a template dialect. Rizzy uses `RazorComponentResult`. The thin adapters operate at HTTP boundaries.

### Assessment

Thin adapters are easiest for their maintainers to keep compatible because the host framework owns routing and rendering. They shift coordination, branching, and partial-file drift into every application. Htmxor's value is precisely that it centralizes this work once. A stable design therefore needs a deeper module built only from supported host APIs, with generated metadata where reflection over private runtime state would otherwise be required.

In codebase-design terms, Htmxor is pursuing **depth**: a compact developer interface hides a substantial routing, rendering, protocol, and security implementation. Its **leverage** comes from removing endpoint and branching code from every consuming application. That complexity needs **locality** inside one module, with a generated catalog as the seam between Razor authoring and small public-framework adapters.

The target is not “as thin as possible.” The target is the smallest maintainable implementation that preserves component-owned routes and fragments.

## Htmx changes the prototype never absorbed

Yes, the comparison reveals gaps that are easy to miss if the review stops at backend APIs. There is a timing nuance: htmx 2.0.0 through 2.0.2 were released before Htmxor's last code commit on `main` on 2024-09-09, but Htmxor still embeds 1.9.12. Treating those releases as unfinished prototype migration work is an assessment, not proof that implementation had started. htmx 2.0.3 through 2.0.10 were released after that commit and add behavior that the prototype could not have covered.

The official sources for this boundary are the [htmx 2.0 release](https://htmx.org/posts/2024-06-17-htmx-2-0-0-is-released/), [1.x-to-2.x migration guide](https://htmx.org/migration-guide-htmx-1/), current [htmx documentation](https://htmx.org/docs/), and [2.x changelog](https://github.com/bigskysoftware/htmx/blob/master/CHANGELOG.md).

| Client change or current contract | Timing relative to Htmxor | Gap exposed by the backend comparison | Htmxor v1 consequence |
| --- | --- | --- | --- |
| Extensions moved out of core; legacy `hx-sse`/`hx-ws` were removed; module builds were added | htmx 2.0.0; not adopted by the 1.9.12 prototype | Most thin adapters intentionally leave extension loading and version matching to the app; django-htmx and Giraffe provide more asset help | Leave the htmx runtime and native extensions application-owned; test Htmxor's replaceable browser adapter, script order, and CSP without constraining the runtime version |
| DELETE now sends parameters in the URL/query instead of a form-encoded body; `selfRequestsOnly` now defaults to `true`; `scrollBehavior` changed from `smooth` to `instant`; legacy `hx-on` became `hx-on:` | htmx 2.0.0; released before the last Htmxor commit but not adopted | Typed protocol libraries usually expose the request but do not prove application binders and typed option values against changed wire/default behavior | Generate DELETE route/query binding, add `instant` to typed configuration, diagnose old event syntax, and test rather than assume htmx 1 form bodies |
| Default 4xx/5xx response handling does not swap; applications can configure `responseHandling` or `htmx:beforeSwap` | Current htmx 2 contract; absent from the prototype's declared-error model | None of the reviewed documented interfaces combined a server marker for intentional error fragments with matching client response handling | Mark intentional error fragments server-side and let the Htmxor adapter opt only those responses into swapping; unexpected/security failures remain errors |
| A history-cache miss needs a complete page; official guidance says `historyRestoreAsHxRequest` should be `false` when `HX-Request` selects partials | Current htmx 2 contract; history paths changed again in 2.0.5, 2.0.8, and 2.0.9 | `Vary: HX-Request` helpers do not configure the browser or guarantee that history restore reaches a full representation | Emit the safe client profile and make `HX-History-Restore-Request: true` select the normal full-page candidate regardless of `HX-Request` |
| Nested OOB processing is configurable; 2.0.3 enabled `hx-preserve` in OOB swaps | 2.0.3 shipped after Htmxor's last change | Libraries with OOB helpers generally leave nested extraction and preservation semantics to htmx/application configuration | Choose and document a nested-OOB profile; test reusable fragments, preserved nodes, and Blazor DOM ownership |
| 2.0.5 moved history cache to `sessionStorage`, routed restoration through standard swap paths, and added `inherit` for several inherited attributes | After Htmxor | Backend integrations rarely surface these because they are browser behavior, but their fragment and navigation tests can still fail | Run pushed-URL, boosted-form, inheritance, and cache-hit/miss tests against exact recorded versions while allowing applications to run newer versions through the same conformance suite |
| 2.0.7 added optional form `reportValidity()` behavior and an indicator accessibility fix | After Htmxor | Server-side validation helpers do not restore the browser's visible constraint-validation UX | Set `reportValidityOfForms: true` in the supported profile and retain authoritative server validation |
| 2.0.8 added `htmx.ajax` `pushURL` and a relative-history fix; 2.0.9 fixed `HX-Location` push/replace, history normalization, and OOB diagnostics; 2.0.10 restored type definitions and improved settle-selector escaping | After Htmxor | A broad “supports htmx 2” claim hides patch-sensitive navigation, OOB, typing, and selector behavior | Publish exact verified test runs, not a runtime allowlist, and let applications execute the same suite against an upgraded client |

This does not mean every omission is a defect in the other libraries. Htmx.Net, django-htmx, and axum-htmx are deliberately thin in places. But Htmxor promises automatic same-URL full/fragment selection, selected/OOB rendering, and a browser bridge, so history, response handling, cache variation, and client defaults cross its abstraction boundary and become Htmxor responsibilities.

Among the reviewed .NET integrations and documented versions, none presents the complete Htmxor combination proposed here: component-owned `.razor` routes, normal-only/HTMX-only/both reachability, component-local explicit actions, inline single/multiple fragment selection, automatic HTTP variation with fail-closed cache storage, marked error-fragment swapping, and coexistence with stock Blazor render modes. The non-.NET comparisons demonstrate individual patterns rather than being expected to support Blazor. That combination is the opportunity, provided v1 proves it on public framework seams.

The companion [Htmxor v1 interface sketch](htmxor-v1-interface-sketch.md) shows the proposed developer code and the generated routing, security, rendering, cache, error-response, and htmx 2 browser contracts behind it.

## Recommended v1 developer contract

The following is a design assessment derived from the comparison. Exact attribute and directive names remain a separate API-design decision.

### Keep route ownership in the Razor component

One application-level setup call is acceptable. Per-feature endpoint code is not.

| Component declaration | Normal request | HTMX request | v1 intent |
| --- | --- | --- | --- |
| `@page` with default Htmxor participation | Stock Blazor page | Direct static SSR component/fragment | **Both** |
| explicit Htmxor route without `@page` | Not routed by Blazor | Direct static SSR component/fragment | **HTMX-only** |
| `@page` with an explicit Htmxor opt-out | Stock Blazor page | No direct Htmxor endpoint | **Normal-only** |

This completes the original model instead of replacing it. Existing `@page` dual reachability should be retained as the compatibility default unless migration evidence justifies an opt-in default.

### Make endpoint verbs explicit server declarations

An inferred `@page` Htmxor endpoint should default to GET. POST, PUT, PATCH, and DELETE require an explicit server-side declaration and mandatory antiforgery validation. An HTMX-only or dual route should bind each local handler to the exact normalized route and HTTP verb.

A source generator may inspect literal and transitive `hx-get`, `hx-post`, and related attributes to produce diagnostics and catch missing declarations. Those client attributes cannot be the authority that creates server verbs:

- markup can target another component or an external/dynamic URL;
- attributes can be conditional, splatted, or computed at runtime;
- a reused child component must not silently widen the parent's server attack surface;
- request headers and element ids are forgeable.

The generator should validate resolvable call sites against declared endpoints. The route/action catalog should come from explicit component metadata and handlers.

A build-time catalog may safely derive POST from an explicit component-local `@onpost` handler, or from `[HtmxRoute(Methods = ...)]`, because those are server declarations. Finding `hx-post` only proves that a caller may exist; it must not create or widen the endpoint.

The exact build-time mechanism needs a spike. Roslyn generators run unordered and ordinarily cannot see files produced by another generator. Because Razor compilation can itself use a source generator, a separate C# generator must not assume that Razor-generated component syntax is a supported input seam. Options include treating `.razor` files as explicit additional inputs, using a supported Razor-aware analyzer/compiler extension, or producing the manifest in an earlier build stage. Route correctness must not depend on experimental generator ordering. [Roslyn source-generator design](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.md).

### Preserve whole, selected, and OOB output

The direct endpoint should support:

1. the whole component output by default;
2. one inline named or predicate-selected fragment;
3. several selected fragments in one response;
4. OOB fragments and layout-owned outlets;
5. typed HX response headers and status control.

Django, Thymeleaf, Giraffe, and templ all validate the maintainability benefit of fragment locality. Htmxor should keep fragments in the `.razor` component and allow request metadata to choose them without a C# controller branch.

The performance contract must be explicit. Selecting output can avoid building and serializing nonselected child subtrees, but it does not imply that the enclosing component was never instantiated or that its lifecycle/data loading did not run. templ documents the same boundary. If v1 promises stronger execution pruning, that behavior needs a separate public design and benchmark evidence.

### Treat HTMX selection as representation, never authorization

`HX-Request`, `HX-Target`, `HX-Trigger`, and custom fragment selectors are client input. They can select among server-declared representations after routing and authorization. They cannot grant access, enable a verb, or identify a trusted action.

Endpoint metadata should carry standard ASP.NET authorization, antiforgery, rate-limit, cache, and request-size policies. Unsafe methods should fail closed when antiforgery is missing or invalid. Spring's automatic unsafe-verb CSRF integration and axum-htmx's explicit guard warning are the clearest models in the survey.

### Make cache and protocol behavior automatic

When the same URL has normal and HTMX representations, Htmxor should add `Vary: HX-Request`. If output also varies by history restore, target, trigger, or another declared selector, the endpoint should add only the matching `Vary` fields. This follows django-htmx's cache guidance and axum-htmx's `AutoVaryLayer` pattern.

Correct `Vary` fields prevent full and fragment representations from colliding, but they do not make personalized content safe for a shared cache. Responses affected by authorization, cookies, user state, or antiforgery-derived content need the appropriate `private`, `no-store`, or output-cache policy as well.

For v1, Razor component output should receive `Cache-Control: private, no-store` by default as well as bypassing ASP.NET output-cache storage. Shared storage should require a mechanically enforced token-free response class with canonical selectors; a source scan that merely fails to notice a nested or dynamic antiforgery token is not sufficient. HTTP `Vary` remains correct and automatic even while all storage is disabled.

Typed request parsing should use exact protocol values and validate URL-valued headers before exposing trusted-looking conveniences. Typed response helpers should cover the stable htmx 2 response headers. Protocol support should be separable from component routing so it can be tested without rendering Blazor components.

### Decouple the browser asset

Htmxor should not hardwire any browser-library release into the renderer or server package. Application ownership, as in Htmx.Net and Spring, should be the default and the server should neither inspect nor enforce `htmx.version`.

The maintainable boundary has two extension points:

- a small browser-core module for Htmxor's antiforgery and handled-fragment behavior, connected through a replaceable runtime adapter that maps public htmx lifecycle events;
- a declarative typed protocol registry for custom request and response headers, parsing, automatic `Vary`, size limits, URL validation, and conflict handling.

Htmx native extensions, `hx-ext`, unknown future `hx-*` attributes, htmx configuration, and the core script stay in application code. A maintained htmx 2 adapter and a separate htmx 4 preview adapter may provide defaults, but neither should include htmx or constrain the version the application serves. Optional asset bundles can exist as non-transitive conveniences.

This is more future-proof than copying all of `htmx.config` into C# or wrapping htmx's extension API. Htmx 2 currently exposes `htmx.defineExtension`, `htmx:configRequest`, and `htmx:beforeSwap`; the [htmx 4 preview extension model](https://four.htmx.org/docs/extensions/using-extensions) already uses a different registration and event-hook shape. The application can replace that small adapter when the browser API moves while retaining the same Razor routes and server protocol features. [Htmx 2 events](https://htmx.org/events/), [extension interface](https://htmx.org/extensions/building/), and [`hx-ext`](https://htmx.org/attributes/hx-ext/).

Compatibility documentation should distinguish **verified** from **required**. Each CI row records the exact htmx and adapter versions exercised. Unknown or newer versions remain allowed; applications can run a published browser conformance kit against their selected script and extensions. CSP nonce, integrity, unsafe-request antiforgery, history restoration, marked-error swapping, OOB behavior, and Blazor DOM ownership belong in that suite.

### Coexist with stock Blazor endpoints and render modes

Normal requests should remain on the stock Blazor endpoint and continue to support static SSR, Interactive Server, Interactive WebAssembly, and Interactive Auto according to normal Blazor rules. The HTMX path should be an additive Htmxor static SSR component/fragment endpoint, built from supported public APIs. [Microsoft Learn describes the .NET 10 render modes and their render locations](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0).

This coexistence has two constraints:

- Interactive Auto does not migrate an already rendered component in place merely because its runtime becomes available. Microsoft states that Auto makes an initial decision and never dynamically changes the render mode of a component already on the page. Serving the same `@rendermode Auto` page through a static direct endpoint needs a public-API prototype before v1 promises it. [Microsoft Learn, Automatic rendering](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0#automatic-auto-rendering).
- HTMX must not replace DOM owned by an active interactive Blazor root. The integration needs a documented ownership boundary and diagnostics for unsafe target placement.

This is a two-path model, not hydration of an HTMX fragment. Normal `@page` requests may use Interactive Auto; direct HTMX requests return static SSR markup. `RazorComponentResult` is the public static-SSR result seam and does not expose a render-mode option. The prototype must establish whether a component declared with `@rendermode InteractiveAuto` can also be rendered through that static result as Htmxor markup without emitting or depending on interactive markers. [RazorComponentResult API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpresults.razorcomponentresult?view=aspnetcore-10.0).

These constraints preserve Blazor behavior rather than turning Htmxor into an alternative interactive-component runtime.

## Recommended implementation boundary

The comparison points to five modules with different reasons to change:

1. **Build-time route and action catalog.** Compile component-owned `@page`, Htmxor route, reachability, verb, fragment, and handler declarations into deterministic metadata through a supported Razor-aware seam. Produce diagnostics for conflicts and resolvable `hx-*` call sites.
2. **Public ASP.NET endpoint adapter.** Map the generated catalog through public routing and endpoint-convention APIs. Attach standard HTTP method, authorization, antiforgery, cache, and request-size metadata.
3. **Public Razor rendering adapter.** Use supported component result/rendering APIs, taking Rizzy's `RazorComponentResult` path as evidence. Prototype full and selected fragment rendering before fixing the API.
4. **Independent HTMX protocol layer.** Parse typed request headers, write typed response headers, calculate `Vary`, and enforce URL/header validation without depending on the component renderer.
5. **Browser adapter and protocol extension boundary.** Keep htmx application-owned; expose one replaceable lifecycle adapter and one declarative typed feature registry; publish exact verified runs and a caller-runnable conformance kit.

The v1 implementation should not remove/replace private Blazor services, copy a private endpoint invoker, or reflect over private renderer/form types. Those seams are the source of upgrade fragility in the prototype; the documented and source integration seams reviewed for the maintained alternatives instead use public host-framework extension points.

## What to borrow, preserve, and avoid

| Source | Borrow | Preserve in Htmxor instead | Avoid |
| --- | --- | --- | --- |
| Htmx.Net | Typed HTTP protocol layer, explicit `Vary`, caller-owned assets | Component-owned route and automatic full/direct choice | Repeated page-handler branches and partial naming |
| Rizzy | Public Razor component result seam, layout/OOB response concepts | `.razor` route ownership | Mandatory controller or Minimal API route for each component; beta-only client coupling |
| RazorX | Public endpoint policies and explicit security metadata | Zero feature endpoint boilerplate | Page component plus separate route-handler pair |
| Django 6 / django-htmx | Inline named partials, exact header parsing, CSP-aware asset selection, cache guidance | Route and fragment choice in the component | Repeated URLconf/view/template coordination |
| FastHX | Clear reachability modes, component and error selectors, renderer protocol | Full-page HTML and HTMX fragment from one component route | Treating HTMX-only as authorization |
| holm | Automatic page/action discovery, local submit handlers, explicit verb decorators, deterministic route names | `.razor` `@page` ownership, declarative reachability, and inline named/multiple selection from the same component | Runtime-only route diagnostics, manual HX-header branching, and application-defined CSRF/cache variation |
| htmx-spring-boot | Unsafe-verb CSRF, auth-flow adapters, request conditions, multi-fragment results, compatibility matrix | Component-local route and action | Controller/template split |
| axum-htmx | Typed extractors/response parts, automatic `Vary`, explicit header-spoofing warning | Razor rendering integration | Leaving all protocol and selection plumbing to each app |
| templ | Compile-time component model, inline named fragments, honest execution semantics | No app-authored handler for each route | Full-page render and transfer when only one server fragment is needed |

## Roadmap consequences

The comparison supports these priorities for a stable v1 roadmap:

1. Freeze the component-owned authoring contract and specify all three reachability modes.
2. Specify route, verb, local-handler identity, antiforgery, authorization, and header-trust invariants before renderer work.
3. Prove a public-API endpoint and Razor component rendering path on supported .NET versions. Cover a normal Auto page with static HTMX fragments outside interactive roots, the same `@rendermode Auto` page reached through both paths, `hx-boost` versus Blazor enhanced-navigation ownership, insertion and removal of interactive roots, DOM cleanup, and whether either runtime must explicitly process newly inserted content.
4. Select and prove the Razor-aware build-time generation seam, then build route/action metadata and diagnostics. Treat markup inspection as validation only.
5. Restore whole-component, inline fragment, multiple-fragment, layout, and OOB behavior on the new rendering seam.
6. Split typed HTMX protocol and automatic HTTP/cache-key variation from rendering; keep shared component-output storage disabled until token-free rendering is mechanically enforced.
7. Remove the embedded 1.9.12 client. Make the htmx runtime, native extensions, configuration, and upgrade schedule application-owned; ship a separately replaceable htmx 2 reference adapter.
8. Specify and prove the typed protocol feature registry, protected-header rules, automatic variation, browser-core contract, and adapter security ordering.
9. Publish the browser conformance kit. Record exact verified htmx/adapter runs in CI without turning them into a dependency or runtime allowlist.
10. Benchmark normal Blazor requests, direct whole-component requests, single-fragment requests, multiple/OOB requests, form posts, and streaming where supported. Measure allocations, time to first byte, total time, and work performed outside selected fragments.

The central product decision is therefore clear. holm shows that endpoint-free page/action discovery and same-URL full/partial branching are not unique by themselves. Htmxor's defensible combination is idiomatic Blazor `@page` ownership, explicit declarative reachability modes, component-local handlers, and inline server-selected single/multiple fragments. Keep that interface. Move away only from private Blazor implementation seams, broad implicit verb exposure, fixed client assets, and unspecified security/cache behavior.
