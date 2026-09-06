namespace App;
#if NET9_0_OR_GREATER
public interface Dependency : Microsoft.AspNetCore.SignalR.IHubContext<Microsoft.AspNetCore.SignalR.Hub>;
#endif
