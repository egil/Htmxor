using System.Diagnostics.CodeAnalysis;

namespace Htmxor.Builder;

internal sealed class HtmxorLayoutComponentMetadata([DynamicallyAccessedMembers(LinkerFlags.Component)] Type layoutType)
{
	public Type Type { get; } = layoutType;
}
