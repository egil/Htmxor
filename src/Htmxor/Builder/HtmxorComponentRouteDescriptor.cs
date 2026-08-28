namespace Htmxor.Builder;

internal sealed record HtmxorComponentRouteDescriptor(
	Type ComponentType,
	string NormalizedRoute,
	IReadOnlyList<object> Metadata,
	IReadOnlyList<string> HttpMethods);
