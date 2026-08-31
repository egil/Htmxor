using System.Diagnostics.CodeAnalysis;
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
	/// Gets or sets additional attributes for the optional wrapper element.
	/// </summary>
	[Parameter(CaptureUnmatchedValues = true)]
	[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "This follows Blazor's additional-attribute convention.")]
	public IDictionary<string, object>? AdditionalAttributes { get; set; }

	/// <summary>
	/// Gets or sets the child content that should be rendered if the <see cref="Match"/> predicate returns <see langword="true"/>.
	/// </summary>
	[Parameter, EditorRequired]
	public required RenderFragment ChildContent { get; set; }

	/// <summary>
	/// Gets or sets the optional wrapper element name.
	/// </summary>
	/// <remarks>
	/// The fragment is wrapperless when no element, <see cref="Id"/>, or additional attributes are supplied.
	/// A <c>div</c> is used when a wrapper is requested without an element name.
	/// </remarks>
	[Parameter]
	public string? Element { get; set; }

	/// <summary>
	/// Gets or sets the optional wrapper element identifier.
	/// </summary>
	[Parameter]
	public string? Id { get; set; }

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
		if (!ShouldOutput(Context, 0, 0))
		{
			return;
		}

		var wrapper = Element;
		if (wrapper is null && (Id is not null || AdditionalAttributes?.Count > 0))
		{
			wrapper = "div";
		}

		if (wrapper is null)
		{
			builder.AddContent(0, ChildContent);
			return;
		}

		builder.OpenElement(1, wrapper);
		if (AdditionalAttributes is not null)
		{
			builder.AddMultipleAttributes(2, AdditionalAttributes);
		}
		builder.AddAttribute(3, Constants.Attributes.Id, Id);
		builder.AddContent(4, ChildContent);
		builder.CloseElement();
	}

	/// <inheritdoc/>
	protected override void OnParametersSet()
	{
		Element = Normalize(Element);
		Id = Normalize(Id);
	}

	/// <inheritdoc/>
	public override bool ShouldOutput([NotNull] HtmxContext context, int directConditionalChildren, int conditionalChildren)
		=> (RenderDuringStandardRequest && context.Request.RoutingMode is RoutingMode.Standard)
		|| (context.Request.RoutingMode is RoutingMode.Direct &&
			(Match?.Invoke(context.Request) ?? MatchesTarget(context.Request.Target)));

	private bool MatchesTarget(string? target)
		=> Id is null
		|| string.Equals(Id, target, StringComparison.Ordinal)
		|| HtmxElementIdentity.Matches(Element ?? "div", Id, target);

	private static string? Normalize(string? value)
		=> string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
