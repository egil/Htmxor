using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Htmxor.Http;

public class TriggerBuilderTests
{
	[Fact]
	public void TriggerBuilder_OnEvent_CreatesCorrectTrigger()
	{
		// Arrange
		var eventName = "click";

		// Act
		var builder = new TriggerBuilder().OnEvent(eventName);
		var (triggerString, _) = builder.Build();

		// Assert
		triggerString.Should().Be("click");
	}

	[Fact]
	public void TriggerBuilder_Sse_CreatesCorrectTrigger()
	{
		// Arrange
		var sseEvent = "message";

		// Act
		var builder = new TriggerBuilder().Sse(sseEvent);
		var (triggerString, _) = builder.Build();

		// Assert
		triggerString.Should().Be("sse sseEvent:message");
	}

	[Fact]
	public void TriggerBuilder_Load_CreatesCorrectTrigger()
	{
		// Arrange
		// Act
		var builder = new TriggerBuilder().Load();
		var (triggerString, _) = builder.Build();

		// Assert
		triggerString.Should().Be("load");
	}

	[Fact]
	public void TriggerBuilder_Revealed_CreatesCorrectTrigger()
	{
		// Arrange
		// Act
		var builder = new TriggerBuilder().Revealed();
		var (triggerString, _) = builder.Build();

		// Assert
		triggerString.Should().Be("revealed");
	}

	[Fact]
	public void TriggerBuilder_Intersect_CreatesCorrectTrigger()
	{
		// Arrange
		var root = ".container";
		var threshold = 0.5f;

		// Act
		var builder = new TriggerBuilder().Intersect(root, threshold);
		var (triggerString, _) = builder.Build();

		// Assert
		triggerString.Should().Be("intersect root:.container threshold:0.5");
	}

	[Fact]
	public void TriggerBuilder_Every_CreatesCorrectTrigger()
	{
		// Arrange
		var interval = TimeSpan.FromSeconds(5);

		// Act
		var builder = new TriggerBuilder().Every(interval);
		var (triggerString, _) = builder.Build();

		// Assert
		triggerString.Should().Be("every 5s");
	}

	[Fact]
	public void TriggerBuilder_Custom_CreatesCorrectTrigger()
	{
		// Arrange
		var customTrigger = "custom-event delay:2s";

		// Act
		var builder = new TriggerBuilder().Custom(customTrigger);
		var (triggerString, _) = builder.Build();

		// Assert
		triggerString.Should().Be("custom-event delay:2s");
	}

	[Fact]
	public void TriggerModifierBuilder_WithCondition_AddsCorrectCondition()
	{
		// Arrange
		var builder = new TriggerBuilder().OnEvent("click");

		// Act
		var (triggerString, modifiers) = builder.WithCondition("ctrlKey").Build();

		// Assert
		triggerString.Should().Be("click[ctrlKey]");
	}

	[Fact]
	public void TriggerModifierBuilder_Once_AddsOnceModifier()
	{
		// Arrange
		var builder = new TriggerBuilder().OnEvent("click");

		// Act
		var (triggerString, modifiers) = builder.Once().Build();

		// Assert
		triggerString.Should().Be("click once");
	}

	[Fact]
	public void TriggerModifierBuilder_Changed_AddsChangedModifier()
	{
		// Arrange
		var builder = new TriggerBuilder().OnEvent("keyup");

		// Act
		var (triggerString, modifiers) = builder.Changed().Build();

		// Assert
		triggerString.Should().Be("keyup changed");
	}

	[Fact]
	public void TriggerModifierBuilder_Delay_AddsDelayModifier()
	{
		// Arrange
		var builder = new TriggerBuilder().OnEvent("click");

		// Act
		var (triggerString, modifiers) = builder.Delay(TimeSpan.FromSeconds(1)).Build();

		// Assert
		triggerString.Should().Be("click delay:1s");
	}

	[Fact]
	public void TriggerModifierBuilder_Throttle_AddsThrottleModifier()
	{
		// Arrange
		var builder = new TriggerBuilder().OnEvent("click");

		// Act
		var (triggerString, modifiers) = builder.Throttle(TimeSpan.FromSeconds(2)).Build();

		// Assert
		triggerString.Should().Be("click throttle:2s");
	}

	[Fact]
	public void TriggerModifierBuilder_From_AddsFromModifier()
	{
		// Arrange
		var builder = new TriggerBuilder().OnEvent("keydown");

		// Act
		var (triggerString, modifiers) = builder.From("document").Build();

		// Assert
		triggerString.Should().Be("keydown from:document");
	}

	[Fact]
	public void TriggerModifierBuilder_Target_AddsTargetModifier()
	{
		// Arrange
		var builder = new TriggerBuilder().OnEvent("click");

		// Act
		var (triggerString, _) = builder.Target(".child").Build();

		// Assert
		triggerString.Should().Be("click target:.child");
	}

	[Fact]
	public void TriggerModifierBuilder_Consume_AddsConsumeModifier()
	{
		// Arrange
		var builder = new TriggerBuilder().OnEvent("click");

		// Act
		var (triggerString, _) = builder.Consume().Build();

		// Assert
		triggerString.Should().Be("click consume");
	}

	[Fact]
	public void TriggerModifierBuilder_Queue_AddsQueueModifier()
	{
		// Arrange
		var builder = new TriggerBuilder().OnEvent("click");

		// Act
		var (triggerString, _) = builder.Queue(TriggerQueueOption.All).Build();

		// Assert
		triggerString.Should().Be("click queue:all");
	}

	[Fact]
	public void TriggerBuilder_ChainedOperations_AddsCorrectModifiers()
	{
		// Arrange
		var builder = new TriggerBuilder();

		// Act
		var (triggerString, _) = builder
			.OnEvent("click")
			.WithCondition("shiftKey")
			.Delay(TimeSpan.FromMilliseconds(500))
			.Throttle(TimeSpan.FromMilliseconds(200))
			.From("window")
			.Target("#specificElement")
			.Consume()
			.Queue(TriggerQueueOption.First)
			.Build();

		// Assert
		triggerString.Should().Be("click[shiftKey] delay:500ms throttle:200ms from:window target:#specificElement consume queue:first");
	}

	[Fact]
	public void TriggerBuilder_MultipleTriggers_AddsCorrectModifiers()
	{
		// Arrange
		var builder = new TriggerBuilder();

		// Act
		var (triggerString, _) = builder
			.OnEvent("click")
			.Or()
			.Load()
			.Build();

		// Assert
		triggerString.Should().Be("click, load");
	}
}
