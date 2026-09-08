// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from ASP.NET Core v10.0.11, commit a5383385245bdacc20ec19f30e46090a8154d8da,
// synchronized 2026-09-08. Exact sources and license: docs/engineering/candidate-form-adapter.md.
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs | reimplements

using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using Htmxor.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Htmxor.Endpoints;

internal partial class HtmxorEndpointCandidateRenderer
{
	private const string StreamingRenderingFramingHeaderName = "ssr-framing";
	private TextWriter? streamingUpdatesWriter;
	private string? streamingFramingCommentMarkup;
	private bool waitForQuiescence;
	private bool hasStreamingComponent;
	private readonly List<Task> nonStreamingPendingTasks = [];
	private readonly HashSet<int> visitedComponentIdsInCurrentStreamingBatch = [];

	internal void InitializeStreamingRenderingFraming(HttpContext context, bool waitForQuiescence)
	{
		streamingUpdatesWriter = null;
		hasStreamingComponent = false;
		nonStreamingPendingTasks.Clear();
		this.waitForQuiescence = waitForQuiescence;
		if (!waitForQuiescence && HtmxorEndpointCandidateFormServices.IsProgressivelyEnhancedNavigation(context.Request))
		{
			var identifier = Guid.NewGuid().ToString();
			context.Response.Headers[StreamingRenderingFramingHeaderName] = identifier;
			streamingFramingCommentMarkup = $"<!--{identifier}-->";
		}
		else
		{
			streamingFramingCommentMarkup = string.Empty;
		}
	}

	internal async Task SendStreamingUpdatesAsync(HttpContext context, Task untilCompleted, TextWriter writer)
	{
		if (streamingUpdatesWriter is not null)
		{
			throw new InvalidOperationException($"{nameof(SendStreamingUpdatesAsync)} can only be called once.");
		}
		if (streamingFramingCommentMarkup is null)
		{
			throw new InvalidOperationException("Cannot begin streaming rendering because no framing header was set.");
		}

		streamingUpdatesWriter = writer;
		#pragma warning disable CA1849 // Framing and writer subscription must happen before yielding the renderer context.
		writer.Write(streamingFramingCommentMarkup);
		#pragma warning restore CA1849 // Call async methods when in an async method.
		EmitInitializersIfNecessary(context, writer);
		await writer.FlushAsync();
		try
		{
			await untilCompleted;
		}
		catch (NavigationException exception)
		{
			WriteNavigationAfterResponseStarted(writer, context, exception.Location);
		}
		catch (Exception exception)
		{
			WriteExceptionAfterResponseStarted(writer, context, exception);
			await writer.FlushAsync();
			await context.Response.CompleteAsync();
			throw;
		}
	}

	protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
	{
		UpdateNamedSubmitEvents(in renderBatch);
		for (var index = 0; !waitForQuiescence && index < renderBatch.UpdatedComponents.Count; index++)
		{
			if (IsStreamingComponent(renderBatch.UpdatedComponents.Array[index].ComponentId))
			{
				hasStreamingComponent = true;
				break;
			}
		}
		var writer = streamingUpdatesWriter;
		if (writer is not null)
		{
			SendBatchAsStreamingUpdate(in renderBatch, writer);
			return Task.WhenAll(base.UpdateDisplayAsync(in renderBatch), writer.FlushAsync());
		}
		return base.UpdateDisplayAsync(in renderBatch);
	}

	internal bool HasStreamingComponent => hasStreamingComponent;

	internal Task WaitForNonStreamingPendingTasks()
		=> nonStreamingPendingTasks.Count == 0
			? Task.CompletedTask
			: Task.WhenAll(nonStreamingPendingTasks);

	protected override void AddPendingTask(ComponentState? componentState, Task task)
	{
		base.AddPendingTask(componentState, task);
		if (!waitForQuiescence && componentState is not null && !IsStreamingComponent(componentState.ComponentId))
		{
			nonStreamingPendingTasks.Add(task);
		}
	}

	internal void WriteNotFoundAfterResponseStarted(HttpContext context, TextWriter writer)
	{
		var path = NotFoundEventArgs?.Path;
		if (string.IsNullOrEmpty(path))
		{
			path = context.Items["StatusCodePagesOptions"] as string
				?? throw new InvalidOperationException("The Router NotFoundPage route must be specified or re-execution middleware has to be set to render not found content.");
		}
		var baseUri = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}/";
		WriteResponseTemplate(writer, context, "not-found", $"{baseUri}{path.TrimStart('/')}", useEnhancedNavigation: true);
	}

	private void SendBatchAsStreamingUpdate(in RenderBatch renderBatch, TextWriter writer)
	{
		var count = renderBatch.UpdatedComponents.Count;
		if (count == 0)
		{
			return;
		}

		writer.Write("<blazor-ssr>");
		var bufferSize = count * Marshal.SizeOf<ComponentIdAndDepth>();
		var componentIdsInDepthOrder = bufferSize < 1024
			? MemoryMarshal.Cast<byte, ComponentIdAndDepth>(stackalloc byte[bufferSize])
			: new ComponentIdAndDepth[count];
		for (var index = 0; index < count; index++)
		{
			var componentId = renderBatch.UpdatedComponents.Array[index].ComponentId;
			componentIdsInDepthOrder[index] = new(componentId, GetComponentDepth(componentId));
		}
		componentIdsInDepthOrder.Sort(static (left, right) => left.Depth.CompareTo(right.Depth));

		visitedComponentIdsInCurrentStreamingBatch.Clear();
		var enhancedNavigation = HtmxorEndpointCandidateFormServices.IsProgressivelyEnhancedNavigation(httpContext.Request);
		foreach (var component in componentIdsInDepthOrder)
		{
			if (visitedComponentIdsInCurrentStreamingBatch.Contains(component.Id) || !IsStreamingComponent(component.Id))
			{
				continue;
			}

			writer.Write("<template blazor-component-id=\"");
			writer.Write(component.Id);
			writer.Write(enhancedNavigation ? "\" enhanced-nav=\"true\">" : "\">");
			WriteComponentHtml(component.Id, writer, 0, null, allowStreamingMarkers: false);
			writer.Write("</template>");
		}
		writer.Write("<blazor-ssr-end></blazor-ssr-end></blazor-ssr>");
		writer.Write(streamingFramingCommentMarkup);
	}

	private int GetComponentDepth(int componentId)
	{
		var state = GetComponentState(componentId);
		var depth = 0;
		while (state.ParentComponentState is { } parent)
		{
			depth++;
			state = parent;
		}
		return depth;
	}

	private bool IsStreamingComponent(int componentId)
		=> GetComponentState(componentId).Component.GetType()
			.GetCustomAttributes(typeof(StreamRenderingAttribute), inherit: true)
			.OfType<StreamRenderingAttribute>()
			.Any(attribute => attribute.Enabled);

	private static void WriteNavigationAfterResponseStarted(TextWriter writer, HttpContext context, string destination)
		=> WriteResponseTemplate(
			writer,
			context,
			"redirection",
			destination,
			HtmxorEndpointCandidateFormServices.IsProgressivelyEnhancedNavigation(context.Request));

	private static void WriteExceptionAfterResponseStarted(TextWriter writer, HttpContext context, Exception exception)
	{
		var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
		var options = context.RequestServices.GetRequiredService<IOptions<RazorComponentsServiceOptions>>();
		var message = environment.IsDevelopment() || options.Value.DetailedErrors
			? exception.ToString()
			: "There was an unhandled exception on the current request. For more details turn on detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json'";
		writer.Write("<blazor-ssr><template type=\"error\">");
		writer.Write(HtmlEncoder.Default.Encode(message));
		writer.Write("</template><blazor-ssr-end></blazor-ssr-end></blazor-ssr>");
	}

	private static void WriteResponseTemplate(
		TextWriter writer,
		HttpContext context,
		string type,
		string destination,
		bool useEnhancedNavigation)
	{
		writer.Write("<blazor-ssr><template type=\"");
		writer.Write(type);
		writer.Write("\"");
		if (HttpMethods.IsPost(context.Request.Method))
		{
			writer.Write(" from=\"form-post\"");
		}
		if (useEnhancedNavigation)
		{
			writer.Write(" enhanced=\"true\"");
		}
		writer.Write(">");
		writer.Write(HtmlEncoder.Default.Encode(OpaqueRedirection.CreateProtectedRedirectionUrl(context, destination)));
		writer.Write("</template><blazor-ssr-end></blazor-ssr-end></blazor-ssr>");
	}

	private readonly record struct ComponentIdAndDepth(int Id, int Depth);
}
