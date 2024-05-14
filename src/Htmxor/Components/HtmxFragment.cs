using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Htmxor.Components;

/// <summary>
/// Represents a component that will only render its <see cref="ChildContent"/> if
/// its <see cref="Match"/> predicate returns <see langword="true"/> or
/// if the request is a standard request and <see cref="RenderDuringStandardRequest"/>
/// is <see langword="true"/>.
/// </summary>
public class HtmxFragment : ConditionalComponentBase
{
	/// <summary>
	/// Gets or sets the child content that should be rendered if the <see cref="Match"/> predicate returns <see langword="true"/>.
	/// </summary>
	[Parameter, EditorRequired]
	public required RenderFragment ChildContent { get; set; }

	/// <summary>
	/// Gets or sets the predicate to determine if the <see cref="ChildContent"/> should be rendered.
	/// </summary>
	[Parameter]
	public Func<HtmxRequest, bool>? Match { get; set; }

	/// <summary>
	/// Gets or sets whether or not to render during a standard request.
	/// </summary>
	/// <remarks>Default is <see langword="true"/>.</remarks>
	[Parameter]
	public bool RenderDuringStandardRequest { get; set; } = true;

	/// <inheritdoc/>
	protected override void BuildRenderTree([NotNull] RenderTreeBuilder builder)
	{
		if (ShouldOutput(Context, 0, 0))
		{
			builder.AddContent(0, ChildContent);
		}
	}

	/// <inheritdoc/>
	public override bool ShouldOutput([NotNull] HtmxContext context, int directConditionalChildren, int conditionalChildren)
		=> (RenderDuringStandardRequest && context.Request.RoutingMode is RoutingMode.Standard)
		|| (Match?.Invoke(context.Request) ?? true);
}
