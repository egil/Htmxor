# Issue 339 assertion-sensitivity plan

This is a plan for the retained Tester in green-finalization, after the
Implementor supplies the exact implementation HEAD and ownership handoff.
No inversion is executed or claimed by this red-contract correction. The plan
covers the assertions introduced in HtmxorFormDiagnosticTests; the existing
preservation suites retain their separately established contracts.

When a stock form has independent Htmxor-owned faults, Htmxor reports every
decidable cause once, exposes each cause with a mapped source location locally,
and keeps external-fallback causes in complete results despite unrelated errors.
Owner option1 in issue339 comment6336676592 remains authoritative.

## Execution and restoration protocol

1. Establish the supplied clean implementation HEAD, then run the corrected
   focused command in README.md and require all intended cases green, including
   explicit-action class and compiler-boundary discovery. A baseline failure
   returns to the Implementor; it cannot establish sensitivity.
2. Apply one temporary inversion at a time. Keep real Razor fixtures, real Roslyn
   references, generator execution and compiler prerequisite checks intact except
   when the prerequisite assertion itself is the named target. Run only the
   named method/case using Release, `--blame-hang --blame-hang-timeout 5min` and a
   unique TRX/log path. Require nonzero discovery and the intended assertion
   failure; compilation/setup failure or an earlier assertion is not evidence.
3. For a method with several assertions, vary only the targeted assertion's
   expected operand or test-boundary observation. Require its own failure line;
   do not let the exact-set assertion hide later metadata/locality/output checks.
   Test-boundary corruption is temporary sensitivity instrumentation, never a
   replacement for real compiler dependencies or a retained production seam.
4. Restore the exact original test file after each inversion and rerun the same
   case green. Record the implementation HEAD, temporary diff, command, discovery,
   failure line/result and restored-green result. Verify restoration against the
   saved clean file rather than resetting other agents' changes. Never commit or
   push a deliberate fault. Production reversals are owned by the Implementor
   and require an explicit serialized handoff; the Tester does not edit source.
5. After all inversions are removed, run the corrected focused command again at
   the final unchanged implementation HEAD. Reconcile all case counts, then run
   the required final clean aggregate gates under their separate handoff. Keep
   the sensitivity result distinct from full-profile/coverage evidence.

## Named targets and deliberate inversions

The budget is at most 50 isolated inversion runs, each followed by its targeted
restored-green run, plus the two focused portfolio runs. Do not expand this into
a combinatorial matrix or repeat unchanged successful evidence.

Each item below is a separate run where separate checks are specified. Theory
parameters refer to the explicit data in HtmxorFormDiagnosticTests.

- **Independent reasons and unrelated-error suppression:** use
  `Independent_form_causes_keep_raw_multiplicity_locations_and_approved_locality`
  with EditUnnamed/full/no-unrelated-error, NamedConflict/full/no-unrelated-error,
  and PlainUnnamed/full/unrelated-error. In the test-boundary complete result,
  temporarily omit just form033 (identified by its full form reason), then omit
  just form032, then omit all form diagnostics for the unrelated-error case.
  Each run must fail the raw multiset assertion for the named missing cause.
  Keep callback033 intact so it cannot substitute for form033. In addition,
  request the Implementor's controlled first-cause and blanket-error-guard
  reversals: these named cases must fail, and pass after source restoration.

- **ID, full reason and each mapped-span dimension:** use EditUnnamed/full/
  no-unrelated-error. Temporarily corrupt one returned diagnostic at the test
  boundary: change its ID, then one character in its reason, then mapped path,
  then start line, start column, end line and end column independently. For all eight
  checks, corrupt the serialized observation in AssertDiagnostics after the real
  diagnostic has passed its metadata checks; do not invent a source mapping.
  Each of these eight runs must fail the exact raw multiset assertion, followed
  by its restored-green case. This checks every serialized comparison field.

- **Severity and NotConfigurable:** use EditUnnamed/full/no-unrelated-error.
  In separate runs invert the expected severity to Warning and invert the tag
  assertion to require NotConfigurable absent. Each must fail its own metadata
  assertion before serialization; restore and rerun green separately.

- **Raw multiplicity and ordinal spans:** use Repeated/full/no-unrelated-error.
  Temporarily append a copy of one actual diagnostic to the observed array,
  then in a separate run drop only the second form's033, then change its mapped
  line to the first form's line in the serialized observation. Each must fail
  the raw multiset comparison. Do not introduce Distinct in any sensitivity run;
  duplication must remain visible. Restore and rerun after each corruption.

- **Mapped local feedback and descriptor tags:** use EditUnnamed/local/
  no-unrelated-error. First replace its local result with an empty array; require
  the exact-set failure. Separately invert Assert.NotNull(SourceTree) to Null,
  and invert the CompilationEnd exclusion to inclusion. Each must fail the
  corresponding post-multiset assertion on authentic mapped local diagnostics.
  Restore and rerun the case green after each of the three inversions.

- **External authored fallback and complete-only exception:** use
  PlainUnnamed/full/no-unrelated-error; independently invert the SourceTree-null
  check and the LocationKind.ExternalFile check. Each must fail its own metadata
  assertion. Then use PlainUnnamed/local/no-unrelated-error and replace its
  empty local observation with the already computed complete diagnostics; require
  the empty expected multiset to fail. Restore each run. No generated-C# fallback,
  additional-location workaround or fabricated Razor mapping is introduced.

- **Compiler-owned prerequisites before and after generators:** use
  `Compiler_owned_invalid_facts_do_not_hide_an_independent_valid_form` with
  InvalidName. In separate runs change only the expected CS0103 to CS0246 at
  the pre-generator comparison and at the post-generator comparison. The latter
  run leaves the earlier comparison intact; require the named comparison to fail.
  Use the independent-cause method with EditUnnamed/full/unrelated-error and
  invert expected CS0246 similarly. Restore each run and require green.
  Separately invert the absence-of-CS8785 assertion to require CS8785 on a valid
  fixture; require that driver-diagnostic assertion to fail, then restore green.

- **Invalid facts and independently known POST exclusion (review corrections):**
  use the compiler-control method with InvalidName/full, InvalidName/local,
  MixedInvalid/full and MixedInvalid/local. In each run remove only the first
  form's033 from the observed result and require the exact-set failure. The
  valid neighbor's028/033 must remain unchanged. For InvalidOwner/full and
  InvalidOwner/local, independently invert the expected empty-set comparison
  to require nonempty after the real CS0103 prerequisites; require its own
  no-guessed-owner assertion failure. Restore and rerun each case green.

- **Plain initializer and descendant prerequisites (review corrections):**
  use Invalid_name_or_descendant_preserves_only_independently_decidable_causes
  with ReviewPlainInvalid/full/no-unrelated-error. Corrupt its serialized form033
  observation into032 and require the raw-set failure; callback033 stays intact.
  Use ReviewDescendant/local/unrelated-error and omit only outer033, requiring
  the raw-set failure after CS0103/CS1662/CS0246 checks pass. Separately remove
  CS1662 from only the pre-generator expected compiler multiset, then only the
  post-generator expected multiset, leaving the preceding assertion intact.
  Each run must fail the targeted compiler-prerequisite comparison.
  For Invalid_plain_name_cannot_drive_aggregate_inference/no-unrelated-error,
  invert complete emptiness to require nonempty, then independently invert
  local emptiness. Require the named assertion failure and restore green.
  The authentic stale-scanner dependency and invalid initializer remain intact.

- **Aggregate032 and preserved callback036 across source trees:** use
  `Named_form_and_member_template_post_conflict_retains_its_form_location`
  with crossTree=true. Drop just032 from complete results and require the exact
  set failure. Then separately set the form-tree local observation to its complete
  result and require the final Assert.Empty failure, leaving full assertions
  intact. For crossTree=false, omit just the preserved036 and require the raw
  set failure. Restore each run. Preserve authentic lowered operations, existing
  mappings and the documented controlled partial-tree partition throughout.

- **Genuine aggregate043:** use
  `Missing_generated_form_inference_is_complete_only_at_its_original_mapped_name`.
  Omit043 from its complete observation and require the exact set failure; then
  independently copy the complete observation to the local result and require
  the local-empty assertion to fail. Keep the stale scanner input and actual
  compiled named form unchanged. Restore each run and require green.

- **Valid/stock empty diagnostics and generated output:** use
  `Valid_forms_keep_their_generated_output_without_diagnostics`, Valid then
  Stock. Independently invert complete emptiness and local emptiness to require
  nonempty; each must fail the corresponding assertion. Next, independently
  remove only the expected quoted owner membership from the Generated string
  observed by the test, requiring the owner-string assertion to fail. Finally,
  for Valid alone retain owner membership elsewhere in the string but remove
  its expected NamedFormComponentTypeNames block fragment; the earlier owner
  check must pass and the named-form block check must fail. All changes are
  temporary observation corruption after real generation; restore and rerun.

## Evidence boundary

This portfolio demonstrates that each assertion family rejects its deliberately
incorrect observation and passes after restoration. It does not claim mutation
coverage of every production path. Existing meaningful red plus the controlled
production reversals establish regression sensitivity for the suppression bugs.
No Stryker run is requested. Browser, editor, runtime form/HTTP, package, service,
other-OS and performance/security behavior remain outside this compiler boundary.

## Review-correction execution boundary

The prior 43 inversion/restoration pairs are retained historical evidence for
the unchanged assertion families. After production remediation, execute only
the changed/new targets above (12 isolated runs: four missing033, two invalid
owner emptiness, one plain032 corruption, one descendant033 omission, two
CS1662 prerequisite comparisons, two invalid-inference emptiness), plus the
initial/final corrected focused portfolios. This keeps the new bounded phase
below 50 runs without repeating unchanged evidence. Execution is deferred
until a supplied implementation snapshot is green; no sensitivity pass is
claimed by the current intentional-red contract.

## Explicit-action consumer correction targets (deferred)

After production remediation and a supplied green exact snapshot, execute only
these seven new bounded inversions/restorations for
Explicit_post_conflicts_require_valid_form_names_without_hiding_independent_exclusions:

1. ConsumerInvalidEdit/no-unrelated-error: after real compiler prerequisites,
   invert expected empty complete diagnostics to require nonempty; require that
   no-guessed-cascade assertion failure, then exact source/binary restoration green.
2. ConsumerInvalidPlain/no-unrelated-error: independently invert complete
   emptiness to require nonempty and restore green. Retain authentic separate
   __formName initializer; do not fabricate a diagnostic or mapping.
3. ConsumerValidEdit/unrelated-error: omit only the explicit-action032 observation;
   require its raw multiset failure after actual CS0246 checks, then restore green.
   This protects genuine conflict against blanket suppression/analyzer omission.
4. ConsumerInvalidExcluded/no-unrelated-error: omit only form033 by its full
   reason, retaining action033. Require the exact-set failure and restore green.
5. Same excluded fixture: independently omit only action033, retaining form033;
   require its own exact-set failure and restore green. Raw same-ID multiplicity
   and original distinct primary spans must remain visible.
6. ConsumerInvalidEdit/no-unrelated-error: alter only pre-generator expected
   CS0103 to CS0246; require that precise compiler assertion, then restore green.
7. Same fixture: independently alter only post-generator expected CS0103,
   leaving the pre-generator comparison intact; require the later comparison's
   failure, then restore green.

Use the established isolated test-only observation/expectation inversion
mechanism, actual Razor/Roslyn/generators and exact source/baseline binary
restoration after each, Release/hang flags and unique raw TRX/logs. Initial/final
corrected focused portfolios must include all previous579plus these8cases.
No execution claimed by this intentional-red checkpoint. Unchanged prior43and12
targets are historical and need not be repeated. No production mutation/Stryker
or cold/full gate is part of this bounded red assignment.
