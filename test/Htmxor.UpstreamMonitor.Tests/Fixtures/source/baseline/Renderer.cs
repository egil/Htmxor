namespace Microsoft.AspNetCore.Components.RenderTree;

public abstract class Renderer
{
	protected virtual void ProcessRenderQueue() => ProcessPendingRender();

	private void ProcessPendingRender()
	{
	}
}
