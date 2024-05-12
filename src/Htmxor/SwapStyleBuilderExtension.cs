namespace Htmxor;

/// <summary>
/// Extension methods for the SwapStyle enum to facilitate building swap style commands.
/// </summary>
public static class SwapStyleBuilderExtension
{
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
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder AfterSwapDelay(this SwapStyle style, TimeSpan time) => new SwapStyleBuilder(style).AfterSwapDelay(time);

	/// <summary>
	/// Modifies the amount of time that htmx will wait between the swap 
	/// and the settle logic by including the modifier <c>settle:<paramref name="time"/></c>.
	/// </summary>
	/// <remarks>
	/// <paramref name="time"/> will be converted to milliseconds if less than 1000, otherwise seconds, 
	/// meaning the resulting modifier will be <c>settle:500ms</c> for a <see cref="TimeSpan"/> of 500 milliseconds 
	/// or <c>settle:2s</c> for a <see cref="TimeSpan"/> of 2 seconds..
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <param name="time">The amount of time htmx should wait after receiving a 
	/// response to swap the content.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder AfterSettleDelay(this SwapStyle style, TimeSpan time) => new SwapStyleBuilder(style).AfterSettleDelay(time);

	/// <summary>
	/// Specifies how to set the content scrollbar position after the swap and appends the modifier <c>scroll:<paramref name="direction"/></c>.
	/// </summary>
	/// <remarks>
	/// Sets the swapped content scrollbar position after swapping immediately (without animation). For instance, using <see cref="ScrollDirection.Top"/>
	/// will add the modifier <c>scroll:top</c> which sets the scrollbar position to the top of swap content after the swap.
	/// If css <paramref name="cssSelector"/> is present then the page is scrolled to the <paramref name="direction"/> of the content identified by the css cssSelector.
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <param name="direction">The scroll direction after the swap.</param>
	/// <param name="cssSelector">Optional css selector of the target element.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder Scroll(this SwapStyle style, ScrollDirection direction, string? cssSelector) => new SwapStyleBuilder(style).Scroll(direction, cssSelector);

	/// <summary>
	/// Sets the content scrollbar position to the top of the swapped content after a swap.
	/// </summary>
	/// <remarks>
	/// This method adds the modifier <c>scroll:top</c> to the swap commands, instructing the page to scroll to
	/// the top of the content after content is swapped immediately and without animation. If css <paramref name="cssSelector"/>
	/// is present then the page is scrolled to the top of the content identified by the css cssSelector.
	/// </remarks>
	/// <param name="cssSelector">Optional css selector of the target element.</param>
	/// <param name="style">The swap style.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder ScrollTop(this SwapStyle style, string? cssSelector) => new SwapStyleBuilder(style).ScrollTop(cssSelector);

	/// <summary>
	/// Sets the content scrollbar position to the bottom of the swapped content after a swap.
	/// </summary>
	/// <remarks>
	/// This method adds the modifier <c>scroll:bottom</c> to the swap commands, instructing the page to scroll to
	/// the bottom of the content after content is swapped immediately and without animation. If css <paramref name="cssSelector"/>
	/// is present then the page is scrolled to the bottom of the content identified by the css cssSelector.
	/// </remarks>
	/// <param name="cssSelector">Optional css selector of the target element.</param>
	/// <param name="style">The swap style.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder ScrollBottom(this SwapStyle style, string? cssSelector) => new SwapStyleBuilder(style).ScrollBottom(cssSelector);

	/// <summary>
	/// Determines whether to ignore the document title in the swap response by appending the modifier
	/// <c>ignoreTitle:<paramref name="ignore"/></c>.
	/// </summary>
	/// <remarks>
	/// When set to true, the document title in the swap response will be ignored by adding the modifier
	/// <c>ignoreTitle:true</c>.
	/// This keeps the current title unchanged regardless of the incoming swap content's title tag.
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <param name="ignore">Whether to ignore the title.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder IgnoreTitle(this SwapStyle style, bool ignore = true) => new SwapStyleBuilder(style).IgnoreTitle(ignore);

	/// <summary>
	/// Includes the document title from the swap response in the current page.
	/// </summary>
	/// <remarks>
	/// This method ensures the title of the document is updated according to the swap response by removing any
	/// ignoreTitle modifiers, effectively setting <c>ignoreTitle:false</c>.
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder IncludeTitle(this SwapStyle style) => new SwapStyleBuilder(style).IncludeTitle();

	/// <summary>
	/// Enables or disables transition effects for the swap by appending the modifier <c>transition:{show}</c>.
	/// </summary>
	/// <remarks>
	/// Controls the display of transition effects during the swap. For example, setting <paramref name="show"/> to true
	/// will add the modifier <c>transition:true</c> to enable smooth transitions.
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <param name="show">Whether to show transition effects.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder Transition(this SwapStyle style, bool show) => new SwapStyleBuilder(style).Transition(show);

	/// <summary>
	/// Explicitly includes transition effects for the swap.
	/// </summary>
	/// <remarks>
	/// By calling this method, transition effects are enabled for the swap, adding the modifier <c>transition:true</c>.
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder IncludeTransition(this SwapStyle style) => new SwapStyleBuilder(style).IncludeTransition();

	/// <summary>
	/// Explicitly ignores transition effects for the swap.
	/// </summary>
	/// <remarks>
	/// This method disables transition effects by adding the modifier <c>transition:false</c> to the swap commands.
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder IgnoreTransition(this SwapStyle style) => new SwapStyleBuilder(style).IgnoreTransition();

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
	/// <param name="style">The swap style.</param>
	/// <param name="scroll">Whether to scroll to the focus element.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder ScrollFocus(this SwapStyle style, bool scroll = true) => new SwapStyleBuilder(style).ScrollFocus(scroll);

	/// <summary>
	/// Explicitly preserves focus between requests for inputs that have a defined id attribute without
	/// scrolling.
	/// </summary>
	/// <remarks>
	/// Adds a modifier of <c>focus-scroll:false</c>
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <param name="scroll">Whether to scroll to current focus or preserve focus</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder PreserveFocus(this SwapStyle style, bool scroll = true) => new SwapStyleBuilder(style).PreserveFocus();

	/// <summary>
	/// Specifies a css selector to dynamically target for the swap operation, with a scroll direction after the swap.
	/// </summary>
	/// <remarks>
	/// Adds a show modifier with the specified CSS selector and scroll direction. For example, if <paramref name="cssSelector"/>
	/// is ".item" and <paramref name="direction"/> is <see cref="ScrollDirection.Top"/>, the modifier <c>show:.item:top</c>
	/// is added.
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <param name="cssSelector">Optional css selector of the target element.</param>
	/// <param name="direction">The scroll direction after swap.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder ShowOn(this SwapStyle style, ScrollDirection direction, string? cssSelector = null) => new SwapStyleBuilder(style).ShowOn(direction, cssSelector);

	/// <summary>
	/// Specifies that the swap should show the element matching the css selector at the top of the window.
	/// </summary>
	/// <remarks>
	/// This method adds the modifier <c>show:{cssSelector}:top</c>, directing the swap to display the specified element at the top of the window.
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <param name="cssSelector">Optional css selector of the target element.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder ShowOnTop(this SwapStyle style, string? cssSelector = null) => new SwapStyleBuilder(style).ShowOnTop(cssSelector);

	/// <summary>
	/// Specifies that the swap should show the element matching the css selector at the bottom of the window.
	/// </summary>
	/// <remarks>
	/// This method adds the modifier <c>show:{cssSelector}:bottom</c>, directing the swap to display the specified element at the bottom of the window.
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <param name="cssSelector">The css selector of the target element.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder ShowOnBottom(this SwapStyle style, string? cssSelector = null) => new SwapStyleBuilder(style).ShowOnBottom(cssSelector);

	/// <summary>
	/// Specifies that the swap should show in the window by smoothly scrolling to either the top or bottom of the window.
	/// </summary>
	/// <param name="direction">The direction to scroll the window after the swap.</param>
	/// <remarks>
	/// This method adds the modifier <c>show:window:<paramref name="direction"/></c>, directing the swap to display the specified
	/// element at the bottom of the window.
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder ShowWindow(this SwapStyle style, ScrollDirection direction) => new SwapStyleBuilder(style).ShowWindow(direction);

	/// <summary>
	/// Specifies that the swap should smoothly scroll to the top of the window.
	/// </summary>
	/// <remarks>
	/// This method adds the modifier <c>show:window:top</c>, instructing the content to be displayed
	/// at the top of the window following a swap by smoothly animating the scrollbar position. This can be useful
	/// for ensuring that important content or notifications at the top of the page are immediately visible to
	/// the user after a swap operation.
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder ShowWindowTop(this SwapStyle style) => new SwapStyleBuilder(style).ShowWindowTop();

	/// <summary>
	/// Specifies that the swap should smoothly scroll to the bottom of the window.
	/// </summary>
	/// <remarks>
	/// This method adds the modifier <c>show:window:bottom</c>, instructing the content to be displayed
	/// at the bottom of the window following a swap by smoothly animating the scrollbar position. This positioning
	/// can be used for infinite scrolling, footers, or information appended at the end of the page.
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder ShowWindowBottom(this SwapStyle style) => new SwapStyleBuilder(style).ShowWindowBottom();

	/// <summary>
	/// Turns off scrolling after swap.
	/// </summary>
	/// <remarks>
	/// This method disables automatic scrolling by setting the modifier <c>show:none</c>, ensuring the page
	/// position remains unchanged after the swap.
	/// </remarks>
	/// <param name="style">The swap style.</param>
	/// <returns>A <see cref="SwapStyleBuilder"/> object instance.</returns>
	public static SwapStyleBuilder ShowNone(this SwapStyle style) => new SwapStyleBuilder(style).ShowNone();
}
