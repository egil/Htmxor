using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Numerics;

namespace Htmxor.Http;

/// <summary>
/// A builder class for constructing a swap style command string for HTMX responses.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SwapStyleBuilder
{
    private readonly SwapStyle style;
    private readonly OrderedDictionary modifiers = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the SwapStyleBuilder with a specified swap style.
    /// </summary>
    /// <param name="style">The initial swap style to be applied.</param>
    public SwapStyleBuilder(SwapStyle style = SwapStyle.Default)
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
    /// <param name="time">The amount of time htmx should wait after receiving a 
    /// response to swap the content.</param>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
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
    /// meaning the resulting modifier will be <c>settle:500ms</c> for a <see cref="TimeSpan"/> of 500 milliseconds 
    /// or <c>settle:2s</c> for a <see cref="TimeSpan"/> of 2 seconds..
    /// </remarks>
    /// <param name="time">The amount of time htmx should wait after receiving a 
    /// response to swap the content.</param>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder AfterSettleDelay(TimeSpan time)
    {
        AddModifier("settle", time.TotalMilliseconds < 1000 ? $"{time.TotalMilliseconds}ms" : $"{time.TotalSeconds}s");

        return this;
    }

    /// <summary>
    /// Specifies how to set the content scrollbar position after the swap and appends the modifier <c>scroll:<paramref name="direction"/></c>.
    /// </summary>
    /// <remarks>
    /// Sets the swapped content scrollbar position after swapping immediately (without animation). For instance, using <see cref="ScrollDirection.Top"/>
    /// will add the modifier <c>scroll:top</c> which sets the scrollbar position to the top of swap content after the swap.
    /// If css <paramref name="selector"/> is present then the page is scrolled to the <paramref name="direction"/> of the content identified by the css selector.
    /// </remarks>
    /// <param name="direction">The scroll direction after the swap.</param>
    /// <param name="selector">Optional CSS selector of the target element.</param>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder Scroll(ScrollDirection direction, string? selector = null)
    {
        switch (direction)
        {
            case ScrollDirection.Top:
                AddModifier("scroll", selector is null ? "top" : $"{selector}:top");
                break;
            case ScrollDirection.Bottom:
                AddModifier("scroll", selector is null ? "bottom" : $"{selector}:bottom");
                break;
        }

        return this;
    }

    /// <summary>
    /// Sets the content scrollbar position to the top of the swapped content after a swap.
    /// </summary>
    /// <remarks>
    /// This method adds the modifier <c>scroll:top</c> to the swap commands, instructing the page to scroll to
    /// the top of the content after content is swapped immediately and without animation. If css <paramref name="selector"/>
    /// is present then the page is scrolled to the top of the content identified by the css selector.
    /// </remarks>
    /// <param name="selector">Optional CSS selector of the target element.</param>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder ScrollTop(string? selector = null) => Scroll(ScrollDirection.Top, selector);

    /// <summary>
    /// Sets the content scrollbar position to the bottom of the swapped content after a swap.
    /// </summary>
    /// <remarks>
    /// This method adds the modifier <c>scroll:bottom</c> to the swap commands, instructing the page to scroll to
    /// the bottom of the content after content is swapped immediately and without animation. If css <paramref name="selector"/>
    /// is present then the page is scrolled to the bottom of the content identified by the css selector.
    /// </remarks>
    /// <param name="selector">Optional CSS selector of the target element.</param>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder ScrollBottom(string? selector = null) => Scroll(ScrollDirection.Bottom, selector);

    /// <summary>
    /// Determines whether to ignore the document title in the swap response by appending the modifier
    /// <c>ignoreTitle:<paramref name="ignore"/></c>.
    /// </summary>
    /// <remarks>
    /// When set to true, the document title in the swap response will be ignored by adding the modifier
    /// <c>ignoreTitle:true</c>.
    /// This keeps the current title unchanged regardless of the incoming swap content's title tag.
    /// </remarks>
    /// <param name="ignore">Whether to ignore the title.</param>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
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
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder IncludeTitle() => IgnoreTitle(false);

    /// <summary>
    /// Enables or disables transition effects for the swap by appending the modifier <c>transition:<paramref name="show"/></c>.
    /// </summary>
    /// <remarks>
    /// Controls the display of transition effects during the swap. For example, setting <paramref name="show"/> to true
    /// will add the modifier <c>transition:true</c> to enable smooth transitions.
    /// </remarks>
    /// <param name="show">Whether to show transition effects.</param>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
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
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder IncludeTransition() => Transition(true);

    /// <summary>
    /// Explicitly ignores transition effects for the swap.
    /// </summary>
    /// <remarks>
    /// This method disables transition effects by adding the modifier <c>transition:false</c> to the swap commands.
    /// </remarks>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder IgnoreTransition() => Transition(false);

    /// <summary>
    /// Allows you to specify that htmx should scroll to the focused element when a request completes.
    /// htmx preserves focus between requests for inputs that have a defined id attribute. By
    /// default htmx prevents auto-scrolling to focused inputs between requests which can be
    /// unwanted behavior on longer requests when the user has already scrolled away. 
    /// </summary>
    /// <remarks>
    /// <paramref name="scroll"/> when true will be <c>focus-scroll:true</c>, otherwise when false
    /// will be <c>focus-scroll:false</c>
    /// </remarks>
    /// <param name="scroll">Whether to scroll to the focus element.</param>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder ScrollFocus(bool scroll = true)
    {
        AddModifier("focus-scroll", scroll);

        return this;
    }

    /// <summary>
    /// Explicitly preserves focus between requests for inputs that have a defined id attribute without
    /// scrolling.
    /// </summary>
    /// <remarks>
    /// Adds a modifier of <c>focus-scroll:false</c>
    /// </remarks>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder PreserveFocus() => ScrollFocus(false);

    /// <summary>
    /// Specifies a CSS selector to target for the swap operation, smoothly animating the scrollbar position to either the
    /// top or the bottom of the target element after the swap.
    /// </summary>
    /// <remarks>
    /// Adds a show modifier with the specified CSS selector and scroll direction. For example, if <paramref name="selector"/>
    /// is ".item" and <paramref name="direction"/> is <see cref="ScrollDirection.Top"/>, the modifier <c>show:.item:top</c>
    /// is added.
    /// </remarks>
    /// <param name="direction">The scroll direction after swap.</param>
    /// <param name="selector">Optional CSS selector of the target element.</param>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder ShowOn(ScrollDirection direction, string? selector = null)
    {
        switch (direction)
        {
            case ScrollDirection.Top:
                AddModifier("show", selector is null ? "top" : $"{selector}:top");
                break;
            case ScrollDirection.Bottom:
                AddModifier("show", selector is null ? "bottom" : $"{selector}:bottom");
                break;
        }

        return this;
    }

    /// <summary>
    /// Specifies that the swap should show the top of the element matching the CSS selector.
    /// </summary>
    /// <remarks>
    /// This method adds the modifier <c>show:<paramref name="selector"/>:top</c>, smoothly scrolling to the top of the element identified by
    /// <paramref name="selector"/>.
    /// </remarks>
    /// <param name="selector">Optional CSS selector of the target element.</param>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder ShowOnTop(string? selector = null) => ShowOn(ScrollDirection.Top, selector);

    /// <summary>
    /// Specifies that the swap should show the bottom of the element matching the CSS selector.
    /// </summary>
    /// <remarks>
    /// This method adds the modifier <c>show:<paramref name="selector"/>:bottom</c>, smoothly scrolling to the bottom of the element identified by
    /// <paramref name="selector"/>.
    /// </remarks>
    /// <param name="selector">Optional CSS selector of the target element.</param>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder ShowOnBottom(string? selector = null) => ShowOn(ScrollDirection.Bottom, selector);

    /// <summary>
    /// Specifies that the swap should show in the window by smoothly scrolling to either the top or bottom of the window.
    /// </summary>
    /// <remarks>
    /// This method adds the modifier <c>show:window:<paramref name="direction"/></c>, directing the swap to display the specified
    /// element at the bottom of the window.
    /// </remarks>
    /// <param name="direction">The direction to scroll the window after the swap.</param>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder ShowWindow(ScrollDirection direction)
    {
        switch (direction)
        {
            case ScrollDirection.Top:
                AddModifier("show", "window:top");
                break;
            case ScrollDirection.Bottom:
                AddModifier("show", "window:bottom");
                break;
        }

        return this;
    }

    /// <summary>
    /// Specifies that the swap should smoothly scroll to the top of the window.
    /// </summary>
    /// <remarks>
    /// This method adds the modifier <c>show:window:top</c>, instructing the content to be displayed
    /// at the top of the window following a swap by smoothly animating the scrollbar position. This can be useful
    /// for ensuring that important content or notifications at the top of the page are immediately visible to
    /// the user after a swap operation.
    /// </remarks>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder ShowWindowTop() => ShowWindow(ScrollDirection.Top);

    /// <summary>
    /// Specifies that the swap should smoothly scroll to the bottom of the window.
    /// </summary>
    /// <remarks>
    /// This method adds the modifier <c>show:window:bottom</c>, instructing the content to be displayed
    /// at the bottom of the window following a swap by smoothly animating the scrollbar position. This positioning
    /// can be used for infinite scrolling, footers, or information appended at the end of the page.
    /// </remarks>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder ShowWindowBottom() => ShowWindow(ScrollDirection.Bottom);

    /// <summary>
    /// Turns off scrolling after swap.
    /// </summary>
    /// <remarks>
    /// This method disables automatic scrolling by setting the modifier <c>show:none</c>, ensuring the page
    /// position remains unchanged after the swap.
    /// </remarks>
    /// <returns>This <see cref="SwapStyleBuilder"/> object instance.</returns>
    public SwapStyleBuilder ShowNone()
    {
        AddModifier("show", "none");

        return this;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var (swapStyle, modifier) = Build();
        var style = swapStyle.ToHtmxString();
        var value = !string.IsNullOrWhiteSpace(modifier)
            ? $"{style} {modifier}"
            : style;
        return value;
    }

    /// <summary>
    /// Builds the swap style command string with all specified modifiers.
    /// </summary>
    /// <returns>A tuple containing the <see cref="SwapStyle"/> and the constructed command string.</returns>
    internal (SwapStyle, string) Build()
    {
        var value = string.Empty;

        if (modifiers.Count > 0)
        {
            value = modifiers
                .Cast<DictionaryEntry>()
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

    /// <summary>
    /// Adds a boolean modifier to modifiers
    /// </summary>
    /// <param name="modifier"></param>
    /// <param name="option"></param>
    private void AddModifier(string modifier, bool option) => AddModifier(modifier, option ? "true" : "false");
}
