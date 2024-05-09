using System.Diagnostics;
using Htmxor.Components;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Htmxor.Rendering;

internal class HtmxorComponentState : ComponentState
{
	private readonly HtmxorComponentState? parentComponentState;
	private readonly HtmxContext htmxContext;
	private int directConditionalChildrenCount;
	private int conditionalChildrenCount;
	private IConditionalRender? conditionalOutput;
	private bool isDisposed;

	public HtmxorComponentState(HtmxorRenderer renderer, int componentId, IComponent component, HtmxorComponentState? parentComponentState)
		: base(renderer, componentId, component, parentComponentState)
	{
		htmxContext = renderer.HtmxContext!;
		if (component is IConditionalRender conditionalOutput)
		{
			this.conditionalOutput = conditionalOutput;
			parentComponentState?.ConditionalChildAdded(true);
		}

		this.parentComponentState = parentComponentState;
	}

	public override ValueTask DisposeAsync()
	{
		if (parentComponentState is not null && conditionalOutput is not null && !isDisposed)
		{
			parentComponentState.ConditionalChildDisposed(true);
		}

		isDisposed = true;

		return base.DisposeAsync();
	}

	private void ConditionalChildAdded(bool direct)
	{
		if (direct)
		{
			directConditionalChildrenCount++;
		}

		conditionalChildrenCount++;
		parentComponentState?.ConditionalChildAdded(false);
	}

	private void ConditionalChildDisposed(bool direct)
	{
		if (direct)
		{
			directConditionalChildrenCount--;
		}

		conditionalChildrenCount--;
		parentComponentState?.ConditionalChildDisposed(false);
		Debug.Assert(conditionalChildrenCount >= 0, "conditionalChildrenCount should never be able to be less than zero");
	}

	internal bool ShouldGenerateMarkup()
		=> conditionalOutput?.ShouldOutput(htmxContext, directConditionalChildrenCount, conditionalChildrenCount)
		?? parentComponentState?.ShouldGenerateMarkup()
		?? true;
}
