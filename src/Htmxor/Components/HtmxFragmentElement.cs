using System.Diagnostics.CodeAnalysis;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Htmxor.Components;

/// <inheritdoc/>
/// <remarks>
/// This extends <see cref="HtmxFragment"/> by wrapping the <see cref="HtmxFragment.ChildContent"/> inside
/// an HTML element of kind <see cref="Element"/> with the <c>hx-swap="outerHTML"</c> attribute.
/// This allows htmx to replace the content of the <see cref="HtmxFragmentElement"/> on matching requests.
/// </remarks>
public class HtmxFragmentElement : HtmxFragment
{
	/// <summary>
	/// Captures any additonal attributes that should be set on the <see cref="Element"/>,
	/// </summary>
	[Parameter(CaptureUnmatchedValues = true)]
	[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "False positive. This follows Blazor's conventions for additional attributes.")]
	public IDictionary<string, object>? AdditionalAttributes { get; set; }

	/// <summary>
	/// The ID of the element that <see cref="HtmxFragmentElement"/> should render it's content inside.
	/// This is also the ID that htmx will use to target the element to replace it's content.
	/// </summary>
	[Parameter, EditorRequired]
	public required string Id { get; set; }

	/// <summary>
	/// The element type that <see cref="HtmxFragmentElement"/> should render
	/// <see cref="HtmxFragment.ChildContent"/>.
	/// </summary>
	/// <remarks>Default is a <c>div</c> element.</remarks>
	[Parameter]
	public string Element { get; set; } = "div";

	/// <inheritdoc/>
	public override bool ShouldOutput([NotNull] HtmxContext context, int directConditionalChildren, int conditionalChildren)
		=> (RenderDuringStandardRequest && context.Request.RoutingMode is RoutingMode.Standard)
		|| (Match?.Invoke(context.Request) ?? context.Request.Target == Id);

	/// <inheritdoc/>
	protected override void OnParametersSet()
	{
		Element = string.IsNullOrWhiteSpace(Element) ? "div" : Element.Trim();
		Id = string.IsNullOrWhiteSpace(Id) ? Id : Id.Trim();

		if (AdditionalAttributes is null)
		{
			return;
		}

		RemoveControlledAttributeAndThrow(AdditionalAttributes, Constants.Attributes.HxTarget);
		RemoveControlledAttributeAndThrow(AdditionalAttributes, Constants.Attributes.HxSwap);
	}

	/// <inheritdoc/>
	protected override void BuildRenderTree([NotNull] RenderTreeBuilder builder)
	{
		if (!ShouldOutput(Context, 0, 0))
		{
			return;
		}

		var request = Context.Request;
		builder.OpenElement(1, Element);
		builder.AddAttribute(2, Constants.Attributes.Id, Id);
		builder.AddAttribute(3, Constants.Attributes.HxSwap, Constants.SwapStyles.OuterHTML);

		if (AdditionalAttributes is not null)
		{
			builder.AddMultipleAttributes(4, AdditionalAttributes);
		}

		builder.AddContent(5, ChildContent);
		builder.CloseElement();
	}

	private static void RemoveControlledAttributeAndThrow(IDictionary<string, object> attributes, string attributeName)
	{
		if (attributes.Remove(attributeName))
		{
			throw new ArgumentException($"The '{attributeName}' attribute is controlled by the {nameof(HtmxFragmentElement)} components and should not be set explicitly.");
		}
	}
}
