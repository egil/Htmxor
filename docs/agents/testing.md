# Testing and verification

Htmxor owns one executable quality entry point:

```powershell
dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast
dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full
dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile mutation
dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- fix
```

The protected infrastructure behavior is:

`When a contributor or AI agent invokes the documented repository-owned quality command, Htmxor runs deterministic code-style, build, test, or mutation verification with pinned tooling, detects zero-test and invalid mutation runs, reports exact counts and HEAD provenance, and does not depend on a global Stryker install or hosted dashboard secret.`

## Choosing evidence

Before changing behavior, state `When <observable scenario>, Htmxor <observable outcome>.` Name the failure risk and use the narrowest execution boundary that retains it. Browser behavior needs a real browser boundary. Framework routing, rendering, forms, authorization, antiforgery, packaging, and performance claims need the corresponding real boundary rather than a mock that omits the risk.

Observe a behavioral test failing for the expected reason before trusting its production change. Compilation failures, setup failures, missing infrastructure, and zero discovered tests do not count. For a behavior-preserving change, start from a verified green baseline and use characterization or other documented alternate evidence when direct red would fabricate a behavior change.

Always record the exact commit and command. Include discovered, executed, passed, failed, skipped, and other relevant counts. Name any browser, operating system, host, package boundary, or external dependency that was not exercised.

## Profiles

`fast` restores the repository and pinned tools, verifies analyzer and style errors, builds Release, and runs the non-browser suite. It parses the TRX files and fails when no tests were discovered or executed. Every direct `dotnet test` boundary retains `--blame-hang --blame-hang-timeout 5min` so a stalled test produces evidence and cannot consume the job indefinitely.

Release is the authoritative build configuration and treats every warning as an error. `Directory.Build.props` scopes this rule to Release deliberately: unrestricted or design-time legacy warnings are not an invitation to turn verification work into broad cleanup. The analyzer and style error checks above are separate executable gates in every verification profile.

`full` runs the complete suite and parses its TRX files. Its Htmxor test boundary also retains XPlat Code Coverage characterization under `artifacts/results` and requires at least one nonempty fresh `coverage.cobertura.xml`. VSTest may retain a second copy in the blame-hang diagnostic deployment tree; when multiple copies exist, every byte must match or the evidence is ambiguous and the command fails. The summary records one deterministic canonical relative path and the fresh copy count while retaining every copy in the artifacts. This is coverage characterization, not coverage acceptance; the foundation does not invent a score floor. The caller or CI must first provision browsers through the generated Playwright installer. A run with a cached browser does not prove fresh Linux provisioning.

`mutation` runs the pinned local Stryker tool with the single repository configuration. Like `full`, it requires the caller or CI to build the browser test project and provision Chromium through the generated Playwright installer first; the runner does not silently install browsers or operating-system dependencies. It retains local JSON, HTML, and Markdown reports and prints generated, eligible, killed, survived, skipped, timeout, and error counts with HEAD provenance. A generated report proves execution, not acceptance. Missing or empty reports, zero generated or eligible mutants, zero killed mutants, timeouts, and tool or mutation errors make the quality result red. The scheduled or manually requested CI job must still upload the reports after such a failure and remain red. The initial Htmxor result may characterize known survivor and timeout debt, but it must not describe that debt as green. No mutation-score floor is invented before the baseline is understood.

Ordinary pull-request CI runs `fast` and `full`, not full-scope mutation. Full-scope mutation is authoritative on its scheduled or explicitly requested manual cadence. It covers every production file selected by `stryker-config.json`; the term does not mean Stryker's `Complete` mutation-level preset, and the pinned 4.16 configuration uses its default mutation level. A red full-scope result records legacy debt and must not prompt a weaker rule. There is no `mutation-changed` command yet. If added later, it must label base, HEAD, changed paths, affected production files, and all mutation counts. It is partial feedback and never replaces the full-scope mutation profile.

## Formatting boundary

The legacy repository has 409 pre-existing whitespace, character-set, and final-newline findings under an unrestricted `dotnet format --verify-no-changes` run. Bulk whitespace normalization is outside this foundation change. The executable gate therefore uses these narrower checks, both of which pass on the untouched baseline:

```powershell
dotnet format analyzers Htmxor.sln --verify-no-changes --no-restore --severity error --verbosity minimal
dotnet format style Htmxor.sln --verify-no-changes --no-restore --severity error --verbosity minimal
```

`fix` applies analyzer and style error fixes only. It must not silently reformat the repository's legacy whitespace.

## Code-metrics baseline

Htmxor had no executable metrics gate before this foundation. Each project now declares an explicit profile. Legacy profiles use the audited current limit as a baseline ratchet: a validated change may tighten a limit, but ordinary development must never raise it. Samples use a measured profile, not an exemption.

The initial CA1502 measurements come from the unchanged prerequisite commit `457dd3d11d920771c46407f2531800fe813884e4`:

| Profile | Projects | Complexity ceiling | Measured project maxima | Methods above the 10/5 direction |
| --- | --- | ---: | --- | ---: |
| `legacy-production-baseline` | `Htmxor` | 22 | `Htmxor`: 22 | 9 above 10 |
| `legacy-test-app-baseline` | `Htmxor.TestApp` | 3 | `Htmxor.TestApp`: 3 | 0 above 10 |
| `legacy-tests-baseline` | `Htmxor.Tests` | 10 | `Htmxor.Tests`: 10 | 1 above 5 |
| `legacy-samples-baseline` | `BlazingPizza`, `MinimalHtmxorApp`, `HtmxorExamples` | 7 | 7, 2, 2 | 0 above 10 |
| `production` | `Htmxor.Quality` and every newly added production/tooling project path | 10 | New code must meet the limit | 0 allowed above 10 |
| `tests` | `Htmxor.Quality.Tests` and every newly added focused-test project path | 5 | New tests must meet the limit | 0 allowed above 5 |

The nine production methods above the long-term ceiling of 10 at adoption were `HtmxRouteAttribute.Equals` at 16, `HtmxTriggerSpecification.ToString` at 15, `EndpointMetadata.IsValidFor` at 14, `HtmxorComponentEndpointInvoker.RenderComponentCore` at 18, `HtmxorComponentEndpointInvoker.ValidateRequestAsync` at 15, `HtmxorComponentRequestHost.GetRouteParameters` at 16, `HtmxorRenderer.RenderCore` at 11, `HtmxorRenderer.RenderElement` at 12, and `HtmxorRenderer.RenderAttributes` at 22. The existing test method `AlbaScenarioExtensions.WithHxHeaders` is 10, above the focused-test direction of 5. This is recorded legacy debt, not work assigned to the quality-foundation change.

The second bounded #151 slice removes `HtmxTriggerSpecification.ToString`.
The list above remains the historical adoption measurement; current source has
eight of those production methods, and the fixed project ceiling is unchanged.

All six legacy projects pass CA1505 with method and type minimums of 20. A deliberately malformed probe produced CA1509, which confirms invalid profile entries fail instead of disappearing silently.

Every newly added v1 project path declares `production` or `tests` from its first commit. The legacy profiles are path-specific containment ratchets for the six audited owner paths, including during an in-place rewrite. They are not design authority, a modernization mandate, or reusable by a newly added path. The current central assignment map keeps all six owners on those ratchets. A future assignment-map change may lock an owner to its correct generic role profile only when that profile is at least as strict as the original audited baseline; changing only the project file cannot opt in or roll back. This permits a deliberate `Htmxor` move from 22 to `production` at 10 and an existing-tests move from 10 to `tests` at 5. The test application can be rewritten in place while retaining its ceiling of 3, and samples can do the same at 7; their weaker generic assignments remain rejected. Retirement requires removing the project file and solution entry while marking its historical assignment retired in the same reviewed change. A live assignment for an absent project fails, and a retired path reintroduced later is treated as new and must use its generic role profile.

Before every profile or `fix` action, the repository validator requires an exact project/solution map, one allowed profile per project, and an exact current assignment for every audited owner. New projects under `test/` must use `tests`; every other new project must use `production`. It checks every current assignment against the separate original baseline map and locks all six profile files to their adopted CA1502 ceiling and CA1505 method/type floors, so any tightening or assignment change must be deliberate and a raised ceiling or project-only rollback cannot pass unnoticed. It also validates the exact local Stryker pin and the closed full-scope mutation configuration before any process is dispatched.

The 10 and 5 limits are also the direction for touched legacy code, but this work does not require unrelated refactoring of existing production methods, tests, the test application, or samples. Automated limits are coarse. Reviewers must still reject needless complexity or mixed abstraction below a numeric ceiling. Exact project-level measurements are recorded in [the code-metrics baseline](../engineering/code-metrics.md).

CA1502, CA1505, and CA1509 remain executable errors. Do not weaken a threshold because a gate fails. Record current violations as debt and improve them only in an authorized behavior or design change with suitable evidence.

## Handoff

Report:

- the protected behavior and evidence boundary;
- meaningful-red or alternate baseline evidence;
- exact HEAD, commands, and counts;
- unexercised dependencies and residual risk;
- whether full-scope mutation was applicable and, if run, whether it produced a valid report and a passing quality result.

A retained report and a passing command are separate facts. Say which one the evidence proves.
