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
		if (context.Request.RoutingMode is not RoutingMode.Direct)
		{
			content.WriteHtmlTo(output);
			return;
		}

		// Validate the complete name scope and selection before the writer can commit any HTML.
		var componentIds = ResolveSelectedFragments(context.Response.SelectedFragmentNames);
		if (componentIds.Length == 0)
		{
			content.WriteHtmlTo(output);
			return;
		}

		foreach (var componentId in componentIds)
		{
			WriteCompletedComponentHtml(componentId, output);
		}
	}

	private int[] ResolveSelectedFragments(IReadOnlyList<string> names)
	{
		var declarations = GetNamedFragmentComponents();
		var selectedNames = new HashSet<string>(StringComparer.Ordinal);
		var componentIds = new int[names.Count];
		for (var index = 0; index < names.Count; index++)
		{
			var name = names[index];
			ValidateFragmentName(name);
			if (!selectedNames.Add(name))
			{
				throw new InvalidOperationException($"Fragment '{name}' was selected more than once.");
			}
			if (!declarations.TryGetValue(name, out componentIds[index]))
			{
				throw new InvalidOperationException($"No fragment named '{name}' was rendered.");
			}
		}
		ValidateNonOverlappingFragments(componentIds);
		return componentIds;
	}

	private Dictionary<string, int> GetNamedFragmentComponents()
	{
		var declarations = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var (componentId, fragment) in renderedFragments)
		{
			if (fragment.Name is not { } name)
			{
				continue;
			}
			ValidateFragmentName(name);
			if (!declarations.TryAdd(name, componentId))
			{
				throw new InvalidOperationException($"More than one fragment named '{name}' was rendered.");
			}
		}
		return declarations;
	}

	private static void ValidateFragmentName(string? name)
	{
		if (name is not { Length: > 0 and <= 64 } ||
			!char.IsAsciiLetter(name[0]) ||
			name.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
		{
			throw new InvalidOperationException($"Invalid fragment name '{name}'. Names must start with an ASCII letter, contain only ASCII letters, digits, '-' or '_', and have at most 64 characters.");
		}
	}

	private void ValidateNonOverlappingFragments(int[] componentIds)
	{
		var selectedIds = componentIds.ToHashSet();
		foreach (var componentId in componentIds)
		{
			for (var ancestor = GetComponentState(componentId).ParentComponentState;
				ancestor is not null;
				ancestor = ancestor.ParentComponentState)
			{
				if (selectedIds.Contains(ancestor.ComponentId))
				{
					throw new InvalidOperationException("Selected fragments cannot include both an ancestor and its descendant.");
				}
			}
		}
	}
}
