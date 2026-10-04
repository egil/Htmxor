namespace Htmxor.AspNetCore10;

// A complex parameter value for #275's marker-naming parity cases. Public because it is a Razor component
// parameter type (Issue275ParameterChild.Badge): a generated component class is always public, so its own
// parameter types must be at least as accessible. Note is deliberately left null at every usage site so the
// value's own JSON member exercises stock's null-handling, the same way Subtitle exercises it for a top-level
// null-valued parameter. It carries no behavior of its own.
public sealed record Issue275Badge(string Label, string? Note);
