// Own action coordination through supported Renderer roots, event dispatch, and EventCallback seams.
// Root rendering drains global pending work left by synchronous event exception handling.
// Reviewed 2026-10-12 against ASP.NET Core v10.0.13, 4099f83977f4d29604a9aeb971fb0a7bc1d5f699:
// https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/aspnetcore/src/Components/Components/src/RenderTree/Renderer.cs
// and v11.0.0-rc.2.26504.105, 820230c386db5b0106c5430b3e5aee92a7826bf5:
// https://github.com/dotnet/dotnet/blob/820230c386db5b0106c5430b3e5aee92a7826bf5/src/aspnetcore/src/Components/Components/src/RenderTree/Renderer.cs
// EventCallback receiver/delegate coordination at the same exact versions:
// https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/aspnetcore/src/Components/Components/src/EventCallbackFactory.cs
// https://github.com/dotnet/dotnet/blob/820230c386db5b0106c5430b3e5aee92a7826bf5/src/aspnetcore/src/Components/Components/src/EventCallbackFactory.cs
// Typed event attributes use the framework builder at those same versions:
// https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/aspnetcore/src/Components/Components/src/Rendering/RenderTreeBuilder.cs
// https://github.com/dotnet/dotnet/blob/820230c386db5b0106c5430b3e5aee92a7826bf5/src/aspnetcore/src/Components/Components/src/Rendering/RenderTreeBuilder.cs
// The internal dispatch host implements the public component contract at those versions:
// https://github.com/dotnet/dotnet/blob/4099f83977f4d29604a9aeb971fb0a7bc1d5f699/src/aspnetcore/src/Components/Components/src/IComponent.cs
// https://github.com/dotnet/dotnet/blob/820230c386db5b0106c5430b3e5aee92a7826bf5/src/aspnetcore/src/Components/Components/src/IComponent.cs
// Upstream license: ASP.NET-Core-LICENSE.txt. No private framework member is accessed.
// Htmxor upstream dependency: src/Components/Components/src/RenderTree/Renderer.cs | depends-on-behaviour
// Htmxor upstream dependency: src/Components/Components/src/EventCallbackFactory.cs | depends-on-behaviour
// Htmxor upstream dependency: src/Components/Components/src/Rendering/RenderTreeBuilder.cs | depends-on-behaviour

// Shipped VMR baselines re-reviewed 2026-10-15 for #329; source deltas: docs/engineering/patched-framework-baselines.md.

using System.Reflection;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Endpoints;

internal partial class HtmxorEndpointCandidateRenderer
{
	private const string ExplicitActionEventName = "onhtmxoraction";
	private HtmxorComponentActionRequest? explicitActionRequest;
	private IComponent? explicitActionOwner;
	private HtmxorEndpointCandidateRouteCapture.Session? explicitRouteCapture;
	internal bool HasPreparedExplicitAction => explicitActionRequest is not null;

	internal void PrepareExplicitAction(HttpContext context)
	{
		explicitActionRequest = null;
		explicitActionOwner = null;
		explicitRouteCapture = null;
		var request = context.RequestServices.GetRequiredService<HtmxorComponentActionRequest>();
		if (request.HasExplicitAction && !isReexecuted && context.Features.Get<IExceptionHandlerFeature>() is null)
		{
			explicitActionRequest = request;
			explicitRouteCapture = services.GetRequiredService<HtmxorEndpointCandidateRouteCapture>().Begin();
		}
	}

	internal Task CompleteExplicitActionAsync(Task initialQuiescence)
		=> explicitActionRequest is null ? initialQuiescence : DispatchExplicitActionAsync(initialQuiescence);

	private async Task DispatchExplicitActionAsync(Task initialQuiescence)
	{
		await initialQuiescence;
		var action = explicitActionRequest!.TryConsumeExplicit();
		if (action is null)
		{
			return;
		}

		explicitActionOwner = explicitActionRequest.ExplicitRouteCarrier is { } carrier
			? carrier.Instance as IComponent
			: explicitRouteCapture!.Resolve(action.ComponentType, owner => GetComponentState(owner) is not null);
		if (explicitActionOwner is null)
		{
			return;
		}

		if (explicitActionOwner.GetType() != action.ComponentType)
		{
			throw new InvalidOperationException("The declared action owner was not created by the request renderer.");
		}

		if (GetComponentState(explicitActionOwner) is null)
		{
			return;
		}

		// A separate internal root registers a framework event without adding markup to the response root.
		// Framework dispatch retains receiver error boundaries and quiescence without rebinding the owner.
		var dispatchHost = new ExplicitActionDispatchHost(CreateExplicitCallback(explicitActionOwner, action.ExplicitHandler!));
		var dispatchHostId = AssignRootComponentId(dispatchHost);
		try
		{
			await RenderRootComponentAsync(dispatchHostId, ParameterView.Empty);
			await DispatchEventAsync(GetExplicitEventHandlerId(dispatchHostId), null,
				new HtmxEventArgs(httpContext.GetHtmxContext()), waitForQuiescence: true);
			// Synchronous dispatch exceptions can render fallback children without awaiting their pending work.
			// Re-render only the carrier to drain global quiescence without rebinding the action owner.
			await RenderRootComponentAsync(dispatchHostId, ParameterView.Empty);
		}
		finally
		{
			RemoveRootComponent(dispatchHostId);
		}
	}

	private ulong GetExplicitEventHandlerId(int dispatchHostId)
	{
		var frames = GetCurrentRenderTreeFrames(dispatchHostId);
		if (frames.Count != 2 || frames.Array[1].FrameType != RenderTreeFrameType.Attribute
			|| !string.Equals(frames.Array[1].AttributeName, ExplicitActionEventName, StringComparison.Ordinal)
			|| frames.Array[1].AttributeEventHandlerId == 0)
		{
			throw new InvalidOperationException("The framework did not register the explicit component action callback.");
		}

		return frames.Array[1].AttributeEventHandlerId;
	}

	private static EventCallback<HtmxEventArgs> CreateExplicitCallback(IComponent owner, MethodInfo handler)
	{
		if (handler.ReturnType == typeof(void))
		{
			return handler.GetParameters().Length == 0
				? EventCallback.Factory.Create<HtmxEventArgs>(owner, handler.CreateDelegate<Action>(owner))
				: EventCallback.Factory.Create<HtmxEventArgs>(owner, handler.CreateDelegate<Action<HtmxEventArgs>>(owner));
		}

		return handler.GetParameters().Length == 0
			? EventCallback.Factory.Create<HtmxEventArgs>(owner, handler.CreateDelegate<Func<Task>>(owner))
			: EventCallback.Factory.Create<HtmxEventArgs>(owner, handler.CreateDelegate<Func<HtmxEventArgs, Task>>(owner));
	}

	private sealed class ExplicitActionDispatchHost(EventCallback<HtmxEventArgs> callback) : IComponent
	{
		private RenderHandle renderHandle;

		public void Attach(RenderHandle handle) => renderHandle = handle;

		public Task SetParametersAsync(ParameterView parameters)
		{
			renderHandle.Render(builder =>
			{
				builder.OpenElement(0, "span");
				builder.AddAttribute(1, ExplicitActionEventName, callback);
				builder.CloseElement();
			});
			return Task.CompletedTask;
		}
	}
}
