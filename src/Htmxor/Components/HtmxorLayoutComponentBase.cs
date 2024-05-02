using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Htmxor.Components;

public class HtmxorLayoutComponentBase : LayoutComponentBase, IConditionalOutputComponent
{
	public bool ShouldOutput(int conditionalChildren)
		=> conditionalChildren == 0;

	protected override void BuildRenderTree([NotNull] RenderTreeBuilder builder)
		=> builder.AddContent(0, Body);
}
