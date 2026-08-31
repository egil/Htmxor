# Htmxor v1 developer experience — review draft

This is the compact on-ramp for the intended Htmxor v1 API. It is not a claim
that every example is available in the current beta. The agreed
[v1 goal](https://github.com/egil/Htmxor/blob/main/docs/roadmap/v1/goal.md) wins
where current implementation details differ.

The application supplies htmx 4.0.0, chooses its extensions and configuration,
and owns the client upgrade schedule. Htmxor supplies static-SSR component
routing, instance callback dispatch, fragment selection, response helpers, and
a small antiforgery/callback adapter. It does not bundle htmx.

The exhaustive working-tree draft is
`docs/htmxor-v1-feature-guide.md`; the API findings and rationale are in
`docs/research/htmxor-v1-dx-review.md`.

## Four rules

1. The Razor component owns its routes, callbacks, lifecycle, forms, and HTML.
2. `@page`, `HtmxRoute`, stock forms, and statically discoverable component
   callbacks grant server reachability. Client `hx-*` attributes never do.
3. A normal request follows stock Blazor routing. A direct htmx request returns
   the component or explicitly selected `HtmxFragment` boundaries.
4. Write ordinary htmx markup in Razor. Htmxor types the server protocol where
   that prevents mistakes; it does not make developers wait for a C# wrapper
   before using a client feature or extension.

HTMX headers are untrusted representation hints. An HTMX-only route is not an
authentication or authorization boundary.

## 1. Configure the application

The current v1 plan removes the empty route-group plumbing through #145:

```csharp
builder.Services
    .AddRazorComponents()
    .AddHtmx();

app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddHtmxorComponentEndpoints();
```

The final service/endpoint names are under DX review because `AddHtmx()` sounds
like it installs the browser runtime while `AddHtmxorComponentEndpoints()` uses
the product name. The intended behavior is settled; naming is not.

Supply the app-owned htmx asset before the adapter:

```razor
<script defer src="htmx-4.0.0.min.js"></script>
<HtmxHeadOutlet />
```

## 2. Choose reachability

| Component declaration | Normal request | Direct htmx GET |
| --- | --- | --- |
| `@page "/products"` | Stock page and layout | Same component by convention |
| `@page` plus the v1 normal-only marker | Stock page and layout | Not mapped |
| Component-local `[HtmxRoute("/products")]` without `@page` | Not mapped | HTMX-only component representation |

The normal-only marker's final API name remains to be frozen. `HtmxRoute` can
be written in `.razor`, on the matching `.razor.cs` partial, or on a component
authored entirely in C#. It is component-specific and does not belong in
`_Imports.razor`.

GET is the only implicit method. A C#-authored declaration must provide its
complete `Methods` allow-list.

## 3. Add component-owned actions

The common authoring forms are:

| Intent | Component declaration |
| --- | --- |
| GET | Implicit route GET |
| Normal enhanced submit | Stock `EditForm`/form POST |
| Explicit POST | `@onpost` |
| PUT | `@onput` |
| PATCH | `@onpatch` |
| DELETE | `@ondelete` |
| Future QUERY | Application-authored `@onquery` |

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

Callbacks run on the request-created component instance. Route/query/form state,
dependency injection, authentication state, lifecycle, and rendering stay
available. They are not static Minimal API handlers.

Use a parameterless callback when component state is enough. Accept
`HtmxEventArgs` only when the callback needs request details or a response
operation:

```csharp
private async Task Save(HtmxEventArgs e)
{
    await store.SaveAsync(model);
    e.Response.Trigger("report:saved", new { model.Id });
}
```

`hx-post`, `hx-action`, `hx-method`, and `hx-query` can be checked against a
server declaration, but they never grant a method. Dynamic or ambiguous callback
syntax must produce a useful build diagnostic and require one narrow explicit
declaration.

QUERY is accepted v1 server intent but remains a future claim until its full
compiler, HTTP, proxy, security, and browser evidence exists.

## 4. Keep stock forms stock

Add htmx attributes to `EditForm` rather than creating a parallel form model:

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

The non-JavaScript submission remains meaningful. Named forms,
`[SupplyParameterFromForm]`, validation, authorization, antiforgery, and
lifecycle behavior are Blazor-owned and must remain equivalent on the enhanced
path. Every unsafe method fails closed before binding or callback execution.

## 5. Select server fragments independently from DOM targets

`HtmxFragment` remains the only server fragment concept. It can be wrapperless
or emit an `Element`, `Id`, and ordinary HTML attributes.

V1 still needs one explicit, stable way to name server selection boundaries and
select one or several names. That name must not be the wrapper DOM ID, and it
must not be inferred from forgeable `HX-Target`/`HX-Source` values. The exact API
is deliberately not shown as executable code until the fragment issue approves
it.

Once the server has selected the fragment, use native htmx delivery markup:

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

or ordinary out-of-band markup:

```razor
<HtmxFragment>
    <section id="results">@RenderResults()</section>
    <output id="result-count" hx-swap-oob="outerHTML">@Count</output>
</HtmxFragment>
```

`<hx-partial>` and `hx-swap-oob` describe client delivery. They are not a second
Htmxor fragment model and do not by themselves select server execution.
Excluded child branches below a known fragment boundary must not render or run
their own lifecycle; the owner and required ancestors may.

## 6. Use the request/response context when HTTP behavior differs

`HtmxContext.Request` exposes the method/path and the seven core htmx 4 request
headers: request marker, full/partial request type, boost, current URL, source,
target, and history restore. All header-derived values are untrusted.

`HtmxContext.Response` provides fluent operations for the nine core response
headers:

- location, redirect, and refresh;
- push or replace URL;
- reswap, retarget, and reselect; and
- trigger client events with optional JSON detail.

It also controls ordinary status and an intentionally empty body. General HTTP
headers, cookies, cache policy, and status-independent ASP.NET Core behavior
remain available through the static-SSR `HttpContext`.

The v1 consistency issue will settle .NET naming, malformed/repeated header
behavior, uniform response validation/body effects, and bounded extension-header
hooks.

## 7. Use every htmx feature as native markup

The complete guide maps all official htmx 4.0.0 attributes, request/response
headers, trigger and swap forms, global configuration, events, JavaScript API,
CSS states, `<hx-partial>`, and all 17 official extensions.

The reusable rule is:

| Feature family | Owner |
| --- | --- |
| Server route, method, callback, binding, antiforgery, fragment execution | Htmxor plus stock Blazor/ASP.NET Core |
| Trigger, target, selector, swap, indicators, confirmation, client events | Application-owned htmx markup |
| History, redirects, errors, cache variation | Shared HTTP contract with explicit application policy and conformance evidence |
| SSE, WebSocket, multipart streaming | Application endpoint/extension; streaming component responses are outside v1 |
| Other official or third-party extensions | Raw pass-through by default; bounded server hook only when the protocol requires one |

Literal attributes are the primary client API. The current trigger/swap C# DSL
is incomplete for htmx 4 and must not be frozen merely because it exists in the
beta.

## Open DX decisions for v1

- Pending issue: `refactor(api): freeze the minimal Htmxor v1 public surface`
- Pending issue: `feat(routing): make v1 reachability and action declarations self-explanatory`
- Pending issue: `feat(fragments): separate server selection from DOM delivery`
- Pending issue: `refactor(protocol): finalize the Htmxor v1 HTTP context`

Existing ownership is preserved: #145 removes route-group plumbing, #148 owns
the .NET 10 target, #57 tracks Blazor coexistence, #58 tracks multi-target client
delivery, #69 is a concrete `hx-vals` scenario, and #16 remains outside v1
streaming work.

## Claim discipline

Examples and docs distinguish accepted v1 contract, proved slice,
application-owned client composition, DX proposal, outside-v1 behavior, and
behavior not yet exercised. Markup that compiles or passes through is not by
itself a browser, extension, security, performance, or compatibility claim.
