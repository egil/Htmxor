# Repository delivery contract

Status: repository bindings with explicit readiness decisions still required.
This document does not yet supply a complete launch contract.

Load this document before starting or recovering delivery with
`$orchestrate-milestone-delivery`. The skill owns generic coordination, role
boundaries, supervision modes, and recovery. This document binds that process
to Htmxor; it does not start a run or grant permission to mutate GitHub.

## Established bindings

- Repository: `github.com/egil/Htmxor`; authorized GitHub identity: `egil`;
  default branch: `main`. Apply the identity checks, authorization limits,
  Conventional Commits, exact feature-branch lease, and no-merge-commit rules
  in [AGENTS.md](../../AGENTS.md). Never rewrite the default branch.
- V1 scope: [milestone 1](https://github.com/egil/Htmxor/milestone/1),
  [parent #77](https://github.com/egil/Htmxor/issues/77), and the current run's
  explicit mandate. Native sub-issue and blocking relationships own the graph.
  Issue assignment alone is not a unique execution claim. Reconcile native
  linked branches, pull requests, and Codex task ownership before launching.
- Product requirements: [the v1 goal](../roadmap/v1/goal.md) and the approved
  issue contract. Historical proposals and the progress archive are evidence
  sources, not current scheduling or implementation instructions.
- Verification: [testing and verification](./testing.md) owns commands,
  meaningful red or justified green characterization, Release/static gates,
  test counts, coverage characterization, mutation cadence, and limitations.
  [Code-review standards](./code-review.md) owns independent Standards and Spec
  review. Record the exact snapshot each result covers; an earlier green head
  does not prove a later change.
- Workflow definitions: [CI](../../.github/workflows/ci.yml) owns executable
  PR checks, including fast/full verification and package validation.
  [Upstream monitoring](../../.github/workflows/upstream-monitor.yml) owns its
  separate cadence. Read the current definitions and remote required-check
  configuration when binding a run. A scheduled mutation run is not an ordinary
  PR completion gate; reports and passing checks remain separate facts.

## Durable handoff

Store new delivery state on the owning GitHub issue or pull request. Retain the
originating instruction or recoverable reference, scope, authorized endpoint,
restrictions, and supervision mode with the mandate. Link exact-head review and
verification evidence from that checkpoint. Historical entries in
`docs/roadmap/v1/progress.md` stay archived rather than receiving new run state.

The task assignment must identify the concrete checkpoint and its approved
format before launch. Recovery must distinguish mandate, owner/task/worktree,
linked branch and PR, phase, exact OIDs, pending gates, and next deadline or
wake-up condition. Preserve issue-worktree review receipts through the skill's
recovery boundary; a pushed branch does not preserve ignored local artifacts.

Issue Agent Briefs must identify the protected observable behavior, approved
scope and exclusions, acceptance criteria, dependencies, and verification
contract before code-bearing work starts. Carry the chosen real boundary,
meaningful-red or justified alternate evidence, exact commands/counts, and
unexercised dependencies through the handoff. A planning draft is not readiness.

## Decisions required before delivery launch

The existing repository rules do not establish all values required by the skill.
Resolve each item in an approved task assignment or update this contract with an
explicit decision; record `not applicable` only when that choice is confirmed.

| Binding | Required decision or verified configuration |
| --- | --- |
| Linked branch | Branch naming rule, branch-point rule, and native issue-linked-branch creation procedure; select the permitted non-merge-commit PR strategy |
| Codex ownership | Repository project identity, deterministic issue/task naming, managed worktree conventions, and upward status-signal delivery route |
| GitHub projection | Whether a Project is required; if so, its identity and exact fields/options; milestone and label rules beyond the V1 binding above |
| Checkpoint | Concrete mandate and phase-checkpoint location and schema; ignored review-artifact location and lifecycle |
| CI completion | Required workflow/check contexts, proof for the current PR head, bounded wait budgets, and queued/stuck/cancelled/unavailable handling |
| Automated PR review | Configured provider and trigger, current-head coverage proof, wait budget, unavailable/non-response handling, and comment/thread-resolution protocol |

Use the skill's `blocked` or `human-action` path with the missing binding and
durable issue context while these are unresolved. Read-only discovery may
continue; planning or mutating delivery work must wait for a complete contract.
Never interpret missing automation, a timeout, or absent checks as a pass.

These readiness decisions do not prevent a separately authorized, bounded
documentation edit. They do prevent presenting this file as a fully configured
milestone-delivery workflow. Package publication, releases, deployments, and
other excluded actions still require their own current authority.
