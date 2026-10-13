// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// RouteView page-fragment coordination adapted from ASP.NET Core v10.0.11,
// a5383385245bdacc20ec19f30e46090a8154d8da, and v11.0.0-rc.1.26425.128,
// c3325eeb6b47bc6383c127d4f4827dc9642a2b6e, reviewed 2026-10-13.
// Exact source URLs, signatures and ordering inventory: docs/engineering/candidate-form-adapter.md.
// Page-fragment sources:
// https://github.com/dotnet/aspnetcore/blob/a5383385245bdacc20ec19f30e46090a8154d8da/src/Components/Components/src/RouteView.cs
// https://github.com/dotnet/aspnetcore/blob/c3325eeb6b47bc6383c127d4f4827dc9642a2b6e/src/Components/Components/src/RouteView.cs
// Upstream license: ASP.NET-Core-LICENSE.txt. No private framework field is written.
// Htmxor upstream dependency: src/Components/Components/src/RouteView.cs | private-accesses
// Htmxor upstream dependency: src/Components/Components/src/RouteView.cs | reimplements
// Htmxor upstream dependency: src/Components/Authorization/src/AuthorizeRouteView.cs | private-accesses
// Htmxor upstream dependency: src/Components/Authorization/src/AuthorizeViewCore.cs | depends-on-behaviour
// Htmxor upstream dependency: src/Components/Components/src/LayoutView.cs | depends-on-behaviour
// Htmxor upstream dependency: src/Components/Components/src/Routing/Router.cs | depends-on-behaviour
// Htmxor upstream dependency: src/Components/Components/src/RenderTree/Renderer.cs | depends-on-behaviour
// Htmxor upstream dependency: src/Components/Components/src/Rendering/ComponentState.cs | depends-on-behaviour
// Htmxor upstream dependency: src/Components/Components/src/RenderTree/RenderTreeDiffBuilder.cs | depends-on-behaviour
// Htmxor upstream dependency: src/Components/Components/src/Rendering/RenderTreeBuilder.cs | depends-on-behaviour
// Htmxor upstream dependency: src/Components/Components/src/ComponentFactory.cs | depends-on-behaviour

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Routing;

namespace Htmxor.Endpoints;

internal sealed class HtmxorEndpointCandidateRouteCapture
{
	private readonly MethodInfo renderPage;
	private readonly Type authorizationCore;

	private HtmxorEndpointCandidateRouteCapture()
	{
		renderPage = typeof(RouteView).GetMethod("RenderPageWithParameters",
			BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
			binder: null, [typeof(RenderTreeBuilder)], modifiers: null)
			?? throw Incompatible("RouteView.RenderPageWithParameters(RenderTreeBuilder)");
		authorizationCore = typeof(AuthorizeRouteView).GetNestedType("AuthorizeRouteViewCore", BindingFlags.NonPublic)
			?? throw Incompatible("AuthorizeRouteView.AuthorizeRouteViewCore");
		if (!renderPage.IsPrivate || renderPage.IsGenericMethod || renderPage.ReturnType != typeof(void))
		{
			throw Incompatible("the private RouteView page-fragment signature");
		}

		if (!authorizationCore.IsNestedPrivate || !authorizationCore.IsSealed
			|| authorizationCore.BaseType != typeof(AuthorizeViewCore)
			|| !typeof(LayoutView).GetInterfaces().SequenceEqual([typeof(IComponent)]))
		{
			throw Incompatible("the authorization core or layout helper interfaces");
		}
	}

	internal static HtmxorEndpointCandidateRouteCapture Create() => new();

	internal Session Begin() => new(this);

	private bool IsPageFragment(RenderFragment fragment, RouteView view)
		=> fragment.GetInvocationList().Length == 1 && fragment.Method == renderPage
			&& ReferenceEquals(fragment.Target, view);

	private bool IsStockParent(ComponentState parent)
		=> parent.Component is RouteView || parent.Component.GetType() == authorizationCore;

	private RouteView? ReadPageView(ArrayRange<RenderTreeFrame> frames)
	{
		var offset = 0;
		if (frames.Count > 0 && frames.Array[0].FrameType == RenderTreeFrameType.Region)
		{
			if (frames.Array[0].Sequence != 0 || frames.Array[0].RegionSubtreeLength != frames.Count)
			{
				throw Incompatible("the authorization content region");
			}
			offset = 1;
		}

		if (frames.Count - offset != 3 || !IsLayoutFrame(frames.Array[offset]))
		{
			throw Incompatible("the stock RouteView layout frame without application reference captures");
		}

		var layout = frames.Array[offset + 1];
		var content = frames.Array[offset + 2];
		if (!IsParameter(layout, 1, nameof(LayoutView.Layout))
			|| !IsParameter(content, 2, nameof(LayoutView.ChildContent)))
		{
			throw Incompatible("the stock layout parameters");
		}

		// Denied/authorizing content uses stock layout rendering but is not the page fragment.
		return ReadFragmentView(content.AttributeValue);
	}

	private RouteView? ReadFragmentView(object? value)
		=> value is RenderFragment fragment && fragment.Target is RouteView view
			&& IsPageFragment(fragment, view) ? view : null;

	private static bool IsLayoutFrame(RenderTreeFrame frame)
		=> frame.FrameType == RenderTreeFrameType.Component && frame.Sequence == 0
			&& frame.ComponentType == typeof(LayoutView) && frame.ComponentSubtreeLength == 3;

	private static bool IsParameter(RenderTreeFrame frame, int sequence, string name)
		=> frame.FrameType == RenderTreeFrameType.Attribute && frame.Sequence == sequence
			&& string.Equals(frame.AttributeName, name, StringComparison.Ordinal);

	private static InvalidOperationException Incompatible(string shape)
		=> new($"ASP.NET Core's route capture shape is incompatible: {shape}. Review the monitored framework adapter before handling explicit actions.");

	internal sealed class Session(HtmxorEndpointCandidateRouteCapture adapter)
	{
		private readonly List<Router> routers = [];
		private readonly List<ViewCapture> views = [];

		internal IComponent Observe(IComponent component, ComponentState? parent,
			Func<ComponentState, ArrayRange<RenderTreeFrame>> readFrames)
		{
			RecordRoute(component, parent);
			if (component is not LayoutView || parent is null || !adapter.IsStockParent(parent))
			{
				return component;
			}

			var view = adapter.ReadPageView(readFrames(parent));
			if (view is null)
			{
				return component;
			}

			if (!ReferenceEquals(FindParent<RouteView>(parent), view) || component.GetType() != typeof(LayoutView))
			{
				throw Incompatible("the original activated layout helper and page-fragment receiver");
			}

			var capture = views.SingleOrDefault(candidate => ReferenceEquals(candidate.View, view));
			return capture is null ? component : new LayoutParameterObserver((LayoutView)component, view, adapter, capture);
		}

		private void RecordRoute(IComponent component, ComponentState? parent)
		{
			var router = FindParent<Router>(parent);
			if (component is Router outer && router is null)
			{
				routers.Add(outer);
			}
			if (component is RouteView view && router is not null && routers.Contains(router)
				&& FindParent<RouteView>(parent) is null)
			{
				// Count denied views too; a nested NotAuthorized page must never become the route owner.
				views.Add(new ViewCapture(view, router));
			}
		}

		internal IComponent? Resolve(Type ownerType, Func<IComponent, bool> isLive)
		{
			if (routers.Count != 1 || views.Count != 1 || !ReferenceEquals(views[0].Router, routers[0]))
			{
				throw new InvalidOperationException("An explicit action requires one stock Router with one outermost RouteView; no routed owner was selected.");
			}

			var selected = views[0];
			if (!isLive(selected.View))
			{
				return null;
			}
			if (selected.View.RouteData.PageType != ownerType)
			{
				throw new InvalidOperationException("The rendered route page does not own the declared explicit action.");
			}

			var owners = selected.Owners.Where(isLive).ToArray();
			if (owners.Length > 1)
			{
				throw new InvalidOperationException("The selected RouteView rendered multiple page instances; no explicit action owner was selected.");
			}
			if (owners.Length == 0 && selected.View is not AuthorizeRouteView && selected.Owners.Count == 0)
			{
				throw new InvalidOperationException("The selected RouteView did not render a compatible stock page fragment for the explicit action.");
			}

			return owners.SingleOrDefault();
		}

		private static T? FindParent<T>(ComponentState? parent) where T : class, IComponent
		{
			for (var ancestor = parent; ancestor is not null; ancestor = ancestor.ParentComponentState)
			{
				if (ancestor.Component is T component)
				{
					return component;
				}
			}
			return null;
		}
	}

	private sealed record ViewCapture(RouteView View, Router Router)
	{
		internal HashSet<IComponent> Owners { get; } = new(ReferenceEqualityComparer.Instance);
	}

	// The renderer attaches the original helper after CreateComponentState, then supplies parameters
	// through this shell. Stock LayoutView still owns rendering; the shell adds no component ancestor.
	private sealed class LayoutParameterObserver(LayoutView original, RouteView view,
		HtmxorEndpointCandidateRouteCapture adapter, ViewCapture capture) : LayoutView, IComponent
	{
		void IComponent.Attach(RenderHandle handle)
			=> throw Incompatible("the renderer's original-component attachment ordering");

		Task IComponent.SetParametersAsync(ParameterView parameters)
		{
			if (!parameters.TryGetValue<RenderFragment>(nameof(ChildContent), out var fragment)
				|| fragment is null || !adapter.IsPageFragment(fragment, view))
			{
				throw Incompatible("the selected RouteView page-fragment parameters");
			}

			var values = new Dictionary<string, object?>(parameters.ToDictionary(), StringComparer.Ordinal);
			values[nameof(ChildContent)] = (RenderFragment)RenderPage;
			return original.SetParametersAsync(ParameterView.FromDictionary(values));
		}

		private void RenderPage(RenderTreeBuilder builder)
		{
			builder.OpenComponent(0, view.RouteData.PageType);
			foreach (var parameter in view.RouteData.RouteValues)
			{
				builder.AddComponentParameter(1, parameter.Key, parameter.Value);
			}
			builder.AddComponentReferenceCapture(2, instance => capture.Owners.Add((IComponent)instance));
			builder.CloseComponent();
		}
	}
}
