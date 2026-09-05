namespace First
{
    using Base = Microsoft.AspNetCore.Components.ComponentBase;
    public class Ordinary { }
}
namespace Second
{
    using Base = System.Object;
    public class Dependency : Base { }
}
