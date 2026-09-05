namespace Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;

public abstract class StaticHtmlRenderer<T> : Renderer where T : class
{
	protected StaticHtmlRenderer(IServiceProvider services)
	{
	}

	protected virtual Task RenderAsync(T value) => Task.CompletedTask;

	public abstract string Format(T value);
}
