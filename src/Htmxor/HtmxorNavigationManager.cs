using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Htmxor;

internal sealed class HtmxorNavigationManager : NavigationManager, IHostEnvironmentNavigationManager
{
	void IHostEnvironmentNavigationManager.Initialize(string baseUri, string uri) => Initialize(baseUri, uri);

	protected override void NavigateToCore([StringSyntax("Uri")] string uri, bool forceLoad) => NavigateToCore(uri, new NavigationOptions()
	{
		ForceLoad = forceLoad,
	});

	protected override void NavigateToCore([StringSyntax("Uri")] string uri, NavigationOptions options)
	{
		var absoluteUriString = ToAbsoluteUri(uri).AbsoluteUri;
		throw new HtmxorNavigationException(uri, absoluteUriString, in options);
	}
}
