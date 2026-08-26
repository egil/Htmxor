# Htmxor v1 interface sketch

Design date: 2026-08-26 UTC

Status: proposed interface for discussion. Names and signatures in this document do not exist yet unless identified as current Htmxor behavior.

This document turns the [backend framework comparison](htmx-backend-framework-comparison.md) into developer code. The aim is to preserve Htmxor's defining interface: Razor components own routes, actions, and fragments. Endpoint registration, security metadata, HTMX protocol handling, and static SSR rendering stay behind that interface.

## Outcome

The recommended shape keeps:

- `@page` as the normal route declaration;
- `[HtmxRoute]` for an HTMX-only component route;
- `HtmxFragment`, `HtmxFragmentElement`, `HtmxLayout`, and standard OOB markup;
- one application-level registration call;
- standard ASP.NET authorization, antiforgery, rate-limit, cache, and request-size policies.

It adds:

- an explicit normal-only reachability declaration;
- named inline fragments and an immutable multi-fragment render result;
- build-time, route-bound local actions with explicit HTTP verbs;
- generated endpoint metadata and diagnostics;
- exact typed HTMX request values and typed response parts;
- automatic HTTP variation and fail-closed output-cache variation;
- an application-owned htmx runtime with replaceable browser adapters and typed protocol extensions;
- static direct rendering through public ASP.NET and Razor component interfaces.

One part of the prototype should not remain the v1 core: runtime replay of `@onpost`, `@ondelete`, and related callback ids discovered by rendering the component. The v1 action is still declared in the `.razor` file, but it is a real generated HTTP handler bound to one route and verb.

The proposal extends current [`HtmxRoute`](../../src/Htmxor/HtmxRouteAttribute.cs), [`HtmxFragment`](../../src/Htmxor/Components/HtmxFragment.cs), [`HtmxFragmentElement`](../../src/Htmxor/Components/HtmxFragmentElement.cs), [`HtmxContext`](../../src/Htmxor/Http/HtmxContext.cs), and [`HtmxLayout`](../../src/Htmxor/HtmxLayoutAttribute.cs) concepts. It changes the current [runtime event-handler dispatch](../../src/Htmxor/Rendering/HtmxorRenderer.HtmxorEventDispatch.cs) and [private endpoint discovery](../../src/Htmxor/Builder/HtmxorComponentEndpointRouteBuilderExtensions.cs).

## Requirements used for the design

The callers are application developers writing `.razor` files. They should not write controllers, Minimal API mappings, or parallel partial views for each feature.

The module must support:

- normal-only, HTMX-only, and dual routes;
- safe GET rendering and explicitly declared unsafe actions;
- whole-component, one-fragment, multi-fragment, layout, and OOB output;
- normal Blazor static, Server, WebAssembly, and Auto behavior on the normal route;
- static SSR on the direct HTMX route;
- standard ASP.NET endpoint policies;
- verified htmx 2 behavior without requiring, bundling, or runtime-gating a specific htmx release;
- pass-through support for unknown `hx-*` attributes and public extension seams for future client and wire-protocol features;
- build-time diagnostics where the source is statically knowable;
- a measured per-request cost.

The implementation should hide endpoint construction, route naming, request parsing, policy propagation, antiforgery validation, cache variation, authentication redirects, and Razor component rendering.

Native AOT and trimmed Razor Components are not v1 promises. Generating routes can still reduce reflection and startup work, but the current ASP.NET [Native AOT compatibility matrix](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/native-aot?view=aspnetcore-10.0) does not support Blazor Server, which is one of the normal render modes in scope.

## Three action models and one optional link layer

### 1. Compile the current Razor event syntax

This is the smallest caller change:

```razor
@page "/contacts"

<form hx-post="/contacts" @onpost="CreateAsync">
    ...
</form>

@code {
    private Task CreateAsync(HtmxEventArgs args) => ...;
}
```

A Razor-aware build step could compile the literal route, POST verb, and method group into endpoint metadata. It could reject an `hx-post` without a matching server declaration. This hides action identity, antiforgery, dispatch, and rendering.

The interface is familiar, but its hard cases are not small. Lambdas, conditional attributes, reused child components, multiple handlers on one route, and instance state make static binding difficult. Falling back to render-time callback discovery recreates the prototype's security and request-cost problems. This shape is useful as a migration syntax only when the build can prove the binding.

### 2. Declare static local actions and return a render result

The action remains in the component file, but it becomes a statically bindable HTTP handler:

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class HtmxPostAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}

public readonly record struct HtmxFragmentName(string Value)
{
    public static implicit operator HtmxFragmentName(string value) =>
        new(value);
}

public sealed record HtmxRender
{
    public ImmutableArray<HtmxFragmentName> FragmentNames { get; private init; }
        = [];

    public int StatusCode { get; private init; }
        = StatusCodes.Status200OK;

    public ComponentParameterCollection Parameters { get; private init; }
        = ComponentParameterCollection.Empty;

    public HtmxTarget? RetargetTo { get; private init; }
    public HtmxSwap? ReswapWith { get; private init; }
    public bool IsHandledError { get; private init; }

    public ImmutableArray<HtmxEvent> Events { get; private init; }
        = [];

    public static HtmxRender Full() => new();

    public static HtmxRender Fragment(HtmxFragmentName name) =>
        new() { FragmentNames = [name] };

    public static HtmxRender Fragments(
        params HtmxFragmentName[] names) =>
        new() { FragmentNames = ImmutableArray.CreateRange(names) };

    public static HtmxRender HandledError(
        HtmxFragmentName name,
        int statusCode) =>
        new()
        {
            FragmentNames = [name],
            StatusCode = statusCode,
            IsHandledError = true
        };

    public HtmxRender WithStatus(int statusCode) =>
        this with { StatusCode = statusCode };

    public HtmxRender WithParameters<TParameters>(TParameters parameters) =>
        this with
        {
            Parameters = ComponentParameterCollection.Snapshot(parameters)
        };

    public HtmxRender Retarget(HtmxTarget target) =>
        this with { RetargetTo = target };

    public HtmxRender Reswap(HtmxSwap swap) =>
        this with { ReswapWith = swap };

    public HtmxRender Trigger(HtmxEvent @event) =>
        this with { Events = Events.Add(@event) };
}
```

The factories copy caller-provided fragment names, and `ComponentParameterCollection.Snapshot` copies the parameter-name/value map. Parameter values are request-local references, not recursively cloned objects. This is an immutable dispatch envelope, not a claim that arbitrary application models become deeply immutable.

Usage stays local:

```razor
@code {
    [HtmxPost]
    [Authorize(Policy = "Contacts.Write")]
    private static async Task<HtmxRender> CreateAsync(
        [FromForm] ContactInput input,
        ContactStore contacts,
        CancellationToken cancellationToken)
    {
        var result = await contacts.CreateAsync(input, cancellationToken);

        return result.IsValid
            ? HtmxRender.Fragments("contact-list", "contact-count", "toast")
            : HtmxRender.HandledError(
                    "editor",
                    StatusCodes.Status422UnprocessableEntity)
                .WithParameters(new { Input = input });
    }
}
```

The proposed generator emits a dispatcher and binder intended to follow documented ASP.NET route, query, form, conversion, and dependency-injection conventions. Those semantics are not inherited automatically merely because the method resembles a Minimal API handler; proving parity is a prototype gate. The dispatcher invokes the static action, then renders a fresh owning component according to the returned result. No controller or `MapPost` appears in application code.

This is the recommended shape for the first v1 spike, not a frozen public interface. The static method cannot mutate a renderer-owned component instance. That is intentional. An HTTP action mutates application state, then Htmxor renders a new static representation. The method's route, verb, security policy, parameters, and possible output are visible before a request runs.

### Optional layer: generate typed action references

The strongest compile-time design also generates route references:

```razor
<EditForm Model="Input"
          @attributes="ContactsActions.Create()">
    ...
</EditForm>

<button @attributes="ContactsActions.Delete(contact.Id)"
        hx-target="#contact-list"
        hx-swap="outerHTML">
    Delete
</button>
```

Conceptually, the generated interface is:

```csharp
public static class ContactsActions
{
    public static HtmxActionAttributes Create();
    public static HtmxActionAttributes Delete(Guid id);
}
```

`HtmxActionAttributes` uses `LinkGenerator` and emits the correct `hx-post` or `hx-delete` attribute. Route constraints become typed factory parameters. A renamed action breaks callers at build time.

This catches more mistakes than literal `hx-*` URLs, but the generated attribute bag hides ordinary HTMX markup and increases compiler coupling. It is a good follow-up if diagnostics on literal routes prove insufficient. It should not be required for v1.

### 3. Put every action behind an explicit policy object

The high-control design separates a handler from the component:

```csharp
public interface IHtmxorAction<TComponent>
    where TComponent : IComponent
{
    static abstract HtmxorEndpointPolicy Policy { get; }

    ValueTask<HtmxRender> ExecuteAsync(
        HtmxorActionContext context,
        CancellationToken cancellationToken);
}
```

```csharp
public sealed class DeleteContact(ContactStore contacts)
    : IHtmxorAction<Contacts>
{
    public static HtmxorEndpointPolicy Policy { get; } = new()
    {
        Route = "/contacts/{id:guid}",
        Method = HttpMethod.Delete,
        AuthorizationPolicy = "Contacts.Write",
        Cache = HtmxorCachePolicy.NoStore
    };

    public async ValueTask<HtmxRender> ExecuteAsync(
        HtmxorActionContext context,
        CancellationToken cancellationToken)
    {
        await contacts.DeleteAsync(
            context.RouteValues.GetRequired<Guid>("id"),
            cancellationToken);

        return HtmxRender.Fragments("contact-list", "contact-count");
    }
}
```

Policies and results are easy to inspect and unit test. The cost is a handler type and policy object for every action. Route knowledge moves out of the component and starts to resemble the controller split Htmxor exists to remove.

### Comparison and synthesis

The compiled-event design has the smallest interface, but it either rejects common Razor expressions or forces the implementation back into render-time event discovery. It is easy to write and too easy to make ambiguous.

Static local actions expose slightly more, but they make familiar ASP.NET binding annotations, dependency injection, authorization metadata, and antiforgery statically available before rendering. The implementation can stay on public framework seams if the binding spike proves equivalent behavior. This is the best depth tradeoff.

Typed action references make route construction safer, but they also hide the `hx-*` verb and introduce generated types at every call site. Literal URLs plus build-time diagnostics are a better v1 default. Typed references can remain an optional later addition.

Separate policy objects maximize control and testability. They lose the locality that distinguishes Htmxor. Their immutable policy and result types are still useful inside the generated implementation.

The synthesis is therefore:

1. use static, verb-decorated methods in the `.razor` file for stable actions;
2. return immutable `HtmxRender` values;
3. keep ordinary `hx-*` markup and validate statically resolvable routes;
4. compile immutable endpoint policies internally;
5. support current event syntax only where the build can lower it to the same action descriptor without runtime replay.

## Recommended application setup

The application registers Htmxor once. Both normal Blazor endpoints and direct Htmxor endpoints live under the same route group so common endpoint conventions apply structurally:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication()
    .AddCookie();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Contacts.Read", policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy("Contacts.Write", policy =>
        policy.RequireClaim("scope", "contacts.write"));
});

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddHtmxor(options =>
    {
        options.Assets = HtmxAssets.ApplicationOwned;
        options.Errors.DefaultComponent = typeof(DefaultHtmxProblem);
    });

builder.Services.AddOutputCache();
builder.Services.AddRateLimiter(options =>
    options.AddFixedWindowLimiter("ui", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    }));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();
app.UseOutputCache();

var ui = app.MapGroup("")
    .RequireRateLimiting("ui");

ui.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

ui.MapHtmxorEndpoints();

app.Run();
```

`AddHtmxor` and `MapHtmxorEndpoints` are proposed names. `MapHtmxorEndpoints` consumes the generated catalog. It does not scan assemblies or reflect over the private `RazorComponentsEndpointConventionBuilder` implementation. If the application inserts antiforgery middleware explicitly, it runs after authentication and authorization as shown.

The route group is important. Authorization, CORS, rate limiting, host restrictions, and other conventions applied to `ui` cover both endpoint families. Per-component and per-action attributes add narrower metadata.

## Reachability examples

Plain `@page` remains dual by default for compatibility:

```razor
@page "/contacts"
```

The explicit equivalent is:

```razor
@page "/contacts"
@attribute [HtmxReachability(HtmxReachabilityMode.Both)]
```

A normal-only page opts out of direct HTMX routing:

```razor
@page "/account/security"
@attribute [HtmxReachability(HtmxReachabilityMode.NormalOnly)]
```

An HTMX-only component keeps the existing `[HtmxRoute]` idea and has no `@page`:

```razor
@attribute [HtmxRoute(
    "/contacts/suggestions",
    Methods = [HttpMethods.Get])]
@attribute [HtmxReachability(HtmxReachabilityMode.HtmxOnly)]
```

`HtmxReachabilityMode.HtmxOnly` is a representation constraint. It is not authorization. A caller can forge `HX-Request`.

An HTMX-only URL is not automatically a valid browser-history location. A literal `hx-push-url` or boosted navigation to it is a build diagnostic unless the application declares a normal full-page fallback for that URL.

## One complete component

The following component shows the proposed route, local actions, inline fragments, multi-fragment results, standard endpoint policies, and honest execution behavior in one file.

```razor
@page "/contacts"
@attribute [HtmxReachability(HtmxReachabilityMode.Both)]
@attribute [HtmxLayout(typeof(ContactsHtmxLayout))]
@attribute [Authorize(Policy = "Contacts.Read")]
@attribute [EnableRateLimiting("ui")]
@attribute [RequestSizeLimit(32_768)]
@attribute [HtmxError(
    typeof(ContactNotFoundException),
    typeof(ContactNotFoundFragment),
    StatusCode = StatusCodes.Status404NotFound)]
@inject ContactStore ContactStore
@inject HtmxContext Htmx

<PageTitle>Contacts</PageTitle>

<HtmxFragment Name="editor">
    <EditForm id="editor"
              Model="Input"
              FormName="create-contact"
              hx-post="/contacts"
              hx-target="#contact-list"
              hx-swap="outerHTML">
        <AntiforgeryToken />
        <DataAnnotationsValidator />
        <HtmxServerValidation Messages="Validation" />
        <InputText @bind-Value="Input.Name" />
        <ValidationSummary />
        <button type="submit">Add</button>
    </EditForm>
</HtmxFragment>

<HtmxFragmentElement Name="contact-list" Id="contact-list">
    <ul>
        @foreach (var contact in Contacts)
        {
            <li>
                @contact.Name
                <button hx-delete="/contacts/@contact.Id"
                        hx-target="#contact-list"
                        hx-swap="outerHTML">
                    Delete
                </button>
            </li>
        }
    </ul>
</HtmxFragmentElement>

<HtmxFragment Name="contact-count">
    <span id="contact-count" hx-swap-oob="outerHTML">
        @Contacts.Count contact(s)
    </span>
</HtmxFragment>

<HtmxFragment Name="toast" RenderDuringStandardRequest="false">
    <div id="toast" hx-swap-oob="outerHTML" role="status">
        Contact saved.
    </div>
</HtmxFragment>

@code {
    [Parameter]
    public ContactInput Input { get; set; } = new();

    [Parameter]
    public HtmxValidationState Validation { get; set; }
        = HtmxValidationState.Empty;

    private IReadOnlyList<Contact> Contacts { get; set; } = [];

    protected override async Task OnParametersSetAsync()
    {
        // Full requests include every fragment. A selected response includes
        // only the named fragments. The owning component lifecycle still runs.
        if (Htmx.Selection.IncludesAny("contact-list", "contact-count"))
        {
            Contacts = await ContactStore.ListAsync();
        }
    }

    [HtmxPost]
    [Authorize(Policy = "Contacts.Write")]
    private static async Task<HtmxRender> CreateAsync(
        [FromForm] ContactInput input,
        ContactStore contacts,
        CancellationToken cancellationToken)
    {
        var result = await contacts.CreateAsync(input, cancellationToken);

        return result.IsValid
            ? HtmxRender
                .Fragments("contact-list", "contact-count", "toast")
                .Trigger(HtmxEvent.AfterSwap(
                    "contacts:changed",
                    new { result.ContactId }))
            : HtmxRender
                .HandledError(
                    "editor",
                    StatusCodes.Status422UnprocessableEntity)
                .WithParameters(new
                {
                    Input = input,
                    Validation = HtmxValidationState.From(result.Errors)
                })
                .Retarget(HtmxTarget.Id("editor"));
    }

    [HtmxDelete("{id:guid}")]
    [Authorize(Policy = "Contacts.Write")]
    private static async Task<HtmxRender> DeleteAsync(
        Guid id,
        ContactStore contacts,
        CancellationToken cancellationToken)
    {
        await contacts.DeleteAsync(id, cancellationToken);

        return HtmxRender.Fragments("contact-list", "contact-count");
    }
}
```

The proposed action templates are relative to the owning component route:

```text
GET     /contacts             render Contacts
POST    /contacts             invoke CreateAsync, then render its selection
DELETE  /contacts/{id:guid}   invoke DeleteAsync, then render its selection
```

If a component has several `@page` routes, a relative action must identify its base route. Ambiguous routes fail the build.

Static action methods use familiar ASP.NET binding annotations and injected parameter shapes. Htmxor's generated binder must implement and test the promised subset; it is not automatically the Minimal API binder. The implementation does not have to instantiate a Blazor component to invoke the action. After the mutation, Htmxor creates a new component response through the public Razor rendering path.

`HtmxServerValidation` is a proposed small component that copies `HtmxValidationState` into the current form's `ValidationMessageStore`. That makes application validation visible in the fresh render. Malformed input that fails generated binding before the action still needs the separate binding-error contract listed in the prototype gates below.

Generated actions are HTMX-only in the proposed v1 contract. An exact `HX-Request: true` selects the generated POST or DELETE candidate. Without that header, the candidate is ineligible and normal Blazor form handling applies if the application declared it; otherwise the request receives the normal 405/404 result. The example therefore does not imply a no-JavaScript POST fallback. A future progressive-enhancement mode would need an explicit normal handler and Post/Redirect/Get contract rather than silently reusing an HTMX fragment action.

## What the build emits

The generated code is implementation detail. An approximate catalog entry makes the security and performance model easier to see:

```csharp
private static void MapContacts(RouteGroupBuilder ui)
{
    ui.MapMethods(
            "/contacts",
            [HttpMethods.Get],
            static (HttpContext context) =>
                HtmxorResults.Render<Contacts>(context))
        .WithName("Htmxor.Contacts.GET")
        .WithMetadata(new HtmxorCandidateMetadata(
            routeFamily: "Contacts.GET",
            kind: HtmxorCandidateKind.DirectGet))
        .RequireAuthorization("Contacts.Read")
        .RequireRateLimiting("ui");

    ui.MapMethods(
            "/contacts",
            [HttpMethods.Post],
            ContactsActions.DispatchCreateAsync)
        .WithName("Htmxor.Contacts.POST.Create")
        .WithMetadata(
            new HtmxorCandidateMetadata(
                "Contacts.POST.Create",
                HtmxorCandidateKind.HtmxAction),
            new RequireAntiforgeryTokenAttribute(),
            HtmxorCachePolicy.UnsafeNoStore)
        .RequireAuthorization("Contacts.Read")
        .RequireAuthorization("Contacts.Write")
        .RequireRateLimiting("ui");

    ui.MapMethods(
            "/contacts/{id:guid}",
            [HttpMethods.Delete],
            ContactsActions.DispatchDeleteAsync)
        .WithName("Htmxor.Contacts.DELETE.Delete")
        .WithMetadata(
            new HtmxorCandidateMetadata(
                "Contacts.DELETE.Delete",
                HtmxorCandidateKind.HtmxAction),
            new RequireAntiforgeryTokenAttribute(),
            HtmxorCachePolicy.UnsafeNoStore)
        .RequireAuthorization("Contacts.Read")
        .RequireAuthorization("Contacts.Write")
        .RequireRateLimiting("ui");
}
```

The matching normal GET endpoint receives `HtmxorCandidateKind.NormalPage` and the same route-family id through a supported endpoint convention. A public `MatcherPolicy`/`IEndpointSelectorPolicy` runs during routing:

```csharp
public sealed class HtmxorCandidateMatcherPolicy
    : MatcherPolicy, IEndpointSelectorPolicy
{
    public override int Order => HtmxorMatcherOrder.AfterHttpMethod;

    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints) =>
        endpoints.Any(endpoint =>
            endpoint.Metadata.GetMetadata<HtmxorCandidateMetadata>()
                is not null);

    public Task ApplyAsync(
        HttpContext context,
        CandidateSet candidates)
    {
        var historyRestore =
            HtmxHeaders.IsExactlyTrue(
                context.Request.Headers["HX-History-Restore-Request"]);

        var directRequest = !historyRestore &&
            HtmxHeaders.IsExactlyTrue(
                context.Request.Headers["HX-Request"]);

        foreach (var routeFamily in candidates.HtmxorRouteFamilies())
        {
            routeFamily.KeepOnly(directRequest
                ? HtmxorCandidateKind.DirectGetOrHtmxAction
                : HtmxorCandidateKind.NormalPage);
        }

        return Task.CompletedTask;
    }
}
```

`AddHtmxor` registers this public matcher as a singleton `MatcherPolicy`. Its exact order relative to built-in method/host matching is part of the routing prototype, not an arbitrary value exposed to applications.

This header check is necessarily a routing discriminator, before authorization. It grants no permission: direct and normal GET paths enforce equivalent route-group and component base policies, while an action enforces those policies plus its action-specific restrictions. The normal path retains stock Blazor authorization semantics; the generated direct endpoint lifts the component requirements into endpoint metadata. History restoration always selects a full normal page, even if a misconfigured client also sends `HX-Request`.

Both the matcher and the convention that tags stock Razor candidates must be proved on public ASP.NET APIs. If the stock candidate cannot be tagged without private reflection, this same-path candidate design is not ready for v1.

Metadata alone is not the unsafe-verb enforcement mechanism. Every generated unsafe dispatcher validates before it binds request data or calls application code:

```csharp
private static async Task<IResult> DispatchDeleteAsync(
    HttpContext context)
{
    if (!await HtmxorAntiforgery.IsValidAsync(context))
    {
        return HtmxorResults.AntiforgeryRejected();
    }

    var arguments = await ContactsActionBinding
        .BindDeleteAsync(context);

    var contacts = context.RequestServices
        .GetRequiredService<ContactStore>();

    var render = await Contacts.DeleteAsync(
        arguments.Id,
        contacts,
        context.RequestAborted);

    return HtmxorResults.RenderNoStore<Contacts>(context, render);
}
```

The helper reuses the middleware verdict when it exists and validates explicitly otherwise:

```csharp
public static async ValueTask<bool> IsValidAsync(HttpContext context)
{
    var validation = context.Features
        .Get<IAntiforgeryValidationFeature>();

    if (validation is not null)
    {
        return validation.IsValid;
    }

    var antiforgery = context.RequestServices
        .GetRequiredService<IAntiforgery>();

    return await antiforgery.IsRequestValidAsync(context);
}
```

This avoids repeating POST/PUT/PATCH form parsing and cryptography after `UseAntiforgery`, while explicitly covering DELETE and configurations where middleware produced no verdict. An invalid verdict returns a generic 400 with `Cache-Control: private, no-store`; it carries no handled-fragment marker, and neither binding nor application code runs. Genuine service/configuration failures still propagate to the exception handler. The [`IAntiforgery` guidance](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0#http-method-limitations-and-httpmethodoverridemiddleware-interaction) explicitly calls for validation on DELETE. Regression tests cover failed middleware verdicts, missing/invalid DELETE tokens, missing cookie/header values, binder/action non-invocation, and the generic production response. The build-time binding spike must prove route, query, form, nullability, conversion-error, and validation behavior on this ordering.

The generator also reports:

- duplicate routes, methods, endpoint names, or fragment names;
- unsafe component routes without a declared action;
- a statically resolvable `hx-post`, `hx-put`, `hx-patch`, or `hx-delete` with no matching declared endpoint;
- a statically resolvable verb mismatch;
- an action that returns an unknown literal fragment name;
- a statically resolvable pushed/history URL that has no full-page representation;
- an HTMX target inside a known interactive Blazor root;
- an arbitrary fragment predicate that affects output caching but declares no variation fields.

Client markup supplies evidence for diagnostics. It never creates an endpoint. The action attribute and component route remain the server authority.

The analyzer may follow statically known child-component usages and literal `hx-get`, `hx-post`, `hx-put`, `hx-patch`, and `hx-delete` values to report a missing or mismatched declaration. It cannot soundly infer authority from a computed URL, attribute splat, conditional render, or reusable child that targets a different component. Those cases remain runtime links to an explicitly declared server action.

## Public Razor static rendering

The direct endpoint should render an ordinary response host with `RazorComponentResult`:

```csharp
private static IResult Render<TPage>(
    HttpContext context,
    HtmxRender render,
    IReadOnlyDictionary<string, object?> pageParameters)
    where TPage : IComponent
{
    var plan = HtmxRenderPlan.Create(context, render);
    var hostParameters = new Dictionary<string, object?>
    {
        [nameof(HtmxorResponseHost<TPage>.PageParameters)] = pageParameters,
        [nameof(HtmxorResponseHost<TPage>.Plan)] = plan
    };

    return new RazorComponentResult<HtmxorResponseHost<TPage>>(
        hostParameters)
    {
        ContentType = "text/html; charset=utf-8",
        StatusCode = plan.StatusCode,
        PreventStreamingRendering = plan.MustChooseBeforeHeaders
    };
}
```

`HtmxorResponseHost<TPage>` is an ordinary Razor component. It cascades the typed request and render plan, applies the selected `HtmxLayout`, and renders the owning page. `HtmxFragment` checks the plan before invoking its child content. Htmxor does not replace Blazor's private endpoint invoker or copy its HTML renderer.

`pageParameters` is produced by generated route/query conversion; `RazorComponentResult` does not bind route values to component parameters automatically. The complete plan, including status and selected error component, must exist before the result is returned. `PreventStreamingRendering` waits for quiescence but does not make late status changes or post-render error selection possible. A behavior that can fail during rendering either needs an already selected error result or a separately proved buffering result.

This approach borrows Rizzy's public component-result seam while preserving Htmxor's component-owned route and inline fragments. The result type and its static SSR behavior are documented by the [`RazorComponentResult` API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpresults.razorcomponentresult?view=aspnetcore-10.0).

## Inline, multiple, layout, and OOB output

`Name` extends the current fragment interface without removing target matching:

```razor
<HtmxFragmentElement Name="contact-list" Id="contact-list">
    ...
</HtmxFragmentElement>
```

The selection rules are:

1. A normal request renders the whole page.
2. A direct request with no explicit result can match `HX-Target` to `HtmxFragmentElement.Id`, preserving current behavior.
3. `HtmxRender.Fragment("contact-list")` selects one named region.
4. `HtmxRender.Fragments("contact-list", "contact-count", "toast")` selects several regions in source order.
5. Secondary regions declare ordinary `hx-swap-oob` markup or flow through existing `SectionContent` and `SectionOutlet` layout outlets.

The HTMX layout remains simple:

```razor
@inherits HtmxLayoutComponentBase

<SectionOutlet SectionName="notifications" />
@Body
<ToastOutlet />
```

The page supplies that outlet through standard Blazor sections:

```razor
<SectionContent SectionName="notifications">
    <aside id="notifications" hx-swap-oob="outerHTML">
        Contact saved.
    </aside>
</SectionContent>
```

There is no application-level OOB service that concatenates arbitrary HTML. The component and layout own the markup.

## Honest execution semantics

Named fragment selection controls output, not the entire component lifecycle.

For a request selecting `contact-list`:

- routing, authorization, parameter binding, component construction, and the owning component lifecycle run;
- child content under `contact-list` runs;
- child content under an unselected fragment need not run if `HtmxFragment` rejects it before invoking the render fragment;
- work placed directly in the owning component lifecycle still runs unless the developer gates it with `Htmx.Selection`;
- Htmxor does not promise that an arbitrary span of Razor source becomes an independently executable function.

The practical default is to put expensive fragment-specific loading inside a child component under that fragment. `Htmx.Selection.Includes` exists for the cases where the owning component must coordinate loading. This adopts templ's documented execution honesty without forcing a separate Go-style handler for every response.

## Typed request and response protocol

The existing `HtmxContext` is worth keeping. Its request values should parse the htmx protocol exactly and make untrusted values hard to mistake for server facts:

```csharp
public sealed record HtmxRequest
{
    public bool IsRequest { get; init; }
    public bool IsBoosted { get; init; }
    public bool IsHistoryRestore { get; init; }

    // Client supplied. Useful for representation selection, never authorization.
    public Uri? ClientCurrentUrl { get; init; }

    // Set only when the supplied URL matches the active scheme, host, and port.
    public PathString? SameOriginCurrentPath { get; init; }

    public HtmxTarget? Target { get; init; }
    public HtmxTrigger? Trigger { get; init; }
    public string? TriggerName { get; init; }
    public string? Prompt { get; init; }
}
```

Developer code remains concise:

```csharp
if (Htmx.Request.Target == HtmxTarget.Id("contact-list"))
{
    // Select a representation. Do not make an authorization decision here.
}
```

Boolean request headers count as true only when there is exactly one value and that value is `true`. Header presence alone is insufficient. URL-valued headers are parsed and same-origin checked before Htmxor exposes a local path.

The immutable `HtmxRender` result contains typed response parts:

```csharp
return HtmxRender
    .Fragments("contact-list", "contact-count")
    .Retarget(HtmxTarget.Id("contact-list"))
    .Reswap(HtmxSwap.OuterHtml)
    .Trigger(HtmxEvent.AfterSwap(
        "contacts:changed",
        new { ContactId = id }));
```

This borrows Htmx.Net's typed protocol helpers and axum-htmx's typed extractors and response parts. The protocol types remain usable outside component rendering.

## Automatic cache variation

Generated endpoint metadata knows which request fields can select a different representation. Htmxor always uses those facts for the HTTP `Vary` header. The same facts can drive ASP.NET output-cache lookup only on a response class that Htmxor can mechanically prove is safe to store.

The rules are deterministic:

- dual reachability adds `HX-Request`;
- a target condition adds `HX-Target`;
- a trigger condition adds `HX-Trigger`;
- a history-restore branch adds `HX-History-Restore-Request`;
- existing `Vary` values are merged case-insensitively;
- user-specific output still uses `private`, `no-store`, or an appropriate application cache policy.

The component does not repeat those headers. The following attribute is illustrative of a future cache opt-in, not a v1 guarantee until the token-free proof below exists:

```razor
@page "/contacts"
@attribute [OutputCache(PolicyName = "Contacts.Read")]

<HtmxFragmentElement Id="contact-list" Name="contact-list">
    ...
</HtmxFragmentElement>
```

The generated dual endpoint varies by `HX-Request`. Because selection can use the target id, it also varies by `HX-Target`:

```http
Vary: HX-Request, HX-Target
```

That response header and ASP.NET's output-cache key are separate operations. A simple generated endpoint can express the lookup variation with the public builder:

```csharp
endpoint.CacheOutput(policy => policy
    .SetVaryByHeader("HX-Request", "HX-Target"));
```

`SetVaryByHeader` affects the ASP.NET output-cache key; it does not emit the HTTP `Vary` response header. The two operations are deliberately shown separately. See the public [`OutputCachePolicyBuilder` API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.outputcaching.outputcachepolicybuilder.setvarybyheader?view=aspnetcore-10.0).

For a stable implementation, Htmxor should not place arbitrary attacker-supplied target strings into an unbounded cache key. Its generated `IOutputCachePolicy` first maps headers to a declared representation:

```csharp
public ValueTask CacheRequestAsync(
    OutputCacheContext context,
    CancellationToken cancellationToken)
{
    var representation = ContactsRepresentations.TrySelect(
        context.HttpContext.Request.Headers);

    if (representation is null)
    {
        context.AllowCacheLookup = false;
        context.AllowCacheStorage = false;
        HtmxorCacheHeaders.RequirePrivateNoStore(
            context.HttpContext.Response);
        return ValueTask.CompletedTask;
    }

    context.CacheVaryByRules.VaryByValues["htmxor"] =
        representation.CacheKey; // e.g. "normal" or "direct:contact-list"

    return ValueTask.CompletedTask;
}
```

The response stage separately merges the declared request-header names into HTTP `Vary`. Unknown target or trigger values disable lookup and storage; they do not create new cache keys.

An arbitrary predicate must declare what it reads:

```razor
<HtmxFragment Name="search-results"
              Match="@(request => request.Target?.Value == SearchTarget)"
              VaryBy="HtmxVary.Target">
    ...
</HtmxFragment>
```

If the dependency cannot be stated, Htmxor should disable shared caching for that representation rather than guess. `Vary` prevents representation collisions. It does not make authenticated or personalized output safe for a shared cache.

Every unsafe-action response is `private, no-store` regardless of copied component cache metadata. A response that contains a hidden or meta antiforgery request token must also never be stored in a shared output cache; the token is tied to the current antiforgery cookie and possibly the current identity.

The stable rule is fail-closed: Razor component output is not stored by default, and an unproved, unknown-selector, or dynamically composed representation receives both ASP.NET `NoCache()` and `Cache-Control: private, no-store`. It is insufficient to disable storage only when the generator happens to find an `AntiforgeryToken`, because an earlier cache lookup could serve a token stored by an unobserved nested component. The same `private, no-store` response prevents intermediary caches from creating raw variants for attacker-supplied target or trigger values.

Shared anonymous caching enters v1 only if a prototype supplies a mechanical, runtime-enforced token-free response class with canonical selector keys. Until then, Htmxor still emits correct HTTP `Vary`, but no component HTML is cacheable by the server, browser, or intermediary.

This adopts Htmx.Net's explicit variation support, django-htmx's cache guidance, and axum-htmx's automatic variation.

## Endpoint policy and request conditions

Standard ASP.NET metadata remains the policy interface:

```razor
@page "/contacts"
@attribute [Authorize(Policy = "Contacts.Read")]
@attribute [EnableRateLimiting("ui")]
@attribute [RequestSizeLimit(32_768)]
@attribute [OutputCache(PolicyName = "Contacts.Read")]
```

Actions can add authorization requirements:

```csharp
[HtmxDelete("{id:guid}")]
[Authorize(Policy = "Contacts.Write")]
private static Task<HtmxRender> DeleteAsync(...) => ...;
```

Generated HTMX endpoints receive the same component metadata and route-group conventions as the normal endpoint. Authorization policies compose with AND semantics, so the generated delete endpoint requires both `Contacts.Read` and `Contacts.Write`.

“Action metadata is additive” is too broad for metadata whose meaning is override-based. The proposed v1 precedence is explicit:

| Policy | Generated action rule |
| --- | --- |
| Authorization | Route-group, component, and action policies all apply |
| `AllowAnonymous` | It cannot coexist with a protected generated action at route-group, component, or action scope. Source-known conflicts fail the build; route-group conflicts fail startup validation |
| Antiforgery | Required and explicitly validated for every unsafe verb; no action opt-out in v1 |
| Rate limiting | Exactly one effective named policy is compiled. Matching group/component declarations are retained; conflicting names fail build/startup validation because ASP.NET rate-limit metadata is override-based. An action cannot replace it or apply `DisableRateLimiting` in v1 |
| Request size | Htmxor computes the minimum finite value across group, component, and action metadata, appends that effective limit with final precedence, and rejects `DisableRequestSizeLimit` or a requested increase |
| Output cache | Unsafe actions are always `private, no-store`; an action may make a safe response less cacheable |

Exact `HX-Request` and history-restore values run during routing only to choose an eligible representation candidate. Authentication, authorization, rate limiting, antiforgery, request limits, and binding then run for the selected endpoint. Target, trigger, and named-fragment selection occurs after those gates and cannot change endpoint policy.

The shared route group is the supported place for conventions that must cover both endpoint families. A convention attached only to the `MapRazorComponents` builder does not automatically reach separately generated endpoints. At startup, `MapHtmxorEndpoints` compares security-relevant effective metadata for every normal/direct route family and fails on missing authorization, anonymous override, limiter, host, request-size, or other protected parity. A future composite convention builder may forward conventions to both families; v1 must not silently assume that forwarding happened.

`HX-Request`, `HX-Target`, `HX-Trigger`, fragment names, and action URLs are forgeable input. They may choose among representations that already carry equivalent base policies. They never authorize an action or enable a method.

This is the RazorX idea adapted to Htmxor: ordinary endpoint policies without a per-component Minimal API mapping.

## Unsafe verbs and authentication flows

Every generated POST, PUT, PATCH, and DELETE endpoint requires antiforgery by default. The endpoint validates the token before action invocation. DELETE is not a special case.

Forms use the standard Blazor token:

```razor
<EditForm hx-post="/contacts" ...>
    <AntiforgeryToken />
    ...
</EditForm>
```

Non-form controls such as `hx-delete` need the same protection. `HtmxHeadOutlet` can emit the ASP.NET request token in a meta element, and the separately versioned Htmxor browser adapter adds the configured antiforgery header to unsafe same-origin requests. Any full response containing that per-request meta token is marked `private, no-store`. The antiforgery cookie remains `HttpOnly`; Htmxor does not create a second JavaScript-readable token cookie on every response.

Under htmx 2, DELETE parameters are URL encoded by default. Generated DELETE binding therefore reads declared route values first and query values for remaining inputs; it must not assume the htmx 1 form-body behavior. Antiforgery still comes from the configured header and is validated before that binding.

Cookie authentication can opt into HTMX-aware redirects:

```csharp
builder.Services
    .AddAuthentication()
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/denied";

        options.Events.UseHtmxorRedirects(
            HtmxorAuthNavigation.FullPage);
    });
```

Normal requests retain normal cookie redirects. An exact HTMX request receives `HX-Redirect` to a validated local URL and an explicitly selected non-3xx status, such as 401 or 403, so the login document does not get swapped into a page fragment. htmx does not process response headers from an intercepted 3xx response, as its [`HX-Redirect` contract](https://htmx.org/headers/hx-redirect/) notes. The adapter composes with application cookie events instead of replacing them, and tests multiple cookie schemes as well as login and access-denied paths.

This adopts htmx-spring-boot's unsafe-verb CSRF and authentication-flow handling while retaining ASP.NET authorization and antiforgery.

## Error selection

FastHX's error selector becomes a component-scoped declaration:

```razor
@attribute [HtmxError(
    typeof(ContactNotFoundException),
    typeof(ContactNotFoundFragment),
    StatusCode = StatusCodes.Status404NotFound)]

@attribute [HtmxError(
    typeof(ContactValidationException),
    typeof(ContactValidationFragment),
    StatusCode = StatusCodes.Status422UnprocessableEntity)]
```

The error component receives `ProblemDetails` and declared parameters:

```razor
@* ContactNotFoundFragment.razor *@
@code {
    [Parameter, EditorRequired]
    public required ProblemDetails Problem { get; set; }
}

<section id="contact-problem" role="alert">
    <h2>Contact not found</h2>
    <p>@Problem.Detail</p>
</section>
```

Known application failures select declared HTML. Authorization, antiforgery, binding, and request-size failures occur before the selector. Unexpected exceptions continue to ASP.NET's exception handler. The v1-safe case is an action or generated loader outcome known before `RazorComponentResult` begins. Mapping arbitrary component-lifecycle/render exceptions requires a separately proved buffering result and is not implied by `[HtmxError]`.

htmx 2 does not swap 4xx or 5xx bodies by default. Both an `[HtmxError]` mapping and an action's explicit `HtmxRender.HandledError(...)` declare an application-error fragment. Htmxor buffers that result and adds the marker only after the error fragment renders successfully. Calling `.WithStatus(422)` alone does not mark a response as handled.

```http
HTTP/1.1 422 Unprocessable Entity
HXOR-Fragment-Response: true
HX-Retarget: #editor
```

The small browser adapter changes swap handling only for that server marker:

```javascript
document.addEventListener("htmx:beforeSwap", event => {
  const handled = event.detail.xhr
    .getResponseHeader("HXOR-Fragment-Response") === "true";

  if (handled) {
    event.detail.shouldSwap = true;
    event.detail.isError = false;
  }
});
```

An undeclared 404, binding failure, antiforgery failure, or unexpected 500 has no marker and retains htmx's normal no-swap/error behavior. This is narrower than globally configuring every 4xx response to swap. It follows htmx's documented [`responseHandling` and 422 behavior](https://htmx.org/docs/#response-handling) while letting the server distinguish an intentional error fragment from a transport or security failure.

A global fallback remains available at registration:

```csharp
builder.Services.AddHtmxor(options =>
    options.Errors.DefaultComponent = typeof(DefaultHtmxProblem));
```

## Runtime independence and extension contract

Htmxor's server package should neither contain an htmx script nor declare a JavaScript-version dependency. It should not inspect `htmx.version`, reject an unknown browser runtime, or make a tested patch version an installation requirement. The application owns the htmx runtime, its configuration, extensions, load order, CSP, and upgrade schedule.

The default application setup is therefore ordinary application code:

```razor
<head>
    <meta name="htmx-config"
          content='@ApplicationHtmxConfiguration' />

    <HtmxHeadOutlet Nonce="@CspNonce" />

    <script type="module"
            src="/js/application-htmx.js"
            nonce="@CspNonce"></script>
</head>
```

`HtmxHeadOutlet` has a deliberately narrow role: emit Htmxor-owned metadata such as the antiforgery request token and the handled-fragment marker. It obtains the token with `IAntiforgery.GetAndStoreTokens`, which also establishes the framework's no-cache/no-store response headers; it must not use the non-storing `GetTokens` shortcut. It emits neither htmx nor an `htmx-config` schema.

The application can use any htmx distribution and load native htmx extensions normally:

```javascript
import htmx from "/vendor/htmx/htmx.esm.js";
import "/vendor/htmx/extensions/response-targets.js";

import {
  createHtmxorBridge,
  antiforgery,
  handledFragments
} from "/_content/Htmxor/browser-core.js";

import { adaptHtmx2 } from "/_content/Htmxor/browser-htmx2.js";

const bridge = createHtmxorBridge({
  plugins: [antiforgery(), handledFragments()]
});

adaptHtmx2(htmx).connect(bridge);
```

The names are illustrative, but the ownership boundary is not: `browser-core` contains Htmxor behavior without importing htmx, while the small runtime adapter translates the selected htmx release's public lifecycle events. A future runtime that changes event names or event details needs a new adapter, not a new server package.

The adapter contract should normalize only the lifecycle Htmxor actually needs:

```typescript
export interface HtmxorRuntimeAdapter {
  readonly id: string;
  readonly runtimeVersion?: string; // diagnostics only
  connect(bridge: HtmxorBrowserBridge): Disposable;
}

export interface HtmxorBrowserBridge {
  configureRequest(request: NormalizedRequest): void;
  inspectResponse(response: NormalizedResponse): SwapDecision;
  afterSwap(update: NormalizedDomUpdate): void;
  afterSettle(update: NormalizedDomUpdate): void;
}
```

For example, an application could adapt a future release immediately:

```javascript
const htmxNextAdapter = {
  id: "league-htmx-next",
  runtimeVersion: htmx.version,

  connect(bridge) {
    const disposals = [
      onHtmxNextRequest(event =>
        bridge.configureRequest(normalizeNextRequest(event))),
      onHtmxNextBeforeSwap(event =>
        applyDecision(event,
          bridge.inspectResponse(normalizeNextResponse(event))))
    ];

    return { dispose: () => disposals.forEach(dispose => dispose()) };
  }
};

htmxNextAdapter.connect(bridge);
```

Htmxor should publish a maintained htmx 2 reference adapter and may publish a separate htmx 4 preview adapter. Neither adapter includes htmx or constrains which htmx version the application serves. Optional asset bundles may exist as conveniences, but they are never transitive dependencies of the server or browser-adapter packages.

This split follows the extension points htmx itself exposes. Htmx 2 supports request-header changes through `htmx:configRequest`, swap decisions through `htmx:beforeSwap`, and client extensions through `htmx.defineExtension`. Its [`hx-ext` attribute](https://htmx.org/attributes/hx-ext/) composes extensions in markup. The [htmx 4 preview extension documentation](https://four.htmx.org/docs/extensions/using-extensions) already describes a different registration and event-hook model, which is evidence against freezing Htmxor around one JavaScript API.

### Unknown markup is valid markup

Razor already permits arbitrary attributes, and Htmxor should preserve that property:

```razor
<button hx-post="/contacts"
        hx-ext="toast"
        hx-toast="success"
        hx-some-future-feature="value">
    Save
</button>
```

Analyzers may understand known attributes and diagnose known-invalid combinations. They must not reject or strip an unknown `hx-*` attribute. Client extensions remain native htmx JavaScript; Htmxor should not wrap every htmx extension API.

### Typed server protocol extensions

New htmx or application extensions may introduce request and response headers. The public server seam should be one declarative registry rather than unrestricted per-request middleware:

```csharp
builder.Services.AddHtmxor(options =>
    options.Protocol.Use<ToastProtocol>());
```

```csharp
public sealed class ToastProtocol : IHtmxorProtocolExtension
{
    public static readonly HtmxorRequestFeature<ToastPreference> Preference =
        HtmxorRequestFeature.Create<ToastPreference>("toast.preference");

    public static readonly HtmxorResponseFeature<ToastMessage> Show =
        HtmxorResponseFeature.Create<ToastMessage>("toast.show");

    public void Configure(HtmxorProtocolBuilder protocol)
    {
        protocol.RequestHeader(
            Preference,
            name: "HX-Toast-Preference",
            parser: ToastPreference.TryParse,
            invalidValue: HtmxorInvalidValue.BadRequest,
            affectsRepresentation: true);

        protocol.ResponseHeader(
            Show,
            name: "HX-Toast",
            formatter: ToastJson.Serialize,
            kind: HtmxorHeaderKind.Opaque,
            maxBytes: 4096);
    }
}
```

The component-facing interface stays small:

```csharp
var preference = Htmx.Request.Get(ToastProtocol.Preference);

return HtmxRender
    .Fragment("contact-list")
    .With(
        ToastProtocol.Show,
        new ToastMessage("Contact saved", ToastKind.Success));
```

Registration compiles an immutable feature registry at startup. Parsing and formatting use that registry without reflection or service discovery on each request. A feature marked `affectsRepresentation` automatically adds its owned request header to `Vary`. Custom request values disable shared output caching by default; opting into shared caching requires a bounded canonicalizer with a finite value set.

A validated raw escape hatch can exist for experimentation, but using a raw request value to influence output must force `private, no-store`, and raw response headers still pass through Htmxor's size, newline, protected-header, URL, and conflict validation. The typed registry is the supported path for a reusable extension.

Protocol extensions cannot:

- create a route or HTTP verb;
- change normal-only, HTMX-only, or dual reachability;
- remove authorization, antiforgery, rate-limit, host, cache, CSP, or request-size rules;
- treat an HTMX header or advertised capability as trusted identity;
- access or mutate `HttpContext` directly;
- replace the renderer or a primary response body;
- overwrite a core or already-owned header.

The fixed request order enforces those limits:

1. ASP.NET selects a generated, server-declared route and HTTP verb.
2. Authentication, authorization, rate limiting, and request-size policies run.
3. Htmxor validates antiforgery for every unsafe action.
4. Htmxor parses core and registered request features.
5. Binding and the component-local action run.
6. Htmxor builds the immutable render result and formats registered response features.
7. Htmxor applies final header, URL, cache, and security invariants.
8. Static Razor rendering and response commitment run.

The browser bridge may add the antiforgery header, but the server remains authoritative: omitting or changing it makes an unsafe request fail before binding or application code. Custom browser JavaScript is application code and cannot be made trustworthy by a Htmxor abstraction.

### A verified htmx 2 profile, not a required version

Htmxor should test and document a high-security htmx 2 profile while leaving its exact configuration in application-owned markup:

```html
<meta name="htmx-config"
      content='{
        "historyRestoreAsHxRequest": false,
        "reportValidityOfForms": true,
        "allowNestedOobSwaps": false,
        "scrollBehavior": "instant",
        "allowEval": false,
        "allowScriptTags": false,
        "includeIndicatorStyles": false
      }'>
```

- `historyRestoreAsHxRequest: false` ensures a cache miss requests a complete page. The server matcher also treats `HX-History-Restore-Request: true` as normal-page routing for defense in depth.
- `reportValidityOfForms: true` restores the browser's visible validity reporting before submission. Server validation remains mandatory.
- `allowNestedOobSwaps: false` makes only top-level secondary fragments OOB responses. An application may choose different semantics and own the corresponding browser evidence.
- `allowEval: false`, `allowScriptTags: false`, and external indicator CSS form the strict-CSP profile. An explicit compatibility profile may relax those choices.

Htmxor should not mirror the entire `htmx.config` object into a C# DTO. Otherwise every new htmx option would require a Htmxor release. The adapter and conformance suite, rather than a frozen configuration type, are the compatibility seam.

The generated action binder and analyzer still need runtime-specific evidence. Under the verified htmx 2 profile, DELETE values arrive in the URL by default, current `hx-on:` syntax replaces legacy `hx-on`, and history, OOB, validation, redirects, and extensions receive browser coverage. These requirements come from the official [htmx 1-to-2 migration guide](https://htmx.org/migration-guide-htmx-1/), [events](https://htmx.org/events/), [extension interface](https://htmx.org/extensions/building/), current [configuration and history guidance](https://htmx.org/docs/), and the post-prototype [2.x changelog](https://github.com/bigskysoftware/htmx/blob/master/CHANGELOG.md).

## Blazor Interactive Auto coexistence

The supportable first model gives HTMX and interactive Blazor separate DOM ownership:

```razor
<body>
    <div id="htmx-shell"
         hx-boost="true"
         hx-target="#htmx-shell"
         hx-history-elt
         data-enhance-nav="false">
        <StaticLeagueSummary />
    </div>

    <div id="blazor-shell" hx-disable>
        <Routes @rendermode="InteractiveAuto" />
    </div>

    <script src="_framework/blazor.web.js"></script>
</body>
```

Direct Htmxor endpoints return static SSR and may target only the HTMX-owned region. Blazor owns the interactive root. Htmxor should diagnose statically knowable targets inside that root.

The ownership attributes are part of the safety contract. Without `hx-target`, a boosted request targets `body` and can replace the Blazor shell. `hx-history-elt` limits HTMX snapshots to the stable HTMX island, which must exist on every participating page. `data-enhance-nav="false"` prevents Blazor enhanced navigation from intercepting links inherited from the HTMX island. A history-cache miss still requests the full normal page; htmx restores only the declared history element from it.

The following remains a prototype question rather than a v1 promise:

```razor
@page "/contacts"
@rendermode InteractiveAuto
@attribute [HtmxReachability(HtmxReachabilityMode.Both)]
```

The normal route should use stock Interactive Auto. The direct route would render the same component through static `RazorComponentResult`. The prototype must prove that the direct response contains no interactive markers, that neither runtime removes DOM owned by the other, and that `hx-boost` and Blazor enhanced navigation do not both own one navigation.

Interactive Auto does not turn an HTMX fragment into a Server or WebAssembly component later. The two routes are separate render paths.

The runtime distinction follows the [Blazor render-mode contract](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0), including the rule that an existing Auto component does not later change its selected render mode in place.

## Compatibility as executable evidence

The compatibility matrix records what Htmxor CI has proved. It is not a package dependency, supported-version range, or runtime allowlist:

```json
{
  "server": {
    "htmxor": "1.0.x",
    "dotnet": "10.0.x",
    "aspnetCore": "10.0.x"
  },
  "browserRuns": [
    {
      "htmx": "2.0.10",
      "adapter": "Htmxor.Browser.Htmx2 1.0.x",
      "claim": "verified"
    },
    {
      "htmx": "4.0.0-beta.*",
      "adapter": "Htmxor.Browser.Htmx4.Preview",
      "claim": "preview"
    }
  ]
}
```

An exact row means “verified with this runtime and adapter,” not “requires this runtime.” An application may serve a newer or custom htmx build. Htmxor never rejects it based on a claimed client version; it is simply unverified until the application or Htmxor runs the conformance suite.

The conformance kit should contain raw HTTP fixtures for request parsing, selection, variation, response merging, and security ordering; fake-runtime tests for the normalized browser bridge; and Playwright tests against a caller-supplied htmx script. The public runner should cover unsafe requests, handled and unhandled 4xx responses, history restoration, redirects, OOB and preserved nodes, boosted forms, native extensions, and Blazor DOM ownership. A representative invocation might be:

```text
dotnet test Htmxor.Client.Conformance -- HtmxScript=/vendor/htmx-next.js
```

Htmxor-maintained rows run that same suite in CI and generate the published evidence table. Applications upgrading ahead of the matrix can run it against their chosen script and adapter, then record the result as externally adapted evidence.

## Source-by-source adoption map

| Source | Proposed Htmxor code or behavior |
| --- | --- |
| Htmx.Net | Keep `HtmxContext`; use exact typed request values and immutable typed response parts; calculate `Vary`; default to caller-owned htmx |
| Rizzy | Return direct responses through public `RazorComponentResult`; retain `HtmxLayout`, inline fragments, `SectionContent`, and ordinary OOB markup |
| RazorX | Generate ordinary endpoint metadata; let route-group, component, and action policies cover direct endpoints without per-feature mappings |
| Django 6 / django-htmx | Add `HtmxFragment.Name`; keep fragments inline; parse exact headers; retain CSP-aware guidance and cache variation without owning the runtime |
| FastHX | Add explicit reachability and component-scoped error selection; keep the renderer behind a small result interface |
| holm | Discover component routes and local static actions; use explicit verb attributes and deterministic generated endpoint names |
| htmx-spring-boot | Validate antiforgery on every unsafe verb; adapt authentication redirects; return several named fragments; drive support claims from a compatibility file |
| axum-htmx | Use typed request/response values; add automatic variation; document every HTMX request value as forgeable representation input |
| templ | Keep named fragments inline and state exactly which lifecycle and child-component work still runs |

## Decisions still requiring prototypes

The code examples make the desired interface visible, but these implementation questions still need evidence before the names are frozen:

1. Select a supported Razor-aware build seam for component routes, static action methods, literal `hx-*` diagnostics, and fragment names. Prove Razor symbol discovery and same-partial-type access without depending on source-generator ordering.
2. Prove that generated private static action dispatch can copy parameter attributes and nullability, explicitly validate antiforgery, bind only afterward through public ASP.NET seams, and invoke the method without exposing generated application endpoints.
3. Define malformed-input and form-validation flow from generated binding into a fresh component `EditContext` and `ValidationMessageStore`; `RazorComponentResult` does not provide that transfer automatically.
4. Resolve relative actions on components with multiple `@page` routes.
5. Decide whether custom route constraints can participate in optional typed action references.
6. Define fragment-name scope, nesting, duplicate diagnostics, and multi-fragment output order.
7. Define how arbitrary `Match` predicates declare cache dependencies.
8. Prove same-path stock/direct endpoint tagging and selection with a public matcher and endpoint convention, including startup parity validation for builder-only security conventions.
9. Prove error selection, status changes, marked 4xx swaps, and streaming behavior before response headers start.
10. Prove static rendering of a component that also declares `@rendermode InteractiveAuto`.
11. Prove a mechanically token-free, canonical-selector response class before enabling any server, browser, or intermediary caching for Razor component HTML.
12. Prove that the typed protocol registry rejects header collisions and malformed or oversized values, derives `Vary`, and cannot run before security policies or mutate endpoint authority.
13. Publish the browser-adapter conformance runner and prove that an application-owned htmx build can pass without changing or rebuilding `Htmxor.Server`.
14. Measure normal page, direct whole-component, single-fragment, multi/OOB, form action, and error paths before setting a v1 performance contract.

## Recommendation

Adopt the borrowed ideas through one deep server module, one declarative protocol-extension seam, and one replaceable browser adapter. If the build and binding spikes pass, the application developer should learn component reachability, static local actions, named fragments, and one immutable render result. Htmxor should handle endpoint policies, protocol parsing, antiforgery, cache variation, authentication redirects, and public static rendering. The application should own htmx itself.

The key authoring model remains unchanged: feature routes and output live in `.razor`. The action implementation changes from replaying a renderer-owned callback to invoking a generated, route-bound HTTP handler colocated with that component.
