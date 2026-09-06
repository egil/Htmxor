using Microsoft.AspNetCore.SignalR;
namespace Fixture;
public interface Dependency : IHubContext<Hub<System.IDisposable>, System.IDisposable>;
