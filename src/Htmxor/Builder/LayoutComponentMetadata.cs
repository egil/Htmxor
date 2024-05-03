namespace Htmxor.Builder;

internal sealed class LayoutComponentMetadata([DynamicallyAccessedMembers(LinkerFlags.Component)] Type layoutType)
{
	public Type Type { get; } = layoutType;
}
