# Issue 375 browser preservation

Behavior start: `5988ed64ef29b944090cb91fc05a9b5f919a671f`.

When application-owned htmx 4 invokes component actions, direct partial titles or
named deliveries, Htmxor preserves browser transport, security rejection,
request-instance identity, DOM updates, lifecycle ordering and cache isolation
on every originally covered framework. Production-published package assets and
component actions remain exercised through a package-only consumer.

## Boundaries and inputs

`Issue211BrowserTests` retains 13 net10 and 21 net11 rows. POST, PUT, PATCH and
DELETE exercise explicit tokens on both frameworks, including missing and
invalid tokens. Net11 additionally exercises all four methods without tokens
and four auxiliary fetch CORS policies. The auxiliary fetch rows are distinct
from htmx's default same-origin prevention. The application still explicitly
validates DELETE tokens with the stock validator.

`Issue171BrowserTests` retains one row per framework: two overlapping requests
with independently released descendant gates. `Issue174BrowserTests` retains
alpha and beta delivery rows on each framework. `Issue283TitleBrowserTests`
retains its original net10-only title row. Each browser case owns its Kestrel
host, probes and Chromium process/context. Components are compiled Razor, and
requests, headers, callbacks, responses, DOM and events are real.

`Issue50CacheTests` retains one HTTP-only row per framework, with both priming
orders and all five representations. It has no Browser trait and stays in
fast. `HostIsolationTests` adds one non-browser fixture regression row per
framework: constructing the CORS host must not mutate another host's shared
framework method metadata. It reproduces the defect before the per-endpoint
copy, following the existing #246/Issue210 host pattern. Browser tests carry `Category=Browser`; the quality plan excludes that
category from both destination frameworks only in fast.

The htmx runtime is linked byte-for-byte from the existing application fixture.
Its provenance and license remain in `test/Htmxor.Quality.Tests/Htmx4PackageBrowser/`.
The in-process hosts load the build's static-web-assets manifest, including the
actual Htmxor adapter. This is not Production-publish evidence; that claim stays
in `Htmx4PackageBrowserTests`.

## Assertion ledger and sensitivity

[Preservation.json](Preservation.json) maps every original assertion site to its
original file/line/method, input matrix, framework, destination and classification.
Repeated helper invocations retain their original loops and conditional branches.
A compound equality retains every array element and exact byte/string value.

The local evidence root is `artifacts/reviews/issue-375/`. The `migration/green`
run established all 43 replacement rows before removing any replay. Under
`migration/sensitivity`, `sites.json` identifies each assertion variant;
`<ID>-<TFM>/command.json`, `output.log` and `result.trx` retain exact invocations
and raw failures. Temporary variants reverse equality, containment, emptiness,
identity and boolean assertions; Playwright assertions use `Not`. Value-returning
`Single` checks reverse cardinality to `Empty`. Host-address checks are fixture
controls, not behavioral red evidence. These are assertion reversals, not a
full-scope mutation result. Temporary instrumentation is not committed. Each run selects one assertion site;
a repeated helper fails on its first applicable invocation. The restored green
run executes every original loop and input. This does not claim a separately
executed mutant for every dynamic invocation of the same assertion.

The `migration/behavior-inversions` evidence supplements assertion reversals:
B01 corrupts the production adapter token, B02 clears production fragment
selection, B03 perturbs the test component title, B04 omits test-host output
caching, and H01 reverses the Production fixture assertion. B01/B02 are genuine
production inversions; B03/B04 are explicitly test output/wiring inversions.
All source changes are immediately restored after each experiment.

The instrumented build disables analyzers solely because it adds temporary
conditional branches at each assertion. The original test source is restored
immediately after that build. The normal restored Release build and both final
profiles enforce the unchanged analyzer and complexity gates.

## Real package controls

The published carrier uses `PackedPackageFixture` in `PackedConsumerCollection`.
It copies the immutable fixture package into its consumer package source and
retains real restore, Release publish, Production hosting and browser execution.
It cannot dispose or modify the fixture-owned original package.

Only the four duplicate named/lifecycle/cache rows leave that carrier's filter.
Its unrelated historical rows remain, including the separate output-cache
activation test. `RetainedPackageInventory.txt` enumerates the expected 39 rows.
Package file, dependency, analyzer, asset, package-reference and no-project-reference
assertions remain at real package boundaries. Both framework compile/runtime
asset-selection assertions remain in `PackedSuccessfulConsumerTests` through
`NamedSelectionEvidence.AssertPackageTarget`. The JSON ledger records the exact
reconciliation; project-reference tests do not stand in for these checks.

## Verification limits

The exact candidate receipt, commands, counts, local timings and profile discovery
are in the ignored `test-contract/<HEAD>/verification.md` and
`complete-change/<HEAD>/verification.md` receipts. Prior untouched baseline results
are reused; they do not prove a changed candidate. Cached Chromium execution does
not prove fresh installation, other operating systems or hosted CI. Full-scope
Stryker is outside this assignment. Independent Standards and Spec review belongs
to the parent after the final Tester handoff.
