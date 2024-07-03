// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using Htmxor.Builder;
using Htmxor.DependencyInjection;
using Htmxor.Rendering;
using Htmxor.Rendering.Buffering;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static Htmxor.LinkerFlags;

namespace Htmxor.Endpoints.Results;

internal static partial class HtmxorComponentResultExecutor
{
	public const string DefaultContentType = "text/html; charset=utf-8";

	public static Task ExecuteAsync(HttpContext httpContext, HtmxorComponentResult result)
	{
		ArgumentNullException.ThrowIfNull(httpContext);

		var response = httpContext.Response;
		response.ContentType = result.ContentType ?? DefaultContentType;

		if (result.StatusCode != null)
		{
			response.StatusCode = result.StatusCode.Value;
		}

		return RenderComponentToResponse(
			httpContext,
			result.ComponentType,
			result.Parameters);
	}

	private static Task RenderComponentToResponse(
		HttpContext context,
		[DynamicallyAccessedMembers(Component)] Type componentType,
		IReadOnlyDictionary<string, object?>? componentParameters,
		string? handler = null,
		IFormCollection? form = null)
	{
		var endpointHtmlRenderer = context.RequestServices.GetRequiredService<HtmxorRenderer>();
		return endpointHtmlRenderer.Dispatcher.InvokeAsync(async () =>
		{
			var isErrorHandler = context.Features.Get<IExceptionHandlerFeature>() is not null;
			if (isErrorHandler)
			{
				// Log.InteractivityDisabledForErrorHandling(logger);
			}

			var endpoint = context.GetEndpoint() ?? throw new InvalidOperationException($"An endpoint must be set on the '{nameof(HttpContext)}'.");

			// Metadata controls whether we require antiforgery protection for this endpoint or we should skip it.
			// The default for razor component endpoints is to require the metadata, but it can be overriden by
			// the developer.
			var antiforgeryMetadata = endpoint.Metadata.GetMetadata<IAntiforgeryMetadata>();
			var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
			var result = await ValidateRequestAsync(context, antiforgeryMetadata?.RequiresValidation == true ? antiforgery : null);

			if (!result.IsValid)
			{
				// If the request is not valid we've already set the response to a 400 or similar
				// and we can just exit early.
				return;
			}

			context.Response.OnStarting(() =>
			{
				// Generate the antiforgery tokens before we start streaming the response, as it needs
				// to set the cookie header.
				antiforgery!.GetAndStoreTokens(context);
				return Task.CompletedTask;
			});

			HtmxorRenderer.InitializeStandardComponentServices(
				context,
				componentType: typeof(HtmxorComponentEndpointHost),
				layoutType: null,
				handler: result.HandlerName,
				form: result.IsFormDataRequest && context.Request.HasFormContentType
					? await context.Request.ReadFormAsync()
					: null);

			// We could pool these dictionary instances if we wanted, and possibly even the ParameterView
			// backing buffers could come from a pool like they do during rendering.
			var hostParameters = ParameterView.FromDictionary(new Dictionary<string, object?>
			{
				{ nameof(HtmxorComponentEndpointHost.ComponentType), componentType },
				{ nameof(HtmxorComponentEndpointHost.ComponentParameters), componentParameters },
			});

			// Note that we don't set any interactive rendering mode for the top-level output from a RazorComponentResult,
			// because you never want to serialize the invocation of RazorComponentResultHost. Instead, that host
			// component takes care of switching into your desired render mode when it produces its own output.
			var htmlContent = await endpointHtmlRenderer.RenderEndpointComponent(context, typeof(HtmxorComponentEndpointHost), hostParameters);

			var htmxContext = context.GetHtmxContext();
			Task quiesceTask = Task.CompletedTask;
			if (!result.IsFormDataRequest && htmxContext.Request.EventHandlerId is null)
			{
				quiesceTask = htmlContent.QuiescenceTask;
			}
			else if (result.HandlerName is not null || htmxContext.Request.EventHandlerId is not null)
			{
				try
				{
					quiesceTask = htmxContext.Request.EventHandlerId is not null
						? endpointHtmlRenderer.DispatchHtmxorEventAsync(htmxContext, out var isBadRequest)
						: endpointHtmlRenderer.DispatchSubmitEventAsync(result.HandlerName, out isBadRequest);

					if (isBadRequest)
					{
						return;
					}

					await quiesceTask;
				}
				catch (HtmxorNavigationException navigationException)
				{
					await HtmxorRenderer.HandleNavigationException(context, navigationException);
					quiesceTask = Task.CompletedTask;
				}
			}

			if (!quiesceTask.IsCompleted)
			{
				// An incomplete QuiescenceTask indicates there may be streaming rendering updates.
				// Disable all response buffering and compression on IIS like SignalR's ServerSentEventsServerTransport does.
				var bufferingFeature = context.Features.GetRequiredFeature<IHttpResponseBodyFeature>();
				bufferingFeature.DisableBuffering();
				context.Response.Headers.ContentEncoding = "identity";
			}

			if (context.Response.StatusCode == (int)HttpStatusCode.NoContent || htmxContext.Response.EmptyResponseBodyRequested)
			{
				return;
			}

			// Matches MVC's MemoryPoolHttpResponseStreamWriterFactory.DefaultBufferSize
			var defaultBufferSize = 16 * 1024;
			using var writer = new HttpResponseStreamWriter(context.Response.Body, Encoding.UTF8, defaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
			using var bufferWriter = new ConditionalBufferedTextWriter(writer);


			// Importantly, we must not yield this thread (which holds exclusive access to the renderer sync context)
			// in between the first call to htmlContent.WriteTo and the point where we start listening for subsequent
			// streaming SSR batches (inside SendStreamingUpdatesAsync). Otherwise some other code might dispatch to the
			// renderer sync context and cause a batch that would get missed.
#pragma warning disable CA1849
			htmlContent.WriteTo(bufferWriter, HtmlEncoder.Default); // Don't use WriteToAsync, as per the comment above
#pragma warning restore CA1849

			// Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
			// response asynchronously. In the absence of this line, the buffer gets synchronously written to the
			// response as part of the Dispose which has a perf impact.
			await bufferWriter.FlushAsync();
		});
	}

	private static async Task<RequestValidationState> ValidateRequestAsync(HttpContext context, IAntiforgery? antiforgery)
	{
		var processFormDataRequest = HttpMethods.IsPost(context.Request.Method)
			// Htmx supports both PUT and PATCH in addition to POST.
			|| HttpMethods.IsPut(context.Request.Method)
			|| HttpMethods.IsPatch(context.Request.Method)
			// Disable POST functionality during exception handling.
			// The exception handler middleware will not update the request method, and we don't
			// want to run the form handling logic against the error page.
			&& context.Features.Get<IExceptionHandlerFeature>() == null;

		var request = context.GetHtmxContext().Request;
		if (processFormDataRequest)
		{
			var valid = false;
			// Respect the token validation done by the middleware _if_ it has been set, otherwise
			// run the validation here.
			if (context.Features.Get<IAntiforgeryValidationFeature>() is { } antiForgeryValidationFeature)
			{
				if (!antiForgeryValidationFeature.IsValid)
				{
					//Log.MiddlewareAntiforgeryValidationFailed(logger);
				}
				else
				{
					valid = true;
					//Log.MiddlewareAntiforgeryValidationSucceeded(logger);
				}
			}
			else
			{
				if (antiforgery == null)
				{
					valid = true;
					//Log.EndpointAntiforgeryValidationDisabled(logger);
				}
				else
				{
					valid = await antiforgery.IsRequestValidAsync(context);
					if (valid)
					{
						//Log.EndpointAntiforgeryValidationSucceeded(logger);
					}
					else
					{
						//Log.EndpointAntiforgeryValidationFailed(logger);
					}
				}
			}

			if (!valid)
			{
				context.Response.StatusCode = StatusCodes.Status400BadRequest;

				if (context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true)
				{
					await context.Response.WriteAsync("A valid antiforgery token was not provided with the request. Add an antiforgery token, or disable antiforgery validation for this endpoint.");
				}

				return RequestValidationState.InvalidPostRequest;
			}

			// Read the form asynchronously to ensure Request.Form has been populated.
			await context.Request.ReadFormAsync();

			var handler = GetFormHandler(context, out var isBadRequest);
			return handler is null && !isBadRequest
				? RequestValidationState.ValidHtmxorFormDataRequest
				: new RequestValidationState(valid && !isBadRequest, processFormDataRequest, handler);
		}

		return RequestValidationState.ValidNonFormDataRequest;
	}

	private static string? GetFormHandler(HttpContext context, out bool isBadRequest)
	{
		isBadRequest = false;
		if (context.Request.Form.TryGetValue("_handler", out var value))
		{
			if (value.Count != 1)
			{
				context.Response.StatusCode = StatusCodes.Status400BadRequest;
				isBadRequest = true;
			}
			else
			{
				return value[0]!;
			}
		}
		return null;
	}

	[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
	private readonly struct RequestValidationState(bool isValid, bool isFormDataRequest, string? handlerName)
	{
		public static readonly RequestValidationState ValidHtmxorFormDataRequest = new(true, true, null);
		public static readonly RequestValidationState ValidNonFormDataRequest = new(true, false, null);
		public static readonly RequestValidationState InvalidPostRequest = new(false, true, null);

		public bool IsValid => isValid;

		public bool IsFormDataRequest => isFormDataRequest;

		public string? HandlerName => handlerName;

		private string GetDebuggerDisplay()
		{
			return $"IsValid = {IsValid}, IsFormDataRequest = {IsFormDataRequest}, HandlerName = {HandlerName}";
		}
	}
}
