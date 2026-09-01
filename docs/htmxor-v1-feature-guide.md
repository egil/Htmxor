# Htmxor v1 guide and htmx 4 map

Status: design draft for the planned Htmxor v1 API. The registration names,
client-helper decision, and navigation-response contract below are current;
other proposed APIs remain labeled as proposals.

The [v1 goal](roadmap/v1/goal.md) is the authority when this guide and the
current code differ. The [v1 progress record](roadmap/v1/progress.md) says which
parts have executable evidence.

This guide uses application-supplied htmx 4.0.0 with its default configuration.
The application owns the htmx runtime, extensions, content security policy, and
upgrade schedule. Htmxor supplies server integration and a small browser
adapter. It does not bundle htmx. Use the
[official htmx 4 documentation](https://four.htmx.org/docs/),
[reference](https://four.htmx.org/reference/), and
[extension catalog](https://four.htmx.org/extensions/) for client semantics.

Htmxor has a narrow job: let a static-SSR component answer htmx requests without
forcing the application to build a second endpoint or rendering layer.

## Working model

- The Razor component owns its routes, request callbacks, lifecycle, and HTML.
- `@page`, `HtmxRoute`, stock forms, and statically discoverable component
  callbacks grant server access. Client-authored `hx-*` attributes do not.
- A normal request uses stock Blazor routing. A direct htmx request returns the
  component or named `HtmxFragment` output.
- Write htmx attributes and elements in Razor. Htmxor types the server protocol
  where a typed API prevents mistakes. It does not copy the browser API into C#.

HTMX request headers are untrusted input. An HTMX-only route is a representation
choice, not authorization. Component authorization, antiforgery, host,
rate-limit, cache, and other endpoint policies still apply.

## Configure the application

The first bounded slice of
[#151](https://github.com/egil/Htmxor/issues/151) uses one product name for
the service and endpoint registrations while retaining #145's no-argument
shape:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddHtmxor();

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddHtmxorEndpoints();

app.Run();
```

[#145](https://github.com/egil/Htmxor/issues/145) proved the no-argument root and
standard route-group behavior retained by this call using a separately packed
.NET 10 package. Nested groups, multiple Razor component applications, group
endpoint filters or rate limits, interactive render modes, and the grouped
Kestrel/browser path remain unproved.

`AddHtmxor()` registers Htmxor's server services. `AddHtmxorEndpoints()` adds
Htmxor's component endpoints to the mapped Razor component application. Neither
call installs, selects, or configures htmx; the application owns that runtime.
This naming change is the first bounded slice of #151. The second bounded slice
removes the incomplete client trigger, swap, and constants helpers from the
stable core. The issue remains open for the complete stable type allow-list,
exported-type and member review, and public-API compatibility baseline.

Supply htmx before the Htmxor adapter in `App.razor`:

```razor
<head>
    <script defer src="htmx-4.0.0.min.js"></script>
    <HtmxHeadOutlet />
    <HeadOutlet />
</head>
```

`HtmxHeadOutlet` adds the Htmxor static-web-asset adapter. The adapter binds
rendered component callbacks to the next request and carries the stock Blazor
antiforgery token for unsafe methods. It does not load or configure htmx.

## Choose which requests reach a component

### A normal page that also answers direct htmx GET

`@page` is the common case. It owns the normal Blazor route and, by v1
convention, makes the same component available to a direct htmx GET.

```razor
@page "/products/{Id:int}"

<article id="product">
    <h1>@product.Name</h1>
</article>

@code {
    [Parameter] public int Id { get; set; }
}
```

A browser navigation to `/products/42` renders the stock page and layout. An
htmx partial request to the same URL can render the component without the page
shell. Route parameters, query values, dependency injection, authorization
state, lifecycle methods, and static SSR rendering stay under Blazor.

### A normal-only page

V1 needs an explicit opt-out for a page that must not answer the conventional
direct GET. The marker and its name are not yet settled. Until they are, do not
invent an `hx-*` convention or use target headers as an opt-out. This gap belongs
in Htmxor, not in each application.

### An HTMX-only component route

A component without `@page` declares its route on that component:

```razor
@attribute [HtmxRoute("/products/{Id:int}")]

<article id="product">
    <h2>@product.Name</h2>
</article>

@code {
    [Parameter] public int Id { get; set; }
}
```

`HtmxRoute` may be placed in the `.razor` file, on the matching `.razor.cs`
partial, or on a component authored entirely in C#. It is component-specific;
do not place it in `_Imports.razor`. A headerless normal browser request does
not select this representation. Any HTTP client can forge `HX-*` headers and
thereby look like htmx, so the route must never be treated as an authentication
or authorization boundary.

GET is implicit for Razor-authored declarations. A C#-authored declaration must
specify its complete method allow-list because Razor callback inference is not
available:

```csharp
[HtmxRoute("/products/{id:int}", Methods = [HttpMethods.Get, HttpMethods.Put])]
public sealed partial class ProductCard : ComponentBase
{
}
```

An explicit `Methods` value is the complete allow-list. It does not add to an
inferred list.

## Declare component actions

The v1 method model is small:

| HTTP method | Server declaration | Status |
| --- | --- | --- |
| `GET` | Implicit for `@page` and Razor-authored `HtmxRoute` | Accepted v1 model |
| `POST` | Stock static-SSR form or statically discoverable `@onpost` | Accepted and proved in focused tests |
| `PUT` | Statically discoverable `@onput` | Accepted and proved in focused tests |
| `PATCH` | Statically discoverable `@onpatch` | Accepted and proved in focused tests |
| `DELETE` | Statically discoverable `@ondelete` | Accepted and proved in focused tests |
| `QUERY` | Application-authored `@onquery` | Proved for one binding per stock or HTMX-only route owner |
| Other methods | Narrow explicit declaration and extension API | Not implicit in v1 |

[#111](https://github.com/egil/Htmxor/issues/111) proves one statically discoverable
`@onquery` method-group binding on an `@page` and on an omitted-`Methods`
`HtmxRoute`. Its package-only test uses htmx 4.0.0, form-encoded content, real
Kestrel, and Chromium. It also proves that client-only `hx-query` cannot grant
the method. The slice does not cover JSON or other content types, large or
streaming bodies, cancellation, concurrent QUERY requests, several QUERY
bindings on one component, composition with an unsafe action, broad typed route
conversion, or reverse proxies.

An ordinary element callback keeps behavior near the markup:

```razor
@page "/todos/{TodoId:int}"

<button hx-delete="/todos/@TodoId"
        hx-target="#todo-@TodoId"
        hx-swap="delete"
        @ondelete="DeleteTodo">
    Delete
</button>

@code {
    [Parameter] public int TodoId { get; set; }

    private async Task DeleteTodo()
    {
        await store.DeleteAsync(TodoId);
    }
}
```

Use a parameterless callback when the operation needs only component state and
injected services. Accept `HtmxEventArgs` when the callback must inspect the
htmx request or set an htmx response header:

```razor
@code {
    private async Task Save(HtmxEventArgs e)
    {
        await store.SaveAsync(model);
        e.Response.Trigger("product:saved", new { model.Id });
    }
}
```

Callbacks execute on the request-created component instance. They are not
static endpoint handlers, and Htmxor does not ask the application to duplicate
the action in a controller or Minimal API.

The generator must diagnose dynamic or ambiguous bindings at build time and
point to the component, attribute, and remediation. A successful compile that
silently omits a route is not an acceptable fallback. The browser attributes
`hx-post`, `hx-method`, and `hx-query` may be checked for consistency, but none
of them is server authority.

### Progressive-enhancement forms

Keep the stock Blazor form and add htmx behavior:

```razor
<EditForm Model="model"
          FormName="create-product"
          method="post"
          action="/products"
          hx-post="/products"
          hx-target="#product-list"
          hx-swap="beforeend">
    <DataAnnotationsValidator />
    <InputText @bind-Value="model.Name" />
    <ValidationMessage For="() => model.Name" />
    <AntiforgeryToken />
    <button type="submit">Create</button>
</EditForm>
```

The normal form submission remains meaningful without JavaScript. Named-form
dispatch, `[SupplyParameterFromForm]`, `Input*`, validation, antiforgery,
authentication state, and lifecycle callbacks are Blazor features and must
behave the same on the enhanced path. All unsafe methods fail closed before
body binding or component callbacks. Do not change state on GET.

For a PUT, PATCH, or DELETE that is not expressed by an HTML form method, render
stock antiforgery credentials with an `EditForm` or `<AntiforgeryToken />`; the
adapter carries the request token. In htmx 4, DELETE request values are not sent
in the body by default, so do not treat body transport as proof of antiforgery.

## Select response fragments

`HtmxFragment` is the sole server-side selection boundary in v1. It may be
wrapperless, or it may emit a wrapper when `Element`, `Id`, or additional HTML
attributes are supplied:

```razor
<HtmxFragment Id="product-list" Element="ul" class="products">
    @foreach (var product in products)
    {
        <li>@product.Name</li>
    }
</HtmxFragment>
```

For a normal request the fragment participates in the full component output. A
direct request can select the whole component, one fragment, or several
fragments. The component and required ancestors may run, but excluded child
branches below a known selection boundary must not render or execute their own
lifecycle work.

The current API uses `Id`, request target matching, `Match`, and
`RenderDuringStandardRequest` for selection. That makes a DOM delivery detail
double as a server execution key and makes multi-fragment responses difficult to
read. The DX review proposes a stable fragment name that is independent of the
wrapper `Id`, plus one explicit response-level way to select multiple names.
The exact names remain to be decided; examples must not present a proposed
`Name` or `RenderFragments` member as shipped API.

Keep server execution separate from browser delivery:

| Concern | Owned by |
| --- | --- |
| Which child branches the server executes and emits | Named `HtmxFragment` selection |
| The primary DOM destination | `hx-target` or `HX-Retarget` |
| A subsection extracted from returned HTML | `hx-select` or `HX-Reselect` |
| Additional DOM destinations | `hx-swap-oob`, `hx-select-oob`, or `<hx-partial>` |
| Wrapper identity and CSS hooks | `HtmxFragment` `Element`, `Id`, and attributes |

Do not route capabilities by `HX-Target` or `HX-Source`. Those headers are
useful representation hints but are optional, forgeable, and unable to encode
all extended-selector cases.

### Multiple targets

Htmx 4 provides two application-authored delivery forms. Htmxor passes both
through unchanged.

Out-of-band markup identifies the destination on the returned element:

```razor
<HtmxFragment>
    <main id="results">@RenderResults()</main>
    <aside id="count" hx-swap-oob="outerHTML">@Count results</aside>
</HtmxFragment>
```

`<hx-partial>` separates the delivery selector and swap from its payload:

```razor
<HtmxFragment>
    <hx-partial hx-target="#results" hx-swap="innerHTML">
        @RenderResults()
    </hx-partial>
    <hx-partial hx-target="#count" hx-swap="innerHTML">
        @Count
    </hx-partial>
</HtmxFragment>
```

`<hx-partial>` is an htmx delivery envelope, not a second Htmxor fragment
concept. Htmx 4 processes the main swap before out-of-band and partial swaps.
For a pure OOB or partial response, suppress or empty the main swap according to
htmx's documented behavior.

## Inspect the request and shape the response

`HtmxContext` contains one request-scoped `HtmxRequest` and `HtmxResponse`. It is
available to component callbacks through `HtmxEventArgs` and may also be
injected or cascaded where the final v1 API permits.

### Request values

| Htmxor value | htmx input | Intended use |
| --- | --- | --- |
| `IsHtmxRequest` | Exactly one lowercase `HX-Request: true` after trimming HTTP spaces or tabs | Choose an HTML representation, never authorization |
| `RequestType` | `HX-Request-Type: full\|partial` | Decide stock page versus direct representation |
| `IsBoosted` | `HX-Boosted` | Preserve boosted navigation semantics |
| `IsHistoryRestoreRequest` | `HX-History-Restore-Request` | Return the representation history restoration expects |
| `CurrentUrl` | `HX-Current-URL` | Optional browser-location hint |
| `Source` | `HX-Source` | Optional `tag#id` source hint |
| `Target` | `HX-Target` | Optional `tag#id` target hint |
| `Method`, `Path` | HTTP request line | Bind the action to the normalized route and method |

Every header-derived value is untrusted. Htmxor recognizes a request only when
`HX-Request` contains exactly one value whose surrounding HTTP spaces or tabs
trim to lowercase `true`. Missing, blank, `false`, malformed, comma-joined, and
repeated markers make
`IsHtmxRequest` false, retain standard routing, and suppress all dependent
`HX-*` context. The current beta member is `CurrentURL`; later issue #154 slices
own its planned `CurrentUrl` rename and a clear API for additional protocol
headers.

### Response operations

The response API covers the core htmx 4 response headers plus HTTP status and
body control. The second bounded
[#154](https://github.com/egil/Htmxor/issues/154) slice makes these navigation
operations one current contract:

| Operation | Wire result | Component output | Use |
| --- | --- | --- | --- |
| `Location(string/Uri)` | `HX-Location: <uri-reference>` | Suppressed | Make a new htmx request without a full page reload |
| `PushUrl(string/Uri)` | `HX-Push-Url: <uri-reference>` | Kept | Push a history entry and process the returned component output normally |
| `PreventBrowserHistoryUpdate()` | `HX-Push-Url: false` | Kept | Prevent a history push while processing the returned component output |
| `Redirect(string/Uri)` | `HX-Redirect: <uri-reference>` | Suppressed | Perform a full browser navigation |
| `Refresh()` | `HX-Refresh: true` | Suppressed | Refresh the current page |
| `ReplaceUrl(string/Uri)` | `HX-Replace-Url: <uri-reference>` | Kept | Replace the current history entry and process the returned component output normally |
| `PreventBrowserCurrentUrlUpdate()` | `HX-Replace-Url: false` | Kept | Prevent current-URL replacement while processing the returned component output |

Each destination overload rejects null, blank, surrounding whitespace, control
characters, and malformed URI references; it does not trim or repair the input.
`PushUrl` and `ReplaceUrl` also reject the reserved history literals `true` and
`false`. A string is emitted exactly as supplied. A `Uri` is emitted through
`Uri.OriginalString`, preserving the caller's URI text rather than a normalized
`ToString()` value.

Every destination permits relative URI references. After resolution against the
active request, `Location`, `PushUrl`, and `ReplaceUrl` must use HTTP or HTTPS
with the same scheme, host, and effective port as that request. `Redirect`
deliberately permits a destination that resolves to cross-origin HTTP or HTTPS.
Every destination overload rejects a destination that resolves to a non-HTTP(S)
scheme.

Destination arguments are validated before Htmxor checks the request marker.
All seven operations then require exactly one lowercase `HX-Request: true` after
trimming surrounding HTTP spaces or tabs, and mutate no response state when
either check fails. A successful call returns the same `HtmxResponse` instance.

The last successful navigation call wins: it clears the other core navigation
headers before writing one exact value, and its automatic component-body effect
replaces the previous navigation operation's effect. `EmptyBody()` is
independent; once explicitly selected it continues to suppress component output
for that render even if a later push, replace, or prevent operation would
otherwise keep it. A subsequent component render on the same `HttpContext`,
including an error-handler re-execution, starts with fresh body-control state.
Before an unstarted suppressed response is written, Htmxor clears a positive
declared `Content-Length`; the suppressed `WriteAsync` overloads returning
`Task` and `ValueTask` preserve pre-canceled tokens. Navigation operations do
not change the response status code. Htmx does not process these response
headers on a 3xx response, so a response that expects htmx to act on one must
use an appropriate non-3xx status.

These are separate choices, not a chain; each line belongs in a different
callback or branch:

```csharp
args.Response.Location("/orders/42");
args.Response.Redirect(new Uri("https://idp.example/login"));
args.Response.ReplaceUrl("?page=2");
```

The other currently exposed server response operations are:

| Operation | Wire result | Use |
| --- | --- | --- |
| `Reswap(...)` | `HX-Reswap` | Override the swap style and modifiers |
| `Retarget(...)` | `HX-Retarget` | Override the target selector |
| `Reselect(...)` | `HX-Reselect` | Override response selection |
| `Trigger(...)` | `HX-Trigger` | Dispatch one or more client events with optional JSON details |
| `StatusCode(...)` | HTTP status | Select success, validation, handled-error, or no-content semantics |
| `EmptyBody()` | Empty HTTP body | Return only status, headers, cookies, and other metadata |

The second bounded #151 slice preserves `HtmxResponse.Trigger(...)`, raw
`HtmxResponse.Reswap(string)`, `SwapStyle`, and
`HtmxResponse.Reswap(SwapStyle, string?)` because they remain server-protocol
operations rather than client attribute-authoring helpers. The raw overload is
the escape hatch for application-selected or extension-provided swap values. The
first bounded #154 slice makes it use the same strict request guard as the other
covered operations.

The second bounded #154 slice removes `Location(LocationTarget)`,
`LocationTarget`, and `AjaxContext`; they did not model htmx 4 accurately, and no
replacement structured `HX-Location` model is added. `Reswap`, `Retarget`,
`Reselect`, trigger serialization, status 286/`StopPolling`, remaining request
parsing and naming, extension headers, and the complete protocol matrix remain
later #154 work.

## Htmx 4 attribute reference

Razor accepts ordinary htmx attribute names, including colon modifiers and
extension syntax. Prefer literal markup when the value is clear:

```razor
<div hx-confirm:inherited="Add this product?">
    <button hx-post="/cart"
            hx-vals:append="{ source: 'detail' }"
            hx-on:click="this.disabled = true">
        Add
    </button>
</div>
```

Use `@(...)` or an attribute value for C# interpolation. Htmxor should not reject
unknown attributes or newer extension values. Static analysis, when enabled,
must use a selected htmx profile and remain forward-compatible.

The second bounded #151 slice therefore removes the public `Constants`, the
`Trigger` facade, builders, and supporting types, `SwapStyleBuilder`,
`SwapStyleBuilderExtension`, and `ScrollDirection`, and the builder-based
`HtmxResponse.Reswap(...)` overload. `SwapStyleExtensions` becomes internal.
It adds no optional adapter: native Razor and raw string values are the
forward-compatible client surface. This is a package and markup-authoring
decision; it does not claim that any browser or extension behavior was executed.

The following tables cover every attribute in the official htmx 4.0.0 editor
metadata. The last column says what Htmxor or the application must do on the
server. Htmx still owns all DOM behavior.

### Requests, triggers, targeting, and swaps

| Attribute | htmx behavior | Server work |
| --- | --- | --- |
| `hx-get` | Issue GET | Use an `@page` or `HtmxRoute` GET. Keep GET side-effect free. |
| `hx-post` | Issue POST | Use a stock form or `@onpost`; render antiforgery credentials. |
| `hx-put` | Issue PUT | Declare `@onput`; unsafe and antiforgery-protected. |
| `hx-patch` | Issue PATCH | Declare `@onpatch`; unsafe and antiforgery-protected. |
| `hx-delete` | Issue DELETE | Declare `@ondelete`; unsafe and antiforgery-protected. DELETE values are not body data by default in htmx 4. |
| `hx-query` | Issue QUERY | Requires application-authored `@onquery`. Issue #111 proves one form-encoded binding per route owner; the client never grants the method. |
| `hx-action` | Set the request URL | Pair with `hx-method`. When present, `hx-action` takes precedence over verb-specific htmx attributes. It never grants the server route. |
| `hx-method` | Select GET, POST, PUT, PATCH, DELETE, or QUERY | The component declaration must independently allow the method. Useful for progressive markup resembling form `action`/`method`. |
| `hx-on` | Handle one or more htmx or DOM events | Client behavior only. Prefer external listeners where the application's CSP disallows inline script. |
| `hx-trigger` | Choose events, filters, polling, and modifiers | No server API is needed. Account for duplicate, aborted, delayed, or concurrent requests. |
| `hx-target` | Choose the primary DOM target | Does not select or authorize a server action. May inform representation; extended selectors need no element ID. |
| `hx-swap` | Choose insertion/morph/deletion and modifiers | Return compatible HTML. `HtmxResponse.Reswap` can override it. |
| `hx-select` | Extract a selector from the response | The server may return a full representation. htmx sends request type `full`; cache accordingly. |
| `hx-select-oob` | Extract additional OOB selectors | The response must include matching elements. This is client-side selection, not fragment execution control. |
| `hx-swap-oob` | Mark returned content for an OOB swap | Emit raw markup inside the selected server fragment. Preserve intended IDs/selectors. |
| `hx-status` | Override swap behavior by exact or wildcard HTTP status | Return honest HTTP statuses. Use this for validation/handled errors instead of flattening every response to 200. |

On a form, native `action` and `method` remain the no-JavaScript destination and
verb. `hx-action` and `hx-method` may select a different enhanced destination
and verb when htmx runs. Both server routes must be declared independently.
`hx-action` takes precedence over `hx-get`, `hx-post`, and the other
verb-specific htmx attributes on the same element.

`hx-trigger` supports ordinary events; comma-separated triggers; event filters;
`once`, `changed`, `delay`, `throttle`, `from`, `target`, `prevent`, `stop` or
`consume`, `halt`, `capture`, and `passive`; special `load`, `revealed`, and
`intersect` events; `root`, `rootMargin`, and `threshold` intersection options;
and `every` polling. Filters evaluate JavaScript and therefore conflict with a
strict no-eval CSP. Polling is just repeated component GETs: make callbacks
idempotent and choose explicit cache and concurrency behavior.

`hx-swap` supports `innerHTML`, `outerHTML`, `outerSync`, `before` or
`beforebegin`, `prepend` or `afterbegin`, `append` or `beforeend`, `after` or
`afterend`, `delete`, `none`, `innerMorph`, `outerMorph`, `textContent`, and
extension-provided styles. Its modifiers are `swap`, `settle`, `transition`,
`ignoreTitle`, `strip`, `focusScroll`, `swapEmpty`, `scroll`, `show`, and
`target`. Htmxor value helpers must not claim completeness unless they cover
this exact profile and allow unknown extension styles.

### Values, forms, request control, and UI state

| Attribute | htmx behavior | Server work |
| --- | --- | --- |
| `hx-vals` | Add request values using HCON/JSON or `js:` | Bind through stock query/form/component facilities. Validate input; do not bind domain entities blindly. |
| `hx-include` | Include values from selected elements | The resulting request is ordinary untrusted input. Extended selectors are client-only. |
| `hx-headers` | Add request headers using HCON/JSON or `js:` | Read application headers from `HttpContext`. Never use client headers as authorization evidence. |
| `hx-encoding` | Choose encoding, including `multipart/form-data` | File upload requires stock ASP.NET Core/Blazor limits and a proved multipart path; it is not yet a blanket v1 claim. |
| `hx-validate` | Run HTML constraint validation before sending | Server validation remains mandatory. Stock Blazor validation owns rendered messages. |
| `hx-config` | Set per-request timeout, credentials, cache, redirect, referrer, integrity, and validation | Htmxor sees the resulting HTTP request. htmx resets `mode` to its global value and does not allow an injected attribute to widen origin policy. |
| `hx-sync` | Coordinate requests with drop, abort, replace, or queue strategies | Server work can still race or be cancelled. Make state changes atomic/idempotent and honor cancellation where possible. |
| `hx-confirm` | Ask for confirmation before a request | Client UX only; never a server authorization or validation check. |
| `hx-indicator` | Apply the request class to selected indicators | Return stable indicator elements when a swap must preserve them. |
| `hx-disable` | Disable selected elements while in flight | Client duplicate-submit aid only. Server idempotency remains necessary. |
| `hx-ignore` | Exclude a subtree from htmx processing | Useful around untrusted or separately owned markup. The server emits it as ordinary HTML. |
| `hx-preserve` | Preserve a stable-ID element through swaps | Return the same stable ID. Useful for media and third-party widgets. |
| `hx-morph-skip` | Freeze an element during morphing | Emit it on markup whose identity/state must remain untouched. |
| `hx-morph-skip-children` | Morph attributes but preserve children | Useful at Blazor interactive or third-party DOM ownership boundaries. |
| `hx-boost` | Progressively enhance eligible links and forms | Same URL must return a full representation when htmx requests one. Preserve accessibility and normal navigation. |
| `hx-preload` | Preload content | Requires the preload extension. GET endpoints must be safe because preloading may run without a committed user action. |
| `hx-pending` | Show template content while a request is pending | Requires the pending extension; no special Htmxor route. |

`hx-vals`, `hx-headers`, and other inheritable attributes use explicit htmx 4
inheritance with the `:inherited` modifier. `:append` adds rather than replaces
an inherited value. The application may enable global implicit inheritance, but
Htmxor examples and conformance use htmx 4 defaults. Razor successfully treats
the colon forms as ordinary attributes; a configurable htmx meta character can
be used where another tool cannot.

### History and navigation

| Attribute | htmx behavior | Server work |
| --- | --- | --- |
| `hx-push-url` | Push `true`, `false`, or a URL | Return a restorable full-page URL. `PushUrl(...)` may override it with a validated URL; `PreventBrowserHistoryUpdate()` sends `false`. |
| `hx-replace-url` | Replace `true`, `false`, or a URL | Return a restorable full-page URL. `ReplaceUrl(...)` may override it with a validated URL; `PreventBrowserCurrentUrlUpdate()` sends `false`. |
| `hx-history-elt` | Select the element restored during history navigation | The server still returns the representation indicated by the request type. Keep its identity stable. |
| `hx-history` | Opt out of the history-cache extension with `false` | This belongs to the optional history-cache extension, not Htmxor authorization or caching. |

Htmx 4 fetches full-page content for history navigation when it needs the
server. Htmxor must interpret `HX-History-Restore-Request` and
`HX-Request-Type` together and avoid returning an isolated fragment where a full
document is required. Browser history storage and ASP.NET Core output caching
are separate systems.

### Extension-specific attributes

| Attribute | Extension | Htmxor composition |
| --- | --- | --- |
| `hx-sse:connect`, `hx-sse:close` | `hx-sse` | Application owns the SSE endpoint and extension. Streaming component responses are outside v1. |
| `hx-ws:connect`, `hx-ws:send` | `hx-ws` | Application owns the WebSocket endpoint and extension. This is not a generated component action. |
| `hx-head` | `hx-head` | Application owns the extension and full head-merging policy. `PageTitle` in returned HTML is the narrower core case. |
| `hx-browser-indicator` | `hx-browser-indicator` | Client-only browser loading indicator. |
| `hx-targets` | `hx-targets` | Sends one response to multiple matching targets. Select server output independently. |
| `hx-ptag` | `hx-ptag` | Add an explicit parser for its request headers and vary caches by any value that changes output. |
| `hx-nonce` | `hx-csp` | The application owns the CSP extension and creates trusted nonces. Never echo a client-supplied nonce. |
| `hx-live`, `hx-live:*` | `hx-live` | Client reactive behavior over returned DOM. Keep component/static-SSR and client state ownership explicit. |
| `hx-prompt` | `hx-prompt` | `HX-Prompt` is untrusted input; validate it like any other value. |

The editor metadata also documents `hx-preload`, `hx-pending`, and `hx-history`
with their extensions in the preceding tables. Unknown extension attributes,
colon namespaces, swap styles, events, headers, and elements must continue to
pass through.

## Common component recipes

These recipes use native htmx markup and the server APIs above. They add no
Htmxor helper types.

### Active search

Use a safe GET for an idempotent search:

```razor
<input name="q"
       hx-get="/products/search"
       hx-trigger="input changed delay:300ms"
       hx-target="#search-results"
       hx-sync="this:replace" />
<section id="search-results"></section>
```

Declare `/products/search` as an `@page` or HTMX-only GET component. Bind and
validate the query as ordinary untrusted input. `hx-sync` suppresses stale
client delivery; the server must still tolerate cancellation and overlap.

Use `hx-query` only when the component declares `@onquery` and the deployed
HTTP path accepts QUERY. The proved slice covers one form-encoded value on
Kestrel and Chromium, not reverse proxies or the broader request shapes listed
above.

### Lazy load and infinite scrolling

Use `load`, `revealed`, or `intersect` on a safe component GET:

```razor
<div hx-get="/products/page/2"
     hx-trigger="revealed"
     hx-swap="outerHTML">
    Loading...
</div>
```

The response can append rows and return the next sentinel. Do not use a GET that
records a business side effect merely because htmx may issue it only when the
sentinel becomes visible. The current `HtmxAsyncLoad` helper is not a frozen v1
requirement; raw markup is the baseline to beat.

### Polling

```razor
<output hx-get="/jobs/@JobId/status"
        hx-trigger="every 2s"
        hx-swap="outerHTML">
    @status
</output>
```

Regular polling repeats a safe GET. Load polling returns another element with a
`load delay:...` trigger and ends by omitting that trigger from the terminal
response. Htmxor should not expose a special polling status inherited from an
earlier htmx version unless exact htmx 4 evidence establishes it.

### Inline validation and handled errors

Return the stock Blazor validation markup with an honest status, then declare
the client delivery explicitly:

```razor
<EditForm Model="model"
          FormName="profile"
          method="post"
          hx-post="/profile"
          hx-target="this"
          hx-swap="outerHTML">
    ...
</EditForm>
```

The callback may set status 422 after normal validation. Htmx 4 swaps 422
responses by default, so this example needs no `hx-status` override. When an
application changes status handling, values such as `swap:outerHTML`,
`swap:none`, or `target:#validation-errors` must describe the intended action.
Unexpected exceptions remain errors; do not turn every failure into status 200
merely to force a swap.

### Delete a repeated row

Give the page route enough information to identify the component action, then
use a relative target only for delivery:

```razor
@attribute [HtmxRoute("/products/{ProductId:int}")]

<li id="product-@ProductId">
    @product.Name
    <button hx-delete="/products/@ProductId"
            hx-target="closest li"
            hx-swap="delete"
            @ondelete="DeleteProduct">
        Delete
    </button>
</li>

@code {
    [Parameter] public int ProductId { get; set; }

    private Task DeleteProduct() => products.DeleteAsync(ProductId);
}
```

A normal list page can render this `ProductRow` repeatedly. Its route and
callback grant DELETE. `closest li` is not a server key. Render a stock
antiforgery token in the static-SSR output and keep the action safe against
duplicate delivery.

### Boosted navigation and history

Put `hx-boost:inherited="true"` on a deliberate navigation region, or put
`hx-boost="true"` on each eligible link or form. Keep each link or form valid
without JavaScript. A boosted request may require a full
representation even though `HX-Request` is present. URLs pushed or replaced in
history must remain refreshable and authorized. Test history cache misses and
authentication redirects, not only the forward click.

### Server-triggered client behavior

Return HTML first, then use a namespaced event for application behavior:

```csharp
args.Response.Trigger("cart:changed", new { ItemCount = cart.Count });
```

```razor
<output hx-on:cart:changed="this.textContent = event.detail.itemCount"></output>
```

The JSON event detail is a public client contract. Prefer markup and ordinary
events over putting application logic into a growing set of Htmxor-specific
response methods.

## Core response shapes and HTTP behavior

### Ordinary fragment

Return the selected component or fragment markup. `hx-target`, `hx-swap`, and
optionally `hx-select` determine delivery.

### Full representation selected by the client

An `hx-select` or body-level request can ask for a full representation and then
extract a subsection. Honor `HX-Request-Type: full`; do not always assume an
`HX-Request` wants shell-free output.

### Out-of-band and partial response

Return the selected server fragment containing raw `hx-swap-oob` elements or
`<hx-partial>` envelopes. Main content is processed before additional targets in
htmx 4. For a response with no main payload, choose `hx-swap="none"`,
`swapEmpty`, or the matching global setting.

### Empty, no-content, validation, and error responses

Htmx 4 swaps response bodies for statuses by default except the configured
`noSwap` statuses, initially 204 and 304. Use honest status codes, `hx-status`,
or global `noSwap` configuration. A handled validation response can return
validation markup with its intended status; an unexpected exception remains an
error. `HtmxResponse.EmptyBody()` is distinct from status 204 because headers,
cookies, and status semantics still matter.

### Redirect and history responses

Choose among `HX-Location`, `HX-Redirect`, `HX-Refresh`, `HX-Push-Url`, and
`HX-Replace-Url` according to whether the browser should make an htmx request,
perform a full navigation, refresh, or mutate history while processing the
current response. `Location`, `Redirect`, and `Refresh` suppress component
output. Push, replace, and both prevent operations keep it. The last navigation
operation controls this automatic choice, while an explicit `EmptyBody()` stays
in effect independently during the current component render.

These operations leave the response status unchanged, and htmx does not process
their headers on a 3xx response. Test authentication challenges and return URLs;
URI validation and a response header are not substitutes for endpoint
authorization or an application decision that the destination is allowed.

Before adapting a stock local 302 response for direct htmx rendering, Htmxor
validates the destination and, when it is invalid, leaves the stock status and
`Location` unchanged. When the renderer handles `NavigationManager` with both
`ForceLoad` and `ReplaceHistoryEntry`, it preserves the required full load with
one `HX-Redirect`. It does not also emit `HX-Replace-Url`, and this bounded
decision does not claim `ReplaceHistoryEntry` parity in browser history.

## Htmx HTTP headers

### Request headers

| Header | Htmxor handling |
| --- | --- |
| `HX-Request` | Recognize exactly one value that equals lowercase `true` after trimming surrounding HTTP spaces or tabs; otherwise ignore dependent htmx context. |
| `HX-Request-Type` | Distinguish `full` and `partial` representations. |
| `HX-Boosted` | Preserve boosted navigation semantics. |
| `HX-Current-URL` | Expose as an optional, untrusted URI. |
| `HX-Source` | Expose the optional `tag#id` source hint. |
| `HX-Target` | Expose the optional `tag#id` target hint. |
| `HX-History-Restore-Request` | Return the correct restoration representation. |

Extensions can add headers such as `HX-Prompt` or `HX-PTag`; application headers
can be added with `hx-headers`. Htmxor needs a bounded extension mechanism but
must not elevate any of them to authentication or authorization evidence.

### Response headers

| Header | Htmxor operation |
| --- | --- |
| `HX-Location` | `HtmxResponse.Location(string/Uri)` |
| `HX-Push-Url` | `PushUrl(string/Uri)` or `PreventBrowserHistoryUpdate()` |
| `HX-Redirect` | `Redirect(string/Uri)` |
| `HX-Refresh` | `Refresh()` |
| `HX-Replace-Url` | `ReplaceUrl(string/Uri)` or `PreventBrowserCurrentUrlUpdate()` |
| `HX-Reswap` | `Reswap(...)` |
| `HX-Retarget` | `Retarget(...)` |
| `HX-Reselect` | `Reselect(...)` |
| `HX-Trigger` | `Trigger(...)` |

Use the static-SSR `HttpContext` for application headers, cookies, status codes,
and cache headers before the response starts. Htmxor does not need wrappers for
`Content-Language`, ETag, `Cache-Control`, or other general HTTP features.
The five navigation headers above are mutually exclusive: the last successful
navigation operation leaves exactly one of them on the response.

## Events, JavaScript, CSS, and configuration

Htmx owns the client features in this section. Htmxor returns markup that uses
them and listens to `htmx:config:request` in its adapter. The application owns
its scripts and content security policy.

### Event families

The official core event suffixes below use the `htmx:` prefix, for example
`htmx:before:request`:

| Family | Exact suffixes |
| --- | --- |
| Initialization and processing | `before:init`, `after:init`, `before:process`, `before:on:init`, `after:process`, `process:{type}`, `after:implicitInheritance`, `before:cleanup`, `after:cleanup` |
| Request | `config:request`, `confirm`, `before:request`, `before:response`, `after:request`, `response:error`, `finally:request`, `error` |
| Swap and settle | `before:swap`, `after:swap`, `finally:swap`, `before:settle`, `after:settle` |
| History | `before:history:update`, `after:history:update`, `after:history:push`, `after:history:replace`, `before:history:restore` |
| View transitions | `before:viewTransition`, `after:viewTransition` |
| Control and triggers | `abort` is listened for by htmx; `every` and `intersect` are trigger events |
`process:{type}` is a pattern whose final segment names a registered template
type. `after:implicitInheritance` is an internal debugging event that fires only
when `htmx.config.implicitInheritance` is enabled. V1 examples retain explicit
inheritance.


Official extensions add these `htmx:` event suffixes:

| Extension family | Exact suffixes |
| --- | --- |
| SSE | `sse:before:connection`, `sse:after:connection`, `sse:close`, `sse:error`, `sse:before:message`, `sse:after:message` |
| WebSocket | `ws:before:connection`, `ws:after:connection`, `ws:close`, `ws:error`, `ws:before:message:outgoing`, `ws:after:message:outgoing`, `ws:before:message:incoming`, `ws:after:message:incoming` |
| Download | `download:start`, `download:progress`, `download:complete` |
| Head | `head:before:merge`, `head:before:add`, `head:before:remove`, `head:after:merge` |
| History cache | `history:cache:before:save`, `history:cache:after:save`, `history:cache:miss`, `history:cache:hit`, `history:cache:before:restore`, `history:cache:after:restore` |
| CSP | `security:strip`, `security:violation` |
| Prompt | `prompt` |

Use `hx-on:<event>` or standard `addEventListener`. In Razor, colon event names
remain ordinary attributes. Server-triggered application events use
`HX-Trigger`; choose namespaced names such as `product:saved` and version the
event detail like any other client contract. Event context and cancellation are
defined by the selected htmx profile, not a Htmxor enum. Extension events are
client composition until an exact extension/browser profile is exercised.

### JavaScript API

The global htmx 4 API exposes `ajax`, `find`, `findAll`, `process`, `swap`,
`initialize`, `on`, `onLoad`, `trigger`, `registerExtension`, `parseInterval`,
and `timeout`, plus `htmx.config`. Use these directly in application JavaScript
or extensions. Htmxor does not wrap them. If application code inserts htmx
markup outside an htmx swap, that code must call `htmx.process`.

### Other client integrations

- Installation can use a vendored script, npm/bundler import, ES module, CDN,
  or the application-selected
  [`htmax.js`](https://four.htmx.org/docs/htmax) distribution. Htmxor v1
  evidence vendors the exact 4.0.0 asset; Htmxor neither chooses nor downloads
  it at runtime.
- `hx-on:*`, standard listeners, Alpine.js, `hx-live`, hyperscript, and other
  JavaScript libraries can all react to returned markup. The application owns
  script order, state, CSP, cleanup, and any Alpine compatibility extension.
- htmx processes open shadow roots and supports the `host` extended selector
  for web components. Targets outside a shadow root need the htmx-supported
  event or selector composition; Htmxor only returns HTML and cannot erase the
  browser's shadow-DOM boundary.
- CSS transitions use the htmx swap classes and matching stable IDs. View
  transitions use the `transition` swap modifier or global `transitions`
  configuration. Htmxor must preserve markup identity but does not own the
  animation.
- Official editor metadata is the preferred completion/reference source for
  client attributes. A Htmxor analyzer may add server-authority diagnostics but
  should not compete with or freeze that metadata.

### CSS classes

Core htmx adds or uses `htmx-added`, `htmx-indicator`, `htmx-request`,
`htmx-settling`, and `htmx-swapping`. The pending extension adds its own pending
state. Htmxor emits no competing UI framework; style these classes in the
application.

### Global configuration

| Configuration | Htmxor consequence |
| --- | --- |
| `logAll` | Useful for browser diagnostics; no server contract change. |
| `prefix` | Alternate `data-hx-` prefix; analyzers must respect the selected profile. |
| `transitions` | Client view-transition behavior only. |
| `history` | Changes history restoration/reload behavior; exercise the chosen mode. |
| `mode` | Defaults to same-origin and is security-sensitive. |
| `defaultSwap` | Changes client delivery; server still returns HTML. |
| `indicatorClass`, `requestClass`, `includeIndicatorCSS` | Application styling and CSP concern. |
| `defaultTimeout` | Client cancellation can leave server work in progress; actions must be safe. |
| `inlineScriptNonce` | Application CSP ownership. |
| `extensions` | Application-selected extension set. |
| `morphIgnore`, `morphScanLimit`, `morphSkip`, `morphSkipChildren` | Define DOM preservation during morphs. |
| `noSwap` | Defines statuses that skip a swap; keep HTTP status semantics honest. |
| `allowEmptySwapAfterOOB` | Controls whether a main swap follows pure additional content. |
| `implicitInheritance` | V1 examples use the htmx 4 default, explicit inheritance. |
| `defaultFocusScroll` | Client focus/scroll behavior and accessibility. |
| `defaultSettleDelay` | Client settle timing. |
| `metaCharacter` | Changes modifier syntax; analyzers/editor support must be profile-aware. |

Set initialization-only options (`prefix`, `extensions`, and `metaCharacter`)
through the htmx configuration meta tag before htmx initializes. The application
owns that tag. Htmxor's evidence uses defaults unless a test names an override.

## Official extension map

Extensions are application-owned scripts or modules loaded after htmx or
supplied by the `htmax.js` distribution described above. Htmxor must accept
their markup and headers, but support claims require executable evidence for the
exact extension and version.

| Official extension | Composition with Htmxor v1 |
| --- | --- |
| `hx-multipart` | Streams `multipart/mixed`. Streaming fragment responses are outside v1; use application endpoints unless separately proved. |
| `hx-sse` | Application-owned SSE endpoints can return HTML. Htmxor-generated component streaming is outside v1. |
| `hx-ws` | Application-owned WebSocket endpoints can send HTML. Component callback routes are ordinary HTTP, not sockets. |
| `hx-browser-indicator` | Pure client UX; compatible with returned markup. |
| `hx-live` | Pure client reactive scripting; define ownership at any interactive-Blazor boundary. |
| `hx-pending` | Pure client pending template behavior. |
| `hx-prompt` | Treat `HX-Prompt` as untrusted application input. |
| `hx-preload` | Requires safe, idempotent GET and appropriate caching. |
| `hx-history-cache` | Browser session cache is separate from server output caching and authentication. |
| `hx-ptag` | Poll tag/header needs application validation, protocol access, and cache-aware responses. |
| `hx-download` | Application endpoint owns file response/content disposition; not a component-fragment claim. |
| `hx-head` | Application owns head merge policy; test scripts, styles, canonical links, and deduplication. |
| `hx-targets` | One returned representation can update several nodes; server fragment selection stays independent. |
| `hx-upsert` | Extension swap style must pass through even when a typed helper does not know it. |
| `htmx-2-compat` | Not the v1 conformance profile; v1 evidence uses htmx 4 defaults. |
| `hx-alpine-compat` | Client interoperability; define Alpine, htmx, and interactive-Blazor DOM ownership. |
| `hx-csp` | Application owns nonce policy and strict-CSP setup; Htmxor must not weaken it. |

Third-party and future extensions follow the same rule: start with raw markup,
use application HTTP endpoints where needed, and add an Htmxor protocol API only
when the extension sends data the server must understand.

## Caching, security, and operational behavior

### Output caching

When one URL returns full and direct representations, vary the ASP.NET Core
output cache by every input that changes output. `HX-Request` is the minimum for
the simple two-representation case; htmx 4 request type, target, history restore,
boosting, selected fragment names, authentication, culture, tenant, query, and
application headers may also matter. Unavailable variation is not equivalent to
one cached representation being safe for all requests. Do not cache unsafe
methods.

### Security

- Treat `HX-*`, callback identifiers, selectors, URLs, and all form/query/header
  values as attacker-controlled.
- Re-evaluate authentication and authorization on every request and preserve the
  effective endpoint metadata on every generated representation.
- Validate antiforgery before binding or callbacks for POST, PUT, PATCH, DELETE,
  and every other unsafe method.
- Escape untrusted output. Isolate intentional raw HTML and consider
  `hx-ignore` where untrusted and trusted markup meet.
- Keep htmx's same-origin default. If the application deliberately changes the
  global fetch mode, require both the matching server CORS policy and an
  explicit browser destination allow-list such as CSP `connect-src`. Review
  which credentials and application data may leave the origin. An htmx
  attribute cannot widen the global fetch mode.
- Choose a CSP profile. Trigger filters, `js:` values, and inline
  `hx-on` require evaluated or inline script capabilities; strict-CSP apps should
  use external listeners and the application-owned `hx-csp` strategy where
  appropriate.
- Htmxor rejects malformed or non-HTTP(S) navigation destinations and restricts
  location and history operations to the active request origin. The application
  must still authorize which same-origin paths and deliberate cross-origin
  redirect destinations its own behavior may select.

### Concurrency and cancellation

`hx-sync`, disabled buttons, and client timeouts improve UX but do not serialize
the server. Component actions should tolerate duplicate delivery, cancellation,
late completion, and concurrent requests. Use application-level idempotency and
data-store concurrency controls for important state changes.

### Accessibility and progressive enhancement

Use semantic links, buttons, forms, labels, focus behavior, status/live regions,
and restorable URLs. Prefer stock form submission and link navigation as the
non-JavaScript baseline. After swaps, verify focus, validation summaries,
announcements, title changes, and keyboard behavior. Client convenience is not
evidence that the fallback works.

### Interactive Blazor coexistence

Normal pages may contain Interactive Server, WebAssembly, or Auto components.
A direct Htmxor response is static SSR and does not promise that a detached
fragment hydrates. Do not let htmx replace DOM owned by an interactive Blazor
root unless the application has an explicit integration contract. Use stable
boundaries, `hx-preserve`, morph-skip attributes, or targets outside the
interactive subtree. Enhanced Blazor navigation and htmx boosting also need one
declared owner in each navigation region.

## Diagnostics and release claims

For browser investigation, enable `htmx.config.logAll`, inspect the network
request and `HX-*` headers, listen for the request/swap events, and load the
unminified application-owned htmx asset when necessary. Server diagnostics
should identify the selected component route, normalized method, request type,
fragment names, and rejected declaration without logging sensitive form values
or authorization data.

Use these claim labels in documentation and issues:

| Label | Meaning |
| --- | --- |
| Accepted v1 contract | Required by the agreed goal but not necessarily implemented |
| Proved slice | Exercised by the exact command and artifact in the progress record |
| Client composition | Htmxor passes markup/protocol through; the application owns the htmx feature |
| DX proposal | Review recommendation requiring an approved API issue |
| Outside v1 | Deliberately excluded from the v1 release contract |
| Not yet exercised | Plausible or intended, but no release claim may be made yet |

The v1 release must identify the exact .NET target, Htmxor package, htmx 4.0.0
asset, browser/operating system, extensions, configuration, commands, and test
counts it exercised. In particular, raw extension compatibility, file upload,
history modes, streaming transports, interactive DOM coexistence, CSP profiles,
and performance are not proved merely because their markup compiles or passes
through.

## Current beta and planned v1

The current repository contains useful prototypes and evidence. The first two
bounded #151 slices make the registration names current and remove the
incomplete client trigger, swap, and constants helpers from the stable core. The
first two bounded #154 slices add the strict request classifier and the
navigation-response contract above, including removal of the inaccurate
structured location prototype. Other parts of the public API do not yet match
this guide:

- `HtmxRoute` exposes target/current-URL properties that the current source
  generator rejects on the v1 path;
- direct method inference and diagnostics cover only a limited set of Razor
  syntax;
- fragment selection currently couples `Id`, request-target matching, `Match`,
  and rendering flags rather than a clear named-selection model;
- some infrastructure/prototype types are public because of current assembly
  boundaries rather than an intentional stable developer contract; and
- several client compositions in this guide are not yet browser-conformance
  claims.

The companion [DX review](research/htmxor-v1-dx-review.md) turns those
observations into proposed v1 decisions and issue boundaries.
