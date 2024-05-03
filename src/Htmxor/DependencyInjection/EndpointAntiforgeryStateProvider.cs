// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;

namespace Htmxor.DependencyInjection;

internal class EndpointAntiforgeryStateProvider(IAntiforgery antiforgery, PersistentComponentState state) : DefaultAntiforgeryStateProvider(state)
{
	private HttpContext? context;

	internal void SetRequestContext(HttpContext context)
	{
		this.context = context;
	}

	public override AntiforgeryRequestToken? GetAntiforgeryToken()
	{
		if (context == null)
		{
			// We're in an interactive context. Use the token persisted during static rendering.
			return base.GetAntiforgeryToken();
		}

		// We already have a callback setup to generate the token when the response starts if needed.
		// If we need the tokens before we start streaming the response, we'll generate and store them;
		// otherwise we'll just retrieve them.
		// In case there are no tokens available, we are going to return null and no-op.
		var tokens = !context.Response.HasStarted ? antiforgery.GetAndStoreTokens(context) : antiforgery.GetTokens(context);
		if (tokens.RequestToken is null)
		{
			return null;
		}

		return new AntiforgeryRequestToken(tokens.RequestToken, tokens.FormFieldName);
	}
}
