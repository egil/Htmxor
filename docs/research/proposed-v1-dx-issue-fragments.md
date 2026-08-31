Parent: #77

## Outcome

Separate the server decision about which component branches execute and render
from htmx's client decision about where returned HTML is delivered.

Protected behavior:

> When a direct request selects one or several named fragments, Htmxor emits
> those component-owned boundaries in the declared order without making DOM IDs,
> CSS selectors, or forgeable HTMX headers server capability keys.

## Problem

`HtmxFragment` is the correct single server fragment concept, but the current
selection API combines several concerns:

- `Id` both requests a wrapper and can become the default direct-selection key;
- `Match` can run an arbitrary predicate over untrusted request data;
- `RenderDuringStandardRequest` adds another implicit branch; and
- a wrapperless fragment without an ID matches direct requests broadly.

Multi-fragment responses therefore require readers to execute predicates and
flags mentally. Renaming a DOM ID can change server execution, and raw
`<hx-partial>` or OOB markup can be mistaken for the thing that selects server
work.

## Scope

- Add a stable server fragment name independent of `Element`, `Id`, and other
  wrapper attributes.
- Define explicit selection of the whole component, one fragment, and an ordered
  set of fragments.
- Define duplicate names, unknown names, nesting, default selection, and normal
  full-request behavior.
- Keep `HtmxFragment` wrapperless by default and retain optional wrapper element,
  ID, and arbitrary attributes.
- Preserve raw `hx-swap-oob`, `hx-select-oob`, and `<hx-partial>` markup as
  client-delivery composition inside selected output.
- Specify lifecycle/execution behavior at and below the selection boundary.
- Define the cache-vary input for any application-selected fragment set.

## Acceptance criteria

- [ ] Server fragment names do not alter emitted markup unless the developer
      separately supplies wrapper attributes.
- [ ] DOM IDs and `HX-Target`/`HX-Source` are not required to select server
      fragments.
- [ ] Whole, single, and ordered multi-fragment responses have one obvious API
      and deterministic output.
- [ ] Duplicate and unknown names fail predictably with useful diagnostics or
      documented HTTP behavior.
- [ ] Normal full rendering includes the documented fragment content without
      requiring direct-request flags.
- [ ] Excluded child branches below a known boundary do not render and do not run
      their sync/async lifecycle or data work.
- [ ] The owning component and required ancestors retain the explicitly
      documented lifecycle behavior.
- [ ] Raw OOB and `<hx-partial>` content reaches the client unchanged and htmx 4
      main-before-additional ordering is covered by browser evidence.
- [ ] Concurrent requests cannot leak fragment selection across request scopes.
- [ ] Cache guidance identifies every representation input used by the test app.

## Exclusions

- A second `HtmxFragmentElement` or typed `<hx-partial>` hierarchy.
- Treating `HX-Target`, CSS selectors, or HTMX headers as authorization.
- Streaming fragment updates or detached interactive hydration.
- Claiming that excluded owners/ancestors never run.

## Evidence

Record meaningful-red execution-count tests before implementation. Green
evidence must include exact HEAD, lifecycle/data counters, ordered response
markup, concurrent isolation, htmx 4.0.0 browser delivery, and every lifecycle or
browser dimension not exercised. Separate Standards and Spec reviews are
required.
