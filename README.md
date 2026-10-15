# Htmxor - supercharging Blazor Static SSR with Htmx
![Htmxor logo](https://github.com/egil/Htmxor/blob/main/docs/htmxor-x.svg)

This package integrates HTMX requests with Blazor static SSR components.

Htmxor v1 targets .NET 10 and .NET 11 under the approved [compatibility work](https://github.com/egil/Htmxor/issues/207). The runtime package contains both target assets; .NET 11 compatibility remains limited to the scenarios and exact prerelease versions exercised by that work. Existing .NET 8 applications can continue to use the previous beta package.

The application supplies and configures the htmx runtime. The unreleased v1 API
in this repository registers only Htmxor's server integration and component
endpoints:

```csharp
builder.Services.AddRazorComponents().AddHtmxor();
app.MapRazorComponents<App>().AddHtmxorEndpoints();
```

Author client behavior with native Razor `hx-*` attributes and literal htmx
values. Htmxor does not expose a version-bound trigger or swap-builder DSL;
server response operations such as `HtmxResponse.Trigger(...)` and
the open-string `Reswap(string)`, `Retarget(string)`, and `Reselect(string)`
operations remain available.

Successful `Trigger(...)` calls merge exact, case-sensitive event names into
one compact `HX-Trigger` JSON object. Event-detail data uses the application's
ASP.NET Core JSON options by default, with a per-call `JsonSerializerOptions`
override; Htmxor owns final header-safe encoding. See the
[v1 guide](docs/htmxor-v1-feature-guide.md#trigger-response-events) for merge,
validation, encoding, and no-detail behavior.

For htmx 4 polling, return replacement markup without `hx-get` and
`hx-trigger` when the work is complete. Htmxor deliberately has no
`StopPolling` or status-286 helper; `HtmxResponse.StatusCode(HttpStatusCode)`
remains general HTTP status control for valid htmx requests and still requires
the strict `HX-Request: true` marker. For a normal request, use
`HttpContext.Response.StatusCode`; either path may deliberately use numeric 286.
See the [native polling guidance](docs/htmxor-v1-feature-guide.md#polling).

Blazor Static SSR comes with basic interactivity via enhanced navigation and enhanced form handling.
Adding Htmx (htmx.org) to the mix gives you access to another level of interactivity while still
retaining all the advantages of Blazor SSR stateless nature.

Use the [source-package instructions](https://github.com/egil/Htmxor/blob/main/docs/index.md#getting-started-with-the-unreleased-v1-api)
to try these calls. The [published NuGet package](https://www.nuget.org/packages/Htmxor)
is the previous beta and exposes the previous registration names.

## Documentation

See https://github.com/egil/Htmxor/blob/main/docs/index.md.

For the unreleased v1 source package, unsupported route and action declarations
fail the build with cause-specific `HTMXOR004`–`HTMXOR043` errors at their source
locations. The diagnostic names the unavailable route or action and the reason;
for example, `HTMXOR005` identifies a malformed route declaration and
`HTMXOR042` identifies a compiled Razor binding without a generated action.
`HTMXOR003` continues to identify invalid normal-only route declarations.
Compiler- and Razor-owned errors retain their own diagnostic IDs.

Stable source-package diagnostic IDs:

| ID | Cause |
| --- | --- |
| `HTMXOR003` | Unsupported normal-only route declaration |
| `HTMXOR004` | Unsupported HTMX-only route target or origin |
| `HTMXOR005` | Malformed HTMX-only route member or method |
| `HTMXOR006` | Inferred action uses a lambda or closure |
| `HTMXOR007` | Inferred action uses an EventCallback field |
| `HTMXOR008` | Inferred action uses an EventCallback parameter |
| `HTMXOR009` | Inferred action uses a method call |
| `HTMXOR010` | Inferred action uses a computed expression |
| `HTMXOR011` | Inferred action uses an overloaded handler |
| `HTMXOR012` | Inferred action uses a generic handler |
| `HTMXOR013` | Action handler is static |
| `HTMXOR014` | Inferred action has no route owner |
| `HTMXOR015` | GET action binding is redundant |
| `HTMXOR016` | Action handler returns ValueTask |
| `HTMXOR017` | Explicit action handler is overloaded |
| `HTMXOR018` | Action handler is async void |
| `HTMXOR019` | Action handler returns Task of a value |
| `HTMXOR020` | Action handler accepts EventArgs |
| `HTMXOR021` | Action handler accepts object |
| `HTMXOR022` | Explicit action handler is generic |
| `HTMXOR023` | Action handler has a ref or out parameter |
| `HTMXOR024` | Action handler has an optional parameter |
| `HTMXOR025` | Action handler has unsupported parameters |
| `HTMXOR026` | Action handler returns an unsupported value |
| `HTMXOR027` | EditForm has no name |
| `HTMXOR028` | POST form has no name |
| `HTMXOR029` | Explicit action uses an unsupported method |
| `HTMXOR030` | Explicit action has no local route owner |
| `HTMXOR031` | Route method has conflicting handlers |
| `HTMXOR032` | Named form conflicts with POST action |
| `HTMXOR033` | Action or form is outside explicit route methods |
| `HTMXOR034` | Action is on a normal-only component |
| `HTMXOR035` | Explicit action is not an ordinary method |
| `HTMXOR036` | Inferred binding is outside owner markup |
| `HTMXOR037` | Inferred binding has no matching Razor owner |
| `HTMXOR038` | Local page binding conflicts with compiled route |
| `HTMXOR039` | Stock route binding has no local page |
| `HTMXOR040` | Action handler is not a component member |
| `HTMXOR041` | Action handler is not a method |
| `HTMXOR042` | Compiled binding has no matching action |
| `HTMXOR043` | Compiled form has no matching POST inference |

## Samples

The following Blazor Web Apps (Htmxor) are used to test Htmxor and demo the capabilities of it.

- [Blazing Pizza workshop as Htmxor App](https://github.com/egil/Htmxor/tree/main/samples/BlazingPizza)
- [Htmxor Examples](https://github.com/egil/Htmxor/tree/main/samples/HtmxorExamples)
- [Minimal Htmxor App template](https://github.com/egil/Htmxor/tree/main/samples/MinimalHtmxorApp)
