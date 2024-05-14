using Htmxor.Http;
using Microsoft.AspNetCore.Components;

namespace Htmxor.Components;

/// <summary>
/// Represents a component that will only render its output based on the result from its <see cref="ShouldOutput(HtmxContext, int, int)" />.
/// </summary>
public abstract class ConditionalComponentBase : ComponentBase, IConditionalRender
{
	/// <summary>
	/// The <see cref="HtmxContext"/> for the current request.
	/// </summary>
	[Inject]
	protected HtmxContext Context { get; private set; } = default!;

	/// <inheritdoc/>
	/// <remarks>The <see cref="ConditionalComponentBase"/> defaults to returning <see langword="true"/>
	/// when the request is a full page request or if there are no direct conditional children.</remarks>
	public virtual bool ShouldOutput([NotNull] HtmxContext context, int directConditionalChildren, int conditionalChildren)
		=> context.Request.RoutingMode is RoutingMode.Standard || directConditionalChildren == 0;
}
