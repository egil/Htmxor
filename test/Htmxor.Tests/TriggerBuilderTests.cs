using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Htmxor;

public class TriggerBuilderTests
{
	[Fact]
	public void TriggerBuilder_OnEvent_CreatesCorrectTrigger()
	{
		var sut = new TriggerBuilder();
		var eventName = "click";

		var (triggerString, _) = sut.OnEvent(eventName).Build();

		triggerString.Should().Be(eventName);
	}

	[Fact]
	public void TriggerBuilder_Sse_CreatesCorrectTrigger()
	{
		var sut = new TriggerBuilder();
		var sseEvent = "message";

		var (triggerString, _) = sut.Sse(sseEvent).Build();

		triggerString.Should().Be("sse sseEvent:message");
	}

	[Fact]
	public void TriggerBuilder_Load_CreatesCorrectTrigger()
	{
		var sut = new TriggerBuilder();

		var (triggerString, _) = sut.Load().Build();

		triggerString.Should().Be("load");
	}

	[Fact]
	public void TriggerBuilder_Revealed_CreatesCorrectTrigger()
	{
		var sut = new TriggerBuilder();

		var (triggerString, _) = sut.Revealed().Build();

		triggerString.Should().Be("revealed");
	}

	[Fact]
	public void TriggerBuilder_Intersect_CreatesCorrectTrigger()
	{
		var root = ".container";
		var threshold = 0.5f;
		var sut = new TriggerBuilder();

		var (triggerString, _) = sut.Intersect(root, threshold).Build();

		triggerString.Should().Be("intersect root:.container threshold:0.5");
	}

	[Fact]
	public void TriggerBuilder_Every_CreatesCorrectTrigger()
	{
		var interval = TimeSpan.FromSeconds(5);
		var sut = new TriggerBuilder();

		var (triggerString, _) = sut.Every(interval).Build();

		triggerString.Should().Be("every 5s");
	}

	[Fact]
	public void TriggerBuilder_Custom_CreatesCorrectTrigger()
	{
		var customTrigger = "custom-event delay:2s";
		var sut = new TriggerBuilder();

		var (triggerString, _) = sut.Custom(customTrigger).Build();

		triggerString.Should().Be("custom-event delay:2s");
	}

	[Fact]
	public void TriggerModifierBuilder_WithCondition_AddsCorrectCondition()
	{
		var sut = new TriggerBuilder().OnEvent("click");

		var (triggerString, modifiers) = sut.WithCondition("ctrlKey").Build();

		triggerString.Should().Be("click[ctrlKey]");
	}

	[Fact]
	public void TriggerModifierBuilder_Once_AddsOnceModifier()
	{
		var sut = new TriggerBuilder().OnEvent("click");

		var (triggerString, modifiers) = sut.Once().Build();

		triggerString.Should().Be("click once");
	}

	[Fact]
	public void TriggerModifierBuilder_Changed_AddsChangedModifier()
	{
		var sut = new TriggerBuilder().OnEvent("keyup");

		var (triggerString, modifiers) = sut.Changed().Build();

		triggerString.Should().Be("keyup changed");
	}

	[Fact]
	public void TriggerModifierBuilder_Delay_AddsDelayModifier()
	{
		var sut = new TriggerBuilder().OnEvent("click");

		var (triggerString, modifiers) = sut.Delay(TimeSpan.FromSeconds(1)).Build();

		triggerString.Should().Be("click delay:1s");
	}

	[Fact]
	public void TriggerModifierBuilder_Throttle_AddsThrottleModifier()
	{
		var sut = new TriggerBuilder().OnEvent("click");

		var (triggerString, modifiers) = sut.Throttle(TimeSpan.FromSeconds(2)).Build();

		triggerString.Should().Be("click throttle:2s");
	}

	[Fact]
	public void TriggerModifierBuilder_From_AddsFromModifier()
	{
		var sut = new TriggerBuilder().OnEvent("keydown");

		var (triggerString, modifiers) = sut.From("document").Build();

		triggerString.Should().Be("keydown from:document");
	}

	[Fact]
	public void TriggerModifierBuilder_Target_AddsTargetModifier()
	{
		var sut = new TriggerBuilder().OnEvent("click");

		var (triggerString, _) = sut.Target(".child").Build();

		triggerString.Should().Be("click target:.child");
	}

	[Fact]
	public void TriggerModifierBuilder_Consume_AddsConsumeModifier()
	{
		var builder = new TriggerBuilder().OnEvent("click");

		var (triggerString, _) = builder.Consume().Build();

		triggerString.Should().Be("click consume");
	}

	[Fact]
	public void TriggerModifierBuilder_Queue_AddsQueueModifier()
	{
		var sut = new TriggerBuilder().OnEvent("click");

		var (triggerString, _) = sut.Queue(TriggerQueueOption.All).Build();

		triggerString.Should().Be("click queue:all");
	}

	[Fact]
	public void TriggerBuilder_ChainedOperations_AddsCorrectModifiers()
	{
		var sut = new TriggerBuilder();

		var (triggerString, _) = sut
			.OnEvent("click")
			.WithCondition("shiftKey")
			.Delay(TimeSpan.FromMilliseconds(500))
			.Throttle(TimeSpan.FromMilliseconds(200))
			.From("window")
			.Target("#specificElement")
			.Consume()
			.Queue(TriggerQueueOption.First)
			.Build();

		triggerString.Should().Be("click[shiftKey] delay:500ms throttle:200ms from:window target:#specificElement consume queue:first");
	}

	[Fact]
	public void TriggerBuilder_MultipleTriggers_AddsCorrectModifiers()
	{
		var sut = new TriggerBuilder();

		var (triggerString, _) = sut
			.OnEvent("click")
			.Or()
			.Load()
			.Build();

		triggerString.Should().Be("click, load");
	}
}
