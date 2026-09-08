# Server fragment selection

Issue [#168](https://github.com/egil/Htmxor/issues/168) implements the valid flat
selection portion of the approved
[#167 contract](https://github.com/egil/Htmxor/issues/167#issuecomment-5531911245).
Issue [#169](https://github.com/egil/Htmxor/issues/169) completes its nested and
invalid-selection rules.

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

Names start with an ASCII letter, continue with ASCII letters, digits, hyphens,
or underscores, and contain at most 64 characters. No trimming or case
normalization occurs. A null declaration is unnamed; an empty or malformed
declaration is invalid. Before writing a direct response, Htmxor validates every
rendered named declaration, including declarations outside the selection and
declarations in whole-component output. Invalid names and duplicate declarations
raise a diagnostic `InvalidOperationException` before response HTML is written.
The application's normal error handling remains responsible for the HTTP error
response.

Every selected name must exist exactly once and may appear only once in the
selection. Selecting a nested child emits that child's optional wrapper and
subtree, excluding its ancestors' wrappers and siblings. Selecting a parent
emits its whole rendered subtree. A selection containing both an ancestor and
its descendant fails before output, in either caller order. The renderer uses
Blazor's completed component-state parent links to detect that overlap.

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

The routine full profile also found four existing packaged browser fixtures
whose `Match`, `RenderDuringStandardRequest`, implicit ID selection, and
skipped-descendant expectations belonged to the legacy renderer. The #122,
#137, #139, and #144 fixtures now select explicit names. Application-authored
Razor conditions retain their normal-page controls and direct-request async
gates. Their assertions retain exact selected response content, browser
completion ordering, and request isolation, while recording completed excluded
descendant work. The existing published-package Chromium suite passed all 39
cases after this fixture-only migration. Its preceding four failures are retained
in `artifacts/issue168/full-before-fixture-migration.log` and the matching TRX.

The #169 focused renderer matrix records meaningful red at test checkpoint
`d5f263f6f60bff5a81298e428333a6fb123f940e`, against unchanged production at
`b2bb028f6d5b5e611fdbf27d83decd4ba96150b3`: 37 executed, 12 baseline passes,
and 25 failures. The packed HTTP consumer executed 18 cases, with 12 baseline
passes and six failures. In both nested-overlap orders, the old writer emitted
the child twice. Malformed declarations, unselected duplicate declarations,
and repeated selections supplied the other missing-behavior evidence. Existing
valid nested output was retained as baseline coverage.

The packed timing probe places a 32 KiB fragment before invalid selections,
exceeding the response writer's 16 KiB buffer. Test middleware observes the real
response-start state and emits a sentinel only when no response has started.
This probe enables TestServer synchronous I/O because the existing writer
flushes large output synchronously; it does not establish default-host
large-output compatibility. Initial synchronous-I/O and 128 KiB pipe-backpressure
failures are recorded separately from meaningful red.

Concurrent requests, cancellation, browser delivery, caching, and skipped-work
optimization retain their separately owned acceptance contracts.
The migrated conformance fixtures do not establish those complete contracts.
No non-Linux, other-framework, other-browser, or release-candidate package claim
is made. Full-scope mutation is not part of this ordinary issue check.
