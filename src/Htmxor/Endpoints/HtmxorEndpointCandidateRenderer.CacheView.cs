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


namespace Htmxor.Endpoints;

internal partial class HtmxorEndpointCandidateRenderer
{
	// Supplies the two inputs stock gives CacheView from EndpointComponentState as component state is created:
	// whether the boundary sits in a streaming context, and the tree position that contributes to its key.
	private void TrackForCacheView(
		int componentId, IComponent component, ComponentState? parentComponentState, ComponentState state)
	{
		// TrackStreamRendering has already recorded whether this component is in a streaming context, the
		// inherited EndpointComponentState.StreamRendering that CacheView needs.
		if (component is CacheView cacheView)
		{
			// Every other shape mismatch in this adapter fails loudly. A parentless boundary would otherwise
			// skip Initialize and fall back to a key carrying neither the representation nor the ancestor
			// flag -- precisely the state in which an ordinary and an htmx response share an entry. Stock
			// always has a parent here, so this is unreachable rather than merely unlikely; degrade the wrong
			// way and it degrades silently.
			if (parentComponentState is null)
			{
				throw new InvalidOperationException(
					$"A {nameof(CacheView)} was created with no parent component state, so Htmxor cannot supply the " +
					"tree position its cache key requires. Report this with the page that produced it.");
			}

			var ancestorTypeName = parentComponentState.Component?.GetType().FullName ?? "";
			services.GetRequiredService<HtmxorEndpointCandidateCacheViewServices>().Initialize(
				cacheView,
				streamRenderingByComponentId[componentId],
				() => ComputeCacheViewTreePositionKey(parentComponentState, cacheView, ancestorTypeName, state));
		}
	}


	// One bit, and only because an htmx request must never be served an entry an ordinary request stored:
	// the same URL serves both and their bodies differ. Nothing writes into the htmx key space, since an htmx
	// request caches nothing, so the bit is what makes "stores nothing" also mean "is served nothing".
	//
	// Nothing else about the request belongs here. Naming individual dimensions was incomplete; keying on
	// every HX-* header made the key unbounded attacker-controlled input with a forgeable encoding; letting
	// the author declare it was sound but left the feature inert beneath the documented layout. Caching htmx
	// responses is #236's problem, not a dimension to be guessed at here.
	internal string CurrentRepresentation()
		=> httpContext.GetHtmxContext().Request.IsHtmxRequest ? "htmx" : "ordinary";

	// Mirrors stock EndpointComponentState's position computation: multiple CacheView components under one
	// parent must not share a key. The result is deliberately not equal to stock's, because the representation
	// is appended above, and stock's SSRRenderModeBoundary GetComponentKey override is not mirrored.
	private string ComputeCacheViewTreePositionKey(
		ComponentState parentComponentState, CacheView target, string ancestorTypeName, ComponentState state)
	{
		// Read inside the factory rather than at component-state creation: an ancestor's parameters are set
		// before its children resolve their entries, but not before this state is constructed, and a render
		// mode can be decided per request.
		var beneathRequestVarying = HasUncacheableAncestor(state);
		var frames = GetCurrentRenderTreeFrames(parentComponentState.ComponentId);
		for (var index = 0; index < frames.Count; index++)
		{
			ref var frame = ref frames.Array[index];
			if (frame.FrameType is RenderTreeFrameType.Component && ReferenceEquals(frame.Component, target))
			{
				return HtmxorEndpointCandidateCacheViewServices.ComputeTreePositionKey(
					ancestorTypeName, frame.Sequence, frame.ComponentKey, CurrentRepresentation(), beneathRequestVarying);
			}
		}

		throw new InvalidOperationException(
			$"Could not locate the CacheView in the render tree of its parent component '{parentComponentState.Component?.GetType().FullName}' while computing its cache key.");
	}
}
#endif
