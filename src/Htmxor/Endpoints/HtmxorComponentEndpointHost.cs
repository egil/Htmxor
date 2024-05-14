// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using global::Microsoft.AspNetCore.Components.Rendering;
using global::Microsoft.AspNetCore.Components;
using static Htmxor.LinkerFlags;

namespace Htmxor.Endpoints;

/// <summary>
/// Internal component type that acts as a root component when executing a Htmxor Component endpoint. It takes
/// care of rendering the component inside its hierarchy of layouts (via LayoutView) as well as converting
/// any information we want into component parameters. We could also use this to supply other data from the
/// original HttpContext as component parameters, e.g., for model binding.
///
/// It happens to be almost the same as RouteView except it doesn't supply any query parameters. We can
/// resolve that at the same time we implement support for form posts.
/// </summary>
internal class HtmxorComponentEndpointHost : IComponent
{
	private RenderHandle _renderHandle;

	[Parameter]
	[DynamicallyAccessedMembers(Component)]
	public Type ComponentType { get; set; } = default!;

	[Parameter]
	public IReadOnlyDictionary<string, object?>? ComponentParameters { get; set; }

	public void Attach(RenderHandle renderHandle)
		=> _renderHandle = renderHandle;

	public Task SetParametersAsync(ParameterView parameters)
	{
		parameters.SetParameterProperties(this);
		_renderHandle.Render(BuildRenderTree);
		return Task.CompletedTask;
	}

	private void BuildRenderTree(RenderTreeBuilder builder)
	{
		// Since GetCustomAttributes does not guarantee the order of attributes
		// returned, having a priority allows users to define a default HtmxLayout
		// e.g. in their _Imports.razor, and then override that on a component base,
		// just as they can with the @layout directive in razor files.
		var pageLayoutType = ComponentType
			                     .GetCustomAttributes<HtmxLayoutAttribute>(true)
			                     .MaxBy(x => x.Priority)?.LayoutType ??
							ComponentType
								.GetCustomAttribute<LayoutAttribute>()?.LayoutType;

		builder.OpenComponent<LayoutView>(0);
		builder.AddComponentParameter(1, nameof(LayoutView.Layout), pageLayoutType);
		builder.AddComponentParameter(2, nameof(LayoutView.ChildContent), (RenderFragment)RenderPageWithParameters);
		builder.CloseComponent();
	}

	private void RenderPageWithParameters(RenderTreeBuilder builder)
	{
		builder.OpenComponent(0, ComponentType);

		if (ComponentParameters is not null)
		{
			foreach (var kvp in ComponentParameters)
			{
				builder.AddComponentParameter(1, kvp.Key, kvp.Value);
			}
		}

		builder.CloseComponent();
	}
}