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

The first bounded slice of
[#151](https://github.com/egil/Htmxor/issues/151) makes a consistent,
no-argument registration pair current.
[#145](https://github.com/egil/Htmxor/issues/145) proved the no-argument root and
standard route-group behavior retained by this pair:

```csharp
builder.Services
    .AddRazorComponents()
    .AddHtmxor();

app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddHtmxorEndpoints();
```

`AddHtmxor()` registers Htmxor's server services, and
`AddHtmxorEndpoints()` adds its component endpoints. Neither call installs,
selects, or configures htmx; the application supplies that runtime. The beta
does not retain forwarding aliases for the old names. This registration change
is the first bounded slice of #151. The second removes the incomplete client
trigger, swap, and constants helpers from the stable core. #151 remains open for
the complete stable allow-list, exported-type and member review, and public-API
compatibility baseline.

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

[#111](https://github.com/egil/Htmxor/issues/111) proves one statically discoverable
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

The sixth bounded [#154](https://github.com/egil/Htmxor/issues/154) slice makes
the request side current. `HtmxContext.Request` parses the seven core htmx 4
headers once. A single normalized lowercase `HX-Request: true` is required
before dependent context is exposed; request type recognizes only `full` and
`partial`, only `partial` selects direct routing, boolean boost/history fields
recognize only one lowercase `true`, `CurrentUrl` is one untrusted absolute
HTTP(S) `Uri`, and `Source`/`Target` retain one exact nonblank open string after
HTTP optional-whitespace trimming. Repeated, malformed, contradictory,
control-containing, and ill-formed values fail closed by field. Current-URL
route filters resolve relative declarations against the parsed URL, compare
scheme/host/port using URI rules, and keep path/query comparison ordinal and
case-sensitive. These headers remain representation hints, never route-method,
action, authorization, or antiforgery authority; the generated action identity
header remains internal and outside the seven-header surface.

The second bounded
[#154](https://github.com/egil/Htmxor/issues/154) slice makes the navigation
choices current:

| Operation | Wire result | Component output |
| --- | --- | --- |
| `Location(string/Uri)` | `HX-Location` | Suppressed |
| `PushUrl(string/Uri)` | `HX-Push-Url` | Kept |
| `PreventBrowserHistoryUpdate()` | `HX-Push-Url: false` | Kept |
| `Redirect(string/Uri)` | `HX-Redirect` | Suppressed |
| `Refresh()` | `HX-Refresh: true` | Suppressed |
| `ReplaceUrl(string/Uri)` | `HX-Replace-Url` | Kept |
| `PreventBrowserCurrentUrlUpdate()` | `HX-Replace-Url: false` | Kept |

These examples are separate choices, not a chain. Each line belongs in a
different callback or branch:

```csharp
args.Response.Location("/orders/42");
args.Response.Redirect(new Uri("https://idp.example/login"));
args.Response.ReplaceUrl("?page=2");
```

Destination overloads reject null, blank, surrounding whitespace, controls,
and malformed URI references without trimming or repairing the value. `PushUrl`
and `ReplaceUrl` also reject the reserved `true` and `false` history literals.
Strings are emitted exactly as supplied; `Uri` overloads use
`Uri.OriginalString`. Relative references are accepted. After resolution against
the request, location, push, and replace destinations must be same-origin
HTTP(S); `Redirect` also permits deliberate cross-origin HTTP(S). Destinations
that resolve to non-HTTP(S) schemes are rejected.

Arguments are validated before the strict htmx marker guard, and response state
changes only after both checks succeed. Successful calls return the same
`HtmxResponse` instance. The last navigation call clears the other navigation
headers, writes one exact value, and replaces the previous automatic navigation
body effect with its own. An explicit `EmptyBody()` remains independent from
that automatic effect during the current render. Suppression resets before a
later component render on the same `HttpContext`, including error-handler
re-execution. Before an unstarted suppressed response is written, Htmxor clears
a positive declared `Content-Length`; the suppressed `WriteAsync` overloads
returning `Task` and `ValueTask` preserve pre-canceled tokens. Navigation calls
do not change status, and htmx does not process these response headers on 3xx
responses.

For direct htmx rendering, Htmxor validates a stock local 302 redirect before
changing status or removing `Location`; an invalid stock redirect remains
unchanged. `ForceLoad` plus `ReplaceHistoryEntry` emits one `HX-Redirect` to
preserve the required full load and no conflicting `HX-Replace-Url`. This does
not establish `ReplaceHistoryEntry` parity in browser history.

Use the stock `HttpContext` for cookies, cache policy, general response headers,
and other ASP.NET Core behavior. The first bounded
[#154](https://github.com/egil/Htmxor/issues/154) slice recognizes only exactly
one `HX-Request` value whose surrounding HTTP spaces or tabs trim to lowercase
`true`. Missing, blank, `false`, malformed, comma-joined, and repeated markers
retain stock or not-found routing, ignore dependent htmx context, and cannot
mutate response headers, status, or body-control state through the covered
Htmxor operations. The fifth bounded #154 slice resolves the status question
through native htmx 4 polling: terminal replacement markup omits polling
attributes, and status 286 is not a stop signal. Request naming and remaining
value policies, extension headers, and the complete protocol matrix remain
later #154 work.

The third bounded #154 slice makes the three swap and selection response
operations current:

| Operation | Wire result | Body effect |
| --- | --- | --- |
| `Reswap(string)` | `HX-Reswap` | Unchanged |
| `Retarget(string)` | `HX-Retarget` | Unchanged |
| `Reselect(string)` | `HX-Reselect` | Unchanged |

Each argument is one complete open htmx or extension value. Htmxor rejects null,
empty, whitespace-only, surrounding whitespace, and values containing control
characters before the strict htmx marker guard. It does not trim, repair, or
parse accepted values through a closed grammar. Failure mutates nothing.
Success returns the same response, preserves the exact value, and overwrites
only the matching header, so the three different headers may coexist. These
calls do not change status, suppress component output, or reset suppression
previously selected by `EmptyBody()` or navigation.

## Write htmx as htmx

Literal htmx attributes are the main client API. Razor already accepts the htmx
4 colon forms and `<hx-partial>`. The application can use new client features
without waiting for a Htmxor package release.

Htmxor owns component routes, methods, callbacks, binding compatibility,
antiforgery, fragment execution, and typed response headers. Htmx owns triggers,
targets, selectors, swaps, indicators, confirmation, client events, and its
extensions. Htmxor enforces the navigation response's baseline URI, header, and
body rules. Destination authorization and broader navigation, cache, and error
policy remain application HTTP decisions.

SSE, WebSocket, and multipart streaming use application endpoints. Streaming
component responses remain outside v1. Other extensions pass through as markup
unless their protocol needs a small server hook.

The second bounded #151 slice removes the public `Constants`, the `Trigger`
facade, builders, and supporting types, `SwapStyleBuilder`,
`SwapStyleBuilderExtension`, `ScrollDirection`, and the builder-based
`HtmxResponse.Reswap(...)` overload from the stable core. Native Razor and raw
strings are the client surface; this slice does not add an optional adapter
package.

Server-protocol operations remain distinct. The fourth #154 slice writes one
compact `HX-Trigger` JSON object. It safely encodes exact, case-sensitive,
well-formed UTF-16 event names, appends distinct calls in call and wire-member
order, and replaces a later duplicate's detail at its first position. That
deterministic wire text is not a JSON semantic-order promise; JavaScript
enumeration can reorder integer-like property names. Missing details and
details that serialize to JSON null use `{}` because exact htmx 4.0.0
dereferences JSON-null detail and does not dispatch it. Application
`JsonOptions` or a per-call `JsonSerializerOptions` control the detail data
contract; Htmxor owns final compact, header-safe encoding. A detail member named
`target` retains its htmx dispatch-target meaning.

Invalid input, serialization failure, or marker failure leaves response state
unchanged. The first successful Htmxor call replaces a manual `HX-Trigger`;
subsequent calls merge only Htmxor-owned events. Success preserves fluent
identity, status, and body behavior. The removed htmx 2 timing overload and
timed headers stay removed.

The fifth bounded #154 slice removes the obsolete `StopPolling` and
`HtmxStatusCodes` surface. htmx 4 polling stops through terminal replacement
markup that omits `hx-get` and `hx-trigger`, as described by the [official
polling guide](https://four.htmx.org/patterns/polling). The exact [htmx 4.0.0
source](https://github.com/bigskysoftware/htmx/blob/v4.0.0/src/htmx.js) has no
status-286 stop rule. `StatusCode(HttpStatusCode)` remains general application
HTTP status control, including a deliberate numeric 286. Focused and packed
public-surface checks plus the Production Kestrel/Chromium proof cover the API
and browser boundary.

The third #154 slice retains raw
`HtmxResponse.Reswap(string)` beside `Retarget(string)` and `Reselect(string)`,
but removes `SwapStyle`, its typed overload, and its converter rather than
turning an incomplete htmx 4 profile into a stable promise. The second #154
slice removes `Location(LocationTarget)`, `LocationTarget`, and `AjaxContext`
because they did not accurately model htmx 4 and had no maintained consumer
proving their value. `Location(Uri)` joins `Location(string)`; no replacement
structured `HX-Location` model is introduced. These package decisions do not
claim malformed-marker browser behavior or compatibility with an unexecuted
extension. Separately packed consumers prove exact `Location(Uri)` wire text
and `HX-Location` plus an empty body for an actionless generated route; neither
proof is a browser execution. Application-owned htmx 4.0.0 browser evidence
does consume `HX-Reswap`, `HX-Retarget`, and `HX-Reselect` together with core
values and proves the resulting target and DOM shape.

## V1 decisions still open

- [#151: freeze the v1 public API](https://github.com/egil/Htmxor/issues/151)
- [#152: make routes and actions explain themselves](https://github.com/egil/Htmxor/issues/152)
- [#153: separate fragment selection from DOM delivery](https://github.com/egil/Htmxor/issues/153)
- [#154: finish the remaining htmx 4 request and response API](https://github.com/egil/Htmxor/issues/154)

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
