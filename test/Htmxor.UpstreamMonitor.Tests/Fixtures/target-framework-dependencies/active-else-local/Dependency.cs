namespace App;
#if NET10_0_OR_GREATER
public class ComponentBase;
public class Dependency : ComponentBase;
#else
public class Dependency : Microsoft.AspNetCore.Components.ComponentBase;
#endif
