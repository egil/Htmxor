using Htmxor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Htmxor.AspNetCore10;

[HtmxLayout(typeof(Issue283.Issue283DefaultLayout), Priority = -10)]
[Layout(typeof(Issue283.Issue283StockLayout))]
public abstract class Issue283CSharpBase : ComponentBase
{
	protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddMarkupContent(0, "<p data-page>csharp</p>");
}

[Route("/issue-283/dual/csharp")]
public sealed class Issue283DualCSharp : Issue283CSharpBase;

[HtmxRoute("/issue-283/htmx/csharp", Methods = ["GET"])]
public sealed class Issue283HtmxCSharp : Issue283CSharpBase;

[Route("/issue-283/inherited")]
[HtmxLayout(typeof(Issue283.Issue283OverrideLayout), Priority = 20)]
public sealed class Issue283Inherited : Issue283CSharpBase;

[HtmxLayout(typeof(Issue283.Issue283OverrideLayout), Priority = 30)]
public abstract class Issue283HighPriorityBase : Issue283CSharpBase;

[Route("/issue-283/base-priority")]
[HtmxLayout(typeof(Issue283.Issue283DefaultLayout), Priority = 5)]
public sealed class Issue283BasePriority : Issue283HighPriorityBase;

[Route("/issue-283/plain")]
[HtmxLayout(typeof(Issue283PlainLayout))]
public sealed class Issue283PlainPage : Issue283CSharpBase;

public sealed class Issue283PlainLayout : IComponent
{
	private RenderHandle handle;
	[Parameter] public RenderFragment? Body { get; set; }
	public void Attach(RenderHandle renderHandle) => handle = renderHandle;
	public Task SetParametersAsync(ParameterView parameters)
	{
		parameters.SetParameterProperties(this);
		handle.Render(builder =>
		{
			builder.OpenElement(0, "article");
			builder.AddAttribute(1, "data-plain-layout", true);
			builder.AddContent(2, Body);
			builder.CloseElement();
		});
		return Task.CompletedTask;
	}
}

public sealed class Issue283HeadLayout : HtmxLayoutComponentBase;
