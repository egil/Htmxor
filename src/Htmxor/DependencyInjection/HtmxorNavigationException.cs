using Microsoft.AspNetCore.Components;

namespace Htmxor.DependencyInjection;

/// <summary>
/// Exception thrown to indicate that a navigation operation was requested during static service side rendering.
/// </summary>
[SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Not relevant in this context.")]
public sealed class HtmxorNavigationException : NavigationException
{
	/// <summary>
	/// Gets the URI that was originally passed to the <see cref="NavigationManager"/>.
	/// </summary>
	public string RequestedLocation { get; }

	/// <summary>
	/// Gets the <see cref="NavigationOptions"/> that was originally passed to the <see cref="NavigationManager"/>.
	/// </summary>
	public NavigationOptions Options { get; }

	public HtmxorNavigationException(
		[StringSyntax("Uri")] string requestedUri,
		[StringSyntax("Uri")] string absoluteUri,
		in NavigationOptions options) : base(absoluteUri)
	{
		RequestedLocation = requestedUri;
		Options = options;
	}
}
