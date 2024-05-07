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
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Htmxor.Endpoints;

internal partial class HtmxorComponentEndpointInvoker : IHtmxorComponentEndpointInvoker
{
	public const string DefaultContentType = "text/html; charset=utf-8";

	private readonly HtmxorRenderer renderer;
	private readonly ILogger<HtmxorComponentEndpointInvoker> logger;

	public HtmxorComponentEndpointInvoker(HtmxorRenderer renderer, ILogger<HtmxorComponentEndpointInvoker> logger)
	{
		this.renderer = renderer;
		this.logger = logger;
	}

	public Task Render(HttpContext context)
		=> renderer.Dispatcher.InvokeAsync(() => RenderComponentCore(context));

	private async Task RenderComponentCore(HttpContext context)
	{
		context.Response.ContentType = DefaultContentType;
		var isErrorHandler = context.Features.Get<IExceptionHandlerFeature>() is not null;
		if (isErrorHandler)
		{
			Log.InteractivityDisabledForErrorHandling(logger);
		}

		var endpoint = context.GetEndpoint() ?? throw new InvalidOperationException($"An endpoint must be set on the '{nameof(HttpContext)}'.");

		var rootComponent = endpoint.Metadata.GetRequiredMetadata<RootComponentMetadata>().Type;
		var pageComponent = endpoint.Metadata.GetRequiredMetadata<ComponentTypeMetadata>().Type;
		var layoutComponent = endpoint.Metadata.GetMetadata<LayoutComponentMetadata>()?.Type;
		Log.BeginRenderRootComponent(logger, rootComponent.Name, pageComponent.Name);

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
			componentType: pageComponent,
			layoutType: layoutComponent,
			handler: result.HandlerName,
			form: result.IsFormDataRequest && context.Request.HasFormContentType
				? await context.Request.ReadFormAsync()
				: null);

		// Note that we always use Static rendering mode for the top-level output from a RazorComponentResult,
		// because you never want to serialize the invocation of RazorComponentResultHost. Instead, that host
		// component takes care of switching into your desired render mode when it produces its own output.
		var htmlContent = await renderer.RenderEndpointComponent(
			context,
			rootComponent,
			ParameterView.Empty);

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
				var isBadRequest = false;
				quiesceTask = htmxContext.Request.EventHandlerId is not null
					? renderer.DispatchHtmxorEventAsync(htmxContext, out isBadRequest)
					: renderer.DispatchSubmitEventAsync(result.HandlerName, out isBadRequest);

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
		await using var writer = new HttpResponseStreamWriter(context.Response.Body, Encoding.UTF8, defaultBufferSize, ArrayPool<byte>.Shared, ArrayPool<char>.Shared);
		using var bufferWriter = new ConditionalBufferedTextWriter(writer);

		// Importantly, we must not yield this thread (which holds exclusive access to the renderer sync context)
		// in between the first call to htmlContent.WriteTo and the point where we start listening for subsequent
		// streaming SSR batches (inside SendStreamingUpdatesAsync). Otherwise some other code might dispatch to the
		// renderer sync context and cause a batch that would get missed.
#pragma warning disable CA1849 // Don't use WriteToAsync, as per the comment above
		htmlContent.WriteTo(bufferWriter, HtmlEncoder.Default);
#pragma warning restore CA1849 // Call async methods when in an async method

		// Invoke FlushAsync to ensure any buffered content is asynchronously written to the underlying
		// response asynchronously. In the absence of this line, the buffer gets synchronously written to the
		// response as part of the Dispose which has a perf impact.
		await bufferWriter.FlushAsync();
	}

	private async Task<RequestValidationState> ValidateRequestAsync(HttpContext context, IAntiforgery? antiforgery)
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
					Log.MiddlewareAntiforgeryValidationFailed(logger);
				}
				else
				{
					valid = true;
					Log.MiddlewareAntiforgeryValidationSucceeded(logger);
				}
			}
			else
			{
				if (antiforgery == null)
				{
					valid = true;
					Log.EndpointAntiforgeryValidationDisabled(logger);
				}
				else
				{
					valid = await antiforgery.IsRequestValidAsync(context);
					if (valid)
					{
						Log.EndpointAntiforgeryValidationSucceeded(logger);
					}
					else
					{
						Log.EndpointAntiforgeryValidationFailed(logger);
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

	public static partial class Log
	{
		[LoggerMessage(1, LogLevel.Debug, "Begin render root component '{componentType}' with page '{pageType}'.", EventName = nameof(BeginRenderRootComponent))]
		public static partial void BeginRenderRootComponent(ILogger<HtmxorComponentEndpointInvoker> logger, string componentType, string pageType);

		[LoggerMessage(2, LogLevel.Debug, "The antiforgery middleware already failed to validate the current token.", EventName = nameof(MiddlewareAntiforgeryValidationFailed))]
		public static partial void MiddlewareAntiforgeryValidationFailed(ILogger<HtmxorComponentEndpointInvoker> logger);

		[LoggerMessage(3, LogLevel.Debug, "The antiforgery middleware already succeeded to validate the current token.", EventName = nameof(MiddlewareAntiforgeryValidationSucceeded))]
		public static partial void MiddlewareAntiforgeryValidationSucceeded(ILogger<HtmxorComponentEndpointInvoker> logger);

		[LoggerMessage(4, LogLevel.Debug, "The endpoint disabled antiforgery token validation.", EventName = nameof(EndpointAntiforgeryValidationDisabled))]
		public static partial void EndpointAntiforgeryValidationDisabled(ILogger<HtmxorComponentEndpointInvoker> logger);

		[LoggerMessage(5, LogLevel.Information, "Antiforgery token validation failed for the current request.", EventName = nameof(EndpointAntiforgeryValidationFailed))]
		public static partial void EndpointAntiforgeryValidationFailed(ILogger<HtmxorComponentEndpointInvoker> logger);

		[LoggerMessage(6, LogLevel.Debug, "Antiforgery token validation succeeded for the current request.", EventName = nameof(EndpointAntiforgeryValidationSucceeded))]
		public static partial void EndpointAntiforgeryValidationSucceeded(ILogger<HtmxorComponentEndpointInvoker> logger);

		[LoggerMessage(7, LogLevel.Debug, "Error handling in progress. Interactive components are not enabled.", EventName = nameof(InteractivityDisabledForErrorHandling))]
		public static partial void InteractivityDisabledForErrorHandling(ILogger<HtmxorComponentEndpointInvoker> logger);
	}
}
