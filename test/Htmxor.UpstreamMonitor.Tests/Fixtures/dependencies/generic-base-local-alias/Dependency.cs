using LocalInput = Htmxor.UpstreamMonitor.Tests.BaseSyntaxFixtures.GenericBaseLocalAlias.InputBase<string>;

namespace Htmxor.UpstreamMonitor.Tests.BaseSyntaxFixtures.GenericBaseLocalAlias;

internal sealed class Dependency() : LocalInput()
{
}

internal class InputBase<T>
{
}
