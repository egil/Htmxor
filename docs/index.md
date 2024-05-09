# Htmxor

[add introduction]

## Getting Started

To create a minimal Blazor + htmx app with various examples, download the [Minimal Htmxor App template](https://github.com/egil/Htmxor/tree/main/samples/MinimalHtmxorApp).

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
    +   .AddHtmx();

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
    + app.UseHtmxAntiforgery();
      app.MapRazorComponents<App>()
    +    .AddHtmxorComponentEndpoints(app);

      app.Run();
    ```
    Note: You can use `AddHtmx(options => { ... })` to change [htmx's config](https://htmx.org/reference/#config) for your app.

3. **Update App.razor**

   Modify `App.razor` to include Htmxor components:

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
    +     <HtmxHeadOutlet />
          <HeadOutlet />
      </head>

      <!--
        Adding hx-boost="true" is optional.
        Learn more here: https://htmx.org/  attributes/hx-boost/
      -->
    + <body hx-boost="true">
          <Routes />

    - <script src="_framework/blazor.web.js"></script>
      </body>

      </html>
    ```

4. **Create an Optional Direct Request Layout**

   Optionally, create a layout that will be used during [direct routing](routing.md#direct-routing), e.g., `/Components/Layout/HtmxorLayout.razor`:

    ```razor
    @inherits HtmxLayoutComponentBase
    @Body
    ```

    The `HtmxLayoutComponentBase` includes the `<HeadOutlet>` component. This makes it possible to use the `<PageTitle>` component during htmx requests to update the page title.

5. **Update _Imports.razor (Optional)**

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

    + @attribute [HtmxLayout(typeof(HtmxorLayout))]
    ```

    Note that we set up the custom layout for all components by defining the `[HtmxLayout(typeof(HtmxorLayout))]` attribute in the `_Imports.razor` file.

## Routing in Htmxor

Htmxor routing and Blazor Static Web Apps routing differ in ways that enhance htmx scenarios. In Htmxor, there are two types of routing:

In Htmxor, there are **two** types of routing:

- **Standard routing**
- **Direct routing**

The routing mode is determined by the presence or absence of [htmx headers](https://htmx.org/reference/#request_headers):

```
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

It allows a single routable component to render specific parts for particular requests or the full content for others. This way, you can keep all related fragments within a single component, avoiding the need to split them into separate, individually routable components.

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
