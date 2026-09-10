#if NET10_0
public class Dependency : Microsoft.AspNetCore.Components.ComponentBase { }
#elif NET11_0
public class Dependency : Microsoft.AspNetCore.Components.NavigationManager
{
	protected override void NavigateToCore(string uri, Microsoft.AspNetCore.Components.NavigationOptions options) { }
}
#endif
