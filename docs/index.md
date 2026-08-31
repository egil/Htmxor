# Htmxor

[add introduction]

## V1 design

The [v1 guide and htmx 4 map](htmxor-v1-feature-guide.md) describes the planned
API. It is not documentation for the current beta. The
[developer experience review](research/htmxor-v1-dx-review.md) explains the
remaining API decisions and links the issues that track them.

## Getting Started

To create a minimal Blazor + htmx app with various examples, download the [Minimal Htmxor App template](https://github.com/egil/Htmxor/tree/main/samples/MinimalHtmxorApp).

Htmxor v1 requires .NET 10. It does not support .NET 8, and it does not yet claim .NET 11 compatibility.

The application supplies and configures the htmx runtime; Htmxor does not
distribute one. Current browser evidence covers application-owned htmx 4.0.0
GET, POST, PUT, PATCH, and DELETE paths described in the
[v1 progress record](roadmap/v1/progress.md).

To start fresh from a (new) Blazor Web App project, follow these steps:

1. **Add the Htmxor Package**

   Install the [Htmxor package from NuGet](https://www.nuget.org/packages/Htmxor).


2. **Update `Program.cs`**

   Modify `Program.cs` to include Htmxor services and middleware:

    ```diff
      var builder = WebApplication.CreateBuilder(args);

      // Add services to the container.
      builder.Services
          .AddRazorComponents()
    +     .AddHtmx();

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
    +    .AddHtmxorComponentEndpoints();

      app.Run();
    ```

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

   Optionally, create a layout that will be used during [direct routing](htmxor-v1-feature-guide.md#a-normal-page-that-also-answers-direct-htmx-get), e.g., `/Components/Layout/HtmxorLayout.razor`:

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
    + @using static Htmxor.Constants

    + @* only if adding a custom layout for using during direct requests in step 4 above *@
    + @attribute [HtmxLayout(typeof(HtmxorLayout))]
    ```

    Note that we set up the custom layout for all components by defining the `[HtmxLayout(typeof(HtmxorLayout))]` attribute in the `_Imports.razor` file.

## Response headers

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
`HX-Request` is absent and the direct component representation when
`HX-Request: true` is present, include `HX-Request` in the ASP.NET Core
OutputCache key. Configure the standard component attribute together with the
OutputCache services and middleware:

```csharp
builder.Services.AddOutputCache();

// Authentication and authorization middleware must run before OutputCache.
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
```

```razor
@using Microsoft.AspNetCore.OutputCaching
@attribute [OutputCache(VaryByHeaderNames = ["HX-Request"])]
@page "/cached-component"
```

The standard `VaryByHeaderNames` configuration is sufficient while
`HX-Request` is the sole input that changes the response representation.
`CacheOutput(policy => policy.SetVaryByHeader("HX-Request"))` is its endpoint-
policy equivalent. An application that also varies output for boosted requests,
targets, history restoration, selected fragments, authentication, or other
request data must add every such input to its cache policy. Htmxor does not infer
an application's complete cache key.

ASP.NET Core OutputCache caches safe requests by default; do not opt unsafe
component methods into output caching. Htmxor does not emit an antiforgery
cookie for a safe GET that contains no stock antiforgery component.

## Routing in Htmxor

Htmxor routing and Blazor Static Web Apps routing differ in ways that enhance htmx scenarios. In Htmxor, there are two types of routing:

In Htmxor, there are **two** types of routing:

- **Standard routing**
- **Direct routing**

The routing mode is determined by the htmx 4
[`HX-Request`](https://four.htmx.org/reference/#headers) and
`HX-Request-Type` headers together:

```text
if ( HX-Request is present && HX-Request-Type is exactly "partial" )
    RoutingMode.Direct
else
    RoutingMode.Standard
```

Here's a detailed look at each mode:

### Standard Routing

Standard routing is used for a normal browser request and for an htmx request
whose `HX-Request-Type` is missing, invalid, repeated, or exactly `full`.

In this mode, routing behaves like conventional Blazor Static Web Apps routing. The root component (typically App.razor or the component passed to `MapRazorComponents<TRootComponent>()` in `Program.cs`) is rendered.

The root component usually renders a `<Router>` component that determines which `@page`-annotated component to render based on the HTTP request, using the layout specified for that page.

Example:

```
HTTP GET /my-page
App --> Routes --> MainLayout --> MyPage
```

### Direct Routing

Direct routing is selected only when `HX-Request` is present and the single
`HX-Request-Type` value is exactly `partial`. It bypasses the root component
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
