# Htmxor stable v1 tracker proposal

Status: review draft only. Nothing in this directory has been published to GitHub.

The agreed product and engineering target is [the Htmxor v1 goal](./goal.md).
The remaining files are tracker drafts and must stay consistent with that goal.
Use the [v1 orchestrator brief](./orchestrator-brief.md) to track proved progress
and select one implementation slice at a time.

This packet prepares the first tracker changes for a stable Htmxor v1 without
prematurely turning architecture hypotheses into implementation tickets.

## Proposed first publication

After review and explicit approval, publish only:

1. the `Htmxor v1` milestone;
2. the stable-v1 parent issue;
3. issue 01, which proves the stock Blazor execution seam and captures its
   executable compatibility baseline;
4. issue 02, which proves or bounds lifecycle-preserving unsafe-verb dispatch; and
5. the approved migration comments on existing open issues.

Do not publish the later implementation slices until issues 01 and 02 record the
target-framework, execution, and custom-action decisions. Their boundaries depend
on whether Htmxor can delegate through the stock component endpoint invoker, how
much .NET 11 is used, and which gaps require generated code or an upstream
ASP.NET Core change.

## Draft files

| Draft | Purpose |
| --- | --- |
| [V1 goal](./goal.md) | Agreed product and engineering target |
| [Orchestrator brief](./orchestrator-brief.md) | Agent input for progress tracking and next-slice selection |
| [Progress record](./progress.md) | Last reviewed evidence, active work, and next candidate |
| [Milestone](./proposed-milestone.md) | Stable-v1 outcome and release gates |
| [Parent issue](./proposed-parent-issue.md) | Product contract, scope, sequencing, and later issue map |
| [Issue 01](./proposed-issue-01-stock-invoker-spike.md) | Executable stock-invoker and target-framework decision |
| [Issue 02](./proposed-issue-02-unsafe-verbs-spike.md) | Lifecycle-preserving unsafe-verb decision |
| [Existing issue comments](./proposed-existing-issue-comments.md) | Proposed disposition without losing issue history or ownership |
| [Open pull request comments](./proposed-open-pull-request-comments.md) | Proposed disposition for stale PRs #41 and #74 |

## Inputs

- [Stable v1 gap analysis](../../research/stable-v1-gap-analysis.md)
- [Blazor static SSR progressive enhancement](../../research/blazor-static-ssr-progressive-enhancement.md)
- [.NET 11 Blazor and ASP.NET Core opportunities](../../research/dotnet-11-blazor-aspnetcore-opportunities.md)
- [HTMX backend framework comparison](../../research/htmx-backend-framework-comparison.md)
- [Htmxor v1 interface sketch](../../research/htmxor-v1-interface-sketch.md)

## Provisional implementation slices

These are deliberately titles and outcomes, not ready-for-agent issues. Issues 01
and 02 must settle their shared execution and action boundaries first.

| ID | Type | Proposed outcome | Blocked by |
| --- | --- | --- | --- |
| 03 | HITL | Prove .NET 11 static-SSR validation when the first validatable form arrives in an HTMX swap | 01 |
| 04 | HITL | Prove request-safe fragment caching with .NET 11 `CacheView` | 01 |
| 05 | HITL | Freeze the convention-first route, action, fragment, and extension contract | 01-04 |
| 06 | AFK | Preserve stock Blazor behavior when Htmxor is installed | 05 |
| 07 | AFK | Generate normal-only, HTMX-only, and dual component GET routes | 06 |
| 08 | AFK | Return one selected component fragment without rendering excluded branches | 07 |
| 09 | AFK | Progressively enhance a stock `EditForm` POST | 03, 08 |
| 10 | AFK | Generate secure PUT, PATCH, and DELETE component actions | 02, 08 |
| 11 | AFK | Support caller-owned HTMX runtimes and a bounded protocol/analyzer extension seam | 06-08 |
| 12 | AFK | Make full/fragment caching, history, errors, redirects, and authentication flows correct | 04, 09-11 |
| 13 | AFK | Coexist with Interactive Server, WebAssembly, Auto, and enhanced navigation | 07, 09, 11 |
| 14 | HITL | Set the v1 request-cost budget from repeatable measurements | 09-13 |
| 15 | HITL | Prove the exact NuGet release candidate in clean and ForTheLeague consumers | 14 |

## Review questions

1. Is a decision gate before implementation the right publication boundary?
2. Is unsafe-verb lifecycle dispatch the correct second architecture gate, or
   should it be deferred until the stock GET/POST path is accepted?
3. Does the provisional slicing keep each later issue narrow enough to be
   independently assigned?
4. Should ForTheLeague validation block Htmxor v1, or only block adopting it in
   ForTheLeague?
5. Are any existing issues separately owned and therefore inappropriate to fold
   under the parent issue?
