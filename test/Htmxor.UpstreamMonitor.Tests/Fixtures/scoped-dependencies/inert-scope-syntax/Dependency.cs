namespace Fixture;
// global using Microsoft.AspNetCore.Components;
/* namespace Hidden { using Base = Microsoft.AspNetCore.Components.ComponentBase; class Example : Base; } */
public class Dependency
{
    public const string Example = "global using Microsoft.AspNetCore.Components; class Example : ComponentBase;";
    public const string Raw = """
        namespace Hidden { using Base = Microsoft.AspNetCore.Components.ComponentBase; class Example() : Base(); }
        """;
}
