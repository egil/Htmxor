# Diagnostic help contract sensitivity plan

Issue: [#335](https://github.com/egil/Htmxor/issues/335). This test-owned plan
addresses accepted finding `LR-cbdd7a48-P001`. It specifies future verification;
none of these reversals has been executed in the red-contract phase.

When a developer opens help for any active HTMXOR diagnostic, Htmxor supplies a
stable documentation link to that ID's cause and supported fix.

## Preconditions and restoration protocol

The same Tester receives a new green-finalization assignment for an exact clean
implementation SHA. First build Release with `CI=true` and run both
`HtmxorDiagnosticHelpTests` facts green. Record the SHA, discovery, commands,
counts, and original bytes of each file before temporary edits. Keep those
backups in the ignored issue artifacts, never `/tmp`. Coordinate temporary
production metadata edits with the Implementor under that assignment's explicit
reversal authority; retain production ownership with the Implementor.

Apply exactly one fault at a time. For each case below, run the named fact and
require discovery/execution and its stated behavioral assertion failure. A
compiler, analyzer-build, setup, or zero-test failure is insufficient. Restore
the original bytes immediately, verify the diff contains no remaining fault,
and run both contract facts green (2 executed, 2 passed, 0 failed/skipped).
Record each red/restored-green pair separately, including incidental failures
from assertions sharing that fact. Stop and return an infeasible fault to the
Implementor rather than weakening the contract or suppressing build checks.

Use the focused repository test command with `--configuration Release`,
`--blame-hang --blame-hang-timeout 5min`, `CI=true`, an appropriate
`FullyQualifiedName` filter, and distinct TRX/log paths per fault/restoration.
Metadata or test-helper edits require real recompilation for both the fault
and restoration runs. Markdown-only faults use the already-built green
assembly with `--no-build`: the subject is the real local document read by the
test. This also prevents Roslyn's separate release-tracking build checks from
masking the document assertion for temporarily missing/duplicate release rows.
After all restorations, rebuild Release and rerun both facts without
`--no-build`. Keep deterministic builds and source mapping enabled throughout.
Fast/full and preservation checks remain the final assignment's separate gates.

## Concrete fault-to-assertion cases

The **descriptor fact** below is
`Every_active_descriptor_exposes_its_stable_documentation_link`; the **page
fact** is
`Diagnostic_help_page_has_every_active_anchor_and_is_linked_from_the_documentation_entry`.
Each numbered case uses the restoration protocol above.

1. **Descriptor URI:** clear only the HTMXOR004 manifest-origin descriptor's
   help URI, preserving the other descriptor with that ID. Run the descriptor
   fact after rebuilding. Expect exact `HelpLinkUri` equality to report the
   empty URI against the absolute canonical URL ending `#htmxor004`, despite
   another descriptor of the same ID retaining a valid link. This specifically
   exercises entry-by-entry validation before deduplication.

2. **Missing page:** temporarily rename `docs/diagnostics.md` within the
   checkout. Run the page fact with `--no-build`. Expect its explicit page
   existence assertion, not a file-read exception. Rename the file back.

3. **Missing anchor:** remove only the `HTMXOR003` ID heading from the page,
   retaining its prose and all other headings. Run the page fact with
   `--no-build`. Expect anchor-set equivalence to report missing `htmxor003`.

4. **Entry link:** remove the canonical diagnostics-page link from
   `docs/index.md` while retaining the page and headings. Run the page fact
   with `--no-build`. Expect the documentation-entry link containment assertion
   to fail. Remove every occurrence of that same page link if implementation
   contains more than one; unrelated links remain.

5. **Descriptor-ID omission:** temporarily filter HTMXOR003 out of the route
   analyzer's public `SupportedDiagnostics` result while retaining its
   construction and the other entries. It is the single-owner ID, so this
   removes the ID from the union rather than only removing a redundant entry.
   Rebuild and run the descriptor fact. Expect fixed active-ID inventory
   equivalence to report missing HTMXOR003 (and release-to-descriptor inventory
   may also fail). Restore the public result before rebuilding/restoring green.

6. **Release omission:** remove just the HTMXOR003 row from
   `AnalyzerReleases.Unshipped.md`. Run the descriptor fact with `--no-build`.
   Expect release-ID equivalence to report missing HTMXOR003, while actual
   analyzer discovery and active inventory remain intact.

7. **Release duplicate:** duplicate just the HTMXOR003 release row. Run the
   descriptor fact with `--no-build`. Expect release-row uniqueness to report
   the duplicate; equivalence can also report its changed multiplicity.

8. **Heading duplicate:** duplicate just the `HTMXOR003` ID heading in the
   diagnostics page. Run the page fact with `--no-build`. Expect heading-anchor
   uniqueness to report duplicate `htmxor003`; anchor equivalence can also
   report the changed multiplicity. This remains distinct from missing-anchor
   sensitivity, because Markdown disambiguates repeated heading anchors.

9. **No analyzers discovered:** temporarily replace the test-owned
   `DiscoverAnalyzers` result with an empty array. Rebuild and run the
   descriptor fact. Expect `analyzers.Should().NotBeEmpty()` to fail; the
   independently fixed ID inventory must also report all 41 IDs missing. This
   fault represents a discovery regression without deleting production types
   or fabricating a setup/assembly-load failure.

10. **One analyzer exposes no descriptors:** temporarily return an empty
    `SupportedDiagnostics` array from the route analyzer while leaving the
    other analyzers intact. Rebuild and run the descriptor fact. Expect that
    analyzer's `SupportedDiagnostics.Should().NotBeEmpty()` guard to fail; its
    lost unique IDs also cause fixed-inventory failures. Restore/rebuild before
    the required two-fact green run. Do not treat a compile-time analyzer
    inventory warning as the required behavioral failure.

## Representative grouping and limits

These ten isolated faults cover each assertion family without running one
reversal for all 146 entries or all 41 headings. URI equality is one unconditional
comparison per entry: empty, relative, wrong-page, and wrong-fragment values all
fail that same equality, so the shared-ID empty-URI case represents that family.
All entries and IDs remain checked in every permanent green run. The separate
HTMXOR003 omission represents fixed ID-set equality; release omission and
duplication separately cover set completeness and uniqueness. Anchor omission
and duplication similarly cover both anchor assertions. Entry-link containment
and page existence have their own faults because neither follows from a URI or
anchor failure. Empty analyzer discovery and one empty descriptor inventory
exercise the two different nonempty guards; the per-analyzer guard's identical
loop body needs one representative analyzer, not three repeated injections.

The runtime checkout locator is test infrastructure following the repository's
`AppContext.BaseDirectory` ascent to `Htmxor.sln`. Its portability evidence is
real `CI=true` compilation/execution with deterministic mapped source paths;
it is not an additional product assertion family requiring an invented fault.
Cause/fix prose accuracy and compiler/framework ownership stay with Spec review.
These local tests make no browser/editor or hosted-HTTP availability claim.
