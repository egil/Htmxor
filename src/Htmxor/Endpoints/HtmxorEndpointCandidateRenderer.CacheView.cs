#if NET11_0_OR_GREATER
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// CacheView component-state coordination adapted from ASP.NET Core v11.0.0-rc.1.26425.128 at
// commit c3325eeb6b47bc6383c127d4f4827dc9642a2b6e, synchronized 2026-09-16; see
// docs/engineering/candidate-form-adapter.md for the approved #219 dependency inventory.
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointComponentState.cs | reimplements

using System.Collections.Concurrent;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Htmxor.Endpoints.HtmxorEndpointCandidateRenderer))]

namespace Htmxor.Endpoints;

internal partial class HtmxorEndpointCandidateRenderer
{
	// Supplies the two inputs stock gives CacheView from EndpointComponentState as component state is created:
	// whether the boundary sits in a streaming context, and the tree position that contributes to its key.
	private void TrackForCacheView(
		int componentId, IComponent component, ComponentState? parentComponentState, ComponentState state)
	{
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
				() => ComputeCacheViewTreePositionKey(parentComponentState, cacheView, ancestorTypeName, state));
		}
	}

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
	// One bounded bit: whether this is an htmx request. It exists only so that an ordinary response and an
	// htmx response can never share an entry, since the same URL serves both and their bodies differ.
	//
	// Nothing else about the request enters the key. An earlier revision put every HX-* header here, which
	// closed a real hole and opened two worse ones: the key became unbounded attacker-controlled input, so a
	// client could force an entry per request, and the name=value encoding was forgeable, so a crafted single
	// header impersonated two and was served another client's stored body. Both were measured.
	//
	// Variation beyond this bit is the application's to declare, exactly as it is under stock. VaryByHeader
	// takes a header list and reaches stock's own length-prefixed resolver, so <CacheView VaryByHeader=
	// "HX-Target"> keys correctly with no Htmxor encoding involved and a cardinality the author chose.
	// Read through the lazy key factory and the write path, both of which run after parameter binding, because
	// component state is created before VaryByHeader has a value.
	internal string CurrentRepresentation(CacheView cacheView)
		=> !httpContext.GetHtmxContext().Request.IsHtmxRequest
			? "ordinary"
			: DeclaresRequestVariation(cacheView) ? "htmx-declared" : "htmx";

	// An htmx request is cached only where the application said what its content varies by. Undeclared, the
	// boundary is suppressed through the framework's own switch -- PrepareAsync returns null, so there is no
	// store and no hit -- rather than cached against a key that does not describe the request. Htmxor cannot
	// know what a subtree read, and guessing wrongly serves one request another's markup; guessing broadly
	// makes the key attacker-controlled. Declaring is the only answer that is both safe and bounded, and it
	// is the answer stock already gives for every other per-request input.
	internal static bool DeclaresRequestVariation(CacheView cacheView)
		=> !string.IsNullOrEmpty(cacheView.VaryByHeader) || !string.IsNullOrEmpty(cacheView.VaryBy);

	// Mirrors stock EndpointComponentState's position computation: multiple CacheView components under one
	// parent must not share a key. The result is deliberately not equal to stock's, because the representation
	// is appended above, and stock's SSRRenderModeBoundary GetComponentKey override is not mirrored.
	private string ComputeCacheViewTreePositionKey(
		ComponentState parentComponentState, CacheView target, string ancestorTypeName, ComponentState state)
	{
		// Read inside the factory rather than at component-state creation: an ancestor's parameters, including
		// an HtmxFragment's Name, are set before its children resolve their entries but not before this state
		// is constructed.
		var beneathRequestVarying = HasUncacheableAncestor(state);
		var frames = GetCurrentRenderTreeFrames(parentComponentState.ComponentId);
		for (var index = 0; index < frames.Count; index++)
		{
			ref var frame = ref frames.Array[index];
			if (frame.FrameType is RenderTreeFrameType.Component && ReferenceEquals(frame.Component, target))
			{
				return HtmxorEndpointCandidateCacheViewServices.ComputeTreePositionKey(
					ancestorTypeName, frame.Sequence, frame.ComponentKey, CurrentRepresentation(target), beneathRequestVarying);
			}
		}

		throw new InvalidOperationException(
			$"Could not locate the CacheView in the render tree of its parent component '{parentComponentState.Component?.GetType().FullName}' while computing its cache key.");
	}
}
#endif
