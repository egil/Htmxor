using Microsoft.AspNetCore.Components;
namespace App { using Base = ComponentBase; public class Unrelated; }
namespace App { using Base = Project.ComponentBase; public class Dependency : Base; }
