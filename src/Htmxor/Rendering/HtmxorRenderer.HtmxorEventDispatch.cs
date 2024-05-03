using System.Globalization;
using System.Text;
using Htmxor.Http;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Htmxor.Rendering;

internal partial class HtmxorRenderer
{
	// This maps between the hash of the hx-xxx="url" attribute and one or more event handlers delegates and ultimately the componentId and eventhandler id.
	// The reason is that multiple event bindings can point to the same event handler (delegate), but it is also possible that multiple 
	// different event bindings with different delegates are set on different elements with which has the same hx-xxx="url".
	// This keeps track of the collisions as well as the individual duplicated bindings, such that disposed event handler bindings
	// can be correctly cleaned up as needed.
	private readonly Dictionary<string, Dictionary<Delegate, List<(int ComponentId, ulong EventHandlerId)>>> htmxorEvents = new(StringComparer.Ordinal);

	[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Method names are safe to convert to lower.")]
	internal Task DispatchHtmxorEventAsync(HtmxContext context, out bool isBadRequest)
	{
		var htmxorEventId = context.Request.EventHandlerId;

		if (string.IsNullOrEmpty(htmxorEventId))
		{
			isBadRequest = true;
			return ReturnErrorResponse($"The {context.Request.Method} request does not specify which event handler should be invoked.");
		}

		if (!htmxorEvents.TryGetValue(htmxorEventId, out var handlerInfoSet) || handlerInfoSet.Count == 0)
		{
			// This may happen if you deploy an app update and someone still on the old page submits a form,
			// or if you're dynamically building the UI and the submitted form doesn't exist the next time
			// the page is rendered
			isBadRequest = true;
			return ReturnErrorResponse($"Cannot handle the 'hx-{context.Request.Method.ToLowerInvariant()}' request because event handler specified in the request does not exists on the page currently.");
		}

		if (handlerInfoSet.Count > 1)
		{
			// We could allow multiple events with the same name, since they are all tracked separately. However
			// this is most likely a mistake on the developer's part so we will consider it an error.
			// This is an internal server error, not a bad request, because the application itself is at fault
			// and needs to find out about it. End users can't trigger this unless the app has a bug.
			throw new InvalidOperationException(CreateMessageForAmbiguousHtmxorEvent($"@on{context.Request.Method.ToLowerInvariant()}", handlerInfoSet.Values.SelectMany(x => x)));
		}

		isBadRequest = false;
		var eventHandlerId = handlerInfoSet.First().Value[0].EventHandlerId;
		return DispatchEventAsync(eventHandlerId, null, new HtmxEventArgs(context), waitForQuiescence: true);
	}

	private string CreateMessageForAmbiguousHtmxorEvent(string htmxEventName, IEnumerable<(int ComponentId, ulong EventHandlerId)> handlerInfoSet)
	{
		var sb = new StringBuilder(
			$"There is more than one event handler bound to the current '{htmxEventName}' request. " +
			$"Multiple elements can have the same '{htmxEventName}' . " +
			$"The following components use this name:");

		foreach (var (componentId, _) in handlerInfoSet)
			sb.Append(CultureInfo.InvariantCulture, $"\n - {GenerateComponentPath(componentId)}");

		return sb.ToString();
	}

	private void UpdateHtmxorEvents(in RenderBatch renderBatch)
	{
		RemoveDisposedHtmxorEvents(in renderBatch);
		AddHtmxorEvents(in renderBatch);
	}

	private void RemoveDisposedHtmxorEvents(in RenderBatch renderBatch)
	{
		for (int i = 0; i < renderBatch.DisposedEventHandlerIDs.Count; i++)
		{
			var eventHandlerId = renderBatch.DisposedEventHandlerIDs.Array[i];
			if (htmxorEventsByEventHandlerId.TryGetValue(eventHandlerId, out var eventIdHandlerPair)
				&& htmxorEvents.TryGetValue(eventIdHandlerPair.HtmxorEventId, out var handlerInfoSet)
				&& handlerInfoSet.TryGetValue(eventIdHandlerPair.Handler, out var list))
			{

				if (list.RemoveAll(x => x.EventHandlerId == eventHandlerId) > 0)
				{
					if (list.Count == 0)
					{
						handlerInfoSet.Remove(eventIdHandlerPair.Handler);
					}

					break;
				}
			}
		}
	}

	private void AddHtmxorEvents(in RenderBatch renderBatch)
	{
		var frameCount = renderBatch.ReferenceFrames.Count;
		var framesArray = renderBatch.ReferenceFrames.Array;
		var componentId = 0;
		var componentFrameIndex = 0;
		for (var i = 0; i < frameCount; i++)
		{
			ref var frame = ref framesArray[i];
			if (frame.FrameType == RenderTreeFrameType.Component)
			{
				componentId = frame.ComponentId;
				componentFrameIndex = i + 1;
				continue;
			}

			if (frame.FrameType != RenderTreeFrameType.Attribute || frame.AttributeEventHandlerId == 0)
			{
				continue;
			}

			if (IsHxEventActionAttribute(ref frame))
			{
				ref var hxUrlFrame = ref FindHxActionAttribute(in framesArray, i);
				var htmxorEventId = CreateHxActionHash(ref hxUrlFrame);
				var handlerInfoSet = GetOrAddNewToDictionary(htmxorEvents, htmxorEventId);

				var handler = (Delegate)frame.AttributeValue;
				var handlerInfoList = GetOrAddNewToDictionary(handlerInfoSet, handler);

				handlerInfoList.Add((componentId, frame.AttributeEventHandlerId));
				htmxorEventsByEventHandlerId.Add(frame.AttributeEventHandlerId, (htmxorEventId, handler));
			}
		}
	}

	private static ref RenderTreeFrame FindHxActionAttribute(in RenderTreeFrame[] frames, int start)
	{
		const string hxActionPrefix = "hx-";

		// Search back in frames from start until frame.FrameType != RenderTreeFrameType.Attribute
		// and forward until frame.FrameType != RenderTreeFrameType.Attribute, looking for
		// an attribute with the name hx-XXX where XXX matches start frames onXXX name. Return
		// the value of that frame, or throw exception.
		// this assumes all attributes on an element is adjacent in the frames array.
		ref var handlerFrame = ref frames[start];
		var hxActionName = hxActionPrefix + handlerFrame.AttributeName[2..];

		for (int i = start - 1; i >= 0; i--)
		{
			ref var candidate = ref frames[i];
			if (candidate.FrameType != RenderTreeFrameType.Attribute)
			{
				break;
			}

			if (candidate.AttributeName.Equals(hxActionName, StringComparison.OrdinalIgnoreCase) && candidate.AttributeValue is string url)
			{
				return ref candidate;
			}
		}

		for (int i = start + 1; i < frames.Length; i++)
		{
			ref var candidate = ref frames[i];
			if (candidate.FrameType != RenderTreeFrameType.Attribute)
			{
				break;
			}

			if (candidate.AttributeName.Equals(hxActionName, StringComparison.OrdinalIgnoreCase) && candidate.AttributeValue is string url)
			{
				return ref candidate;
			}
		}

		throw new InvalidOperationException($"Could not find an hx-XXX attribute matching the onXXX attribute '{handlerFrame.AttributeName}'.");
	}
}
