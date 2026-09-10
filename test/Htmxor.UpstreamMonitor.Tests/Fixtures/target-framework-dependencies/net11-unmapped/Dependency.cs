namespace App;

#if NET11_0
public interface Dependency : Microsoft.AspNetCore.SignalR.IHubContext<Microsoft.AspNetCore.SignalR.Hub>;
#endif
