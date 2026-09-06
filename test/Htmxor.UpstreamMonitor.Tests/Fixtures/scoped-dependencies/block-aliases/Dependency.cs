namespace First
{
    using Base = System.Object;
    public class Ordinary : Base { }
}
namespace Second
{
    using Base = Microsoft.AspNetCore.Components.ComponentBase;
    public class Dependency : Base { }
}
