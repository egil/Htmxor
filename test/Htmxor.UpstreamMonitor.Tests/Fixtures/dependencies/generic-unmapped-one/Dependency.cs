using Microsoft.AspNetCore.SignalR;

namespace Htmxor.UpstreamMonitor.Tests.BaseSyntaxFixtures.GenericUnmappedOne;

internal interface Dependency : IHubContext<Hub>
{
}
