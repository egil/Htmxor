// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from ASP.NET Core v10.0.11, commit a5383385245bdacc20ec19f30e46090a8154d8da,
// synchronized 2026-09-08. Exact sources and license: docs/engineering/candidate-form-adapter.md.
// Enhanced-navigation framing in InitializeStreamingRenderingFraming follows EndpointHtmlRenderer.Streaming.cs,
// first for .NET 11 at v11.0.0-rc.1.26425.128 (c3325eeb6b47bc6383c127d4f4827dc9642a2b6e, #214, synchronized
// 2026-09-13), and on both targets since #263 (synchronized 2026-10-01), identical at v10.0.11
// (a5383385245bdacc20ec19f30e46090a8154d8da): every response that is not re-executed frames, error-handler
// responses included. Direct htmx responses never frame.
// https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs
// https://github.com/dotnet/aspnetcore/blob/v11.0.0-rc.1.26425.128/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs
// https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs
// The stop gate in UpdateDisplayAsync (#264, synchronized 2026-09-30) reimplements EndpointHtmlRenderer.cs at
// v10.0.11 (a5383385245bdacc20ec19f30e46090a8154d8da) and, identically, at v11.0.0-rc.1.26425.128
// (c3325eeb6b47bc6383c127d4f4827dc9642a2b6e):
// https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs
// https://github.com/dotnet/aspnetcore/blob/v11.0.0-rc.1.26425.128/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs
// https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs
// Streaming classification and non-streaming quiescence (#260, synchronized 2026-09-30) reimplement
// EndpointComponentState.cs (the inherited StreamRendering and its hot-reload cache), EndpointHtmlRenderer.cs
// (AddPendingTask) and EndpointHtmlRenderer.Prerendering.cs (WaitForNonStreamingPendingTasks and
// HandleNavigationException) at v10.0.11 (a5383385245bdacc20ec19f30e46090a8154d8da) and at
// v11.0.0-rc.1.26425.128 (c3325eeb6b47bc6383c127d4f4827dc9642a2b6e); the two releases differ only where
// v10.0.11 stops tracking re-executed work:
// https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Rendering/EndpointComponentState.cs
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/Rendering/EndpointComponentState.cs
// https://github.com/dotnet/aspnetcore/blob/v11.0.0-rc.1.26425.128/src/Components/Endpoints/src/Rendering/EndpointComponentState.cs
// https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/Rendering/EndpointComponentState.cs
// https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Prerendering.cs
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Prerendering.cs
// https://github.com/dotnet/aspnetcore/blob/v11.0.0-rc.1.26425.128/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Prerendering.cs
// https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Prerendering.cs
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Streaming.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.Prerendering.cs | reimplements
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointComponentState.cs | reimplements

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using Htmxor.Http;
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
using Microsoft.AspNetCore.Diagnostics;

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Htmxor.Endpoints.HtmxorEndpointCandidateRenderer))]

namespace Htmxor.Endpoints;

internal partial class HtmxorEndpointCandidateRenderer
{
	private const string StreamingRenderingFramingHeaderName = "ssr-framing";
	private TextWriter? streamingUpdatesWriter;
	private string? streamingFramingCommentMarkup;
	private bool waitForQuiescence;
	private bool isReexecuted;
	private readonly List<Task> nonStreamingPendingTasks = [];
	private Task? nonStreamingPendingTasksCompletion;
	private readonly Dictionary<int, bool> streamRenderingByComponentId = [];
	private static readonly ConcurrentDictionary<Type, bool?> StreamRenderingByComponentType = new();
	private readonly HashSet<int> visitedComponentIdsInCurrentStreamingBatch = [];

	internal void InitializeStreamingRenderingFraming(HttpContext context, bool waitForQuiescence)
	{
		streamingUpdatesWriter = null;
		nonStreamingPendingTasks.Clear();
		nonStreamingPendingTasksCompletion = null;
		this.waitForQuiescence = waitForQuiescence;
		isReexecuted = context.Features.Get<IStatusCodeReExecuteFeature>() is not null;
		var allowFraming = !isReexecuted && context.GetHtmxContext().Request.RoutingMode is not RoutingMode.Direct;
		if (allowFraming && HtmxorEndpointCandidateFormServices.IsProgressivelyEnhancedNavigation(context.Request))
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
		RemoveDisposedFragments(in renderBatch);
		UpdateNamedSubmitEvents(in renderBatch);
		var writer = streamingUpdatesWriter;
		if (writer is not null && !rendererIsStopped)
		{
			SendBatchAsStreamingUpdate(in renderBatch, writer);
			return Task.WhenAll(base.UpdateDisplayAsync(in renderBatch), writer.FlushAsync());
		}
		return base.UpdateDisplayAsync(in renderBatch);
	}

	// Waits for non-streaming work until none is left, since waiting can reveal more of it, and answers a
	// navigation from any of it before the response starts, as stock does.
	internal Task WaitForNonStreamingPendingTasks()
	{
		return nonStreamingPendingTasksCompletion ??= Execute();

		async Task Execute()
		{
			while (nonStreamingPendingTasks.Count > 0)
			{
				var pendingWork = Task.WhenAll(nonStreamingPendingTasks);
				nonStreamingPendingTasks.Clear();
				try
				{
					await pendingWork;
				}
				catch (NavigationException navigationException)
				{
					HandleNavigationException(navigationException);
				}
			}
		}
	}

	internal void HandleNavigationException(NavigationException navigationException)
	{
		if (httpContext.Response.HasStarted)
		{
			// Stock's message, including its missing space, so the failure reads the same.
			throw new InvalidOperationException(
				"A navigation command was attempted during prerendering after the server already started sending the response. " +
				"Navigation commands can not be issued during server-side prerendering after the response from the server has started. Applications must buffer the" +
				"response and avoid using features like FlushAsync() before all components on the page have been rendered to prevent failed navigation commands.",
				navigationException);
		}
		HtmxorEndpointCandidateInvoker.HandleNavigationBeforeResponseStarted(httpContext, navigationException.Location);
	}

	protected override void AddPendingTask(ComponentState? componentState, Task task)
	{
#if !NET11_0_OR_GREATER
		// v10.0.11 does not track work on a re-executed request at all, so its response does not wait for it.
		if (isReexecuted)
		{
			return;
		}
#endif
		base.AddPendingTask(componentState, task);
		// Work with no owning component, such as a pending DisposeAsync, is non-streaming, as in stock.
		if (!waitForQuiescence && !IsInStreamingContext(componentState))
		{
			nonStreamingPendingTasks.Add(task);
		}
	}

	// Stock's EndpointComponentState.StreamRendering: a component's own [StreamRendering], or else its logical
	// parent's, so a component inside a streaming subtree streams too.
	private void TrackStreamRendering(int componentId, IComponent component, ComponentState state)
		=> streamRenderingByComponentId[componentId] =
			GetStreamRenderingAttribute(component) ?? IsInStreamingContext(state.LogicalParentComponentState);

	internal bool IsInStreamingContext(int componentId)
		=> streamRenderingByComponentId.TryGetValue(componentId, out var streaming) && streaming;

	private bool IsInStreamingContext(ComponentState? componentState)
		=> componentState is not null && IsInStreamingContext(componentState.ComponentId);

	// Upstream registers EndpointComponentState as a metadata update handler so this cache cannot outlive the
	// attributes it describes. Invoked by the hot reload host through reflection.
	internal static void ClearCache(Type[]? _) => StreamRenderingByComponentType.Clear();

	private static bool? GetStreamRenderingAttribute(IComponent component)
		=> StreamRenderingByComponentType.GetOrAdd(component.GetType(), static type => type
			.GetCustomAttributes(typeof(StreamRenderingAttribute), inherit: true)
			.OfType<StreamRenderingAttribute>()
			.Select(attribute => (bool?)attribute.Enabled)
			.FirstOrDefault());

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
			if (visitedComponentIdsInCurrentStreamingBatch.Contains(component.Id) || !IsInStreamingContext(component.Id))
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
