using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bunit;

namespace Htmxor.Http;

public class SwapStyleBuilderTests : TestContext
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
        Assert.Equal(swapStyle, resultStyle);
        Assert.Empty(modifiers);  // Expect no modifiers if none are added
    }

    [Fact]
    public void SwapStyleBuilder_After_AddsCorrectDelay()
    {
        // Arrange
        var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

        // Act
        var (_, modifiers) = builder.After(TimeSpan.FromSeconds(1)).Build();

        // Assert
        Assert.Equal("swap:1s", modifiers);
    }

    [Fact]
    public void SwapStyleBuilder_Scroll_AddsCorrectDirection()
    {
        // Arrange
        var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

        // Act
        var (_, modifiers) = builder.Scroll(ScrollDirection.Bottom).Build();

        // Assert
        Assert.Equal("scroll:bottom", modifiers);
    }

    [Fact]
    public void SwapStyleBuilder_IgnoreTitle_AddsCorrectFlag()
    {
        // Arrange
        var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

        // Act
        var (_, modifiers) = builder.IgnoreTitle(true).Build();

        // Assert
        Assert.Equal("ignoreTitle:true", modifiers);
    }

    [Fact]
    public void SwapStyleBuilder_Transition_AddsCorrectFlag()
    {
        // Arrange
        var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

        // Act
        var (_, modifiers) = builder.Transition(true).Build();

        // Assert
        Assert.Equal("transition:true", modifiers);
    }

    [Fact]
    public void SwapStyleBuilder_FocusScroll_AddsCorrectFlag()
    {
        // Arrange
        var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

        // Act
        var (_, modifiers) = builder.FocusScroll(true).Build();

        // Assert
        Assert.Equal("focus-scroll:true", modifiers);
    }

    [Fact]
    public void SwapStyleBuilder_ShowOn_AddsCorrectSelectorAndDirection()
    {
        // Arrange
        var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);
        var selector = "#dynamic-area";

        // Act
        var (_, modifiers) = builder.ShowOn(selector, ScrollDirection.Top).Build();

        // Assert
        Assert.Equal("show:#dynamic-area:top", modifiers);
    }

    [Fact]
    public void SwapStyleBuilder_ShowWindow_AddsCorrectWindowAndDirection()
    {
        // Arrange
        var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

        // Act
        var (_, modifiers) = builder.ShowWindow(ScrollDirection.Top).Build();

        // Assert
        Assert.Equal("show:window:top", modifiers);
    }

    [Fact]
    public void SwapStyleBuilder_ChainedOperations_AddsCorrectModifiers()
    {
        // Arrange
        var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

        // Act
        var (_, modifiers) = builder
            .After(TimeSpan.FromSeconds(1))
            .Scroll(ScrollDirection.Top)
            .Transition(true)
            .IgnoreTitle(false)
            .Build();

        // Assert
        Assert.Equal("swap:1s scroll:top transition:true ignoreTitle:false", modifiers);
    }

    [Fact]
    public void SwapStyleBuilder_After_With250Milliseconds_AddsCorrectDelay()
    {
        // Arrange
        var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

        // Act
        var (_, modifiers) = builder.After(TimeSpan.FromMilliseconds(250)).Build();

        // Assert
        Assert.Equal("swap:250ms", modifiers);
    }

    [Fact]
    public void SwapStyleBuilder_ShowOn_BottomDirection_AddsCorrectModifier()
    {
        // Arrange
        var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);
        var selector = "#element-id";

        // Act
        var (_, modifiers) = builder.ShowOn(selector, ScrollDirection.Bottom).Build();

        // Assert
        Assert.Equal("show:#element-id:bottom", modifiers);
    }

    [Fact]
    public void SwapStyleBuilder_ShowWindow_BottomDirection_AddsCorrectModifier()
    {
        // Arrange
        var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

        // Act
        var (_, modifiers) = builder.ShowWindow(ScrollDirection.Bottom).Build();

        // Assert
        Assert.Equal("show:window:bottom", modifiers);
    }

    [Fact]
    public void SwapStyleBuilder_NullSwapStyle_ReturnsNullStyle()
    {
        // Arrange & Act
        var builder = new SwapStyleBuilder(null);
        var (style, _) = builder.Build();

        // Assert
        Assert.Null(style);
    }

    [Fact]
    public void SwapStyleBuilder_ShowNone_ReturnsCorrectValue()
    {
        // Arrange
        var builder = new SwapStyleBuilder(SwapStyle.InnerHTML);

        // Act
        var (_, modifiers) = builder.ShowNone().Build();

        // Assert
        Assert.Equal("show:none", modifiers);
    }
}
