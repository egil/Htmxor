using Htmxor.Http;
using Microsoft.AspNetCore.Components;

namespace Htmxor.Components;

public class HtmxRequestViewContext
{
    private static readonly RenderFragment EmptyRenderFragment = builder => { };

    public HtmxContext Context { get; }

    public RenderFragment HtmxContent { get; private set; } = EmptyRenderFragment;

    public HtmxRequestViewContext(HtmxContext context)
    {
        Context = context;
    }

    internal void SetHtmxContent(RenderFragment htmxContent)
        => HtmxContent = htmxContent;
}
