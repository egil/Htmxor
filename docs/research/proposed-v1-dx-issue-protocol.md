Parent: #77

## Outcome

Finalize a small, uniform, htmx 4-aware `HtmxContext` contract that types the
core protocol while allowing bounded extension behavior without wrapping
general `HttpContext` features.

Protected behavior:

> When a component callback inspects an htmx request or shapes its response,
> every core header has predictable parsing, naming, validation, and fluent
> behavior, and malformed or extension input cannot silently broaden the
> request.

## Problem

`HtmxContext` as `Request` plus `Response` is easy to understand, but current
members are uneven:

- `CurrentURL` does not follow normal .NET acronym casing;
- boolean classification is based on header presence in places;
- malformed, repeated, and contradictory values do not share a complete public
  policy;
- some response mutators enforce an HTMX-request guard while others do not;
- argument, URI/string overload, fluent return, and empty-body effects vary;
  and
- earlier-version helpers and closed client value types can appear to be htmx 4
  protocol promises.

Htmx 4.0.0 has seven core request headers and nine core response headers.
Extensions add their own headers and events. General status, cookies, cache,
content language, ETags, and other HTTP features already belong to
`HttpContext`.

## Scope

- Apply normal .NET naming, including a reviewed compatibility plan for renamed
  members.
- Define conservative parsing for missing, whitespace, malformed, repeated, and
  contradictory core request headers.
- Type all core htmx 4 request values: request marker, full/partial request type,
  boosted, current URL, source, target, and history restoration.
- Keep every header-derived value explicitly documented as untrusted.
- Make all nine core response-header operations consistent in guard behavior,
  argument validation, fluent return, URL overloads, serialization, overwrite/
  merge rules, and body effects.
- Keep ordinary HTTP status and explicit empty-body control without wrapping
  unrelated `HttpContext` behavior.
- Add bounded request/response extension-header access that does not turn
  unknown protocol into authorization evidence.
- Remove or separately re-prove helpers inherited from htmx 1/2 assumptions
  before stable v1.

## Acceptance criteria

- [ ] A public contract table maps every member to all seven request and all nine
      response headers.
- [ ] Missing, repeated, malformed, false, and contradictory request values have
      wire-level tests and fail conservatively.
- [ ] `HX-Request-Type: full` and `partial`, boosted navigation, and history
      restoration select the documented representation.
- [ ] `HX-Location`, redirect, refresh, push/replace URL, reswap, retarget,
      reselect, and trigger serialization have exact header tests.
- [ ] Every fluent mutator follows the same HTMX-only guard and argument policy,
      with body suppression documented and tested.
- [ ] Event detail JSON uses the application's configured serializer policy where
      intended and has deterministic merge/overwrite behavior.
- [ ] Extension headers can be read/written through a bounded API without a new
      Htmxor package release and without bypassing security metadata.
- [ ] General application headers remain a documented `HttpContext` concern.
- [ ] htmx 4.0.0 browser evidence covers redirects, history, handled errors,
      triggered events, empty responses, and any selected configuration changes.

## Exclusions

- Wrapping all ASP.NET Core request/response APIs.
- Treating HTMX or extension headers as authentication/authorization evidence.
- A typed wrapper for every client attribute, event, extension, or swap style.
- Compatibility claims for versions or extensions not executed.

## Evidence

Record meaningful-red wire tests before changing behavior. Report exact
red/green SHAs, commands and counts, packed-package and htmx 4.0.0 browser
results, browser/OS/configuration, and every unexercised protocol branch.
Separate Standards and Spec reviews are required.
