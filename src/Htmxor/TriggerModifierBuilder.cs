using Htmxor.Http;
using static Htmxor.Constants;
using System.Runtime.CompilerServices;

namespace Htmxor;

public sealed class TriggerModifierBuilder : ITriggerBuilder
{
	private readonly HtmxTriggerSpecification specification;
	private readonly TriggerBuilder parentBuilder;

	internal TriggerModifierBuilder(HtmxTriggerSpecification specification, TriggerBuilder parentBuilder)
	{
		this.specification = specification;
		this.parentBuilder = parentBuilder;

		parentBuilder.AddTrigger(specification);
	}

	public TriggerModifierBuilder WithCondition(string condition)
	{
		specification.EventFilter = condition;
		return this;
	}

	public TriggerModifierBuilder Once()
	{
		specification.Once = true;
		return this;
	}

	public TriggerModifierBuilder Changed()
	{
		specification.Changed = true;
		return this;
	}

	public TriggerModifierBuilder Delay(TimeSpan timing)
	{
		specification.Delay = (int)timing.TotalMilliseconds;
		return this;
	}

	public TriggerModifierBuilder Throttle(TimeSpan timing)
	{
		specification.Throttle = (int)timing.TotalMilliseconds;
		return this;
	}

	public TriggerModifierBuilder From(string selector)
	{
		specification.From = selector;
		return this;
	}

	public TriggerModifierBuilder Target(string selector)
	{
		specification.Target = selector;
		return this;
	}

	public TriggerModifierBuilder Consume()
	{
		specification.Consume = true;
		return this;
	}

	public TriggerModifierBuilder Queue(TriggerQueueOption option = TriggerQueueOption.Last)
	{
		var value = option switch
		{
			TriggerQueueOption.First => "first",
			TriggerQueueOption.Last => "last",
			TriggerQueueOption.None => "none",
			TriggerQueueOption.All => "all",
			_ => throw new SwitchExpressionException(option),
		};

		specification.Queue = value;

		return this;
	}

	public TriggerBuilder Or()
	{
		return parentBuilder;
	}

	public KeyValuePair<string, List<HtmxTriggerSpecification>> Build() => parentBuilder.Build();

	public override string ToString() => parentBuilder.ToString();
}

