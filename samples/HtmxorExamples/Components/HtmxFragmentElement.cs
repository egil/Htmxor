using System.Diagnostics.CodeAnalysis;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Htmxor.Components;

public class HtmxFragmentElement : HtmxFragment
{
	[Parameter(CaptureUnmatchedValues = true)]
	public IDictionary<string, object>? AdditionalAttributes { get; set; }

	/// <summary>
	/// The ID of the element that <see cref="HtmxAsyncLoad"/> should render it's content inside.
	/// This is also the ID that htmx will use to target the element to replace it's content.
	/// </summary>
	[Parameter, EditorRequired]
	public required string Id { get; set; }

	/// <summary>
	/// The element type that <see cref="HtmxAsyncLoad"/> should render
	/// <see cref="ChildContent"/> and <see cref="Loading"/> into.
	/// </summary>
	/// <remarks>Default is a <c>div</c> element.</remarks>
	[Parameter]
	public string Element { get; set; } = "div";

	public override bool ShouldOutput([NotNull] HtmxContext context, int directConditionalChildren, int conditionalChildren)
		=> (OnStandardRequest && context.Request.RoutingMode is RoutingMode.Standard)
		|| (Match?.Invoke(context.Request) ?? context.Request.Target == Id);

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
