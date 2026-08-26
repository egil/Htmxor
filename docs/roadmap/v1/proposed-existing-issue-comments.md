# Proposed comments for existing open issues

Status: review draft only. Do not post these comments, change milestones, or close
issues until the stable-v1 parent and owning child issues have real GitHub URLs.

Replace placeholders such as `{parent}` and `{issue-11}` immediately before
publication. Preserve any separately assigned owner and ask before changing an
issue's labels, assignees, or milestone.

## #11: htmx v2

> The stable-v1 research confirms this remains release-blocking, but the desired
> outcome is broader than replacing the embedded script. Htmxor should stop owning
> the browser runtime, publish a tested HTMX 2 reference profile, and provide a
> conformance runner and extension seam for application-supplied versions.
>
> Proposed owner: `{issue-11}` under `{parent}`. Keep this issue open until that
> compatibility row passes; do not add a hard server-package dependency on a
> specific HTMX version.

## #15: browser logs

> Proposed stable-v1 disposition: defer browser-log transport. V1 will provide
> normal server-side HTTP tracing/correlation and browser integration hooks, but
> it will not define event volume, serialization, privacy, or delivery semantics
> for automatic browser-log forwarding.
>
> Keep this issue open outside the v1 milestone unless we agree on a bounded
> contract. Parent context: `{parent}`.

## #16: streaming

> Proposed stable-v1 disposition: defer streaming fragment updates. Blazor
> streaming SSR and HTMX fragment swapping do not currently form one supported
> protocol, and the stock endpoint path must first prove its buffering and
> response-completion behavior.
>
> Keep this issue open outside the v1 milestone. `{issue-01}` will record any
> newly supported bounded case; otherwise v1 documents quiescent fragment
> responses. Parent context: `{parent}`.

## #18: response headers

> This remains part of v1, but it should be implemented as a version-neutral,
> typed request/response feature registry with protected-header rules. It should
> not require Htmxor to release a new server package for every new HTMX header.
>
> Proposed owner: `{issue-11}` under `{parent}`. Close this issue only when the
> public extension contract and HTTP-boundary coverage exist.

## #30: event callback association

> The research reproduced a more serious form of this problem: an action token is
> not reliably bound to its HTTP method and route, so a PATCH request can select a
> DELETE callback. Stable v1 should remove render-time callback replay and compile
> statically discoverable component actions into method-bound endpoint metadata.
>
> Baseline owner: `{issue-02}`. Proposed fix owner: `{issue-10}` under `{parent}`.
> Keep this issue open until the negative HTTP-boundary regression proves that an
> action cannot be replayed under another route or verb.

## #40: empty action URLs

> The desired convention remains that an explicitly empty HTMX action URL means
> the current route. Stable v1 should preserve that behavior, document it for
> ordinary and nested routes, and cover it in the progressive-form/browser
> compatibility slice.
>
> Proposed owners: `{issue-09}` and `{issue-10}` under `{parent}`. Close this issue
> only after the documentation and route-level regressions exist.

## #48: community standards

> This remains a stable-release gate. The release-candidate slice should add the
> security policy, supported-version policy, contribution guidance, and a clear
> route for reporting vulnerabilities before v1 is tagged.
>
> Proposed owner: `{issue-15}` under `{parent}`. Preserve this issue until those
> files are present in the exact release candidate.

## #50: output caching

> This remains a v1 and ForTheLeague adoption gate. Full and fragment
> representations must vary correctly, anonymous cacheable GETs must not receive
> an unnecessary per-user token cookie, and .NET 11 `CacheView` integration must
> not cache request-dependent fragment selection. `CacheView` does not replace
> HTTP `Vary` or output-cache policy. Because live cache holes reject
> `RenderFragment`/`ChildContent` parameters, an outer cache should fail loudly
> around `HtmxFragment`; a `CacheView` may instead cache stable content inside the
> selected fragment.
>
> Proposed owner: `{issue-12}` under `{parent}`. Keep this issue open until cache
> correctness and request-cost evidence pass at the HTTP boundary.

## #56: CSRF token placement

> Stable v1 should replace the readable response-wide cookie design with a
> documented threat model and supported ASP.NET Core antiforgery/CSRF seams. Every
> generated unsafe method must opt into validation and fail closed before
> component code; the high-security profile must include synchronized-token
> coverage even if .NET 11 automatic cross-origin protection is also used.
>
> Spike owner: `{issue-02}`. Proposed implementation owner: `{issue-10}` under
> `{parent}`. Keep this issue open through security review.

## #57: standard Blazor coexistence

> This is now a top-level v1 contract: registering Htmxor must not alter an
> ordinary Blazor page or form. Htmxor should reuse stock Blazor services and add
> generated representations only for components that opt in by convention or
> explicit metadata.
>
> Architecture owner: `{issue-01}`; implementation owners: `{issue-06}` and
> `{issue-13}` under `{parent}`. Keep this issue open until
> the zero-break integration suite passes.

## #58: htmx extensions

> HTMX extensions remain application-owned. Stable v1 should define a replaceable
> browser adapter, typed server protocol hooks, pass-through behavior for unknown
> `hx-*` markup, and a conformance runner. An analyzer may validate against a
> selected known HTMX profile, but it must allow an application to teach it about
> newer or custom extension values.
>
> Proposed owner: `{issue-11}` under `{parent}`. Keep this issue open until a
> custom extension can be demonstrated without rebuilding Htmxor.Server.

## #64: redirects

> Redirects belong to the stock component lifecycle and the version-neutral HTMX
> response layer, not a globally replaced `NavigationManager`. The v1 matrix must
> cover local and external redirects, nested paths, status codes, history, normal
> no-JavaScript form submission, and HTMX requests.
>
> Architecture owner: `{issue-01}`; proposed behavior owners: `{issue-09}` and
> `{issue-12}` under `{parent}`. Keep this issue open until those browser and HTTP
> cases pass.

## #67: duplicate initialization

> Duplicate component initialization and data loading must be reproduced before
> v1 performance claims are accepted. The baseline should retain the AWS Lambda
> scenario's causal details where practical and compare it with the selected
> stock Blazor execution path.
>
> Reproduction and execution owner: `{issue-01}`; any remaining performance work
> belongs to `{issue-14}`. Keep this issue open
> until the duplicate work is either fixed or proven external to Htmxor with a
> minimal authentic fixture.

## #69: `hx-vals`

> Stable v1 should preserve arbitrary valid `hx-*` attributes and document the
> existing `hx-vals` component case. A helper is justified only if it materially
> improves safe JSON encoding; it must not become a closed enum or a server
> dependency on one HTMX release.
>
> Proposed owner: `{issue-11}` under `{parent}`. Close this issue after the
> pass-through regression and documentation exist, or split a narrowly scoped
> safe-encoding helper if the evidence warrants it.

## #72: static files in Production

> The previous publish-output explanation is not sufficient release evidence.
> Stable v1 requires a clean Production publish and package-consumer test that
> loads the application-owned HTMX asset and all framework static assets from the
> exact release-candidate package.
>
> Baseline owner: `{issue-01}`; release owner: `{issue-15}` under `{parent}`. Keep
> this issue open until the packaged Production smoke passes.

## #75: static asset fingerprinting

> This remains a release blocker and evidence against copying the framework
> renderer. The selected execution seam must preserve Blazor's resource
> collection and fingerprinted asset behavior, and the clean package consumer
> must verify it after publish.
>
> Architecture owner: `{issue-01}`; release owner: `{issue-15}` under `{parent}`.
> Keep this issue open until both the supported framework path and packaged smoke
> are green.
