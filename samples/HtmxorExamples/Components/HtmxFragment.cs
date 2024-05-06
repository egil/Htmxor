using System.Diagnostics.CodeAnalysis;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Htmxor.Components;

public class HtmxFragment : ConditionalComponentBase
{
	[Parameter, EditorRequired]
	public required RenderFragment ChildContent { get; set; }

	[Parameter]
	public Func<HtmxRequest, bool>? Match { get; set; }

	[Parameter]
	public bool OnStandardRequest { get; set; } = true;

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		if (ShouldOutput(Context, 0, 0))
		{
			builder.AddContent(0, ChildContent);
		}
	}

	public override bool ShouldOutput([NotNull] HtmxContext context, int directConditionalChildren, int conditionalChildren)
		=> (OnStandardRequest && context.Request.RoutingMode is RoutingMode.Standard)
		|| (Match?.Invoke(context.Request) ?? true);

}
