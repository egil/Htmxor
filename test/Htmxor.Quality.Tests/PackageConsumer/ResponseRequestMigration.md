# Response and request consumer coverage (#359)

When the suite runs, Htmxor retains the response/request behavior formerly asserted by packed consumers on both target frameworks, while package contents, public metadata, and package-only compilation remain tested against the real package.

The comparison base is `c71dbf381394e22ede72096ee44abfb2fc22e848`. The original performance baseline remains `9b920c891f747d0bfd940fa1449bdefd6e47d7f3`. This classification covers G01/G02/G10/G11/G12/G19–G23. Routing, actions, and forms owned by #362 remain in the default consumer; #363 owns package consolidation, policy, and the final timing target.

## Packed entry points

Each entry point below was mixed: its runtime assertions were B (behavior), while its package/archive, selected compile/runtime asset, supported-consumer build, and metadata assertions were P (packaging). The P assertions remain. A small generated-route discovery probe remains where that is the package claim; the old response-body assertion does not remain in that probe.

| Packed class / original method | Behavior carrier in `Htmxor.AspNetCore10.Tests` | Retained package evidence |
| --- | --- | --- |
| `PackedSelectionConsumerTests.Package_only_application_serializes_selected_fragment_bodies` | Both `Issue168SelectionTests` methods, all 12 rows | Package, consumer boundary, TFM assets, one component discovery probe |
| `PackedNestedSelectionConsumerTests.Package_only_application_resolves_nested_and_invalid_selections_before_output` | All three `Issue169SelectionTests` methods, 18 rows | Same package checks and one component discovery probe |
| `PackedStatusCodeConsumerTests.Packed_status_and_guard_contract_preserves_ordinary_and_htmx_request_behavior` | `Issue287GuardTests` and `Issue287StatusCodeTests`, seven rows | Same package checks and one form-component discovery probe |
| `PackedStatusCodeBodyConsumerTests.Packed_status_code_body_and_redirect_contract_uses_real_Kestrel` | All ten `Issue291StatusCodeTests` methods, 74 rows | Same package checks and one Kestrel component discovery probe |
| `PackedContextConstructionConsumerTests.Packed_wrappers_hide_construction_and_generated_post_preserves_request_identity` | `Issue299ContextTests`, three runtime tests, plus five callback identity checks in `Issue299Page` | Four constructor metadata rows, hidden factory/return-type metadata; separate inaccessible-constructor build and its supported compilation control |
| `PackedResponseAuthoringConsumerTests.Packed_integer_status_preserves_hosted_body_contract` | `Issue293StatusCodeTests`, all 28 hosted cases | Public integer signature and delegate binding, package/consumer/TFM asset checks |
| `PackedPackageConsumerTests.Package_only_application_discovers_explicit_CSharp_routes_and_supported_actions` | Shared-template B methods mapped below | Five surface/registration tests and all retained #362 behavior; 94 → 21 nested cases |
| `PackedPackageConsumerTests.Package_only_application_preserves_actionless_unsafe_route_antiforgery` | The same shared-template B migration, performed once | Actionless replay and all its additional assertions; 97 → 24 nested cases |

The existing dual-TFM `HtmxResponseAuthoringTests` are not an equivalent of #293's 28 Kestrel cases: their integer API comparisons use direct contexts and internal suppression observations, without transport write enforcement or shutdown log drainage. The hosted cases therefore have an explicit carrier rather than being deleted as duplicates.

## Shared-template method ledger

All source names below refer to `PackageConsumerTests.cs.template` at the comparison base. Destination files are partials of `Issue359ResponseTests`. Every listed input and assertion is retained unless the dependency-control disposition below explicitly says otherwise. Loops are expanded into independently discovered cases so an earlier failure cannot hide another operation or input.

| Original method | Verdict / destination | Input preservation |
| --- | --- | --- |
| `Package_removes_legacy_status_286_contract_but_keeps_general_status` | Mixed: removed-type/method surface stays packed; B → `.Status.cs` | Same response instance and exact status 286, both TFMs |
| `Swap_and_selection_values_are_validated_before_the_marker_without_response_mutation` | B → `.SwapSelection.cs` | Seven values × three operations = 21 rows; status, count, and complete header projection retained |
| `Swap_and_selection_operations_preserve_exact_open_values_and_coexist` | B → `.SwapSelection.cs` | Exact three open values, overwrites, identity, application header and status |
| `Navigation_destinations_are_validated_before_the_marker_guard_without_mutation` | B → `.Navigation.cs` | Seven destinations × four operations = 28 rows |
| `Navigation_destinations_enforce_history_scheme_and_origin_rules` | B → `.Navigation.cs` | 14 rejected combinations, four case-distinct relative references, one external redirect |
| `Navigation_operations_preserve_Uri_text_and_use_one_last_call_wins_header` | B → `.Navigation.cs` | Three URI overload rows and one complete last-call chain |
| `Navigation_component_applies_one_header_and_the_documented_body_effect` | B → `.Navigation.cs` | All 11 original HTTP rows, including URI and empty/navigation ordering |
| `Swap_and_selection_component_preserves_status_body_and_other_headers` | B → `.SwapSelection.cs` | All three HTTP rows |
| `Trigger_merges_one_deterministic_object_with_selected_serializer_options` | B → `.Trigger.cs` | Full expected JSON, options, ordering, case, null, escaping, identity and response state |
| `Trigger_failures_leave_manual_response_state_unchanged` | B → `.Trigger.cs` | Nine named invalid-name rows, two serializer rows, one marker-rejection row |
| `Trigger_preserves_status_and_body_decisions_through_TestServer` | B → `.Trigger.cs` | All three original loop cases become theory rows |
| `Package_renders_native_htmx4_markup_and_keeps_server_response_operations` | B → `.Markup.cs` | All response operations; three literal controls paired with new real component HTTP assertions |
| `Package_exposes_current_url_naming_and_conservative_core_request_matrix` | Mixed: reflection surface stays packed; B → `.Request.cs` | Original positive/normalized/comma cases and every invalid value become 43 cases |
| `Invalid_marker_cannot_reach_an_htmx_only_route` | B → `.Request.cs` | All eight marker rows; matching URL/target hints prevent unrelated route filters masking marker failure |
| `Invalid_marker_keeps_the_stock_page_and_rejects_response_operations_without_mutation` | B → `.Request.cs` | All eight markers, all 16 operations and every core response header |
| `One_normalized_true_marker_retains_direct_representation` | B → `.Request.cs` | Original normalized marker and representation checks |
| `Invalid_core_request_headers_fail_closed_through_TestServer` | B → `.Request.cs` | All 27 wire cases, parsed fields and actual shell/representation checks |

The complete old source-line/assertion → carrier/inversion ledger is retained with the exact-SHA verification receipt. It distinguishes nested cardinality checks from value comparisons and records actual behavioral failures, not compilation or discovery failures.

## Boundaries and dependency controls

The destination project explicitly includes its C# and Razor sources on net11 and uses the real generator analyzer. Pure response/request operations use fresh direct contexts. Rendering and dispatch use real component endpoints. #291 and #293 retain real Kestrel, redirect following disabled, and `StopAsync` drainage before log absence is asserted. Hosts are shared where state permits; selection failure observations, form callback counters, and stopped Kestrel hosts retain request isolation.

Root approved preserving dependency-only controls green when they anchor a paired Htmxor assertion. Each paired owned outcome has separate production-inversion evidence. The dependency pins are ASP.NET Core/.NET `10.0.13` and `11.0.0-rc.2.26504.105`.

| Original control | Disposition / paired owned outcome |
| --- | --- |
| Template line 471, `Uri.OriginalString != Uri.ToString()` | Retained for the three URI operations. A production inversion using normalized `AbsoluteUri` produces valid headers with wrong text, independently failing the exact-value assertions. |
| Template lines 732, 736, 740, framework `HtmlRenderer` literal checks | All three retained. Each anchors its matching real Htmxor HTTP literal assertion, which fails when Htmxor omits completed output. This makes no browser-execution claim. |
| #291 lines 171, 177, 213, 218, stock status/exception controls with Htmxor disabled | All four retained. Candidate raw-status preservation and expected exception behavior have separate inversions. |
| #291 lines 25, 173, 174, 193, 215, 216, forbidden-body wire checks | Retained Kestrel 204/304 controls, paired with owned exact status and suppression/error-log behavior. Kestrel's empty wire body alone does not prove Htmxor avoided an illegal write. |
| #293 line 51, forbidden-body branch | Final 204/304 cells retain the same Kestrel control and owned log/status anchors. Allowed-status cells with independent suppression remain owned behavior and have suppression inversions. |
| #287 guard line 48, direct `DefaultHttpContext.Response.HasStarted == false` | Dropped explicitly: the default feature cannot observe a transport start and is unnecessary as a parity anchor. Real TestServer selection-start and Kestrel boundaries remain. No owned state/output assertion was dropped. |

The header-constant reflection guard at template line 989 is a surface check: exact shipped constant string types remain covered by the real-package public-surface allowlist on both TFMs. The in-process copy only supports enumerating the actual names for the owned no-header-emission assertions. Factory/constructor metadata stays packed. The #299 package callback retains compilation of every supported context/request/response access; its five runtime identity assertions execute in-process.

This slice does not consolidate consumers, change runner/CI/browser/policy code, or claim the final under-two-minute target. Before/after timings for the final consolidated package portfolio belong to #363.
