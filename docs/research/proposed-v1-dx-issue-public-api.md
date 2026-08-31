Parent: #77

## Outcome

Freeze a deliberately small, consistently named Htmxor v1 public API before the
first stable package. The stable surface should expose the component authoring
model and protocol operations developers need, without making renderer,
generator, prototype, or incomplete client-helper types permanent compatibility
promises.

Protected behavior:

> When a developer adds Htmxor to a Blazor static-SSR app, the registration and
> public types describe Htmxor's server integration without implying that
> Htmxor installs or owns the htmx browser runtime.

## Problem

Registration currently mixes `AddHtmx()` with
`AddHtmxorComponentEndpoints(...)`. The first can be read as installing the
client runtime even though the application owns htmx. The package also exports
the intended authoring types alongside infrastructure/prototype types and a
trigger/swap/constants DSL that is incomplete for htmx 4.0.0.

#145 already owns removal of the empty route-group argument. This issue owns the
remaining stable-surface decision, not that implementation.

## Scope

- Decide and document one consistent service/endpoint naming pair. Evaluate
  `AddHtmxor()` / `AddHtmxorEndpoints()` against retaining the current names.
- Publish an intentional allow-list of stable authoring and protocol types.
- Review every exported type/member and explicitly keep, reshape, internalize,
  or remove it before v1.
- Decide whether the client trigger/swap/constants helpers are removed from the
  stable core or moved to an explicitly optional, htmx-profile-versioned adapter
  with a raw escape hatch.
- Add a package public-API compatibility baseline that fails on unreviewed
  additions, removals, and signature changes.
- Document source/binary compatibility policy for stable v1.

Candidates for review include registration/mapping extensions, `HtmxRoute`, the
normal-only marker, `HtmxFragment`, `HtmxHeadOutlet`, context/request/response
types, callback event args, layout/async conveniences, structured location
types, conditional-render infrastructure, invoker/generator bridges, renderer
exceptions, and the client DSL.

## Acceptance criteria

- [ ] The stable public allow-list and member signatures are reviewed and
      recorded.
- [ ] Service and endpoint names are consistent and cannot reasonably imply
      that Htmxor supplies htmx.
- [ ] A minimal app configures Htmxor without application-authored route-group
      plumbing after #145.
- [ ] Advanced endpoint conventions retain authorization, rate limit, host,
      cache, and arbitrary metadata behavior through the chosen mapping API.
- [ ] Every non-allow-listed prototype/infrastructure export is internalized or
      has a recorded reason to remain public.
- [ ] The client-helper decision is tested against the complete official htmx
      4.0.0 metadata and permits unknown extension values.
- [ ] Packed-package validation enforces the approved API baseline.
- [ ] Documentation clearly separates application-owned htmx from Htmxor's
      adapter and server services.

## Exclusions

- Implementing the no-route-group mapping already owned by #145.
- Changing the target framework already owned by #148.
- Redesigning route/action declarations, fragment selection, or wire semantics;
  those have separate DX issues.
- Bundling, downloading, or selecting an htmx runtime.

## Evidence

Record a meaningful-red API/package test first, then the exact green HEAD,
commands, test counts, packed-package consumer result, and any compatibility
dimension not exercised. Separate Standards and Spec reviews are required.
