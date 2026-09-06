namespace Fixture
{
    using Base = System.Object;
    namespace Nested
    {
        using Base = Microsoft.AspNetCore.Components.ComponentBase;
        public class Dependency : Base { }
    }
}
