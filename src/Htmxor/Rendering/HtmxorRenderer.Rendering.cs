// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Htmxor.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using static Htmxor.LinkerFlags;

namespace Htmxor.Rendering;

internal partial class HtmxorRenderer
{
	internal async ValueTask<RenderedComponentHtmlContent> RenderEndpointComponent(
		HttpContext httpContext,
		[DynamicallyAccessedMembers(Component)] Type rootComponentType,
		ParameterView parameters)
	{
		SetHttpContext(httpContext);

		try
		{
			var component = BeginRenderingComponent(rootComponentType, parameters);
			var result = new RenderedComponentHtmlContent(Dispatcher, component);
			await result.QuiescenceTask;
			return result;
		}
		catch (HtmxorNavigationException navigationException)
		{
			return await HandleNavigationException(this.httpContext, navigationException);
		}
	}

	[SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Urls are user provided, no value in converting to Uri.")]
	public static ValueTask<RenderedComponentHtmlContent> HandleNavigationException(HttpContext httpContext, HtmxorNavigationException navigationException)
	{
		var htmxContext = httpContext.GetHtmxContext();
		if (httpContext.Response.HasStarted)
		{
			// If we're not doing streaming SSR, this has no choice but to be a fatal error because there's no way to
			// communicate the redirection to the browser.
			// If we are doing streaming SSR, this should not generally happen because if you navigate during the initial
			// synchronous render, the response would not yet have started, and if you do so during some later async
			// phase, we would already have exited this method since streaming SSR means not awaiting quiescence.
			throw new InvalidOperationException(
				"A navigation command was attempted during prerendering after the server already started sending the response. " +
				"Navigation commands can not be issued during server-side prerendering after the response from the server has started. Applications must buffer the" +
				"response and avoid using features like FlushAsync() before all components on the page have been rendered to prevent failed navigation commands.");
		}
		else if (IsPossibleExternalDestination(httpContext.Request, navigationException.Location))
		{
			// For progressively-enhanced nav, we prefer to use opaque redirections for external URLs rather than
			// forcing the request to be retried, since that allows post-redirect-get to work, plus avoids a
			// duplicated request. The client can't rely on receiving this header, though, since non-Blazor endpoints
			// wouldn't return it.

			// Originally Blazor would return a blazor-enhanced-nav-redirect-location header. Here we rely on Htmx's
			// handling of headers for htmx requests and uses the built in browser redirect for non-htmx requests.
			// TODO: validate that this works as intended.
			if (htmxContext.Request.IsHtmxRequest)
			{
				htmxContext.Response.Redirect(OpaqueRedirection.CreateProtectedRedirectionUrl(httpContext, navigationException.Location));
			}
			else
			{
				httpContext.Response.Redirect(OpaqueRedirection.CreateProtectedRedirectionUrl(httpContext, navigationException.Location));
			}

			return new ValueTask<RenderedComponentHtmlContent>(RenderedComponentHtmlContent.Empty);
		}

		var navOptions = navigationException.Options;
		if (htmxContext.Request.IsHtmxRequest)
		{
			if (navOptions.ForceLoad)
			{
				htmxContext.Response.Redirect(navigationException.RequestedLocation);

				if (navOptions.ReplaceHistoryEntry)
				{
					htmxContext.Response.ReplaceUrl(navigationException.RequestedLocation);
				}
			}
			else
			{
				if (navOptions.ReplaceHistoryEntry)
				{
					htmxContext.Response.ReplaceUrl(navigationException.RequestedLocation);
				}
				else
				{
					htmxContext.Response.Redirect(navigationException.RequestedLocation);
				}
			}
		}
		else
		{
			httpContext.Response.Redirect(navigationException.Location);
		}

		return new ValueTask<RenderedComponentHtmlContent>(RenderedComponentHtmlContent.Empty);
	}

	private static bool IsPossibleExternalDestination(HttpRequest request, string destinationUrl)
	{
		if (!Uri.TryCreate(destinationUrl, UriKind.Absolute, out var absoluteUri))
		{
			return false;
		}

		return !string.Equals(absoluteUri.Scheme, request.Scheme, StringComparison.OrdinalIgnoreCase)
			|| !string.Equals(absoluteUri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase);
	}
}
