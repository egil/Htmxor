using Project;
namespace App;
public record struct Dependency(int Value) : IComponent;
public record class OtherDependency : IComponent;
