// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from ASP.NET Core v10.0.13, commit 4099f83977f4d29604a9aeb971fb0a7bc1d5f699,
// synchronized 2026-09-05. Exact sources and license: docs/engineering/candidate-form-adapter.md.
// .NET 11 validation follows v11.0.0-rc.2.26504.105 at
// 820230c386db5b0106c5430b3e5aee92a7826bf5, synchronized 2026-09-13; see that inventory.
// PUT/PATCH action form mapping extends stock POST coordination for #318, reviewed 2026-10-23
// against both baselines above; the stock mapper accepts an empty name for an unscoped form.
// Htmxor upstream dependency: src/Components/Endpoints/src/RazorComponentEndpointInvoker.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs | mirrors
// Htmxor upstream dependency: src/Components/Endpoints/src/FormMapping/HttpContextFormValueMapper.cs | depends-on-behaviour

// Shipped VMR baselines re-reviewed 2026-10-15 for #329; source deltas: docs/engineering/patched-framework-baselines.md.

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
		var hasActiveAction = context.RequestServices.GetRequiredService<HtmxorComponentActionRequest>().HasActiveAction;
		var isFormAction = hasActiveAction &&
			(HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsPatch(context.Request.Method));
		// Exception middleware preserves the method; its error page must not bind or submit the failed form.
		if ((!HttpMethods.IsPost(context.Request.Method) && !isFormAction) ||
			context.Features.Get<IExceptionHandlerFeature>() is not null)
		{
			return new(true, false, null, null);
		}

		// Activated callbacks need form state when present, but must not also dispatch a named form.
		var dispatchForm = !hasActiveAction;
		if (!dispatchForm && !context.Request.HasFormContentType)
		{
			return new(true, false, null, null);
		}

		if (HasUnsupportedContentType(context.Request))
		{
			return await RejectAsync(context, "The request has an incorrect Content-type.");
		}

		// The middleware result is authoritative even when endpoint validation is disabled.
#if NET11_0_OR_GREATER
		var valid = context.Features.Get<IAntiforgeryValidationFeature>() is not { IsValid: false };
#else
		var valid = context.Features.Get<IAntiforgeryValidationFeature>() is { } validation
			? validation.IsValid
			: antiforgery is null || await antiforgery.IsRequestValidAsync(context);
#endif
		if (!valid)
		{
			return await RejectAsync(context, "A valid antiforgery token was not provided with the request. Add an antiforgery token, or disable antiforgery validation for this endpoint.");
		}

		var form = await context.Request.ReadFormAsync();
		if (!form.TryGetValue("_handler", out var handler))
		{
			// Null means no incoming form to the stock mapper. An unnamed action uses the empty form name.
			return new(true, dispatchForm, isFormAction ? string.Empty : null, form);
		}

		if (handler.Count == 1)
		{
			return new(true, dispatchForm, handler[0], form);
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
