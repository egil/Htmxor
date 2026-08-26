# Blazor static SSR progressive enhancement

Research date: 2026-08-26 UTC

This note examines whether Htmxor can be added to an existing Blazor Web App without changing its behavior, then progressively enhance ordinary static server-side rendered components. It focuses on .NET 10 forms and reusable components. The latest servicing release at the research date is [.NET 10.0.11](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.11/10.0.11.md).

Sources are Microsoft Learn and the `dotnet/aspnetcore` implementation. Source links use the .NET 10 release tag where the repository exposes one. The few Htmxor recommendations are marked as design conclusions rather than framework facts.

## Executive finding

The requested compatibility goal is achievable, but only if Htmxor treats Blazor's static SSR endpoint pipeline as the platform contract:

> A component that works as a stock Blazor static SSR component should continue to work unchanged inside a Htmxor response, including `EditForm`, built-in `Input*` components, form mapping, validation, antiforgery, and reusable library forms.

That rules out a standalone custom renderer as the main v1 execution path. A generic HTML renderer can instantiate components and produce markup, but stock static form handling also requires request validation, form-handler selection, form-value mapping, component lifecycle execution, submit-event dispatch, rerendering, and enhanced-navigation response signaling. The framework's Razor component endpoint invoker coordinates those operations. [`RazorComponentEndpointInvoker`](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs) shows the full sequence.

The safe v1 baseline is:

1. Leave normal requests on the stock `MapRazorComponents` endpoints.
2. Reuse the public [`IRazorComponentEndpointInvoker`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.endpoints.irazorcomponentendpointinvoker?view=aspnetcore-10.0), with the framework's endpoint metadata and services, for Htmxor component endpoints that must support stock static SSR behavior.
3. Perform HTMX response selection around or within that execution path without taking ownership of form mapping or submit dispatch.
4. Accept full-tree rendering and response buffering as the correctness baseline if the public API does not expose a cheaper fragment seam. Optimize only after parity tests prove that a new path preserves the same behavior.

This is a useful constraint. It lets Htmxor support first-party and static-SSR-capable third-party components by inheritance rather than by reimplementing each Blazor feature.

## The zero-break installation contract

Adding Htmxor should be inert until the application opts a route or markup region into HTMX behavior. Installing the package and registering its services must not change existing component output or request handling.

The concrete contract should be:

- Htmxor does not replace Blazor's renderer, `IRazorComponentEndpointInvoker`, form mapper, antiforgery provider, navigation manager, or other services registered by `AddRazorComponents`.
- Existing `MapRazorComponents<TRootComponent>()` endpoints remain the sole handlers for ordinary page GETs and stock form POSTs.
- Htmxor does not inject an htmx script, enable `hx-boost`, or modify links and forms merely because its services are registered.
- Htmxor-owned routes fail generation or startup when they collide with an existing ASP.NET Core endpoint. Registration order must not decide which route wins.
- A request without an HTMX signal follows the same endpoint, render mode, authorization, antiforgery, lifecycle, status, redirect, streaming, and enhanced-navigation behavior as it did before Htmxor was installed.
- A component that contains no Htmxor component or `hx-*` attribute has no runtime dependency on Htmxor.
- Removing or failing to load htmx leaves every progressively enhanced link and form with a valid HTML navigation or submission path.

Microsoft's library guidance supports this model. Static SSR runs components for one HTTP request, emits HTML, then discards the renderer and component state. Read-only components work naturally. Form `@onsubmit` is the special event that remains functional in static SSR. Components can provide better interactive behavior while keeping an HTML and form-post baseline. [Razor class libraries and static SSR](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/class-libraries-and-static-server-side-rendering?view=aspnetcore-10.0).

## How a stock static SSR form works

Static form handling is not a single callback invocation. The framework coordinates several layers.

### Initial render

Consider an ordinary component:

```razor
@page "/contacts/{Id:int}/edit"

<EditForm Model="Model"
          FormName="EditContact"
          OnValidSubmit="SaveAsync">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <InputText @bind-Value="Model!.Name" />
    <button type="submit">Save</button>
</EditForm>

@code {
    [SupplyParameterFromForm]
    private ContactInput? Model { get; set; }

    protected override void OnInitialized() => Model ??= new();

    private Task SaveAsync() => Contacts.SaveAsync(Id, Model!);
}
```

During static SSR, `EditForm`:

- creates or accepts an `EditContext` and cascades it to its children;
- renders a real HTML `<form>`;
- sets `method="post"` when the static form-mapping context is present;
- registers its `onsubmit` callback as a named event using `FormName`;
- renders a form-mapping validator and an antiforgery token;
- renders `data-enhance` only when the developer sets `Enhance`.

These behaviors are in the [.NET 10 `EditForm` implementation](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Components/Web/src/Forms/EditForm.cs). They depend on the `FormMappingContext` supplied by the endpoint rendering environment. When that context is missing, `EditForm` does not set up the static POST contract.

`InputBase<TValue>` derives each input's HTML `name` from its bound value expression and field prefix. The form mapper uses the same naming convention. On a failed post, the input retrieves the attempted value and exposes validation state through the `EditContext`; it also emits `aria-invalid="true"` unless the component author supplied that attribute. [.NET 10 `InputBase<TValue>` source](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Components/Web/src/Forms/InputBase.cs).

### POST processing

For a Razor component endpoint, the .NET 10 endpoint invoker performs this order:

1. Validate the POST content type.
2. Enforce endpoint antiforgery metadata, using middleware's result when available or validating through `IAntiforgery` itself.
3. Read the form and obtain the single `_handler` value that identifies the named form.
4. Initialize the renderer's request services with the page component type, selected handler, and posted form.
5. Render the root component so `[SupplyParameterFromForm]` can populate the new component instance and named event registrations can be discovered.
6. Dispatch the selected submit event on that rendered instance.
7. Await non-streaming tasks, rerender, and write the response, including streaming framing when applicable.

The implementation rejects an invalid content type, a missing or invalid antiforgery token, multiple `_handler` values, and unknown or ambiguous handlers. See [`RazorComponentEndpointInvoker.RenderComponentCore`](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs#L38) and its request validation path.

This sequence explains why rendering a fresh component and independently calling a method is not equivalent. The handler must run on the instance that received route and form values and participated in the render tree. Its rerender carries validation messages, attempted invalid values, and callback state into the response.

### Form mapping

`[SupplyParameterFromForm]` is a cascading-value feature, not MVC model binding. It supports primitives, collections, complex and recursive types, constructors, and enums. `RazorComponentsServiceOptions` bounds collection size, recursion, error count, and key size. Microsoft recommends a dedicated form model or DTO to prevent overposting because MVC attributes such as `[BindNever]` don't apply. [Blazor forms binding](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/binding?view=aspnetcore-10.0).

The framework records attempted values and mapping failures in `FormMappingContext`. `FormMappingValidator` transfers those failures to the form's `EditContext`, which stops `OnValidSubmit` from running when mapping failed. [`SupplyParameterFromFormValueProvider`](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Components/Web/src/Forms/Mapping/SupplyParameterFromFormValueProvider.cs) owns the property lookup and error association.

`FormName` is required when a statically rendered form is submitted. A name only has to be unique inside its form mapping scope. `FormMappingScope` exists so an application can include a library component whose author chose a form name that is also used elsewhere. [Form names and `FormMappingScope`](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/binding?view=aspnetcore-10.0#form-names).

### Validation

`EditForm` owns the submit validation convention:

- `OnSubmit` gives the component responsibility for calling `EditContext.Validate()`.
- Without `OnSubmit`, `EditForm` validates and then calls exactly one of `OnValidSubmit` or `OnInvalidSubmit`.
- `DataAnnotationsValidator` attaches data-annotations validation to the `EditContext`.
- `ValidationMessage` and `ValidationSummary` render the resulting messages.
- Mapping errors participate in the same `EditContext`, so malformed values cannot reach `OnValidSubmit` as if they were valid.

This behavior is documented in the [forms overview](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/?view=aspnetcore-10.0#handle-form-submission) and implemented by [.NET 10 `EditForm`](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Components/Web/src/Forms/EditForm.cs).

There is a documentation-version trap. The current Learn page rendered with `view=aspnetcore-10.0` contains both "client-side validation requires a circuit" and a statement that static SSR gains automatic client validation. The .NET 10 source has server validation on POST and no static-SSR client-validation emission. Static SSR client validation was announced as a [.NET 11 preview feature](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview5/aspnetcore.md#client-side-validation-for-blazor-static-ssr-forms). Htmxor should therefore treat server-side validation as the .NET 10 contract and not promise .NET 11 client validation while targeting .NET 10.

### Antiforgery

`AddRazorComponents` adds antiforgery services. In current .NET 10 apps, automatic header-based CSRF protection checks `Sec-Fetch-Site` and `Origin` on unsafe requests. Calling `UseAntiforgery` adds token validation; it must run after authentication and authorization and after routing. `EditForm` automatically renders `AntiforgeryToken` and requires validation. Plain HTML forms must include `<AntiforgeryToken />` themselves. [Blazor forms antiforgery guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/?view=aspnetcore-10.0#antiforgery-support).

For Htmxor, an HTMX form submission must retain the antiforgery cookie and hidden field, use a supported form content type, and reach an endpoint with the same antiforgery metadata. `HX-Request` is never an antiforgery or authorization signal.

## Progressive enhancement with an ordinary `EditForm`

An existing `EditForm` already has the correct non-JavaScript baseline. It posts to the current component route, names the form, sends the model fields and antiforgery token, invokes the component callback, and receives a new page.

The least invasive HTMX enhancement is boosting a region rather than replacing `EditForm`:

```razor
<HtmxFragment Id="contact-editor">
    <div hx-boost="true"
         hx-target="#contact-editor"
         hx-select="#contact-editor">
        <EditForm Model="Model"
                  FormName="EditContact"
                  OnValidSubmit="SaveAsync">
            <DataAnnotationsValidator />
            <ValidationSummary />

            <InputText @bind-Value="Model!.Name" />
            <ValidationMessage For="() => Model!.Name" />
            <button type="submit">Save</button>
        </EditForm>
    </div>
</HtmxFragment>
```

This design has two execution paths:

| Browser state | Result |
| --- | --- |
| htmx is absent, blocked, or disabled | The browser performs the stock full-page form POST. Blazor binds, validates, invokes the callback, and renders the complete page. |
| htmx is active | Boosting submits the same form fields, `_handler`, and antiforgery token to the same URL. The same Blazor endpoint pipeline handles the POST. HTMX applies the selected response region. |

No `hx-post` is needed on `EditForm`, and no Htmxor-specific form component is needed. Htmxor may use the request's target to select `contact-editor` server-side. The returned fragment must contain the form, inputs, and validation components when the post is invalid. Returning only a success child would discard the attempted values and validation UI.

This is a design conclusion, not a claim that htmx and Blazor automatically coordinate. A form must have one JavaScript submission owner. `EditForm Enhance` emits `data-enhance`, which asks `blazor.web.js` to intercept the same submit event. Htmxor should do one of the following:

- exclude `data-enhance` forms from Htmxor boosting by default; or
- diagnose a form that is both Blazor-enhanced and HTMX-boosted and require the developer to choose.

It should not rely on event-listener registration order. Microsoft states that enhanced form posts only work with Blazor endpoints and that `Enhance` is opt-in per form. [Enhanced form handling](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/navigation?view=aspnetcore-10.0#enhanced-navigation-and-form-handling).

The full-page fallback also means Htmxor must preserve ordinary `action`, `method`, submit-button names and values, multipart encoding, redirects, and status codes. An HTMX-only construct with no valid HTML fallback is an opt-in feature, not progressive enhancement.

## First-party component compatibility

If Htmxor preserves the endpoint pipeline, the following components need no Htmxor adapters:

| Blazor feature | Static SSR behavior Htmxor should inherit |
| --- | --- |
| `EditForm` | Real form element, named submit callback, `EditContext`, automatic POST method in the mapping context, antiforgery token, validation dispatch |
| `InputText`, `InputTextArea`, `InputNumber`, `InputDate`, `InputCheckbox`, `InputSelect`, `InputRadioGroup`, and related `InputBase<T>` controls | Stable HTML field names, field prefixes, attempted-value restoration, parsing errors, validation CSS, `aria-invalid` |
| `[SupplyParameterFromForm]` | Request-scoped model construction and bounded form mapping, including private component properties |
| `DataAnnotationsValidator` | Server-side validation attached to the posted form's `EditContext` in .NET 10 |
| `ValidationMessage` and `ValidationSummary` | Errors included in the rerendered form or fragment |
| `FormMappingScope` | Isolation of duplicate form names in reusable components and Razor class libraries |
| `<AntiforgeryToken />` and `RequireAntiforgeryToken` | Hidden token generation and endpoint validation |
| `[StreamRendering]` | Stock initial output and streamed updates, subject to response-selection support |
| Route, query, authentication, and `HttpContext` cascading values | The same request-scoped values as a normal component endpoint |

`InputFile` needs a narrower promise. Its rich `OnChange` and `IBrowserFile` APIs require an interactive runtime. A static-SSR-capable file component may still render a native `<input type="file">` and use a normal multipart form post. Microsoft explicitly recommends this kind of reduced HTML baseline for progressively enhanced library components. [Static SSR library guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/class-libraries-and-static-server-side-rendering?view=aspnetcore-10.0#options-for-component-authors) and [Blazor file uploads](https://learn.microsoft.com/en-us/aspnet/core/blazor/file-uploads?view=aspnetcore-10.0).

## Third-party component compatibility

The correct claim is not "every Blazor component works with Htmxor." It is:

> Any component that meets Blazor's static SSR contract should retain that contract when rendered through Htmxor.

That includes third-party components that:

- render useful HTML without a live circuit or WebAssembly runtime;
- use links or form posts for their static behavior;
- use `EditForm`, `EditContext`, and the supported validation components without replacing their semantics;
- derive custom fields from `InputBase<TValue>` and put `NameAttributeValue` on the actual HTML control when static rendering is possible;
- or otherwise emit correct HTML `name` and `value` fields that match their `[SupplyParameterFromForm]` model;
- use `FormMappingScope` or document form names when packaged forms may be repeated;
- treat `HttpContext` as nullable and avoid requiring it in interactive modes;
- avoid requiring `OnAfterRender`, element references, arbitrary .NET DOM events, or JavaScript interop for their static baseline.

Microsoft specifically instructs custom `InputBase<TValue>` authors to emit `name="@NameAttributeValue"` when the component may be statically rendered. Without it, the component may look correct on GET but cannot bind its value on POST. [Custom input component guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/binding?view=aspnetcore-10.0#custom-input-components).

Components that only work interactively remain interactive-only. Htmxor should not imitate a circuit, synthesize arbitrary DOM events, or claim compatibility because the initial HTML happened to render. Static SSR discards component and renderer state after each request, and only form submit has the framework's special server event path. [Static SSR capabilities and restrictions](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/class-libraries-and-static-server-side-rendering?view=aspnetcore-10.0#understand-the-capabilities-and-restrictions-of-static-ssr).

## Public seams Htmxor can reuse

| Public seam | Suitable Htmxor use | Constraint |
| --- | --- | --- |
| `AddRazorComponents` and `MapRazorComponents<TRootComponent>` | Keep all normal pages and their GET/POST behavior on the framework path | Htmxor must compose with these registrations, not replace them |
| `IRazorComponentEndpointInvoker.Render(HttpContext)` | Execute a component endpoint with framework routing state, form handling, lifecycle, streaming, and response behavior | The endpoint must supply the metadata and middleware contract expected by the invoker |
| `RootComponentMetadata` and `ComponentTypeMetadata` | Tell the invoker which application root and page component to render | Both are required by the implementation; route values and root composition must match stock behavior |
| ASP.NET Core endpoint metadata and conventions | Apply authorization, antiforgery, HTTP methods, rate limits, caching rules, and request limits before rendering | Htmxor must not treat HTMX headers as policy |
| `RazorComponentsServiceOptions` | Retain application-configured form-mapping limits and culture behavior | Do not create an independent form binder with different limits |
| `FormMappingScope`, `FormMappingContext`, `EditContext`, and `InputBase<T>` | Let built-in and third-party components participate through Blazor's existing contracts | Most implementation details beneath these types remain framework-owned |
| Response-body capture around a stock endpoint | Correctness-first full-page rendering followed by HTMX fragment extraction | It costs full rendering, buffering, and parsing; streaming requires a deliberate policy |
| `RazorComponentResult<TComponent>` | Render a component as the result of an endpoint that already owns its HTTP operation | It is not a substitute for routed component form POST dispatch |
| `HtmlRenderer` | Tests and generic static component rendering | It does not provide the routed endpoint's form selection, request validation, submit dispatch, navigation framing, or streaming coordination |

`IRazorComponentEndpointInvoker`, `RootComponentMetadata`, and `ComponentTypeMetadata` are public .NET 10 APIs. The interface documentation says that Razor component endpoints provide the root and page components through those metadata types. [Endpoint API namespace](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.endpoints?view=aspnetcore-10.0). `AddRazorComponents` registers the framework implementation and form services. [.NET 10 service registration source](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceCollectionExtensions.cs).

This makes a public-API Htmxor endpoint plausible. It still needs a prototype. The prototype must prove that a generated endpoint with the same root/page metadata preserves route parameters, layouts, authorization, antiforgery, form mapping, named submit dispatch, redirects, not-found behavior, streaming, and interactive render-mode markers.

## Where a custom renderer breaks behavior

The prototype's renderer must not be treated as equivalent merely because it produces visually similar HTML. These are the failure points:

1. Without the endpoint's initialized form mapper, `[SupplyParameterFromForm]` receives no posted model.
2. Without `FormMappingContext`, `EditForm` does not activate its static SSR behavior, including automatic POST method, named event, mapping validator, and antiforgery child.
3. Without named-event registration and `_handler` selection, multiple forms become ambiguous and the wrong callback may run.
4. Without `FormMappingValidator`, parse and mapping errors do not join the `EditContext`; `OnValidSubmit` could run on invalid input.
5. Without endpoint antiforgery metadata and the invoker's validation order, a forged POST may reach component code.
6. Without dispatch on the rendered instance, the callback runs without the route/form/lifecycle state used to render the page.
7. Without the second render, validation messages, attempted invalid values, success state, and navigation results do not reach the response.
8. Without enhanced-navigation response signaling, a form marked `Enhance` is not a valid Blazor enhanced-form endpoint.
9. Without streaming coordination, the renderer may emit the wrong phases of a POST response or allow headers and cookies to become immutable too early.
10. Without the normal root component, layouts, head content, cascading authentication state, routing state, error boundaries, render modes, and persistent state may differ.

The framework implementation is internal precisely where these operations are tightly coordinated. Htmxor should call the public invoker rather than subclass or copy `EndpointHtmlRenderer`.

## Response selection and performance

There is an unavoidable tension between fragment efficiency and framework compatibility.

The public endpoint invoker writes a complete component endpoint response. It does not expose a public hook that returns a selected render-tree subtree after submit dispatch. Htmxor has three plausible stages:

1. Start with stock execution plus buffered response selection. This preserves behavior but pays for the complete render and HTML parse.
2. Let `HtmxFragment` suppress unrelated child content during the framework render when the request identifies one server-declared fragment. This can avoid work beneath those fragment boundaries, but the root, route, lifecycle, and form pipeline still run.
3. Pursue a faster custom rendering path only if benchmarks justify it and a compatibility suite proves parity with the stock path.

The performance documentation must be honest. Selecting one response region does not imply that the page component was never instantiated or that its lifecycle and data loading did not run. A third-party component inside a skipped `HtmxFragment` may avoid rendering, but components above the selection point still execute.

For form failures, the selected response needs the whole form region. For success, a component may intentionally select a smaller result or an out-of-band update. Selection is representation logic after authorization, antiforgery, binding, and validation, never a shortcut around them.

## Required compatibility suite

The v1 gate should run the same component scenarios through stock Blazor and Htmxor and compare observable behavior.

### Installation invariants

- A static Blazor Web App with Htmxor services registered but no opted-in route or markup has unchanged GET and POST behavior.
- Existing endpoint metadata, route precedence, status codes, redirects, content type, and render mode remain unchanged.
- No script or `hx-*` attribute appears unless the app requested it.

### Form parity

- Plain HTML form with `@formname`, `@onsubmit`, `AntiforgeryToken`, and `[SupplyParameterFromForm]`.
- `EditForm` with every built-in `InputBase<T>` input family.
- Valid, invalid, unparseable, missing, repeated, nested, collection, and over-limit values.
- `OnSubmit`, `OnValidSubmit`, and `OnInvalidSubmit` dispatch to the exact named form.
- Two same-named library forms isolated by different `FormMappingScope` values.
- Antiforgery success, missing token, invalid token, and cross-origin rejection.
- URL-encoded and multipart posts.
- Redirect, not found, cookie/header mutation, and an async submit handler.
- Validation fragment contains attempted values, messages, `aria-invalid`, and the refreshed token.

### Progressive enhancement

- With htmx unavailable, the boosted example completes as an ordinary full-page POST.
- With htmx available, the same form reaches the same callback and swaps only the declared region.
- `data-enhance` and HTMX ownership conflict produces a diagnostic or deterministic exclusion.
- A nested third-party static SSR form posts without Htmxor-specific code.
- History, focus, and validation summary behavior remain accessible after a swap.

### Rendering boundaries

- Static-only page and component.
- Static page containing Interactive Server, WebAssembly, and Auto islands outside the HTMX target.
- An attempted HTMX replacement of a live interactive root is rejected or diagnosed.
- Streaming GET and POST behavior is either preserved or explicitly disabled with documented buffering.

## Roadmap consequence

Progressive enhancement should become a protected v1 behavior, not a sample-only convenience:

1. Define the zero-break installation contract and encode it in integration tests before changing the renderer.
2. Prototype Htmxor endpoints through `IRazorComponentEndpointInvoker` with public root/page metadata.
3. Prove stock `EditForm`, `Input*`, form mapping, validation, and antiforgery parity.
4. Add an HTMX-boosted form path whose no-JavaScript fallback is the unchanged Blazor POST.
5. Define the one-owner rule for Blazor enhanced forms versus HTMX forms.
6. Restore server-side fragment selection without taking form ownership away from Blazor.
7. Benchmark the correctness-first path, then optimize selected rendering behind the same compatibility suite.

The practical design rule is simple:

> Htmxor should add another representation of a Blazor component request. It should not create another Blazor form runtime.
