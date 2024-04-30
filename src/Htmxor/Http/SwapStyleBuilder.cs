using System.Collections;
using System.Collections.Specialized;

namespace Htmxor.Http;

/// <summary>
/// A builder class for constructing a swap style command string for HTMX responses.
/// </summary>
public sealed class SwapStyleBuilder 
{
    private readonly SwapStyle? style;
    private readonly OrderedDictionary modifiers = new();

    /// <summary>
    /// Initializes a new instance of the SwapStyleBuilder with a specified swap style.
    /// </summary>
    /// <param name="style">The initial swap style to be applied.</param>
    public SwapStyleBuilder(SwapStyle? style = null)
    {
        this.style = style;
    }

    /// <summary>
    /// Modifies the amount of time that htmx will wait after receiving a 
    /// response to swap the content by including the modifier <c>swap:<paramref name="time"/></c>.
    /// </summary>
    /// <remarks>
    /// <paramref name="time"/> will be converted to milliseconds if less than 1000, otherwise seconds, 
    /// meaning the resulting modifier will be <c>swap:500ms</c> for a <see cref="TimeSpan"/> of 500 milliseconds 
    /// or <c>swap:2s</c> for a <see cref="TimeSpan"/> of 2 seconds..
    /// </remarks>
    /// <param name="style">The swap style.</param>
    /// <param name="time">The amount of time htmx should wait after receiving a 
    /// response to swap the content.</param>
    public SwapStyleBuilder AfterSwapDelay(TimeSpan time)
    {
        AddModifier("swap", time.TotalMilliseconds < 1000 ? $"{time.TotalMilliseconds}ms" : $"{time.TotalSeconds}s");

        return this;
    }

    /// <summary>
    /// Modifies the amount of time that htmx will wait between the swap 
    /// and the settle logic by including the modifier <c>settle:<paramref name="time"/></c>.
    /// </summary>
    /// <remarks>
    /// <paramref name="time"/> will be converted to milliseconds if less than 1000, otherwise seconds, 
    /// meaning the resulting modifier will be <c>swap:500ms</c> for a <see cref="TimeSpan"/> of 500 milliseconds 
    /// or <c>swap:2s</c> for a <see cref="TimeSpan"/> of 2 seconds..
    /// </remarks>
    /// <param name="time">The amount of time htmx should wait after receiving a 
    /// response to swap the content.</param>
    public SwapStyleBuilder AfterSettleDelay(TimeSpan time)
    {
	    AddModifier("settle", time.TotalMilliseconds < 1000 ? $"{time.TotalMilliseconds}ms" : $"{time.TotalSeconds}s");

	    return this;
    }

    /// <summary>
    /// Specifies the direction to scroll the page after the swap and appends the modifier <c>scroll:{direction}</c>.
    /// </summary>
    /// <remarks>
    /// Sets the scroll direction on the page after swapping. For instance, using <see cref="ScrollDirection.Top"/>
    /// will add the modifier <c>scroll:top</c> which instructs the page to scroll to the top after the swap.
    /// </remarks>
    /// <param name="direction">The scroll direction after the swap.</param>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder Scroll(ScrollDirection direction)
    {
        switch (direction)
        {
            case ScrollDirection.Top:
                AddModifier("scroll", "top");
                break;
            case ScrollDirection.Bottom:
                AddModifier("scroll", "bottom");
                break;
        }

        return this;
    }

    /// <summary>
    /// Automatically scrolls to the top of the page after a swap.
    /// </summary>
    /// <remarks>
    /// This method adds the modifier <c>scroll:top</c> to the swap commands, instructing the page to scroll to
    /// the top after content is swapped.
    /// </remarks>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder ScrollTop() => Scroll(ScrollDirection.Top);

    /// <summary>
    /// Automatically scrolls to the bottom of the page after a swap.
    /// </summary>
    /// <remarks>
    /// This method adds the modifier <c>scroll:bottom</c> to the swap commands, instructing the page to scroll to
    /// the bottom after content is swapped.
    /// </remarks>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder ScrollBottom() => Scroll(ScrollDirection.Bottom);

    /// <summary>
    /// Determines whether to ignore the document title in the swap response by appending the modifier
    /// <c>ignoreTitle:{ignore}</c>.
    /// </summary>
    /// <remarks>
    /// When set to true, the document title in the swap response will be ignored by adding the modifier
    /// <c>ignoreTitle:true</c>.
    /// This keeps the current title unchanged regardless of the incoming swap content's title tag.
    /// </remarks>
    /// <param name="ignore">Whether to ignore the title.</param>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder IgnoreTitle(bool ignore = true)
    {
        AddModifier("ignoreTitle", ignore);

        return this;
    }

    /// <summary>
    /// Includes the document title from the swap response in the current page.
    /// </summary>
    /// <remarks>
    /// This method ensures the title of the document is updated according to the swap response by removing any
    /// ignoreTitle modifiers, effectively setting <c>ignoreTitle:false</c>.
    /// </remarks>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder IncludeTitle() => IgnoreTitle(false);

    /// <summary>
    /// Enables or disables transition effects for the swap by appending the modifier <c>transition:{show}</c>.
    /// </summary>
    /// <remarks>
    /// Controls the display of transition effects during the swap. For example, setting <paramref name="show"/> to true
    /// will add the modifier <c>transition:true</c> to enable smooth transitions.
    /// </remarks>
    /// <param name="show">Whether to show transition effects.</param>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder Transition(bool show)
    {
        AddModifier("transition", show);

        return this;
    }

    /// <summary>
    /// Explicitly includes transition effects for the swap.
    /// </summary>
    /// <remarks>
    /// By calling this method, transition effects are enabled for the swap, adding the modifier <c>transition:true</c>.
    /// </remarks>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder IncludeTransition() => Transition(true);

    /// <summary>
    /// Explicitly ignores transition effects for the swap.
    /// </summary>
    /// <remarks>
    /// This method disables transition effects by adding the modifier <c>transition:false</c> to the swap commands.
    /// </remarks>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder IgnoreTransition() => Transition(false);

    /// <summary>
    /// htmx preserves focus between requests for inputs that have a defined id attribute. By
    /// default htmx prevents auto-scrolling to focused inputs between requests which can be
    /// unwanted behavior on longer requests when the user has already scrolled away. 
    /// </summary>
    /// <remarks>
    /// <paramref name="scroll"/> when true will be <c>focus-scroll:true</c>, otherwise when false
    /// will be <c>focus-scroll:false</c>
    /// </remarks>
    /// <param name="scroll">Whether to scroll to the focus element.</param>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder ScrollFocus(bool scroll = true)
    {
        AddModifier("focus-scroll", scroll);

        return this;
    }

    public SwapStyleBuilder PreserveFocus() => ScrollFocus(false);

    /// <summary>
    /// Specifies a CSS selector to dynamically target for the swap operation, with an optional scroll direction after the swap.
    /// </summary>
    /// <remarks>
    /// Adds a show modifier with the specified CSS selector and scroll direction. For example, if <paramref name="selector"/> is ".item" and <paramref name="direction"/> is <see cref="ScrollDirection.Top"/>, the modifier <c>show:.item:top</c> is added.
    /// </remarks>
    /// <param name="selector">The CSS selector of the target element.</param>
    /// <param name="direction">The scroll direction after swap.</param>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder ShowOn(string selector, ScrollDirection direction)
    {
        switch (direction)
        {
            case ScrollDirection.Top:
                AddModifier("show", $"{selector}:top");
                break;
            case ScrollDirection.Bottom:
                AddModifier("show", $"{selector}:bottom");
                break;
        }

        return this;
    }

    /// <summary>
    /// Specifies that the swap should show the element matching the CSS selector at the top of the window.
    /// </summary>
    /// <remarks>
    /// This method adds the modifier <c>show:{selector}:top</c>, directing the swap to display the specified element at the top of the window.
    /// </remarks>
    /// <param name="selector">The CSS selector of the target element.</param>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder ShowOnTop(string selector) => ShowOn(selector, ScrollDirection.Top);

    /// <summary>
    /// Specifies that the swap should show the element matching the CSS selector at the bottom of the window.
    /// </summary>
    /// <remarks>
    /// This method adds the modifier <c>show:{selector}:bottom</c>, directing the swap to display the specified element at the bottom of the window.
    /// </remarks>
    /// <param name="selector">The CSS selector of the target element.</param>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder ShowOnBottom(string selector) => ShowOn(selector, ScrollDirection.Bottom);

    /// <summary>
    /// Specifies that the swap should show in the window, with an optional scroll direction.
    /// </summary>
    /// <param name="direction">The direction to scroll the window after the swap.</param>
    /// <remarks>
    /// This method adds the modifier <c>show:window:{direction}</c>, directing the swap to display the specified element at the bottom of the window.
    /// </remarks>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder ShowOnWindow(ScrollDirection direction)
    {
        switch (direction)
        {
            case ScrollDirection.Top:
                AddModifier("show", $"window:top");
                break;
            case ScrollDirection.Bottom:
                AddModifier("show", $"window:bottom");
                break;
        }

        return this;
    }

    /// <summary>
    /// Specifies that the swap should show content at the top of the window.
    /// </summary>
    /// <remarks>
    /// This method adds the modifier <c>show:window:top</c>, instructing the content to be displayed
    /// at the top of the window following a swap. This can be useful for ensuring that important content or
    /// notifications are immediately visible to the user after a swap operation.
    /// </remarks>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder ShowOnWindowTop() => ShowOnWindow(ScrollDirection.Top);

    /// <summary>
    /// Specifies that the swap should show content at the bottom of the window.
    /// </summary>
    /// <remarks>
    /// This method adds the modifier <c>show:window:bottom</c>, instructing the content to be displayed
    /// at the bottom of the window following a swap. This positioning can be used for infinite scrolling, footers or
    /// lower-priority information that should not immediately distract from other content.
    /// </remarks>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder ShowOnWindowBottom() => ShowOnWindow(ScrollDirection.Bottom);

    /// <summary>
    /// Turns off scrolling after swap.
    /// </summary>
    /// <remarks>
    /// This method disables automatic scrolling by setting the modifier <c>show:none</c>, ensuring the page
    /// position remains unchanged after the swap.
    /// </remarks>
    /// <returns>The SwapStyleBuilder instance for chaining.</returns>
    public SwapStyleBuilder ShowNone()
    {
        AddModifier("show", "none");

        return this;
    }

    /// <summary>
    /// Builds the swap style command string with all specified modifiers.
    /// </summary>
    /// <returns>A tuple containing the SwapStyle and the constructed command string.</returns>
    internal (SwapStyle?, string) Build()
    {
        var value = string.Empty;

        if (modifiers.Count > 0)
        {
            value = modifiers.Cast<DictionaryEntry>()
                .Select(entry => $"{entry.Key}:{entry.Value}")
                .Aggregate((current, next) => $"{current} {next}");
        }

        return (style, value);
    }

    /// <summary>
    /// Adds a modifier to modifiers, overriding existing values if present
    /// </summary>
    /// <param name="modifier"></param>
    /// <param name="options"></param>
    private void AddModifier(string modifier, string options)
    {
        if (modifiers.Contains(modifier))
            modifiers.Remove(modifier);
        
        modifiers.Add(modifier, options);
    }

    private void AddModifier(string modifier, bool option) => AddModifier(modifier, option ? "true" : "false");
}

/// <summary>
/// Extension methods for the SwapStyle enum to facilitate building swap style commands.
/// </summary>
public static class SwapStyleBuilderExtension
{
    // Each method below returns a SwapStyleBuilder instance initialized with the respective SwapStyle
    // and applies the specified modifier to it. This allows for fluent configuration of swap style commands.

    public static SwapStyleBuilder AfterSwapDelay(this SwapStyle style, TimeSpan time) => new SwapStyleBuilder(style).AfterSwapDelay(time);
    public static SwapStyleBuilder AfterSettleDelay(this SwapStyle style, TimeSpan time) => new SwapStyleBuilder(style).AfterSettleDelay(time);
    public static SwapStyleBuilder Scroll(this SwapStyle style, ScrollDirection direction) => new SwapStyleBuilder(style).Scroll(direction);
    public static SwapStyleBuilder ScrollTop(this SwapStyle style) => new SwapStyleBuilder(style).ScrollTop();
    public static SwapStyleBuilder ScrollBottom(this SwapStyle style) => new SwapStyleBuilder(style).ScrollBottom();
    public static SwapStyleBuilder IgnoreTitle(this SwapStyle style, bool ignore = true) => new SwapStyleBuilder(style).IgnoreTitle(ignore);
    public static SwapStyleBuilder IncludeTitle(this SwapStyle style) => new SwapStyleBuilder(style).IncludeTitle();
    public static SwapStyleBuilder Transition(this SwapStyle style, bool show) => new SwapStyleBuilder(style).Transition(show);
    public static SwapStyleBuilder IncludeTransition(this SwapStyle style) => new SwapStyleBuilder(style).IncludeTransition();
    public static SwapStyleBuilder IgnoreTransition(this SwapStyle style) => new SwapStyleBuilder(style).IgnoreTransition();
    public static SwapStyleBuilder ScrollFocus(this SwapStyle style, bool scroll = true) => new SwapStyleBuilder(style).ScrollFocus(scroll);
    public static SwapStyleBuilder PreserveFocus(this SwapStyle style, bool scroll = true) => new SwapStyleBuilder(style).PreserveFocus();
    public static SwapStyleBuilder ShowOn(this SwapStyle style, string selector, ScrollDirection direction) => new SwapStyleBuilder(style).ShowOn(selector, direction);
    public static SwapStyleBuilder ShowOnTop(this SwapStyle style, string selector) => new SwapStyleBuilder(style).ShowOnTop(selector);
    public static SwapStyleBuilder ShowOnBottom(this SwapStyle style, string selector) => new SwapStyleBuilder(style).ShowOnBottom(selector);
    public static SwapStyleBuilder ShowOnWindow(this SwapStyle style, ScrollDirection direction) => new SwapStyleBuilder(style).ShowOnWindow(direction);
    public static SwapStyleBuilder ShowOnWindowTop(this SwapStyle style) => new SwapStyleBuilder(style).ShowOnWindowTop();
    public static SwapStyleBuilder ShowOnWindowBottom(this SwapStyle style) => new SwapStyleBuilder(style).ShowOnWindowBottom();
    public static SwapStyleBuilder ShowNone(this SwapStyle style) => new SwapStyleBuilder(style).ShowNone();
}