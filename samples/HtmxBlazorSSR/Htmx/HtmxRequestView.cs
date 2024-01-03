using Microsoft.AspNetCore.Components;

namespace HtmxBlazorSSR.Htmx;

public class HtmxRequestView : IComponent
{
    private static readonly RenderFragment EmptyRenderFragment = builder => { };

    private RenderHandle renderHandler;

    [Inject] private HtmxContext Context { get; set; } = default!;

    [Parameter] public RenderFragment<HtmxContext>? ChildContent { get; set; }

    [Parameter] public RenderFragment<HtmxContext>? HtmxContent { get; set; }

    [Parameter] public RenderFragment? RegularContent { get; set; }

    public void Attach(RenderHandle renderHandle) => this.renderHandler = renderHandle;

    public Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (Context.Request.IsHtmxRequest && Context.Request.Trigger is not null)
        {
            renderHandler.Render((ChildContent ?? HtmxContent)?.Invoke(Context) ?? EmptyRenderFragment);
        }
        else
        {
            renderHandler.Render((RegularContent ?? EmptyRenderFragment));
        }

        return Task.CompletedTask;
    }
}
