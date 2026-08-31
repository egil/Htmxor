# Code-metrics baseline ratchets

Htmxor enables CA1502, CA1505, and CA1509 as errors in every project. Each
project declares one profile, and the build rejects a missing, unknown,
duplicate, or overridden profile. The profile files under
`eng/quality/code-metrics` are the analyzer's executable policy.

The legacy profiles record the measured starting point at commit
`457dd3d11d920771c46407f2531800fe813884e4`. Htmxor had no code-metrics gate
before this baseline. A complexity ceiling can move down and a maintainability
floor can move up after verified improvement. Ordinary work must not move either
in the weaker direction.

| Profile | Projects | Complexity ceiling | Audited maximum | Method/type MI minima | Findings beyond the 10/5 direction |
| --- | --- | ---: | ---: | ---: | ---: |
| `legacy-production-baseline` | `src/Htmxor` | 22 | 22 | 39 / 43 | 9 above 10 |
| `legacy-test-app-baseline` | `test/Htmxor.TestApp` | 3 | 3 | 65 / 56 | 0 above 10 |
| `legacy-tests-baseline` | `test/Htmxor.Tests` | 10 | 10 | 42 / 51 | 1 above 5 |
| `legacy-samples-baseline` | all three sample projects | 7 | 7 | see below | 0 above 10 |
| `production` | all newly added production/tooling project paths | 10 | new code | 20 minimum | not applicable |
| `tests` | all newly added focused-test project paths | 5 | new code | 20 minimum | not applicable |

All audited legacy projects had zero CA1505 findings at the method and type
minimum of 20. The sample complexity maximum and method/type maintainability
minima were 7 and 43/35 for `BlazingPizza`, 2 and 60/51 for
`MinimalHtmxorApp`, and 2 and 47/46 for `HtmxorExamples`. CA1509 was also
exercised with an invalid configuration and failed the build as intended.

The nine production methods above the direction-of-travel ceiling of 10 were:

- `HtmxRouteAttribute.Equals`, complexity 16.
- `HtmxTriggerSpecification.ToString`, complexity 15.
- `EndpointMetadata.IsValidFor`, complexity 14.
- `HtmxorComponentEndpointInvoker.RenderComponentCore`, complexity 18.
- `HtmxorComponentEndpointInvoker.ValidateRequestAsync`, complexity 15.
- `HtmxorComponentRequestHost.GetRouteParameters`, complexity 16.
- `HtmxorRenderer.RenderCore`, complexity 11.
- `HtmxorRenderer.RenderElement`, complexity 12.
- `HtmxorRenderer.RenderAttributes`, complexity 22.

The second bounded #151 slice removes `HtmxTriggerSpecification.ToString`.
The nine-method list remains the historical adoption measurement; current source
therefore contains eight of those methods, while the fixed project ceiling is
unchanged.

The one existing test above the focused-test ceiling of 5 was
`AlbaScenarioExtensions.WithHxHeaders`, complexity 10. This change records that
debt; it does not refactor product or test behavior merely to establish the
gate.

The 10 ceiling for production, applications, samples, and tooling and the 5
ceiling for focused tests remain the design direction when legacy code is
touched. The coarser baseline only prevents regression in an untouched project
class. Reviewers must still reject needless branches, mixed abstraction levels,
and unfocused scenarios below the numeric ceiling. Metrics inform that review;
they do not replace design judgment.

Every newly added v1 project path must declare `production` or `tests` from its
first commit. The four `legacy-*` profiles are fixed no-regression envelopes for
the six audited owner paths, including during an in-place rewrite. They are
never design authority, a modernization mandate, or reusable by a newly added
path. Today a central map assigns all six owners to their legacy profile. A
future map change may lock an owner to its generic role profile only when that
profile is at least as strict as the separate original baseline; changing only
the project file cannot opt in or roll back. Retirement removes the project and
solution entry while marking its historical path retired in the same reviewed
change. A live assignment for an absent project fails; a retired path returning
later is new and must use its generic role profile. The repository quality
command also locks each profile file to the adopted CA1502 and CA1505 values,
requires the project files and solution entries to match exactly, maps new
`test/` project paths to `tests`, and maps every other new project path to
`production`.
