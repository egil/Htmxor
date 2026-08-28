namespace Htmxor.Builder;

internal sealed record HtmxorComponentActionDescriptor(
	Type ComponentType,
	string NormalizedRoute,
	string HttpMethod,
	string HandlerIdentity,
	HtmxorGeneratedComponentAction? GeneratedAction = null);
