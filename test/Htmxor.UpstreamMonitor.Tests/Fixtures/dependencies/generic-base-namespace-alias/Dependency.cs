using Forms = global::Microsoft.AspNetCore.Components.Forms;

namespace Htmxor.UpstreamMonitor.Tests.BaseSyntaxFixtures.GenericBaseNamespaceAlias;

internal abstract class Dependency() : Forms.InputBase<(string Name, int Count)>()
{
}
