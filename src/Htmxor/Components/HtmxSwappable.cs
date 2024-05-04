using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Htmxor.Http;

namespace Htmxor.Components;

/// <summary>
/// Enables swapping HTML content dynamically based on specified parameters through Htmx.
/// </summary>
public sealed class HtmxSwappable : ComponentBase
{
	private string swapParam = string.Empty;

	[Inject]
	private HtmxContext Context { get; set; } = default!;

	[SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "False positive. This is a parameter.")]
	[Parameter(CaptureUnmatchedValues = true)]
	public IDictionary<string, object>? AdditionalAttributes { get; set; }

	/// <summary>
	/// Gets or sets the target DOM element ID where the component should be rendered.
	/// </summary>
	[Parameter]
	public string? TargetId { get; set; }

	/// <summary>
	/// Gets or sets the swap style to be applied when the content is swapped.
	/// The default swap style is OuterHTML.
	/// </summary>
	[Parameter]
	public SwapStyle SwapStyle { get; set; } = SwapStyle.OuterHTML;

	/// <summary>
	/// Gets or sets the CSS selector for the content swap. This is optional.
	/// </summary>
	[Parameter] public string? Selector { get; set; } 

	/// <summary>
	/// Gets or sets the child content to be rendered within the component.
	/// This parameter is required.
	/// </summary>
	[Parameter, EditorRequired]
	public RenderFragment ChildContent { get; set; } = default!;

	protected override void OnParametersSet()
	{
		TargetId = string.IsNullOrWhiteSpace(TargetId) ? TargetId : TargetId.Trim();

		if (string.IsNullOrWhiteSpace(TargetId) && string.IsNullOrWhiteSpace(Selector))
		{
			throw new ArgumentException($"Either {nameof(TargetId)} or {nameof(Selector)} must be provided to determine the OOB swap target");
		}
		
		// If the user manually added some of these attributes they are removed
		// to avoid inconsistent behavior.
		// The user should not set these attributes manually.
		if (AdditionalAttributes is not null)
		{
			AdditionalAttributes.Remove(HtmxConstants.Attributes.Id);
			AdditionalAttributes.Remove(HtmxConstants.Attributes.HxSwapOob);
		}

		if (SwapStyle == SwapStyle.Default)
			SwapStyle = SwapStyle.OuterHTML;

		var style = SwapStyle.ToHtmxString();
		swapParam = !string.IsNullOrEmpty(Selector) ? $"{style}:{Selector}" : style;
	}

	protected override void BuildRenderTree([NotNull] RenderTreeBuilder builder)
	{
		if (Context.Request.Target == TargetId)
		{
			builder.AddContent(0, ChildContent);
		}
		else
		{
			builder.OpenElement(1, "div");

			if (TargetId != null)
				builder.AddAttribute(2, "id", TargetId);

			builder.AddAttribute(3, "hx-swap-oob", swapParam);
			builder.AddContent(4, ChildContent);
			builder.CloseElement();
		}
	}
}
