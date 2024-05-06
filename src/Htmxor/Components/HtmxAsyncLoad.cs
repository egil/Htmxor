using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Htmxor.Components;

/// <summary>
/// A component that will render out an element with the <c>hx-trigger="load"</c> attribute
/// that causes htmx to issue a second request to the server to load the child content in the background.
/// This is useful when loading the child content takes a long time and you want to show a loading spinner.
/// </summary>
public sealed class HtmxAsyncLoad : ConditionalComponentBase
{
	[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "False positive. This is a parameter.")]
	[Parameter(CaptureUnmatchedValues = true)]
	public IDictionary<string, object>? AdditionalAttributes { get; set; }

	/// <summary>
	/// The content to render out when the htmx load request arrives.
	/// </summary>
	[Parameter, EditorRequired]
	public required RenderFragment ChildContent { get; set; }

	/// <summary>
	/// The content to should be rendered while the htmx load request is in progress.
	/// </summary>
	[Parameter]
	public RenderFragment? Loading { get; set; }

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

	protected override void OnParametersSet()
	{
		Element = string.IsNullOrWhiteSpace(Element) ? "div" : Element.Trim();
		Id = string.IsNullOrWhiteSpace(Id) ? Id : Id.Trim();

		if (AdditionalAttributes is null)
		{
			return;
		}

		RemoveControlledAttributeAndThrow(AdditionalAttributes, Constants.Attributes.HxGet);
		RemoveControlledAttributeAndThrow(AdditionalAttributes, Constants.Attributes.HxTrigger);
		RemoveControlledAttributeAndThrow(AdditionalAttributes, Constants.Attributes.HxTarget);
		RemoveControlledAttributeAndThrow(AdditionalAttributes, Constants.Attributes.HxSwap);
	}

	protected override void BuildRenderTree([NotNull] RenderTreeBuilder builder)
	{
		var request = Context.Request;
		builder.OpenElement(1, Element);
		builder.AddAttribute(2, Constants.Attributes.Id, Id);
		if (request.RoutingMode == RoutingMode.Standard)
		{
			builder.AddAttribute(3, Constants.Attributes.HxGet, request.Path);
			builder.AddAttribute(4, Constants.Attributes.HxTrigger, Constants.Triggers.Load);
			builder.AddAttribute(5, Constants.Attributes.HxTarget, $"#{Id}");
			builder.AddAttribute(6, Constants.Attributes.HxSwap, Constants.SwapStyles.OuterHTML);
		}

		if (AdditionalAttributes is not null)
		{
			builder.AddMultipleAttributes(7, AdditionalAttributes);
		}

		if (request.RoutingMode == RoutingMode.Standard && Loading is not null)
		{
			builder.AddContent(8, Loading);
		}

		else if (request.RoutingMode == RoutingMode.Direct || request.Target == Id && request.Trigger == Id)
		{
			builder.AddContent(9, ChildContent);
		}

		builder.CloseElement();
	}

	private static void RemoveControlledAttributeAndThrow(IDictionary<string, object> attributes, string attributeName)
	{
		if (attributes.Remove(attributeName))
		{
			throw new ArgumentException($"The '{attributeName}' attribute is controlled by the {nameof(HtmxAsyncLoad)} components and should not be set explicitly.");
		}
	}
}
