using Microsoft.AspNetCore.Components;
namespace App
{
    using Base = ComponentBase;
    namespace Nested
    {
        public class ComponentBase;
        public class Dependency : Base;
    }
}
