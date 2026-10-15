# Patched framework baseline review (#329)

Reviewed 2026-10-15 under the [owner-approved prerequisite](https://github.com/egil/Htmxor/issues/77#issuecomment-6180682303).

When an application uses Htmxor on either pinned framework lane, Htmxor preserves paired stock component-endpoint behavior and validates its monitored adapter against that exact shipped baseline.

| Lane | SDK | Runtime and ASP.NET Core packages | Runtime tag | Built-source commit in dotnet/dotnet |
| --- | --- | --- | --- | --- |
| net10.0 | 10.0.402 | 10.0.13 | v10.0.13 | 4099f83977f4d29604a9aeb971fb0a7bc1d5f699 |
| net11.0 | 11.0.100-rc.2.26504.105 | 11.0.0-rc.2.26504.105 | v11.0.0-rc.2.26504.105 | 820230c386db5b0106c5430b3e5aee92a7826bf5 |

Microsoft release metadata for [10.0](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json) and [11.0](https://builds.dotnet.microsoft.com/dotnet/release-metadata/11.0/releases.json) identifies these October 13 releases. Exact App.Ref and WebAssembly.Server package versions are pinned coherently. SDK roll-forward remains disabled; RC2 evidence does not certify GA.

## Exact binary and source identity

The installed ASP.NET shared-framework `.version` files and assembly `SourceCommitUrl` identify the dotnet/dotnet VMR commits above. Each same-named runtime tag resolves to that commit. ASP.NET sources reside below `src/aspnetcore/` in the VMR. Assembly `OriginalRepoCommitHash` instead identifies component repository commits `0ef4bbfa3291b306a21e5001cb0491277bdd35bc` and `4a70870818bf5662f21b0c28f9a8c267dccb77f0`; those omit shipped patches. They are not the adapter baseline.

The monitor discovers stable runtime tags from the dotnet/aspnetcore release catalog and resolves that exact tag in dotnet/dotnet. The VMR release catalog contains SDK tags: for example v10.0.402 resolves to a different commit and cannot substitute for v10.0.13. NET11 retains its explicit reviewed prerelease tag. Source downloads, inventory and source links use the VMR root; canonical watch/dependency paths remain relative to that root.

## Upstream delta assessment

Compared complete non-truncated VMR Git subtrees from the previously installed binaries to the patched binaries. Previous source commits are `e2f47b0110ed922f21a1522da67279133ce28f32` (10.0.11) and `3551975be08744f0418857c5bed8ab1545c5dd47` (11 RC1). Traverse each commit's root through `src/aspnetcore`, then recursively inventory that subtree. GitHub's capped compare response cannot establish completeness. The tables retain every watched blob: 56 NET10 files (4 changed) and 67 NET11 files (5 changed), including the newly explicit NET11 native CSRF dependency. No watched path was added or removed between these built sources.

- Both lanes: `RazorComponentEndpointInvoker` discards abandoned rendered content after navigation. The adapter now suppresses that output while retaining completed Session/TempData write-back. Completed-submit and pending-render tests pair stock and Htmxor.
- Both lanes: `EndpointHtmlRenderer.Prerendering` captures the first pending navigation, drains all queued non-streaming work, then rethrows it to the invoker. The adapter follows this ordering; paired gated rendering checks ensure a redirect cannot complete before queued work drains.
- Both lanes: `AuthorizeViewCore` cancels superseded asynchronous authorization evaluation. Rendering and authorization remain framework-owned, so installed stock supplies this patch; no copied authorization implementation or new private access is introduced.
- NET10: `Router` renders not-found content through the application's `Found` fragment. NET11 already contained this change in its previous shipped VMR, although the component-repository comparison misleadingly showed it as new. Htmxor uses the framework Router; custom Found composition coverage is not newly claimed here.
- NET11: `DefaultCsrfProtection` trusts an allowed CORS origin only when its policy also supports credentials. Paired ordinary forms and generated actions preserve stock acceptance of credential-enabled policies and rejection of public-origin policies before application effects. No security policy is weakened. This dependency is now explicitly watched only for NET11.
- NET11: `WebAssemblyComponentSerializationSettings` freezes options with `MakeReadOnly(populateMissingResolver: true)`. Naming, null omission and case-insensitive reads are unchanged. Htmxor owns options that System.Text.Json freezes on first serialization; existing paired marker tests exercise payload behavior. Trimming/native-AOT certification is not claimed.

The retained MIT license is byte-identical to `src/aspnetcore/LICENSE.txt` at both patched VMR commits (SHA256 `cfc21f5e8bd655ae997eec916138b707b1d290b83272c02a95c9f821b8c87310`). Existing approved private-member inventory and framework seams are unchanged.

## Pin and verification scope

Root SDK, CI SDK matrices, upstream workflow, paired runtime/package pins, packed-consumer SDK/runtime and assembly expectations, sample framework packages, monitor reference packs and reviewed records move together. Adapter provenance and the v1 baseline move with them. Historical research, authentic compiler fixtures and synthetic monitor releases retain their original identities.

Exact commands, test counts, deliberate assertion reversals and environment limits are recorded in the issue/PR delivery evidence. Local TestServer evidence alone does not establish browser, packed consumer, live provider or cross-platform behavior; each requires its own recorded gate. #302 overlaps RC2 work but is not automatically closed by this review. #217 client validation and #220 GA migration remain separate.

## Watched source inventory

Paths below are relative to VMR `src/aspnetcore/`.

### net10.0

| Source | Previous blob | Patched blob | Verdict |
| --- | --- | --- | --- |
| `src/Components/Authorization/src/AuthorizeRouteView.cs` | `f44afadff3eab2afe1a8e41b94844348c0c17d22` | `f44afadff3eab2afe1a8e41b94844348c0c17d22` | Unchanged |
| `src/Components/Authorization/src/AuthorizeViewCore.cs` | `e63eae4f11a02dd3ceb7ec06a40b44399cb16ca7` | `fd95c1aa3e6d6f89d8e6462eea45a9f970a9b11a` | Reviewed above |
| `src/Components/Components/src/ComponentBase.cs` | `eefe8eb7465537d36f5fc0052c2bc48915f60a6f` | `eefe8eb7465537d36f5fc0052c2bc48915f60a6f` | Unchanged |
| `src/Components/Components/src/ComponentFactory.cs` | `0618c9a4d20a549fad572970cc9f7ab8ebd15f43` | `0618c9a4d20a549fad572970cc9f7ab8ebd15f43` | Unchanged |
| `src/Components/Components/src/DynamicComponent.cs` | `974fb1452b16bc5249922f5d464a17e9fd2f4cba` | `974fb1452b16bc5249922f5d464a17e9fd2f4cba` | Unchanged |
| `src/Components/Components/src/EventCallbackFactory.cs` | `11c637a75e5dfbb557411b7f675843289c757efb` | `11c637a75e5dfbb557411b7f675843289c757efb` | Unchanged |
| `src/Components/Components/src/IComponent.cs` | `b27a816993f990ae97b05cf4b44d312a5944607f` | `b27a816993f990ae97b05cf4b44d312a5944607f` | Unchanged |
| `src/Components/Components/src/IPersistentComponentStateStore.cs` | `cd4c63ddbdf7612ae04184e7ddd2b0d4a53d9b85` | `cd4c63ddbdf7612ae04184e7ddd2b0d4a53d9b85` | Unchanged |
| `src/Components/Components/src/LayoutComponentBase.cs` | `35c650a1558423910287ed83fcc42f88c6e674f4` | `35c650a1558423910287ed83fcc42f88c6e674f4` | Unchanged |
| `src/Components/Components/src/LayoutView.cs` | `7e198bda8e0fb87cdb8413b83e2d432ba95de97f` | `7e198bda8e0fb87cdb8413b83e2d432ba95de97f` | Unchanged |
| `src/Components/Components/src/NavigationException.cs` | `c30bf3b59e1c851f206d8ab2ce55adf616ea368f` | `c30bf3b59e1c851f206d8ab2ce55adf616ea368f` | Unchanged |
| `src/Components/Components/src/NavigationManager.cs` | `fecbfaca6c28fd5b43ea793f5fcae0a28e67a2e1` | `fecbfaca6c28fd5b43ea793f5fcae0a28e67a2e1` | Unchanged |
| `src/Components/Components/src/RenderTree/RenderTreeDiffBuilder.cs` | `e61fb6d2d932ed0b669e85778d62358e4732ee51` | `e61fb6d2d932ed0b669e85778d62358e4732ee51` | Unchanged |
| `src/Components/Components/src/RenderTree/Renderer.cs` | `ad0864443da41f183afee05c29dc69c003c62477` | `ad0864443da41f183afee05c29dc69c003c62477` | Unchanged |
| `src/Components/Components/src/Rendering/ComponentState.cs` | `d1b42a80b1536ed130848a9906eea2386db75367` | `d1b42a80b1536ed130848a9906eea2386db75367` | Unchanged |
| `src/Components/Components/src/Rendering/RenderTreeBuilder.cs` | `9654f83720efb3af2275170f3399a8262e250d08` | `9654f83720efb3af2275170f3399a8262e250d08` | Unchanged |
| `src/Components/Components/src/RouteView.cs` | `36560a2aa3427f05ff27cc73ac1dbe4dadc6bf8d` | `36560a2aa3427f05ff27cc73ac1dbe4dadc6bf8d` | Unchanged |
| `src/Components/Components/src/Routing/IHostEnvironmentNavigationManager.cs` | `c1488a9b7bc3f79b32cc5c35a24cdb9a8450ead3` | `c1488a9b7bc3f79b32cc5c35a24cdb9a8450ead3` | Unchanged |
| `src/Components/Components/src/Routing/IRoutingStateProvider.cs` | `ba0ec86d48eb68628d68cd3047d3abab9316a52f` | `ba0ec86d48eb68628d68cd3047d3abab9316a52f` | Unchanged |
| `src/Components/Components/src/Routing/Router.cs` | `625936e9c975cd40c71c656e491ffb941c9f1654` | `e940c56ccef87615484a73f5c1081f64f8bd8b0f` | Reviewed above |
| `src/Components/Endpoints/src/Builder/ConfiguredRenderModesMetadata.cs` | `65c99a924843244e59b1d7a74bfdcb6b33b66cb1` | `65c99a924843244e59b1d7a74bfdcb6b33b66cb1` | Unchanged |
| `src/Components/Endpoints/src/Builder/RazorComponentEndpointFactory.cs` | `bf16eec3cf9db718f6959ea06d8da6e0f0a69c84` | `bf16eec3cf9db718f6959ea06d8da6e0f0a69c84` | Unchanged |
| `src/Components/Endpoints/src/Builder/ResourceCollectionUrlMetadata.cs` | `1099f240f4da4b19406618f7fd75f648ceab459e` | `1099f240f4da4b19406618f7fd75f648ceab459e` | Unchanged |
| `src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceCollectionExtensions.cs` | `dc365194fcbe9f598bba80ff6a3f5bc09a995420` | `dc365194fcbe9f598bba80ff6a3f5bc09a995420` | Unchanged |
| `src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceOptions.cs` | `de13d40cc46ba1f689c8e58f52f1bb3c28636ead` | `de13d40cc46ba1f689c8e58f52f1bb3c28636ead` | Unchanged |
| `src/Components/Endpoints/src/DependencyInjection/WebAssemblyComponentSerializer.cs` | `2934ea4ccbcf1416d7100266c7a21ee81bfd0217` | `2934ea4ccbcf1416d7100266c7a21ee81bfd0217` | Unchanged |
| `src/Components/Endpoints/src/DependencyInjection/WebAssemblySettingsEmitter.cs` | `87ae37d74f7f644d5e0ce4fdf7a6bd87cde3c17f` | `87ae37d74f7f644d5e0ce4fdf7a6bd87cde3c17f` | Unchanged |
| `src/Components/Endpoints/src/Discovery/IRazorComponentApplication.cs` | `16d2b0a1fa030dd8d5a0202802b0a46a00f582b9` | `16d2b0a1fa030dd8d5a0202802b0a46a00f582b9` | Unchanged |
| `src/Components/Endpoints/src/Discovery/PageCollectionBuilder.cs` | `37e2d394ee45aef4188c3d169e9bcd1dd69498df` | `37e2d394ee45aef4188c3d169e9bcd1dd69498df` | Unchanged |
| `src/Components/Endpoints/src/FormMapping/HttpContextFormDataProvider.cs` | `812e1a10a64d156f6094b0cd92986edb9f5cfca7` | `812e1a10a64d156f6094b0cd92986edb9f5cfca7` | Unchanged |
| `src/Components/Endpoints/src/Forms/EndpointAntiforgeryStateProvider.cs` | `8a0a00df13d104d5ebd231c5509c993c2124d24c` | `8a0a00df13d104d5ebd231c5509c993c2124d24c` | Unchanged |
| `src/Components/Endpoints/src/IRazorComponentEndpointInvoker.cs` | `22a3b61b44d0b95674f474c5b70d486af57bbda5` | `22a3b61b44d0b95674f474c5b70d486af57bbda5` | Unchanged |
| `src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs` | `22119d0522c98b9a219e23a1b5f6dad2ec52fbd0` | `a02985c483994752d5d21f907dbc2a894ed78c11` | Reviewed above |
| `src/Components/Endpoints/src/Rendering/EndpointComponentState.cs` | `6c03d0585f2f6f4bac729877f50e9b8a2e9b548d` | `6c03d0585f2f6f4bac729877f50e9b8a2e9b548d` | Unchanged |
| `src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.EventDispatch.cs` | `a06d3e66d6223996dfd60efe4bb9b0d7c54c6b3b` | `a06d3e66d6223996dfd60efe4bb9b0d7c54c6b3b` | Unchanged |
| `src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Prerendering.cs` | `ec8407b17a967ac59b85abf00f695f5d116610d5` | `e6330db699e69fae9f3cc5ce8dfe365861982412` | Reviewed above |
| `src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.PrerenderingState.cs` | `161967076ec531ad6028e1211a4649055512ab21` | `161967076ec531ad6028e1211a4649055512ab21` | Unchanged |
| `src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs` | `50728a8c32714336c5b853aa029784f4c5626c55` | `50728a8c32714336c5b853aa029784f4c5626c55` | Unchanged |
| `src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs` | `4d95f68c0fc5ca39c1e166940fadb69436e5c119` | `4d95f68c0fc5ca39c1e166940fadb69436e5c119` | Unchanged |
| `src/Components/Endpoints/src/Rendering/SSRRenderModeBoundary.cs` | `1febaa8f4af59cc36539bf95ed65fe63f627d86a` | `1febaa8f4af59cc36539bf95ed65fe63f627d86a` | Unchanged |
| `src/Components/Shared/src/ResourceCollectionProvider.cs` | `2084abdb3e53c6d4b69730bdeed75c65e7621cd1` | `2084abdb3e53c6d4b69730bdeed75c65e7621cd1` | Unchanged |
| `src/Components/Web/src/Forms/AntiforgeryStateProvider.cs` | `0390b9a55ebf8ca463a2000aa164e405883d9f03` | `0390b9a55ebf8ca463a2000aa164e405883d9f03` | Unchanged |
| `src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.HtmlWriting.cs` | `c48b1d798b15dec26759c1ac8509bb0abaa9a9c4` | `c48b1d798b15dec26759c1ac8509bb0abaa9a9c4` | Unchanged |
| `src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.cs` | `a77b3e98c4eccc734581648c745b87e44eba91c5` | `a77b3e98c4eccc734581648c745b87e44eba91c5` | Unchanged |
| `src/Html.Abstractions/src/IHtmlAsyncContent.cs` | `4496273157b10db56fafbb2319334c1d9ac50c3e` | `4496273157b10db56fafbb2319334c1d9ac50c3e` | Unchanged |
| `src/Http/Routing/src/EndpointDataSource.cs` | `83b65d054a52603a9ad13a29837e847069374036` | `83b65d054a52603a9ad13a29837e847069374036` | Unchanged |
| `src/Http/Routing/src/Matching/IEndpointSelectorPolicy.cs` | `775a042f4d024d7573fe403717995f0c84cf73e2` | `775a042f4d024d7573fe403717995f0c84cf73e2` | Unchanged |
| `src/Http/Routing/src/Matching/MatcherPolicy.cs` | `ea9bd79818e3aef111b4dec8efb9ed53fae1e891` | `ea9bd79818e3aef111b4dec8efb9ed53fae1e891` | Unchanged |
| `src/Shared/Components/ComponentMarker.cs` | `8bf072db25afda6ccd76abd536c90b6b5401d449` | `8bf072db25afda6ccd76abd536c90b6b5401d449` | Unchanged |
| `src/Shared/Components/ComponentParameter.cs` | `5990e4db899158caaf8d87ff6d6ead0351856682` | `5990e4db899158caaf8d87ff6d6ead0351856682` | Unchanged |
| `src/Shared/Components/PrerenderComponentApplicationStore.cs` | `761f9c4521fc1540bd2793359d4b8b7614aca65d` | `761f9c4521fc1540bd2793359d4b8b7614aca65d` | Unchanged |
| `src/Shared/Components/ProtectedPrerenderComponentApplicationStore.cs` | `8e3f16969ac1a9e3ddafc98d1fe9a5b2dd74311e` | `8e3f16969ac1a9e3ddafc98d1fe9a5b2dd74311e` | Unchanged |
| `src/Shared/Components/ServerComponent.cs` | `88eb06e92bc721b35c608a51de26018161e3921f` | `88eb06e92bc721b35c608a51de26018161e3921f` | Unchanged |
| `src/Shared/Components/ServerComponentSerializationSettings.cs` | `b584642fe1bb89a56e15476a8a07b5948c41da56` | `b584642fe1bb89a56e15476a8a07b5948c41da56` | Unchanged |
| `src/Shared/Components/ServerComponentSerializer.cs` | `a6bbdd347e6af6ccbe2099d7b48ed4df3836b621` | `a6bbdd347e6af6ccbe2099d7b48ed4df3836b621` | Unchanged |
| `src/Shared/Components/WebAssemblyComponentSerializationSettings.cs` | `4a2f1aed3de03af1ff752cd0c66183a8e2c00e73` | `4a2f1aed3de03af1ff752cd0c66183a8e2c00e73` | Unchanged |

### net11.0

| Source | Previous blob | Patched blob | Verdict |
| --- | --- | --- | --- |
| `src/Components/Authorization/src/AuthorizeRouteView.cs` | `926c5929c4ed7ed2aef49332171b435cf864f793` | `926c5929c4ed7ed2aef49332171b435cf864f793` | Unchanged |
| `src/Components/Authorization/src/AuthorizeViewCore.cs` | `1331200c888d8f14f35900a2574b30bf8cc079b4` | `bee6519be5993b219044c8a19dd5a3abb7e149ff` | Reviewed above |
| `src/Components/Components/src/ComponentBase.cs` | `958a68f15046dab4027a09a1ba2402c8dc632124` | `958a68f15046dab4027a09a1ba2402c8dc632124` | Unchanged |
| `src/Components/Components/src/ComponentFactory.cs` | `0b3d84796196be91c54cf5deeb9aad52707f8201` | `0b3d84796196be91c54cf5deeb9aad52707f8201` | Unchanged |
| `src/Components/Components/src/DynamicComponent.cs` | `974fb1452b16bc5249922f5d464a17e9fd2f4cba` | `974fb1452b16bc5249922f5d464a17e9fd2f4cba` | Unchanged |
| `src/Components/Components/src/EventCallbackFactory.cs` | `11c637a75e5dfbb557411b7f675843289c757efb` | `11c637a75e5dfbb557411b7f675843289c757efb` | Unchanged |
| `src/Components/Components/src/IComponent.cs` | `b27a816993f990ae97b05cf4b44d312a5944607f` | `b27a816993f990ae97b05cf4b44d312a5944607f` | Unchanged |
| `src/Components/Components/src/IPersistentComponentStateStore.cs` | `cd4c63ddbdf7612ae04184e7ddd2b0d4a53d9b85` | `cd4c63ddbdf7612ae04184e7ddd2b0d4a53d9b85` | Unchanged |
| `src/Components/Components/src/LayoutComponentBase.cs` | `35c650a1558423910287ed83fcc42f88c6e674f4` | `35c650a1558423910287ed83fcc42f88c6e674f4` | Unchanged |
| `src/Components/Components/src/LayoutView.cs` | `7e198bda8e0fb87cdb8413b83e2d432ba95de97f` | `7e198bda8e0fb87cdb8413b83e2d432ba95de97f` | Unchanged |
| `src/Components/Components/src/NavigationException.cs` | `c30bf3b59e1c851f206d8ab2ce55adf616ea368f` | `c30bf3b59e1c851f206d8ab2ce55adf616ea368f` | Unchanged |
| `src/Components/Components/src/NavigationManager.cs` | `260c38496c15dcb5463129cd123445f4b4d68c5f` | `260c38496c15dcb5463129cd123445f4b4d68c5f` | Unchanged |
| `src/Components/Components/src/RenderTree/RenderTreeDiffBuilder.cs` | `320026064ef37e5e87fd866dec2512b075418e6a` | `320026064ef37e5e87fd866dec2512b075418e6a` | Unchanged |
| `src/Components/Components/src/RenderTree/Renderer.cs` | `015879de2a8d772d4dd459ab73a830f2447c54a0` | `015879de2a8d772d4dd459ab73a830f2447c54a0` | Unchanged |
| `src/Components/Components/src/Rendering/ComponentState.cs` | `f94a52fc5adc209c9b4a43373780a6f176ec02b5` | `f94a52fc5adc209c9b4a43373780a6f176ec02b5` | Unchanged |
| `src/Components/Components/src/Rendering/RenderTreeBuilder.cs` | `0b7b81b2dc84c6875febe05cf32c6cc57d059a99` | `0b7b81b2dc84c6875febe05cf32c6cc57d059a99` | Unchanged |
| `src/Components/Components/src/RouteView.cs` | `ab40bf32f14b2316c39082ecd0109848ffba3806` | `ab40bf32f14b2316c39082ecd0109848ffba3806` | Unchanged |
| `src/Components/Components/src/Routing/IHostEnvironmentNavigationManager.cs` | `c1488a9b7bc3f79b32cc5c35a24cdb9a8450ead3` | `c1488a9b7bc3f79b32cc5c35a24cdb9a8450ead3` | Unchanged |
| `src/Components/Components/src/Routing/IRoutingStateProvider.cs` | `ba0ec86d48eb68628d68cd3047d3abab9316a52f` | `ba0ec86d48eb68628d68cd3047d3abab9316a52f` | Unchanged |
| `src/Components/Components/src/Routing/Router.cs` | `2e315d3c217f6f301303854de997e186d113cad0` | `2e315d3c217f6f301303854de997e186d113cad0` | Unchanged |
| `src/Components/Endpoints/src/Builder/ConfiguredRenderModesMetadata.cs` | `65c99a924843244e59b1d7a74bfdcb6b33b66cb1` | `65c99a924843244e59b1d7a74bfdcb6b33b66cb1` | Unchanged |
| `src/Components/Endpoints/src/Builder/RazorComponentEndpointFactory.cs` | `c714400b2380757f761a6b98094f0aaaf74a6c35` | `c714400b2380757f761a6b98094f0aaaf74a6c35` | Unchanged |
| `src/Components/Endpoints/src/Builder/ResourceCollectionUrlMetadata.cs` | `1099f240f4da4b19406618f7fd75f648ceab459e` | `1099f240f4da4b19406618f7fd75f648ceab459e` | Unchanged |
| `src/Components/Endpoints/src/CacheView/CacheView.cs` | `ec89fe3f9d121aa5ac485c5134d6330e86f75606` | `ec89fe3f9d121aa5ac485c5134d6330e86f75606` | Unchanged |
| `src/Components/Endpoints/src/CacheView/CacheViewRenderState.cs` | `94a98ae5ce048a8cbd68b0ee2b8da06b0b74bf7f` | `94a98ae5ce048a8cbd68b0ee2b8da06b0b74bf7f` | Unchanged |
| `src/Components/Endpoints/src/CacheView/CacheViewService.cs` | `60ba110c8b35b3cf6b0d25a7ab36b6f1c2147f8e` | `60ba110c8b35b3cf6b0d25a7ab36b6f1c2147f8e` | Unchanged |
| `src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceCollectionExtensions.cs` | `171829f951fb0267476eddfd5bdad3cc1a4d1faa` | `171829f951fb0267476eddfd5bdad3cc1a4d1faa` | Unchanged |
| `src/Components/Endpoints/src/DependencyInjection/RazorComponentsServiceOptions.cs` | `5c26d065eb6eb24103d9f2642e7f7e5b1887b6cc` | `5c26d065eb6eb24103d9f2642e7f7e5b1887b6cc` | Unchanged |
| `src/Components/Endpoints/src/DependencyInjection/TempDataService.cs` | `6e70b7f281ab4e62130a8e26807c2568f47a8a2d` | `6e70b7f281ab4e62130a8e26807c2568f47a8a2d` | Unchanged |
| `src/Components/Endpoints/src/DependencyInjection/WebAssemblyComponentSerializer.cs` | `2934ea4ccbcf1416d7100266c7a21ee81bfd0217` | `2934ea4ccbcf1416d7100266c7a21ee81bfd0217` | Unchanged |
| `src/Components/Endpoints/src/Discovery/IRazorComponentApplication.cs` | `16d2b0a1fa030dd8d5a0202802b0a46a00f582b9` | `16d2b0a1fa030dd8d5a0202802b0a46a00f582b9` | Unchanged |
| `src/Components/Endpoints/src/Discovery/PageCollectionBuilder.cs` | `37e2d394ee45aef4188c3d169e9bcd1dd69498df` | `37e2d394ee45aef4188c3d169e9bcd1dd69498df` | Unchanged |
| `src/Components/Endpoints/src/FormMapping/HttpContextFormDataProvider.cs` | `812e1a10a64d156f6094b0cd92986edb9f5cfca7` | `812e1a10a64d156f6094b0cd92986edb9f5cfca7` | Unchanged |
| `src/Components/Endpoints/src/Forms/EndpointAntiforgeryStateProvider.cs` | `ddcbc00f6fb2f01b8630b55be99eea9da72a6524` | `ddcbc00f6fb2f01b8630b55be99eea9da72a6524` | Unchanged |
| `src/Components/Endpoints/src/IRazorComponentEndpointInvoker.cs` | `22a3b61b44d0b95674f474c5b70d486af57bbda5` | `22a3b61b44d0b95674f474c5b70d486af57bbda5` | Unchanged |
| `src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs` | `58d8a4e3090db8f883e60d07265c12bd835452d3` | `8c6aa220012450a11c36eccf7067a37bac6b4e46` | Reviewed above |
| `src/Components/Endpoints/src/Rendering/CacheViewTextWriter.cs` | `c4486da7469749e39426d29842e1c71afcae88e7` | `c4486da7469749e39426d29842e1c71afcae88e7` | Unchanged |
| `src/Components/Endpoints/src/Rendering/EndpointComponentState.cs` | `41d8ced49c88fb6618112d4f86d0ddb2c1f728d6` | `41d8ced49c88fb6618112d4f86d0ddb2c1f728d6` | Unchanged |
| `src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.EventDispatch.cs` | `a06d3e66d6223996dfd60efe4bb9b0d7c54c6b3b` | `a06d3e66d6223996dfd60efe4bb9b0d7c54c6b3b` | Unchanged |
| `src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Prerendering.cs` | `0276cdfd15155c441eeafc3e1946c85af5d39874` | `478e6e893c387ce4aa958c35974f945b2be9924f` | Reviewed above |
| `src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.PrerenderingState.cs` | `161967076ec531ad6028e1211a4649055512ab21` | `161967076ec531ad6028e1211a4649055512ab21` | Unchanged |
| `src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs` | `20e0f961bbdeca27bba486e215c360848f3a7af9` | `20e0f961bbdeca27bba486e215c360848f3a7af9` | Unchanged |
| `src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs` | `f452532a800eb01af537369b369f7d6e1a990f3d` | `f452532a800eb01af537369b369f7d6e1a990f3d` | Unchanged |
| `src/Components/Endpoints/src/Rendering/SSRRenderModeBoundary.cs` | `9170b83f3284324a113d97dd22b6087e913f943d` | `9170b83f3284324a113d97dd22b6087e913f943d` | Unchanged |
| `src/Components/Endpoints/src/SessionCascadingValueSupplier.cs` | `c2775fa21579dbe0ccf76f1372d25667a9d236b3` | `c2775fa21579dbe0ccf76f1372d25667a9d236b3` | Unchanged |
| `src/Components/Endpoints/src/TempData/TempDataCascadingValueSupplier.cs` | `d3f66faaab83d60e58010eeb11cf3aa08e8ac0db` | `d3f66faaab83d60e58010eeb11cf3aa08e8ac0db` | Unchanged |
| `src/Components/Endpoints/src/TempData/TempDataProviderServiceCollectionExtensions.cs` | `ca1be3acd280c2fe939a168b310b7283367f25bd` | `ca1be3acd280c2fe939a168b310b7283367f25bd` | Unchanged |
| `src/Components/Shared/src/ComponentKeyHelper.cs` | `f42e8217d9e45d5015c823de7727304b1e7744f5` | `f42e8217d9e45d5015c823de7727304b1e7744f5` | Unchanged |
| `src/Components/Shared/src/RenderFragmentCapture.cs` | `4d25c5574dde6bb0552c0d6d617e129d40b0a9af` | `4d25c5574dde6bb0552c0d6d617e129d40b0a9af` | Unchanged |
| `src/Components/Shared/src/ResourceCollectionProvider.cs` | `4398e5adc759440967f7b32950593f3b48750567` | `4398e5adc759440967f7b32950593f3b48750567` | Unchanged |
| `src/Components/Web/src/Forms/AntiforgeryStateProvider.cs` | `0390b9a55ebf8ca463a2000aa164e405883d9f03` | `0390b9a55ebf8ca463a2000aa164e405883d9f03` | Unchanged |
| `src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.HtmlWriting.cs` | `c48b1d798b15dec26759c1ac8509bb0abaa9a9c4` | `c48b1d798b15dec26759c1ac8509bb0abaa9a9c4` | Unchanged |
| `src/Components/Web/src/HtmlRendering/StaticHtmlRenderer.cs` | `a77b3e98c4eccc734581648c745b87e44eba91c5` | `a77b3e98c4eccc734581648c745b87e44eba91c5` | Unchanged |
| `src/DefaultBuilder/src/Internal/DefaultCsrfProtection.cs` | `d4ba8ac51d712278b63a62b4a42d0bb14b65fdb4` | `ac178cc3397bc226dad6840081891bf2dbca6418` | Reviewed above |
| `src/Html.Abstractions/src/IHtmlAsyncContent.cs` | `4496273157b10db56fafbb2319334c1d9ac50c3e` | `4496273157b10db56fafbb2319334c1d9ac50c3e` | Unchanged |
| `src/Http/Routing/src/EndpointDataSource.cs` | `83b65d054a52603a9ad13a29837e847069374036` | `83b65d054a52603a9ad13a29837e847069374036` | Unchanged |
| `src/Http/Routing/src/Matching/IEndpointSelectorPolicy.cs` | `09756af7528bada3710a5292f9d1198dbb0bfa26` | `09756af7528bada3710a5292f9d1198dbb0bfa26` | Unchanged |
| `src/Http/Routing/src/Matching/MatcherPolicy.cs` | `ea9bd79818e3aef111b4dec8efb9ed53fae1e891` | `ea9bd79818e3aef111b4dec8efb9ed53fae1e891` | Unchanged |
| `src/Shared/Components/ComponentMarker.cs` | `8bf072db25afda6ccd76abd536c90b6b5401d449` | `8bf072db25afda6ccd76abd536c90b6b5401d449` | Unchanged |
| `src/Shared/Components/ComponentParameter.cs` | `5990e4db899158caaf8d87ff6d6ead0351856682` | `5990e4db899158caaf8d87ff6d6ead0351856682` | Unchanged |
| `src/Shared/Components/PrerenderComponentApplicationStore.cs` | `257e0f94287b565197d6c2a0ad0ccaf42edd17ac` | `257e0f94287b565197d6c2a0ad0ccaf42edd17ac` | Unchanged |
| `src/Shared/Components/ProtectedPrerenderComponentApplicationStore.cs` | `8e3f16969ac1a9e3ddafc98d1fe9a5b2dd74311e` | `8e3f16969ac1a9e3ddafc98d1fe9a5b2dd74311e` | Unchanged |
| `src/Shared/Components/ServerComponent.cs` | `88eb06e92bc721b35c608a51de26018161e3921f` | `88eb06e92bc721b35c608a51de26018161e3921f` | Unchanged |
| `src/Shared/Components/ServerComponentSerializationSettings.cs` | `b7bc8b18952dfd0214ae0d7e02d8249f33ede90a` | `b7bc8b18952dfd0214ae0d7e02d8249f33ede90a` | Unchanged |
| `src/Shared/Components/ServerComponentSerializer.cs` | `fab5eb1cc23627ae045981e1d88df9adafac7c72` | `fab5eb1cc23627ae045981e1d88df9adafac7c72` | Unchanged |
| `src/Shared/Components/WebAssemblyComponentSerializationSettings.cs` | `4a2f1aed3de03af1ff752cd0c66183a8e2c00e73` | `9d01a6528f2477f1bb7118f180593fadac429aef` | Reviewed above |
| `src/Shared/MiddlewareInvokedKeys.cs` | `dfb9c17d9c1cd0f04689a97e46ed23da070bd081` | `dfb9c17d9c1cd0f04689a97e46ed23da070bd081` | Unchanged |
