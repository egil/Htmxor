// Public DynamicComponent.Instance is read after initial quiescence at the exact framework pins
// inventoried in docs/engineering/candidate-form-adapter.md; no separate page instance is created.
// Htmxor upstream dependency: src/Components/Components/src/DynamicComponent.cs | depends-on-behaviour

using Htmxor.Builder;
using Microsoft.AspNetCore.Components;

namespace Htmxor.Endpoints;

internal sealed class HtmxorComponentActionRequest : IHtmxorGeneratedComponentActionRequest
{
	private HtmxorComponentActionDescriptor? activeDescriptor;
	internal DynamicComponent? ExplicitRouteCarrier { get; private set; }

	internal void CaptureExplicitRouteCarrier(DynamicComponent carrier)
	{
		if (ExplicitRouteCarrier is not null && !ReferenceEquals(ExplicitRouteCarrier, carrier))
		{
			throw new InvalidOperationException("The direct action route created multiple component carriers.");
		}
		ExplicitRouteCarrier = carrier;
	}

	internal bool HasActiveAction => Volatile.Read(ref activeDescriptor) is not null;

	internal bool HasExplicitAction => Volatile.Read(ref activeDescriptor)?.ExplicitHandler is not null;

	internal HtmxorComponentActionDescriptor? TryConsumeExplicit()
	{
		var descriptor = Volatile.Read(ref activeDescriptor);
		return descriptor?.ExplicitHandler is not null && TryConsume(descriptor) ? descriptor : null;
	}

	public void Activate(HtmxorComponentActionDescriptor descriptor)
	{
		ArgumentNullException.ThrowIfNull(descriptor);
		if (Interlocked.CompareExchange(ref activeDescriptor, descriptor, null) is not null)
		{
			throw new InvalidOperationException("Only one component action can be active during a request.");
		}
	}

	public bool TryConsume(HtmxorComponentActionDescriptor descriptor)
	{
		ArgumentNullException.ThrowIfNull(descriptor);
		return ReferenceEquals(
			Interlocked.CompareExchange(ref activeDescriptor, null, descriptor),
			descriptor);
	}

	bool IHtmxorGeneratedComponentActionRequest.TryConsume(HtmxorGeneratedComponentAction action)
	{
		ArgumentNullException.ThrowIfNull(action);
		var descriptor = Volatile.Read(ref activeDescriptor);
		return descriptor is not null &&
			ReferenceEquals(descriptor.GeneratedAction, action) &&
			ReferenceEquals(
				Interlocked.CompareExchange(ref activeDescriptor, null, descriptor),
				descriptor);
	}
}
