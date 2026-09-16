// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// CacheView component-state coordination adapted from ASP.NET Core v11.0.0-rc.1.26425.128 at
// commit c3325eeb6b47bc6383c127d4f4827dc9642a2b6e, synchronized 2026-09-15; see
// docs/engineering/candidate-form-adapter.md for the approved #219 dependency inventory.
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointComponentState.cs | reimplements

using System.Collections.Concurrent;
using Htmxor.Components;
using Htmxor.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Htmxor.Endpoints.HtmxorEndpointCandidateRenderer))]

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
#if NET11_0_OR_GREATER
		// This is the inherited notion stock keeps on EndpointComponentState.StreamRendering, which CacheView
		// needs: a component inside a streaming subtree is itself in a streaming context. It is deliberately
		// not the same question as IsStreamingComponent, which asks only whether this component's own type
		// opted in and decides whether to emit that component's streaming markers.
		streamRenderingByComponentId[componentId] =
			GetStreamRenderingAttribute(component) ?? IsInheritedStreamRendering(state.LogicalParentComponentState);
		if (component is CacheView cacheView && parentComponentState is not null)
		{
			var ancestorTypeName = parentComponentState.Component?.GetType().FullName ?? "";
			services.GetRequiredService<HtmxorEndpointCandidateCacheViewServices>().Initialize(
				cacheView,
				streamRenderingByComponentId[componentId],
				() => ComputeCacheViewTreePositionKey(parentComponentState, cacheView, ancestorTypeName));
		}
#endif
		return state;
	}

#if NET11_0_OR_GREATER
	private readonly Dictionary<int, bool> streamRenderingByComponentId = [];

	internal bool IsInStreamingContext(int componentId)
		=> streamRenderingByComponentId.TryGetValue(componentId, out var streaming) && streaming;

	private bool IsInheritedStreamRendering(ComponentState? parentComponentState)
		=> parentComponentState is not null &&
			streamRenderingByComponentId.TryGetValue(parentComponentState.ComponentId, out var streaming) &&
			streaming;

	private static readonly ConcurrentDictionary<Type, bool?> StreamRenderingByComponentType = new();

	// Upstream registers EndpointComponentState as a metadata update handler so this cache cannot outlive the
	// attributes it describes. Invoked by the hot reload host through reflection.
	internal static void ClearCache(Type[]? _) => StreamRenderingByComponentType.Clear();

	private static bool? GetStreamRenderingAttribute(IComponent component)
		=> StreamRenderingByComponentType.GetOrAdd(component.GetType(), static type => type
			.GetCustomAttributes(typeof(StreamRenderingAttribute), inherit: true)
			.OfType<StreamRenderingAttribute>()
			.Select(attribute => (bool?)attribute.Enabled)
			.FirstOrDefault());

	// One URL serves more than one representation under Htmxor, and the framework's key has no dimension for
	// that, so an ordinary response and an htmx one would share an entry. They do not contain the same thing:
	// a body cached for one is wrong for the other, and a named fragment stored around is never reconstructed,
	// which fails the request. The representation therefore joins the position Htmxor already supplies.
	private string CurrentRepresentation()
	{
		// Only the request's own immutable properties. Selected fragment names were tried and removed: selection
		// is permitted from ordinary lifecycle code, so the value is not yet stable when the framework asks for
		// the key, and it added nothing — a boundary holding a fragment abandons its capture regardless.
		var request = httpContext.GetHtmxContext().Request;
		return $"{request.IsHtmxRequest}.{request.RoutingMode}";
	}

	// Mirrors stock EndpointComponentState's position computation: multiple CacheView components under one
	// parent must not share a key. The result is deliberately not equal to stock's, because the representation
	// is appended above, and stock's SSRRenderModeBoundary GetComponentKey override is not mirrored.
	private string ComputeCacheViewTreePositionKey(ComponentState parentComponentState, CacheView target, string ancestorTypeName)
	{
		var frames = GetCurrentRenderTreeFrames(parentComponentState.ComponentId);
		for (var index = 0; index < frames.Count; index++)
		{
			ref var frame = ref frames.Array[index];
			if (frame.FrameType is RenderTreeFrameType.Component && ReferenceEquals(frame.Component, target))
			{
				return HtmxorEndpointCandidateCacheViewServices.ComputeTreePositionKey(
					ancestorTypeName, frame.Sequence, frame.ComponentKey, CurrentRepresentation());
			}
		}

		throw new InvalidOperationException(
			$"Could not locate the CacheView in the render tree of its parent component '{parentComponentState.Component?.GetType().FullName}' while computing its cache key.");
	}
#endif

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
