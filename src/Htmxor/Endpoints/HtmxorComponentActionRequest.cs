using Htmxor.Builder;

namespace Htmxor.Endpoints;

internal sealed class HtmxorComponentActionRequest : IHtmxorGeneratedComponentActionRequest
{
	private HtmxorComponentActionDescriptor? activeDescriptor;

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
