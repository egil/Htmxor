using Microsoft.AspNetCore.Components;

namespace BlazorSSR.Components.FlashMessages;

public record class FlashMessage
{
    public required RenderFragment Content { get; init; }

    public FlashMessageType Type { get; init; } = FlashMessageType.Info;
}
