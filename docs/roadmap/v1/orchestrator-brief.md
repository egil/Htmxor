# Htmxor v1 orchestrator brief

Use this brief as input to the agent responsible for moving Htmxor toward v1.

## Objective

Deliver [the agreed Htmxor v1 goal](./goal.md) one verified slice at a time.
Keep the goal stable. Plan only far enough to choose and complete the next
slice.

## Sources of truth

Read these in order:

1. [The v1 goal](./goal.md) defines the product and engineering target.
2. [The progress record](./progress.md) states the last reviewed position. Check
   it against the repository before relying on it.
3. The current repository and executable tests define what exists now.
4. The research documents under `docs/research` explain known gaps and possible
   approaches. Treat unproved approaches as hypotheses.
5. The proposed milestone, parent issue, and issue drafts in this directory are
   planning notes. They do not override the goal or current evidence.
6. Once work moves to GitHub, live issue state and merged commits define delivery
   progress. Recheck them instead of relying on an old summary.

## Operating rule

Keep one implementation slice in progress unless two slices are independent and
the user approves parallel work. A slice must make one useful part of the v1
goal observably true. Infrastructure belongs in the first slice that needs it.

Do not decompose the whole v1 goal into implementation-ready issues in advance.
Later work depends on what the current slice proves about Blazor and ASP.NET
Core. Keep a short list of candidate next slices, then select one after reviewing
the latest evidence.

## Loop

For each slice:

1. Inspect the branch, commit, working tree, tests, open work, and relevant
   framework contracts.
2. State the protected behavior as: `When <scenario>, Htmxor <observable
   outcome>.`
3. Explain why this is the most important unfinished slice and which v1 risk it
   removes.
4. Choose the narrowest test boundary that includes the risk. Framework routing,
   rendering, forms, security, browser behavior, packaging, and performance need
   real boundaries rather than mocked substitutes.
5. Record meaningful failing evidence before changing behavior. A compilation
   error or missing dependency is not behavioral evidence.
6. Implement the smallest coherent change that satisfies the protected
   behavior. Do not pull later v1 features into the slice.
7. Run the focused tests, then the broader applicable checks. Record the exact
   commit and commands.
8. Review the result against the v1 goal. Mark only proved behavior complete.
9. Update the progress record and recommend one next slice.

## Progress record

Keep a concise record with these fields:

- Current commit and supported framework/runtime under test.
- Proven v1 behavior, with executable evidence.
- Current slice and owner.
- Known defects or failed experiments that affect the next decision.
- Deferred v1 behavior and why it is deferred.
- The recommended next slice, its protected behavior, and the evidence needed.
- A human decision needed before work can continue, if any.

Do not report research, code written, or tests added as completed behavior. State
what a consumer can now do and cite the evidence that proves it.

## Decision rules

Prefer the next slice that does the most to reduce one of these risks:

- Htmxor cannot run on the intended Blazor version through supported APIs.
- Adding Htmxor changes stock Blazor behavior.
- A request can bypass route, method, authorization, or antiforgery intent.
- The component-owned route and lifecycle model cannot express a required use
  case without endpoint boilerplate.
- Fragment handling performs or transfers work it claims to avoid.
- The application cannot choose and upgrade its own HTMX runtime.
- Behavior differs from application-supplied htmx 4.0.0 running with htmx 4
  defaults.
- Package, browser, cache, or performance behavior differs from project-reference
  tests.

Choose a thin end-to-end behavior over a broad internal refactor. Prefer a slice
that leaves reusable verification behind.

## Guardrails

- Do not introduce application-authored controllers or Minimal API endpoints for
  component routes.
- Do not replace component instance callbacks with static endpoint handlers.
- Do not copy private Blazor renderer code or use new private reflection.
- Do not treat HTMX headers as authorization evidence.
- Do not bind Htmxor to one embedded HTMX version.
- Keep the v1 package, samples, documentation, and executable evidence on .NET
  10 only. Do not retain .NET 8 compatibility or claim .NET 11 until a separate
  target and compatibility matrix are executed.
- Use application-supplied htmx 4.0.0 with htmx 4 defaults for v1 browser,
  example, and release evidence. Do not claim compatibility with another htmx
  version unless that version was executed.
- Do not claim support for a .NET version, browser path, package, or performance
  budget that was not executed.
- Do not publish issues, pull requests, packages, or releases without the user's
  approval.
- Stop for a human decision when evidence requires changing the v1 goal, public
  developer model, supported target framework, or security posture.

## Cycle output

At the end of each cycle, report:

1. What consumer behavior became true.
2. The commit and executable evidence.
3. What remains unproved.
4. The one recommended next slice and why it comes next.
5. Any decision that needs the user.
