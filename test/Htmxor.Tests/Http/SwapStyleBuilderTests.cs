namespace Htmxor.Http;

public class SwapStyleBuilderTests
{
	[Fact]
	public void SwapStyleBuilder_InitializeAndBuild_ReturnsCorrectValues()
	{
		// Arrange
		var swapStyle = SwapStyle.InnerHTML;

		// Act
		var builder = new SwapStyleBuilder(swapStyle);
		var (resultStyle, modifiers) = builder.Build();

		// Assert
		resultStyle.Should().Be(swapStyle);
		modifiers.Should().BeEmpty();  // Expect no modifiers if none are added
	}

	[Fact]
	public void SwapStyleBuilder_AfterSwap_AddsCorrectDelay()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

		// Act
		var (_, modifiers) = builder.AfterSwapDelay(TimeSpan.FromSeconds(1)).Build();

		// Assert
		modifiers.Should().Be("swap:1s");
	}

	[Fact]
	public void SwapStyleBuilder_AfterSettle_AddsCorrectDelay()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

		// Act
		var (_, modifiers) = builder.AfterSettleDelay(TimeSpan.FromSeconds(1)).Build();

		// Assert
		modifiers.Should().Be("settle:1s");
	}

	[Fact]
	public void SwapStyleBuilder_Scroll_AddsCorrectDirection()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

		// Act
		var (_, modifiers) = builder.Scroll(ScrollDirection.Bottom).Build();

		// Assert
		modifiers.Should().Be("scroll:bottom");
	}

	[Fact]
	public void SwapStyleBuilder_IgnoreTitle_AddsCorrectFlag()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

		// Act
		var (_, modifiers) = builder.IgnoreTitle(true).Build();

		// Assert
		modifiers.Should().Be("ignoreTitle:true");
	}

	[Fact]
	public void SwapStyleBuilder_Transition_AddsCorrectFlag()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

		// Act
		var (_, modifiers) = builder.Transition(true).Build();

		// Assert
		modifiers.Should().Be("transition:true");
	}

	[Fact]
	public void SwapStyleBuilder_ScrollFocus_AddsCorrectFlag()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

		// Act
		var (_, modifiers) = builder.ScrollFocus(true).Build();

		// Assert
		modifiers.Should().Be("focus-scroll:true");
	}

	[Fact]
	public void SwapStyleBuilder_ShowOn_AddsCorrectSelectorAndDirection()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);
		var selector = "#dynamic-area";

		// Act
		var (_, modifiers) = builder.ShowOn(ScrollDirection.Top, selector).Build();

		// Assert
		modifiers.Should().Be("show:#dynamic-area:top");
	}

	[Fact]
	public void SwapStyleBuilder_ShowWindow_AddsCorrectWindowAndDirection()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

		// Act
		var (_, modifiers) = builder.ShowWindow(ScrollDirection.Top).Build();

		// Assert
		modifiers.Should().Be("show:window:top");
	}

	[Fact]
	public void SwapStyleBuilder_ChainedOperations_AddsCorrectModifiers()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

		// Act
		var (_, modifiers) = builder
			.AfterSwapDelay(TimeSpan.FromSeconds(1))
			.Scroll(ScrollDirection.Top)
			.Transition(true)
			.IgnoreTitle(false)
			.Build();

		// Assert
		modifiers.Should().Be("swap:1s scroll:top transition:true ignoreTitle:false");
	}

	[Fact]
	public void SwapStyleBuilder_After_With250Milliseconds_AddsCorrectDelay()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

		// Act
		var (_, modifiers) = builder.AfterSwapDelay(TimeSpan.FromMilliseconds(250)).Build();

		// Assert
		modifiers.Should().Be("swap:250ms");
	}

	[Fact]
	public void SwapStyleBuilder_ShowOn_BottomDirection_AddsCorrectModifier()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);
		var selector = "#element-id";

		// Act
		var (_, modifiers) = builder.ShowOn(ScrollDirection.Bottom, selector).Build();

		// Assert
		modifiers.Should().Be("show:#element-id:bottom");
	}

	[Fact]
	public void SwapStyleBuilder_ShowWindow_BottomDirection_AddsCorrectModifier()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

		// Act
		var (_, modifiers) = builder.ShowWindow(ScrollDirection.Bottom).Build();

		// Assert
		modifiers.Should().Be("show:window:bottom");
	}

	[Fact]
	public void SwapStyleBuilder_NullSwapStyle_ReturnsNullStyle()
	{
		// Arrange & Act
		var builder = new SwapStyleBuilder();
		var (style, _) = builder.Build();

		// Assert
		style.Should().Be(SwapStyle.Default);
	}

	[Fact]
	public void SwapStyleBuilder_ShowNone_ReturnsCorrectValue()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

		// Act
		var (_, modifiers) = builder.ShowNone().Build();

		// Assert
		modifiers.Should().Be("show:none");
	}

	[Fact]
	public void SwapStyleBuilder_ShowOnTop_ReturnsCorrectValue()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

		// Act
		var (_, modifiers) = builder.ShowOnTop().Build();

		// Assert
		modifiers.Should().Be("show:top");
	}

	[Fact]
	public void SwapStyleBuilder_ShowOnBottom_ReturnsCorrectValue()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

		// Act
		var (_, modifiers) = builder.ShowOnBottom().Build();

		// Assert
		modifiers.Should().Be("show:bottom");
	}

	[Fact]
	public void SwapStyleBuilder_MixedShowOverrides_ReturnsCorrectValue()
	{
		// Arrange
		var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

		// Act
		var (_, modifiers) = builder.ShowOnTop().AfterSettleDelay(TimeSpan.FromMilliseconds(250)).ShowWindowTop().ShowOnBottom().Build();

		// Assert
		modifiers.Should().Be("settle:250ms show:bottom");
	}
}
