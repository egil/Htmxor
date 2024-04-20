using Htmxor.Configuration.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Htmxor.Components;

/// <summary>
/// This component will render a meta tag with the serialized <see cref="HtmxConfig"/> object,
/// enabling customization of Htmx.
/// </summary>
/// <remarks>
/// Configure the <see cref="HtmxConfig"/> via the 
/// <see cref="HtmxorApplicationBuilderExtensions.AddHtmx(IRazorComponentsBuilder, Action{Htmxor.HtmxConfig}?)"/> 
/// method.
/// </remarks>
public class HtmxConfigHeadOutlet : IComponent
{
    [Inject] private HtmxConfig Config { get; set; } = default!;

    /// <inheritdoc/>
    public void Attach(RenderHandle renderHandle)
    {
        var json = JsonSerializer.Serialize(Config, HtmxJsonSerializerContext.Default.HtmxConfig);
        renderHandle.Render(builder =>
        {
            builder.AddMarkupContent(0, @$"<meta name=""htmx-config"" content='{json}'>");
        });
    }

    /// <inheritdoc/>
    public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
}
