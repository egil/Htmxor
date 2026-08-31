Parent: #77

## Outcome

Make normal-only, HTMX-only, and dual component reachability—and every generated
component action—obvious from the component declaration and from build
diagnostics.

Protected behavior:

> When a developer declares a component route or instance callback, Htmxor
> either generates exactly that authorized route/method or reports at the
> authoring location why it cannot, without treating client `hx-*` markup as
> server authority.

## Problem

The agreed v1 convention is simple: `@page` provides normal routing plus a
direct htmx GET, a component-local `HtmxRoute` provides an HTMX-only route, GET
is the only implicit method, and stock forms or statically discoverable
instance callbacks provide unsafe methods. The current public/compiler surface
still has avoidable uncertainty:

- the normal-only opt-out has no frozen API;
- `HtmxRoute` publicly advertises `CurrentURL`, `Target`, and `Targets`, while
  the current v1 generator path rejects named arguments other than `Methods`;
- the supported static Razor grammar and dynamic-case escape hatch are not a
  clear developer contract; and
- two broad diagnostic IDs cover several materially different authoring
  failures.

Client headers and DOM identity are untrusted representation hints. Route plus
HTTP method must identify the server capability.

## Scope

- Freeze one component-local normal-only declaration and its conflicts with
  HTMX-only declarations.
- Make `.razor`, matching `.razor.cs`, and pure-C# route declarations equivalent
  wherever the authoring model permits.
- Remove or fully implement every public `HtmxRoute` member; no IntelliSense
  affordance may be a guaranteed build error.
- Freeze the supported stock-form and `@onpost`/`@onput`/`@onpatch`/
  `@ondelete`/`@onquery` instance callback shapes.
- Provide one narrow explicit method/action declaration for genuinely dynamic
  or compiler-ambiguous cases.
- Emit cause-specific, location-specific diagnostics for unsupported,
  malformed, duplicate, contradictory, dynamic, static, or unsafe declarations.
- Optionally diagnose a statically known client method with no server action,
  but never reject unknown attributes, extension syntax, or newer values.

## Acceptance criteria

- [ ] The normal-only, HTMX-only, and dual route matrix has executable tests for
      Razor, code-behind, and pure-C# components.
- [ ] GET remains the sole implicit method.
- [ ] An explicit `Methods` declaration is documented as the complete allow-list
      and validated accordingly.
- [ ] No public route property is rejected merely because the supported pipeline
      never implements it.
- [ ] Every unsupported declaration reports the source location, omitted
      generated behavior, and narrow remediation.
- [ ] Dynamic/ambiguous binding cannot silently omit or broaden a route.
- [ ] Static handlers remain rejected; callbacks execute on the request-created
      component instance.
- [ ] `hx-get`, `hx-post`, `hx-action`, `hx-method`, and `hx-query` never grant
      route or method reachability.
- [ ] Unsafe actions retain authorization metadata and fail antiforgery before
      binding or callback execution.
- [ ] Packed-package tests cover the supported authoring forms and diagnostics.

## Exclusions

- Controllers, Minimal API duplicates, or static endpoint-style callbacks.
- Selecting or authorizing routes from `HX-Target`, `HX-Source`,
  `HX-Current-URL`, or other client headers.
- Transparent runtime action discovery.
- Fragment selection and response delivery.

## Evidence

Start with a meaningful behavioral or analyzer red for each added contract.
Record exact red/green SHAs, generator/analyzer and HTTP test counts, packed
consumer evidence, and unexercised Razor/compiler shapes. Separate Standards and
Spec reviews are required.
