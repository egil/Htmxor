using System.ComponentModel;

namespace Htmxor.Builder;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class HtmxorGeneratedComponentAction
{
	public HtmxorGeneratedComponentAction(
		Type componentType,
		string httpMethod,
		string handlerIdentity)
	{
		ArgumentNullException.ThrowIfNull(componentType);
		ArgumentException.ThrowIfNullOrWhiteSpace(httpMethod);
		ArgumentException.ThrowIfNullOrWhiteSpace(handlerIdentity);

		ComponentType = componentType;
		HttpMethod = httpMethod;
		HandlerIdentity = handlerIdentity;
	}

	internal Type ComponentType { get; }

	internal string HttpMethod { get; }

	internal string HandlerIdentity { get; }
}
