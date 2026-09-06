using Microsoft.AspNetCore.SignalR;

namespace Htmxor.UpstreamMonitor.Tests.BaseSyntaxFixtures.GenericUnmappedTwo;

internal interface Dependency : IHubContext<Hub<IDisposable>, IDisposable>
{
}
