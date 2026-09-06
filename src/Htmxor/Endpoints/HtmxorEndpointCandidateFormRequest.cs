// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from ASP.NET Core v10.0.11, commit a5383385245bdacc20ec19f30e46090a8154d8da,
// synchronized 2026-09-05. Exact sources and license: docs/engineering/candidate-form-adapter.md.
// Htmxor upstream dependency: src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs | mirrors

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Htmxor.Endpoints;

internal readonly record struct HtmxorEndpointCandidateFormRequest(
	bool IsValid, bool IsPost, string? HandlerName, IFormCollection? Form)
{
	internal static async Task<HtmxorEndpointCandidateFormRequest> ValidateAsync(
		HttpContext context, IAntiforgery? antiforgery)
	{
		// Exception middleware preserves POST; its error page must not bind or submit the failed form.
		if (!HttpMethods.IsPost(context.Request.Method) || context.Features.Get<IExceptionHandlerFeature>() is not null)
		{
			return new(true, false, null, null);
		}

		if (HasUnsupportedContentType(context.Request))
		{
			return await RejectAsync(context, "The request has an incorrect Content-type.");
		}

		// The middleware result is authoritative even when endpoint validation is disabled.
		var valid = context.Features.Get<IAntiforgeryValidationFeature>() is { } validation
			? validation.IsValid
			: antiforgery is null || await antiforgery.IsRequestValidAsync(context);
		if (!valid)
		{
			return await RejectAsync(context, "A valid antiforgery token was not provided with the request. Add an antiforgery token, or disable antiforgery validation for this endpoint.");
		}

		var form = await context.Request.ReadFormAsync();
		if (!form.TryGetValue("_handler", out var handler))
		{
			return new(true, true, null, form);
		}

		if (handler.Count == 1)
		{
			return new(true, true, handler[0], form);
		}

		context.Response.StatusCode = StatusCodes.Status400BadRequest;
		return new(false, true, null, null);
	}

	private static bool HasUnsupportedContentType(HttpRequest request)
		=> request.ContentType is not null &&
			MediaTypeHeaderValue.TryParse(request.ContentType, out var type) &&
			!type.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) &&
			!type.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase);

	private static async Task<HtmxorEndpointCandidateFormRequest> RejectAsync(HttpContext context, string message)
	{
		context.Response.StatusCode = StatusCodes.Status400BadRequest;
		var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
		var options = context.RequestServices.GetRequiredService<IOptions<RazorComponentsServiceOptions>>();
		if (environment.IsDevelopment() || options.Value.DetailedErrors)
		{
			await context.Response.WriteAsync(message);
		}

		return new(false, true, null, null);
	}
}
