# Htmxor - supercharging Blazor Static SSR with Htmx
![Htmxor logo](https://github.com/egil/Htmxor/blob/main/docs/htmxor-x.svg)

This package enables Blazor Static SSR on .NET 10 to be used seamlessly with Htmx.

Htmxor v1 targets .NET 10 only. Existing .NET 8 applications can continue to use the previous beta package; .NET 11 support will be added only after a separate compatibility matrix is executed.

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

Blazor Static SSR comes with basic interactivity via enhanced navigation and enhanced form handling.
Adding Htmx (htmx.org) to the mix gives you access to another level of interactivity while still
retaining all the advantages of Blazor SSR stateless nature.

Use the [source-package instructions](https://github.com/egil/Htmxor/blob/main/docs/index.md#getting-started-with-the-unreleased-v1-api)
to try these calls. The [published NuGet package](https://www.nuget.org/packages/Htmxor)
is the previous beta and exposes the previous registration names.

## Documentation

See https://github.com/egil/Htmxor/blob/main/docs/index.md.

## Samples

The following Blazor Web Apps (Htmxor) are used to test Htmxor and demo the capabilities of it.

- [Blazing Pizza workshop as Htmxor App](https://github.com/egil/Htmxor/tree/main/samples/BlazingPizza)
- [Htmxor Examples](https://github.com/egil/Htmxor/tree/main/samples/HtmxorExamples)
- [Minimal Htmxor App template](https://github.com/egil/Htmxor/tree/main/samples/MinimalHtmxorApp)
