# Code-review standards

Review the complete merge-base-to-HEAD diff on two independent axes. Standards checks this repository's engineering rules and credible defects. Spec checks the approved issue, specification, pull request contract, or repository goal. Report each axis separately. One cannot cancel a finding on the other.

## Standards

Check correctness, security, compatibility, failure behavior, maintainability, and scope where the diff creates a real risk. Read surrounding code and tests before making a finding.

For behavior changes, verify that the author stated protected behavior, chose the narrowest faithful boundary, recorded meaningful behavioral red or justified alternate evidence, and reported exact commands, counts, HEAD, and unexercised dependencies. Zero discovered tests are a failure, not evidence.

Treat metrics as executable no-regression limits and review aids. The legacy complexity ceilings are 22 for production, 3 for the test application, 10 for existing tests, and 7 for samples. They may tighten but must not loosen after adoption. Every newly added production/tooling project path uses `production` at 10, and every newly added focused-test project path uses `tests` at 5. An audited owner path keeps its centrally assigned ratchet through an in-place rewrite; moving to an equally strict or stricter generic role profile requires a deliberate assignment-map update that also prevents project-only rollback. Retirement must remove the project and solution entry while advancing central state in the same reviewed change; later reintroduction uses the generic role profile. Legacy profiles are path-specific containment ratchets, never design authority or a demand to modernize the stepping-stone code. For touched legacy code, 10 and 5 remain the direction, but do not demand unrelated cleanup solely to reduce a metric. CA1502, CA1505, and CA1509 do not replace judgment about cohesion or abstraction. Raise a finding when a changed method mixes conceptual levels, couples unrelated responsibilities, or adds needless complexity, even if it passes the coarser profile. The full audit is recorded in `docs/agents/testing.md`.

For mutation changes, distinguish execution from acceptance. A retained report does not make timeouts or errors green. Full-scope mutation covers every production file selected by `stryker-config.json` and is authoritative on its scheduled or explicitly requested manual cadence, not an ordinary pull-request check. The term does not refer to Stryker's `Complete` mutation-level preset. Changed-scope mutation is deliberately absent; any future version is partial and cannot replace a required full-scope run. Do not request fixes to existing surviving or timed-out mutants unless the approved scope owns them, and never weaken the rule because a manual run is red.

Do not demand full whitespace normalization. The repository gate intentionally verifies analyzer and style errors only, and `fix` must not rewrite legacy whitespace.

## Spec

Identify the governing requirement in this order: the approved issue or specification, the pull request contract, then a referenced repository document. If none exists, state `No spec available` rather than inventing requirements.

Check every acceptance criterion, edge and failure path, explicit exclusion, and authority limit. Reject unrequested product behavior, framework-support changes, security drift, or claims about dependencies that were not exercised. For v1 work, enforce the forbidden implementation shapes and stop conditions in `docs/roadmap/v1/orchestrator-brief.md`.

## Findings

Report only actionable problems introduced by the change. Give each finding one axis and priority, explain the triggering scenario and impact, cite the rule or requirement, and keep any line range tight. Do not use review comments for praise, optional cleanup, or a summary of passing automation.

End with separate Standards and Spec outcomes, the finding count and worst priority for each, exact verification inspected or run, and relevant environments not exercised.
