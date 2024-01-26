using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Htmxor.Configuration;

public class HtmxConfigHeadOutlet : IComponent
{
    private string _jsonConfig = string.Empty;

    [Inject] private IOptionsSnapshot<HtmxConfig> Options { get; set; } = default!;

    /// <summary>
    /// Specify a named configuration to use a non-default configuration
    /// </summary>
    [Parameter] public string? Configuration { get; set; } = default!;

    public void Attach(RenderHandle renderHandle)
    {
        renderHandle.Render(builder =>
        {
            builder.AddMarkupContent(0, @$"<meta name=""htmx-config"" content='{_jsonConfig}'>");
        });
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        Configuration = parameters.GetValueOrDefault<string?>("Configuration");

        var config = string.IsNullOrEmpty(Configuration) ?
            Options.Value : Options.Get(Configuration);

        _jsonConfig = JsonSerializer.Serialize(config, HtmxConfig.SerializerOptions);

        return Task.CompletedTask;
    }
}
