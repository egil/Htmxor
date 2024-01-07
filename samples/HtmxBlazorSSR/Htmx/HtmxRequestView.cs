using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HtmxBlazorSSR.Htmx;

public class HtmxRequestView : IComponent
{
    private static readonly RenderFragment EmptyRenderFragment = builder => { };
    private RenderHandle renderHandle;

    [Inject] private HtmxContext Context { get; set; } = default!;

    [Parameter] public RenderFragment<HtmxRequestViewContext>? ChildContent { get; set; }

    public void Attach(RenderHandle renderHandle) => this.renderHandle = renderHandle;

    public Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        var ctx = new HtmxRequestViewContext(Context);
        renderHandle.Render(builder =>
        {
            builder.OpenComponent<CascadingValue<HtmxRequestViewContext>>(0);
            builder.AddComponentParameter(1, nameof(CascadingValue<HtmxRequestViewContext>.Value), ctx);
            builder.AddComponentParameter(2, nameof(CascadingValue<HtmxRequestViewContext>.ChildContent), (ChildContent?.Invoke(ctx) ?? EmptyRenderFragment));
            builder.CloseComponent();
        });
        return Task.CompletedTask;
    }
}

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

public class HtmxContent : IComponent
{
    private RenderHandle renderHandle;

    [CascadingParameter] private HtmxRequestViewContext ViewContext { get; set; } = default!;

    [Parameter, EditorRequired] public required RenderFragment ChildContent { get; set; }

    public void Attach(RenderHandle renderHandle) => this.renderHandle = renderHandle;

    public Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        ViewContext.SetHtmxContent(ChildContent);
        if (ViewContext.Context.Request.IsHtmxRequest)
        {
            renderHandle.Render(ChildContent);
        }
        return Task.CompletedTask;
    }
}

public class FullPageContent : IComponent
{
    private RenderHandle renderHandle;

    [CascadingParameter] private HtmxRequestViewContext ViewContext { get; set; } = default!;

    [Parameter, EditorRequired] public required RenderFragment ChildContent { get; set; }

    public void Attach(RenderHandle renderHandle) => this.renderHandle = renderHandle;

    public Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        if (!ViewContext.Context.Request.IsHtmxRequest)
        {
            renderHandle.Render(ChildContent);
        }
        return Task.CompletedTask;
    }
}
