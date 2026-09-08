# Server fragment selection

Issue [#168](https://github.com/egil/Htmxor/issues/168) implements the valid flat
selection portion of the approved
[#167 contract](https://github.com/egil/Htmxor/issues/167#issuecomment-5531911245).

When application code selects whole, one stable fragment name, or an ordered
valid flat set during a direct request, Htmxor completes normal rendering and
emits exactly the component-owned HTML in caller order.

`HtmxFragment.Name` identifies a server fragment. It is case-sensitive, does not
emit an HTML attribute, and does not request a wrapper. `Element`, `Id`, and
additional attributes independently request and describe optional wrapper
markup. Neither wrapper identifiers nor `HX-Target` or `HX-Source` choose server
fragments.

Component-instance code uses the request's `HtmxContext.Response`:

```csharp
Htmx.Response.SelectWholeComponent();
Htmx.Response.SelectFragment("Totals");
Htmx.Response.SelectFragments("Totals", "Rows");
```

Each operation replaces the previous selection and returns the same response
object. `SelectedFragmentNames` exposes a read-only snapshot in caller order;
an empty list means whole-component selection. The response copies the caller's
array. Selection operations can run in normal lifecycle code on either request
path. Normal requests always emit the complete page; direct requests default to
the whole routed component, without the page shell.

The active endpoint renderer records real `HtmxFragment` component states while
Blazor constructs the tree. It reads their final names and resolves all selected
component IDs before writing any selected HTML. The writer delegates each
boundary to the existing monitored renderer seam. It does not parse a completed
response or invoke application render fragments to discover topology. Direct
responses wait for complete rendering. Excluded branches may perform lifecycle,
rendering, and data work; selection makes no skipped-work claim.

The legacy conditional renderer retains `Match`, `RenderDuringStandardRequest`,
and implicit ID matching pending the separately owned legacy removal work. The
active v1 endpoint renderer ignores those legacy filters and constructs all
fragment content before choosing response output.

## Verification scope

The focused component-state tests prove whole/default selection, normal-request
output, single selection, reversed caller order, copied read-only introspection,
and separate request state. The independently packed static-SSR HTTP consumer
proves exact normal and direct bodies, optional wrapper forms, distinct-case
names, header independence, and quiescent child rendering with scoped injection.

Meaningful red was recorded on base
`1abdf719afdaedc79ad604d57a45cc5a89aa2114` with the public API and tests present
but the writer still serializing the root:

| Boundary | Executed | Passed | Failed | Failure |
| --- | ---: | ---: | ---: | --- |
| Focused renderer | 6 | 4 | 2 | Single and ordered selections received root and sibling HTML |
| Packed HTTP consumer | 11 | 2 | 9 | Selected responses received root HTML; legacy target matching omitted an ID wrapper |

The packed green fixture also corrected its default request to omit an empty
query value and added distinct-case selection, bringing that boundary to 12
cases. Focused red is retained in `artifacts/issue168/red/issue168-red.trx`;
packed red and green output are retained under `artifacts/issue168/package/`.

Nested selection, complete invalid-name validation and diagnostics, overlapping
sets, concurrent requests, cancellation, browser delivery, caching, and
skipped-work optimization remain separately owned. These tests make no browser,
non-Linux, other-framework, or release-candidate package claim. Full-scope
mutation is not part of this ordinary issue check.
