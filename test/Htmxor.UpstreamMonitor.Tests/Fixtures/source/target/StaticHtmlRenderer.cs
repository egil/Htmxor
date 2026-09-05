namespace Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;

public abstract class StaticHtmlRenderer<T> : RendererV2 where T : notnull, IDisposable
{
	public StaticHtmlRenderer(IServiceProvider services)
	{
	}

	protected abstract ValueTask RenderAsync(T value);

	protected virtual bool CanRender(T value) => true;
}
