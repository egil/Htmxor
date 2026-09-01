# Htmxor

[add introduction]

## V1 design

The [v1 guide and htmx 4 map](htmxor-v1-feature-guide.md) documents the current
registration pair and navigation-response contract and labels the remaining v1
APIs by status. The
[developer experience review](research/htmxor-v1-dx-review.md) explains the
remaining API decisions and links the issues that track them.

## Getting Started with the unreleased v1 API

The setup below targets the v1 API in the repository revision containing this
page. It does not compile against the currently published beta package.

To create a minimal Blazor + htmx app with various examples, download the [Minimal Htmxor App template](https://github.com/egil/Htmxor/tree/main/samples/MinimalHtmxorApp).

Htmxor v1 requires .NET 10. It does not support .NET 8, and it does not yet claim .NET 11 compatibility.

The application supplies and configures the htmx runtime; Htmxor does not
distribute one. The [v1 progress record](roadmap/v1/progress.md) identifies the
exact application-owned htmx 4.0.0 request and navigation operations exercised
by current browser evidence and the dimensions that remain unproved.

To start fresh from a (new) Blazor Web App project, follow these steps:

1. **Build and add the Htmxor package**

   From the Htmxor repository root, build a local package with a known version:

   ```console
   dotnet pack src/Htmxor/Htmxor.csproj --configuration Release --output artifacts/packages -p:MinVerVersionOverride=1.0.0-local.1
   ```

   From the application project directory, add that package. Replace the source
   path with the absolute path to the repository's `artifacts/packages`
   directory:

   ```console
   dotnet add package Htmxor --version 1.0.0-local.1 --source /absolute/path/to/Htmxor/artifacts/packages
   ```

   The published packages on [NuGet](https://www.nuget.org/packages/Htmxor)
   expose the previous beta registration API until v1 is published.


2. **Update `Program.cs`**

   Modify `Program.cs` to include Htmxor services and endpoint mapping:

    ```diff
      var builder = WebApplication.CreateBuilder(args);

      // Add services to the container.
      builder.Services
          .AddRazorComponents()
    +     .AddHtmxor();

      var app = builder.Build();

      // Configure the HTTP request pipeline.
      if (!app.Environment.IsDevelopment())
      {
          app.UseExceptionHandler("/Error", createScopeForErrors: true);
          app.UseHsts();
      }

      app.UseHttpsRedirection();

      app.UseAntiforgery();
      app.MapStaticAssets();
      app.MapRazorComponents<App>()
    +    .AddHtmxorEndpoints();

      app.Run();
    ```

   These calls register Htmxor's server integration and component endpoints.
   They do not install, select, or configure the application-owned htmx runtime.

   Keep the stock `MapStaticAssets()` call. Htmxor uses ASP.NET Core static web
   assets for its adapter and does not require a separate file provider or
   custom asset pipeline. Verify Production behavior from published output;
   changing the environment of an unpublished source-tree run is not the same
   deployment boundary.
3. **Supply the htmx runtime**

   Install the exact `htmx.org@4.0.0` package with your JavaScript package
   manager, then copy `node_modules/htmx.org/dist/htmx.min.js` to
   `wwwroot/htmx-4.0.0.min.js`. Verify that the copied file has SHA-256
   `E484D9171A9DB30A39C8F16E3D709D4137F3211C659F8E6125816635033D593F`.
   Its Zero-Clause BSD license must accompany the application. The
   [package-browser fixture](../test/Htmxor.Quality.Tests/Htmx4PackageBrowser)
   records the source archive, license, and exact asset used by current
   browser evidence.

   Unsafe components render stock Blazor antiforgery credentials through an
   `EditForm` or `<AntiforgeryToken />`. The adapter sends that request token
   through htmx 4's request context; ASP.NET Core owns the antiforgery cookie
   and validates the request before component callbacks run. Htmxor does not
   require a separate antiforgery middleware or readable request-token cookie.

4. **Update App.razor**

   Add the application-owned runtime and the Htmxor adapter to `App.razor`:

    ```diff
      <!DOCTYPE html>
      <html lang="en">

      <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <base href="/" />
          <link rel="stylesheet" href="@Assets["bootstrap/bootstrap.min.css"]" />
          <link rel="stylesheet" href="@Assets["app.css"]" />
          <link rel="stylesheet" href="@Assets["MinimalHtmxorApp.styles.css"]" />
          <link rel="icon" type="image/png" href="@Assets["favicon.png"]" />
    +     <script defer src="htmx-4.0.0.min.js"></script>
    +     <HtmxHeadOutlet />
          <HeadOutlet />
      </head>

      <!--
        Adding hx-boost:inherited="true" is optional.
        hx-boost returns the "enhanced navigation" and "enhanced forms"
        features that are lost by removing blazor.web.js script below.
        Learn more here: https://four.htmx.org/reference/attributes/hx-boost
      -->
    +     <body hx-boost:inherited="true">
          <Routes />

    -     <script src="_framework/blazor.web.js"></script>
      </body>

      </html>
    ```

   Preserve the stock `@Assets[...]` references when adding Htmxor. In a
   published Production app, ASP.NET Core continues to emit and serve their
   fingerprinted URLs. `HtmxHeadOutlet` adds only Htmxor's
   `_content/Htmxor/htmxor.js` adapter; the application continues to own the
   htmx runtime and its application assets.

5. **Create an Optional Direct Request Layout**

   Optionally, create a layout that will be used during [direct routing](#direct-routing), e.g., `/Components/Layout/HtmxorLayout.razor`:

    ```razor
    @inherits HtmxLayoutComponentBase
    @Body
    ```

    The `HtmxLayoutComponentBase` includes the `<HeadOutlet>` component. This makes it possible to use the `<PageTitle>` component during htmx requests to update the page title.

6. **Update _Imports.razor (Optional)**

   Modify _Imports.razor to include Htmxor namespaces and set a default layout:

    ```diff
      @using System.Net.Http
      @using System.Net.Http.Json
      @using Microsoft.AspNetCore.Components.Forms
      @using Microsoft.AspNetCore.Components.Routing
      @using Microsoft.AspNetCore.Components.Web
      @using static Microsoft.AspNetCore.Components.Web.RenderMode
      @using Microsoft.AspNetCore.Components.Web.Virtualization
      @using Microsoft.JSInterop
    + @using Htmxor.Components
    + @using Htmxor.Http
    + @using Htmxor

    + @* only if adding a custom layout for using during direct requests in step 4 above *@
    + @attribute [HtmxLayout(typeof(HtmxorLayout))]
    ```

    Note that we set up the custom layout for all components by defining the `[HtmxLayout(typeof(HtmxorLayout))]` attribute in the `_Imports.razor` file.

## Htmx navigation responses

Inside a component callback, select one navigation response according to the
browser behavior the application needs. These examples are separate choices,
not a fluent chain; each line belongs in a different callback or branch:

```csharp
args.Response.Location("/orders/42");
args.Response.Redirect(new Uri("https://idp.example/login"));
args.Response.ReplaceUrl("?page=2");
```

The current navigation operations are:

| Operation | Response header | Component output |
| --- | --- | --- |
| `Location(string/Uri)` | `HX-Location` | Suppressed |
| `PushUrl(string/Uri)` | `HX-Push-Url` | Kept |
| `PreventBrowserHistoryUpdate()` | `HX-Push-Url: false` | Kept |
| `Redirect(string/Uri)` | `HX-Redirect` | Suppressed |
| `Refresh()` | `HX-Refresh: true` | Suppressed |
| `ReplaceUrl(string/Uri)` | `HX-Replace-Url` | Kept |
| `PreventBrowserCurrentUrlUpdate()` | `HX-Replace-Url: false` | Kept |

Destination overloads reject null, blank, surrounding whitespace, control
characters, and malformed URI references without trimming or repairing them.
`PushUrl` and `ReplaceUrl` also reject the reserved history literals `true` and
`false`. String overloads preserve their exact text. `Uri` overloads emit
`Uri.OriginalString` rather than a normalized `ToString()` value.

Relative URI references are accepted. After resolution against the active
request, destinations for `Location`, `PushUrl`, and `ReplaceUrl` must be
same-origin HTTP(S), using that request's scheme, host, and effective port.
`Redirect` also permits a deliberate destination that resolves to cross-origin
HTTP(S). Destinations that resolve to non-HTTP(S) schemes are rejected. Htmxor
enforces these baseline rules; the application still decides and authorizes
which destinations its behavior may select.

Destination arguments are validated first. Htmxor then requires exactly one
lowercase `HX-Request: true` value after trimming surrounding HTTP spaces or
tabs, and only then changes the response. A failed argument or marker check
changes no response state. Every successful call returns the same
`HtmxResponse` instance.

Navigation operations are last-call-wins: a successful call clears the other
navigation headers, writes one exact value, and replaces any earlier automatic
navigation body effect with its own. `EmptyBody()` is independent, so an
explicit empty-body choice remains in effect after a later push, replace, or
prevent operation during the current component render. Suppression state resets
before another component render on the same `HttpContext`, including an error
handler's component re-execution. Before an unstarted suppressed response is
written, Htmxor clears a positive declared `Content-Length`; the suppressed
`WriteAsync` overloads returning `Task` and `ValueTask` preserve pre-canceled
tokens. Navigation operations do not change the status code. Htmx does not
process these response headers on 3xx responses.

For direct htmx rendering, Htmxor validates a stock local 302 redirect before
changing its status or removing `Location`; an invalid redirect remains a stock
response. A `NavigationManager` command that combines `ForceLoad` with
`ReplaceHistoryEntry` produces one `HX-Redirect` to preserve the required full
load. It does not also emit `HX-Replace-Url`, and Htmxor does not claim
replace-history parity for that combination.

The earlier `Location(LocationTarget)` overload and its `LocationTarget` and
`AjaxContext` types have been removed because they did not model htmx 4
accurately. No replacement structured `HX-Location` model is included in this
slice.

## Htmx swap and selection responses

One component response may override all three parts of a swap. Unlike navigation
operations, these calls may be chained because their headers coexist:

```csharp
args.Response
    .Reswap("outerHTML settle:25ms")
    .Retarget("#orders")
    .Reselect("[data-order]");
```

`Reswap(string)`, `Retarget(string)`, and `Reselect(string)` each accept one
complete open htmx or extension-defined value. Htmxor does not parse the value
through a closed swap-style or selector grammar, and it emits valid input
exactly as supplied. The public `SwapStyle` enum, its typed `Reswap` overload,
and the converter have been removed because they represented only part of htmx
4 and could not represent extension-defined values.

Each call rejects null, empty, whitespace-only, surrounding-whitespace, and
control-character input before checking for exactly one normalized
`HX-Request: true` marker. A failed validation or marker check changes no
response state. A successful call returns the same `HtmxResponse`, replaces any
earlier value for its own header, and leaves the other two headers, all unrelated
headers, status, and the current body-control choice unchanged. The three calls
retain component output unless `EmptyBody()` or a suppressing navigation
operation already selected an empty body.

## Application response headers

A static SSR component can set an application response header through the
standard cascading `HttpContext`. Do so during parameter or initialization
lifecycle work, before the response starts:

```razor
@using Microsoft.AspNetCore.Http
@using System.Globalization

@code {
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string SelectedLanguage => CultureInfo.CurrentUICulture.Name;

    protected override void OnParametersSet()
    {
        if (HttpContext is not null && !HttpContext.Response.HasStarted)
        {
            HttpContext.Response.Headers.ContentLanguage = SelectedLanguage;
        }
    }
}
```

The application remains responsible for choosing and validating the value and
for any cache policy affected by it. The cascading `HttpContext` is available
during static SSR; do not rely on it in interactive rendering or attempt to
change headers after the response has started. Htmxor preserves headers written
this way on both the stock full-page GET and direct shell-free GET paths, so an
additional Htmxor response-header API is not needed for this application-owned
case.

## Output caching

For the bounded case where one component URL returns the stock full page when
`HX-Request` is not exactly one lowercase `true` value after HTTP optional
whitespace trimming, the stock htmx full representation when a valid marker has
request type `full`, and the direct component representation when a valid marker
has request type `partial`, include both `HX-Request` and `HX-Request-Type` in
the ASP.NET Core OutputCache key. Configure the standard component attribute
together with the OutputCache services and middleware:

```csharp
builder.Services.AddOutputCache();

// Authentication and authorization middleware must run before OutputCache.
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
```

```razor
@using Microsoft.AspNetCore.OutputCaching
@attribute [OutputCache(VaryByHeaderNames = ["HX-Request", "HX-Request-Type"])]
@page "/cached-component"
```

The standard `VaryByHeaderNames` configuration distinguishes normal, htmx full,
and htmx partial representations. `CacheOutput(policy =>
policy.SetVaryByHeader("HX-Request", "HX-Request-Type"))` is its endpoint-
policy equivalent. An application that also varies output for boosted requests,
targets, history restoration, selected fragments, authentication, or other
request data must add every such input to its cache policy. Htmxor does not infer
an application's complete cache key.

ASP.NET Core OutputCache caches safe requests by default; do not opt unsafe
component methods into output caching. Htmxor does not emit an antiforgery
cookie for a safe GET that contains no stock antiforgery component.

## Routing in Htmxor

Htmxor supports two routing modes for Blazor static SSR:

- **Standard routing**
- **Direct routing**

The routing mode is determined by the htmx 4
[`HX-Request`](https://four.htmx.org/reference/#headers) and
`HX-Request-Type` headers together:

```text
if ( HX-Request has exactly one value
     && removing surrounding HTTP spaces or tabs produces exactly "true"
     && HX-Request-Type has exactly one value
     && that value is exactly "partial" )
    RoutingMode.Direct
else
    RoutingMode.Standard
```

Here's a detailed look at each mode:

### Standard Routing

Standard routing is used for a normal browser request, for a request whose
`HX-Request` marker is missing, blank, `false`, malformed, comma-joined, or
repeated, and for an htmx request whose `HX-Request-Type` is missing, invalid,
repeated, or exactly `full`. Dependent `HX-*` values do not change the result
when the request marker is invalid.

In this mode, routing behaves like conventional Blazor Static Web Apps routing. The root component (typically App.razor or the component passed to `MapRazorComponents<TRootComponent>()` in `Program.cs`) is rendered.

The root component usually renders a `<Router>` component that determines which `@page`-annotated component to render based on the HTTP request, using the layout specified for that page.

Example:

```
HTTP GET /my-page
App --> Routes --> MainLayout --> MyPage
```

### Direct Routing

Direct routing is selected only when `HX-Request` contains one value whose
surrounding HTTP spaces or tabs trim to lowercase `true` and the single
`HX-Request-Type` value is exactly `partial`.
It bypasses the root component
(`App.razor`) and the standard layout (`MainLayout`). Instead, it routes
directly to the component that matches the request.

If the target component has a `HtmxLayout` attribute, that layout is rendered first.

Example:

```
HTTP GET /my-htmx-page-with-layout
HtmxLayout --> MyHtmxPageWithHtmxLayout

HTTP GET /my-htmx-page
MyHtmxPage
```

This allows `MyHtmxPage` to be rendered directly, optionally including a specified `HtmxLayout`.

## Conditional Rendering aka. Template Fragments

In Htmxor, conditional rendering supports the [template fragments](https://htmx.org/essays/template-fragments/) pattern.

It allows a single routable component to render specific parts for particular requests or the full content for others. This way, you can keep all related fragments within a single component, avoiding splitting them into separate, individually routable components.

By consolidating the HTML into one file, it becomes easier to understand feature functionality, adhering to the [Locality of Behavior](https://htmx.org/essays/locality-of-behaviour/) design principle.

### Enabling Conditional Rendering

TODO:

- ConditionalComponentBase
- IConditionalRender

## Layouts

TODO: 

- HtmxLayout

## Events Handlers

TODO:

- How handlers are associated with requests
