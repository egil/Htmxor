# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Authority

[`AGENTS.md`](AGENTS.md) is the source of truth for how to work here — evidence rules, v1 scope limits, git and GitHub policy, verification, and review. Read it and the documents it links before changing product behavior. This file only adds what it does not cover.

## Iterating on a single test

`docs/agents/testing.md` owns the authoritative quality commands. Those profiles restore, format-check, and build Release before any test runs, so they are slow for a single-test edit loop. While iterating, run one project with a filter:

```bash
dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --filter "FullyQualifiedName~RequestRoutingTest"
```

Browser tests are the exception: the `fast` profile excludes them (`Category!=Browser`, `FullyQualifiedName!~Htmxor.E2E`), and running them needs Chromium provisioned from the generated installer at `test/Htmxor.Tests/bin/<Configuration>/net10.0/playwright.ps1`.

A filtered Debug run is a working signal, never the evidence a change is verified — that comes from the repository-owned quality command.

## Architecture

Htmxor makes a Blazor static-SSR component answer HTMX requests directly, with no parallel controller or endpoint layer. `src/Htmxor` multi-targets `net10.0;net11.0` and the request path composes four layers:

- **`Htmxor.Generators`** (`src/Htmxor.Generators`, `netstandard2.0`) is a Roslyn generator and analyzer that ships inside the same NuGet package under `analyzers/dotnet/cs/`. It reads `@page`, `HtmxRoute`, and statically discoverable `@onpost`/`@onput`/`@onpatch`/`@ondelete` bindings and emits route and action manifests. Route and HTTP-method discovery is compile-time; adding a runtime reflection scan instead is the wrong layer.
- **`Builder/`** turns those manifests into endpoints. `ComponentEndpointDataSource` builds the endpoint list from the attributed-route and generated-action catalogs; `ComponentEndpointMatcherPolicy` and `HtmxorDirectEndpointMatcherPolicy` then choose between the two `RoutingMode` values — `Standard` (request goes through the root component and `Router`, as in stock Blazor) and `Direct` (request goes straight to the matched component, wrapped only by `HtmxLayoutAttribute`). `HtmxorRouteProcessorFactory` emits metadata-only `RouteAttribute` types through public `Reflection.Emit` for HTMX-only routes, so dynamic-code support is required and Native AOT is outside the v1 goal.
- **`Endpoints/`** invokes a component instance per request. `HtmxorComponentEndpointInvoker` drives `HtmxorComponentRequestHost` (standard) or `HtmxorDirectComponentHost` (direct), both `IComponent` roots — request handling runs on the component instance, so DI, lifecycle, route/query values, form state, and authentication all stay available.
- **`Rendering/`** is a partial-class static HTML renderer (`HtmxorRenderer.*.cs`) paired with `Rendering/Buffering`. `IConditionalRender` plus `ConditionalBufferedTextWriter` let a component tree render once while only the requested fragments and out-of-band content reach the response.

`Http/` (`HtmxContext`, `HtmxRequest`, `HtmxResponse`) is the typed surface over `hx-*` request and response headers, and the only place header names belong. Client behavior is authored with native Razor `hx-*` attributes and literal htmx values; there is deliberately no trigger or swap-builder DSL to re-add, and the application supplies and configures the htmx runtime.

### The framework adapter

`Endpoints/HtmxorEndpointCandidate*` is the approved adapter: ASP.NET Core endpoint-renderer coordination narrowly reimplemented from `dotnet/aspnetcore`, carrying `Endpoints/ASP.NET-Core-LICENSE.txt` (packed into the NuGet under `licenses/`). Every file records its exact upstream source, tag, and commit in a header comment, and `eng/Htmxor.UpstreamMonitor` checks those against `eng/Htmxor.UpstreamMonitor/upstream-watch.json` — the drift gate is `check --profile upstream`, a fourth profile alongside `fast`, `full`, and `mutation`.

This is the only place private framework access and copied upstream coordination are permitted, and only under the conditions in `AGENTS.md`. Editing one of these files means updating its provenance header and the watch manifest in the same change.

## Test projects

Each covers a different boundary, and the quality profiles run all of them:

- `test/Htmxor.Tests` — the main suite: unit, bUnit component, Alba in-process HTTP against `test/Htmxor.TestApp`, and Playwright browser tests under `E2E/`.
- `test/Htmxor.AspNetCore10.Tests` — component-endpoint behavior, one file set per tracked v1 issue number. It multi-targets `net10.0;net11.0` with pinned runtime versions and runs as two separate boundaries, so a change must hold on both.
- `test/Htmxor.Quality.Tests` and `test/Htmxor.UpstreamMonitor.Tests` — the `eng/` tooling itself, including the code-metrics policy validator and the upstream watch fixtures.

New projects declare a `CodeMetricsProfile` in their `.csproj` from the first commit (`tests` under `test/`, `production` elsewhere); the `legacy-*` profiles belong to the audited existing paths only.
