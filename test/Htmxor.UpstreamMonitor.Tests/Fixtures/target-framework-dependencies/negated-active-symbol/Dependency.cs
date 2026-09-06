namespace App;
#if !NET10_0
public class Dependency : Microsoft.AspNetCore.Components.ComponentBase;
#else
public class ComponentBase;
public class Dependency : ComponentBase;
#endif
