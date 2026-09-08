using System.Diagnostics.CodeAnalysis;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Htmxor.Components;

/// <summary>
/// Defines a server-selectable fragment with optional wrapper markup.
/// </summary>
/// <remarks>
/// The v1 endpoint renderer completes the component tree before selecting response output by
/// <see cref="Name"/>. <see cref="Match"/> and <see cref="RenderDuringStandardRequest"/>
/// apply only to the legacy conditional renderer.
/// </remarks>
public class HtmxFragment : ConditionalComponentBase
{
	/// <summary>
	/// Gets or sets the stable, case-sensitive server selection name. This does not emit markup or request a wrapper.
	/// </summary>
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

	/// <summary>
	/// Gets or sets the legacy conditional renderer's child-content predicate.
	/// </summary>
	[Parameter]
	public Func<HtmxRequest, bool>? Match { get; set; }

	/// <summary>
	/// Gets or sets whether the legacy conditional renderer emits this fragment during a standard request.
	/// </summary>
	/// <remarks>Default is <see langword="true"/>.</remarks>
	[Parameter]
	public bool RenderDuringStandardRequest { get; set; } = true;

	/// <inheritdoc/>
	protected override void BuildRenderTree([NotNull] RenderTreeBuilder builder)
	{
		if (!Context.UsesCompletedFragmentSelection && !ShouldOutput(Context, 0, 0))
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
