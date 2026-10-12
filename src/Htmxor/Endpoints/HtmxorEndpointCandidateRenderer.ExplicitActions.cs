// Own action coordination through supported Renderer/CreateComponentState/AddPendingTask and EventCallback seams.
// Reviewed 2026-10-12 against ASP.NET Core v10.0.11, a5383385245bdacc20ec19f30e46090a8154d8da:
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Components/src/RenderTree/Renderer.cs
// and v11.0.0-rc.1.26425.128, c3325eeb6b47bc6383c127d4f4827dc9642a2b6e:
// https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Components/src/RenderTree/Renderer.cs
// EventCallback receiver/delegate coordination at the same exact versions:
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Components/src/EventCallbackFactory.cs
// https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Components/src/EventCallbackFactory.cs
// Upstream license: ASP.NET-Core-LICENSE.txt. No private framework member is accessed.
// Renderer stops collecting prerender tasks when initial quiescence finishes. Collect only the explicit event's
// subsequent work through its supported hook; rebinding the root would run parameters and lifecycle twice.
// Htmxor upstream dependency: src/Components/Components/src/RenderTree/Renderer.cs | depends-on-behaviour
// Htmxor upstream dependency: src/Components/Components/src/EventCallbackFactory.cs | depends-on-behaviour

using System.Reflection;
using Htmxor.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Htmxor.Endpoints;

internal partial class HtmxorEndpointCandidateRenderer
{
	private HtmxorComponentActionRequest? explicitActionRequest;
	private Type? explicitActionOwnerType;
	private IComponent? explicitActionOwner;
	private List<Task>? explicitActionPendingTasks;

	internal void PrepareExplicitAction(HttpContext context)
	{
		explicitActionRequest = null;
		explicitActionOwnerType = null;
		explicitActionOwner = null;
		explicitActionPendingTasks = null;
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

	private void TrackExplicitActionPendingTask(Task task) => explicitActionPendingTasks?.Add(task);

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

		explicitActionPendingTasks = [];
		try
		{
			await CreateExplicitCallback(explicitActionOwner, action.ExplicitHandler!).InvokeAsync(new HtmxEventArgs(httpContext.GetHtmxContext()));
			while (explicitActionPendingTasks.Count > 0)
			{
				var pending = Task.WhenAll(explicitActionPendingTasks);
				explicitActionPendingTasks.Clear();
				await pending;
			}
		}
		finally
		{
			explicitActionPendingTasks = null;
		}
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
}
