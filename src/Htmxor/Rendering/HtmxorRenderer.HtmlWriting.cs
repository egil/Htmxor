// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using Htmxor.Components;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Htmxor.Rendering;

internal partial class HtmxorRenderer
{
	private static readonly HashSet<string> SelfClosingElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
	};

	private static readonly CascadingParameterInfo CascadingParameterInfoInstance =
		(CascadingParameterInfo)Activator.CreateInstance(
			typeof(CascadingParameterInfo),
			BindingFlags.NonPublic | BindingFlags.Instance,
			binder: null,
			args: [new CascadingParameterAttribute(), string.Empty, typeof(FormMappingContext)],
			culture: CultureInfo.InvariantCulture)!;

	private readonly TextEncoder javaScriptEncoder;
	private TextEncoder htmlEncoder;
	private string? closestSelectValueAsString;

	/// <summary>
	/// Renders the specified component as HTML to the output.
	/// </summary>
	/// <param name="componentId">The ID of the component whose current HTML state is to be rendered.</param>
	/// <param name="output">The output destination.</param>
	private void WriteComponent(int componentId, TextWriter output)
	{
		// We're about to walk over some buffers inside the renderer that can be mutated during rendering.
		// So, we require exclusive access to the renderer during this synchronous process.
		Dispatcher.AssertAccess();

		var frames = GetCurrentRenderTreeFrames(componentId);
		RenderFrames(componentId, output, frames, 0, frames.Count);
	}

	/// <summary>
	/// Renders the specified component frame as HTML to the output.
	/// </summary>
	/// <param name="output">The output destination.</param>
	/// <param name="componentFrame">The <see cref="RenderTreeFrame"/> representing the component to be rendered.</param>
	private void RenderChildComponent(TextWriter output, ref RenderTreeFrame componentFrame)
	{
		WriteComponent(componentFrame.ComponentId, output);
	}

	private int RenderFrames(int componentId, TextWriter output, ArrayRange<RenderTreeFrame> frames, int position, int maxElements)
	{
		var nextPosition = position;
		var endPosition = position + maxElements;
		while (position < endPosition)
		{
			nextPosition = RenderCore(componentId, output, frames, position);
			if (position == nextPosition)
			{
				throw new InvalidOperationException("We didn't consume any input.");
			}
			position = nextPosition;
		}

		return nextPosition;
	}

	private int RenderCore(
		int componentId,
		TextWriter output,
		ArrayRange<RenderTreeFrame> frames,
		int position)
	{
		ref var frame = ref frames.Array[position];
		switch (frame.FrameType)
		{
			case RenderTreeFrameType.Element:
				return RenderElement(componentId, output, frames, position);
			case RenderTreeFrameType.Attribute:
				throw new InvalidOperationException($"Attributes should only be encountered within {nameof(RenderElement)}");
			case RenderTreeFrameType.Text:
				htmlEncoder.Encode(output, frame.TextContent);
				return ++position;
			case RenderTreeFrameType.Markup:
				output.Write(frame.MarkupContent);
				return ++position;
			case RenderTreeFrameType.Component:
				return RenderChildComponent(output, frames, position);
			case RenderTreeFrameType.Region:
				return RenderFrames(componentId, output, frames, position + 1, frame.RegionSubtreeLength - 1);
			case RenderTreeFrameType.ElementReferenceCapture:
			case RenderTreeFrameType.ComponentReferenceCapture:
				return ++position;
			case RenderTreeFrameType.NamedEvent:
				RenderHiddenFieldForNamedSubmitEvent(componentId, output, frames, position);
				return ++position;
			default:
				throw new InvalidOperationException($"Invalid element frame type '{frame.FrameType}'.");
		}
	}

	private int RenderElement(int componentId, TextWriter output, ArrayRange<RenderTreeFrame> frames, int position)
	{
		ref var frame = ref frames.Array[position];
		output.Write('<');
		output.Write(frame.ElementName);
		int afterElement;
		var isTextArea = string.Equals(frame.ElementName, "textarea", StringComparison.OrdinalIgnoreCase);
		var isForm = string.Equals(frame.ElementName, "form", StringComparison.OrdinalIgnoreCase);

		// We don't want to include value attribute of textarea element.
		var afterAttributes = RenderAttributes(output, frames, position + 1, frame.ElementSubtreeLength - 1, !isTextArea, isForm: isForm, out var capturedValueAttribute);

		// When we see an <option> as a descendant of a <select>, and the option's "value" attribute matches the
		// "value" attribute on the <select>, then we auto-add the "selected" attribute to that option. This is
		// a way of converting Blazor's select binding feature to regular static HTML.
		if (closestSelectValueAsString != null
			&& string.Equals(frame.ElementName, "option", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(capturedValueAttribute, closestSelectValueAsString, StringComparison.Ordinal))
		{
			output.Write(" selected");
		}

		var remainingElements = frame.ElementSubtreeLength + position - afterAttributes;
		if (remainingElements > 0 || isTextArea)
		{
			output.Write('>');

			var isSelect = string.Equals(frame.ElementName, "select", StringComparison.OrdinalIgnoreCase);
			if (isSelect)
			{
				closestSelectValueAsString = capturedValueAttribute;
			}

			if (isTextArea && !string.IsNullOrEmpty(capturedValueAttribute))
			{
				// Textarea is a special type of form field where the value is given as text content instead of a 'value' attribute
				// So, if we captured a value attribute, use that instead of any child content
				htmlEncoder.Encode(output, capturedValueAttribute);
				afterElement = position + frame.ElementSubtreeLength; // Skip descendants
			}
			else if (string.Equals(frame.ElementName, "script", StringComparison.OrdinalIgnoreCase))
			{
				afterElement = RenderScriptElementChildren(componentId, output, frames, afterAttributes, remainingElements);
			}
			else
			{
				afterElement = RenderChildren(componentId, output, frames, afterAttributes, remainingElements);
			}

			if (isSelect)
			{
				// There's no concept of nested <select> elements, so as soon as we're exiting one of them,
				// we can safely say there is no longer any value for this
				closestSelectValueAsString = null;
			}

			output.Write("</");
			output.Write(frame.ElementName);
			output.Write('>');
			Debug.Assert(afterElement == position + frame.ElementSubtreeLength);
			return afterElement;
		}
		else
		{
			if (SelfClosingElements.Contains(frame.ElementName))
			{
				output.Write(" />");
			}
			else
			{
				output.Write("></");
				output.Write(frame.ElementName);
				output.Write('>');
			}
			Debug.Assert(afterAttributes == position + frame.ElementSubtreeLength);
			return afterAttributes;
		}
	}

	private int RenderScriptElementChildren(int componentId, TextWriter output, ArrayRange<RenderTreeFrame> frames, int position, int maxElements)
	{
		// Inside a <script> context, AddContent calls should result in the text being
		// JavaScript encoded rather than HTML encoded. It's not that we recommend inserting
		// user-supplied content inside a <script> block, but that if someone does, we
		// want the encoding style to match the context for correctness and safety. This is
		// also consistent with .cshtml's treatment of <script>.
		var originalEncoder = htmlEncoder;
		try
		{
			htmlEncoder = javaScriptEncoder;
			return RenderChildren(componentId, output, frames, position, maxElements);
		}
		finally
		{
			htmlEncoder = originalEncoder;
		}
	}

	private void RenderHiddenFieldForNamedSubmitEvent(int componentId, TextWriter output, ArrayRange<RenderTreeFrame> frames, int namedEventFramePosition)
	{
		// Strictly speaking we could just emit the hidden input unconditionally, but since we currently
		// only intend to support this for "form submit" events, validate that's the case
		ref var namedEventFrame = ref frames.Array[namedEventFramePosition];
		if (string.Equals(namedEventFrame.NamedEventType, "onsubmit", StringComparison.Ordinal)
			&& TryFindEnclosingElementFrame(frames, namedEventFramePosition, out var enclosingElementFrameIndex))
		{
			ref var enclosingElementFrame = ref frames.Array[enclosingElementFrameIndex];
			if (string.Equals(enclosingElementFrame.ElementName, "form", StringComparison.OrdinalIgnoreCase))
			{
				if (TryCreateScopeQualifiedEventName(componentId, namedEventFrame.NamedEventAssignedName, out var combinedFormName))
				{
					output.Write("<input type=\"hidden\" name=\"_handler\" value=\"");
					htmlEncoder.Encode(output, combinedFormName);
					output.Write("\" />");
				}
			}
		}
	}

	/// <summary>
	/// Creates the fully scope-qualified name for a named event, if the component is within
	/// a <see cref="FormMappingContext"/> (whether or not that mapping context is named).
	/// </summary>
	/// <param name="componentId">The ID of the component that defines a named event.</param>
	/// <param name="assignedEventName">The name assigned to the named event.</param>
	/// <param name="scopeQualifiedEventName">The scope-qualified event name.</param>
	/// <returns>A flag to indicate whether a value could be produced.</returns>
	protected bool TryCreateScopeQualifiedEventName(int componentId, string assignedEventName, [NotNullWhen(true)] out string? scopeQualifiedEventName)
	{
		if (FindFormMappingContext(componentId) is { } mappingContext)
		{
			var mappingScopeName = mappingContext.MappingScopeName;
			scopeQualifiedEventName = string.IsNullOrEmpty(mappingScopeName)
				? assignedEventName
				: $"[{mappingScopeName}]{assignedEventName}";
			return true;
		}
		else
		{
			scopeQualifiedEventName = null;
			return false;
		}
	}

	private FormMappingContext? FindFormMappingContext(int forComponentId)
	{
		var componentState = GetComponentState(forComponentId);

		// This code was replaced by the reflection based code below
		//var supplier = CascadingParameterState.GetMatchingCascadingValueSupplier(
		//    in _findFormMappingContext,
		//    this,
		//    componentState);

		var serviceProviderCascadingValueSuppliersProperty = typeof(Renderer).GetProperty("ServiceProviderCascadingValueSuppliers", BindingFlags.Instance | BindingFlags.NonPublic)!;
		var rendererValueCascadingValueSuppliers = (object[])serviceProviderCascadingValueSuppliersProperty.GetValue(this)!;
		var valueSupplierType = rendererValueCascadingValueSuppliers.GetType().GetElementType()!;

		var supplier = GetMatchingCascadingValueSupplier(
			in CascadingParameterInfoInstance,
			componentState,
			rendererValueCascadingValueSuppliers,
			valueSupplierType);

		if (supplier is not null)
		{
			var getCurrentValueMethod = valueSupplierType.GetMethod("GetCurrentValue", BindingFlags.Instance | BindingFlags.Public)!;
			return (FormMappingContext?)getCurrentValueMethod.Invoke(supplier, [CascadingParameterInfoInstance]);
		}

		return null;
	}

	internal static object? GetMatchingCascadingValueSupplier(
		in CascadingParameterInfo info,
		ComponentState? componentState,
		object[] rendererValueCascadingValueSuppliers,
		Type valueSupplierType)
	{
		var canSupplyValueMethod = valueSupplierType.GetMethod("CanSupplyValue", BindingFlags.Instance | BindingFlags.Public)!;

		// First scan up through the component hierarchy
		var candidate = componentState;
		while (candidate is not null)
		{
			var isValueSupplier = Array.Exists(candidate.Component.GetType().GetInterfaces(), x => x == valueSupplierType);

			if (isValueSupplier && (bool)canSupplyValueMethod.Invoke(candidate.Component, [info])!)
			{
				return candidate.Component;
			}

			candidate = candidate.LogicalParentComponentState;
		}

		// We got to the root and found no match, so now look at the providers registered in DI
		foreach (var valueSupplier in rendererValueCascadingValueSuppliers)
		{
			if ((bool)canSupplyValueMethod.Invoke(valueSupplier, [info])!)
			{
				return valueSupplier;
			}
		}

		// No match
		return null;
	}

	private static bool TryFindEnclosingElementFrame(ArrayRange<RenderTreeFrame> frames, int frameIndex, out int result)
	{
		while (--frameIndex >= 0)
		{
			if (frames.Array[frameIndex].FrameType == RenderTreeFrameType.Element)
			{
				result = frameIndex;
				return true;
			}
		}

		result = default;
		return false;
	}

	private int RenderAttributes(
		TextWriter output,
		ArrayRange<RenderTreeFrame> frames,
		int position,
		int maxElements,
		bool includeValueAttribute,
		bool isForm,
		out string? capturedValueAttribute)
	{
		capturedValueAttribute = null;

		if (maxElements == 0)
		{
			EmitFormActionIfNotExplicit(output, isForm, hasExplicitActionValue: false);
			return position;
		}

		var hasHxAction = false;
		var hasExplicitActionValue = false;
		ulong hxEventHandlerId = 0;
		for (var i = 0; i < maxElements; i++)
		{
			var candidateIndex = position + i;
			ref var frame = ref frames.Array[candidateIndex];

			if (frame.FrameType != RenderTreeFrameType.Attribute)
			{
				if (frame.FrameType == RenderTreeFrameType.ElementReferenceCapture)
				{
					continue;
				}

				EmitFormActionIfNotExplicit(output, isForm, hasExplicitActionValue);
				EmitHtmxorEventHandlerId(output);

				return candidateIndex;
			}

			if (frame.AttributeName.Equals("value", StringComparison.OrdinalIgnoreCase))
			{
				capturedValueAttribute = frame.AttributeValue as string;

				if (!includeValueAttribute)
				{
					continue;
				}
			}

			if (isForm && HasActionAttribute(ref frame))
			{
				hasExplicitActionValue = true;
			}

			if (!hasHxAction && IsHxActionAttribute(ref frame))
			{
				hasHxAction = true;
			}
			else if (hxEventHandlerId == 0 && IsHxEventActionAttribute(ref frame) && frame.AttributeEventHandlerId > 0)
			{
				hxEventHandlerId = frame.AttributeEventHandlerId;
			}

			switch (frame.AttributeValue)
			{
				case bool flag when flag:
					output.Write(' ');
					output.Write(frame.AttributeName);
					break;
				case string value:
					output.Write(' ');
					output.Write(frame.AttributeName);
					output.Write('=');
					output.Write('\"');
					htmlEncoder.Encode(output, value);
					output.Write('\"');
					break;
				default:
					break;
			}
		}

		EmitFormActionIfNotExplicit(output, isForm, hasExplicitActionValue);
		EmitHtmxorEventHandlerId(output);
		return position + maxElements;

		void EmitFormActionIfNotExplicit(TextWriter output, bool isForm, bool hasExplicitActionValue)
		{
			if (isForm && navigationManager != null && !hasExplicitActionValue)
			{
				output.Write(' ');
				output.Write("action");
				output.Write('=');
				output.Write('\"');
				htmlEncoder.Encode(output, GetRootRelativeUrlForFormAction(navigationManager));
				output.Write('\"');
			}
		}

		void EmitHtmxorEventHandlerId(TextWriter output)
		{
			// TODO: handle hx-header already existing.
			// hx-headers='{"myHeader": "My Value"}'
			if (hasHxAction && hxEventHandlerId > 0)
			{
				output.Write(' ');
				output.Write("hx-headers");
				output.Write('=');
				output.Write("'{\"");
				output.Write(HtmxRequestHeaderNames.EventHandlerId);
				output.Write("\":\"");
				output.Write(htmxorEventsByEventHandlerId[hxEventHandlerId].HtmxorEventId);
				output.Write("\"}'");
			}
		}
	}

	internal static bool HasActionAttribute(ref RenderTreeFrame frame)
		=> (IsHxActionAttribute(ref frame) || frame.AttributeName.Equals("action", StringComparison.OrdinalIgnoreCase))
		&& !string.IsNullOrEmpty(frame.AttributeValue as string);

	internal static bool IsHxActionAttribute(ref RenderTreeFrame frame)
		=> frame.AttributeName.Equals(HtmxConstants.ActionAttributes.Get, StringComparison.OrdinalIgnoreCase)
		|| frame.AttributeName.Equals(HtmxConstants.ActionAttributes.Post, StringComparison.OrdinalIgnoreCase)
		|| frame.AttributeName.Equals(HtmxConstants.ActionAttributes.Put, StringComparison.OrdinalIgnoreCase)
		|| frame.AttributeName.Equals(HtmxConstants.ActionAttributes.Delete, StringComparison.OrdinalIgnoreCase)
		|| frame.AttributeName.Equals(HtmxConstants.ActionAttributes.Patch, StringComparison.OrdinalIgnoreCase);

	internal static bool IsHxEventActionAttribute(ref RenderTreeFrame frame)
		=> frame.AttributeName.Equals(HtmxConstants.EventActionAttributes.Get, StringComparison.OrdinalIgnoreCase)
		|| frame.AttributeName.Equals(HtmxConstants.EventActionAttributes.Post, StringComparison.OrdinalIgnoreCase)
		|| frame.AttributeName.Equals(HtmxConstants.EventActionAttributes.Put, StringComparison.OrdinalIgnoreCase)
		|| frame.AttributeName.Equals(HtmxConstants.EventActionAttributes.Delete, StringComparison.OrdinalIgnoreCase)
		|| frame.AttributeName.Equals(HtmxConstants.EventActionAttributes.Patch, StringComparison.OrdinalIgnoreCase);

	internal static string CreateHxActionHash(ref RenderTreeFrame hxActionFrame)
	{
		// Implementation of the 32 bit Fnv1a hash function
		const uint offsetBasis = 2166136261;
		const uint fnvPrime = 16777619;

		uint hash = offsetBasis;
		foreach (byte datum in Encoding.UTF8.GetBytes($"{hxActionFrame.AttributeName}={hxActionFrame.AttributeValue}"))
		{
			hash ^= datum;
			hash *= fnvPrime;
		}

		return hash.ToString("X8", CultureInfo.InvariantCulture);
	}

	private static string GetRootRelativeUrlForFormAction(NavigationManager navigationManager)
	{
		// We want a root-relative URL because:
		// - if we used a base-relative one, then if currentUrl==baseHref, that would result
		//   in an empty string, but forms have special handling for action="" (it means "submit
		//   to the current URL, but that would be wrong if there's an uncommitted navigation in
		//   flight, e.g., after the user clicking 'back' - it would go to whatever's now in the
		//   address bar, ignoring where the form was rendered)
		// - if we used an absolute URL, then it creates a significant extra pit of failure for
		//   apps hosted behind a reverse proxy (e.g., container apps), because the server's view
		//   of the absolute URL isn't usable outside the container
		//   - of course, sites hosted behind URL rewriting that modifies the path will still be
		//     wrong, but developers won't do that often as it makes things like <a href> really
		//     difficult to get right. In that case, developers must emit an action attribute manually.
		return new Uri(navigationManager.Uri, UriKind.Absolute).PathAndQuery;
	}

	private int RenderChildren(int componentId, TextWriter output, ArrayRange<RenderTreeFrame> frames, int position, int maxElements)
	{
		if (maxElements == 0)
		{
			return position;
		}

		return RenderFrames(componentId, output, frames, position, maxElements);
	}

	private int RenderChildComponent(TextWriter output, ArrayRange<RenderTreeFrame> frames, int position)
	{
		ref var frame = ref frames.Array[position];
		RenderChildComponent(output, ref frame);
		return position + frame.ComponentSubtreeLength;
	}
}
