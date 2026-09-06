namespace App;
public record struct Dependency : Microsoft.AspNetCore.Components.IComponent
{
    public void Attach(Microsoft.AspNetCore.Components.RenderHandle handle) { }
    public System.Threading.Tasks.Task SetParametersAsync(Microsoft.AspNetCore.Components.ParameterView parameters)
        => System.Threading.Tasks.Task.CompletedTask;
}
