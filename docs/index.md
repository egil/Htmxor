# Htmxor

[add introduction]

## Getting Started

To create a minimal Blazor + htmx app with various examples, download the [Minimal Htmxor App template](https://github.com/egil/Htmxor/tree/main/samples/MinimalHtmxorApp).

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

      app.UseStaticFiles();
      app.UseAntiforgery();
    + var htmxorRoutes = app.MapGroup(string.Empty);
      app.MapRazorComponents<App>()
    +    .AddHtmxorComponentEndpoints(htmxorRoutes);

      app.Run();
    ```
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
          <link rel="stylesheet" href="bootstrap/bootstrap.min.css" />
          <link rel="stylesheet" href="app.css" />
          <link rel="stylesheet" href="MinimalHtmxorApp.styles.css" />
          <link rel="icon" type="image/png" href="favicon.png" />
    +     <script defer src="htmx-4.0.0.min.js"></script>
    +     <HtmxHeadOutlet />
          <HeadOutlet />
      </head>

      <!--
        Adding hx-boost="true" is optional.
        hx-boost returns the "enhanced navigation" and "enhanced forms"
        features that are lost by removing blazor.web.js script below.
        Learn more here: https://htmx.org/attributes/hx-boost/
      -->
    + <body hx-boost="true">
          <Routes />

    -     <script src="_framework/blazor.web.js"></script>
      </body>

      </html>
    ```

5. **Create an Optional Direct Request Layout**

   Optionally, create a layout that will be used during [direct routing](routing.md#direct-routing), e.g., `/Components/Layout/HtmxorLayout.razor`:

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

## Routing in Htmxor

Htmxor routing and Blazor Static Web Apps routing differ in ways that enhance htmx scenarios. In Htmxor, there are two types of routing:

In Htmxor, there are **two** types of routing:

- **Standard routing**
- **Direct routing**

The routing mode is determined by the presence or absence of [htmx headers](https://htmx.org/reference/#request_headers):

```python
if ( HX-Request is null || ( HX-Boosted is not null && HX-Target is null ) )
    RoutingMode.Standard
else
    RoutingMode.Direct
```

Here's a detailed look at each mode:

### Standard Routing

Standard routing is used when the `HX-Request` header is missing, or when `HX-Boosted` is present and `HX-Target` is missing.

In this mode, routing behaves like conventional Blazor Static Web Apps routing. The root component (typically App.razor or the component passed to `MapRazorComponents<TRootComponent>()` in `Program.cs`) is rendered.

The root component usually renders a `<Router>` component that determines which `@page`-annotated component to render based on the HTTP request, using the layout specified for that page.

Example:

```
HTTP GET /my-page
App --> Routes --> MainLayout --> MyPage
```

### Direct Routing

Direct routing bypasses the root component (`App.razor`) and the standard layout (`MainLayout`). Instead, it routes directly to the component that matches the request.

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
