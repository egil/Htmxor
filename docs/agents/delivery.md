# Repository delivery contract

Status: approved delivery bindings.

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

## Delivery bindings

### Linked branches and pull requests

- Create issue branches with GitHub's native `gh issue develop` flow from a freshly fetched `origin/main`. Name them `egil/issue-<number>-<short-slug>`. Before edits, read back the native issue linkage and require matching local, remote, and linked-branch OIDs.
- Each issue receives one independently mergeable pull request. Preserve a clean Conventional Commit history: use GitHub's rebase strategy by default, with an exact pull-request-head guard. Squash is permitted when it is needed to make the issue history coherent. Never create a local merge commit or rewrite `main`.
- Immediately before merge, fetch `main` and require the reviewed comparison base and current pull-request head to remain current. A changed base requires the delivery skill's rebase, verification, and review recovery path.

### Codex ownership and worktrees

- Use one owner task named `issue_<number>_implementor` per issue. Its isolated worktree is `/home/egil/src/worktrees/Htmxor/milestone-1-issue-<number>` and must remain outside any repository worktree to avoid nested-source discovery.
- The owner verifies its path, branch, upstream, `HEAD`, and remote OID, sends one `provisioned` receipt, and makes no edits until the Supervisor returns an exact-OID `proceed` receipt.
- Owners report only `completed`, `decomposed`, `planning-checkpoint`, `blocked`, or `human-action` to their Supervisor. `provisioned` is the setup handshake, not a progress signal.

### Durable checkpoints and review artifacts

- Record a new `## Delivery checkpoint` comment on the owning GitHub issue at each durable phase boundary. Do not edit history. The comment records the mandate reference, supervision mode, branch, comparison base and candidate OIDs, owner/worktree, phase, exact verification and review receipts, pending gates, and next deadline or wake-up condition.
- Keep exact-snapshot Tester, Standards, and Spec receipts as ignored files at `artifacts/reviews/issue-<number>/...` in the issue worktree until the pull request merges. A receipt is valid only for its recorded candidate OID.

### Verification and pull-request checks

- Require the repository-owned `fast` and `full` profiles plus the issue's focused verification boundary. Record exact HEAD, commands, counts, and unexercised dependencies. Full-scope mutation remains scheduled or manually requested evidence, not an ordinary pull-request gate.
- For the exact current pull-request head, require green applicable CI contexts: `run-test`, package creation and validation, dependency review, Infer#, and CodeQL. `deploy` and scheduled Stryker are not merge gates.
- A required check that is queued, stuck, cancelled, or unavailable for 45 minutes is blocked. Recover by a documented rerun or infrastructure diagnosis; never treat absence, timeout, or a stale-head result as a pass.

### Automated pull-request review

- Trigger Copilot with `@copilot review` after the pull request is ready and its current-head CI is green. A normal qualifying review identifies the current head. Address every finding with a recorded disposition and resolve every addressed thread.
- Wait up to 30 minutes for the review. If GitHub's timeline shows a Copilot session started and finished for the current head but no review result was published, record that exact evidence and treat it as no new findings. Other unavailable or non-response cases are blocked.

### Projection, authority, and guided cadence

- No GitHub Project projection is required unless one is subsequently configured. Milestone 1, existing labels, and native parent/dependency relationships remain authoritative.
- The active milestone mandate authorizes in-scope branches, pushes, pull requests, review replies, rebases, and merges. It excludes deployment, releases, package publication, protection bypasses, unrelated work, and another owner's branch.
- Guided delivery pauses after the user-designated parent's complete child set. For the current #207 parent, its child issues continue without an intermediate user planning pause.
