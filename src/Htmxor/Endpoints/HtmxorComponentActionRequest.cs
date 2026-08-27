using Htmxor.Builder;

namespace Htmxor.Endpoints;

internal sealed class HtmxorComponentActionRequest
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
}
