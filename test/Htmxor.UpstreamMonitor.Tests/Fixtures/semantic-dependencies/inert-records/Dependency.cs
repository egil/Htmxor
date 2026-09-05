namespace App;
// public record struct Example : Microsoft.AspNetCore.Components.IComponent;
/* public record class Example(int Value) : Microsoft.AspNetCore.Components.IComponent; */
public class Dependency
{
    public const string Example = "public record struct Example : Microsoft.AspNetCore.Components.IComponent;";
    public const string Raw = """
        using Microsoft.AspNetCore.Components;
        namespace App { using Base = ComponentBase; public class Example : Base; }
        public record class Example(int Value) : Microsoft.AspNetCore.Components.IComponent;
        """;
}
