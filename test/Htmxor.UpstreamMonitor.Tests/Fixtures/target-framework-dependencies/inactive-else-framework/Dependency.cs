namespace App;
#if NET9_0
public class ComponentBase;
public class Dependency : ComponentBase;
#else
public class Dependency : Microsoft.AspNetCore.Components.ComponentBase;
#endif
