using System.Diagnostics;
using Htmxor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Htmxor.Rendering;

internal class HtmxorComponentState : ComponentState
{
	private readonly HtmxorComponentState? parentComponentState;
	private int conditionalChildrenCount;
	private IConditionalOutputComponent? conditionalOutput;
	private bool isDisposed;

	public HtmxorComponentState(HtmxorRenderer renderer, int componentId, IComponent component, HtmxorComponentState? parentComponentState)
		: base(renderer, componentId, component, parentComponentState)
	{
		if (component is IConditionalOutputComponent conditionalOutput)
		{
			this.conditionalOutput = conditionalOutput;
			parentComponentState?.ConditionalChildAdded();
		}

		this.parentComponentState = parentComponentState;
	}

	public override ValueTask DisposeAsync()
	{
		if (parentComponentState is not null && conditionalOutput is not null && !isDisposed)
		{
			parentComponentState.ConditionalChildDisposed();
		}

		isDisposed = true;

		return base.DisposeAsync();
	}

	private void ConditionalChildAdded()
	{
		conditionalChildrenCount++;
		parentComponentState?.ConditionalChildAdded();
	}

	private void ConditionalChildDisposed()
	{
		conditionalChildrenCount--;
		parentComponentState?.ConditionalChildDisposed();
		Debug.Assert(conditionalChildrenCount >= 0, "conditionalChildrenCount should never be able to be less than zero");
	}

	internal bool ShouldGenerateMarkup()
		=> conditionalOutput?.ShouldOutput(conditionalChildrenCount)
		?? parentComponentState?.ShouldGenerateMarkup()
		?? true;
}
