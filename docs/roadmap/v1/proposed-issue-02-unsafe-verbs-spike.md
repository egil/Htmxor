# Proposed issue 02

Status: review draft only.

## Title

`spike: bind PUT, PATCH, and DELETE to live component instances`

## Triage

- Type: HITL for the supported verb/action-scope decision; evidence gathering and
  prototype work are AFK.
- Proposed state after publication: needs spike
- Parent: proposed stable-v1 parent issue

## What to build

Prove whether Htmxor can compile `@onput`, `@onpatch`, and `@ondelete` Razor event
declarations into method- and route-bound handlers that run on the component
instance created by the supported Blazor request lifecycle.

The developer-facing model remains Razor event syntax. Do not replace callbacks
with static methods that are Minimal API handlers placed inside a `.razor` file.
Do not discover actions by rendering a component, scanning a runtime render tree,
or trusting a client-supplied callback hash.

The spike should test how far a source generator can infer server intent from
`@on*` method groups in the page and statically reachable child components.
Literal `hx-*` attributes may be checked against that intent for consistency,
but they must not expand the server method allow-list. Dynamic handler
expressions, lambdas, generic or dynamic child composition, and multiple page
routes must receive explicit supported semantics or actionable build
diagnostics.

Evaluate the .NET 11 public cascading subscription API as a possible way for
generated code to register the actual component instance. Do not assume instance
observation also supplies a supported pre-response async dispatch or rerender
serialization hook; prove those separately.

## Acceptance criteria

- [ ] The spike confirms and records the stock component endpoint invoker's
      current POST boundary on .NET 10 and the inspected .NET 11 preview.
- [ ] At least one PUT, PATCH, or DELETE request invokes a method group on the live
      component instance after route, query, request, authentication, and normal
      component lifecycle state have been supplied.
- [ ] Authorization and antiforgery/CSRF validation finish before form/body binding
      or application callback code for every supported unsafe verb.
- [ ] Normalized route and HTTP method are part of action identity. A PATCH request
      carrying information associated with a DELETE action cannot invoke the
      DELETE callback.
- [ ] A component with several actions and several `@page` routes dispatches only
      the statically matching route, method, and callback.
- [ ] Statically discoverable `@onpost`, `@onput`, `@onpatch`, and `@ondelete`
      method-group bindings demonstrate the intended inferred allow-list. Literal
      `hx-*` attributes are checked for mismatches without granting another
      method. GET remains the only default when no unsafe handler intent is
      provable.
- [ ] Page-local and statically reachable child-component declarations are tested.
      The result states where graph inference stops and what narrow override is
      required for dynamic or third-party composition.
- [ ] Lambdas, computed attribute values, missing callbacks, duplicate matches, and
      ambiguous routes produce deterministic supported behavior or build
      diagnostics rather than a broad runtime method allowance.
- [ ] The .NET 11 cascading subscription candidate records whether it can observe
      the correct live component instance and whether it can dispatch and render
      the action response before the stock endpoint writes output. Its
      renderer-implementation-detail status is included in the support decision.
- [ ] The candidate uses no static local endpoint method, private renderer access,
      runtime callback discovery, or globally replaced Blazor service.
- [ ] If no supported framework path preserves instance lifecycle, the issue
      produces an upstream-ready ASP.NET Core seam proposal and a human decision
      to reduce v1's custom unsafe-verb scope rather than ship an undocumented
      renderer fork.
- [ ] No production library behavior is introduced by the spike.

## Verification contract

- **Protected behavior:** When a Razor component declares one unsafe HTMX action,
  only the matching HTTP method and route can invoke that callback, and it runs
  with the component's normal request lifecycle state after security succeeds.
- **Risk and evidence:** Method confusion, route confusion, callback replay, and
  bypassed framework lifecycle/security; hosted HTTP integration tests exercise
  real routing, authentication/authorization, antiforgery/CSRF, binding, component
  construction, and generated dispatch.
- **Observation seam:** An actual unsafe HTTP request followed by the component's
  public response or durable test-visible outcome. Build diagnostics are observed
  through generator/analyzer compilation tests for statically ambiguous cases.
- **Boundary fidelity:** Keep ASP.NET Core routing, security middleware, request
  binding, and Blazor component execution real. Use deterministic application
  state and identities; do not mock the security or dispatch boundary being
  proved.
- **Meaningful red:** The current PATCH-with-DELETE-identifier request invokes the
  wrong callback at the starting revision. Reversing the proposed method/route
  binding must make the negative HTTP test fail for that same reason.
- **Success evidence:** One supported instance-dispatch design passes the positive
  and negative verb matrix and has compile-time behavior for every analyzed Razor
  declaration shape, or the maintainer accepts a reduced v1 verb contract backed
  by an upstream proposal.
- **Residual risk:** This spike does not implement the production source generator,
  complete form-validation UI, caller-owned HTMX adapter, cache/history policy, or
  release security review.

## Blocked by

- Proposed issue 01 must first select the supported stock GET/POST execution path
  and component-instance lifecycle boundary.
