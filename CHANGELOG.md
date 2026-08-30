# Unreleased

## Breaking changes

- Htmxor now emits response events only through htmx 4's `HX-Trigger` header. Remove the `TriggerTiming` argument from `HtmxResponse.Trigger(...)` calls; htmx 4 dispatches these response events after the swap completes. The `TriggerTiming` type and the `TriggerAfterSwap` and `TriggerAfterSettle` response-header constants have been removed.
