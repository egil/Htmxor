# Htmxor - supercharging Blazor Static SSR with Htmx
![Htmxor logo](https://raw.githubusercontent.com/egil/Htmxor/main/docs/htmxor.svg)

This packages enables Blazor Static SSR (.NET 8 and later) to be used seamlessly with Htmx. 

Blazor Static SSR comes with basic interactivity via enhanced navigation and enhanced form handling.
Adding Htmx (htmx.org) to the mix gives you access to another level of interactivity while still
retaining all the advantages of Blazor SSR stateless nature.

**NOTE:** _This package is highly experimental!_

**Nuget:** https://www.nuget.org/packages/Htmxor

## Samples

The following Blazor Web Apps (Htmxor) are used to test Htmxor and demo the capabilities of it.

- [Blazing Pizza workshop as Htmxor App](https://github.com/egil/Htmxor/tree/main/samples/BlazingPizza)
- [Htmxor - TestApp](https://github.com/egil/Htmxor/tree/main/test/Htmxor.TestApp)
- [Minimal Htmxor App template](https://github.com/egil/Htmxor/tree/main/samples/MinimalHtmxorApp)

## Getting started

Download the [Minimal Htmxor App template](https://github.com/egil/Htmxor/tree/main/samples/MinimalHtmxorApp) for a complete (but) minimal Blazor + htmx app, with various small examples included.

Starting fresh, here is what you need to do.

1. Include the [Htmxor package from NuGet](https://www.nuget.org/packages/Htmxor).

2. Modify `Program.cs` to look like this:

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
      // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

Note, there is an overload for `AddHtmx(options => { ... })` that allows you to configure all [htmx's configuration options](https://htmx.org/reference/#config) for your app.

3. Modify `App.razor` to look like this:

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
  
+ <!-- Adding hx-boost="true" is optional. Learn more here: https://htmx.org/attributes/hx-boost/ -->
+ <body hx-boost="true">
      <Routes />

-     <script src="_framework/blazor.web.js"></script>
  </body>
  
  </html>
```

4. Optionally, create a htmx-request specific layout, e.g., `/Components/Layout/HtmxorLayout.razor`:

```razor
@* 
This is a default layout component that is used with htmx requests.
It only includes the <HeadOutlet /> which makes it possible to update the 
page title during htmx requests by using the <PageTitle></PageTitle> component.
*@
@inherits LayoutComponentBase
<HeadOutlet />
@Body
```

5. Optionally, update `_Imports.razor`:

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
  
+ @attribute [HxLayout(typeof(HtmxorLayout))]
```

Note that we set up the custom layout for all components by defining the `[HxLayout(typeof(HtmxorLayout))]` attribute in the `_Imports.razor` file.