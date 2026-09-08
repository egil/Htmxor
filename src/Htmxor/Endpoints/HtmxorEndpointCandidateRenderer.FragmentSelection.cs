using Htmxor.Components;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;

namespace Htmxor.Endpoints;

internal partial class HtmxorEndpointCandidateRenderer
{
	private readonly Dictionary<int, HtmxFragment> renderedFragments = [];

	protected override ComponentState CreateComponentState(int componentId, IComponent component, ComponentState? parentComponentState)
	{
		var state = base.CreateComponentState(componentId, component, parentComponentState);
		if (component is HtmxFragment fragment)
		{
			renderedFragments.Add(componentId, fragment);
		}
		return state;
	}

	private void RemoveDisposedFragments(in RenderBatch renderBatch)
	{
		foreach (var componentId in renderBatch.DisposedComponentIDs.Array.AsSpan(0, renderBatch.DisposedComponentIDs.Count))
		{
			renderedFragments.Remove(componentId);
		}
	}

	internal void WriteResponseHtml(HtmlRootComponent content, HtmxContext context, TextWriter output)
	{
		Dispatcher.AssertAccess();
		var names = context.Response.SelectedFragmentNames;
		if (context.Request.RoutingMode is not RoutingMode.Direct || names.Count == 0)
		{
			content.WriteHtmlTo(output);
			return;
		}

		// Resolve every boundary before writing so a missing name cannot leave a partial response.
		var componentIds = names.Select(name => renderedFragments.Single(fragment =>
			string.Equals(fragment.Value.Name, name, StringComparison.Ordinal)).Key).ToArray();
		foreach (var componentId in componentIds)
		{
			WriteCompletedComponentHtml(componentId, output);
		}
	}
}
