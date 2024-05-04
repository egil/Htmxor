# Getting Started

To create a minimal Blazor + htmx app with various examples, download the [Minimal Htmxor App template](https://github.com/egil/Htmxor/tree/main/samples/MinimalHtmxorApp).

To start fresh, follow these steps:

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
	+   <HtmxHeadOutlet />
	  	<HeadOutlet />
	  </head>
	  
	  <!-- 
	    Adding hx-boost="true" is optional. 
	    Learn more here: https://htmx.org/  attributes/hx-boost/ 
	  -->
	+ <body hx-boost="true">
	  	<Routes />
  
	-   <script src="_framework/blazor.web.js"></script>
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
	
	+ @attribute [HtmxLayout(typeof(HtmxorLayout))]
	```

	Note that we set up the custom layout for all components by defining the `[HtmxLayout(typeof(HtmxorLayout))]` attribute in the `_Imports.razor` file.
