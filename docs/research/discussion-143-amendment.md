# Htmxor v1 developer experience

Status: draft replacement for discussion #143. It describes the planned v1 API,
not every feature in the current beta. The
[v1 goal](https://github.com/egil/Htmxor/blob/main/docs/roadmap/v1/goal.md) wins
when the plan and the current code differ.

Htmxor should feel like Blazor with htmx markup added, not a second web
framework. The component keeps its route, callbacks, lifecycle, form handling,
and HTML. Htmxor connects that component to an htmx request.

The application supplies htmx 4.0.0 and chooses its extensions, configuration,
content security policy, and upgrade schedule. Htmxor does not bundle htmx.

For detail, see the
[v1 guide](https://github.com/egil/Htmxor/blob/main/docs/htmxor-v1-feature-guide.md)
and the
[API review](https://github.com/egil/Htmxor/blob/main/docs/research/htmxor-v1-dx-review.md).

## The model

- The Razor component owns its server behavior and HTML.
- `@page`, `HtmxRoute`, stock forms, and statically discoverable callbacks grant
  server access. An `hx-*` attribute never grants a route or HTTP method.
- A normal request uses stock Blazor routing. A direct htmx request returns the
  component or selected `HtmxFragment` output.
- Client behavior stays in native htmx markup. Htmxor types the server protocol
  where a typed API prevents mistakes.

Treat every HTMX header as untrusted input. An HTMX-only route chooses a
representation. It is not an authentication or authorization check.

## Configure the application

The no-argument registration API is current.
[#145](https://github.com/egil/Htmxor/issues/145) proved it at the application
root and through one standard ASP.NET Core route group:

```csharp
builder.Services
    .AddRazorComponents()
    .AddHtmx();

app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddHtmxorComponentEndpoints();
```

The registration behavior is settled for those cases, but the names are not.
`AddHtmx()` sounds as if it installs the browser library even though the
application supplies that library.
[#151](https://github.com/egil/Htmxor/issues/151) will settle the service and
endpoint names before v1.

Load the application-owned htmx asset before the Htmxor adapter:

```razor
<script defer src="htmx-4.0.0.min.js"></script>
<HtmxHeadOutlet />
```

## Choose which requests reach a component

| Declaration | Normal request | Direct htmx GET |
| --- | --- | --- |
| `@page "/products"` | Stock page and layout | Same component |
| `@page` plus the planned normal-only marker | Stock page and layout | Not mapped |
| Component-local `[HtmxRoute("/products")]` without `@page` | Not mapped | Component output |

The normal-only marker still needs a final name. `HtmxRoute` can sit in the
`.razor` file, on its `.razor.cs` partial, or on a component written in C#. It
belongs to one component, so it does not belong in `_Imports.razor`.

GET is the only implicit method. A route written in C# must state its full
`Methods` allow-list.

## Put actions beside their markup

| Request | Component declaration |
| --- | --- |
| GET | Implicit route GET |
| Normal enhanced submit | Stock `EditForm` or form POST |
| POST | `@onpost` |
| PUT | `@onput` |
| PATCH | `@onpatch` |
| DELETE | `@ondelete` |
| QUERY, bounded proof | Application-authored `@onquery` |

```razor
@page "/reports/{ReportId:int}"

<button hx-delete="/reports/@ReportId"
        hx-target="#report-@ReportId"
        hx-swap="delete"
        @ondelete="DeleteReport">
    Delete
</button>

@code {
    [Parameter] public int ReportId { get; set; }

    private Task DeleteReport() => reports.DeleteAsync(ReportId);
}
```

The callback runs on the component instance created for the request. Route,
query, and form state remain available, as do dependency injection,
authentication state, lifecycle methods, and rendering. There is no duplicate
controller or Minimal API handler.

Use a parameterless callback when component state is enough. Accept
`HtmxEventArgs` when the callback needs request data or needs to change the
response:

```csharp
private async Task Save(HtmxEventArgs e)
{
    await store.SaveAsync(model);
    e.Response.Trigger("report:saved", new { model.Id });
}
```

Client attributes such as `hx-post` and `hx-method` can be checked against the
server declaration, but they cannot create one. If Htmxor cannot resolve a
callback at build time, the diagnostic should point to that callback and say
what declaration is needed.

[#111](https://github.com/egil/Htmxor/issues/111) proves one static
`@onquery` binding for each stock and HTMX-only route owner through the
compiler, a separately packed .NET 10 package, Kestrel, Chromium, and htmx 4.0.0
with form-encoded content. It also proves that client-only `hx-query` receives
`405` and invokes no callback. This is a bounded slice. JSON and other content
types, large or streaming bodies, cancellation, concurrent QUERY requests,
several bindings on one component, composition with unsafe actions, broad typed
route conversion, and reverse proxies remain unproved.

## Keep Blazor forms

Add htmx attributes to `EditForm` instead of learning another form API:

```razor
<EditForm Model="model"
          FormName="save-report"
          method="post"
          action="/reports"
          hx-post="/reports"
          hx-target="#reports">
    <DataAnnotationsValidator />
    <InputText @bind-Value="model.Title" />
    <ValidationMessage For="() => model.Title" />
    <AntiforgeryToken />
    <button type="submit">Save</button>
</EditForm>
```

The form must still work without JavaScript. Named forms,
`[SupplyParameterFromForm]`, validation, authorization, antiforgery, and
lifecycle behavior remain Blazor features. Unsafe methods must pass antiforgery
validation before binding or callback execution.

## Keep server selection separate from browser delivery

`HtmxFragment` is the one server fragment concept. It can be wrapperless or
render an `Element`, `Id`, and normal HTML attributes.

V1 still needs a stable name for each selectable fragment and one way to select
several names. That name must not depend on a DOM ID or on forgeable
`HX-Target` and `HX-Source` values. The final member names are omitted here
because [#153](https://github.com/egil/Htmxor/issues/153) has not settled them.

After Htmxor selects the server output, native htmx markup decides where the
browser puts it:

```razor
<HtmxFragment>
    <hx-partial hx-target="#results" hx-swap="innerHTML">
        @RenderResults()
    </hx-partial>
    <hx-partial hx-target="#result-count" hx-swap="innerHTML">
        @Count
    </hx-partial>
</HtmxFragment>
```

```razor
<HtmxFragment>
    <section id="results">@RenderResults()</section>
    <output id="result-count" hx-swap-oob="outerHTML">@Count</output>
</HtmxFragment>
```

`<hx-partial>` and `hx-swap-oob` are delivery instructions. They do not select a
server fragment. A child branch excluded by fragment selection must not render
or run its own lifecycle work. The component and required ancestors may still
run.

## Use the HTTP context when the response changes

`HtmxContext.Request` exposes the request line and the seven core htmx 4 request
headers. Those headers cover the request marker, full or partial request type,
boost, current URL, source, target, and history restoration. Every value derived
from a header is untrusted.

`HtmxContext.Response` writes the nine core response headers for location,
redirect, refresh, history updates, swap overrides, target overrides, response
selection, and client events. It also controls status and an empty body.

Use the stock `HttpContext` for cookies, cache policy, general response headers,
and other ASP.NET Core behavior. [#154](https://github.com/egil/Htmxor/issues/154)
will settle naming, malformed headers, response validation, body effects, and
extension headers.

## Write htmx as htmx

Literal htmx attributes are the main client API. Razor already accepts the htmx
4 colon forms and `<hx-partial>`. The application can use new client features
without waiting for a Htmxor package release.

Htmxor owns component routes, methods, callbacks, binding compatibility,
antiforgery, fragment execution, and typed response headers. Htmx owns triggers,
targets, selectors, swaps, indicators, confirmation, client events, and its
extensions. General navigation, cache, and error policy remain application HTTP
decisions.

SSE, WebSocket, and multipart streaming use application endpoints. Streaming
component responses remain outside v1. Other extensions pass through as markup
unless their protocol needs a small server hook.

The current trigger and swap C# helpers do not cover htmx 4. They should not
become stable v1 API merely because they exist in the beta.

## V1 decisions still open

- [#151: freeze the v1 public API](https://github.com/egil/Htmxor/issues/151)
- [#152: make routes and actions explain themselves](https://github.com/egil/Htmxor/issues/152)
- [#153: separate fragment selection from DOM delivery](https://github.com/egil/Htmxor/issues/153)
- [#154: finish the htmx 4 request and response API](https://github.com/egil/Htmxor/issues/154)

Existing issues retain their scope. #145 is closed after proving no-argument
registration at the root and through one standard route group. #148 owns the
.NET 10 target, #57 covers Blazor coexistence, #58 covers multi-target client
delivery, #69 records an `hx-vals` use case, and #16 covers streaming outside
v1.

## Evidence labels

The guide marks behavior as an accepted v1 contract, proved slice,
application-owned client behavior, API proposal, outside v1, or not yet
exercised. Markup that compiles is evidence of Razor syntax. It is not evidence
for browser behavior, security, performance, or extension compatibility.
