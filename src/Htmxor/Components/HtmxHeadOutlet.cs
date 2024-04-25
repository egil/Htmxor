using System.Text.Json;
using Htmxor.Configuration.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Components;

/// <summary>
/// This component will add the necessary script tags to load the embedded version of 
/// Htmx and related JavaScript needed for Htmxor to work.
/// It will also add a meta tag with the serialized <see cref="HtmxConfig"/> object,
/// enabling customization of Htmx.
/// </summary>
/// <remarks>
/// Configure the <see cref="HtmxConfig"/> via the 
/// <see cref="HtmxorApplicationBuilderExtensions.AddHtmx(IRazorComponentsBuilder, Action{Htmxor.HtmxConfig}?)"/> 
/// method.
/// </remarks>
public class HtmxHeadOutlet : IComponent
{
    [Inject] private HtmxConfig Config { get; set; } = default!;

    /// <summary>
    /// Gets or sets whether or not to add the scripts thats that reference the embedded version of Htmx. Default is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// If this set to <see langword="false"/>, include the <c>_content/Htmxor/htmxor.js</c> your application manually.
    /// </remarks>
    [Parameter] public bool UseEmbeddedHtmx { get; set; } = true;

    /// <inheritdoc/>
    public void Attach(RenderHandle renderHandle)
    {
        var json = JsonSerializer.Serialize(Config, HtmxJsonSerializerContext.Default.HtmxConfig);
        renderHandle.Render(builder =>
        {
            if (UseEmbeddedHtmx)
            {
                builder.AddMarkupContent(0, @"<script src=""_content/Htmxor/htmx/htmx.min.js""></script>");
                builder.AddMarkupContent(1, @"<script src=""_content/Htmxor/htmx/event-header.js""></script>");
            }

            builder.AddMarkupContent(2, @"<script src=""_content/Htmxor/htmxor.js""></script>");
            builder.AddMarkupContent(3, @$"<meta name=""htmx-config"" content='{json}'>");
        });
    }

    /// <inheritdoc/>
    public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
}
