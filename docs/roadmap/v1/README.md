# Htmxor v1 roadmap

Status: GitHub owns delivery scope, dependencies, ownership, and current state.
This directory retains the product goal and historical planning and evidence.

## Active guidance

- [V1 goal](./goal.md): agreed product and engineering requirements, not a claim
  that every requirement is implemented or verified.
- [Htmxor v1 milestone](https://github.com/egil/Htmxor/milestone/1) and
  [parent #77](https://github.com/egil/Htmxor/issues/77): live delivery scope.
  Read each issue's approved contract, comments, native sub-issues and blocking
  relationships, linked pull requests, and exact-head verification evidence.
  Native GitHub relationships govern the dependency graph; flag contradictory
  prose instead of silently scheduling from it.
- `$orchestrate-milestone-delivery`: delivery coordination and supervision mode.
  Before starting a run, load the [repository delivery contract](../../agents/delivery.md)
  and resolve its readiness requirements. A roadmap link does not launch a run
  or grant mutation authority.
- [Testing and verification](../../agents/testing.md) and
  [code-review standards](../../agents/code-review.md): executable evidence and
  independent Standards/Spec gates.

The user's current directions and repository instructions retain their authority
over issue text. When an approved issue decision and the goal disagree, reconcile
that disagreement before implementation. Use the live graph and saved delivery
mandate to select work, rather than a numbered plan or historical "next slice."

## Historical material

These files are archived in place to preserve links and evidence. Their old
publication gates, architecture hypotheses, API proposals, scheduling directions,
and status claims are non-operative. Read them only to investigate historical
context or an exact recorded verification result; use the active sources above
for current decisions. New delivery checkpoints belong on the owning GitHub
issue or pull request, not in this archive.

| File | Retained purpose |
| --- | --- |
| [Progress archive](./progress.md) | Historical commands, exact commits, counts, and limitations |
| [Proposed milestone](./proposed-milestone.md) | Original milestone draft |
| [Proposed parent](./proposed-parent-issue.md) | Original product and sequencing proposal |
| [Proposed issue 01](./proposed-issue-01-stock-invoker-spike.md) | Initial execution-seam hypothesis |
| [Proposed issue 02](./proposed-issue-02-unsafe-verbs-spike.md) | Initial unsafe-verb hypothesis |
| [Proposed issue comments](./proposed-existing-issue-comments.md) | Original tracker migration drafts |
| [Proposed PR comments](./proposed-open-pull-request-comments.md) | Historical PR-disposition drafts |

Research linked from those documents describes its recorded baseline, not
current implementation instructions. The former `orchestrator-brief.md` was
retired; use the delivery skill and repository contract, not an older checkout's
copy. Existing issue text referring to that brief needs reconciliation during
readiness checking, not restoration of the retired file.

## Executable baseline

[The #154 package public-surface allow-list](./issue-154-package-public-surface.txt)
is an active test input used by `PackedPackageConsumerTests`, not an archived
proposal. Preserve it here unless a reviewed change also updates its consumer.
