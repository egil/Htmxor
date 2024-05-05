# Routing in Htmxor

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

## Standard Routing

Standard routing is used when the `HX-Request` header is missing, or when `HX-Boosted` is present and `HX-Target` is missing.

In this mode, routing behaves like conventional Blazor Static Web Apps routing. The root component (typically App.razor or the component passed to `MapRazorComponents<TRootComponent>()` in `Program.cs`) is rendered.

The root component usually renders a `<Router>` component that determines which `@page`-annotated component to render based on the HTTP request, using the layout specified for that page.

Example:

```
HTTP GET /my-page
App --> Routes --> MainLayout --> MyPage
```

## Direct Routing

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
