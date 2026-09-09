using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Htmxor.Components;

/// <summary>
/// Defines a server-selectable fragment with optional wrapper markup.
/// </summary>
/// <remarks>The v1 endpoint renderer completes the component tree before selecting response output by <see cref="Name"/>.</remarks>
public class HtmxFragment : ComponentBase
{
	/// <summary>
	/// Gets or sets the stable, case-sensitive server selection name. This does not emit markup or request a wrapper.
	/// </summary>
	/// <remarks>
	/// A name starts with an ASCII letter, followed by ASCII letters, digits, hyphens, or underscores,
	/// and has at most 64 characters. A null name leaves the fragment unnamed. Invalid or duplicate
	/// declarations fail before direct-response output. Selecting a named fragment includes its own
	/// optional wrapper and rendered subtree, without its ancestors' output.
	/// </remarks>
	[Parameter]
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets additional attributes for the optional wrapper element.
	/// </summary>
	[Parameter(CaptureUnmatchedValues = true)]
	[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "This follows Blazor's additional-attribute convention.")]
	public IDictionary<string, object>? AdditionalAttributes { get; set; }

	/// <summary>
	/// Gets or sets the fragment's child content.
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

	/// <inheritdoc/>
	protected override void BuildRenderTree([NotNull] RenderTreeBuilder builder)
	{
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
		builder.AddAttribute(3, HtmxorAttributeNames.Id, Id);
		builder.AddContent(4, ChildContent);
		builder.CloseElement();
	}

	/// <inheritdoc/>
	protected override void OnParametersSet()
	{
		Element = Normalize(Element);
		Id = Normalize(Id);
	}

	private static string? Normalize(string? value)
		=> string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
