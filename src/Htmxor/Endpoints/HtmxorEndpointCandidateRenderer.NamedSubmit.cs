// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from ASP.NET Core v10.0.11, commit a5383385245bdacc20ec19f30e46090a8154d8da,
// synchronized 2026-09-05. Exact sources and license: docs/engineering/candidate-form-adapter.md.
// Htmxor upstream dependency: src/Components/Endpoints/src/Rendering/EndpointHtmlRenderer.EventDispatch.cs | reimplements

using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Htmxor.Endpoints;

internal partial class HtmxorEndpointCandidateRenderer
{
	private readonly Dictionary<(int ComponentId, int FrameIndex), string> namedSubmitEventsByLocation = new();
	private readonly Dictionary<string, HashSet<(int ComponentId, int FrameIndex)>> namedSubmitEventsByScopeQualifiedName = new(StringComparer.Ordinal);

	internal Task DispatchSubmitEventAsync(string? handlerName, out bool isBadRequest)
	{
		if (string.IsNullOrEmpty(handlerName))
		{
			isBadRequest = true;
			return ReturnErrorResponse("The POST request does not specify which form is being submitted. To fix this, ensure <form> elements have a @formname attribute with any unique value, or pass a FormName parameter if using <EditForm>.");
		}

		if (!namedSubmitEventsByScopeQualifiedName.TryGetValue(handlerName, out var locationsForName) || locationsForName.Count == 0)
		{
			isBadRequest = true;
			return ReturnErrorResponse($"Cannot submit the form '{handlerName}' because no form on the page currently has that name.");
		}

		if (locationsForName.Count > 1)
		{
			throw new InvalidOperationException(CreateMessageForAmbiguousNamedSubmitEvent(handlerName, locationsForName));
		}

		isBadRequest = false;
		var frameLocation = locationsForName.Single();
		var eventHandlerId = FindEventHandlerIdForNamedEvent("onsubmit", frameLocation.ComponentId, frameLocation.FrameIndex);
		return eventHandlerId.HasValue
			? DispatchEventAsync(eventHandlerId.Value, null, EventArgs.Empty, waitForQuiescence: true)
			: Task.CompletedTask;
	}

	private string CreateMessageForAmbiguousNamedSubmitEvent(string scopeQualifiedName, IEnumerable<(int ComponentId, int FrameIndex)> locations)
	{
		var sb = new StringBuilder($"There is more than one named submit event with the name '{scopeQualifiedName}'. Ensure named submit events have unique names, or are in scopes with distinct names. The following components use this name:");

		foreach (var location in locations)
		{
			sb.Append(CultureInfo.InvariantCulture, $"\n - {GenerateComponentPath(location.ComponentId)}");
		}

		return sb.ToString();
	}

	private Task ReturnErrorResponse(string detailedMessage)
	{
		httpContext.Response.StatusCode = 400;
		httpContext.Response.ContentType = "text/plain";
		return httpContext.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true
			? httpContext.Response.WriteAsync(detailedMessage)
			: Task.CompletedTask;
	}

	protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
	{
		UpdateNamedSubmitEvents(in renderBatch);
		return base.UpdateDisplayAsync(in renderBatch);
	}

	private void UpdateNamedSubmitEvents(in RenderBatch renderBatch)
	{
		if (renderBatch.NamedEventChanges is { } changes)
		{
			// A batch can replace a named event at the same location; remove old scope membership first.
			ProcessNamedSubmitEventRemovals(changes);
			ProcessNamedSubmitEventAdditions(changes);
		}
	}

	private void ProcessNamedSubmitEventRemovals(ArrayRange<NamedEventChange> changes)
	{
		var changesCount = changes.Count;
		var changesArray = changes.Array;
		for (var i = 0; i < changesCount; i++)
		{
			ref var change = ref changesArray[i];
			if (change.ChangeType == NamedEventChangeType.Removed
				&& string.Equals(change.EventType, "onsubmit", StringComparison.Ordinal))
			{
				var location = (change.ComponentId, change.FrameIndex);
				if (namedSubmitEventsByLocation.Remove(location, out var scopeQualifiedName))
				{
					var locationsForName = namedSubmitEventsByScopeQualifiedName[scopeQualifiedName];
					locationsForName.Remove(location);
					if (locationsForName.Count == 0)
					{
						namedSubmitEventsByScopeQualifiedName.Remove(scopeQualifiedName);
					}
				}
			}
		}
	}

	private void ProcessNamedSubmitEventAdditions(ArrayRange<NamedEventChange> changes)
	{
		var changesCount = changes.Count;
		var changesArray = changes.Array;
		for (var i = 0; i < changesCount; i++)
		{
			ref var change = ref changesArray[i];
			if (change.ChangeType == NamedEventChangeType.Added
				&& string.Equals(change.EventType, "onsubmit", StringComparison.Ordinal))
			{
				if (TryCreateScopeQualifiedEventName(change.ComponentId, change.AssignedName, out var scopeQualifiedName))
				{
					var locationsForName = GetOrAddNewToDictionary(namedSubmitEventsByScopeQualifiedName, scopeQualifiedName);
					var location = (change.ComponentId, change.FrameIndex);
					if (!locationsForName.Add(location))
					{
						throw new InvalidOperationException("A single named submit event is tracked more than once at the same location.");
					}

					namedSubmitEventsByLocation.Add(location, scopeQualifiedName);
				}
			}
		}
	}

	private static TVal GetOrAddNewToDictionary<TKey, TVal>(Dictionary<TKey, TVal> dictionary, TKey key) where TKey : notnull where TVal : new()
	{
		if (!dictionary.TryGetValue(key, out var value))
		{
			value = new();
			dictionary.Add(key, value);
		}

		return value;
	}

	private ulong? FindEventHandlerIdForNamedEvent(string eventType, int componentId, int frameIndex)
	{
		var frames = GetCurrentRenderTreeFrames(componentId);
		ref var frame = ref frames.Array[frameIndex];

		if (frame.FrameType != RenderTreeFrameType.NamedEvent)
		{
			throw new InvalidOperationException($"The named value frame for component '{componentId}' at index '{frameIndex}' unexpectedly matches a frame of type '{frame.FrameType}'.");
		}

		if (!string.Equals(frame.NamedEventType, eventType, StringComparison.Ordinal))
		{
			throw new InvalidOperationException($"Expected a named value with name '{eventType}' but found the name '{frame.NamedEventType}'.");
		}

		for (var i = frameIndex - 1; i >= 0; i--)
		{
			ref var candidate = ref frames.Array[i];
			if (candidate.FrameType == RenderTreeFrameType.Attribute)
			{
				if (candidate.AttributeEventHandlerId > 0 && string.Equals(candidate.AttributeName, eventType, StringComparison.OrdinalIgnoreCase))
				{
					return candidate.AttributeEventHandlerId;
				}
			}
			else if (candidate.FrameType == RenderTreeFrameType.Element)
			{
				break;
			}
		}

		return default;
	}

	private string GenerateComponentPath(int componentId)
	{
		Stack<string> stack = new();

		for (var current = GetComponentState(componentId); current != null; current = current.ParentComponentState)
		{
			stack.Push(current.Component.GetType().Name);
		}

		var builder = new StringBuilder();
		builder.AppendJoin(" > ", stack);
		return builder.ToString();
	}
}
