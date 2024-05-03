using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Htmxor.Http;

namespace Htmxor.Components;

/// <summary>
/// Enables swapping HTML content dynamically based on specified parameters through Htmx.
/// </summary>
public sealed class HtmxSwappable : IComponent, IConditionalOutputComponent
{
	private RenderHandle renderHandle;
	private string swapParam = string.Empty;
	[Inject] private HtmxContext Context { get; set; } = default!;
	
	/// <summary>
	/// Gets or sets the target DOM element ID where the component should be rendered.
	/// This parameter is required.
	/// </summary>
	[Parameter, EditorRequired]
	public string TargetId { get; set; } = default!;

	/// <summary>
	/// Gets or sets the swap style to be applied when the content is swapped.
	/// The default swap style is InnerHTML.
	/// </summary>
	[Parameter]
	public SwapStyle SwapStyle { get; set; } = SwapStyle.InnerHTML;

	/// <summary>
	/// Gets or sets the CSS selector for the content swap. This is optional.
	/// </summary>
	[Parameter] public string Selector { get; set; } = default!;

	/// <summary>
	/// Gets or sets the child content to be rendered within the component.
	/// This parameter is required.
	/// </summary>
	[Parameter, EditorRequired]
	public RenderFragment ChildContent { get; set; } = default!;

	/// <summary>
	/// Gets or sets the child content to be rendered within the component.
	/// This parameter is required.
	/// </summary>
	[Parameter]
	public RenderFragment? Placeholder { get; set; } = default!;

	[Parameter]
	public bool LazyLoad { get; set; }

	[Parameter]
	public string LazyLoadTrigger { get; set; } = default!;

	[Parameter] public bool Condition { get; set; } = true;

	public Task SetParametersAsync(ParameterView parameters)
	{
		if (!parameters.TryGetValue<RenderFragment>(nameof(ChildContent), out var childContent))
		{
			throw new ArgumentException($"{nameof(HtmxPartial)} requires a value for the parameter {nameof(ChildContent)}.", nameof(parameters));
		}

		parameters.TryGetValue<RenderFragment>(nameof(Placeholder), out var placeHolder);

		ChildContent = childContent;
		Placeholder = placeHolder;
		Condition = parameters.GetValueOrDefault(nameof(Condition), true);
		SwapStyle = parameters.GetValueOrDefault(nameof(SwapStyle), SwapStyle.InnerHTML);
		Selector = parameters.GetValueOrDefault(nameof(Selector), string.Empty);
		LazyLoad = parameters.GetValueOrDefault(nameof(LazyLoad), false);
		LazyLoadTrigger = parameters.GetValueOrDefault(nameof(LazyLoadTrigger), "load");

		if (SwapStyle == SwapStyle.Default && !string.IsNullOrWhiteSpace(Selector))
		{
			throw new ArgumentException(
				$"{nameof(Selector)} parameter must not be set when the parameter {nameof(SwapStyle)} is SwapStyle.Default",
				nameof(parameters));
		}

		var style = SwapStyle == SwapStyle.Default ? "true" : SwapStyle.ToHtmxString();
		swapParam = !string.IsNullOrEmpty(Selector) ? $"{style}:{Selector}" : style;

		renderHandle.Render(ChildContent);
		return Task.CompletedTask;
	}

	void IComponent.Attach(RenderHandle renderHandle)
	{
		this.renderHandle = renderHandle;
		var url = Context.Request.CurrentURL?.PathAndQuery;

		renderHandle.Render(builder =>
		{
			builder.OpenElement(0, "div");
			builder.AddAttribute(1, "id", TargetId);
			if (Context.Request.IsHtmxRequest)
			{
				builder.AddAttribute(2, "hx-swap-oob", swapParam);
			}

			if (LazyLoad && !string.IsNullOrWhiteSpace(url) && Placeholder != null)
			{
				builder.AddAttribute(3, "hx-get", url);
				builder.AddAttribute(4, "hx-trigger", LazyLoadTrigger);
				builder.AddContent(5, Placeholder);
			}
			else
			{
				builder.AddContent(6, ChildContent);
			}
			builder.CloseElement();
		});
	}

	bool IConditionalOutputComponent.ShouldOutput(int _) => Condition;
}
