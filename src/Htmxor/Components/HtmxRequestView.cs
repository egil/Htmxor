using Htmxor.Http;
using Microsoft.AspNetCore.Components;

namespace Htmxor.Components;

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
            builder.AddComponentParameter(2, nameof(CascadingValue<HtmxRequestViewContext>.ChildContent), ChildContent?.Invoke(ctx) ?? EmptyRenderFragment);
            builder.CloseComponent();
        });
        return Task.CompletedTask;
    }
}
