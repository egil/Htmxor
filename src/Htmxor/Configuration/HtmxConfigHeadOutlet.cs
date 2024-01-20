using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Htmxor.Configuration;

public class HtmxConfigHeadOutlet : IComponent
{
    [Inject] private HtmxConfig Config { get; set; } = default!;

    public void Attach(RenderHandle renderHandle)
    {
        var json = JsonSerializer.Serialize(Config, HtmxConfig.SerializerOptions);
        renderHandle.Render(builder =>
        {
            builder.AddMarkupContent(0, @$"<meta name=""htmx-config"" content='{json}'>");
        });
    }

    public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
}
