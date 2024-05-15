namespace Htmxor;

public class SwapStyleBuilderTests
{
	[Fact]
	public void SwapStyleBuilder_InitializeAndBuild_ReturnsCorrectValues()
	{
		var swapStyle = SwapStyle.innerHTML;
		var sut = new SwapStyleBuilder(swapStyle);

		var (resultStyle, modifiers) = sut.Build();

		resultStyle.Should().Be(swapStyle);
		modifiers.Should().BeEmpty();  // Expect no modifiers if none are added
	}

	[Fact]
	public void SwapStyleBuilder_AfterSwap_AddsCorrectDelay()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);

		var (_, modifiers) = sut.AfterSwapDelay(TimeSpan.FromSeconds(1)).Build();

		modifiers.Should().Be("swap:1s");
	}

	[Fact]
	public void SwapStyleBuilder_AfterSettle_AddsCorrectDelay()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);

		var (_, modifiers) = sut.AfterSettleDelay(TimeSpan.FromSeconds(1)).Build();

		modifiers.Should().Be("settle:1s");
	}

	[Fact]
	public void SwapStyleBuilder_Scroll_AddsCorrectDirection()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);

		var (_, modifiers) = sut.Scroll(ScrollDirection.bottom).Build();

		modifiers.Should().Be("scroll:bottom");
	}

	[Fact]
	public void SwapStyleBuilder_IgnoreTitle_AddsCorrectFlag()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);

		var (_, modifiers) = sut.IgnoreTitle(true).Build();

		modifiers.Should().Be("ignoreTitle:true");
	}

	[Fact]
	public void SwapStyleBuilder_Transition_AddsCorrectFlag()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);

		var (_, modifiers) = sut.Transition(true).Build();

		modifiers.Should().Be("transition:true");
	}

	[Fact]
	public void SwapStyleBuilder_ScrollFocus_AddsCorrectFlag()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);

		var (_, modifiers) = sut.ScrollFocus(true).Build();

		modifiers.Should().Be("focus-scroll:true");
	}

	[Fact]
	public void SwapStyleBuilder_ShowOn_AddsCorrectSelectorAndDirection()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);
		var selector = "#dynamic-area";

		var (_, modifiers) = sut.ShowOn(ScrollDirection.top, selector).Build();

		modifiers.Should().Be("show:#dynamic-area:top");
	}

	[Fact]
	public void SwapStyleBuilder_ShowWindow_AddsCorrectWindowAndDirection()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);

		var (_, modifiers) = sut.ShowWindow(ScrollDirection.top).Build();

		modifiers.Should().Be("show:window:top");
	}

	[Fact]
	public void SwapStyleBuilder_ChainedOperations_AddsCorrectModifiers()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);

		var (_, modifiers) = sut
			.AfterSwapDelay(TimeSpan.FromSeconds(1))
			.Scroll(ScrollDirection.top)
			.Transition(true)
			.IgnoreTitle(false)
			.Build();

		modifiers.Should().Be("swap:1s scroll:top transition:true ignoreTitle:false");
	}

	[Fact]
	public void SwapStyleBuilder_After_With250Milliseconds_AddsCorrectDelay()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);

		var (_, modifiers) = sut.AfterSwapDelay(TimeSpan.FromMilliseconds(250)).Build();

		modifiers.Should().Be("swap:250ms");
	}

	[Fact]
	public void SwapStyleBuilder_ShowOn_BottomDirection_AddsCorrectModifier()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);
		var selector = "#element-id";

		var (_, modifiers) = sut.ShowOn(ScrollDirection.bottom, selector).Build();

		modifiers.Should().Be("show:#element-id:bottom");
	}

	[Fact]
	public void SwapStyleBuilder_ShowWindow_BottomDirection_AddsCorrectModifier()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);

		var (_, modifiers) = sut.ShowWindow(ScrollDirection.bottom).Build();

		modifiers.Should().Be("show:window:bottom");
	}

	[Fact]
	public void SwapStyleBuilder_NullSwapStyle_ReturnsNullStyle()
	{
		var sut = new SwapStyleBuilder();

		var (style, _) = sut.Build();

		style.Should().Be(SwapStyle.Default);
	}

	[Fact]
	public void SwapStyleBuilder_ShowNone_ReturnsCorrectValue()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);

		var (_, modifiers) = sut.ShowNone().Build();

		modifiers.Should().Be("show:none");
	}

	[Fact]
	public void SwapStyleBuilder_ShowOnTop_ReturnsCorrectValue()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);

		var (_, modifiers) = sut.ShowOnTop().Build();

		modifiers.Should().Be("show:top");
	}

	[Fact]
	public void SwapStyleBuilder_ShowOnBottom_ReturnsCorrectValue()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);

		var (_, modifiers) = sut.ShowOnBottom().Build();

		modifiers.Should().Be("show:bottom");
	}

	[Fact]
	public void SwapStyleBuilder_MixedShowOverrides_ReturnsCorrectValue()
	{
		var sut = new SwapStyleBuilder(SwapStyle.innerHTML);

		var (_, modifiers) = sut.ShowOnTop()
			.AfterSettleDelay(TimeSpan.FromMilliseconds(250))
			.ShowWindowTop()
			.ShowOnBottom()
			.Build();

		modifiers.Should().Be("settle:250ms show:bottom");
	}
}
