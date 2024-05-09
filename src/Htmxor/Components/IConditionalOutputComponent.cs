using Htmxor.Http;

namespace Htmxor.Components;

/// <summary>
/// Represents a component that can conditionally produce markup.
/// </summary>
public interface IConditionalRender
{
	/// <summary>
	/// Determine whether this component should produce any markup during a request.
	/// </summary>
	/// <param name="context">The current request context.</param>
	/// <param name="directConditionalChildren">The number of direct child components that implements <see cref="IConditionalRender"/>.</param>
	/// <param name="conditionalChildren">The number of child/grand-child components that implements <see cref="IConditionalRender"/>.</param>
	/// <returns><see langword="true"/> if the component should produce markup, <see langword="false"/> otherwise.</returns>
	bool ShouldOutput([NotNull] HtmxContext context, int directConditionalChildren, int conditionalChildren);
}

