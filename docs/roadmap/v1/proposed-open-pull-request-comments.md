# Proposed comments for open pull requests

Status: review draft only. Both pull requests were still open when checked on
2026-08-26. Do not post, close, rebase, or otherwise modify them without explicit
approval.

## PR #41: `feat: Add HtmxorComponentResult and HtmxorHtmlRenderer`

> The stable-v1 research confirms the problem this draft was exploring, but it
> does not yet support merging a custom component result and renderer as the v1
> execution boundary. The current ASP.NET Core comparison shows that a generic
> `RazorComponentResult` path does not perform all routed-component endpoint work,
> including named form initialization and some request-completion behavior. A
> copied renderer would also preserve the private-framework maintenance burden v1
> is intended to remove.
>
> Proposed next step: keep this PR unchanged while `{issue-01}` compares its
> behavior with a generated endpoint that delegates through Blazor's stock
> component endpoint invoker. Reuse useful tests or design discoveries with
> attribution. After the decision, either narrow the PR to the accepted internal
> seam or close it as superseded with a link to the spike evidence. Do not merge
> or ask the author to rebase before that decision.

## PR #74: `fix UseEmbeddedHtmx being ignored`

> This fixes a real inconsistency in the prototype option, but the stable-v1
> direction removes the premise of the option: applications always own the HTMX
> script and Htmxor publishes compatibility evidence and adapter hooks instead of
> embedding a runtime that can be disabled.
>
> Proposed next step: preserve the PR discussion as evidence for the Production
> static-asset regression in issues #72 and #75. Once `{issue-11}` exists for the
> caller-owned runtime and `{issue-15}` owns the exact-package Production smoke,
> close this PR as superseded rather than merge a switch that the v1 package
> should no longer expose. Do not discard any independent header-property fix; if
> one exists, split it into a narrowly scoped issue or PR after review.
