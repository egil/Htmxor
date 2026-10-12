// Own action coordination through supported Renderer roots, event dispatch, and EventCallback seams.
// Reviewed 2026-10-12 against ASP.NET Core v10.0.11, a5383385245bdacc20ec19f30e46090a8154d8da:
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Components/src/RenderTree/Renderer.cs
// and v11.0.0-rc.1.26425.128, c3325eeb6b47bc6383c127d4f4827dc9642a2b6e:
// https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Components/src/RenderTree/Renderer.cs
// EventCallback receiver/delegate coordination at the same exact versions:
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Components/src/EventCallbackFactory.cs
// https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Components/src/EventCallbackFactory.cs
// Typed event attributes use the framework builder at those same versions:
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Components/src/Rendering/RenderTreeBuilder.cs
// https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Components/src/Rendering/RenderTreeBuilder.cs
// Upstream license: ASP.NET-Core-LICENSE.txt. No private framework member is accessed.
// Htmxor upstream dependency: src/Components/Components/src/RenderTree/Renderer.cs | depends-on-behaviour
// Htmxor upstream dependency: src/Components/Components/src/EventCallbackFactory.cs | depends-on-behaviour
// Htmxor upstream dependency: src/Components/Components/src/Rendering/RenderTreeBuilder.cs | depends-on-behaviour

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
	private Type? explicitActionOwnerType;
	private IComponent? explicitActionOwner;
	internal bool HasPreparedExplicitAction => explicitActionRequest is not null;

	internal void PrepareExplicitAction(HttpContext context)
	{
		explicitActionRequest = null;
		explicitActionOwnerType = null;
		explicitActionOwner = null;
		var request = context.RequestServices.GetRequiredService<HtmxorComponentActionRequest>();
		if (request.HasExplicitAction && !isReexecuted && context.Features.Get<IExceptionHandlerFeature>() is null)
		{
			explicitActionRequest = request;
			explicitActionOwnerType = request.ExplicitOwnerType;
		}
	}

	private void ObserveExplicitActionOwner(IComponent component)
	{
		if (explicitActionOwner is null && component.GetType() == explicitActionOwnerType)
		{
			explicitActionOwner = component;
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

		if (explicitActionOwner is null || explicitActionOwner.GetType() != action.ComponentType)
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
