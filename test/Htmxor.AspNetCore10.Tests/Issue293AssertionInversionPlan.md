# Issue #293 assertion sensitivity plan

This is a bounded plan for green-finalization, not executed inversion evidence.
It covers the tests introduced since immutable behavior start
`6268ef5677ef7b9ee9a538c3b211c8956bcad0be`, including the accepted findings
`LR-22cac485-S001`, `LR-22cac485-P001`, and `LR-22cac485-P002`.
The test-contract remediation phase changes no production code. Execute this
plan only after the Implementor hands back an implemented, green snapshot and
authorizes the temporary faults needed for green-finalization.

When a developer uses HtmxResponse, Htmxor offers nonthrowing destination checks,
consistent parameter names, integer status parity, and cause-specific errors,
while preserving the #154 wire contract.

## Commands and execution discipline

Run the following from the issue worktree. D10 and D11 mean the two concrete
commands below, with FILTER replaced by the fully qualified test selector named
in each mapping. Always execute both target frameworks for a mapped fault.
Use a separate results directory and TRX filename per fault, variant, target,
and restored run. Keep the exact command, SHA, changed diff, selected row,
assertion/failure text and discovered/executed/pass/fail/skip counts in the
future green-finalization receipt.

```bash
dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj -c Release -f net10.0 --filter 'FullyQualifiedName~HtmxResponseAuthoringTests.FILTER' --blame-hang --blame-hang-timeout 5min --logger 'trx;LogFileName=inversion-net10.trx' --results-directory artifacts/issue-293-inversions/FAULT
dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj -c Release -f net11.0 --filter 'FullyQualifiedName~HtmxResponseAuthoringTests.FILTER' --blame-hang --blame-hang-timeout 5min --logger 'trx;LogFileName=inversion-net11.trx' --results-directory artifacts/issue-293-inversions/FAULT
```

P means this packed command. It selects both net10.0 and net11.0 isolated
consumers, including their real Kestrel boundary, SDK pins and package assets.
S means the surface command, also selecting both packed framework assemblies.

```bash
dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj -c Release --filter FullyQualifiedName~PackedResponseAuthoringConsumerTests.Packed_integer_status_preserves_hosted_body_contract --blame-hang --blame-hang-timeout 5min --logger 'trx;LogFileName=hosted-inversion.trx' --results-directory artifacts/issue-293-inversions/FAULT
dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj -c Release --filter FullyQualifiedName~PackedPackageConsumerTests.Package_only_public_surface_matches_retained_allow_list --blame-hang --blame-hang-timeout 5min --logger 'trx;LogFileName=surface-inversion.trx' --results-directory artifacts/issue-293-inversions/FAULT
```

Freeze the handed-off implementation SHA. Obtain a green baseline for D10/D11
(all HtmxResponseAuthoringTests), P (28 inner cases each), and S. Introduce one
compiling semantic fault at a time on the real subject side; never invert the
expected assertion or alter both the throwing oracle and Try implementation.
For snapshot field probes, a temporary test-only wrapper may apply the fault
to the actual response after the real call and before assertion, leaving the
expected response untouched. This validates the observation seam explicitly;
record it as a probe rather than a production mutation.

After EVERY fault/variant, restore only that saved diff (no broad reset), verify
no fault remains with git diff against the frozen snapshot, and rerun the same
focused command green. Repack after every packed fault and restoration; stale
packages are invalid evidence. Require nonzero discovery and the named failing
observation, not compilation, setup, API absence, an earlier assertion, timeout
or an incidental HTTP error. If a fault masks a later assertion, isolate the
later observation with a separate compiling fault. Record every mapped variant;
family coverage is not established by one earlier failure. If an assertion
cannot detect its mapped fault, return it for correction rather than claim it
proved. No full mutation campaign or unrelated suite is part of this plan.

## Direct navigation and marker mappings

Selectors below abbreviate only the method suffix after HtmxResponseAuthoringTests.

- **N1 — Member and delegate contract.** Selector
  `Try_navigation_matches_throwing_wire_contract_and_preserves_state_on_rejection`.
  Temporarily omit each of the four string Try members separately. D10/D11 must
  fail RequireOperation's NotNull for that operation, after compilation. For
  signature/delegate shape use a compiling wrong Boolean return signature;
  delegate binding or packed surface must reject it. Restore each member and
  repeat green. These variants prove the intentionally reflective entry seam;
  later variants keep members present.
- **N2 — Acceptance Boolean.** Same selector. Return false after an accepted
  `/orders/42` operation, then return true without mutation for a rejected null,
  whitespace or wrong-origin local destination. D10/D11 must fail acceptance
  equality for all four operations (Redirect uses its own rejected mailto input).
  The throwing counterpart remains the #154 oracle. Restore each independent
  return-value fault. In a separate exception-classification probe, change only
  the throwing oracle's rejected-input exception to InvalidOperationException
  while the Try path still returns false: the rejection IsAssignableFrom
  assertion must fail after Boolean equality passes. Restore the oracle before
  any other variant; this is a controlled reversal, not a new URL policy.
- **N3 — Accepted wire output and body decisions.** Same selector. On the Try
  path only: omit its header, write a different navigation header, append instead
  of replacing, and normalize `../orders/%7E42?next=/a/../b` or same-origin
  original text; run these as separate variants for each operation. Header-array
  equality must fail. Change the resulting status or fragment selection and
  expect the corresponding AssertEquivalent equality to fail. Separately flip
  navigation suppression for Location/Redirect or make PushUrl/ReplaceUrl retain
  prior navigation suppression; test visible/navigation seeds. Independently
  discard explicit-empty and API-204 suppression on successful navigation;
  the empty/status seeds must fail suppression parity. D10/D11, same selector,
  with one variant per affected observation, followed by restoration.
- **N4 — Rejection has no state mutation.** Same selector. Keep return false and
  the rejection policy intact; on the actual side alone alter, one at a time:
  status, any existing header/value/order, application Items, Body stream
  identity, HasStarted feature flag, suppression decision, SelectedFragmentNames.
  D10/D11 must reach and fail the matching AssertUnchanged field check for a
  rejected destination. Probe suppression separately for visible, explicit
  empty, navigation and status seeds. Body/HasStarted probes may replace the
  actual test response feature or body just after the real call; they must not
  modify the oracle. Preserve all earlier fields so each later check is reached.
- **N5 — Hidden trigger state.** Same selector. On rejection clear only the
  actual response's retained trigger accumulation, leaving existing headers
  unchanged until `Trigger("after-rejection")`. D10/D11 must pass initial
  snapshot checks then fail the post-rejection header equality. Restore trigger
  state. This proves the follow-up comparison rather than only header snapshots.
- **N6 — Strict marker.** Selector
  `Try_navigation_retains_strict_marker_guard_without_mutation`. Bypass each
  Try operation's marker check for valid `/orders/42`: D10/D11 must fail
  Throws<InvalidOperationException> for absent/invalid/multiple markers.
  Separately retain that exception but alter each snapshot field before throwing;
  D10/D11 must pass Throws and fail the corresponding unchanged-state assertion.
  Use the N4 per-field probes where a direct mutation is impractical; restore
  each variant. Do not invent different URL/marker precedence policy.

## Integer status and hosted mappings

- **I1 — Entry, any-request behavior and fluent return.** Selector
  `Integer_status_matches_enum_on_any_request`. Omit only StatusCode(int) to
  fail member presence. With it restored, add the strict htmx marker guard:
  ordinary/false/TRUE rows must fail at the call. Separately return another
  HtmxResponse over the same context (test probe if construction is internal)
  while retaining status mutation: Same must fail. Restore each fault.
- **I2 — Status and enum parity.** Same selector. Make int status a no-op for
  422 then 286, and separately replace the requested status with 200 for each
  remaining listed code. D10/D11 must fail explicit status equality, while enum
  calls remain unchanged. Independently change int-path headers, suppression or
  fragments without changing status; matching AssertEquivalent fields must fail.
  In particular omit int-selected 204 and 304 suppression separately.
- **I3 — Redirect rejection.** Selector
  `Integer_redirect_status_rejects_with_navigation_guidance_and_no_mutation`.
  Permit each of 301/302/303/307/308 in the int overload: Throws must fail.
  Separately exempt each status in the enum oracle while int still rejects:
  the second Throws must fail, proving the parity anchor is checked.
  Separately throw a different exception type, change ParamName, omit Location(
  guidance, and omit Redirect( guidance; D10/D11 must fail the corresponding
  assertion after earlier assertions pass. Finally keep rejection and guidance
  correct but mutate each state field and independent suppression seed using
  N4's probes: unchanged-state assertions must fail. Restore every variant.
- **H1 — Integer call-path sentinel.** Command P. First omit the integer member
  while keeping the reflective fixture compiling: each hosted row must fail
  RequireStatusCode's NotNull before HTTP. Restore it. Temporarily make ONLY
  StatusCode(int) a no-op while the enum overload stays correct. The compiled
  staged page must still contain calls to Issue293StatusCodeTests.Apply for
  both Status and FinalStatus. The 422/286 and 204/304 HTTP rows must fail status
  or body assertions, proving the hosted page actually uses int, not enum.
  If P remains green, integer-path evidence is invalid and staging must be fixed.
  Restore the int implementation and require P green after repacking.
- **H2 — API suppression and restoration.** P. Separately omit suppression for
  int 204 and 304: direct API rows must fail empty-body/no-exception observations
  (or status if the fault changes status, which is NOT sufficient here).
  Separately retain suppression after a later allowed API status: transition
  rows ending 422/286/200 must fail the visible component-output assertion.
  Keep final status and X-Status-Probe correct to reach body checks. Restore.
- **H3 — Exact selected status/raw overrides.** P. Make API-204 suppression apply
  after raw 304, then the reverse. Mixed rows with visible seed must fail the
  required stock InvalidOperationException log observation, even if both bodies
  remain empty. Conversely force writes for matching API-selected 204/304;
  empty-body/no-exception checks must reject logged stock write errors. Restore
  each direction separately. Do not redefine raw HTTP/stock behavior.
- **H4 — Independent decisions.** P. Clear explicit-empty suppression on later
  API 422/286, then clear navigation suppression; corresponding seed rows must
  fail empty-body checks while status stays correct. Independently let raw
  204/304 overrides bypass each independent suppression decision: mixed rows
  must fail no-exception/no-error checks. Restore each reason/direction.
- **H5 — Hosted output, logs and headers.** P. For a visible output row keep
  correct status but replace the component sentinel; Contains must fail. Omit
  X-Status-Probe or change its value; header observation must fail. Inject a
  logged exception with no error-level entry into an otherwise correct hosted
  request, then an error-level log with no exception: the respective Empty
  assertion must fail after correct status/body/header. For a raw-override row,
  alter exception type or the selected status in its text with body/status
  unchanged: Contains(predicate) must fail. Restore and repack each variant.

## Message, metadata and complete packed-surface mappings

- **M1 — Required guidance and argument identity.** Selector
  `Extension_header_name_errors_identify_the_cause_and_alternative`. Replace the
  name exception with a non-ArgumentException, then with wrong ParamName; each
  must fail its respective assertion. Separately omit or swap required guidance
  for every listed core operation, HXOR reservation, and non-HX header access.
  D10/D11 must fail the positive semantic check for that cause, not full prose.
- **M2 — Distinct causes and corresponding core operation.** Same selector.
  Keep required guidance present, then add each unrelated guidance token in a
  separate variant: another typed operation, reserved, or
  HttpContext.Response.Headers. D10/D11 must fail DoesNotContain, after positive
  guidance and exception assertions pass. For reserved/non-HX errors no typed
  operation is appropriate; each must reject such advice and the other cause.
  A core error must reject all eight other response operations as alternatives.
  Restore each token variant. No full-message equality is prescribed.
- **M3 — Generic all-guidance regression.** Same selector. Replace all rejected
  name messages with ONE identical message containing Location(, PushUrl(,
  ReplaceUrl(, Redirect(, Refresh(, Reswap(, Retarget(, Reselect(, Trigger(,
  reserved and HttpContext.Response.Headers, retaining ArgumentException and
  ParamName=name. All 12 D10/D11 rows must fail negative semantic assertions
  despite positive checks passing. This is the explicit P001 inversion.
- **M4 — Failure preserves state.** Same selector. Keep correct cause-specific
  messages and exceptions but mutate each captured field before throwing,
  separately using N4's actual-side probes. D10/D11 must reach and fail the
  corresponding unchanged-state assertion; restore each variant.
- **A1 — Parameter metadata at both boundaries.** D10/D11 selector
  `Authoring_parameter_names_describe_the_complete_value` and command S.
  Independently rename Location(string)'s url back to path, Location(Uri)'s url
  back to path, and Reswap's value back to modifier. Direct expected-name equality
  and packed metadata equality must fail for that individual variant. Separately
  remove an overload to fail NotNull and packed equality; add an extra parameter
  in a compiling variant to fail shape/surface. Restore and repack each variant.
- **A2 — Metadata reader sensitivity.** Command S. Keep production names correct
  but temporarily make the test formatter omit authoring parameter names, then
  return a wrong name for each signature separately. Packed equality must fail
  with baseline unchanged. Restore formatter and repack; this is a deliberate
  test-tool probe, not a production behavior claim.
- **A3 — Full surface allow-list and exclusions.** S. Separately add a public Uri
  overload to each Try operation, add a public validator type, add one unrelated
  public member, alter a Try return type or parameter type, and omit each allowed
  added member. Each compiling variant must fail full packed equality on both
  frameworks. Do not modify the expected allow-list to accept the fault. Restore
  every member/type variant and require repacked equality green.
- **P1 — Package boundary and execution evidence.** P. Following a genuine green
  packed consumer run, independently alter the actual-side evidence just before
  its check: change package contents to omit the expected library/metadata;
  change the consumer project/assets from package to project-reference evidence;
  then change a compile or runtime lib/net11.0 asset entry to lib/net10.0 (and the
  reverse for the other target). Package, consumer-package-boundary and
  AssertPackageTarget checks must each reject the corresponding evidence probe.
  Keep the process successful and other evidence valid so earlier checks do not
  mask the selected assertion. Inspect global.json and runtime metadata against
  the pinned SDK/runtime as separate execution-fidelity evidence; do not claim
  SDK setup failures as behavioral inversions. Independently supply a wrong
  parsed TRX count with exit 0, then a nonzero exit result with correct counts;
  the wrapper's exact 28/28 and success checks must reject the respective probe.
  Restore all fixture/evidence probes. Retention/copy side effects are evidence
  plumbing, not new product assertions; verify retained TRX and logs match the
  failing/restored process and never count wrapper failures as extra leaf failures.

## Completion receipt

Record the green implementation SHA and all temporary diffs/variants, focused
commands, nonzero discovery, intended failing assertion, and restored-green run
for every mapping above. Missing-member red from the contract phase cannot stand
in for these downstream observations. Preserve earlier reviewer receipts and
leave no injected fault in production, test fixtures, formatter or expected
allow-list. Independent Standards and Spec reviewers must assess the final
implemented snapshot; this plan and the remediation receipt grant no approval.
