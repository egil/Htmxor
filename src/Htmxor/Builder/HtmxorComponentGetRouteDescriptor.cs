namespace Htmxor.Builder;

internal sealed record HtmxorComponentGetRouteDescriptor(
	Type ComponentType,
	string NormalizedRoute,
	IReadOnlyList<object> Metadata);
