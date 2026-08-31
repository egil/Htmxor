# Htmxor v1 progress

Last updated: 2026-08-31

## Repository state

- Baseline commit for the first v1 slice: `66139317b9edae1fff2ff73fa5175381ee3487b1`.
- Verified implementation commit for issue #78: `8dcca3c0749cf53e310b1dff9dc22612b0d5e8f5`.
- Verified implementation commit for issue #81: `0c3fec1b8c3425ef37c2d93a5fa131f3b0c2a649`.
- Verified evidence commit for issue #83: `46f5b5324c64bff111a8e9bbb38ea812c22067ef`.
- Verified implementation commit for issue #85: `0a87dcd8b50cb5fd1be6a4ddae57601986aaea4a`.
- Verified implementation commit for issue #87: `8c2a528dbff8c528d52199c60330c99ded851b83`.
- Verified post-review test head for issue #87: `645065ef809306f744bc7cdb8adf1f799b3c0784`. Production code is unchanged from the implementation commit; the only executable delta is a test identifier correction.
- Issue #87 progress commits are documentation-only. Executable claims are tied to the tested heads above, not to the later documentation heads.
- Verified executable proof commit for issue #89: `d5153938a2142b49a6b9c5168c14fda4944e315e`.
- This issue #89 progress change is documentation-only. Executable claims are tied to the tested commit above, not to the later documentation head.
- Verified implementation commit for issue #91: `47da4a36eb4909f8d120ab032bb12435196a23b9`.
- This issue #91 progress change is documentation-only. Executable claims are tied to the tested implementation commit above, not to the later documentation head.
- Verified implementation and compilation-test commit for issue #93: `0f8d4d761c89afc860ec0cd5058b2b65fd737ee9`.
- Verified post-review fix commit for issue #93: `cf8cbb38bea4374636e072688e8da5927d6296f8`.
- This issue #93 progress change is documentation-only. Executable claims are tied to the tested implementation and post-review fix commits above, not to the later documentation head.
- Verified executable fix commit for issue #95: `58fa7aece281f053b1b7bffeec7ebcb8f7dfb33e`, based on exact `origin/main` commit `55e8d23ea18d4a0c8068be436afc95256a97be09`.
- This issue #95 progress change is documentation-only. Executable claims are tied to the tested commit above, not to the later documentation head.
- Verified implementation commit for issue #97: `a94cf491205ed12863ad8ed0ca623a1a7b686c6b`, based on exact fetched `origin/main` commit `e222f75e72f152718c43c534944717dc1a62c51a`.
- Verified compiler-backed follow-up commit for issue #97: `3dc8350de488ace5d02d4244bdd87ef9953d0469`, based on merged `origin/main` commit `7f88974aa94bb77c8a50cdff7ecd92f4e7993861`.
- Verified post-review constrained-route fix for issue #97: `f02a1c84dde19ed5221396339ce22ac4e936bbc6`.
- This issue #97 progress change is documentation-only. Executable claims are tied to the tested implementation commits above, not to the later documentation head.
- Verified executable proof commit for issue #100: `e03501dab0df0cf7efedc65cfab73419601d7ca8`, based on exact fetched `origin/main` commit `d6e440f0fcb029174571979062705681b7a94d46`.
- Verified post-review compiled-route fix for issue #100: `42082a1bacb71364f5ccf513c8b5e791528d83cf`.
- This issue #100 progress change is documentation-only. Executable claims are tied to the tested commits above, not to the later documentation head.
- Preserved meaningful-red commit for issue #103 after rebase: `371c1125a4442b6df688a686abbe8b49269721a6`.
- Verified implementation commit for issue #103: `fb7e31f3d8378d7b7ab8f521f862429da14dfb50`, based on exact fetched `origin/main` commit `b313e8dc6913ae7cbe424e86192aad7440761ac1`.
- Preserved post-review explicit-allow-list red for issue #103: `b78ff30f3c43acbef1ab99b69e51fad7b539879d`.
- Verified post-review explicit-allow-list fix for issue #103: `732a957c36d080ddef39ca24db744b7d0c803fa4`.
- Preserved issue #103 audit reds: effective antiforgery metadata ordering at `834e1058991c191c846ad6252180d7194397308d`, actionless unsafe-route validation at `9ed1938ca40e13cb2ac0d0066ba7b64c5220eab2`, static handler ownership at `adfb00626985a8c5d960af37b92e7dfbd412f342`, mutable omitted-method defaults at `c52ac7c9388d4b40c52205c556d7b03d9a1b4ba7`, and later supported markup at `bc2715c0d650bc434d6656414503764c274bf0ec`.
- Verified issue #103 audit fix commit: `217dd95642400509759d77c3bd8bc4ca53e178a6`, based on exact fetched `origin/main` commit `b313e8dc6913ae7cbe424e86192aad7440761ac1`.
- Preserved issue #103 audit-review static-delegate red: `cdc476767f947187864ae72d8dd45fc905a9999e`.
- Verified issue #103 audit-review fix: `6299b537ceb8954f191997310d9ddfc8c5dc0bee`.
- Preserved issue #103 second audit-review reds: multiline script raw-text binding at `0250a15f740a9d4c79e3d730784b2c9848287df4` and mismatched packaged partial filename at `75668806318873f0415e58c010eaec6e435039b7`.
- Verified issue #103 second audit-review fixes: `7c2d3365569751d3e63e7c5b19658452e2fced48` and `cfc995d9f14b89441224539d76c0279062ea52a4`.
- Preserved issue #103 third audit-review residual raw-text red: `2301078da8447b8a4c6e8d733962eeef7a18a80f`.
- Verified issue #103 third audit-review parser fix: `ce388a1f10fadb121a48ab6f259f62536a5b693b`.
- Preserved issue #103 fourth audit-review self-closing raw-text red: `67776f803b91a60582246d6c6d022ac8e79db872`.
- Verified issue #103 fourth audit-review parser fix: `f1a7884364ad241a32050e59115ea62cbbf1dae5`; the compiler-backed fail-closed component-markup boundary is recorded at `36d92a73f1151f14850632a1d45108e2a948bcca`.
- Preserved issue #103 final root-audit plaintext red: `a129585dfed8bf01c23b54b4131acc3f95f88fba`.
- Verified issue #103 final root-audit plaintext fix: `561bcc2da118ee09e515c037663eebdaf4cb27f6`.
- Preserved issue #103 imported-static ownership red: `7ef7af1ee1ca686c3417370282792041325d82c9`.
- Verified issue #103 imported-static ownership fix: `5177466f0afb09fd087b21d2bd44c04344c5b72b`.
- Preserved issue #103 inaccessible-base ownership red: `fcb89cebded23186d8e0df0833c244a0b5b4d6fb`.
- Verified issue #103 accessibility fix: `88631ad5cf3da3c7d44f111fefd4355c8bf3fc13`; the accessible inherited-handler control is recorded at `696d4539f68ea33a56aa6210412bec87895a2efa`.
- This issue #103 progress change is documentation-only. Executable claims are tied to the tested implementation, post-review, audit-fix, and audit-review-fix commits above, not to the later documentation head.
- Preserved meaningful-red commit for issue #106: `6285dae3646ff8357bdc413315dc1138c69b4de9`.
- Verified executable proof commit for issue #106: `b51b1644e394b2f8a8c9ca6072a7170fff6e5221`, based on exact fetched `origin/main` commit `a489f30f7a20ec801fe52b5ab4f894382d1d9c90`.
- Preserved post-review synthetic Razor-generator visibility control for issue #106: `b56547a2fd5c8922200632048445826b6f1a70da`.
- Verified post-review Razor-manifest compatibility hardening for issue #106: `8cc1badea33f950b43b51ed3d82f6d50e0373480`.
- Preserved post-review C# route-ownership red for issue #106: `21614bb1366482325296263dff7f2da3834f7951`.
- Verified post-review matching-code-behind fix for issue #106: `9e4b3565e95177154b8fdf9e79f3a9ae1b92d30b`.
- This issue #106 progress change is documentation-only. Executable claims are tied to the tested executable proof and post-review fix commits above, not to the later documentation head.
- Preserved meaningful-red commit for issue #108: `ad519a1cee4829f21f4a7678caf568fdac6fb755`.
- Verified executable proof commit for issue #108: `e1b9106553d1838a08916e76edc1ce1181ebd61b`, based on exact freshly fetched `origin/main` commit `5bcd9b89b5a8b885467e3c9f13da629f9cc1d32d`.
- Verified post-proof client-configuration cleanup for issue #108: `8302006d4bc2c6a0627f99c94376f6e0941f0e19`. This removes the obsolete public client-configuration surface instead of leaving it as a silent no-op after Htmxor stopped emitting client configuration.
- Verified Linux pre-publication evidence head for issue #108: `d2d3885c36a78572e93d72e0f9e038240bc9dc90`. This head has the same executable tree as the client-configuration cleanup plus the recorded issue #108 documentation.
- Preserved issue #108 pre-publication sample-ownership red: `bc60bff7829162c72c5bcf776d2895b5b5cf7298`. All three unsafe samples failed the new ownership guard because they advertised htmx 4 without retaining the legacy antiforgery configuration required by the current adapter.
- Verified issue #108 pre-publication review fix: `e2ac91524ec9ce911cb7f66a0fff7bbcee1ff4c2`. The unsafe samples now explicitly own their temporary legacy runtime and configuration, while the documentation gives an exact acquisition and hash-verification path for a fresh application's htmx 4 asset.
- This issue #108 progress change is documentation-only. Executable claims are tied to the tested Linux pre-publication and review-fix heads above, not to the later documentation head.
- Preserved meaningful-red commit for issue #56: `8f83086d18004f1a0abb6963ca4b41a4868b506f`.
- Verified executable proof commit for issue #56: `87cac54b9dfa958d4b3c98a0cfc897bf803cd301`, based on exact `origin/main` commit `8bfa41b3da340b1d10b1d43b31124ece2ba44d4c`.
- Verified post-proof htmx 4 fixture correction for issue #56: `05b7bbe9035df7b3e31e39d42d596405d5a1203e`. The maintained browser fixture now declares explicit targets and swaps required by htmx 4 defaults.
- Verified post-review action-context proof for issue #56: `8679d748cf999c16e624964e26ae276c92d2873e`. The package browser no longer authors an action identity on generated action requests; a separate in-browser htmx 4 context control verifies opaque identity transport without granting server reachability.
- This issue #56 progress change is documentation-only. Executable claims are tied to the tested executable, fixture-correction, and post-review heads above, not to the later documentation head.
- Preserved meaningful-red commit for issue #111: `e84fc820d6ad9b9928df70badb04eacc36fce1af`, based on exact `origin/main` commit `065fbc9135f9b6e6820e43461c3586657197c5ca`.
- Verified executable proof commit for issue #111: `d2f5f90bbcd1cae78570ddc3dc2253b426878e9f`.
- This issue #111 progress change is documentation-only. Executable claims are tied to the tested proof commit above, not to this later documentation head.
- Preserved meaningful-red commit for issue #50: `57a459de02022319538ae17c85ba445683ee7cfe`, based on exact `origin/main` commit `f17fac74fbcee9738b51a53089e7dc2df628b462`.
- Verified executable and documentation head for issue #50: `35bd2a7b24b85f1d4518c322ce44a9066d645f1e`. The final component-attribute proof is at `7b77a5dc38391cd358932d52310777d93fb546c0`.
- This issue #50 progress change is documentation-only. Executable claims are tied to the tested head above, not to this later documentation head.
- Preserved negative-control commit for issue #18: `e601dd38a67a87b1603ae36ddcb317813073a8f7`, based on exact fetched `origin/main` commit `8693ecb4c9180ba6afbb3bb4f85037cb9dd9f3ff`.
- Verified initial executable proof commit for issue #18: `5434ee0e99f6c74a26a94067b273105f28ca98af`.
- Verified post-review deterministic-overlap proof commit for issue #18: `19c52fbe9256425650148227306bf242a29068d3`.
- This issue #18 progress change is documentation-only. Executable claims are tied to the tested proof commits above, not to this later documentation head.
- Verified executable proof commit for issues #72 and #75: `6869313cb5cb7a7eb7d808f19f6fb2ceddc911f6`, based on exact fetched `origin/main` commit `99e34b85ec22f8566b33afd8126cb8f7a6c6b3be`.
- This progress change for issues #72 and #75 is documentation-only. Executable claims are tied to the tested proof commit above, not to this later documentation head.
- Preserved meaningful public-surface red for issue #116: `aba0b84c765637b950b2f9ee1ef11cae810a2ebe`, based on exact fetched `origin/main` commit `036294a223d634abb64fe2ee1085499b2853c7af`.
- Preserved post-review application-options red for issue #116: `69bc8ef2f8e189052b96e06ecb1b486eb1c8623c`.
- Verified post-review executable proof for issue #116: `13eedd1bd84a4c94e7f8e7daf4df44331124bb90`.
- Preserved Copilot-review invalid-event-name red for issue #116: `616e115a93317b8d381aed7acf61d73d4cb927f0`.
- Verified post-Copilot executable proof for issue #116: `9e400591e7e2447cc0d29360dcf8d0598e614da4`.
- This issue #116 progress change is documentation-only. Executable claims are tied to the tested proof commits above, most recently the post-Copilot head, not to the later documentation head.
- Preserved meaningful routing red for issue #118: `c41e5734695273ab6845ced98a182b2a902648c8`, based on exact fetched `origin/main` commit `7f6936da6c045d2d0aee1b42c4fffca0d6ff560c`.
- Verified post-review executable proof for issue #118: `6affff55302b8aaeddc7fc4fd26f619e96824f52`.
- This issue #118 progress change is documentation-only. Executable claims are tied to the tested proof head above, not to the later documentation head.
- Verified post-review executable characterization for issue #120: `f366f14185765db4b4dd65045cc9075fd24e4e22`, based on exact fetched `origin/main` commit `2ec870b629e6aeb8b2f56f62397d12c343ba2e45`. No production change was required.
- This issue #120 progress change is documentation-only. Executable claims are tied to the tested characterization head above, not to the later documentation head.
- Preserved meaningful package/browser red for issue #122: `e5798770689f1e35ad501f70153a1ba8f1e27c4b`, based on exact freshly fetched `origin/main` commit `e28345567db5d07eea6c29b8ba4ed8825c514333`.
- Verified pre-review executable proof for issue #122: `09caf7421a6dbbc22b73cddb4f23dadacd0d1f60`.
- Verified post-review executable proof for issue #122: `0986832151a42c0bedfb01fd266aad056d9df219`.
- This issue #122 progress change is documentation-only. Executable claims are tied most recently to the post-review proof head above, not to this later documentation head.
- Preserved meaningful package/browser red for issue #64: `67f0963204ade54367b76cbdb8aaf76a3a96bb07`, based on exact freshly fetched `origin/main` commit `e77f88e5475caf861afe3c5ca052c239266263ae`.
- Verified executable proof commit for issue #64: `b8bb2204e6107063281a9d93e4fe23e0c4f76301`.
- Preserved Standards-review default-port red for issue #64: `476cf2e76d8f5e36ea7ec9fee95772cfcf84581f`.
- Verified Standards-review semantic-origin fix for issue #64: `5ce00d0610e4e04576595b54114aeac22a136798`.
- This issue #64 progress change is documentation-only. Executable claims are tied to the tested proof and Standards-review fix commits above, not to this later documentation head.
- Preserved meaningful compiler-boundary red for issue #125: `ddee137ed41269ba0238b1c3b193f65790d6a1f2`, based on exact fetched `origin/main` commit `e56475542eb25cb449f7e0723b5f58664bb96aaa`.
- Verified executable proof commit for issue #125: `a6e4f95f29dc7d3e23b5f62ab2db9f4f52800a36`.
- This issue #125 progress change is documentation-only. Executable claims are tied to the tested proof commit above, not to this later documentation head.
- Preserved meaningful package/runtime red for issue #127: `4d58d03e6e703d564cd49660f96bfe739d52f968`, based on exact `origin/main` commit `cf468f4db77557fd12a4c955b7daeb239f0a25b2`.
- Verified executable proof commit for issue #127: `16ec8f77db0006b317977ea765c624ae3de674a9`.
- This issue #127 progress change is documentation-only. Executable claims are tied to the tested proof commit above, not to the later documentation head.
- Preserved meaningful package/browser red for issue #129: `c4ab4199c3dd33e6bc1c24a95493b1e44deddd51`, based on exact freshly fetched `origin/main` commit `5de3ba5a36a9773dbafc39b9a5f7a67e8b274958`.
- Verified executable proof commit for issue #129: `669db318326a55e7a7e0bff3d8a07ddc27268343`.
- Verified post-review browser-assertion commit for issue #129: `5ea0995ec82e302cd00f7c5e4e6d657cb2edb6af`.
- This issue #129 progress change is documentation-only. Executable claims are tied to the tested executable commits above, not to the later documentation head.
- Preserved meaningful package/browser red for issue #131: `58c111e0a6190191ab6d3178575536ab5da76e8e`, based on exact freshly fetched `origin/main` commit `5707b09cf8b7459ca1a753d2f7fe183017e2e8ca`.
- Verified executable proof commit for issue #131: `a0f60d90faa004038c9320bd04afdd7a392cdb96`.
- This issue #131 progress change is documentation-only. Executable claims are tied to the tested commits above, not to the later documentation head.
- Framework boundary under test: ASP.NET Core 10.0.11 and Blazor static SSR. Issues #95, #97, #100, #103, #106, and #125 use a separate external .NET 10 Razor consumer on TestServer that restores a locally packed `net8.0` Htmxor package instead of referencing an Htmxor project. Issues #108, #56, #111, #50, #18, #72, #75, #116, #118, #120, #122, #64, #127, #129, and #131 use a separate package-only .NET 10 application on real Kestrel; its browser cases use Chromium, while issues #18, #50, and #127 also use `HttpClient` for server-response assertions. Issues #72, #75, #116, #118, #120, #122, #64, #127, #129, and #131 publish that external application before running its tests from the publish output in Production.
- Product target correction authorized on 2026-08-28: v1 documentation,
  examples, browser conformance, and release evidence target an
  application-supplied htmx 4.0.0 script running with htmx 4 defaults. Htmxor
  does not embed or silently select that runtime. Issue #108 is the first narrow
  executed htmx 4 browser slice; the remaining conformance matrix is unproved.
- V1 slices proved on this tree: issue #78, stock `@page` routing with a direct HTMX GET; issue #81, every documented .NET 10 Blazor component-route constraint plus typed optional presence and absence; issue #83, authorization-policy and authenticated-user parity for normal and direct GETs; issue #85, one stock named `EditForm` POST with form binding, antiforgery ordering, request-component callback dispatch, and direct component output; issue #87, one shared runtime path for component-owned PUT, PATCH, and DELETE actions represented by fixed future-generator output; issue #89, composition of that assumed generated action output with an application-authored asynchronous parameter lifecycle override; issue #91, one assumed-generated constrained HTMX-only GET route for a component without `@page`, using stock Blazor invocation and static SSR; issue #93, build-time discovery and emission for that one constrained HTMX-only GET route without checked-in generated output; issue #95, analyzer packaging and one application-level registration that connects the generated route to runtime in an external package-only consumer; issue #97, deterministic aggregation of two supported package-consumer declarations through that single registration call; issue #100, one package-generated stock-page PUT callback bound to the compiled component endpoint while two explicit HTMX-only controls remain GET-only; issue #103, shared POST, PUT, PATCH, and DELETE inference for stock `@page` and omitted-`Methods` HTMX-only routes with explicit-method conflicts rejected before mapping; issue #106, explicit authoritative C# method discovery for matching `.razor.cs` partials and all-C# components, deterministic rejection and registration suppression when a C# declaration omits `Methods`, and no method widening from manual render-tree code; issue #108, removal of Htmxor-owned htmx distribution and one package-only application-owned htmx 4.0.0 stock-page and component-GET browser path; issue #56, stock antiforgery and generated POST, PUT, PATCH, and DELETE callback dispatch through the htmx 4 request context in a package-only browser consumer; issue #111, generated safe QUERY callback dispatch for stock and HTMX-only route owners through the real htmx 4 package/browser boundary; issue #50, standard OutputCache variation for one stock full/direct GET pair in a package-only Kestrel consumer; issue #18, dynamic application response headers through the stock request-owned `HttpContext` on normal and direct GET paths; issues #72 and #75, published Production startup plus stock fingerprinted application-asset and packaged-adapter compatibility; issue #116, one htmx 4 `HX-Trigger` response-event surface with post-swap Chromium dispatch and configured JSON details; issue #118, typed htmx 4 full/partial request context, complete source/target identities, stock/direct representation selection, and forged-header fail-closed controls; issue #120, distinct native POST and htmx 4 PUT form destinations with stock full-page fallback, direct partial swapping, and server-owned route, method, authorization, and antiforgery decisions; issue #122, one pure multi-target htmx 4 partial response composed from server-selected `HtmxFragment` instances; issue #64, stock local `NavigationManager.NavigateTo` redirect parity for ordinary GETs and successful `HX-Redirect` full-page navigation for direct htmx GETs; issue #125, static ID-selector `hx-target` order independence for all five generated action methods under stock and omitted-`Methods` route owners; issue #127, `Int32` route-value delivery for one omitted-Methods generated HTMX-only route on direct GET and its declared PUT action; issue #129, application-selected component error status/body plus native htmx 4 default and source-owned no-swap policies through the published package/browser boundary; issue #131, native htmx 4 DELETE form-value placement without stock antiforgery-token transport through the published package/browser boundary.
- Current implementation slice: issue #131, antiforgery-safe native htmx 4 DELETE form inclusion for one generated stock-page action through the locally packed Production Kestrel/Chromium boundary.

## Proven v1 behavior

Protected behavior:

> When a .NET 10 Blazor static SSR application adds Htmxor and maps one `@page`
> component, Htmxor preserves the normal stock full-page GET and returns the
> endpoint-selected component for a direct HTMX GET without a parallel
> application endpoint.

The hosted test proves that one `.razor` file owns the route, the application
starts, and exactly one component endpoint represents that page. A normal GET
uses the stock Blazor shell. A direct HTMX GET to the same route returns the
page component without that shell. Both responses retain application-supplied
endpoint metadata.

The public integration captures the stock Razor Components request delegate in
a final endpoint convention. Normal requests call it unchanged. For a direct
HTMX GET, Htmxor gives that delegate a request-local copy of the selected route
endpoint, preserving its route pattern, order, display name, and ordered
metadata while replacing only its root component metadata. The internal direct
host renders the `RouteData` already selected by the stock invoker, so it does
not perform a second routing pass.

The new public path does not use private reflection, copy Blazor renderer code,
add a controller or Minimal API handler, declare a duplicate application route,
or replace stock routing, rendering, navigation, or endpoint-invoker services.
The old prototype remains internal to the legacy test application for behavior
that later slices have not replaced.

Protected behavior for issue #81:

> When a direct HTMX GET selects a stock `@page` component whose route uses any
> route constraint supported by Blazor on .NET 10, Htmxor supplies the same typed
> route values, query value, and request-scoped dependency as stock Blazor and
> initializes one component instance without another route or application
> endpoint.

The proved constraint and parameter-type set is:

- `bool` to `System.Boolean`;
- `datetime` to `System.DateTime`;
- `decimal` to `System.Decimal`;
- `double` to `System.Double`;
- `float` to `System.Single`;
- `guid` to `System.Guid`;
- `int` to `System.Int32`;
- `long` to `System.Int64`;
- `nonfile` to `System.String`.

The hosted matrix proves representative valid typed output and rejected-input
parity for every constraint, including rejection of the file-like
`document.txt` by `nonfile`. A constrained optional `int` has matching present
and absent behavior. Every successful normal and direct request retains the
query value, request-scoped service value, and initialization count `1`. Each
component-owned route template has one endpoint. Normal responses retain the
stock application shell and direct responses omit it.

The direct host passes the endpoint-selected `RouteData` through the stock
public `Router`. Its endpoint-supplied route-data path performs Blazor's
constrained-value processing and returns the selected component without another
route match. The existing endpoint routing, query supplier, dependency
injection, lifecycle, and static SSR renderer remain in charge. The supported
path neither copies nor extends the legacy hand-written conversion switch.

Protected behavior for issue #83:

> When a stock `@page` component requires an authorization policy, Htmxor
> enforces the same policy and supplies the same authenticated user on normal
> and direct GETs without treating HTMX request headers as authorization
> evidence.

The hosted proof uses one deterministic authentication scheme and one claim
policy through the real ASP.NET Core 10 authentication and authorization
middleware. Anonymous requests receive `401` on both paths. An authenticated
user without the required claim receives `403` on both paths. The `HX-Request`
header alone does not authorize a request.

An authorized user's name and required claim reach the component unchanged on
both paths. The normal response retains the stock application shell, while the
direct response returns the protected component without that shell. The
application still owns one component route and does not add a controller,
Minimal API handler, or duplicate endpoint. No production change was required;
the existing metadata-preserving direct path already satisfied this slice.

Protected behavior for issue #85:

> When a component-owned stock form is submitted through HTMX, Htmxor lets
> Blazor bind the form, validates antiforgery before application code, invokes
> the request component callback, and returns the component response without a
> parallel endpoint.

The hosted proof uses one component-owned `@page` route, one named stock
`EditForm`, and one `[SupplyParameterFromForm]` input. A normal GET renders the
stock application shell and supplies the form handler, antiforgery token, and
cookie. A valid direct HTMX POST binds `accepted-value`, initializes one request
component with a new request-scoped dependency, invokes its callback once with
that value, and returns the updated component without the stock shell.

A direct POST without the antiforgery token and cookie returns `400` before the
form property setter, component initialization, or callback records any
activity. The application still owns one component route. It adds no controller,
Minimal API handler, duplicate route, static endpoint-style action, custom form
binder, or antiforgery runtime.

The public endpoint convention now applies its request-local root-component
substitution to direct GET and POST requests. It preserves the stock component
endpoint's ordered metadata and invokes its captured request delegate, leaving
ASP.NET Core 10.0.11 responsible for antiforgery validation, form mapping,
component lifecycle, named callback dispatch, and rendering. Other HTTP methods
continue through the stock delegate unchanged.

Protected behavior for issue #87:

> When a Razor component declares PUT, PATCH, and DELETE actions, only the
> matching HTTP method can invoke each callback, and every callback runs on the
> request-owned component instance after authorization and antiforgery succeed.

The hosted proof uses one component-owned `@page` route with distinct `@onput`,
`@onpatch`, and `@ondelete` method groups. A hand-authored `.g.cs` stand-in
represents assumed future-generator output: component type, exact normalized
route, HTTP method, server-owned handler identity, descriptor registration, and
the component-side lifecycle hook. It does not discover or analyze Razor, emit
diagnostics, implement a source generator, or define the final generator API.

A final endpoint convention matches each fixed descriptor to the existing stock
component endpoint by component type and normalized route. It preserves the
stock request delegate and metadata, extends the effective `GET, POST` method
metadata with PUT, PATCH, and DELETE, and attaches the server-owned descriptors.
The stock method set and final-convention ordering are visible in the official
[ASP.NET Core 10.0.11 endpoint factory](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Builder/RazorComponentEndpointFactory.cs).

After routing and the retained authorization policy succeed, the shared action
wrapper calls the public `IAntiforgery.ValidateRequestAsync` before it arms a
request-scoped descriptor. This explicitly covers DELETE, which ASP.NET Core's
[antiforgery guidance](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0#http-method-limitations-and-httpmethodoverridemiddleware-interaction)
requires handlers to validate directly. The wrapper then invokes the unchanged
stock component delegate through Htmxor's request-local direct-render endpoint.

The fixed partial runs on the routed page instance. It awaits the public
[`ComponentBase.SetParametersAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.componentbase.setparametersasync?view=aspnetcore-10.0)
contract, atomically consumes the matching descriptor once, and invokes the
declared method group through `EventCallback` on `this`. This supplies route,
query, authenticated user, request-scoped dependency, and normal initialization
and parameter lifecycle state before the callback, while the stock renderer
writes the callback-updated component response. ASP.NET Core's stock
[`RazorComponentEndpointInvoker`](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs)
still reserves its form-dispatch path for POST; the proved lifecycle hook is the
narrow supported seam for these non-POST actions.

The full-fidelity DELETE case observes route value `42`, query value
`from-query`, authenticated user `issue-87-user`, a new request-scoped service,
one parameter lifecycle pass, one initialization, one callback, and the
callback-mutated direct response. Compact positive cases prove distinct PUT,
PATCH, and DELETE callbacks through the same runtime path. Cross-method cases
carry another action's client-supplied identity but invoke only the callback
selected by the actual HTTP method. An undeclared `PROPFIND` with a DELETE
identity remains `405`. Invalid antiforgery tokens for PUT, PATCH, and DELETE
return `400` with zero parameter, initialization, or callback activity.

The application maps only the stock Razor component endpoint. It adds no
controller, Minimal API handler, duplicate route, static component action,
renderer reflection, runtime render-tree discovery, renderer copy, or global
Blazor service replacement.

Protected behavior for issue #89:

> When a Razor component already overrides `SetParametersAsync`, assumed
> generated action code preserves that application lifecycle method and invokes
> the matching unsafe action exactly once after parameter processing completes.

The hosted proof uses one component-owned `@page` route, one asynchronous
application override, and one DELETE action. The override awaits stock parameter
processing, yields before recording its own completion, and requests the render
that exposes that application state. A hand-authored `.g.cs` stand-in adds
`IComponent` to another partial declaration and explicitly reimplements
`IComponent.SetParametersAsync`. It awaits the component's public virtual
`SetParametersAsync` method, which preserves the application override, before it
atomically consumes and invokes the armed action.

This composition follows public contracts. ASP.NET Core 10.0.11 stores and
invokes rendered components through [`IComponent`](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Components/src/IComponent.cs#L6-L27),
while [`ComponentBase.SetParametersAsync`](https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Components/src/ComponentBase.cs#L210-L250)
remains public and virtual. C# merges interfaces across
[partial declarations](https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/classes#1527-partial-type-declarations)
and permits a derived type to
[reimplement an inherited interface](https://learn.microsoft.com/dotnet/csharp/language-reference/language-specification/interfaces#1967-interface-re-implementation).
The generated explicit implementation changes the `IComponent` dispatch target,
while its ordinary virtual call reaches the application override. The inherited
`ComponentBase` implementation still supplies `IComponent.Attach`.

An ordinary authorized GET renders the stock application shell. It runs the
application override, initialization, and parameter callback once, completes the
override's asynchronous work, and does not invoke the unsafe callback. An
authorized, antiforgery-valid DELETE creates a new request component and observes
the ordered sequence `override-start`, `initialized`, `parameters-set`,
`override-complete`, then `callback`. Route value `42`, query value `from-query`,
authenticated user `issue-89-user`, the request-scoped dependency, and the
application's completed state all reach the callback. The callback runs once and
its state appears in the direct response.

No production runtime change was needed. Exact-once action dispatch comes from
the request-scoped descriptor's atomic `TryConsume`, not from an assumption that
Blazor supplies parameters only once. The stand-in proves neither source-generator
behavior nor a final emitted API.

Protected behavior for issue #91:

> When a component without `@page` declares an HTMX-only GET route, assumed
> generated registration maps that route through the stock Blazor invoker so an
> authorized direct HTMX request receives the request-owned component, while a
> normal GET cannot reach it.

The ASP.NET Core 10.0.11 hosted proof uses one component without `@page`, one
`/reports/{ReportId:int}` GET route under an application route group, one query
value, one claim policy, and one request-scoped dependency. A hand-authored
`.g.cs` stand-in supplies only the component type, normalized constrained route,
GET and authorization metadata, and registration on the exact route group. It
does not prove source-generator behavior or a final generated API.

The shared internal registration maps the normalized route with the public
[`MapGet`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.endpointroutebuilderextensions.mapget?view=aspnetcore-10.0)
overload that accepts an explicit `RequestDelegate`. That delegate resolves the
public `IRazorComponentEndpointInvoker` and calls `Render`, matching the stock
[Razor component endpoint factory's invocation path](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Builder/RazorComponentEndpointFactory.cs#L18-L68).
The endpoint carries component, direct-root, link-suppression, route, and
authorization metadata. Mapping it on the exact route group preserves the
group's prefix and application metadata marker.

A direct-only matcher removes this endpoint from normal requests; it grants no
authority. ASP.NET Core authorization still enforces the endpoint policy. The
stock invoker initializes route state, and the stock static SSR renderer creates
the request-owned component. A narrow direct root renders that route data without
a `Router` route-table lookup, which the component cannot satisfy because it has
no `@page`. This path neither copies renderer code nor replaces Blazor services.

An authorized direct GET returns `200` without the stock shell and renders one
initialized component containing route value `42`, query value `from-query`,
identity `issue-91-user`, the scoped dependency, and the group marker. A normal
GET returns `404`, an anonymous direct GET returns `401`, rejected constrained
input returns `404`, and POST, PUT, PATCH, and DELETE return `405`. Exactly one
component endpoint owns the prefixed route. The host application authors no
matching handler; the checked-in `.g.cs` stand-in supplies the assumed-generated
registration.

Protected behavior for issue #93:

> When a component without `@page` declares one statically discoverable
> HTMX-only GET route, Htmxor emits the proved descriptor and exact-group
> registration so the hosted HTTP behavior passes without checked-in generated
> code.

The [.NET 10 Razor SDK source-generator targets](https://github.com/dotnet/sdk/blob/v10.0.400/src/RazorSdk/Targets/Microsoft.NET.Sdk.Razor.SourceGenerators.targets#L15-L72)
supply `.razor` files as compiler `AdditionalFiles` to a `netstandard2.0`
incremental generator, project-referenced through documented
[`ProjectReference` metadata](https://learn.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-items?view=visualstudio#projectreference).
The generator consumes the raw files through Roslyn's public
[`AdditionalTextsProvider`](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md#additional-file-transformation)
and reads the public `RootNamespace` and `MSBuildProjectDirectory` analyzer
properties. It uses no custom target, private Razor API, generated Razor C#,
`obj` scraping, or renderer reflection.

The supported tracer recognizes exactly the current project-root component:
one literal `/reports/{ReportId:int}` route, explicit GET only, one literal
`issue-91-policy` authorization policy, and no `@page`. It emits the same
`Issue91GeneratedRoute` descriptor and registration shape that issue #91 proved,
so the existing application call registers on the exact route group and retains
its prefix and metadata. The checked-in `Issue91GeneratedRoute.g.cs` stand-in is
removed.

Compilation evidence verifies that the emitted descriptor and registration
compile against the required runtime seam. A representative explicit POST
declaration emits no source and reports one deterministic `HTMXOR001` error at
the declaration. More than one authorization declaration is also unsupported;
the generator emits no source rather than silently dropping an effective policy.
The unchanged hosted issue #91 matrix retains `200` for an authorized direct GET,
`404` for a normal GET and rejected constraint, `401` for an anonymous direct
GET, and `405` for POST, PUT, PATCH, and DELETE. This tracer does not establish a
final public generator API or packaged-consumer contract.

Protected behavior for issue #95:

> When a .NET 10 Blazor static SSR application references only a locally packed
> Htmxor package, uses one application-level Htmxor registration, and declares
> the supported HTMX-only GET route, Htmxor generates and registers that route
> so an authorized direct HTMX request receives the component without
> per-component endpoint code.

The Htmxor package now carries `Htmxor.Generators.dll` and its portable PDB at
the documented NuGet analyzer path `analyzers/dotnet/cs`. A private, build-only
project reference orders the generator build without adding the generator or
Roslyn packages to the package dependency graph. The separate consumer restores
an exact local Htmxor package version and contains no Htmxor project reference,
`InternalsVisibleTo`, direct generated-type reference, per-component endpoint
registration, or application-authored endpoint handler.

The generated source adds one internal overload for the existing
`AddHtmxorComponentEndpoints` registration call when its endpoint argument is
the exact `RouteGroupBuilder`. That overload invokes the existing application
registration and a hidden public infrastructure bridge with the generated
component type, normalized route, and authorization policy. The runtime bridge
validates that route and policy against the component type, copies its effective
public attributes with inheritance, constructs the internal GET descriptor, and
maps it on the supplied group. This keeps the generated-to-runtime connection
out of application source and uses no private reflection, copied renderer code,
controller, or Minimal API endpoint.

The package-only consumer proves an authorized direct HTMX GET returns `200`
with route value `42`, authenticated user, and application group metadata, while
omitting the stock HTML shell. A normal GET returns `404`, and an anonymous
direct HTMX GET returns `401`. The group prefix and metadata remain effective.
One host constraint declared on the component's C# partial type is also
effective: the allowed host reaches the component and a different host returns
`404`.
Package inspection verifies the runtime assembly and analyzer locations, no
generator or Roslyn dependency in the nuspec, and no generator or Roslyn
assembly in the consumer runtime dependency graph or output.

Protected behavior for issue #97:

> When a package-only .NET 10 Blazor static SSR application declares two
> supported HTMX-only GET components and calls Htmxor registration once, Htmxor
> maps both routes so each authorized direct HTMX request reaches its own
> component while normal requests cannot reach either.

The source generator now reads only Razor additional-file paths. It emits one
sorted manifest of project-root component metadata names and one application
registration extension; it contains no route, policy, component `typeof`, or
per-component endpoint code. A packaged diagnostic analyzer receives the final
compilation after Razor generation and validates the real component symbols and
their bound `AttributeData` by exact type identity. Compiler-equivalent array
creation, collection expressions, aliases, component-local constants, combined
and multiline attribute lists, and post-markup directives therefore converge on
their compiler values without Htmxor reading or parsing Razor text.

The analyzer reports deterministic nonconfigurable `HTMXOR001` errors for every
compiler-valid Htmxor declaration outside the supported envelope. The generated
extension passes its application assembly, sorted manifest, and exact caller
route group to a runtime catalog. That catalog scans exact compiled attributes,
validates and constructs the complete descriptor set before mapping any
endpoint, then performs application-level Htmxor registration once and maps both
routes in type-name order. This preserves fail-closed startup behavior even if
the analyzer is bypassed.

The package-only consumer declares exactly two project-root components with the
original `HtmxRouteAttribute`, no `@page`, distinct constrained GET routes,
distinct authorization policies, distinct output, and distinct effective
`Host` metadata from their C# partial types. Its summary route and authorization
policy use component-local constants declared in `@code`; both directives occur
after markup and that code block, and the code also contains `"@*"` as ordinary
C# string content. The route uses a collection expression and the policy uses
the attribute constructor. One `/issue-97-group` route group, one
`MapRazorComponents` call, and one application-level Htmxor registration produce
exactly two component endpoints. Each route returns `200` only for its own
authorized direct HTMX request, renders its own output without the stock shell,
and retains its own compiled route attribute, authorization policy, host, and
group metadata. Normal requests return `404`; the other component's policy
returns `403`; anonymous direct requests return `401`; and rejected route
constraints or hosts return `404`.

Protected behavior for issue #100:

> When a package-only .NET 10 Blazor static SSR application declares a stock
> `@page "/reports/{id:int}"` component with one supported
> `@onput="PutReport"` callback and calls Htmxor registration once, Htmxor
> attaches the generated PUT action to that compiled stock endpoint. An
> authorized, antiforgery-valid request reaches the request-owned component
> callback, while an invalid request cannot bind input or invoke it. The stock
> GET and existing HTMX-only GET routes remain available and GET-only.

The narrow action generator recognizes one project-root stock page whose
supported one-line directive preamble contains one simple `@page` directive
and whose first markup start tag contains one double-quoted `@onput` simple
method-group binding. It uses `@page` only as an eligibility signal and never
copies or normalizes its route text. It does not inspect `hx-put`. The generated
token contains only the component type, PUT method, and server-owned handler
identity. The generated partial component hook first awaits normal
`SetParametersAsync` processing, consumes only that exact request token, and
invokes the method group through `EventCallback` on the request-owned instance.

Registration validates that the one generated PUT token belongs to the
project-root component manifest. The stock endpoint final convention matches
it through public `ComponentTypeMetadata` and creates the internal action
descriptor from the compiled endpoint's final route pattern. Authorization
therefore runs in the normal pipeline, and antiforgery completes before the
scoped action token is armed. Generated HTMX-only routes receive no action
descriptor; the two controls with explicit GET methods stay GET-only and have
no action or antiforgery metadata.

The locally packed consumer retains both issue #97 HTMX-only GET components
and adds one stock report page. Its normal GET returns `200` with the stock
shell. An authorized PUT with a valid token returns `200`, observes route value
`42`, query value `from-query`, the authenticated report user, a fresh
request-scoped dependency, and completed binding and initialization, invokes
the callback once, and renders the resulting state without the stock shell.
Missing or invalid antiforgery evidence returns `400`; the wrong user returns
`403`; and a different method or the `hx-put`-only summary route returns `405`.
All rejected requests record zero binding, initialization, and callback
activity. A forged client handler header cannot select a callback.

A computed `@onput` lambda fails the separate package-only Release build with
nonconfigurable `HTMXOR002` and produces no consumer assembly. Razor and HTML
comments, quoted attribute values including explicit raw-string expressions,
Razor code strings, raw attribute metadata, and script text do not declare an
action. The existing issue #87 stock `@page` PUT, PATCH, and DELETE stand-ins
remain separate and green.

Protected behavior for issue #103:

> When a package-only .NET 10 Blazor static SSR component owns a route through
> `@page` or `HtmxRoute` without `Methods`, Htmxor keeps GET implicit, adds only
> the POST, PUT, PATCH, and DELETE methods expressed by supported component
> bindings, and invokes only the matching request-owned callback after
> authorization and antiforgery succeed. Explicit methods remain authoritative,
> and client declarations never grant a server method.

The shared action generator recognizes simple double-quoted method-group
bindings for `@onpost`, `@onput`, `@onpatch`, and `@ondelete` on HTML elements or
Razor component tags, including a supported tag after a complete single-line
ordinary markup line. It can emit distinct actions for different unsafe methods
on one tag. A supported handler name must resolve only to instance methods;
static methods and delegate-valued fields or properties fail with
nonconfigurable `HTMXOR002`, so callbacks remain owned by the request component
instance. Prior ordinary markup is limited to self-closing syntax on an actual
HTML void element or one matching pair containing supported plain text;
incomplete, nested, non-void self-closing, `plaintext`, and raw-text shapes fail
closed. Stock components use their compiled `@page` endpoint as route owner; an
omitted-`Methods` `HtmxRoute` produces one HTMX-only endpoint with immutable
implicit GET plus only its declared unsafe methods. The runtime validates the
complete action and route set before adding endpoint conventions or mappings.

An explicit `HtmxRoute.Methods` set is authoritative. A supported binding whose
method belongs to that set is generated and mapped; a binding outside that set
produces deterministic nonconfigurable `HTMXOR002`, and runtime validation also
fails before mapping if analyzer diagnostics are bypassed. `HTMXOR002` has one
internal descriptor shared by analyzer and generator. Route declarations
originating from `_Imports.razor` are rejected for both stock and HTMX-only
owners. Client-only `hx-post`, `hx-put`, `hx-patch`, `hx-delete`, htmx 4
`hx-action` plus `hx-method`, and `hx-query` declarations emit no action and do
not alter the GET, POST, PUT, PATCH, and DELETE server allow-list.

Unsafe endpoint metadata is fail-closed by effective ordering: Htmxor appends
required antiforgery metadata when an earlier effective entry disables
validation. An explicit unsafe `HtmxRoute` validates the selected request before
rendering even when no generated action exists. Public default-method arrays are
fresh values and cannot mutate the catalog's internal omitted-route GET
invariant.

The locally packed consumer retains its two existing HTMX-only routes: the
summary route explicitly allows GET plus an actionless DELETE, while the report
route omits `Methods` and infers PATCH from a Razor component-tag binding. It
also adds a stock report-page DELETE. The PATCH handler lives in the matching
`.razor.cs` partial. An authorized, antiforgery-valid summary DELETE renders the
request component without a callback; missing and invalid tokens are rejected
before parameter binding or initialization. Other authorized,
antiforgery-valid requests reach only
their route- and method-selected request component, complete route/query
parameter delivery and initialization, invoke the selected callback once, and
render its state through static SSR. Representative wrong-method, cross-route,
cross-component, unauthorized, and antiforgery-invalid requests cannot select or
reach another callback. The application continues to use the single packaged
registration and authors no controller, Minimal API component endpoint, static
handler, renderer copy, private reflection, or global Blazor service replacement.

Protected behavior for issue #108:

> When a .NET 10 Blazor static SSR application supplies htmx 4.0.0, Htmxor
> emits and packages no htmx runtime or legacy htmx extension, stock full-page
> GET remains available, and a real Chromium interaction can use `hx-get`
> against a component-owned route and swap returned static SSR HTML.

The external .NET 10 application restores the locally packed Htmxor package,
has no Htmxor project reference or internals access, and owns the exact
`htmx.org@4.0.0` asset with SHA-256
`E484D9171A9DB30A39C8F16E3D709D4137F3211C659F8E6125816635033D593F`.
Package inspection finds Htmxor's narrow `htmxor.js` adapter but no htmx
runtime, type declarations, or event-header extension. `HtmxHeadOutlet` emits
only that adapter and no runtime or Htmxor-owned configuration payload.

Real Chromium first navigates normally to the stock `@page` route and observes
the complete Blazor document. It then confirms `window.htmx.version` is exactly
`4.0.0`, all browser requests are loopback, the runtime came from the
application path, and no Htmxor runtime, legacy extension, or compatibility
extension was requested. Activating the accessible `hx-get` control sends
`HX-Request: true` to a second component-owned `@page` route, receives shell-free
static SSR, and visibly swaps that markup into the intended target. At issue
#108's executable head, the retained 1.9.12 browser fixture owned and explicitly
labelled its legacy asset and configuration; issue #56 later migrated that
maintained fixture to htmx 4.0.0.

Protected behavior for issue #56:

> When a package-only .NET 10 Blazor static SSR application uses stock
> antiforgery with htmx 4.0.0, Htmxor carries the Htmxor action identity and
> request-verification token through htmx 4's request context, rejects missing
> or invalid tokens before effects, and dispatches an antiforgery-valid POST, PUT,
> PATCH, or DELETE to exactly the matching component-instance callback before
> swapping shell-free static SSR.

The application owns the exact htmx 4.0.0 asset and uses no Htmxor client
configuration. Htmxor's narrow adapter observes `htmx:config:request`, reads the
source element and request from the htmx 4 context, carries an existing Htmxor action
identity, and copies the nearest stock Blazor antiforgery input to the
`RequestVerificationToken` request header only for unsafe methods. The server
continues to select only compiled generated methods and actions; client
attributes are transport context, not authority.

Real Chromium proves a normal full-page GET, no unrelated antiforgery cookie on
a GET-only page, missing-token POST and invalid-token PUT rejection with no
binding, initialization, or callback effects, and successful POST, PUT, PATCH,
and DELETE requests. Each valid request reaches exactly its selected callback,
returns shell-free static SSR, and visibly swaps the response. The package-only
consumer authors no controller, Minimal API component endpoint, static handler,
renderer copy, private reflection, or global Blazor service replacement.
An in-browser context control separately supplies an opaque Htmxor identity
and confirms the adapter copies it and the nearest stock token from htmx 4's
standardized request context; generated endpoint actions remain selected by the
compiled route and method rather than by client-authored identity.

Protected behavior for issue #111:

> When a package-only .NET 10 Blazor static SSR application supplies htmx
> 4.0.0 and a component with a stock `@page` route or an omitted-`Methods`
> `HtmxRoute` statically declares `@onquery`, Htmxor exposes QUERY only for
> that component, carries htmx 4 request content through the supported Blazor
> request-instance boundary, invokes the matching component callback, and
> swaps its shell-free static SSR response.

The public Razor event declaration and the shared action generator now recognize
one supported `@onquery` method-group binding within the existing fail-closed
markup grammar. Omitted-method stock and HTMX-only route owners retain implicit
GET and add QUERY only from that binding. An explicit `HtmxRoute.Methods` list
accepts QUERY when listed and remains authoritative: excluding the binding
produces nonconfigurable `HTMXOR002`, while runtime validation also rejects a
conflict before mapping. Client `hx-query`, `hx-action`, and `hx-method`
attributes and manual `BuildRenderTree` calls emit no generated action.

QUERY is a supported generated action but is not in Htmxor's unsafe-method
predicate. It therefore receives neither unsafe-method antiforgery metadata nor
Htmxor antiforgery validation merely because it has request content. Existing
POST, PUT, PATCH, and DELETE actions retain their prior metadata and validation.

The separate package-only `net10.0` consumer owns the exact htmx 4.0.0 asset,
restores a locally packed Htmxor package, starts real Kestrel, and runs real
Chromium. Its normal stock-page GET returns the full shell. Real `hx-query`
interactions send form-encoded content to both a stock `@page` component and an
omitted-method HTMX-only component, initialize new request instances, invoke
only the route-and-method-selected callbacks, read the submitted value through
public ASP.NET Core request APIs, return shell-free static SSR, and visibly swap
the result. The request instances are distinct, and each callback observes the
same request identifier as its own initialization. Neither QUERY sends a
request-verification header or requires an antiforgery input. A client-only
`hx-query` to a stock page without `@onquery` returns `405` with no new component
or callback effects.

Protected behavior for issue #50:

> When OutputCache is enabled for one stock component URL, Htmxor keeps cached
> full-page and HTMX-fragment GET representations distinct in either warm-up
> order, and safe GET remains cacheable.

The separate package-only `net10.0` consumer applies the stock
`OutputCacheAttribute` to one `@page` component with
`VaryByHeaderNames = ["HX-Request"]`, registers the standard OutputCache
services and middleware, restores a locally packed Htmxor package, and starts
real Kestrel. No Htmxor production change or custom cache implementation is
required.

An activation control sends two normal GETs and observes one component render,
proving the component attribute actually activates caching. The full-first and
HTMX-first cases each use a fresh application and cache. In both orders, the
normal response retains the stock shell, the direct response omits it, each
representation renders once, and a repeated request reuses the exact cached
body. Both safe representations emit no `Set-Cookie` header. The existing
POST, PUT, PATCH, and DELETE browser cases continue to pass with OutputCache
enabled and retain authorization and antiforgery behavior.

This one-header policy is proved only for absent `HX-Request` versus
`HX-Request: true`. Applications whose output also depends on boosted requests,
targets, history restoration, fragment selection, authentication, or other
request data must include every such input in their cache policy.

Protected behavior for issue #18:

> When a request-owned component dynamically sets one arbitrary non-HTMX
> response header through supported ASP.NET Core request state, Htmxor preserves
> that header on stock normal and direct HTMX GET representations before
> response start, without stale values crossing repeated or interleaved
> requests.

The separate package-only `net10.0` consumer uses the stock cascading
`HttpContext` to set `Content-Language` during component parameter processing.
An asynchronous fixture gate holds four component requests inside that
lifecycle boundary until all four have arrived, so their header-write windows
overlap deterministically. The normal and direct requests select distinct
standards-defined values;
both representations return the matching header, the normal response retains
the stock shell, and the direct response remains shell-free. Interleaved normal
and direct requests to another route return no `Content-Language`, and the
component records that the response had not started before its write.

The deletion test leaves no Htmxor production change. The proposed
`IControlResponseHeaders`, arbitrary attribute execution, and a generic
`HtmxResponse.Headers` alias add no capability for this bounded dynamic case.
Static declarative metadata, HTMX response-header migration, trailers,
redirects, errors, streaming SSR, caching policy, unsafe methods, and broader
header validation remain separate decisions.

Protected behavior for issues #72 and #75:

> When a .NET 10 Blazor static SSR application uses `MapStaticAssets` and
> `@Assets["app.css"]`, adding `AddHtmx` and `HtmxHeadOutlet` preserves the
> stock fingerprinted application URL. A published Production app starts,
> serves the application asset and Htmxor adapter, retains normal full-page
> GET, and executes an application-owned htmx 4.0.0 direct component GET.

The external consumer restores a locally packed Htmxor package, publishes to a
separate output directory, and runs its six tests from that published output
with `ASPNETCORE_ENVIRONMENT` fixed to Production by the application host. An
action-free stock control registers only `AddRazorComponents`,
`MapStaticAssets`, and the stock component root. The Htmxor host adds the
public service and endpoint registration plus `HtmxHeadOutlet`. Both render the
same `app.1nbgm5cxuk.css` URL from `@Assets["app.css"]`, and both serve that
fingerprinted asset from publish output.

The Htmxor-enabled app also serves `_content/Htmxor/htmxor.js` from the packed
package, returns the ordinary document shell on a normal GET, and executes the
exact application-owned htmx 4.0.0 runtime in real Chromium. A browser click
sends a direct `HX-Request: true` GET, receives shell-free component output,
and visibly swaps it into the declared target. Package inspection continues to
verify that Htmxor contains the adapter but no htmx runtime or legacy extension.

The deletion test requires no production change. The public `AddHtmx` path no
longer replaces the stock renderer blamed by the historical #75 report, and
Htmxor no longer owns the htmx files involved in the historical #72 report.
Supported ASP.NET Core static-web-assets and stock Blazor resource collection
behavior already satisfy both current contracts.

Protected behavior for issue #116:

> When application-owned exact htmx 4.0.0 makes a direct component request,
> component code can use Htmxor's response convenience API to emit one or more
> events through `HX-Trigger`, and Chromium observes the declared event after
> the visible response swap. Htmxor exposes neither of the removed timed trigger
> headers.

The beta public API now has one `HtmxResponse.Trigger` behavior. It removes
`TriggerTiming`, `TriggerAfterSwap`, and `TriggerAfterSettle`; every convenience
call merges into `HX-Trigger`. Focused tests retain single events, multiple
events, case-insensitive duplicate suppression, mixed events with and without
JSON details, and event-detail serialization through the application's normal
`IOptions<JsonOptions>` registration. The changelog tells beta callers to
remove the timing argument and rely on htmx 4's post-swap dispatch.
Both overloads reject null, empty, or whitespace event names before attempting
to merge a response header.

The package-only `net10.0` consumer restores a locally packed Htmxor package,
publishes the application, and runs seven tests from Production publish output.
Its issue #116 page owns the component route and calls `HtmxResponse.Trigger`
during the real request lifecycle. A normal GET retains the stock document
shell. Exact application-owned htmx 4.0.0 then sends a direct GET, receives a
shell-free response containing `HX-Trigger` and neither removed header, visibly
swaps the response, and dispatches the server-declared event. The Chromium
listener records that the swapped marker already exists when the event arrives
and observes the detail name configured through `ConfigureHttpJsonOptions`.

This slice adds no generic response-header abstraction, application handler,
controller, Minimal API endpoint, static component action, private reflection,
renderer copy, or embedded htmx runtime. Request-header migration, other
response headers, actions and methods, QUERY, fragments, redirects, navigation,
error swapping, and broader browser conformance remain separate slices.

Protected behavior for issue #118:

> When application-owned exact htmx 4.0.0 requests a component, Htmxor uses one
> valid `HX-Request-Type` value to choose stock full-page or direct partial
> representation, exposes complete `HX-Source` and `HX-Target` element
> identities to component code, and never lets those forgeable values grant a
> route, method, action, authorization, or antiforgery bypass.

`HtmxRequest.RequestType` is a nullable typed `Full` or `Partial` value. Only
one exact `partial` value selects `RoutingMode.Direct`; `full`, missing, blank,
unknown, multi-valued, or contradictory input stays on the stock path, so an
HTMX-only route is not reachable. Full body-level and `hx-select` requests use
the stock page representation, including generated unsafe actions after their
server-owned method/action selection and antiforgery validation.

`HtmxRequest.Source` and `Target` preserve htmx 4's raw `tag#id` or tag-only
shape. Retained target representation hints compare tags case-insensitively and
IDs ordinally. `HtmxAsyncLoad` uses the same bounded comparison to preserve its
own lazy-load representation. A retained `HtmxRoute.Target` or `Targets` hint
can filter the server-declared endpoint candidates by representation identity,
but cannot create or grant route, method, action, authorization, or antiforgery
authority; `Source` does not route. The request-side `Trigger`, `TriggerName`,
and `Prompt` properties and constants, plus the `HtmxRoute.Trigger` and
`TriggerName` selectors, are removed. Source-based endpoint routing was not
added, and fragment behavior remains deferred.

Beta migration: replace request `Trigger`/`TriggerName` reads with `Source`,
update target comparisons from id-only values to `tag#id` or tag-only values,
and use `RequestType` rather than boost/target inference for representation.
Remove trigger-based `HtmxRoute` filters instead of replacing them with source
filters. Optional prompt-extension headers remain available through the stock
`HttpContext.Request.Headers` collection.

Protected behavior for issue #125:

> When a Razor-backed component owned by either a stock `@page` route or an
> omitted-`Methods` `HtmxRoute` declares a simple application-authored
> `@onpost`, `@onput`, `@onpatch`, `@ondelete`, or `@onquery` binding on an
> otherwise supported element with static `hx-target="#selector"`, Htmxor
> generates the same component action and HTTP allow-list whether `hx-target`
> appears before or after the handler binding.

The additional-text action parser now accepts one narrowly characterized
preceding attribute shape: lowercase `hx-target` with a double-quoted `#`
followed by one simple identifier. All other preceding attributes continue
through the existing bounded parser, and the handler binding must still be one
double-quoted simple method-group name. `hx-target` without a binding emits no
action and cannot widen the server allow-list.

Compiler coverage compares byte-identical action and route-registration output
for both attribute orders across POST, PUT, PATCH, DELETE, and QUERY under both
supported Razor route owners. The locally packed package consumer places the
target first on a stock PUT and an explicit-Methods HTMX-only PATCH. Both keep
their compiled route and method selection, authorization, antiforgery,
request-instance lifecycle, callback dispatch, and static SSR response through
the real generated registration boundary.

Protected behavior for issue #127:

> When a package-consuming .NET 10 Blazor static-SSR application has a Razor
> component without `@page` own an omitted-`Methods` `HtmxRoute` containing
> `{ItemId:int}`, Htmxor supplies `ItemId` to the request-owned component as
> `Int32` for direct GET rendering and its declared component action. Invalid
> input remains rejected by routing without a callback.

The bounded action generator emits one private nested route-processing component
for the supported literal omitted-Methods Razor route. It carries the same
template through the public stock `RouteAttribute`, but is not an exported page
or an endpoint owner. Runtime validation requires the processor to be a
nonabstract `IComponent` in the application assembly, not a public top-level
type, and to have exactly one stock route matching the compiled `HtmxRoute`.
The generated HTMX-only endpoint remains the only selected route.

For each matching request, Htmxor changes only the request-local endpoint view
given to the stock Razor component invoker: the stock public `Router` receives
the private nested processing type and endpoint-selected `RouteData`, processes the
constrained value without another route match, and the existing direct host
renders the original request-owned component with those processed values. The
normal application endpoint collection is unchanged, and no conversion switch,
second match, private reflection, renderer copy, controller, or Minimal API
handler is added.

The published Production package consumer proves direct GET and real htmx 4 PUT
delivery of `ItemId` as `System.Int32`, query supply, request-scoped state,
initialization, callback identity, authorization, antiforgery ordering, and
shell-free static SSR. A normal request remains `404`. `not-an-int` remains
`404` with no new initialization or callback. The retained issue #111 QUERY
route also uses an `int` parameter and remains green through the same processing
path.

Protected behavior for issue #129:

> When a package-consuming .NET 10 Blazor static-SSR application returns
> application-owned rendered HTML with HTTP `422` from a generated component
> action, Htmxor preserves that status and body. Application-supplied htmx 4.0.0
> follows its native policy: by default it swaps the error body into the selected
> target and fires `htmx:response:error`; when the source explicitly declares
> `hx-status:422="swap:none"`, the same response and event remain observable but
> the target stays unchanged.

The deletion test leaves Htmxor production code unchanged. The generated PUT
callback selects `422 Unprocessable Entity` through the existing public
request-owned `HtmxEventArgs.Response.StatusCode(HttpStatusCode)` API, changes
component state, and renders deterministic direct HTML. A normal GET remains
`200` with the stock application shell. A missing antiforgery token returns
`400` before another request component initializes or the callback runs.

The separate package-only `net10.0` consumer restores a locally packed Htmxor
package, publishes in Release, fixes its runtime environment to Production,
starts real Kestrel, and runs application-owned exact htmx 4.0.0 in Chromium.
Two application-authored buttons call the same generated PUT action with the
same valid stock antiforgery token. Each browser network response retains exact
`422` and the same nonempty component body, while each callback runs on a
distinct initialized request component.

Under native htmx 4 defaults, the first response visibly replaces its target
and dispatches one `htmx:response:error` whose standardized request context
reports status `422`. The second source declares
`hx-status:422="swap:none"`; it receives the same status and text-identical body
and dispatches the same error event, but its target's complete markup remains
unchanged. The client declaration changes only browser policy. It does not add
or widen the server route, PUT action, authorization, or antiforgery intent.

Protected behavior for issue #131:

> When a package-consuming .NET 10 Blazor static-SSR application invokes one
> generated component DELETE action, Htmxor preserves native htmx 4.0.0
> request-value behavior without exposing the stock antiforgery token. Default
> `hx-delete` excludes enclosing form values. Explicit
> `hx-include="closest form"` carries the application value in the DELETE URL,
> while Htmxor retains the valid antiforgery header and removes only the stock
> antiforgery field from request transport. Authorization and antiforgery reject
> before the request component callback.

The separate package-only application publishes in Production, starts Kestrel,
and runs exact application-owned htmx 4.0.0 in Chromium. One authorized stock
`@page` owns one generated `@ondelete` action. Two relative sources retain the
compiled route and existing `mode` and `existing` query values. The default
source sends no body and no enclosing application or antiforgery form value.
The explicit source also sends no body, carries the deterministic application
value through htmx 4's native DELETE query placement, and keeps both the stock
antiforgery field name and its exact token value out of the URL.

Both successful requests carry the exact stock token in Htmxor's
`RequestVerificationToken` header, invoke the callback once on distinct
initialized request components, and visibly swap deterministic static SSR
output. An unauthenticated DELETE returns `401`, and an authenticated
DELETE with an invalid token returns `400`; neither initializes another request
component or invokes the callback. The generated DELETE declaration remains the
server method authority; the existing package/browser matrix retains its
client-only DELETE `405` control.

The adapter correction is limited to htmx's pending DELETE value collection at
`htmx:config:request`: after copying the valid token into the header, it removes
only `__RequestVerificationToken`. It does not rewrite routes, parse or scrub
general query values, accept query antiforgery evidence, change another method,
or alter server authorization, antiforgery metadata, middleware, or validation.

## Executable evidence

- Meaningful red at `66139317b9edae1fff2ff73fa5175381ee3487b1`: the new .NET 10 hosted test discovered and executed one test, then failed during real application startup with the expected `NullReferenceException` in the obsolete private-reflection component discovery path.
- Focused proof at `8dcca3c0749cf53e310b1dff9dc22612b0d5e8f5`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 1 passed, 0 failed, 0 skipped.
- Broader proof at the same clean commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 102 quality tests, 1 .NET 10 hosted test, and 150 existing non-browser tests. Total: 253 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Mutation testing was not run. Issue #78 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for the complete issue #81 matrix at `29cecb64a8bf9466c3bd7c2dfdb9874d347edcb0`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --blame-hang --blame-hang-timeout 5min` discovered and executed 21 tests; 12 passed, 9 failed, 0 skipped. Direct GETs returned `500` for all eight typed constraints and for a present optional `int`, while their normal GETs returned `200`. The `nonfile` valid case, all nine rejected-input parity cases, optional absence, and the issue #78 route test passed before the production change.
- Focused proof at clean implementation commit `0c3fec1b8c3425ef37c2d93a5fa131f3b0c2a649`: the same command discovered and executed 21 tests; 21 passed, 0 failed, 0 skipped.
- Broader proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 102 quality tests, 21 .NET 10 hosted tests, and 150 existing non-browser tests. Total: 273 discovered, 273 executed, 273 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Mutation testing was not run. Issue #81 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for issue #83 used the test tree at commit `46f5b5324c64bff111a8e9bbb38ea812c22067ef` plus a temporary negative-control mutation that removed authorization metadata from component endpoints: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue83AuthorizationTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 4 tests; 2 passed and 2 failed. The anonymous and claim-deficient cases reached their first status assertion with `200` instead of `401` and `403`. The mutation was removed and left no production diff.
- Focused proof at the same clean commit with the same filtered command: 4 discovered, 4 executed, 4 passed, 0 failed, 0 skipped.
- Hosted-project proof at the same clean commit without the filter: 25 discovered, 25 executed, 25 passed, 0 failed, 0 skipped.
- Broader proof at the same clean commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj --no-restore -- check --profile fast` passed 102 quality tests, 25 .NET 10 hosted tests, and 150 existing non-browser tests. Total: 277 discovered, 277 executed, 277 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Mutation testing was not run. Issue #83 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for issue #85 used the test tree preserved in `6d0fcf4dafe6e840423eb6e32eec41b1c8e3c7e3` with the unchanged production behavior from `4f2c0d81d25141643894d19972e1b701a9982615`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue85FormTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 2 tests; 1 passed and 1 failed. Before the shell assertion failed, the valid POST had bound `accepted-value`, initialized one request component with a new request scope, and invoked its callback once with that value. Its response still contained the stock `<html>` shell. The missing-token request passed with `400` and zero form-binding, initialization, or callback activity.
- Focused proof at clean implementation commit `0a87dcd8b50cb5fd1be6a4ddae57601986aaea4a`: the same filtered command discovered and executed 2 tests; 2 passed, 0 failed, 0 skipped.
- Hosted-project proof at the same clean implementation commit without the filter: 27 discovered, 27 executed, 27 passed, 0 failed, 0 skipped.
- Broader proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj --no-restore -- check --profile fast` passed 102 quality tests, 27 .NET 10 hosted tests, and 150 existing non-browser tests. Total: 279 discovered, 279 executed, 279 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Mutation testing was not run. Issue #85 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for issue #87 is preserved at `e48bc29bec6da718ee4e2c90cd60ed09a3f26f4b`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue87DeleteActionTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 hosted test; 0 passed, 1 failed, 0 skipped. An authorized DELETE with a valid antiforgery cookie and token expected `200` but received `405` from the real stock endpoint before the callback could run.
- Focused proof at clean implementation commit `8c2a528dbff8c528d52199c60330c99ded851b83`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue87UnsafeActionTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 11 cases; 11 passed, 0 failed, 0 skipped.
- Hosted-project proof at the same clean implementation commit without the filter: 38 discovered, 38 executed, 38 passed, 0 failed, 0 skipped.
- Broader proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj --no-restore -- check --profile fast` passed 102 quality tests, 38 .NET 10 hosted tests, and 150 existing non-browser tests. Total: 290 discovered, 290 executed, 290 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Independent Standards and Spec reviews examined `31a61637dcf44ffbd8f3e9c5bbdc38224986c549..8c2a528dbff8c528d52199c60330c99ded851b83`; both passed with zero actionable findings.
- A GitHub review later found one P3 grammar error in a test identifier. Commit `645065ef809306f744bc7cdb8adf1f799b3c0784` corrected only that identifier. At that exact clean head, the focused issue #87 command again passed 11 of 11 cases, and the fast profile again passed 102 quality, 38 hosted, and 150 library tests: 290 passed with 0 failures, skips, errors, or timeouts and a Release build with 0 warnings or errors. Separate Standards and Spec rereviews both passed with zero remaining findings.
- Mutation testing was not run. Issue #87 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for issue #89 is preserved at `4561dc26d1d80f6c776ca46a3131e66982aed164`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue89LifecycleCompositionTests" --blame-hang --blame-hang-timeout 5min` discovered and executed one hosted test; 0 passed, 1 failed, 0 skipped. The normal request first proved one completed application lifecycle pass with no action. The authorized, antiforgery-valid DELETE then returned `200`, completed the application override once, and rendered route, query, user, request scope, and application state, but the response retained callback count `0`; the assertion required `1`.
- Focused proof at clean executable commit `d5153938a2142b49a6b9c5168c14fda4944e315e`: the same command discovered and executed one hosted test; 1 passed, 0 failed, 0 skipped.
- Issue #87 regression proof at the same clean commit used `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue89LifecycleCompositionTests|FullyQualifiedName~Issue87UnsafeActionTests" --blame-hang --blame-hang-timeout 5min`; 12 cases were discovered, executed, and passed with 0 failures or skips. This retained issue #87 method identity, authorization metadata, and antiforgery coverage alongside the composition proof.
- Broader proof at the same clean commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 102 quality tests, 39 .NET 10 hosted tests, and 150 existing non-browser tests. Total: 291 discovered, 291 executed, 291 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Mutation testing was not run. Issue #89 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for issue #91 is preserved at `319a23680d2b89b7eed39504c9e974e0e3772cae`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue91HtmxOnlyRouteTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 hosted test; 0 passed, 1 failed, 0 skipped. The authorized direct HTMX GET expected `200` but received `404` because the component had neither a stock `@page` endpoint nor an assumed-generated HTMX-only endpoint.
- Focused proof at clean implementation commit `47da4a36eb4909f8d120ab032bb12435196a23b9`: the same command discovered and executed 1 hosted test; 1 passed, 0 failed, 0 skipped.
- Broader proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj --no-restore -- check --profile fast` passed 102 quality tests, 40 .NET 10 hosted tests, and 150 existing non-browser tests. Total: 292 discovered, 292 executed, 292 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- The first Standards review found one P3 root-component naming ambiguity. The implementation commit was amended, affected evidence was rerun, and separate Standards and Spec rereviews of `9c89b7f5629b53a1dfed8fd1186dd44d374524c6...47da4a36eb4909f8d120ab032bb12435196a23b9` passed with zero actionable findings.
- Mutation testing was not run. Issue #91 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for issue #93 is preserved at `c3be62f0886117667afdc0e1f2ef97511785ed10`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue91HtmxOnlyRouteTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 hosted test; 0 passed, 1 failed, 0 skipped. With the declaration present and checked-in generated stand-in absent, the authorized direct HTMX GET expected `200` but received `404`.
- Compilation proof at clean implementation commit `0f8d4d761c89afc860ec0cd5058b2b65fd737ee9`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorRouteGeneratorTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 2 tests; 2 passed, 0 failed, 0 skipped. The supported declaration emitted compiling descriptor and exact-group registration source. The unsupported explicit POST declaration emitted no source and one deterministic `HTMXOR001` error.
- Focused hosted proof at the same clean implementation commit: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue91HtmxOnlyRouteTests" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 hosted test; 1 passed, 0 failed, 0 skipped.
- Hosted-project proof at the same clean implementation commit without the filter discovered and executed 40 tests; 40 passed, 0 failed, 0 skipped.
- Fast-profile proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 102 quality tests, 40 .NET 10 hosted tests, and 152 non-browser library and generator tests. Total: 294 discovered, 294 executed, 294 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` passed 102 quality tests, 40 .NET 10 hosted tests, and all 154 library, generator, and browser tests. Total: 296 discovered, 296 executed, 296 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its Cobertura report was `artifacts/results/full/htmxor/26c8a3f7-9950-475f-b3b3-aa5473a791ce/coverage.cobertura.xml`, with two fresh copies recorded by the profile.
- The first independent Standards review of `c3408fc969883f4862d9c6f5c38d698d92931e36...ccfcf1c3a7c505e0481b3571d7850be93e1b80b0` found one P1: a second authorization declaration was accepted but omitted from generated endpoint metadata. The independent Spec review passed with zero actionable findings.
- Review-fix TDD red used the reviewed `ccfcf1c3a7c505e0481b3571d7850be93e1b80b0` tree plus the new test only: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Multiple_authorization_policies_report_one_deterministic_diagnostic" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 0 passed, 1 failed, 0 skipped because the old generator emitted source. After the production fix, the same command passed 1 of 1.
- Focused post-review proof at clean fix commit `cf8cbb38bea4374636e072688e8da5927d6296f8`: the generator-test command with filter `FullyQualifiedName~HtmxorRouteGeneratorTests` discovered and executed 3 tests; 3 passed, 0 failed, 0 skipped. The hosted issue #91 filter discovered and executed 1 test; 1 passed, 0 failed, 0 skipped.
- Fast-profile post-review proof at the same clean fix commit passed 102 quality tests, 40 .NET 10 hosted tests, and 153 non-browser library and generator tests. Total: 295 discovered, 295 executed, 295 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile post-review proof at the same clean fix commit passed 102 quality tests, 40 .NET 10 hosted tests, and all 155 library, generator, and browser tests. Total: 297 discovered, 297 executed, 297 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its Cobertura report was `artifacts/results/full/htmxor/abd714d5-512c-4559-a80e-bb7a21141143/coverage.cobertura.xml`, with two fresh copies recorded by the profile.
- Mutation testing was not run. Issue #93 makes it optional diagnostic evidence for this proof of concept.
- Meaningful red for issue #95 is preserved as Git tree `6c6c18d01ff427c0d6c0d9fd09523b0bdba8252a` over base `55e8d23ea18d4a0c8068be436afc95256a97be09`. The focused `PackedPackageConsumerTests` command packed the unchanged Htmxor package, restored and built the separate .NET 10 consumer, and discovered and executed one outer test and one hosted consumer test. Both failed only because the authorized direct HTMX GET expected `200` but received `404`; pack, restore, build, and test discovery all succeeded, with no generator-load error.
- Focused package proof at immutable executable tree `9952bb350bd3262e3fde6c755737430e861689d9`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 1 of 1 outer tests. Its parsed inner TRX also recorded 1 discovered, 1 executed, and 1 passed. The same test inspected the packed analyzer and runtime assets, package dependencies, authored consumer source, and runtime output.
- Generator compilation proof at the same executable tree: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorRouteGeneratorTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 3 of 3 tests.
- Existing issue #91 hosted regression proof at the same executable tree: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue91HtmxOnlyRouteTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 1 of 1 hosted tests.
- Fast-profile proof at the same executable tree: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 103 quality tests, 40 .NET 10 hosted tests, and 153 non-browser library and generator tests. Total: 296 discovered, 296 executed, 296 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same executable tree: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` passed 103 quality tests, 40 .NET 10 hosted tests, and all 155 library, generator, and browser tests. Total: 298 discovered, 298 executed, 298 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its Cobertura report was `artifacts/results/full/htmxor/111c5d18-fc35-4fa5-aa70-c7569a9b4d77/coverage.cobertura.xml`.
- The first independent Spec review of complete tree `8a44b07001a82137066304cfc0955a7aae856b5a` passed with zero actionable findings. The separate Standards review found two P1 risks: package-wide activation of the intentionally narrow tracer, and silent loss of unrepresented component security metadata.
- An initial review-fix red is preserved as Git tree `bb11835a1e167390d76fa1e4abbcc36583f690de`: one generator test failed because a third inline component attribute still produced registration source. A narrow inline-only rejection made that test pass, but the Standards rereview of tree `73073b5f236563f953447714f3985a36c7ad6606` correctly found that C# partial, inherited, and imported attributes remained invisible to the raw Razor parser. That partial fix was removed.
- Effective-metadata red is preserved as Git tree `0c75ebf5fbfd4519c3d599295fcb4079770ab4c0`. The focused packed-consumer command completed pack, restore, build, generator loading, and one hosted test; the wrong-host request expected `404` but received `200` because the bridge had dropped `[Host]` from the component's C# partial type. At executable tree `9952bb350bd3262e3fde6c755737430e861689d9`, the same outer and inner tests each passed 1 of 1 after the bridge copied public component attributes with inheritance.
- Post-metadata independent Spec rereview of complete tree `c8bd8e37612fc4c80588d8f7ee33dcb12788e54c` passed with zero actionable findings. The separate Standards rereview found no remaining implementation defect, confirmed the component-metadata risk was fixed, and retained package-wide activation as a developer-model decision gate. On 2026-08-28 the user accepted that compatibility break for this non-publishing spike because the published Htmxor package remains a beta. It is release debt, not a blocker for issue #95.
- Pull request #96 initially published exact head `fdb41ba684bace69adbaabf9c219568cf810fa2a`. GitHub Actions run `33151983806` passed package creation, the test job, dependency review, Infer#, and all CodeQL analyses. NuGet validation alone failed with rule 111, `Symbol file not found`, for `analyzers/dotnet/cs/Htmxor.Generators.dll`; the package had omitted the generator's existing portable PDB. This was a package-content failure, not runner or setup evidence.
- CI-fix red is preserved at test-first commit `2d3d85f0604d7e2d668cbb9d93d3c3fd404b857f`. The focused package-consumer command completed pack, restore, build, generator loading, and its hosted test before the outer test failed because `analyzers/dotnet/cs/Htmxor.Generators.pdb` was absent. Fix commit `58fa7aece281f053b1b7bffeec7ebcb8f7dfb33e` packs that PDB beside the analyzer DLL; the focused outer and parsed inner tests each passed 1 of 1.
- Post-CI-fix fast proof at `58fa7aece281f053b1b7bffeec7ebcb8f7dfb33e` passed 103 quality tests, 40 .NET 10 hosted tests, and 153 non-browser library and generator tests: 296 discovered, 296 executed, and 296 passed with no failures, skips, errors, or timeouts. The Release build produced 0 warnings and 0 errors. The full profile passed 103 quality, 40 hosted, and all 155 library, generator, and browser tests: 298 discovered, 298 executed, and 298 passed with no failures, skips, errors, or timeouts. Its fresh Cobertura report was `artifacts/results/full/htmxor/cc201443-d4b7-44bc-90b5-dc97fb0f99ba/coverage.cobertura.xml`.
- Meziantou NuGet validator 2.0.3, run locally with `ContinuousIntegrationBuild=true`, reported no analyzer symbol-location or deterministic-path errors after the fix. It could not validate the two analyzer source URLs because commit `58fa7aece281f053b1b7bffeec7ebcb8f7dfb33e` had not yet been pushed; final source-link and package validation therefore remain a publication-boundary CI check.
- The referenced base-main CI test job did not execute tests because `packages.microsoft.com` returned `403` while Playwright installed Ubuntu dependencies. This is external setup evidence, not a passing baseline or a product failure. Issue #95 does not change the runner.
- Mutation testing was not run. Issue #95 makes it optional diagnostic evidence for this proof of concept.
- Baseline test-only evidence for issue #97 is preserved at commit `86e53fabdbb60945b800e0af117e097de90c9ff0`, whose production tree is unchanged from exact base `e222f75e72f152718c43c534944717dc1a62c51a`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Two_supported_declarations_emit_one_deterministic_compiling_registration" --blame-hang --blame-hang-timeout 5min` discovered and executed one test; it failed with two `HTMXOR001` diagnostics because the generator rejected both supported declarations. The packed consumer at the same test-only tree likewise reached consumer compilation and failed with `HTMXOR001`. These are expected compilation failures, not meaningful behavioral red.
- Meaningful red is preserved at commit `65a636da4b7b0b8c1e9533dec2133bfc09d334d3`, whose exact tree is `37369dba82ed8fdbdb1273de2845bcee37f685e7`. The focused `PackedPackageConsumerTests` command packed the package, restored and built the consumer, loaded the generator, and discovered and executed one outer test plus two inner hosted tests. The report test passed with `200`; the summary test failed with expected `200` but actual `404` because the controlled generator emitted only the first validated registration. The temporary one-route control is completed by the following implementation commit.
- Focused generator proof at clean implementation commit `a94cf491205ed12863ad8ed0ca623a1a7b686c6b`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorRouteGeneratorTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 5 of 5 tests. The two supported declarations emitted one byte-identical generated source regardless of input order, both registrations compiled, and a supported-plus-unsupported set emitted no source with one deterministic diagnostic.
- Focused package proof at the same clean implementation commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 1 of 1 outer tests. Its parsed inner TRX recorded 2 discovered, 2 executed, and 2 passed. The outer test also checked the local package assets, consumer dependency and output boundaries, exactly two authored route and policy declarations, one route group, one component mapping, one Htmxor registration call, and no generated-type or per-component endpoint code.
- Existing single-route regression proof at the same clean implementation commit: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Issue91HtmxOnlyRouteTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 1 of 1 hosted tests.
- Fast-profile proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 103 quality tests, 40 .NET 10 hosted tests, and 155 non-browser library and generator tests. Total: 298 discovered, 298 executed, 298 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` passed 103 quality tests, 40 .NET 10 hosted tests, and all 157 library, generator, and browser tests. Total: 300 discovered, 300 executed, 300 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its fresh Cobertura report was `artifacts/results/full/htmxor/101680d4-a3ff-4099-831a-10326ea5027f/coverage.cobertura.xml`.
- Mutation testing was not run. Issue #97 makes it optional diagnostic evidence and does not require unrelated mutant repair.
- Compiler-backed follow-up negative control used exact Git tree `471a19734492799f1886eb6b1981db51a49738c9` over clean commit `75b1dbc4873dc1ad466ed48c445813716f94d4e3`. The only mutation changed registration rendering to `declarations.Take(1)`. The focused `PackedPackageConsumerTests` command packed the package, restored and built the .NET 10 consumer, loaded the generator, and discovered and executed one outer test plus two inner hosted tests. The report test passed; the summary test failed with expected `200` but actual `404`. Inner totals were 2 discovered, 2 executed, 1 passed, 1 failed, 0 skipped, 0 errors, and 0 timeouts. The temporary mutation was immediately reverted and the worktree returned to the clean parent tree.
- Compiler-backed fast-profile proof at implementation commit `38dc18473a5b4d84714833a6cccbe9518ec80a12`: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 103 quality tests, 40 .NET 10 hosted tests, and 162 non-browser library and generator tests. Total: 305 discovered, 305 executed, 305 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. The only worktree difference reported by the command was the then-untracked research note; production and test inputs matched the commit.
- Final-compilation focused proof at clean commit `3dc8350de488ace5d02d4244bdd87ef9953d0469`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorRouteGeneratorTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 23 of 23 tests. One generator test proves `AdditionalText.GetText()` is never called and emission is input-order independent. Sixteen analyzer tests exercise final-compilation symbol and typed-constant validation, mapped nonconfigurable diagnostics, component-local constants, aliases, array forms, unsupported filters and authorization, declarations outside the root manifest, and the two-component ceiling. Six runtime tests exercise compiled metadata, distinct paired descriptors, group metadata, declarations outside the manifest, unrelated unrouted manifest entries, and zero mappings when the second declaration or its metadata construction fails.
- Focused package proof at the same clean commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` passed 1 of 1 outer tests. Its parsed inner TRX recorded 2 discovered, 2 executed, and 2 passed. The consumer was restored, Release-built, and hosted on .NET 10 from the locally packed package. The first sandboxed run was not product evidence because Windows Event Log access denied while reporting an underlying exception; the same command outside that boundary passed.
- Fast-profile proof at the same clean commit passed 103 quality tests, 40 .NET 10 hosted tests, and 173 non-browser library, generator, analyzer, and runtime tests. Total: 316 discovered, 316 executed, 316 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same clean commit passed 103 quality tests, 40 .NET 10 hosted tests, and all 175 library, generator, analyzer, runtime, and browser tests. Total: 318 discovered, 318 executed, 318 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. The profile retained two identical coverage copies; the canonical report was `artifacts/results/full/htmxor/6894e6f5-d7f9-4003-8f21-3dbb7547490a/coverage.cobertura.xml`.
- The final-compilation, package, fast, and full proofs used .NET SDK 10.0.400 on Microsoft Windows NT 10.0.26200.0.
- Independent Standards and Spec reviews of exact clean head `ef894aa32618e6f78ac1b96b4bae6e21a4508d5a` each found the same P2 defect: the analyzer's colon heuristic accepted compiler-valid route constants such as `/{Id:}`, `/{Id=foo:bar}`, or a valid constrained parameter followed by an unclosed parameter, while the runtime route parser rejected them only during startup.
- Review-fix TDD red used production head `ef894aa32618e6f78ac1b96b4bae6e21a4508d5a` plus only the three new analyzer theory cases now retained in `f02a1c84dde19ed5221396339ce22ac4e936bbc6`. The focused unsupported-metadata command compiled and executed 12 cases; 9 passed and the 3 new cases failed because the analyzer returned no diagnostic. The fix links one narrow route-template contract into both analyzer and runtime assemblies. The same command then passed 12 of 12, and the complete generator, analyzer, and runtime selection passed 26 of 26.
- Post-review package proof at executable tree `f02a1c84dde19ed5221396339ce22ac4e936bbc6` passed 1 of 1 outer tests with 2 of 2 parsed inner hosted tests. The fast profile passed 103 quality, 40 .NET 10 hosted, and 176 non-browser library tests: 319 discovered, executed, and passed. The full profile passed 103 quality, 40 hosted, and all 178 library and browser tests: 321 discovered, executed, and passed. Both profiles had 0 failures, skips, errors, or timeouts and Release builds with 0 warnings or errors. The full profile's canonical coverage report was `artifacts/results/full/htmxor/27a133a0-a1fd-471e-941f-b5a55e95f78a/coverage.cobertura.xml`.
- Full-scope mutation was not run. It is optional diagnostic evidence for this issue and would include unrelated legacy production scope.
- Revised meaningful red for issue #100 is preserved at clean test-only commit `e21c195b82b1f754bfb66b55719347b245616d12`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Package_only_application_registers_two_generated_get_routes_and_one_stock_page_put_action" --blame-hang --blame-hang-timeout 10min` packed Htmxor, restored and Release-built the separate .NET 10 consumer, and started its TestServer. The outer test discovered, executed, and failed 1 of 1. Its inner TRX discovered and executed 8 hosted HTTP tests: 4 passed and 4 failed. Both explicit GET-only HTMX-route controls and the stock GET passed. The authorized stock PUT returned `405 MethodNotAllowed` with binding, initialization, and callback counts all zero; the missing-token, invalid-token, and unauthorized cases also stopped at `405` because PUT was not yet on the stock route.
- Complementary generator red at the same clean test-only commit used `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Simple_stock_page_method_group_emits_one_shared_action_and_compiles_with_route_manifest|FullyQualifiedName~Page_directive_like_text_inside_later_code_comment_does_not_suppress_supported_action|FullyQualifiedName~Stock_page_onput_emits_an_action_without_copying_route_text" --blame-hang --blame-hang-timeout 10min`; 3 tests were discovered and executed, and all 3 failed only because the generated PUT source was absent.
- Post-review route-identity red is preserved at clean test-only commit `58bd3050990f38554fca050cc4a91d473df393c3`. The focused generator and registration selection discovered and executed 26 tests; 25 passed and the new compiled-route cardinality test failed because registration accepted two direct stock routes. The dedicated package command `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Package_only_application_rejects_a_put_component_with_two_compiled_stock_routes" --blame-hang --blame-hang-timeout 10min` discovered, executed, and failed 1 of 1 outer tests after packing Htmxor and restoring, Release-building, and starting the separate .NET 10 consumer. Its parsed inner TRX discovered, executed, and passed 8 of 8 hosted tests after the fixture added `[Route("/alternate-reports/{ReportId:int}")]` in the `.razor.cs` partial and targeted that compiled route. The passing inner checks included the authorized antiforgery-valid alternate-route PUT, request binding, lifecycle, and callback, proving the unintended action widening. A prior sandboxed package selection failed with `NU1301` because network access was denied; it was setup evidence, not product evidence.
- Post-review focused proof at exact clean implementation commit `42082a1bacb71364f5ccf513c8b5e791528d83cf`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorPutActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 26 of 26 tests. The selection covers supported stock-page generation without route text, `hx-put` exclusion, explicit GET-only HTMX-route non-widening, lookalike and dynamic fail-closed cases, manifest validation, compiled endpoint binding, and deterministic rejection when an action owner has two compiled stock routes.
- Post-review package proof at the same exact clean commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 3 of 3 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 8 of 8 hosted HTTP tests. The second-route consumer restored and Release-built but proved registration fails before serving. The computed-callback consumer proved nonconfigurable `HTMXOR002` and no consumer assembly.
- Fast-profile proof at the same exact clean commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `42082a1bacb71364f5ccf513c8b5e791528d83cf`, passed 105 quality tests, 40 .NET 10 hosted tests, and 196 non-browser library, generator, analyzer, and runtime tests. Total: 341 discovered, 341 executed, 341 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Issue #100's revised exact-head proofs used .NET SDK 10.0.400 on Microsoft Windows 10.0.26200. The local full profile and mutation testing were not run: ordinary pull-request CI owns the configured full profile, and mutation is optional for this proof of concept.
- Meaningful red for issue #103 was executed at the original clean test-only commit `16e465ac9f8b89d7bcade0511026d6fdeb1b1e31`, based on exact then-current `origin/main` `bb37e6fe6c07e135b7c1815b62ca271636cd8728`; the same test change is preserved after rebase at `371c1125a4442b6df688a686abbe8b49269721a6`. The focused package-consumer command discovered and executed one outer test and 11 inner hosted tests. The inner run passed 9 and failed 2: an authorized stock DELETE and an omitted-`Methods` HTMX-only PATCH each expected `200` but received `405`, with binding, initialization, and callback counts all zero. The compiler matrix command discovered and executed 8 tests; the existing stock PUT case passed and the other 7 failed only because their expected generated action was absent. Restore, pack, build, generator loading, host startup, and test discovery succeeded.
- Focused compiler, analyzer, and runtime-catalog proof at exact clean implementation commit `fb7e31f3d8378d7b7ab8f521f862429da14dfb50`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 68 of 68 tests. The selection covers all four unsafe bindings under both route owners, HTML and Razor component tags, multiple distinct methods on one tag, omitted and explicit methods, `_Imports.razor` rejection, deterministic nonconfigurable conflicts, and the `hx-post`/`hx-put`/`hx-patch`/`hx-delete`, `hx-action` plus `hx-method`, and `hx-query` negative controls.
- Focused package proof at the same exact clean implementation commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 11 of 11 hosted HTTP tests. The other package builds retained the multiple-stock-route and computed-handler failures and proved the new explicit-method conflict produces nonconfigurable `HTMXOR002` without a consumer assembly.
- Fast-profile proof at the same exact clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `fb7e31f3d8378d7b7ab8f521f862429da14dfb50`, passed 106 quality tests, 40 .NET 10 hosted tests, and 219 non-browser library, generator, analyzer, and runtime tests. Total: 365 discovered, 365 executed, 365 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same exact clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `fb7e31f3d8378d7b7ab8f521f862429da14dfb50`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 221 library, generator, analyzer, runtime, and legacy-browser tests. Total: 367 discovered, 367 executed, 367 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/77d75cf8-6d20-498c-80c3-2a4027532b45/coverage.cobertura.xml`.
- Independent review at documentation head `a7bc1e09c305b1964cadb9a807e4d442f863f93f` found that explicit `HtmxRoute.Methods` was treated as a blanket GET-only restriction rather than an authoritative membership allow-list, and that analyzer and generator duplicated the `HTMXOR002` descriptor. Those reviews and the preceding evidence were invalidated by the fixes below.
- Post-review meaningful red is preserved at clean test-only commit `b78ff30f3c43acbef1ab99b69e51fad7b539879d`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Binding_inside_explicit_htmx_route_methods_is_supported|FullyQualifiedName~Bridge_binds_an_action_allowed_by_explicit_htmx_route_methods" --blame-hang --blame-hang-timeout 5min` discovered and executed 2 tests; both failed. The compiler test received `HTMXOR001` because explicit GET plus PATCH was rejected, and the runtime catalog test threw before binding the explicitly allowed PATCH. Setup, build, and discovery succeeded.
- Post-review focused compiler, analyzer, and runtime-catalog proof at exact clean fix commit `732a957c36d080ddef39ca24db744b7d0c803fa4`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 70 of 70 tests. Matching explicit GET plus PATCH declarations now compile and bind while inferred methods outside the explicit set still fail closed with nonconfigurable `HTMXOR002`; unsupported `QUERY` remains rejected, omitted-`Methods` inference is unchanged, and the client-only negative controls remain covered.
- Post-review package proof at the same exact clean fix commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 11 of 11 hosted HTTP tests. The rejected consumers retained their multiple-stock-route, computed-handler, and explicit-method-conflict failures.
- Post-review fast-profile proof at the same exact clean fix commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `732a957c36d080ddef39ca24db744b7d0c803fa4`, passed 106 quality tests, 40 .NET 10 hosted tests, and 221 non-browser library, generator, analyzer, and runtime tests. Total: 367 discovered, 367 executed, 367 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Post-review full-profile proof at the same exact clean fix commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `732a957c36d080ddef39ca24db744b7d0c803fa4`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 223 library, generator, analyzer, runtime, and legacy-browser tests. Total: 369 discovered, 369 executed, 369 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/1052b5d6-42d0-4e23-bf98-5966e6a5441a/coverage.cobertura.xml`.
- Audit antiforgery-ordering red is preserved at clean test-only commit `834e1058991c191c846ad6252180d7194397308d`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Unsafe_generated_route_requires_effective_antiforgery_after_prior_disabling_metadata" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 0 passed and 1 failed because ordered required-then-disabled metadata left effective validation false. The same command passed 1 of 1 after `6b2fc684afb780a71032b8ec800526a9c830dc0a` appended Htmxor's required metadata when the effective last entry was not true.
- Audit actionless-route red is preserved at clean test-only commit `9ed1938ca40e13cb2ac0d0066ba7b64c5220eab2`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Package_only_application_infers_stock_and_htmx_only_unsafe_actions" --blame-hang --blame-hang-timeout 10min` discovered and executed 1 outer test, which failed because its parsed inner run passed 12 and failed 2 of 14 hosted tests. Missing and invalid antiforgery tokens on an explicit GET plus DELETE route with no generated action both returned `200`, bound parameters once, and initialized the component once. The authorized valid-token DELETE already rendered successfully. After `47302b39e2ad02c8bdd2c10eb15bb5da38ebde40`, the same outer selection passed 1 of 1 and its inner run passed 14 of 14; the rejected requests return `400` before binding or initialization while the authorized request still renders without a callback.
- Audit static-handler red is preserved at clean test-only commit `adfb00626985a8c5d960af37b92e7dfbd412f342`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Static_handler_is_rejected_as_a_nonconfigurable_action_declaration" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 0 passed and 1 failed because the analyzer returned no diagnostic for a static `@ondelete` method group. After `1e641355974493aab0655a9e763fe17e367d3303`, the same command passed 1 of 1 with deterministic nonconfigurable `HTMXOR002` resolved through public Roslyn symbols.
- Audit mutable-default red is preserved at clean test-only commit `c52ac7c9388d4b40c52205c556d7b03d9a1b4ba7`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Build_keeps_omitted_methods_get_only_when_public_defaults_are_mutated" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 0 passed and 1 failed because mutating the public shared default widened a subsequently constructed omitted route to `POST`. After `18345461d328c70256f8181308cc51b248d16370`, the same command passed 1 of 1 across sequential POST and TRACE mutation controls; new attribute instances and catalog descriptors remained GET-only.
- Audit later-markup red is preserved at clean test-only commit `bc2715c0d650bc434d6656414503764c274bf0ec`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~binding_after_prior_markup_emits_a_compiling_action" --blame-hang --blame-hang-timeout 5min` discovered and executed 2 tests; both failed because no action source was generated. After `217dd95642400509759d77c3bd8bc4ca53e178a6`, the same command passed 2 of 2 for a later HTML binding under `@page` and a later Razor component-tag binding under omitted-`Methods` `HtmxRoute`. The complete `HtmxorActionGeneratorTests` selection passed 36 of 36, retaining fail-closed comment, code, raw-string, interpolation, and nonbinding controls.
- Audit focused proof at exact clean implementation commit `217dd95642400509759d77c3bd8bc4ca53e178a6`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 75 of 75 tests.
- Audit packed-package proof at the same exact clean implementation commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests. The rejected consumers retained their multiple-stock-route, computed-handler, and explicit-method-conflict failures.
- Audit fast-profile proof at the same exact clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `217dd95642400509759d77c3bd8bc4ca53e178a6`, passed 106 quality tests, 40 .NET 10 hosted tests, and 226 non-browser library, generator, analyzer, and runtime tests. Total: 372 discovered, 372 executed, 372 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Audit full-profile proof at the same exact clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `217dd95642400509759d77c3bd8bc4ca53e178a6`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 228 library, generator, analyzer, runtime, and legacy-browser tests. Total: 374 discovered, 374 executed, 374 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/c95a2b41-c438-43ae-a4d5-e98b7b93288e/coverage.cobertura.xml`.
- Independent Standards and Spec reviews at exact documentation head `4c63e0ea1caf675cf5e651666c12977f23f86bc8` both found the same P1: a simple handler identifier could resolve to a static delegate-valued field or property because semantic validation inspected only method symbols. Those reviews and all preceding exact-head evidence were invalidated by the fix below.
- Audit-review meaningful red is preserved at clean test-only commit `cdc476767f947187864ae72d8dd45fc905a9999e`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Static_delegate_handler_member_is_rejected_as_a_nonconfigurable_action_declaration" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 0 passed and 1 failed because a static `Func<HtmxEventArgs, Task>` field bound through `@ondelete` received no diagnostic. After `6299b537ceb8954f191997310d9ddfc8c5dc0bee`, the static method, static delegate field, and supported instance method selection discovered, executed, and passed 3 of 3 with deterministic nonconfigurable `HTMXOR002` for both unsupported handler shapes.
- Audit-review focused proof at exact clean implementation fix `6299b537ceb8954f191997310d9ddfc8c5dc0bee`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 76 of 76 tests.
- Audit-review packed-package proof at the same exact clean implementation fix: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests; the three rejected consumers retained their expected compiler or startup failures.
- Audit-review fast-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `6299b537ceb8954f191997310d9ddfc8c5dc0bee`, passed 106 quality tests, 40 .NET 10 hosted tests, and 227 non-browser library, generator, analyzer, and runtime tests. Total: 373 discovered, 373 executed, 373 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Audit-review full-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `6299b537ceb8954f191997310d9ddfc8c5dc0bee`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 229 library, generator, analyzer, runtime, and legacy-browser tests. Total: 375 discovered, 375 executed, 375 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/22e4f97e-4196-40af-91ac-133b51de9727/coverage.cobertura.xml`.
- Fresh independent rereviews at exact documentation head `c5cd75ab3df9509c0455b28c75f516f37a1d7798` invalidated those reviews and exact-head checks with two separate P1 findings. Standards proved that an `@ondelete`-like token inside multiline `<script>` raw text emitted a DELETE action after the prior-markup change. Spec found that the successful package handler was staged as `Issue97ReportComponentCodeBehind.cs`, not the required matching `Issue97ReportComponent.razor.cs` partial.
- The multiline-script meaningful red is preserved at clean test-only commit `0250a15f740a9d4c79e3d730784b2c9848287df4`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Nonbinding_ondelete_inside_multiline_script_text_does_not_emit_an_action" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 0 passed and 1 failed because `HtmxorGeneratedActions.g.cs` was emitted. After `7c2d3365569751d3e63e7c5b19658452e2fced48`, the new raw-text negative plus both approved later-markup positives passed 3 of 3, and the complete generator selection passed 37 of 37. Prior markup must now be a complete matching single-line element or self-closing element.
- The matching-partial meaningful red is preserved at clean test-only commit `75668806318873f0415e58c010eaec6e435039b7`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Package_only_application_infers_stock_and_htmx_only_unsafe_actions" --blame-hang --blame-hang-timeout 10min` packed Htmxor, restored and Release-built the separate consumer, and passed all 14 inner hosted tests before the outer source-boundary assertion failed because `Issue97ReportComponent.razor.cs` did not exist. Commit `cfc995d9f14b89441224539d76c0279062ea52a4` renames only that template; the identical selection then passed 1 of 1 outer and 14 of 14 inner tests with `PatchReport` in the matching partial.
- Second audit-review focused proof at exact clean implementation fix `cfc995d9f14b89441224539d76c0279062ea52a4`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 77 of 77 tests.
- Second audit-review packed-package proof at the same exact clean implementation fix: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests; the three rejected consumers retained their expected compiler or startup failures.
- Second audit-review fast-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `cfc995d9f14b89441224539d76c0279062ea52a4`, passed 106 quality tests, 40 .NET 10 hosted tests, and 228 non-browser library, generator, analyzer, and runtime tests. Total: 374 discovered, 374 executed, 374 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Second audit-review full-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `cfc995d9f14b89441224539d76c0279062ea52a4`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 230 library, generator, analyzer, runtime, and legacy-browser tests. Total: 376 discovered, 376 executed, 376 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/e6fce091-e760-43b4-8081-56e8b0491bed/coverage.cobertura.xml`.
- A fresh Spec rereview passed exact documentation head `9ef8f8fc4bede1eef52f0e83a078e3b5d39a8848`, but Standards invalidated both rereviews and all preceding exact-head evidence with one residual P1. The self-closing check trusted the line's final slash rather than the opening tag's first closing delimiter, and a matching outer suffix could hide a nested raw-text opener. Both shapes still emitted DELETE actions.
- The residual raw-text meaningful red is preserved at clean test-only commit `2301078da8447b8a4c6e8d733962eeef7a18a80f`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Nonbinding_ondelete_after_misleading_script_slash_does_not_emit_an_action|FullyQualifiedName~Nonbinding_ondelete_after_nested_raw_text_suffix_does_not_emit_an_action" --blame-hang --blame-hang-timeout 5min` discovered and executed 2 tests; 0 passed and both failed because action source was emitted. Commit `494b232d02680045a52e6a1b037cc566de465e7e` adds a passing genuine `<hr />` control. After `ce388a1f10fadb121a48ab6f259f62536a5b693b`, the two residual negatives, original multiline-script negative, and both approved later-markup cases passed 5 of 5; the self-closing control passed 1 of 1; and the complete generator selection passed 40 of 40.
- Third audit-review focused proof at exact clean implementation fix `ce388a1f10fadb121a48ab6f259f62536a5b693b`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 80 of 80 tests.
- Third audit-review packed-package proof at the same exact clean implementation fix: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests; the three rejected consumers retained their expected compiler or startup failures.
- Third audit-review fast-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `ce388a1f10fadb121a48ab6f259f62536a5b693b`, passed 106 quality tests, 40 .NET 10 hosted tests, and 231 non-browser library, generator, analyzer, and runtime tests. Total: 377 discovered, 377 executed, 377 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Third audit-review full-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `ce388a1f10fadb121a48ab6f259f62536a5b693b`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 233 library, generator, analyzer, runtime, and legacy-browser tests. Total: 379 discovered, 379 executed, 379 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/b60caa65-c69b-46a3-ac72-8049f3b43c0f/coverage.cobertura.xml`.
- A fresh Standards review at exact documentation head `5e208d5ab65bbd32a15fbe1a55c76cc4cf13ad11` invalidated both publication reviews and all preceding exact-head evidence with one P1. The parser accepted `<script />` as complete prior markup even though HTML keeps the raw-text element open, so a later `@ondelete` token emitted a DELETE action. No additional Standards findings were identified; the concurrent Spec review found no defect before the head changed but correctly issued no final verdict.
- The self-closing raw-text meaningful red is preserved at clean test-only commit `67776f803b91a60582246d6c6d022ac8e79db872`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Nonbinding_ondelete_after_self_closing_script_syntax_does_not_emit_an_action" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 test; 0 passed and 1 failed because `HtmxorGeneratedActions.g.cs` was emitted. After `f1a7884364ad241a32050e59115ea62cbbf1dae5`, that negative and the genuine `<hr />` positive passed 2 of 2. Commit `36d92a73f1151f14850632a1d45108e2a948bcca` adds a passing compiler boundary control proving that a prior self-closing Razor component line fails closed while bindings on Razor component tags after supported ordinary markup remain green.
- Fourth audit-review focused proof at exact clean evidence commit `36d92a73f1151f14850632a1d45108e2a948bcca`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 82 of 82 tests.
- Fourth audit-review packed-package proof at the same exact clean evidence commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests; the three rejected consumers retained their expected compiler or startup failures.
- Fourth audit-review fast-profile proof at the same exact clean evidence commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `36d92a73f1151f14850632a1d45108e2a948bcca`, passed 106 quality tests, 40 .NET 10 hosted tests, and 233 non-browser library, generator, analyzer, and runtime tests. Total: 379 discovered, 379 executed, 379 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Fourth audit-review full-profile proof at the same exact clean evidence commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `36d92a73f1151f14850632a1d45108e2a948bcca`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 235 library, generator, analyzer, runtime, and legacy-browser tests. Total: 381 discovered, 381 executed, 381 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/ccb7429c-e349-4b6f-a953-610bbb18155d/coverage.cobertura.xml`.
- The final root Spec challenge passed exact documentation head `fc2b12c0e94631db076db468a38ad39848fd103b` with zero findings, but Standards found one remaining P1. The parser accepted `<plaintext></plaintext>` as a complete paired prior element even though HTML's `plaintext` tokenizer state ignores the apparent closing tag through EOF, so a later `@ondelete` token emitted DELETE. The audit confirmed all earlier security, ownership, default, package, and raw-text findings fixed.
- The plaintext meaningful red is preserved at exact clean test-only commit `a129585dfed8bf01c23b54b4131acc3f95f88fba`: after a successful locked restore, `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~apparent_plaintext_pair" --blame-hang --blame-hang-timeout 5min` discovered and executed 2 lowercase and uppercase controls; 0 passed and both failed because `HtmxorGeneratedActions.g.cs` was emitted. The detached evidence worktree was removed afterward.
- After `561bcc2da118ee09e515c037663eebdaf4cb27f6`, the lowercase and uppercase negatives, ordinary paired-markup positive, and existing multiline-script negative passed 4 of 4. The fix excludes only `plaintext` case-insensitively from paired prior markup. A source review of the HTML tokenizer states found no second element context whose apparent end tag cannot exit before EOF; RAWTEXT, RCDATA, script data, and scripting-enabled `noscript` recognize their appropriate end tags. This was a scope check, not browser evidence.
- Final root-audit focused proof at exact clean implementation fix `561bcc2da118ee09e515c037663eebdaf4cb27f6`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 84 of 84 tests.
- Final root-audit packed-package proof at the same exact clean implementation fix: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests; the three rejected consumers retained their expected compiler or startup failures.
- Final root-audit fast-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `561bcc2da118ee09e515c037663eebdaf4cb27f6`, passed 106 quality tests, 40 .NET 10 hosted tests, and 235 non-browser library, generator, analyzer, and runtime tests. Total: 381 discovered, 381 executed, 381 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Final root-audit full-profile proof at the same exact clean implementation fix: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `561bcc2da118ee09e515c037663eebdaf4cb27f6`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 237 library, generator, analyzer, runtime, and legacy-browser tests. Total: 383 discovered, 383 executed, 383 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/9fffe262-c7e4-42f9-b9ad-0e8dda67896c/coverage.cobertura.xml`.
- At exact documentation head `2f794aa008bb0e53dd3910a5325d6efde1f6a51e`, a fresh Spec review found no issue #103 defect, but the independent Standards review reproduced one request-ownership P1. With a global `using static`, an external static method could satisfy the generated bare handler identifier because the analyzer treated an absent component member as supported. The Standards review found no additional defects, and the head and all preceding checks and reviews were invalidated by the fix below.
- The imported-static meaningful red is preserved at exact clean test-only commit `7ef7af1ee1ca686c3417370282792041325d82c9`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Imported_static_handler_outside_component" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 compiler-backed analyzer test; 0 passed and 1 failed because the diagnostic collection was empty. Compilation, build, and test discovery succeeded. After `5177466f0afb09fd087b21d2bd44c04344c5b72b`, the complete analyzer selection passed 25 of 25; an unsafe action handler must now resolve to an instance method on the request-owned component hierarchy, so an absent component match fails closed with nonconfigurable `HTMXOR002`.
- Imported-static review-fix focused proof at exact clean implementation commit `5177466f0afb09fd087b21d2bd44c04344c5b72b`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 85 of 85 tests.
- Imported-static review-fix packed-package proof at the same exact clean implementation commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests; the three rejected consumers retained their expected compiler or startup failures.
- Imported-static review-fix fast-profile proof at the same exact clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `5177466f0afb09fd087b21d2bd44c04344c5b72b`, passed 106 quality tests, 40 .NET 10 hosted tests, and 236 non-browser library, generator, analyzer, and runtime tests. Total: 382 discovered, 382 executed, 382 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Imported-static review-fix full-profile proof at the same exact clean implementation commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `5177466f0afb09fd087b21d2bd44c04344c5b72b`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 238 library, generator, analyzer, runtime, and legacy-browser tests. Total: 384 discovered, 384 executed, 384 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/103d5638-dcee-4117-bf67-17e57265d843/coverage.cobertura.xml`.
- A fresh Standards review at exact documentation head `f518426fed3d7247006112b996807ef4e0640cf7` invalidated that fix, its evidence, and the concurrent Spec review with one narrower P1. The hierarchy scan counted a private base instance method even though it was inaccessible from the generated component partial; C# could therefore ignore that member and resolve the bare handler identifier to a globally imported external static method. Standards reproduced zero analyzer diagnostics, one generated DELETE action, and zero driver or compilation errors, with no additional findings.
- The inaccessible-base meaningful red is preserved at exact clean test-only commit `fcb89cebded23186d8e0df0833c244a0b5b4d6fb`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Imported_static_handler_with_inaccessible_base_collision" --blame-hang --blame-hang-timeout 5min` discovered and executed 1 compiler-backed analyzer test; 0 passed and 1 failed because the diagnostic collection was empty. Compilation, build, and test discovery succeeded.
- Commit `88631ad5cf3da3c7d44f111fefd4355c8bf3fc13` uses Roslyn's public symbol-accessibility contract so inaccessible hierarchy members cannot grant a server action or mask an imported static handler. Commit `696d4539f68ea33a56aa6210412bec87895a2efa` adds the complementary compiler-backed green control for a protected inherited instance handler; the inaccessible collision and accessible inheritance selection passed 2 of 2.
- Accessibility review-fix focused proof at exact clean evidence commit `696d4539f68ea33a56aa6210412bec87895a2efa`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 87 of 87 tests.
- Accessibility review-fix packed-package proof at the same exact clean evidence commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The supported package consumer's parsed inner TRX discovered, executed, and passed 14 of 14 hosted HTTP tests; the three rejected consumers retained their expected compiler or startup failures.
- Accessibility review-fix fast-profile proof at the same exact clean evidence commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` recorded clean HEAD `696d4539f68ea33a56aa6210412bec87895a2efa`, passed 106 quality tests, 40 .NET 10 hosted tests, and 238 non-browser library, generator, analyzer, and runtime tests. Total: 384 discovered, 384 executed, 384 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Accessibility review-fix full-profile proof at the same exact clean evidence commit: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` recorded clean HEAD `696d4539f68ea33a56aa6210412bec87895a2efa`, passed 106 quality tests, 40 .NET 10 hosted tests, and all 240 library, generator, analyzer, runtime, and legacy-browser tests. Total: 386 discovered, 386 executed, 386 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its canonical coverage report was `artifacts/results/full/htmxor/c41cdb38-afe1-4364-8975-624711fb5c55/coverage.cobertura.xml`.
- The first audit packed-package rerun in the sandbox discovered and executed 4 outer tests, but all 4 failed during fresh temporary-consumer restore with `NU1301` because network access was denied. It was setup evidence, not product evidence; the identical command with network access produced the passing packed-package result above.
- The first sandboxed post-rebase locked restore failed with `NU1301` because NuGet network access was denied; it was setup evidence and its chained no-restore test had no valid restored input. The same `dotnet restore --locked-mode` outside that boundary succeeded before all reported post-rebase proofs.
- Issue #103's exact-head proofs used .NET SDK 10.0.400 on Microsoft Windows NT 10.0.26200.0. The full profile's existing Chromium fixture still used embedded htmx 1.9.12 and did not exercise issue #103's package routes or application-supplied htmx 4.0.0. Mutation testing was not run; it is optional for this proof of concept.
- Issue #106 started from freshly fetched exact `origin/main` `a489f30f7a20ec801fe52b5ab4f894382d1d9c90`. Live issue #106 and parent #77 were open when that work began, neither open pull request owned an overlapping file, and the isolated branch `egil/issue-106-explicit-csharp-routes` was clean before work began. The approved mergeable slice discovers project-root C# route declarations only when `HtmxRoute.Methods` is explicit. This includes a matching `.razor.cs` partial and a component authored entirely in C#. Any C#-origin declaration that omits `Methods` fails with deterministic nonconfigurable `HTMXOR001` and contributes no generated registration. Issue #106 is now closed; positive omitted-`Methods` `.razor.cs` inference remains deferred under the parent v1 work.
- Meaningful red is preserved at clean test-only commit `6285dae3646ff8357bdc413315dc1138c69b4de9`, whose production tree is exact base `a489f30f7a20ec801fe52b5ab4f894382d1d9c90`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~All_CSharp_component_with_explicit_methods_is_in_generated_registration" --blame-hang --blame-hang-timeout 5min` compiled the all-C# component and discovered and executed 1 test; 0 passed and 1 failed because `HtmxorGeneratedRouteRegistration.g.cs` omitted `Htmxor.Consumer.AllCSharpComponent`. The assertion failure, not an analyzer compilation error or setup failure, is the behavioral red.
- A post-publication review raised a future-compatibility risk if a peer Razor generator's output ever becomes visible to Htmxor's syntax provider. SDK 10.0.400 does not expose peer-generator output that way, so this is not the issue #106 product red above. The test-only synthetic control at `b56547a2fd5c8922200632048445826b6f1a70da` deliberately supplied a Razor-generated-path declaration in the input compilation. `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Razor_generated_omitted_methods_candidate_does_not_suppress_Razor_manifest" --blame-hang --blame-hang-timeout 5min` compiled successfully, discovered and executed 1 test, and failed because the generated registration was empty. The hardening at `8cc1badea33f950b43b51ed3d82f6d50e0373480` excludes compiler Razor declarations from C# omission suppression while retaining suppression for every authored C# omission.
- A fresh Spec review found that a Razor-backed type could place an explicit route on an arbitrary project-root C# partial. At test-only commit `21614bb1366482325296263dff7f2da3834f7951`, `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Explicit_route_in_nonmatching_CSharp_partial_reports_nonconfigurable_error" --blame-hang --blame-hang-timeout 5min` compiled both cases and discovered and executed 2 tests; 0 passed and 2 failed because neither `Other.cs` nor `Other.razor.cs` produced the required diagnostic. `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Package_only_application_rejects_an_explicit_route_in_a_nonmatching_partial" --blame-hang --blame-hang-timeout 10min` packed and restored the external consumer, discovered and executed 1 outer test, and failed because the invalid consumer incorrectly built with exit code 0. The fix at `9e4b3565e95177154b8fdf9e79f3a9ae1b92d30b` uses the final compiled Razor declaration to require a project-root matching `.razor.cs`; all-C# components remain valid in arbitrary project-root C# filenames, including when an unrelated same-basename Razor type compiles into another namespace.
- Focused compiler proof at exact clean post-review executable head `9e4b3565e95177154b8fdf9e79f3a9ae1b92d30b`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorRouteGeneratorTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 61 of 61 generator, analyzer, and runtime-catalog tests. The selection covers explicit `.cs` and matching `.razor.cs` discovery, nonmatching-partial rejection, arbitrary-filename all-C# ownership, omitted-Methods diagnostics and registration suppression, C# `#line` provenance, same-name Razor action ownership, compiler Razor-manifest preservation, explicit method membership, manual render-tree isolation, and incremental candidate reuse.
- Focused packed-package proof at the same exact clean executable head: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 7 of 7 outer tests. The local pack restored and Release-built an isolated .NET 10 consumer. Its default TestServer run discovered, executed, and passed 12 of 12 tests; the staged explicit actionless-unsafe regression discovered, executed, and passed 14 of 14. The package boundary proves an explicit `GET, PATCH` declaration in the matching `.razor.cs`, an explicit all-C# `GET`, direct rendering with authorization, route binding and lifecycle, normal and unauthorized unavailability, `405` method isolation despite manual `BuildRenderTree` HTMX attributes and callback construction, antiforgery on an explicit unsafe route without an action, failed compilation plus no generated registration when the all-C# declaration omits `Methods`, and failed compilation with no consumer assembly when a Razor-backed route moves to a nonmatching C# partial.
- Fast-profile proof at the same exact clean executable head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 109 quality tests, 40 .NET 10 hosted tests, and 255 non-browser library, generator, analyzer, and runtime tests. Total: 404 discovered, 404 executed, 404 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same exact clean executable head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` passed 109 quality tests, 40 .NET 10 hosted tests, and all 257 library, generator, analyzer, runtime, and legacy-browser tests. Total: 406 discovered, 406 executed, 406 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. It retained two byte-identical fresh Cobertura copies with SHA-256 `2B6E58D0342DD21DD374691799768B046143F8EE9B8028A503C312F4540939E2`; the canonical report was `artifacts/results/full/htmxor/ed335957-43f8-4558-b8ea-e0aa11b8f48f/coverage.cobertura.xml`.
- The first sandboxed locked restore and packed-consumer attempts failed only with `NU1301` because network access was denied. A sandboxed broad library run also failed when Windows Event Log access was denied while ASP.NET Data Protection reported an underlying exception. Those are setup-boundary observations, not meaningful red or product results; the same restore and test boundaries outside the sandbox passed before the evidence above.
- Issue #106's exact-head proofs used .NET SDK 10.0.400 on Microsoft Windows NT 10.0.26200.0. The full profile exercised the existing cached Chromium fixture, but did not prove fresh browser provisioning, Linux, Kestrel, TLS, a published or signed package, a release candidate, other SDK/compiler versions, or the package-only routes in a browser. It did not exercise htmx 4, QUERY, fragments, interactive render modes, performance, or external services. Full-scope mutation was not run; it is optional for this proof of concept and would include unrelated legacy production scope.
- Issue #108 started from freshly fetched exact `origin/main` `5bcd9b89b5a8b885467e3c9f13da629f9cc1d32d` on isolated branch `egil/issue-108-htmx4-browser-get`. Live issue #108 was open and unblocked, issue #106 was closed, superseded PR #74 was closed, and the only open PR, #41, owned renderer paths outside this slice. The starting worktree was clean, and its HEAD equaled current `origin/main` before the branch was created.
- Meaningful red is preserved at clean test-only commit `ad519a1cee4829f21f4a7678caf568fdac6fb755`, whose production tree is the exact starting base: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Head_outlet_emits_no_Htmxor_owned_htmx_runtime_or_configuration" --blame-hang --blame-hang-timeout 5min` compiled successfully, discovered and executed 1 test, and failed 1 of 1 because the rendered public `HtmxHeadOutlet` contained Htmxor's `htmx-config` payload. This was a public behavioral observation, not a setup, build, or discovery failure.
- Focused head-outlet and configuration proof at clean commit `ed1daabae9e1630d479ee257a7dabda7ba16c5a4`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxHeadOutletTest|FullyQualifiedName~HtmxConfigTest" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 8 of 8 tests. The public outlet emitted only `_content/Htmxor/htmxor.js`; `UseEmbeddedHtmx` was absent, while the remaining server configuration contract retained its independent serialization coverage.
- Focused real-package proof at exact clean executable head `e1b9106553d1838a08916e76edc1ce1181ebd61b`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-build --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests.Package_only_application_discovers_explicit_CSharp_routes_and_supported_actions" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 1 of 1 outer tests; its parsed package-consumer TRX passed 12 of 12 inner tests. The produced nupkg retained `staticwebassets/htmxor.js` and contained no htmx runtime, type declaration, event-header extension, or build-only runtime dependency. The external consumer used no Htmxor project reference or internals access.
- Focused htmx 4 browser proof at the same exact clean executable head: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-build --no-restore --filter "FullyQualifiedName~Htmx4PackageBrowserTests.Package_only_net10_application_uses_application_owned_htmx4_for_component_get" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 1 of 1 outer tests; its parsed external-consumer TRX passed 1 of 1 inner tests. The test packed Htmxor locally, restored and Release-built the isolated `net10.0` application, started real Kestrel, and drove real Chromium. It verified the exact application asset hash, stock full-page navigation, executed htmx 4.0.0, loopback-only script ownership, `HX-Request: true`, a shell-free component response, and the visible target swap with no page or console error.
- Fast-profile proof at the same exact clean executable head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 109 quality tests, 40 .NET 10 hosted tests, and 257 non-browser library, generator, analyzer, and runtime tests. Total: 406 discovered, 406 executed, 406 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. The explicit `Category!=Browser` quality filter excluded the new Chromium consumer, while the legacy project retained its existing fully qualified name filter.
- Full-profile proof at the same exact clean executable head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` passed 110 quality tests, 40 .NET 10 hosted tests, and all 259 library, generator, analyzer, runtime, and legacy-browser tests. Total: 409 discovered, 409 executed, 409 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. Its two fresh Cobertura copies had identical SHA-256 `A6E632461387BC0D1281F397541C860FB3FB22B10906F5DBC675A319496572CA`; the canonical report was `artifacts/results/full/htmxor/f14d646a-5860-414f-a93f-1c97513b81f0/coverage.cobertura.xml`.
- Final WSL package proof at exact clean pre-publication head `d2d3885c36a78572e93d72e0f9e038240bc9dc90`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~PackedPackageConsumerTests.Package_only_application_discovers_explicit_CSharp_routes_and_supported_actions" --logger "console;verbosity=detailed" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 1 of 1 outer tests; its parsed package-consumer TRX passed 12 of 12 inner tests.
- Final WSL htmx 4 browser proof at the same exact clean head: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Htmx4PackageBrowserTests.Package_only_net10_application_uses_application_owned_htmx4_for_component_get" --logger "console;verbosity=normal" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 1 of 1 outer tests; its parsed external-consumer TRX passed 1 of 1 inner tests through real Kestrel and Chromium.
- Final WSL fast-profile proof at the same exact clean head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 109 quality tests, 40 .NET 10 hosted tests, and 252 non-browser library, generator, analyzer, and runtime tests. Total: 401 discovered, 401 executed, 401 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Final WSL full-profile proof at the same exact clean head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` passed 110 quality tests, 40 .NET 10 hosted tests, and all 254 library, generator, analyzer, runtime, and legacy-browser tests. Total: 404 discovered, 404 executed, 404 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained a nonempty fresh Cobertura report.
- An independent Standards review of documentation head `7cb3b025e1069fda2c31b5c12e0dd1959c56de49` found that the fresh-application instructions named an htmx 4 asset without explaining how to acquire it. An independent Spec review found that changing the unsafe samples to htmx 4 claimed compatibility that this slice had not executed: the retained adapter still uses the legacy unsafe-request event seam, while the htmx 4 unsafe adapter migration is explicitly deferred.
- The sample-ownership test-only commit `bc60bff7829162c72c5bcf776d2895b5b5cf7298` preserved the resulting behavioral red. `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~SampleRuntimeOwnershipTests" --blame-hang --blame-hang-timeout 10min` compiled successfully, discovered and executed 3 tests, and failed 3 of 3 because each unsafe sample lacked the required app-owned legacy runtime/configuration declaration.
- At clean review-fix executable head `e2ac91524ec9ce911cb7f66a0fff7bbcee1ff4c2`, the three sample ownership controls passed. A combined focused run covering those controls, the package-only route consumer, and the real htmx 4 browser GET discovered, executed, and passed 5 of 5 outer tests. The package fixture's parsed inner run passed 12 of 12, and the browser fixture's parsed inner run passed 1 of 1.
- Fast-profile proof at the same clean review-fix executable head passed 112 quality tests, 40 .NET 10 hosted tests, and 252 non-browser library, generator, analyzer, and runtime tests. Total: 404 discovered, 404 executed, 404 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same clean review-fix executable head passed 113 quality tests, 40 .NET 10 hosted tests, and all 254 library, generator, analyzer, runtime, and legacy-browser tests. Total: 407 discovered, 407 executed, 407 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained a nonempty fresh Cobertura report.
- The first sandboxed focused-browser attempt failed only during isolated NuGet restore with `NU1301` because socket access was denied. Subsequent test-fixture source-mapping and analyzer failures occurred before browser execution and were corrected as test setup. None is counted as meaningful red. The identical final focused command with network and process access passed as recorded above.
- Issue #108's exact-head proofs used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, cached Chromium revision 1234 / 151.0.7922.34, and Microsoft Windows NT 10.0.26200.0. Kestrel used loopback HTTP. The evidence did not provision a fresh browser, use TLS, run on Linux, publish or sign a package, or call a CDN or other live service during browser execution. Full-scope mutation was not run; issue #108 makes it optional, and it would include unrelated legacy production scope.

- The final pre-publication rerun used the same SDK, runtime, Playwright, and Chromium versions on Ubuntu 26.04 under WSL. Playwright's exact Chromium revision and Linux dependencies were provisioned before the successful focused and full runs. Kestrel again used loopback HTTP. TLS, a published or signed package, a release candidate, other SDK/compiler versions, and external services during browser execution remain unproved. Full-scope mutation was not run; issue #108 makes it optional, and it would include unrelated legacy production scope.
- Issue #56 started from exact `origin/main` `8bfa41b3da340b1d10b1d43b31124ece2ba44d4c` on isolated branch `egil/issue-56-htmx4-antiforgery`. Live issue #56 and parent #77 were open, the starting worktree was clean, and no observed open pull request owned this unsafe-request adapter slice.
- Meaningful red is preserved at clean test-only commit `8f83086d18004f1a0abb6963ca4b41a4868b506f`, whose production tree is the exact starting base: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~Htmx4PackageBrowserTests --blame-hang --blame-hang-timeout 5min --logger "console;verbosity=minimal"` compiled successfully, discovered and executed 1 package-only browser test, and failed 1 of 1. Missing-token POST and invalid-token PUT already returned `400` without effects, but the valid POST expected `200` and received `400` because the legacy adapter did not populate htmx 4's request headers. This was a real Kestrel and Chromium behavioral failure, not a setup or discovery failure.
- Focused package-only browser and sample proof at clean implementation commit `87cac54b9dfa958d4b3c98a0cfc897bf803cd301`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Htmx4PackageBrowserTests|FullyQualifiedName~SampleRuntimeOwnershipTests" --blame-hang --blame-hang-timeout 10min` discovered, executed, and passed 4 of 4 outer tests. The browser fixture's parsed external-consumer run passed its one end-to-end test through a locally packed package, real Kestrel, and real Chromium; all three maintained unsafe samples passed exact htmx 4 runtime and stock-antiforgery ownership checks.
- The first Spec review found that the package browser authored and asserted a fixed action identity instead of separating generated route/method selection from opaque identity transport. It also found a mistyped red SHA and two stale descriptions of the migrated legacy runtime. Commit `8679d748cf999c16e624964e26ae276c92d2873e` removes the authored identity from generated action requests, adds the separate htmx 4 context control described above, and corrects the progress record. The focused package-only browser command passed 1 of 1 outer tests at that clean head.
- Full-profile proof at exact clean executable head `8679d748cf999c16e624964e26ae276c92d2873e`: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` passed 113 quality tests, 40 .NET 10 hosted tests, and all 255 library, generator, analyzer, runtime, and browser tests. Total: 408 discovered, 408 executed, 408 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The authoritative Release build produced 0 warnings and 0 errors. The canonical fresh coverage report was `artifacts/results/full/htmxor/2987e475-f47a-49c4-9ffd-8751655ed65b/coverage.cobertura.xml`.
- Fast-profile proof at the same exact clean executable head passed 112 quality tests, 40 .NET 10 hosted tests, and 253 non-browser library, generator, analyzer, and runtime tests. Total: 405 discovered, 405 executed, 405 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Issue #56's exact-head proof used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, Chromium 151.0.7922.34, Ubuntu 26.04 under WSL, and loopback HTTP. It did not exercise TLS, Windows, macOS, Firefox, WebKit, a published or signed package, another target framework, another htmx version, server-farm antiforgery key sharing, or external services. Full-scope mutation was not run: issue #56 makes it optional, and the configured .NET mutation workload does not exercise the changed static JavaScript or browser-test harness.
- Issue #111 started from exact `origin/main` `065fbc9135f9b6e6820e43461c3586657197c5ca` on isolated branch `egil/issue-111-query-actions`. Live issue #111 and parent #77 were open and unassigned, the starting worktree was clean, and no open pull request owned the QUERY slice.
- Meaningful red is preserved at clean test-only commit `e84fc820d6ad9b9928df70badb04eacc36fce1af`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Htmx4PackageBrowserTests" --blame-hang --blame-hang-timeout 5min` successfully packed Htmxor, restored and Release-built the separate .NET 10 consumer, started Kestrel, and launched Chromium. Its inner run discovered and executed 2 browser tests; the existing unsafe-action test passed and the QUERY test failed. Chromium sent a real QUERY with form-encoded content to the stock page, which returned `405` instead of the required `200`. This was a behavioral reachability failure, not a build, setup, browser, or discovery failure.
- Focused compiler and runtime proof at exact clean executable head `d2f5f90bbcd1cae78570ddc3dc2253b426878e9f`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorRouteGeneratorTests|FullyQualifiedName~HtmxorRouteDeclarationAnalyzerTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 109 of 109 tests. It covers both route owners, explicit QUERY acceptance and exclusion, GET preservation, safe metadata, client-only controls, and manual-render-tree non-discovery.
- Focused package-only browser proof at the same exact clean head: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Htmx4PackageBrowserTests" --blame-hang --blame-hang-timeout 5min` discovered, executed, and passed 1 of 1 outer tests. The asserted external-consumer TRX discovered, executed, and passed 2 of 2 browser tests through the locally packed package, real Kestrel, and real Chromium.
- Fast-profile proof at the same exact clean head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` passed 117 quality tests, 40 .NET 10 hosted tests, and 257 non-browser library, generator, analyzer, and runtime tests. Total: 414 discovered, 414 executed, 414 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same exact clean head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` passed 118 quality tests, 40 .NET 10 hosted tests, and all 259 library, generator, analyzer, runtime, and legacy-browser tests. Total: 417 discovered, 417 executed, 417 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained the canonical fresh coverage report at `artifacts/results/full/htmxor/7f0cfb81-f118-412e-a418-4437532c6995/coverage.cobertura.xml`.
- The first independent Standards review of exact clean executable head `d2f5f90bbcd1cae78570ddc3dc2253b426878e9f` passed with zero findings. The independent Spec review found one P2 documentation gap: this progress record still described QUERY as unimplemented. No executable defect was identified; this documentation change resolves that finding.
- Issue #111's exact-head proof used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, Chromium 151.0.7922.34, Ubuntu 26.04.1 under WSL2, exact application-owned htmx 4.0.0, and Kestrel loopback HTTP. It did not exercise TLS, Windows, macOS, Firefox, WebKit, a published or signed package, a release candidate, another target framework, another htmx version, fresh browser provisioning, or external services. Full-scope mutation was not run; issue #111 makes it optional, and the configured full mutation workload includes unrelated legacy production scope.
- Issue #50 started from freshly fetched exact `origin/main` `f17fac74fbcee9738b51a53089e7dc2df628b462` on isolated branch `egil/issue-50-cache-handling`. Live issue #50 and parent #77 were open, no open pull request owned the slice, and main's fast and full CI jobs were green while its deployment-only NuGet publication step was red.
- Meaningful red is preserved at clean test-only commit `57a459de02022319538ae17c85ba445683ee7cfe`: the focused package-only command discovered and executed 1 outer test; its separate consumer restored and Release-built, started Kestrel, and discovered and executed 3 inner tests. The two existing browser tests passed and the cache test failed. With stock `.CacheOutput()` active but no `HX-Request` variation, the first representation was a real cache hit, then the opposite request mode received that cached body; the render probe did not run again. Safe GET emitted no `Set-Cookie`. This was representation contamination, not a build, setup, discovery, or uncached-response failure.
- Focused package-only proof at exact clean head `35bd2a7b24b85f1d4518c322ce44a9066d645f1e`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~Htmx4PackageBrowserTests --blame-hang --blame-hang-timeout 5min --logger "console;verbosity=minimal"` discovered, executed, and passed 1 of 1 outer tests. The asserted consumer TRX discovered, executed, and passed 4 of 4 inner tests, including the attribute-activation control, both cache warm-up orders, and the existing htmx 4 unsafe-action and QUERY suites.
- Fast-profile proof at the same exact clean head passed 117 quality tests, 40 .NET 10 hosted tests, and 257 non-browser library, generator, analyzer, and runtime tests. Total: 414 discovered, 414 executed, 414 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same exact clean head passed 118 quality tests, 40 .NET 10 hosted tests, and all 259 library, generator, analyzer, runtime, and legacy-browser tests. Total: 417 discovered, 417 executed, 417 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained the canonical fresh coverage report at `artifacts/results/full/htmxor/24894a8d-ad53-4ecf-bc23-52b7bdd1d78e/coverage.cobertura.xml`.
- Independent Standards and Spec reviews of the final executable and guidance head both passed with zero actionable findings after the guidance was bounded to every representation-affecting cache input.
- Issue #50's exact-head proof used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, Chromium 151.0.7922.34, Ubuntu 26.04.1 under WSL2, a locally packed unsigned Htmxor package, and Kestrel loopback HTTP. The cache assertions used `HttpClient`, while the same package consumer's other tests used real Chromium. It did not exercise browser cache storage, TLS, Windows, macOS, Firefox, WebKit, a published or signed package, another framework or htmx version, fresh browser provisioning, authentication-dependent caching, boosted or target-selected representations, distributed providers, invalidation, or external services. Full-scope mutation was not run; issue #50 does not require it and the configured workload includes unrelated production scope.
- Issue #18 started from freshly fetched exact `origin/main` `8693ecb4c9180ba6afbb3bb4f85037cb9dd9f3ff` on isolated branch `egil/issue-18-response-headers`. Live issue #18 and parent #77 were open and unassigned, and no open pull request owned the slice. Main's fast and full CI test job was green; the workflow was red only because its deployment step received a NuGet `403` for the configured API key.
- The clean test-only commit `e601dd38a67a87b1603ae36ddcb317813073a8f7` is a negative control, not meaningful red: the focused package-only command discovered and executed 1 outer test and failed 1. Its separate consumer restored and Release-built, started Kestrel, and discovered and executed 5 inner tests; 4 passed and the new assertion observed that `Content-Language` was absent. The component did not yet attempt a header write, so this proves the assertion, route, and representation boundary rather than a Htmxor defect. Because the supported path required no production change, issue #18 uses verified-green package characterization as the repository-approved alternate evidence: the initial supported-path proof is `5434ee0e99f6c74a26a94067b273105f28ca98af`, strengthened by deterministic lifecycle overlap at `19c52fbe9256425650148227306bf242a29068d3`.
- Focused package-only proof at exact clean head `5434ee0e99f6c74a26a94067b273105f28ca98af`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~Htmx4PackageBrowserTests --blame-hang --blame-hang-timeout 5min --logger "console;verbosity=minimal"` discovered, executed, and passed 1 of 1 outer tests. The asserted consumer TRX discovered, executed, and passed 5 of 5 inner tests.
- Post-review focused proof at exact clean head `19c52fbe9256425650148227306bf242a29068d3` used the same command and again passed 1 of 1 outer tests with 5 of 5 asserted inner tests. Four normal/direct component requests reached the asynchronous lifecycle gate before any was released to observe `Response.HasStarted` and write its request-specific header.
- Fast-profile proof at the post-review exact clean head passed 117 quality tests, 40 ASP.NET Core 10 hosted tests, and 257 non-browser library, generator, analyzer, and runtime tests. Total: 414 discovered, 414 executed, 414 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the post-review exact clean head passed 118 quality tests, 40 ASP.NET Core 10 hosted tests, and all 259 library, generator, analyzer, runtime, and legacy-browser tests. Total: 417 discovered, 417 executed, 417 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained two byte-identical fresh coverage copies; the quality summary selected `artifacts/results/full/htmxor/_eagle1_2026-08-30_20_27_13/In/eagle1/coverage.cobertura.xml` as canonical.
- Issue #18's post-review exact-head proof used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, Chromium 151.0.7922.34, Ubuntu 26.04.1 under WSL2, a locally packed Htmxor package, Kestrel loopback HTTP, and `HttpClient` response assertions. The maintained full suite exercised cached Chromium, but the issue #18 header requests did not require browser behavior. TLS, Windows, macOS, Firefox, WebKit, a published or signed package, another framework or htmx version, fresh browser provisioning, unsafe methods, caching interaction, redirects, errors, trailers, streaming SSR, and external services were not exercised. Full-scope mutation was not run; it is not required for this POC and the configured workload includes unrelated production scope.
- Issues #72 and #75 started from freshly fetched exact `origin/main` `99e34b85ec22f8566b33afd8126cb8f7a6c6b3be` on isolated branch `egil/issues-72-75-static-assets`. That commit is the merge commit for PR #114, issue #18 was closed, issues #72, #75, and parent #77 were open and unassigned, and no open pull request owned the slice. Main's exact-head fast/full test job, package creation and validation, Infer#, and CodeQL analyses later passed; the overall CI workflow was red only because its deployment-only NuGet publication step failed.
- The behavior-preserving alternate baseline at clean exact base `99e34b85ec22f8566b33afd8126cb8f7a6c6b3be` used `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~Htmx4PackageBrowserTests --blame-hang --blame-hang-timeout 5min --logger "console;verbosity=minimal"`. It discovered, executed, and passed 1 of 1 outer tests; that test restored and Release-built the package-only consumer and asserted 5 of 5 inner tests. This verified the existing package/browser boundary before extending it to publish output, Production, and a stock static-assets control.
- Focused package-only proof at exact clean executable commit `6869313cb5cb7a7eb7d808f19f6fb2ceddc911f6` used the same command and discovered, executed, and passed 1 of 1 outer tests. The asserted external-consumer TRX discovered, executed, and passed 6 of 6 inner tests from the publish output under `ASPNETCORE_ENVIRONMENT=Production`. The added test compared stock and Htmxor hosts, served the identical fingerprinted application asset, served the packaged adapter, retained the full-page GET, executed exact application-owned htmx 4.0.0 in Chromium, and visibly swapped one direct GET response.
- Fast-profile proof at the same exact clean executable commit passed 117 quality tests, 40 ASP.NET Core 10 hosted tests, and 257 non-browser library, generator, analyzer, and runtime tests. Total: 414 discovered, 414 executed, 414 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same exact clean executable commit passed 118 quality tests, 40 ASP.NET Core 10 hosted tests, and all 259 library, generator, analyzer, runtime, and legacy-browser tests. Total: 417 discovered, 417 executed, 417 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained the fresh canonical coverage report at `artifacts/results/full/htmxor/15365641-db3e-4786-905e-a19e6876710e/coverage.cobertura.xml`.
- Issues #72 and #75's exact-head proof used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, cached Chromium 151.0.7922.34, Ubuntu 26.04.1 under WSL2, a locally packed unsigned Htmxor package, a published framework-dependent `net10.0` application, Production, and Kestrel loopback HTTP. It did not exercise TLS, Windows, macOS, Firefox, WebKit, a NuGet-published or signed package, self-contained or trimmed publish, a release candidate, another target framework, another htmx version, fresh browser provisioning, reverse proxies, containers, or external services. Full-scope mutation was not run; this package/static-assets characterization does not require it and the configured workload includes unrelated production scope.
- Issue #116 started from freshly fetched exact `origin/main` `036294a223d634abb64fe2ee1085499b2853c7af` on isolated branch `egil/issue-116-htmx4-trigger`. Live issue #116 and parent #77 were open, and no current pull request owned the slice. The starting worktree was clean.
- Meaningful public-surface red is preserved at test-only commit `aba0b84c765637b950b2f9ee1ef11cae810a2ebe`, whose parent is the exact starting base. `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Htmx4_response_trigger_surface_does_not_expose_removed_timing_api" --blame-hang --blame-hang-timeout 5min --logger "trx;LogFileName=issue-116-red.trx" --results-directory artifacts/results/issue-116-red --verbosity minimal` compiled successfully, discovered and executed 1 test, and failed 1 of 1 because the shipped assembly still exposed `Htmxor.TriggerTiming`. The earlier missing-assets and whitespace compilation attempts were setup failures and are not red evidence.
- The first independent Standards review found that the initial application-`JsonOptions` test registered a concrete singleton instead of using ASP.NET Core's options pattern. Corrected test-only commit `69bc8ef2f8e189052b96e06ecb1b486eb1c8623c` preserves the resulting behavioral red: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Trigger_uses_application_json_options_for_event_details" --blame-hang --blame-hang-timeout 5min --logger "trx;LogFileName=issue-116-options-red.trx" --results-directory artifacts/results/issue-116-options-red --verbosity minimal` discovered and executed 1 test and failed 1 of 1. The application configured `snake_case` through `IOptions<JsonOptions>`, while the response detail remained `MessageLevel` because production ignored that registration.
- Focused post-review response proof at exact clean executable head `13eedd1bd84a4c94e7f8e7daf4df44331124bb90`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Htmxor.Http.HtmxResponseTests" --blame-hang --blame-hang-timeout 5min --logger "trx;LogFileName=issue-116-unit-review-fix.trx" --results-directory artifacts/results/issue-116-unit-review-fix --verbosity minimal` discovered, executed, and passed 18 of 18 tests with 0 failures or skips.
- Focused post-review package/browser proof at the same exact clean head: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Htmx4PackageBrowserTests" --blame-hang --blame-hang-timeout 5min --logger "trx;LogFileName=issue-116-browser-review-fix.trx" --results-directory artifacts/results/issue-116-browser-review-fix --verbosity minimal` discovered, executed, and passed 1 of 1 outer tests. Its asserted external-consumer TRX discovered, executed, and passed 7 of 7 inner tests from Production publish output.
- Fast-profile proof at the same exact clean executable head passed 117 quality tests, 40 ASP.NET Core 10 hosted tests, and 249 non-browser library, generator, analyzer, and runtime tests. Total: 406 discovered, 406 executed, 406 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same exact clean executable head passed 118 quality tests, 40 ASP.NET Core 10 hosted tests, and all 251 library, generator, analyzer, runtime, and legacy-browser tests. Total: 409 discovered, 409 executed, 409 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained the fresh canonical coverage report at `artifacts/results/full/htmxor/508cceee-1277-4025-ae19-9371253d7936/coverage.cobertura.xml`.
- Issue #116's exact-head proof used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, cached Chromium 151.0.7922.34, Ubuntu 26.04 under WSL2, exact application-owned htmx 4.0.0, a locally packed unsigned Htmxor package, a published framework-dependent `net10.0` application, Production, and Kestrel loopback HTTP. It did not exercise TLS, Windows, macOS, Firefox, WebKit, a NuGet-published or signed package, self-contained or trimmed publish, a release candidate, another target framework, another htmx version, fresh browser provisioning, concurrent event responses, streaming SSR, reverse proxies, containers, or external services. Full-scope mutation was not run; issue #116 makes it optional and the configured workload includes unrelated production scope.
- Copilot's first review of PR #117 found that both public `Trigger` overloads accepted null or whitespace event names instead of producing an immediate argument exception. Test-only commit `616e115a93317b8d381aed7acf61d73d4cb927f0` preserves the resulting meaningful red. `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Trigger_overloads_reject_null_event_names|FullyQualifiedName~Trigger_overloads_reject_whitespace_event_names" --blame-hang --blame-hang-timeout 5min --logger "trx;LogFileName=issue-116-event-name-red.trx" --results-directory artifacts/results/issue-116-event-name-red --verbosity minimal` compiled successfully, discovered and executed 3 cases, and failed 3 of 3 because neither overload threw for null, empty, or whitespace input.
- Focused post-Copilot response proof at exact clean executable head `9e400591e7e2447cc0d29360dcf8d0598e614da4`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Htmxor.Http.HtmxResponseTests" --blame-hang --blame-hang-timeout 5min --logger "trx;LogFileName=issue-116-copilot-fix.trx" --results-directory artifacts/results/issue-116-copilot-fix --verbosity minimal` discovered, executed, and passed 21 of 21 tests with 0 failures or skips. Both overloads now reject null with `ArgumentNullException` and reject empty or whitespace names with `ArgumentException`, all naming `eventName`.
- Focused post-Copilot package/browser proof at the same exact clean head: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Htmx4PackageBrowserTests" --blame-hang --blame-hang-timeout 5min --logger "trx;LogFileName=issue-116-copilot-browser.trx" --results-directory artifacts/results/issue-116-copilot-browser --verbosity minimal` discovered, executed, and passed 1 of 1 outer tests. Its asserted external-consumer TRX again passed 7 of 7 inner Production tests.
- Fast-profile proof at the post-Copilot exact clean executable head passed 117 quality tests, 40 ASP.NET Core 10 hosted tests, and 252 non-browser library, generator, analyzer, and runtime tests. Total: 409 discovered, 409 executed, 409 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Full-profile proof at the post-Copilot exact clean executable head passed 118 quality tests, 40 ASP.NET Core 10 hosted tests, and all 254 library, generator, analyzer, runtime, and legacy-browser tests. Total: 412 discovered, 412 executed, 412 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained the fresh canonical coverage report at `artifacts/results/full/htmxor/c9fede9a-6787-4d3e-a956-f2b2cc1daa85/coverage.cobertura.xml`.
- Issue #118 started from freshly fetched exact `origin/main` `7f6936da6c045d2d0aee1b42c4fffca0d6ff560c` on branch `egil/issue-118-htmx4-request-headers`. Live issue #118 and parent #77 were open, and no open pull request owned the slice. The starting worktree was clean.
- Meaningful routing red is preserved at clean test-only commit `c41e5734695273ab6845ced98a182b2a902648c8`: `dotnet test test/Htmxor.AspNetCore10.Tests/Htmxor.AspNetCore10.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Htmx_only_get_rejects" --blame-hang --blame-hang-timeout 5min --logger "trx;LogFileName=issue118-red.trx" --results-directory /tmp/htmxor-issue118-red --verbosity minimal` compiled the real ASP.NET Core 10 application and discovered and executed 5 cases; 0 passed and 5 failed because missing, blank, unknown, `full`, and contradictory request-type input all returned `200 OK` from the generated HTMX-only route instead of `404 Not Found`. An earlier missing-assets attempt and a sandboxed MSBuild IPC failure were setup failures, not behavioral evidence.
- Focused package/browser proof at clean exact executable head `6affff55302b8aaeddc7fc4fd26f619e96824f52` is included in the full profile below. Its outer package-consumer test restored a locally packed Htmxor package, published a separate `net10.0` application, and asserted 9 of 9 inner Production tests. Real Chromium with exact application-owned htmx 4.0.0 emitted `partial`, `button#issue-118-partial`, and `div#issue-118-partial-target` for a targeted request and received shell-free output; an `hx-select` request emitted `full`, the complete source/target identities, and received the stock shell. Valid generated POST, PUT, PATCH, and DELETE full requests retained callback dispatch and stock rendering.
- The same package boundary sent forged source, target, and action values. Missing, blank, unknown, full, and contradictory request types could not reach the generated HTMX-only route; a forged DELETE could not widen a GET-only allow-list; forged identities could not bypass authorization; and a forged POST without antiforgery proof returned `400` with no component callback activity.
- Focused route-identity consistency proof at the same exact clean executable head: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxRouteAttributeTests|FullyQualifiedName~HtmxorComponentEndpointMatcherPolicyTest|FullyQualifiedName~HtmxAsyncLoadTests" --blame-hang --blame-hang-timeout 5min --logger "trx;LogFileName=issue118-equality-focused.trx" --results-directory /tmp/htmxor-issue118-equality-focused --verbosity minimal` discovered, executed, and passed 25 of 25 tests with 0 failures or skips. Direct equality, hashing, and `HashSet` controls prove case-insensitive tags, ordinal IDs, and symmetric optional-value differences.
- Final fast-profile proof at the same exact clean executable head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj --configuration Release --no-restore -- check --profile fast` passed 117 quality tests, 45 ASP.NET Core 10 hosted tests, and 262 non-browser library, generator, analyzer, and runtime tests. Total: 424 discovered, 424 executed, 424 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Final full-profile proof at the same exact clean executable head: `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj --configuration Release --no-restore -- check --profile full` passed 118 quality tests, 45 ASP.NET Core 10 hosted tests, and all 264 library, generator, analyzer, runtime, and legacy-browser tests. Total: 427 discovered, 427 executed, 427 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained the fresh canonical coverage report at `artifacts/results/full/htmxor/48013a4c-3da5-47bc-9236-aac1ad48e144/coverage.cobertura.xml`.
- Independent final Standards and Spec reviews inspect `7f6936da6c045d2d0aee1b42c4fffca0d6ff560c..6affff55302b8aaeddc7fc4fd26f619e96824f52` separately after lazy-load isolation, fragment scope, structured element-identity, and route equality/hash findings were resolved.
- Issue #118's exact-head proof used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, cached Chromium 151.0.7922.34, Ubuntu 26.04.1 under WSL2, exact application-owned htmx 4.0.0, a locally packed unsigned Htmxor package, a published framework-dependent `net10.0` application, Production, and Kestrel loopback HTTP. It did not exercise TLS, Windows, macOS, Firefox, WebKit, a NuGet-published or signed package, self-contained or trimmed publish, another target framework or htmx version, fresh browser provisioning, reverse proxies, containers, or external services. Full-scope mutation was not run; issue #118 explicitly makes it optional and the configured workload includes unrelated production scope.
- Issue #120 started from freshly fetched exact `origin/main` `2ec870b629e6aeb8b2f56f62397d12c343ba2e45` on branch `egil/issue-120-hx-action-method`. Live issue #120 and parent #77 were open, no open pull request owned the slice, and the starting worktree was clean.
- The post-review supported-boundary characterization at clean exact executable head `f366f14185765db4b4dd65045cc9075fd24e4e22` required no production change. `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~Htmx4PackageBrowserTests.Package_only_net10_production_publish_preserves_assets_and_component_actions --blame-hang --blame-hang-timeout 5min --logger "trx;LogFileName=issue120-review-fix-green.trx" --results-directory artifacts/results/issue120-review-fix-green` discovered, executed, and passed 1 of 1 outer tests; its asserted external-consumer TRX discovered, executed, and passed 11 of 11 published Production tests.
- JavaScript-disabled Chromium loaded item `23`, observed native and enhanced form destinations derived from that route value, submitted native POST `/issue-120/native/23`, invoked the stock page's `@onpost` callback once, retained antiforgery, and received stock full-page output. Exact application-owned htmx 4.0.0 submitted PUT `/issue-120/enhanced/23` with `HX-Request-Type: partial`, invoked the separate authorized HTMX-only component's omitted-`Methods` `@onput` callback once, retained the adapter-supplied antiforgery header, swapped shell-free component output into the intended target, and left the browser URL on the native page. The two callback observations had distinct request identities and exact owner, method, and path values.
- The same browser proof removed the native token and corrupted the enhanced token; both returned `400` before any callback. An unauthenticated enhanced PUT returned `401`, and changing only the live client `hx-method` value to undeclared DELETE returned `405`; neither invoked a callback. The stock GET and all prior htmx 4 controls remain in the same 11-test published consumer.
- The pre-review executable head `7b53152b4a8c5c16ccaed8eda363b2a9aebd1327` passed the fast profile with 117 quality tests, 45 ASP.NET Core 10 hosted tests, and 262 non-browser library, generator, analyzer, and runtime tests. Total: 424 discovered, 424 executed, 424 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors. The fast profile was not rerun after the focused review fix.
- The same pre-review executable head passed the full profile with 118 quality tests, 45 ASP.NET Core 10 hosted tests, and all 264 library, generator, analyzer, runtime, and legacy-browser tests. Total: 427 discovered, 427 executed, 427 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained the fresh canonical coverage report at `artifacts/results/full/htmxor/1180f6ca-b8c6-46d4-99d2-346dcc3feb6b/coverage.cobertura.xml`. The full profile was not rerun after the focused review fix.
- During fixture characterization, a valid `@onpost` after an earlier static `hx-target="#selector"` compiled but emitted no generated action because the current narrow parser rejects `#` in a preceding attribute value. Reordering the binding before `hx-target` produced identical browser markup and exercised the supported path. Markup order controlling server-intent discovery is a real DX gap and focused follow-up candidate, but general parser expansion is outside issue #120 and no compatibility claim is made for that ordering.
- The issue #120 HTMX-only component deliberately keeps `ItemId` as `string` even though its route uses `{ItemId:int}`. A characterization with `int ItemId` reached the enhanced endpoint but returned `500` because the direct HTMX-only path supplied the constrained route value as `string`, which could not be assigned to `Int32`. Issue #81's typed stock-`@page` proof does not cover HTMX-only routes; typed conversion on this path remains an explicit unproved follow-up rather than an issue #120 fix.
- Issue #120's post-review exact-head proof used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, cached Chromium revision 1234 / 151.0.7922.34 on Ubuntu 26.04 under WSL2, exact application-owned htmx 4.0.0, a locally packed unsigned Htmxor package, a published framework-dependent `net10.0` application, Production, and Kestrel loopback HTTP. The Playwright package manifest selected revision 1234, and the cached binary executed locally as `Google Chrome for Testing 151.0.7922.34`. It did not exercise TLS, Windows, macOS, Firefox, WebKit, a NuGet-published or signed package, self-contained or trimmed publish, another target framework or htmx version, fresh browser provisioning, reverse proxies, containers, external services, or full-scope mutation. Mutation was optional for this POC and the configured workload contains unrelated production scope.
- Issue #122 started from freshly fetched exact `origin/main` `e28345567db5d07eea6c29b8ba4ed8825c514333` on branch `egil/issue-122-hx-partial`. Live issue #122 was open with no comments, and the starting worktree was clean.
- Meaningful red is preserved at clean test-only commit `e5798770689f1e35ad501f70153a1ba8f1e27c4b`. The focused outer test packed Htmxor, restored and published the separate Production `net10.0` consumer, started Kestrel, and discovered and executed 12 inner tests. Eleven passed; the new Chromium fragment case received HTTP `500` because `HtmxFragmentElement` rejected the envelope's application-authored `hx-target` and `hx-swap`. The outer test therefore failed 1 of 1. Earlier missing-assets, sandboxed-socket, and fixture-compilation attempts were setup failures and are not red evidence.
- The beta API is consolidated at clean executable commit `09caf7421a6dbbc22b73cddb4f23dadacd0d1f60`: `HtmxFragment` remains wrapperless by default and optionally emits a supplied element, identifier, and unmatched HTML attributes. `HtmxFragmentElement` is removed. A supplied identifier without an element uses `div`, and default direct selection accepts the complete htmx 4 `tag#id` target identity. The maintained samples use the consolidated component. No typed `HtmxPartial` or `HxPartial` adapter was added because ordinary `HtmxFragment Element="hx-partial"` plus captured attributes is clear and raw application markup composes inside a wrapperless fragment. Post-review executable head `0986832151a42c0bedfb01fd266aad056d9df219` preserves the deleted component's implicit `outerHTML` behavior explicitly in both migrated samples and documents that beta migration requirement.
- Focused API proof at the same clean executable head: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~HtmxFragmentTests --blame-hang --blame-hang-timeout 5min --logger "trx;LogFileName=issue122-fragment-unit.trx" --results-directory artifacts/results/issue122-fragment-unit` discovered, executed, and passed 6 of 6 tests. It covers wrapperless default output, supplied element/id/attributes, whitespace normalization, and complete target-identity selection.
- Focused package/browser proof at the same clean executable head: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~Htmx4PackageBrowserTests.Package_only_net10_production_publish_preserves_assets_and_component_actions --blame-hang --blame-hang-timeout 5min --logger "trx;LogFileName=issue122-green-final.trx" --results-directory artifacts/results/issue122-green-final` discovered, executed, and passed 1 of 1 outer tests. Its asserted consumer TRX discovered, executed, and passed 12 of 12 tests from Production publish output.
- The issue #122 Chromium case first received a stock full-page GET with the application shell, ordinary page content, and no `<hx-partial>` envelope. One real htmx GET then returned no page shell or main-swap payload and exactly two top-level `<hx-partial>` envelopes. The response-level assertions retained each `hx-target` and `hx-swap` value and proved one envelope came from `HtmxFragment Element="hx-partial"` while raw application-authored `<hx-partial>` markup composed through a second wrapperless `HtmxFragment`. Exact htmx 4.0.0 updated the two declared targets with `innerHTML` and `outerHTML` semantics while the request's ordinary main target retained its exact markup.
- Post-review fast-profile proof at clean exact head `0986832151a42c0bedfb01fd266aad056d9df219` passed 117 quality tests, 45 ASP.NET Core 10 hosted tests, and 268 non-browser library, generator, analyzer, and runtime tests. Total: 430 discovered, 430 executed, 430 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- Post-review full-profile proof at the same clean exact head passed 118 quality tests, 45 ASP.NET Core 10 hosted tests, and all 270 library, generator, analyzer, runtime, and legacy-browser tests. Total: 433 discovered, 433 executed, 433 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained two byte-identical fresh coverage copies; the canonical report is `artifacts/results/full/htmxor/0153fb4d-8672-44d9-af9a-747bf634265d/coverage.cobertura.xml`.
- Issue #122's post-review exact-head proof used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, cached Chromium revision 1234 / 151.0.7922.34 on Ubuntu 26.04 under WSL2, exact application-owned htmx 4.0.0, a locally packed unsigned Htmxor package, a published framework-dependent `net10.0` application, Production, and Kestrel loopback HTTP. It did not exercise TLS, Windows, macOS, Firefox, WebKit, a NuGet-published or signed package, self-contained or trimmed publish, another target framework or htmx version, fresh browser provisioning, reverse proxies, containers, external services, redirects, unsafe methods, error swapping, caching interaction, streaming, lifecycle or excluded-work performance, exhaustive ordering or out-of-band behavior, or full-scope mutation. Mutation was optional for this POC.
- Independent Spec review found one maintained-sample P2 at reviewed head `0986832151a42c0bedfb01fd266aad056d9df219`: `Counter.razor` named a nonexistent `HtmxPartial` helper even though the accepted API is `HtmxFragment`. Post-review executable head `582e671b5ba4f47d3a823309e9863cbea5f4aac1` corrects that public-DX text. At that exact clean head the MinimalHtmxorApp Release build produced 0 warnings and 0 errors, focused fragment tests passed 6 of 6, the packed Production/Kestrel/Chromium outer test passed 1 of 1 while asserting 12 of 12 consumer tests, and the fast profile passed 117 quality + 45 ASP.NET Core 10 + 268 non-browser tests, 430 of 430 total. Fresh independent Standards and Spec rereviews at exact `582e671b5ba4f47d3a823309e9863cbea5f4aac1` each passed with zero findings. The full profile was not rerun after the sample-text correction; its 433-of-433 proof remains tied to exact `0986832151a42c0bedfb01fd266aad056d9df219`.
- This later progress commit is documentation-only. Final executable and independent-review claims remain tied to exact head `582e671b5ba4f47d3a823309e9863cbea5f4aac1`.
- Issue #64 started from freshly fetched exact `origin/main` `e77f88e5475caf861afe3c5ca052c239266263ae` on branch `egil/issue-64`. The live issue was open, and the isolated starting worktree had no product changes.
- Meaningful red is preserved at clean test-only commit `67f0963204ade54367b76cbdb8aaf76a3a96bb07`. `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --filter FullyQualifiedName~Htmx4PackageBrowserTests --blame-hang --blame-hang-timeout 5min --logger "console;verbosity=minimal"` packed Htmxor, separately restored and published the Production `net10.0` consumer, started real Kestrel and cached Chromium, and discovered and executed 14 inner tests. Twelve passed. The new raw htmx request failed because the stock component endpoint returned `302 Found` instead of successful `HX-Redirect`; the new Chromium case failed because htmx followed that redirect, left the URL on `/issue-64`, and swapped the destination component into the initiating target. The ordinary request already retained the stock `302` plus absolute same-origin `Location` and no `HX-Redirect`. The outer test failed 1 of 1 only because those two new inner tests failed. Earlier missing-assets and sandboxed-socket attempts were setup failures and are not red evidence.
- The clean executable proof at `b8bb2204e6107063281a9d93e4fe23e0c4f76301` keeps the framework `NavigationManager`, stock Razor component request delegate, and request-local direct-render endpoint. After the stock delegate returns from a direct htmx GET, Htmxor adapts only an unstarted `302` with exactly one absolute same-origin `Location` to `200 OK` with that exact location as `HX-Redirect`. The ordinary path never enters the adaptation. No controller, Minimal API, custom navigation manager, navigation exception, renderer copy, private reflection, or public API was added. The successful package/browser proof also observes that the stock redirect response remains unstarted at this post-delegate seam; otherwise the guarded adaptation could not produce the asserted `200` and header.
- Focused package/browser proof at the same clean executable head: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --filter FullyQualifiedName~Htmx4PackageBrowserTests --blame-hang --blame-hang-timeout 5min --logger "console;verbosity=minimal"` discovered, executed, and passed 1 of 1 outer tests in 16 seconds. Its asserted consumer TRX discovered, executed, and passed 14 of 14 tests from Production publish output. Raw HTTP used redirect following disabled: the ordinary request retained `302`, the absolute same-origin destination `Location`, and no `HX-Redirect`; the equivalent direct htmx request returned `200`, the exact destination in `HX-Redirect`, and no `Location`. Exact application-owned htmx 4.0.0 in Chromium finished at `/issue-64/destination` with the full-page shell, and a session-backed mutation observer proved the initiating target never received the destination fragment.
- The fast-profile command `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile fast` at clean exact post-review executable head `5ce00d0610e4e04576595b54114aeac22a136798` passed 117 quality tests, 45 ASP.NET Core 10 hosted tests, and 268 non-browser library, generator, analyzer, and runtime tests. Total: 430 discovered, 430 executed, 430 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- The full-profile command `dotnet run --project eng/Htmxor.Quality/Htmxor.Quality.csproj -- check --profile full` at the same clean exact post-review executable head passed 118 quality tests, 45 ASP.NET Core 10 hosted tests, and all 270 library, generator, analyzer, runtime, and legacy-browser tests. Total: 433 discovered, 433 executed, 433 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained the fresh coverage report at `artifacts/results/full/htmxor/977f1c6e-49f7-43fa-a716-e545ee310746/coverage.cobertura.xml`.
- Standards review identified a default-port origin gap at `b8bb2204e6107063281a9d93e4fe23e0c4f76301`: converting the absolute redirect URI to `HostString` materialized `:80` or `:443`, while a request `Host` may omit its default port. Meaningful review red is preserved at `476cf2e76d8f5e36ea7ec9fee95772cfcf84581f`. The same focused package/browser command discovered and executed 14 inner tests; 13 passed, and only the new real Kestrel request with an omitted Host port failed because it received `302` instead of `200`.
- Standards-review fix `5ce00d0610e4e04576595b54114aeac22a136798` compares scheme and host plus semantic port identity: an explicit request port must match exactly, while an omitted request port accepts only the redirect URI scheme's default port. The focused package/browser command then passed 1 of 1 outer tests while asserting 14 of 14 published consumer tests. This retains exact non-default-port behavior and adds the default-port control without claiming TLS or reverse-proxy behavior.
- Separate final Standards and Spec rereviews inspected the complete `e77f88e5475caf861afe3c5ca052c239266263ae..5ce00d0610e4e04576595b54114aeac22a136798` executable diff plus this progress draft. Standards passed with 0 findings after confirming the semantic-origin fix and the exercised `Response.HasStarted` seam. Spec passed with 0 findings after checking every live issue #64 acceptance criterion, explicit exclusion, command, SHA, count, environment boundary, and forbidden implementation shape. The earlier Standards P1 default-port finding and Spec P2 exact-command finding were both resolved before these rereviews.
- Issue #64's exact-head proof used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, cached Chromium revision 1234 / Google Chrome for Testing 151.0.7922.34, Ubuntu 26.04.1 under WSL2, exact application-owned htmx 4.0.0, a locally packed unsigned Htmxor package, a published framework-dependent `net10.0` application, Production, and Kestrel loopback HTTP. It did not exercise TLS, Windows, macOS, Firefox, WebKit, a NuGet-published or signed package, self-contained or trimmed publish, another target framework or htmx version, fresh browser provisioning, external redirects, `HX-Location`, `forceLoad`, `ReplaceHistoryEntry`, history push/replace behavior, other redirect status codes, middleware or non-component redirects, identity-provider or authentication-return flows, nested-path resolution, streaming, responses already started, errors, caching interaction, interactive render modes, reverse proxies, containers, external services, or full-scope mutation. Mutation was optional for this POC and the configured workload includes unrelated production scope.
- This issue #64 progress commit is documentation-only. Final executable claims remain tied to exact post-review head `5ce00d0610e4e04576595b54114aeac22a136798`.
- Issue #125 started from clean exact fetched `origin/main` `e56475542eb25cb449f7e0723b5f58664bb96aaa` on branch `egil/issue-125-hx-target-order`. Live issue #125 was open and had no comments.
- Meaningful compiler-boundary red is preserved at test-only commit `ddee137ed41269ba0238b1c3b193f65790d6a1f2`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --filter "FullyQualifiedName~Static_id_target_order_preserves_generated_action_and_allow_list" --blame-hang --blame-hang-timeout 5min --logger "console;verbosity=minimal"` discovered and executed 10 cases; 0 passed, 10 failed, and 0 skipped. Every binding-first control emitted compiling generated action output, while the otherwise equivalent `hx-target="#selector"`-first declaration emitted no `HtmxorGeneratedActions.g.cs` for each of POST, PUT, PATCH, DELETE, and QUERY under both stock and omitted-`Methods` route owners. Earlier missing-assets and sandboxed-IPC attempts were setup failures and are not red evidence.
- Focused compiler proof at clean executable commit `a6e4f95f29dc7d3e23b5f62ab2db9f4f52800a36`: the same project and options with filter `FullyQualifiedName~Static_id_target_order_preserves_generated_action_and_allow_list|FullyQualifiedName~Static_id_target_without_a_binding_does_not_grant_a_server_action` discovered, executed, and passed 12 of 12 cases. The complete action and route-registration sources were byte-identical between orders, and the two client-only target controls emitted no action. The complete `HtmxorActionGeneratorTests` filter then discovered, executed, and passed 58 of 58 cases.
- Locally packed package-consumer proof at the same clean executable commit: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --filter "FullyQualifiedName~Package_only_application_discovers_explicit_CSharp_routes_and_supported_actions" --blame-hang --blame-hang-timeout 5min --logger "console;verbosity=minimal"` discovered, executed, and passed 1 of 1 outer tests. Its asserted inner TRX discovered, executed, and passed 12 of 12 .NET 10 TestServer tests. A target-first stock PUT and target-first explicit-Methods HTMX-only PATCH remained reachable only through their compiled generated actions, with the existing route allow-lists, authorization, antiforgery validation, request-owned component lifecycle, and callback state.
- The fast profile at the same clean executable commit passed 117 quality tests, 45 ASP.NET Core 10 hosted tests, and 280 non-browser library, generator, analyzer, and runtime tests. Total: 442 discovered, 442 executed, 442 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- The full profile at the same clean executable commit passed 118 quality tests, 45 ASP.NET Core 10 hosted tests, and all 282 library, generator, analyzer, runtime, and legacy-browser tests. Total: 445 discovered, 445 executed, 445 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors and retained fresh coverage at `artifacts/results/full/htmxor/3be5ac2c-7aca-4f88-ac60-4260e323d7df/coverage.cobertura.xml`.
- Separate independent Standards and Spec reviews inspected the complete `e56475542eb25cb449f7e0723b5f58664bb96aaa..a6e4f95f29dc7d3e23b5f62ab2db9f4f52800a36` executable diff plus this progress draft. Both initially found the same P2 evidence-labeling error: the compiler matrix uses omitted-`Methods` HTMX-only owners, but the package consumer's PATCH route explicitly declares GET and PATCH. The progress wording was corrected without changing behavior. Final Standards and Spec rereviews each passed with 0 findings and worst priority none. Standards independently reproduced the 10-of-10 red, the current focused green, and the package proof; Spec independently ran the 58-case generator suite and package proof.
- Issue #125's exact-head proof used .NET SDK 10.0.400 on Ubuntu under WSL2, a locally packed unsigned Htmxor package, and a separate `net10.0` consumer on TestServer. The issue-specific package proof did not use a browser because no browser behavior changed. It did not exercise Kestrel, TLS, Windows, macOS, a NuGet-published or signed package, another target framework, arbitrary CSS selectors, dynamic target expressions, general Razor parsing, or full-scope mutation. Mutation was optional for this POC and was not run.
- This issue #125 progress commit is documentation-only. Final executable claims remain tied to exact proof head `a6e4f95f29dc7d3e23b5f62ab2db9f4f52800a36`.
- Issue #127 started from clean exact `origin/main` `cf468f4db77557fd12a4c955b7daeb239f0a25b2` on branch `egil/issue-127-typed-route-values`. The live issue was open with no comments, and the starting worktree was clean and detached at that exact fetched base before the branch was created.
- Meaningful red is preserved at clean test-only commit `4d58d03e6e703d564cd49660f96bfe739d52f968`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --filter FullyQualifiedName~Htmx4PackageBrowserTests --blame-hang --blame-hang-timeout 5min --logger "console;verbosity=minimal"` packed the unchanged runtime, restored and published the separate Production `net10.0` consumer, started Kestrel, and executed 15 inner tests. Thirteen passed. The valid direct GET and real htmx 4 PUT both selected `/issue-120/enhanced/{ItemId:int}` and returned `500` because the raw route string was assigned to `Int32`. The invalid route, authorization, antiforgery, and other retained controls passed. Earlier sandboxed socket and missing-assets attempts were setup failures and are not red evidence.
- Focused compiler/runtime proof at exact clean implementation head `16ec8f77db0006b317977ea765c624ae3de674a9`: `dotnet test test/Htmxor.Tests/Htmxor.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~HtmxorActionGeneratorTests|FullyQualifiedName~HtmxorAttributedRouteCatalogTests" --blame-hang --blame-hang-timeout 5min --logger "console;verbosity=minimal"` discovered, executed, and passed 77 of 77 tests. The new compiler case verifies a private stock-route processor is emitted only for the supported omitted-Methods HTMX-only action, and the complete filters retain route ownership, method inference, generated action identity, and runtime catalog validation.
- Focused package/browser proof at the same exact clean head used the red command and passed 1 of 1 outer tests while asserting 15 of 15 published consumer tests. The issue #127 cases prove normal `404`, valid direct GET `200`, invalid constrained input `404`, and one real htmx 4 PUT `200`; `ItemId` is `23` on both valid paths, the action invokes once, and query, request scope, lifecycle, authorization, antiforgery, action identity, htmx 4.0.0, static SSR output, Kestrel, group mapping, and the retained issue #111 QUERY action remain effective.
- Fast-profile proof at the same exact clean head passed 117 quality tests, 45 ASP.NET Core 10 hosted tests, and 281 non-browser library, generator, analyzer, and runtime tests. Total: 443 discovered, 443 executed, 443 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The authoritative Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same exact clean head passed 118 quality tests, 45 ASP.NET Core 10 hosted tests, and all 283 library, generator, analyzer, runtime, and legacy-browser tests. Total: 446 discovered, 446 executed, 446 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The authoritative Release build produced 0 warnings and 0 errors and retained fresh coverage at `artifacts/results/full/htmxor/14ccda5b-e07b-478f-b7f9-f509082bec68/coverage.cobertura.xml`.
- Issue #127's exact-head proof used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, cached Chromium revision 1234 / 151.0.7922.34 on Ubuntu 26.04.1 under WSL2, exact application-owned htmx 4.0.0, a locally packed unsigned Htmxor package, a published framework-dependent `net10.0` application, Production, and Kestrel loopback HTTP. It did not exercise TLS, Windows, macOS, Firefox, WebKit, a NuGet-published or signed package, self-contained or trimmed publish, another target framework or htmx version, fresh browser provisioning, the complete route-constraint matrix, optional, catch-all, custom, or all-C# route declarations, reverse proxies, containers, external services, or full-scope mutation. Mutation was optional for this POC and was not run.
- Separate independent Standards and Spec reviews inspected the complete `cf468f4db77557fd12a4c955b7daeb239f0a25b2..16ec8f77db0006b317977ea765c624ae3de674a9` executable diff plus this progress draft. Both found and resolved a P2 incorrect full red SHA; Spec also found and resolved P2 prose that briefly described the generated private nested processor as internal and top-level. Standards' final rereviews found and resolved further P2 prose overclaims that runtime validation requires private nesting or generally nonpublic accessibility; the record now states the actual nonabstract application `IComponent`, not-public-top-level, single-stock-route, and exact-template checks. Final outcomes were 0 findings with worst priority none on both axes. Spec independently passed the documented 77-of-77 generator/catalog command; Standards' attempt at that command was blocked before discovery by sandboxed MSBuild IPC and was recorded only as a setup failure. Neither review independently repeated the heavy package/browser or profile commands.
- Automatic Copilot review found that replacing the public four-parameter `HtmxorGeneratedComponentAction` constructor with a five-parameter optional signature would break already compiled callers. Test-only review-red commit `8cb2eabfde0099a2e6c0ffbbfd55de721211cc22` added an exact public-constructor-shape assertion; its focused run discovered and executed one test and failed because the four-parameter constructor was absent.
- Review-fix commit `41850ad79c40ff513ba5ce399db6685e1bbd3dc8` restores the exact four-parameter constructor as a delegating overload and keeps a distinct five-parameter constructor for generated route-processor metadata. The existing generated bridge remains source-compatible and the exact public constructor metadata shape is restored. This review fix did not load a separately precompiled consumer assembly, so runtime binary-compatibility execution remains unproved.
- Focused post-review proof at that exact clean commit passed all 78 generator and attributed-route catalog tests, including the constructor-shape regression.
- Post-review fast-profile proof at that exact clean commit passed 117 quality tests, 45 ASP.NET Core 10 hosted tests, and 282 non-browser library, generator, analyzer, and runtime tests. Total: 444 discovered, 444 executed, 444 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The Release build produced 0 warnings and 0 errors.
- This issue #127 progress commit is documentation-only. Route-behavior claims remain tied to exact proof head `16ec8f77db0006b317977ea765c624ae3de674a9`; the post-review constructor and fast-profile claims are tied to exact fix head `41850ad79c40ff513ba5ce399db6685e1bbd3dc8`.
- Issue #129 started from clean exact freshly fetched `origin/main` `5de3ba5a36a9773dbafc39b9a5f7a67e8b274958` on branch `egil/issue-129-error-response`. The live issue was open with no assignee or comments, no pull request referenced it, and no other worktree owned an issue #129 branch.
- Meaningful package/browser red is preserved at clean test-only commit `c4ab4199c3dd33e6bc1c24a95493b1e44deddd51`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Htmx4PackageBrowserTests" --blame-hang --blame-hang-timeout 5min --logger "console;verbosity=minimal"` packed unchanged Htmxor production code, restored and published the separate Production `net10.0` application, started Kestrel, and executed all 16 inner tests. Fifteen passed and one failed because the issue #129 Chromium network response was `200` instead of the required `422`; the callback's negative-control implementation deliberately omitted only the existing public status-selection call. Earlier missing-assets and sandboxed-socket attempts were setup failures and are not red evidence.
- Focused package/browser proof at exact clean executable commit `669db318326a55e7a7e0bff3d8a07ddc27268343` used the red command and passed 1 of 1 outer tests while asserting 16 of 16 published consumer tests. The issue #129 case proves a normal `200` shell GET, exact application-owned htmx 4.0.0, a missing-token `400` with no new request-component initialization or callback, and two valid generated PUT callbacks on distinct initialized request components. Both browser network responses retain exact `422` and the same deterministic nonempty body containing the expected error marker. The default source visibly presents that marker and fires `htmx:response:error` with context status `422`; the application-authored `hx-status:422="swap:none"` source fires the same error event but leaves its target's complete markup unchanged.
- Post-review focused package/browser proof at exact executable commit `5ea0995ec82e302cd00f7c5e4e6d657cb2edb6af` used the same command and again passed 1 of 1 outer tests while asserting 16 of 16 published consumer tests. The strengthened browser assertion explicitly rejects the stock `<html>` and `data-stock-shell` markers from the error body, captures the default target's initial markup, and proves native default replacement removes its original content. This run occurred with only the progress draft uncommitted; the executable tree exactly matched that commit.
- Fast-profile proof at exact clean executable commit `669db318326a55e7a7e0bff3d8a07ddc27268343` passed 117 quality tests, 45 ASP.NET Core 10 hosted tests, and 282 non-browser library, generator, analyzer, and runtime tests. Total: 444 discovered, 444 executed, 444 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The authoritative Release build produced 0 warnings and 0 errors.
- Full-profile proof at exact clean executable commit `669db318326a55e7a7e0bff3d8a07ddc27268343` passed 118 quality tests, 45 ASP.NET Core 10 hosted tests, and all 284 library, generator, analyzer, runtime, and browser tests. Total: 447 discovered, 447 executed, 447 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The authoritative Release build produced 0 warnings and 0 errors and retained fresh coverage at `artifacts/results/full/htmxor/abd0fdd8-788e-4d22-b9dc-b44b1959ee23/coverage.cobertura.xml`.
- Issue #129's exact-head proof used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, cached Chromium revision 1234 / 151.0.7922.34 on Ubuntu 26.04.1 under WSL2, exact application-owned htmx 4.0.0, a locally packed Htmxor package, a published framework-dependent `net10.0` application, Production, and Kestrel loopback HTTP. It did not exercise TLS, Windows, macOS, Firefox, WebKit, a NuGet-published or signed package, self-contained or trimmed publish, another target framework, htmx version, status code, method, response media type, global policy, exception path, redirect, streaming response, proxy, container, external service, or full-scope mutation. Mutation was optional for this POC and was not run.
- Separate independent Standards and Spec reviews inspected the complete `5de3ba5a36a9773dbafc39b9a5f7a67e8b274958..5ea0995ec82e302cd00f7c5e4e6d657cb2edb6af` executable diff plus this progress draft. Standards found and resolved two P2 evidence gaps: the red record initially attributed every inner test to Chromium, and the browser test did not explicitly reject a stock shell. Spec found and resolved one P2 default-swap gap: the original assertion could not distinguish replacement from append. Both reviews then found and resolved the same P2 profile-provenance ambiguity in the draft. Their rereviews passed with 0 findings and worst priority none. A later Standards audit at `a1e7ed95afd74cac11cac170973f0c94b1d212b1` found this P2 documentation overclaim because Playwright `TextAsync` proves decoded-text equality, not byte identity. The claim now says text-identical body; separate final Standards and Spec rereviews inspected exact documentation-correction head `a5c4475130b57c58fb71bd564cc74418b714e050`, and each passed with 0 findings and worst priority none. Both ran `git diff --check`; neither reran tests or heavy profiles. Spec independently passed the focused package/browser command at `669db318326a55e7a7e0bff3d8a07ddc27268343`; Standards did not independently rerun runtime verification. Neither independently reran fast, full, or mutation.
- Issue #131 started from clean exact freshly fetched `origin/main` `5707b09cf8b7459ca1a753d2f7fe183017e2e8ca` on branch `egil/issue-131-delete-form-security`. Live issue #131 and parent #77 were open with no assignee or comments, no open pull request owned the slice, and no other local or remote worktree or branch owned issue #131.
- Meaningful package/browser red is preserved at clean test-only commit `58c111e0a6190191ab6d3178575536ab5da76e8e`: `dotnet test test/Htmxor.Quality.Tests/Htmxor.Quality.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Htmx4PackageBrowserTests.Package_only_net10_production_publish_preserves_assets_and_component_actions" --blame-hang --blame-hang-timeout 5min --logger "trx;LogFileName=issue-131-red.trx" --results-directory artifacts/results/issue-131-red --verbosity minimal` packed unchanged Htmxor production code, restored and published the separate Production `net10.0` application, started Kestrel, and executed 17 of 17 inner tests. Sixteen passed and the issue #131 Chromium case failed after its `401`, `400`, default DELETE, and explicit DELETE paths executed because the explicit request URL contained `__RequestVerificationToken`. Initial missing-assets and ambiguous relative-fixture-path attempts were setup/fixture failures and are not red evidence.
- Focused package/browser proof at exact clean executable commit `a0f60d90faa004038c9320bd04afdd7a392cdb96` used the same command with `issue-131-green.trx` and passed 1 of 1 outer tests while asserting 17 of 17 published consumer tests. The issue #131 case proves exact application-owned htmx 4.0.0, a relative generated DELETE route, existing query values, no request body, default exclusion of the application and antiforgery form values, retention of the explicitly included application value while only the antiforgery field and value are excluded from the URL, exact valid antiforgery-header equality, distinct initialized request components, one callback each, deterministic swaps, unauthenticated `401`, and authenticated invalid-token `400` before initialization or callback.
- Fast-profile proof at the same exact clean executable commit passed 117 quality tests, 45 ASP.NET Core 10 hosted tests, and 282 non-browser library, generator, analyzer, and runtime tests. Total: 444 discovered, 444 executed, 444 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The authoritative Release build produced 0 warnings and 0 errors.
- Full-profile proof at the same exact clean executable commit passed 118 quality tests, 45 ASP.NET Core 10 hosted tests, and all 284 library, generator, analyzer, runtime, and browser tests. Total: 447 discovered, 447 executed, 447 passed, 0 failed, 0 skipped, 0 errors, and 0 timeouts. The authoritative Release build produced 0 warnings and 0 errors and retained fresh canonical coverage at `artifacts/results/full/htmxor/ed5d5880-49b5-41e7-ac68-2622be2bb1b6/coverage.cobertura.xml`.
- Issue #131's exact-head proof used .NET SDK 10.0.400, ASP.NET Core 10.0.11, Microsoft.Playwright 1.62.0, cached Chromium revision 1234 / 151.0.7922.34, Ubuntu 26.04.1 under WSL2, exact application-owned htmx 4.0.0, a locally packed unsigned Htmxor package, a published framework-dependent `net10.0` application, Production, and Kestrel loopback HTTP. It did not exercise TLS, Windows, macOS, Firefox, WebKit, a NuGet-published or signed package, self-contained or trimmed publish, another target framework or htmx version, inherited inclusion, another selector shape, files, multipart or JSON data, large or streaming payloads, custom antiforgery, proxies, containers, external services, or performance. Full-scope mutation was optional for this POC and was not run.
- Separate Standards and Spec reviews inspected base `5707b09cf8b7459ca1a753d2f7fe183017e2e8ca` through executable `a0f60d90faa004038c9320bd04afdd7a392cdb96` plus docs head `9bccb0e78f1dc220b3e70517d0569fa756ffccff`. Both found the P2 exclusivity wording fixed by `c33ddd40527bcea3e79d4f202f62d46ac094203d`; Standards also found the P2 shell-free wording now removed. Independent Standards and Spec rereviews both inspected corrected head `1f9fc413c2d9f2a1d3cc6a08b64331f7b36d1580` and returned 0 findings; both prior exclusivity wording findings are resolved; Standards' shell-free wording finding is resolved. Neither reviewer reran heavy profiles.

## Remaining limits

- Issues #95, #97, #100, #103, #106, #108, #56, #111, #50, #18, #72, #75, #116, #118, #120, #122, #64, #127, #129, and #131 prove one locally packed package with the current SDK and dependency set. Issues #72, #75, #116, #118, #120, #122, #64, #127, #129, and #131 additionally prove a framework-dependent application publish, but none proves a NuGet-published or signed package, a release candidate, package compatibility across SDK or compiler versions, or a broader target-framework matrix.
- The matrix uses one representative valid and rejected value per documented constraint. It does not exhaust textual representations, undocumented custom conversion constraints, catch-all routes, or unconstrained routes.
- The direct path is proved on ASP.NET Core 10 only. The supported framework matrix remains unproved.
- The authorization proof uses one deterministic scheme and one claim policy. It covers the earlier GET path, issue #120's PUT path, and issue #131's DELETE path, but not scheme selection, custom challenge or forbid handlers, identity-provider integration, or other HTTP methods.
- The issue #85 proof covers one stock named `EditForm`, one valid value, and one missing-token POST. It does not cover multiple forms, validation failures, invalid-token variants, file uploads, normal POST parity, or custom method discovery. Issue #87 proves unsafe route/query instance dispatch separately, without request-body or form binding.
- Issue #103 replaces the verb-specific package proof with generated stock DELETE and HTMX-only PATCH paths. The issue #87 and #89 fixed stand-ins remain as earlier hosted regression fixtures, not as the only unsafe-method evidence.
- Issue #89 covers an application-authored public `SetParametersAsync` override. An application that explicitly implements `IComponent.SetParametersAsync` would conflict with the generated explicit member and needs a future diagnostic or developer-model decision. Repeated parameter delivery, an override that intentionally omits its base call, async actions, request-body and form binding, multiple actions on one verb, multiple-route action mapping, multiple action-owning components, navigation, exception and cancellation behavior, `ShouldRender` overrides, and streaming SSR remain unexercised.
- The issue #85, #87, and #89 hosts run on Windows TestServer with the stock ephemeral Data Protection provider. They do not exercise Kestrel, TLS, persistent key storage, server-farm key sharing, Linux, a browser, or an application-selected HTMX runtime.
- Issues #91, #93, #95, #97, #100, and #103 ran their hosted contract only on Windows TestServer. They did not exercise Kestrel, TLS, Linux runtime, a browser, or an application-selected HTMX runtime. Issue #108 adds one package-only Kestrel and Chromium GET path on Windows and Linux, but it does not prove those earlier package-only routes and actions in a browser.
- Beyond issue #108's GET, issue #56's unsafe-action matrix, issue #111's QUERY slice, issue #50's server OutputCache slice, issue #122's pure multi-target fragment response, issue #64's full-page redirect navigation, issue #129's application-owned error response, and issue #131's native DELETE placement, the htmx 4 browser tests do not exercise layouts, browser caching, concurrency, enhanced navigation, interactive render modes, broader fragment shapes, out-of-band content, history, extensions, or performance.
- Htmxor no longer packages or emits htmx 1.9.12, htmx type declarations, the
  event-header extension, or an Htmxor-owned `htmx-config` payload. No maintained
  sample or browser fixture retains the old 1.9.12 asset or configuration;
  references to it above describe historical evidence at earlier exact heads.
  The maintained samples and browser fixtures now own exact htmx 4.0.0 assets,
  and unsafe UI uses stock Blazor antiforgery inputs. Htmxor still owns only the
  narrow `htmxor.js` adapter.
- Issues #108, #56, #111, #50, #116, #118, #120, #64, #129, and #131 cover application-owned htmx
  4.0.0 GET, unsafe methods, QUERY, one server-cache variation, `HX-Trigger`
  response events, full/partial request type, and complete source/target element
  identities plus distinct `hx-action`/`hx-method` progressive form destinations
  using htmx 4 defaults. Other response headers, explicit inheritance, broader DELETE body behavior,
  broader status codes and error policies, standardized events and request context, broader fragment shapes,
  out-of-band ordering, history, extensions, broader cache policy, repeatable CI browser
  provisioning, package publication, and the supported framework matrix remain
  separate evidence. Issue #64 adds the narrow successful `HX-Redirect` full-page
  navigation shape. Issue #103 proves only at the compiler boundary that client
  declarations do not grant server methods.
  Issue #122 proves one pure response with two selected partial envelopes, two
  target forms, and `innerHTML`/`outerHTML` browser swaps while the ordinary main
  target remains unchanged. Target fallback from an envelope `id`, mixed
  main/OOB/partial content, main-before-partial ordering, missing targets,
  repeated targets, and other swap styles remain unproved.
- Issue #116 proves one direct GET response with one JSON-detail event in
  Chromium. Focused server tests cover multiple and deduplicated names, mixed
  detailed and detail-free events, and configured JSON naming, but those shapes
  were not all dispatched in a browser. Concurrent responses, invalid or
  unusually encoded event names, very large details, explicit per-call
  `JsonSerializerOptions`, exceptions during detail serialization, redirects,
  errors, history restore, boosted requests, streaming responses, caching
  interaction, and other response headers remain unproved.
- Issue #129 proves one generated stock-page PUT callback selecting `422` with
  deterministic HTML, native default swapping, one source-owned `swap:none`
  policy, and `htmx:response:error` status context. It does not prove another
  status code or method, exception-to-response mapping, ProblemDetails or JSON,
  authentication failures, redirects, streaming or response-started errors,
  network failures, retries, logging, global `noSwap`, custom event handling,
  caching interaction, or concurrent error responses.
- Issue #131 proves one generated stock-page DELETE action, default form-value
  exclusion, and explicit `closest form` inclusion with one deterministic
  application input and the stock antiforgery input. It does not prove inherited
  inclusion, another selector shape, automatic application-field selection,
  `hx-vals`, files, JSON, multipart, large or streaming values, a custom
  antiforgery system, another method or htmx version, or general secret/query
  scrubbing. The adapter intentionally removes only the stock field name from
  htmx's pending DELETE values and does not accept antiforgery from the query.
- Issue #111 proves one QUERY callback per route owner with one form-encoded
  value read through the public request API. It does not prove JSON or other
  content types, large or streaming bodies, cancellation, concurrent QUERY
  requests, multiple QUERY bindings on one component, QUERY composition with an
  unsafe action on the same component, or broad typed HTMX-only route
  conversion. Issue #127 retains its representative `int` QUERY owner through
  the same stock route-processing seam.
  Client declarations, including `hx-query`, `hx-action`, and `hx-method`, never
  grant QUERY reachability.
- The legacy test application still uses internal private-reflection discovery and global service replacements. Later slices must replace the behavior they cover instead of extending that prototype.
- Issue #91 proves one assumed-generated HTMX-only GET route with an `int`
  constraint, one authorization policy, and one application route-group metadata
  marker. Issue #127 proves `Int32` delivery for one Razor-backed
  omitted-Methods `{ItemId:int}` route on direct GET and PUT through a published
  package consumer. Neither slice proves other constraints, multiple generated
  routes or components, collisions,
  normal-only or dual generated reachability, HEAD or OPTIONS behavior, or the
  full range of application group and security conventions. Issue #103 adds a
  package-generated stock DELETE and an HTMX-only component-tag PATCH while the
  earlier PUT, PATCH, and DELETE stand-ins remain regression fixtures.
- The issue #97 follow-up removes Razor-text interpretation. Its path-only
  generator does not claim the Razor grammar, while its diagnostic analyzer uses
  the final compilation and therefore sees component-generated members and
  compiler-bound attributes. Nested component directories or namespaces,
  future SDK or analyzer-pipeline changes, more than two routed components,
  multiple routes on one component, collision policy, normal-only or dual
  reachability, and a final public API remain unproved.
- The compiler-bound route-declaration model distinguishes Razor-backed types
  from all-C# types in the final compilation. Issue #106 proves explicit
  `HtmxRoute.Methods` on both a matching project-root `.razor.cs` partial and an
  all-C# project-root component through the real packed-consumer boundary. A
  Razor-backed route in a nonmatching C# partial fails with nonconfigurable
  `HTMXOR001` and produces no consumer assembly. The generator may still list
  that explicit type in the failed compilation because its pre-compilation seam
  cannot distinguish an unrelated same-basename Razor file in another namespace;
  generated-registration suppression is required only for omitted C# methods.
  The original compiled `HtmxRouteAttribute` remains authoritative. A C#
  declaration without `Methods`, including one in
  `.razor.cs`, now fails closed with `HTMXOR001` and no generated registration.
  Inferring omitted methods from companion Razor markup remains deferred until
  a supported pre-compilation ownership seam exists. V1 still does not treat
  `_Imports.razor` as an `HtmxRoute` declaration source. Effective non-route
  metadata from imports remains a separate unproved metadata-preservation case.
- Issues #95, #97, and #100 package those exact tracers and no broader behavior. Generated
  registration overload selection requires the application to pass an endpoint
  argument whose static type is exactly `RouteGroupBuilder`. Widening it to
  `IEndpointRouteBuilder`, or passing the application instead, selects the
  existing fallback and does not register the generated route. Signature
  collisions, alternative registration shapes, and promotion of the hidden
  generated-to-runtime bridge into a final public developer API remain unproved
  and out of scope.
- The analyzer activates for every application that restores the package.
  Applications with more than two `HtmxRoute` components, or with a declaration
  outside the proved project-root route and authorization contract, receive
  build errors. The runtime catalog also scans the application assembly and
  fails closed if compiled declarations do not match the generated manifest.
  Exactly two project-root components are proved; multiple route attributes on
  one component, collision policy, nested namespaces, broader route filters,
  broader unsafe action discovery, IDE live-analysis parity, trimming, Native
  AOT, and startup cost remain unproved. This fail-fast behavior is accepted
  for the locally packed beta spike: unrelated unrouted manifest entries are
  ignored,
  while routed or security metadata is never silently omitted. It is not the
  final v1 compatibility contract and must be resolved before a stable release
  candidate.
- Issue #103 recognizes simple double-quoted method groups for `@onpost`,
  `@onput`, `@onpatch`, and `@ondelete` on supported HTML or Razor component
  start tags, including after a complete single-line ordinary markup line. It
  supports multiple different unsafe methods on one tag and one packaged handler
  implemented in the matching `.razor.cs` partial, but it does not claim the
  Razor or HTML grammar. Apparent paired `plaintext` markup is explicitly
  unsupported and fails closed. Arbitrary or multiline preceding markup,
  complex or dynamic expressions, markup after code or control transitions,
  conditional markup,
  local `@namespace`, nested components, all-C# action declarations, action
  declarations authored in `.razor.cs`, overloads, or multiple callbacks for
  one HTTP method remain unproved. Repeated parameter delivery, exceptions,
  cancellation, and body and form binding expansion also remain unproved. A
  prior self-closing Razor component line is also outside this POC
  parser: the generator receives raw `.razor` `AdditionalText` without Razor
  component-tag resolution, and capitalization cannot safely distinguish a
  component from case-insensitive HTML raw-text elements. Discovery therefore
  fails closed at that line. This does not limit supported bindings on Razor
  component tags after the proved plain-markup prefix. A generated stock action
  owner must still have exactly one direct compiled `RouteAttribute`; zero or
  multiple routes fail before endpoint conventions or HTMX-only mappings are
  added.
- Issue #125 adds only a preceding lowercase `hx-target` with one double-quoted
  simple static ID selector. It does not prove other CSS selector syntax,
  dynamic target expressions, case variants, arbitrary multiline or
  control-flow markup, another handler expression, or general Razor or HTML
  parsing. A target remains client-only and never grants a server method.
- Issue #127 emits a private nested stock route processor only for the supported exact
  literal Razor-backed omitted-Methods `HtmxRoute` when the generator also owns
  a supported action. It proves one required `{ItemId:int}` value, direct GET,
  and PUT. It does not prove actionless routes, explicit `Methods`, all-C#
  declarations, optional or catch-all parameters, custom constraints, the full
  documented constraint matrix, multiple routes on one component, or another
  framework version.
- Issue #64 proves only the stock default local `NavigateTo` shape on a direct
  htmx GET: one unstarted `302` carrying one absolute same-origin `Location`.
  External and relative redirect locations, other status codes, unsafe methods,
  response-started behavior, middleware and non-component redirects, navigation
  options, authentication flows, caching, streaming, errors, and broader browser
  or framework matrices remain unproved and are not claimed.

## Current implementation slice

Issue #131 establishes the current native DELETE request-content contract:

> When a package-consuming .NET 10 Blazor static-SSR application invokes one
> generated component DELETE action, Htmxor preserves native htmx 4.0.0
> request-value behavior without exposing the stock antiforgery token. Default
> `hx-delete` excludes enclosing form values. Explicit
> `hx-include="closest form"` carries the application value through native
> DELETE query placement, while the stock token remains only in Htmxor's valid
> antiforgery header. Authorization and antiforgery reject before callback.

The published Production package consumer proves the two application-authored
source shapes, exact browser URL/body/header behavior, relative route and
existing query preservation, authorization and antiforgery ordering, distinct
request-component lifecycle and callback identity, and deterministic swaps
through Kestrel and Chromium. The narrow adapter correction removes only the
stock antiforgery field from htmx's pending DELETE values after retaining its
header; all other values and methods keep their established behavior.

The recommended next slice is native htmx 4 main-target and out-of-band swap
ordering for one component-owned response. It should use the same package,
Production Kestrel, and Chromium boundary with one generated action, one main
target, one application-authored out-of-band target, and deterministic DOM/event
observations that prove main content applies before out-of-band content without
changing server fragment selection or route, method, authorization, and
antiforgery ownership. Streaming, caching expansion, broader fragment shapes,
lifecycle and excluded-work performance, broader typed constraints, and positive
omitted-`Methods` inference remain separate slices.
