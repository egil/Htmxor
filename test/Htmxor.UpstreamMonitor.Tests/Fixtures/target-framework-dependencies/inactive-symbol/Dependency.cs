namespace App;
#if TARGET_FRAMEWORK_SYMBOL
public class Dependency : Microsoft.AspNetCore.Components.ComponentBase;
#else
public class ComponentBase;
public class Dependency : ComponentBase;
#endif
