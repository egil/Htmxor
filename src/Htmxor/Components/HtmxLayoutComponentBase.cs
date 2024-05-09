using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Htmxor.Components;

/// <summary>
/// Htmx layout component that will render the <see cref="HeadOutlet"/> and the body content.
/// Including <see cref="HeadOutlet"/> enables components like <see cref="PageTitle"/> to work during htmx requests.
/// </summary>
public abstract class HtmxLayoutComponentBase : LayoutComponentBase, IConditionalRender
{
	/// <inheritdoc/>
	/// <remarks>The <see cref="HtmxLayoutComponentBase"/> defaults to returning <see langword="true"/>.</remarks>
	public virtual bool ShouldOutput([NotNull] HtmxContext context, int directConditionalChildren, int conditionalChildren)
		=> true;

	protected override void BuildRenderTree([NotNull] RenderTreeBuilder builder)
	{
		builder.OpenComponent<HeadOutlet>(0);
		builder.CloseComponent();
		builder.AddContent(1, Body);
	}
}
